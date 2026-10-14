using RushArena.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RushArena.Input
{
    public sealed class DesktopInput : IPlayerInput
    {
        public float DegreesPerPixel = 0.09f;
        public bool IsActive => Application.isFocused && Cursor.lockState == CursorLockMode.Locked;
        public PlayerCommand Sample()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Clear();
                return default;
            }
            // Clicking captures the pointer but discards that frame's stale mouse delta.
            if (!IsActive)
            {
                if (Application.isFocused && mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                return default;
            }
            PlayerCommand command = default;
            if (keyboard != null)
            {
                command.MoveX = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                command.MoveY = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
                command.Sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                command.JumpPressed = keyboard.spaceKey.wasPressedThisFrame;
                command.SwitchWeapon = keyboard.qKey.wasPressedThisFrame || keyboard.digit1Key.wasPressedThisFrame || keyboard.digit2Key.wasPressedThisFrame;
            }
            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                command.LookX = delta.x * DegreesPerPixel;
                command.LookY = delta.y * DegreesPerPixel;
                command.Fire = mouse.leftButton.isPressed;
            }
            return command;
        }
        public void Clear() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
