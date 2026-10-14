using RushArena.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RushArena.Input
{
    public sealed class DesktopInput : IPlayerInput
    {
        public static float SuppressFireUntil;
        public static bool IsSettingsOpen;
        private static int suppressLookFrames;
        public float DegreesPerPixel = 0.035f;
        public bool IsActive => Application.isFocused && Cursor.lockState == CursorLockMode.Locked && !IsSettingsOpen;
        public PlayerCommand Sample()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Clear();
                return default;
            }
            if (IsSettingsOpen)
            {
                return default;
            }
            // Clicking captures the pointer but discards initial frames to eliminate cursor-center warp spikes
            if (!IsActive)
            {
                if (Application.isFocused && mouse != null && mouse.leftButton.wasPressedThisFrame && !IsSettingsOpen)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    SuppressFireUntil = Time.unscaledTime + 0.20f;
                    suppressLookFrames = 3;
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
                command.ReloadPressed = keyboard.rKey.wasPressedThisFrame;
                if (keyboard.qKey.wasPressedThisFrame)
                {
                    command.SwitchWeapon = true;
                    command.WeaponScroll = 1;
                }
            }
            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                if (suppressLookFrames > 0)
                {
                    suppressLookFrames--;
                    delta = Vector2.zero;
                }
                else
                {
                    // Clamp per-frame delta to eliminate browser/DPI warp spikes
                    delta.x = Mathf.Clamp(delta.x, -45f, 45f);
                    delta.y = Mathf.Clamp(delta.y, -45f, 45f);
                }

                float sens = DegreesPerPixel * RushArena.Runtime.GameSettings.MouseSensitivity;
                command.LookX = delta.x * sens;
                command.LookY = delta.y * sens * (RushArena.Runtime.GameSettings.InvertY ? -1f : 1f);
                command.Fire = mouse.leftButton.isPressed && Time.unscaledTime >= SuppressFireUntil;
                command.ZoomPressed = mouse.rightButton.isPressed;

                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.05f)
                {
                    command.SwitchWeapon = true;
                    command.WeaponScroll = 1;
                }
                else if (scroll < -0.05f)
                {
                    command.SwitchWeapon = true;
                    command.WeaponScroll = -1;
                }
            }
            return command;
        }
        public void Clear() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; suppressLookFrames = 2; }
    }
}
