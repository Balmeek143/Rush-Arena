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
        public bool IsActive => Application.isFocused && !IsSettingsOpen && (RushArena.Runtime.GameSettings.TouchControlsEnabled || Cursor.lockState == CursorLockMode.Locked);
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
            // Clicking captures the pointer in desktop mode, but touch mode stays unlocked
            if (!IsActive)
            {
                if (!RushArena.Runtime.GameSettings.TouchControlsEnabled && Application.isFocused && mouse != null && mouse.leftButton.wasPressedThisFrame && !IsSettingsOpen)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    SuppressFireUntil = Time.unscaledTime + 0.20f;
                    suppressLookFrames = 3;
                }
                return default;
            }
            PlayerCommand command = default;

            if (RushArena.Runtime.GameSettings.TouchControlsEnabled)
            {
                VirtualTouchControls.UpdateInput();

                command.MoveX = VirtualTouchControls.MoveX;
                command.MoveY = VirtualTouchControls.MoveY;
                if (VirtualTouchControls.Fire) command.Fire = true;
                if (VirtualTouchControls.ConsumeJump()) command.JumpPressed = true;
                if (VirtualTouchControls.ConsumeReload()) command.ReloadPressed = true;
                if (VirtualTouchControls.ConsumeSwitch())
                {
                    command.SwitchWeapon = true;
                    command.WeaponScroll = 1;
                }
                Vector2 touchLook = VirtualTouchControls.ConsumeLook();
                command.LookX += touchLook.x;
                command.LookY += touchLook.y;
            }

            if (keyboard != null)
            {
                float keyX = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                float keyY = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
                if (Mathf.Abs(keyX) > 0.01f || Mathf.Abs(keyY) > 0.01f)
                {
                    command.MoveX = keyX;
                    command.MoveY = keyY;
                }
                if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed) command.Sprint = true;
                if (keyboard.spaceKey.wasPressedThisFrame) command.JumpPressed = true;
                if (keyboard.rKey.wasPressedThisFrame) command.ReloadPressed = true;
                if (keyboard.qKey.wasPressedThisFrame)
                {
                    command.SwitchWeapon = true;
                    command.WeaponScroll = 1;
                }
            }

            if (!RushArena.Runtime.GameSettings.TouchControlsEnabled && mouse != null)
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
                command.LookX += delta.x * sens;
                command.LookY += delta.y * sens * (RushArena.Runtime.GameSettings.InvertY ? -1f : 1f);
                command.Fire = command.Fire || (mouse.leftButton.isPressed && Time.unscaledTime >= SuppressFireUntil);
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
