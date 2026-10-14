using RushArena.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RushArena.Multiplayer
{
    // High-performance, cross-platform Sci-Fi Combat HUD for PC and Mobile.
    public sealed class CombatHud : MonoBehaviour
    {
        private SharedPlayerAvatar avatar;
        private float hitMarkerUntil;
        private float shotMarkerUntil;
        private bool showScoreboard;

        public void Configure(SharedPlayerAvatar value) { avatar = value; }

        public void PulseShot(bool hit)
        {
            shotMarkerUntil = Time.unscaledTime + 0.06f;
            if (hit) hitMarkerUntil = Time.unscaledTime + 0.16f;
        }

        private void Update()
        {
            // Toggle scoreboard on Tab
            if (Keyboard.current != null)
            {
                showScoreboard = Keyboard.current.tabKey.isPressed;
            }
        }

        private void OnGUI()
        {
            if (avatar == null) return;

            DrawCrosshair();
            DrawPlayerStatus();
            DrawWeaponStatus();
            DrawMatchHeader();
            DrawKillFeed();
            DrawPowerupIndicator();

            if (avatar.IsDead)
            {
                DrawEliminationScreen();
            }

            if (SharedMatchManager.Instance != null && SharedMatchManager.Instance.IsMatchOver)
            {
                DrawVictoryBanner();
            }

            if (showScoreboard)
            {
                DrawScoreboard();
            }

            if (Application.isMobilePlatform || (avatar != null && !UnityEngine.Cursor.visible))
            {
                DrawMobileTouchControls();
            }
        }

        private void DrawCrosshair()
        {
            var center = new Rect(Screen.width * 0.5f - 14f, Screen.height * 0.5f - 18f, 28f, 36f);
            bool isHit = Time.unscaledTime < hitMarkerUntil;
            bool isShot = Time.unscaledTime < shotMarkerUntil;

            Color crosshairColor = isHit ? new Color(1f, 0.2f, 0.2f) : (isShot ? new Color(1f, 0.85f, 0.2f) : Color.white);
            string symbol = isHit ? "×" : "+";

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = isHit ? 30 : 26,
                fontStyle = FontStyle.Bold,
                normal = { textColor = crosshairColor }
            };
            GUI.Label(center, symbol, style);
        }

        private void DrawPlayerStatus()
        {
            float w = 240f;
            float h = 64f;
            var rect = new Rect(20f, Screen.height - h - 20f, w, h);
            GUI.Box(rect, string.Empty);

            // Health bar
            float hpPct = Mathf.Clamp01(avatar.Health / 100f);
            GUI.Label(new Rect(30f, Screen.height - h - 14f, 70f, 20f), "HEALTH", BoldLabel(new Color(0.2f, 0.95f, 0.4f)));
            GUI.Label(new Rect(105f, Screen.height - h - 14f, 40f, 20f), $"{avatar.Health:000}", BoldLabel(Color.white));
            GUI.Box(new Rect(30f, Screen.height - h + 8f, 150f * hpPct, 8f), string.Empty);

            // Shield bar
            float spPct = Mathf.Clamp01(avatar.Shield / 50f);
            GUI.Label(new Rect(30f, Screen.height - h + 22f, 70f, 20f), "SHIELD", BoldLabel(new Color(0.2f, 0.8f, 1.0f)));
            GUI.Label(new Rect(105f, Screen.height - h + 22f, 40f, 20f), $"{avatar.Shield:000}", BoldLabel(Color.white));
            GUI.Box(new Rect(30f, Screen.height - h + 44f, 150f * spPct, 8f), string.Empty);
        }

        private void DrawWeaponStatus()
        {
            float w = 220f;
            float h = 64f;
            var rect = new Rect(Screen.width - w - 20f, Screen.height - h - 20f, w, h);
            GUI.Box(rect, string.Empty);

            string weaponName = avatar.CurrentWeapon == WeaponType.AssaultRifle ? "ASSAULT RIFLE" : "PLASMA CANNON";
            string ammoText = avatar.IsReloading ? "RELOADING..." : $"{avatar.AmmoInClip:00} / {avatar.MaxClip:00}";

            GUI.Label(new Rect(Screen.width - w - 10f, Screen.height - h - 14f, 200f, 22f), weaponName, BoldLabel(new Color(1f, 0.75f, 0.2f)));
            GUI.Label(new Rect(Screen.width - w - 10f, Screen.height - h + 10f, 200f, 26f), ammoText, LargeLabel(avatar.IsReloading ? Color.yellow : Color.white));
            GUI.Label(new Rect(Screen.width - w - 10f, Screen.height - h + 38f, 200f, 18f), "[Q] / TAP TO SWITCH", SmallLabel(new Color(0.7f, 0.7f, 0.7f)));
        }

        private void DrawMatchHeader()
        {
            float w = 280f;
            float h = 42f;
            var rect = new Rect(Screen.width * 0.5f - w * 0.5f, 15f, w, h);
            GUI.Box(rect, string.Empty);

            string timerText = "02:00";
            if (SharedMatchManager.Instance != null && SharedMatchManager.Instance.MatchTimer.IsRunning)
            {
                float left = Mathf.Max(0f, (float)SharedMatchManager.Instance.MatchTimer.RemainingTime(avatar.Runner));
                int mins = (int)(left / 60);
                int secs = (int)(left % 60);
                timerText = $"{mins:00}:{secs:00}";
            }

            GUI.Label(new Rect(Screen.width * 0.5f - 130f, 18f, 100f, 32f), $"TIME: {timerText}", LargeLabel(Color.white));
            GUI.Label(new Rect(Screen.width * 0.5f + 10f, 18f, 120f, 32f), $"SCORE: {avatar.Score}", LargeLabel(new Color(0.2f, 0.9f, 1f)));
        }

        private void DrawKillFeed()
        {
            if (SharedMatchManager.Instance == null) return;
            var feed = SharedMatchManager.Instance.KillFeed;
            float top = 65f;
            for (int i = feed.Count - 1; i >= 0; i--)
            {
                var entry = feed[i];
                if (Time.time - entry.Timestamp > 6f) continue;
                string text = $"{entry.Killer}  [ {entry.Weapon} ]  {entry.Victim}";
                GUI.Box(new Rect(Screen.width - 320f, top, 300f, 24f), string.Empty);
                GUI.Label(new Rect(Screen.width - 315f, top + 2f, 290f, 20f), text, SmallLabel(new Color(1f, 0.9f, 0.6f)));
                top += 28f;
            }
        }

        private void DrawPowerupIndicator()
        {
            string label = avatar.PowerupLabel;
            if (string.IsNullOrEmpty(label)) return;

            var rect = new Rect(Screen.width * 0.5f - 100f, Screen.height * 0.5f + 40f, 200f, 28f);
            GUI.Box(rect, string.Empty);
            GUI.Label(rect, $"★ {label} ★", BoldLabel(new Color(1f, 0.3f, 0.9f), TextAnchor.MiddleCenter));
        }

        private void DrawEliminationScreen()
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), string.Empty);
            var rect = new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - 60f, 360f, 120f);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x, rect.y + 15f, rect.width, 34f), "ELIMINATED", LargeLabel(new Color(1f, 0.25f, 0.2f), TextAnchor.MiddleCenter));
            GUI.Label(new Rect(rect.x, rect.y + 55f, rect.width, 28f), $"RESPAWNING IN {avatar.RespawnTimeRemaining:0.0}s...", BoldLabel(Color.white, TextAnchor.MiddleCenter));
        }

        private void DrawVictoryBanner()
        {
            var rect = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 90f, 440f, 180f);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x, rect.y + 18f, rect.width, 40f), "MATCH FINISHED!", LargeLabel(new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter));
            GUI.Label(new Rect(rect.x, rect.y + 65f, rect.width, 32f), $"WINNER: {SharedMatchManager.Instance.WinnerName}", BoldLabel(Color.cyan, TextAnchor.MiddleCenter));

            float restartIn = Mathf.Max(0f, (float)SharedMatchManager.Instance.RestartTimer.RemainingTime(avatar.Runner));
            GUI.Label(new Rect(rect.x, rect.y + 115f, rect.width, 28f), $"Next match starting in {restartIn:0}s...", SmallLabel(Color.white, TextAnchor.MiddleCenter));
        }

        private void DrawScoreboard()
        {
            float w = 520f;
            float h = 320f;
            var rect = new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.5f - h * 0.5f, w, h);
            GUI.Box(rect, "RUSH ARENA - SCOREBOARD");

            GUI.Label(new Rect(rect.x + 30f, rect.y + 35f, 180f, 25f), "PLAYER", BoldLabel(Color.cyan));
            GUI.Label(new Rect(rect.x + 230f, rect.y + 35f, 70f, 25f), "KILLS", BoldLabel(Color.cyan));
            GUI.Label(new Rect(rect.x + 320f, rect.y + 35f, 70f, 25f), "DEATHS", BoldLabel(Color.cyan));
            GUI.Label(new Rect(rect.x + 410f, rect.y + 35f, 80f, 25f), "SCORE", BoldLabel(Color.cyan));

            var avatars = FindObjectsByType<SharedPlayerAvatar>();
            float rowY = rect.y + 65f;
            for (int i = 0; i < avatars.Length && i < 8; i++)
            {
                var a = avatars[i];
                bool isMe = a == avatar;
                Color rowColor = isMe ? new Color(0.3f, 1f, 0.5f) : Color.white;
                GUI.Label(new Rect(rect.x + 30f, rowY, 180f, 22f), a.PlayerDisplayName + (isMe ? " (YOU)" : ""), BoldLabel(rowColor));
                GUI.Label(new Rect(rect.x + 230f, rowY, 70f, 22f), $"{a.Kills}", BoldLabel(rowColor));
                GUI.Label(new Rect(rect.x + 320f, rowY, 70f, 22f), $"{a.Deaths}", BoldLabel(rowColor));
                GUI.Label(new Rect(rect.x + 410f, rowY, 80f, 22f), $"{a.Score}", BoldLabel(rowColor));
                rowY += 26f;
            }
        }

        private void DrawMobileTouchControls()
        {
            // Dedicated button to toggle in-game scoreboard on touch devices
            if (GUI.Button(new Rect(Screen.width - 100f, 15f, 85f, 35f), "SCORES"))
            {
                showScoreboard = !showScoreboard;
            }
        }

        private GUIStyle BoldLabel(Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            return new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14, alignment = anchor, normal = { textColor = color } };
        }

        private GUIStyle LargeLabel(Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            return new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 20, alignment = anchor, normal = { textColor = color } };
        }

        private GUIStyle SmallLabel(Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            return new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Normal, fontSize = 12, alignment = anchor, normal = { textColor = color } };
        }
    }
}
