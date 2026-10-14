using RushArena.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RushArena.Input
{
    // High-performance virtual touch & on-screen controls for Mobile/iPad and WebGL.
    // Provides dual-thumb controls: virtual movement joystick, large prominent FIRE button,
    // tactile JUMP/RELOAD/SWITCH buttons, and smooth touch swipe-to-aim.
    public static class VirtualTouchControls
    {
        public static float MoveX { get; private set; }
        public static float MoveY { get; private set; }
        public static bool Fire { get; private set; }
        private static bool jumpQueued;
        private static bool reloadQueued;
        private static bool switchQueued;
        private static bool slideQueued;
        private static Vector2 lookDeltaAccumulator;

        // Joystick configuration
        private static readonly Vector2 BaseStickPos = new Vector2(105f, 0f); // Y calculated from Screen.height
        private static Vector2 currentKnobPos;
        private static bool isStickActive;
        private static int stickFingerId = -1;

        // Touch tracking
        private static int lookFingerId = -1;
        private static Vector2 lastLookPos;
        private static int fireFingerId = -1;

        public static bool ConsumeJump()
        {
            bool j = jumpQueued;
            jumpQueued = false;
            return j;
        }

        public static bool ConsumeReload()
        {
            bool r = reloadQueued;
            reloadQueued = false;
            return r;
        }

        public static bool ConsumeSwitch()
        {
            bool s = switchQueued;
            switchQueued = false;
            return s;
        }

        public static bool ConsumeSlide()
        {
            bool s = slideQueued;
            slideQueued = false;
            return s;
        }

        public static Vector2 ConsumeLook()
        {
            Vector2 l = lookDeltaAccumulator;
            lookDeltaAccumulator = Vector2.zero;
            return l;
        }

        public static void Reset()
        {
            MoveX = 0f;
            MoveY = 0f;
            Fire = false;
            jumpQueued = false;
            reloadQueued = false;
            switchQueued = false;
            slideQueued = false;
            lookDeltaAccumulator = Vector2.zero;
            isStickActive = false;
            stickFingerId = -1;
            lookFingerId = -1;
            fireFingerId = -1;
        }

        public static void UpdateInput()
        {
            if (!GameSettings.TouchControlsEnabled)
            {
                Reset();
                return;
            }

            Vector2 stickCenter = new Vector2(BaseStickPos.x, Screen.height - 180f);
            float stickRadius = 60f;

            // Define button rects in screen coordinates (0,0 is bottom-left in InputSystem)
            Rect fireRectScreen = new Rect(Screen.width - 135f, 150f, 110f, 110f);
            Rect jumpRectScreen = new Rect(Screen.width - 135f, 275f, 110f, 65f);
            Rect reloadRectScreen = new Rect(Screen.width - 250f, 150f, 95f, 60f);
            Rect switchRectScreen = new Rect(Screen.width - 250f, 220f, 95f, 60f);
            Rect slideRectScreen = new Rect(Screen.width - 250f, 290f, 95f, 60f);
            Rect moveZoneScreen = new Rect(15f, 60f, 200f, 220f);

            bool sawStick = false;
            bool sawFire = false;
            bool sawLook = false;

            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.touches.Count > 0)
            {
                for (int i = 0; i < touchscreen.touches.Count; i++)
                {
                    var touch = touchscreen.touches[i];
                    if (!touch.press.isPressed) continue;

                    int id = touch.touchId.ReadValue();
                    Vector2 screenPos = touch.position.ReadValue();

                    // Button taps
                    if (touch.press.wasPressedThisFrame)
                    {
                        if (jumpRectScreen.Contains(screenPos)) jumpQueued = true;
                        else if (reloadRectScreen.Contains(screenPos)) reloadQueued = true;
                        else if (switchRectScreen.Contains(screenPos)) switchQueued = true;
                        else if (slideRectScreen.Contains(screenPos)) slideQueued = true;
                    }

                    // Fire button
                    if (fireRectScreen.Contains(screenPos))
                    {
                        fireFingerId = id;
                        sawFire = true;
                    }
                    else if (id == fireFingerId)
                    {
                        sawFire = true;
                    }

                    // Movement Joystick
                    if (id == stickFingerId || (stickFingerId < 0 && moveZoneScreen.Contains(screenPos)))
                    {
                        stickFingerId = id;
                        sawStick = true;

                        Vector2 stickScreenCenter = new Vector2(stickCenter.x, Screen.height - stickCenter.y);
                        Vector2 offset = screenPos - stickScreenCenter;
                        Vector2 clamped = Vector2.ClampMagnitude(offset, stickRadius);
                        currentKnobPos = new Vector2(stickCenter.x + clamped.x, stickCenter.y - clamped.y);
                        MoveX = clamped.x / stickRadius;
                        MoveY = clamped.y / stickRadius;
                    }

                    // Look / Aim zone (right or upper screen not on buttons)
                    if (id != stickFingerId && id != fireFingerId)
                    {
                        if (screenPos.x > Screen.width * 0.32f && screenPos.y > 100f && !fireRectScreen.Contains(screenPos) && !jumpRectScreen.Contains(screenPos) && !reloadRectScreen.Contains(screenPos) && !switchRectScreen.Contains(screenPos) && !slideRectScreen.Contains(screenPos))
                        {
                            if (lookFingerId != id)
                            {
                                lookFingerId = id;
                                lastLookPos = screenPos;
                            }
                            else
                            {
                                Vector2 delta = screenPos - lastLookPos;
                                float lookSens = GameSettings.FirearmSensitivity * 0.12f;
                                lookDeltaAccumulator.x += delta.x * lookSens;
                                lookDeltaAccumulator.y += (GameSettings.InvertY ? delta.y : -delta.y) * lookSens;
                                lastLookPos = screenPos;
                            }
                            sawLook = true;
                        }
                    }
                }
            }

            // Fallback for mouse input (allows testing on desktop without touch screen)
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                Vector2 mouseScreen = mouse.position.ReadValue();

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (jumpRectScreen.Contains(mouseScreen)) jumpQueued = true;
                    else if (reloadRectScreen.Contains(mouseScreen)) reloadQueued = true;
                    else if (switchRectScreen.Contains(mouseScreen)) switchQueued = true;
                    else if (slideRectScreen.Contains(mouseScreen)) slideQueued = true;
                }

                if (fireRectScreen.Contains(mouseScreen))
                {
                    sawFire = true;
                }
                else if (moveZoneScreen.Contains(mouseScreen) || isStickActive)
                {
                    isStickActive = true;
                    sawStick = true;
                    Vector2 stickScreenCenter = new Vector2(stickCenter.x, Screen.height - stickCenter.y);
                    Vector2 offset = mouseScreen - stickScreenCenter;
                    Vector2 clamped = Vector2.ClampMagnitude(offset, stickRadius);
                    currentKnobPos = new Vector2(stickCenter.x + clamped.x, stickCenter.y - clamped.y);
                    MoveX = clamped.x / stickRadius;
                    MoveY = clamped.y / stickRadius;
                }
                else if (mouseScreen.x > Screen.width * 0.32f && mouseScreen.y > 100f)
                {
                    if (!sawLook)
                    {
                        Vector2 delta = mouse.delta.ReadValue();
                        float lookSens = GameSettings.FirearmSensitivity * 0.12f;
                        lookDeltaAccumulator.x += delta.x * lookSens;
                        lookDeltaAccumulator.y += (GameSettings.InvertY ? -delta.y : delta.y) * lookSens;
                        sawLook = true;
                    }
                }
            }
            else
            {
                isStickActive = false;
            }

            if (!sawStick)
            {
                stickFingerId = -1;
                MoveX = 0f;
                MoveY = 0f;
                currentKnobPos = stickCenter;
            }

            if (!sawFire)
            {
                fireFingerId = -1;
                Fire = false;
            }
            else
            {
                Fire = true;
            }

            if (!sawLook)
            {
                lookFingerId = -1;
            }
        }

        public static void DrawGUI()
        {
            if (!GameSettings.TouchControlsEnabled) return;

            Vector2 stickCenter = new Vector2(BaseStickPos.x, Screen.height - 180f);
            float stickRadius = 60f;

            // 1. Movement Joystick Base (GUI coordinates)
            Rect baseRect = new Rect(stickCenter.x - stickRadius, stickCenter.y - stickRadius, stickRadius * 2f, stickRadius * 2f);
            DrawCircle(baseRect, new Color(0.06f, 0.10f, 0.16f, 0.70f), new Color(0.2f, 0.8f, 1.0f, 0.65f), 3);

            // D-Pad Directional guides
            GUI.Label(new Rect(stickCenter.x - 12f, stickCenter.y - stickRadius + 4f, 24f, 18f), "▲", CenterLabel(new Color(0.4f, 0.85f, 1f, 0.6f)));
            GUI.Label(new Rect(stickCenter.x - 12f, stickCenter.y + stickRadius - 22f, 24f, 18f), "▼", CenterLabel(new Color(0.4f, 0.85f, 1f, 0.6f)));
            GUI.Label(new Rect(stickCenter.x - stickRadius + 4f, stickCenter.y - 10f, 20f, 18f), "◀", CenterLabel(new Color(0.4f, 0.85f, 1f, 0.6f)));
            GUI.Label(new Rect(stickCenter.x + stickRadius - 24f, stickCenter.y - 10f, 20f, 18f), "▶", CenterLabel(new Color(0.4f, 0.85f, 1f, 0.6f)));

            // Joystick Thumb Knob
            Vector2 knob = (stickFingerId >= 0 || isStickActive) ? currentKnobPos : stickCenter;
            float knobRadius = 26f;
            Rect knobRect = new Rect(knob.x - knobRadius, knob.y - knobRadius, knobRadius * 2f, knobRadius * 2f);
            Color knobColor = (stickFingerId >= 0 || isStickActive) ? new Color(0.1f, 0.85f, 1f, 0.95f) : new Color(0.3f, 0.5f, 0.7f, 0.8f);
            DrawCircle(knobRect, knobColor, Color.white, 2);

            // 2. Large FIRE Button (Bottom Right)
            Rect fireRect = new Rect(Screen.width - 135f, Screen.height - 240f, 110f, 110f);
            Color fireBg = Fire ? new Color(1f, 0.2f, 0.1f, 0.95f) : new Color(0.85f, 0.15f, 0.1f, 0.75f);
            DrawPill(fireRect, fireBg, new Color(1f, 0.85f, 0.3f, 0.9f), 3);
            GUI.Label(new Rect(fireRect.x, fireRect.y + 36f, fireRect.width, 36f), "FIRE", LargeButtonLabel(Color.white));

            // 3. JUMP Button
            Rect jumpRect = new Rect(Screen.width - 135f, Screen.height - 325f, 110f, 65f);
            DrawPill(jumpRect, new Color(0.1f, 0.45f, 0.8f, 0.8f), new Color(0.4f, 0.85f, 1f, 0.9f), 2);
            GUI.Label(new Rect(jumpRect.x, jumpRect.y + 18f, jumpRect.width, 30f), "⇪ JUMP", MediumButtonLabel(Color.white));

            // 4. RELOAD Button
            Rect reloadRect = new Rect(Screen.width - 250f, Screen.height - 210f, 95f, 60f);
            DrawPill(reloadRect, new Color(0.6f, 0.45f, 0.1f, 0.8f), new Color(1f, 0.85f, 0.3f, 0.9f), 2);
            GUI.Label(new Rect(reloadRect.x, reloadRect.y + 16f, reloadRect.width, 28f), "⟳ RELOAD", SmallButtonLabel(Color.white));

            // 5. SWITCH WEAPON Button
            Rect switchRect = new Rect(Screen.width - 250f, Screen.height - 280f, 95f, 60f);
            DrawPill(switchRect, new Color(0.45f, 0.15f, 0.65f, 0.8f), new Color(0.85f, 0.4f, 1f, 0.9f), 2);
            GUI.Label(new Rect(switchRect.x, switchRect.y + 16f, switchRect.width, 28f), "⚡ GUN ⟳", SmallButtonLabel(Color.white));

            // 6. SLIDE Button
            Rect slideRect = new Rect(Screen.width - 250f, Screen.height - 350f, 95f, 60f);
            DrawPill(slideRect, new Color(0.12f, 0.50f, 0.42f, 0.8f), new Color(0.2f, 0.95f, 0.75f, 0.9f), 2);
            GUI.Label(new Rect(slideRect.x, slideRect.y + 16f, slideRect.width, 28f), "⚡ SLIDE", SmallButtonLabel(Color.white));

            // 7. Look / Aim drag indicator (Subtle hint at top-right)
            GUI.Label(new Rect(Screen.width - 240f, 55f, 220f, 24f), "[ Touch & Drag screen to Aim ]", SubtleHintLabel());
        }

        private static void DrawPill(Rect rect, Color bg, Color border, int thickness)
        {
            Color old = GUI.color;
            GUI.color = bg;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = border;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void DrawCircle(Rect rect, Color bg, Color border, int thickness)
        {
            DrawPill(rect, bg, border, thickness);
        }

        private static GUIStyle CenterLabel(Color color)
        {
            return new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12, normal = { textColor = color } };
        }

        private static GUIStyle LargeButtonLabel(Color color)
        {
            return new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 22, normal = { textColor = color } };
        }

        private static GUIStyle MediumButtonLabel(Color color)
        {
            return new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16, normal = { textColor = color } };
        }

        private static GUIStyle SmallButtonLabel(Color color)
        {
            return new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13, normal = { textColor = color } };
        }

        private static GUIStyle SubtleHintLabel()
        {
            return new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Italic, fontSize = 11, normal = { textColor = new Color(0.7f, 0.85f, 1f, 0.6f) } };
        }
    }
}
