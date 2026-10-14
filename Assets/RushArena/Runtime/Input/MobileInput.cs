using RushArena.Core;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace RushArena.Input
{
    // Touch ownership is assigned on Began. Fingers never turn into different controls mid-drag.
    public sealed class MobileInput : IPlayerInput
    {
        private int moveFinger = -1, lookFinger = -1, sprintFinger = -1, fireFinger = -1;
        private Vector2 moveOrigin, move, lastLook;
        private bool suppressUntilReleased;
        public bool IsActive => Application.isFocused;
        public Vector2 Move => move;
        public Rect JumpArea { get; private set; }
        public Rect SprintArea { get; private set; }
        public Rect FireArea { get; private set; }
        public Rect SwitchArea { get; private set; }
        public Rect SafeArea { get; private set; }
        public float JoystickRadius { get; private set; }

        public void UpdateLayout()
        {
            SafeArea = Screen.safeArea;
            float unit = Mathf.Min(SafeArea.width, SafeArea.height);
            JoystickRadius = unit * 0.15f;
            float size = unit * 0.17f;
            JumpArea = new Rect(SafeArea.xMax - size * 1.2f, SafeArea.yMin + size * 0.25f, size, size);
            SprintArea = new Rect(SafeArea.xMax - size * 2.4f, SafeArea.yMin + size * 0.25f, size, size);
            FireArea = new Rect(SafeArea.xMax - size * 1.2f, SafeArea.yMin + size * 1.45f, size, size);
            SwitchArea = new Rect(SafeArea.xMax - size * 2.4f, SafeArea.yMin + size * 1.45f, size, size);
        }

        public PlayerCommand Sample() => SampleWithFocus(IsActive);

        internal PlayerCommand SampleWithFocus(bool focused)
        {
            UpdateLayout();
            if (!focused) { Clear(); return default; }
            var touches = Touch.activeTouches;
            if (suppressUntilReleased)
            {
                if (touches.Count == 0) suppressUntilReleased = false;
                return default;
            }
            PlayerCommand command = default;
            bool sawMove = false, sawLook = false, sawSprint = false, sawFire = false;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                int id = touch.finger.index;
                Vector2 position = touch.screenPosition;
                bool ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                if (touch.phase == TouchPhase.Began && SafeArea.Contains(position))
                {
                    if (JumpArea.Contains(position)) command.JumpPressed = true;
                    else if (SwitchArea.Contains(position)) command.SwitchWeapon = true;
                    else if (FireArea.Contains(position) && fireFinger < 0) fireFinger = id;
                    else if (SprintArea.Contains(position) && sprintFinger < 0) sprintFinger = id;
                    else if (position.x < SafeArea.center.x && moveFinger < 0)
                    { moveFinger = id; moveOrigin = position; }
                    else if (position.x >= SafeArea.center.x && lookFinger < 0)
                    { lookFinger = id; lastLook = position; }
                }
                if (id == moveFinger)
                {
                    sawMove = !ended;
                    if (ended) { moveFinger = -1; move = Vector2.zero; }
                    else move = Vector2.ClampMagnitude((position - moveOrigin) / Mathf.Max(1f, JoystickRadius), 1f);
                }
                if (id == lookFinger)
                {
                    sawLook = !ended;
                    if (ended) lookFinger = -1;
                    else
                    {
                        Vector2 delta = (position - lastLook) * (180f / Mathf.Max(1f, Mathf.Min(SafeArea.width, SafeArea.height)));
                        command.LookX += delta.x;
                        command.LookY += delta.y;
                        lastLook = position;
                    }
                }
                if (id == sprintFinger) { sawSprint = !ended; if (ended) sprintFinger = -1; }
                if (id == fireFinger) { sawFire = !ended; if (ended) fireFinger = -1; }
            }
            // A disappeared/cancelled finger cannot leave a stuck movement or sprint state.
            if (!sawMove) { moveFinger = -1; move = Vector2.zero; }
            if (!sawLook) lookFinger = -1;
            if (!sawSprint) sprintFinger = -1;
            if (!sawFire) fireFinger = -1;
            command.MoveX = move.x; command.MoveY = move.y; command.Sprint = sprintFinger >= 0; command.Fire = fireFinger >= 0;
            return command;
        }

        public void Clear()
        {
            moveFinger = lookFinger = sprintFinger = fireFinger = -1;
            move = Vector2.zero;
            suppressUntilReleased = true;
        }
    }
}
