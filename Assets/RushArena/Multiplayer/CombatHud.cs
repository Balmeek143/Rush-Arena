using System;
using RushArena.Core;
using RushArena.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RushArena.Multiplayer
{
    // High-contrast, esports-style Sci-Fi Combat HUD for PC, WebGL, and Mobile.
    // Features glowing match header, modern kill cards, reload indicators, pickup alerts,
    // and an in-game Settings modal for mouse sensitivity, FOV, volume, bot count, and difficulty.
    public sealed class CombatHud : MonoBehaviour
    {
        private SharedPlayerAvatar avatar;
        private float hitMarkerUntil;
        private float shotMarkerUntil;
        private bool showScoreboard;
        private bool showSettings;

        // Pickup banner state
        private string pickupBannerText = string.Empty;
        private float pickupBannerUntil;

        // Cached GUI textures
        private Texture2D panelTex;
        private Texture2D borderTex;

        public void Configure(SharedPlayerAvatar value) { avatar = value; }

        public void PulseShot(bool hit)
        {
            shotMarkerUntil = Time.unscaledTime + 0.06f;
            if (hit) hitMarkerUntil = Time.unscaledTime + 0.16f;
        }

        public void ShowPickupNotification(string text)
        {
            pickupBannerText = text;
            pickupBannerUntil = Time.time + 2.4f;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                showScoreboard = keyboard.tabKey.isPressed;
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    showSettings = !showSettings;
                    if (showSettings)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (avatar == null) return;

            DrawCrosshair();
            DrawReloadIndicator();
            DrawPlayerStatus();
            DrawWeaponStatus();
            DrawMatchHeader();
            DrawKillFeed();
            DrawPowerupIndicator();
            DrawPickupBanner();

            // Settings button in top-left
            if (GUI.Button(new Rect(15f, 15f, 110f, 32f), showSettings ? "✕ CLOSE" : "⚙ SETTINGS"))
            {
                showSettings = !showSettings;
                if (showSettings)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

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

            if (showSettings)
            {
                DrawSettingsModal();
            }

            if (Application.isMobilePlatform)
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

        private void DrawReloadIndicator()
        {
            if (!avatar.IsReloading) return;

            float w = 180f;
            float h = 32f;
            var rect = new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.5f + 24f, w, h);

            // Dark background pill
            DrawFilledRect(rect, new Color(0.05f, 0.08f, 0.12f, 0.88f));
            DrawOutlineRect(rect, new Color(1f, 0.75f, 0.2f, 0.8f), 2);

            // Progress fill bar
            float p = avatar.ReloadProgress;
            DrawFilledRect(new Rect(rect.x + 4f, rect.y + 22f, (w - 8f) * p, 5f), new Color(1f, 0.8f, 0.2f));

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };
            GUI.Label(new Rect(rect.x, rect.y + 2f, w, 18f), $"RELOADING... {Mathf.RoundToInt(p * 100f)}%", labelStyle);
        }

        private void DrawPlayerStatus()
        {
            float w = 240f;
            float h = 68f;
            var rect = new Rect(20f, Screen.height - h - 20f, w, h);

            DrawFilledRect(rect, new Color(0.04f, 0.06f, 0.10f, 0.85f));
            DrawOutlineRect(rect, new Color(0.2f, 0.6f, 0.9f, 0.6f), 1);

            // Health bar
            float hpPct = Mathf.Clamp01(avatar.Health / 100f);
            GUI.Label(new Rect(30f, Screen.height - h - 14f, 70f, 20f), "HEALTH", BoldLabel(new Color(0.2f, 0.95f, 0.4f)));
            GUI.Label(new Rect(105f, Screen.height - h - 14f, 40f, 20f), $"{avatar.Health:000}", BoldLabel(Color.white));
            DrawFilledRect(new Rect(30f, Screen.height - h + 10f, 170f, 8f), new Color(0.15f, 0.2f, 0.2f, 0.8f));
            DrawFilledRect(new Rect(30f, Screen.height - h + 10f, 170f * hpPct, 8f), new Color(0.2f, 0.95f, 0.4f));

            // Shield bar
            float spPct = Mathf.Clamp01(avatar.Shield / 50f);
            GUI.Label(new Rect(30f, Screen.height - h + 24f, 70f, 20f), "SHIELD", BoldLabel(new Color(0.2f, 0.8f, 1.0f)));
            GUI.Label(new Rect(105f, Screen.height - h + 24f, 40f, 20f), $"{avatar.Shield:000}", BoldLabel(Color.white));
            DrawFilledRect(new Rect(30f, Screen.height - h + 48f, 170f, 8f), new Color(0.1f, 0.15f, 0.25f, 0.8f));
            DrawFilledRect(new Rect(30f, Screen.height - h + 48f, 170f * spPct, 8f), new Color(0.2f, 0.8f, 1.0f));
        }

        private void DrawWeaponStatus()
        {
            float w = 240f;
            float h = 68f;
            var rect = new Rect(Screen.width - w - 20f, Screen.height - h - 20f, w, h);

            DrawFilledRect(rect, new Color(0.04f, 0.06f, 0.10f, 0.85f));
            DrawOutlineRect(rect, new Color(1f, 0.7f, 0.2f, 0.6f), 1);

            string weaponName = avatar.CurrentWeapon switch
            {
                WeaponType.Pistol => "SIDEARM PISTOL",
                WeaponType.AssaultRifle => "ASSAULT RIFLE",
                WeaponType.PlasmaCannon => "PLASMA CANNON",
                WeaponType.Scattergun => "SCATTERGUN",
                _ => "WEAPON"
            };

            string ammoText = avatar.IsReloading ? "RELOADING..." : $"{avatar.AmmoInClip:00} / {avatar.MaxClip:00}";

            GUI.Label(new Rect(Screen.width - w - 10f, Screen.height - h - 14f, 220f, 22f), weaponName, BoldLabel(new Color(1f, 0.8f, 0.25f)));
            GUI.Label(new Rect(Screen.width - w - 10f, Screen.height - h + 10f, 220f, 28f), ammoText, LargeLabel(avatar.IsReloading ? Color.yellow : Color.white));
            GUI.Label(new Rect(Screen.width - w - 10f, Screen.height - h + 40f, 220f, 18f), "[ SCROLL / Q ] CYCLE WEAPON", SmallLabel(new Color(0.7f, 0.75f, 0.85f)));
        }

        // Prominent, high-contrast, glowing match header badge
        private void DrawMatchHeader()
        {
            float w = 360f;
            float h = 50f;
            var rect = new Rect(Screen.width * 0.5f - w * 0.5f, 12f, w, h);

            // High-contrast Obsidian backplate
            DrawFilledRect(rect, new Color(0.03f, 0.05f, 0.09f, 0.92f));
            DrawOutlineRect(rect, new Color(0.15f, 0.7f, 1.0f, 0.85f), 2);

            string timerText = "02:00";
            if (SharedMatchManager.Instance != null && SharedMatchManager.Instance.MatchTimer.IsRunning)
            {
                float left = Mathf.Max(0f, (float)SharedMatchManager.Instance.MatchTimer.RemainingTime(avatar.Runner));
                int mins = (int)(left / 60);
                int secs = (int)(left % 60);
                timerText = $"{mins:00}:{secs:00}";
            }

            var timerStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.92f, 0.35f) }
            };

            var scoreStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.2f, 0.88f, 1.0f) }
            };

            GUI.Label(new Rect(rect.x + 10f, rect.y + 4f, 160f, 40f), $"⏱ {timerText}", timerStyle);
            GUI.Label(new Rect(rect.x + 180f, rect.y + 4f, 170f, 40f), $"SCORE: {avatar.Score}", scoreStyle);
        }

        // Esports-style Kill Feed Cards
        private void DrawKillFeed()
        {
            if (SharedMatchManager.Instance == null) return;
            var feed = SharedMatchManager.Instance.KillFeed;
            float top = 65f;

            for (int i = feed.Count - 1; i >= 0 && top < 260f; i--)
            {
                var entry = feed[i];
                float age = Time.time - entry.Timestamp;
                if (age > 6f) continue;

                float alpha = Mathf.Clamp01((6f - age) / 0.8f);
                float w = 310f;
                float h = 28f;
                var rect = new Rect(Screen.width - w - 15f, top, w, h);

                // Card background
                DrawFilledRect(rect, new Color(0.04f, 0.06f, 0.10f, 0.85f * alpha));

                // Left color accent pill (Cyan for human player, Red for bot)
                bool killerIsBot = entry.Killer.StartsWith("[BOT]");
                DrawFilledRect(new Rect(rect.x, rect.y, 4f, h), killerIsBot ? new Color(1f, 0.3f, 0.3f, alpha) : new Color(0.2f, 0.85f, 1f, alpha));

                string weaponSymbol = entry.Weapon switch
                {
                    "AssaultRifle" => "🔫 RIFLE",
                    "PlasmaCannon" => "⚡ PLASMA",
                    "Scattergun" => "💥 SCATTER",
                    "Pistol" => "🎯 PISTOL",
                    _ => entry.Weapon.ToUpper()
                };

                var killerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = (killerIsBot ? new Color(1f, 0.5f, 0.5f, alpha) : new Color(0.3f, 0.95f, 1f, alpha)) }
                };

                var weaponStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.85f, 0.35f, alpha) }
                };

                var victimStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Normal,
                    fontSize = 12,
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f, alpha) }
                };

                GUI.Label(new Rect(rect.x + 10f, rect.y, 110f, h), entry.Killer, killerStyle);
                GUI.Label(new Rect(rect.x + 115f, rect.y, 80f, h), weaponSymbol, weaponStyle);
                GUI.Label(new Rect(rect.x + 195f, rect.y, 105f, h), entry.Victim, victimStyle);

                top += 32f;
            }
        }

        private void DrawPickupBanner()
        {
            if (Time.time >= pickupBannerUntil || string.IsNullOrEmpty(pickupBannerText)) return;

            float w = 340f;
            float h = 40f;
            var rect = new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.28f, w, h);

            DrawFilledRect(rect, new Color(0.04f, 0.08f, 0.14f, 0.92f));
            DrawOutlineRect(rect, new Color(1f, 0.85f, 0.25f, 0.9f), 2);

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                normal = { textColor = new Color(1f, 0.9f, 0.35f) }
            };
            GUI.Label(rect, $"★ {pickupBannerText} ★", style);
        }

        private void DrawPowerupIndicator()
        {
            string label = avatar.PowerupLabel;
            if (string.IsNullOrEmpty(label)) return;

            var rect = new Rect(Screen.width * 0.5f - 110f, Screen.height * 0.5f + 62f, 220f, 28f);
            DrawFilledRect(rect, new Color(0.08f, 0.04f, 0.12f, 0.88f));
            DrawOutlineRect(rect, new Color(1f, 0.3f, 0.9f, 0.85f), 1);

            GUI.Label(rect, $"★ {label} ★", BoldLabel(new Color(1f, 0.4f, 0.95f), TextAnchor.MiddleCenter));
        }

        private void DrawSettingsModal()
        {
            float w = 460f;
            float h = 420f;
            var rect = new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.5f - h * 0.5f, w, h);

            DrawFilledRect(rect, new Color(0.05f, 0.08f, 0.14f, 0.97f));
            DrawOutlineRect(rect, new Color(0.2f, 0.75f, 1f, 0.9f), 2);

            GUILayout.BeginArea(new Rect(rect.x + 20f, rect.y + 15f, w - 40f, h - 30f));

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.cyan }
            };
            GUILayout.Label("GAMEPLAY SETTINGS", titleStyle);
            GUILayout.Space(10);

            // Mouse Sensitivity
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Mouse Sensitivity: {GameSettings.MouseSensitivity:0.0}x", GUILayout.Width(180));
            float newSens = GUILayout.HorizontalSlider(GameSettings.MouseSensitivity, 0.2f, 3.5f);
            if (Mathf.Abs(newSens - GameSettings.MouseSensitivity) > 0.02f) GameSettings.MouseSensitivity = newSens;
            GUILayout.EndHorizontal();

            // Field of View
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Field of View: {GameSettings.FieldOfView:0}°", GUILayout.Width(180));
            float newFov = GUILayout.HorizontalSlider(GameSettings.FieldOfView, 70f, 110f);
            if (Mathf.Abs(newFov - GameSettings.FieldOfView) > 0.5f) GameSettings.FieldOfView = newFov;
            GUILayout.EndHorizontal();

            // Master Volume
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Master Volume: {Mathf.RoundToInt(GameSettings.MasterVolume * 100)}%", GUILayout.Width(180));
            float newVol = GUILayout.HorizontalSlider(GameSettings.MasterVolume, 0f, 1f);
            if (Mathf.Abs(newVol - GameSettings.MasterVolume) > 0.02f) GameSettings.MasterVolume = newVol;
            GUILayout.EndHorizontal();

            // Invert Y
            GUILayout.BeginHorizontal();
            GUILayout.Label("Invert Mouse Y:", GUILayout.Width(180));
            bool newInvert = GUILayout.Toggle(GameSettings.InvertY, GameSettings.InvertY ? " Enabled" : " Disabled");
            if (newInvert != GameSettings.InvertY) GameSettings.InvertY = newInvert;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("BOT CONFIGURATION", BoldLabel(new Color(1f, 0.8f, 0.3f)));

            // Bot Difficulty
            int curDiff = GameSettings.BotDifficultyLevel;
            string[] diffNames = new[] { "Easy", "Medium", "Hard" };
            GUILayout.BeginHorizontal();
            GUILayout.Label("Bot Difficulty:", GUILayout.Width(120));
            for (int i = 0; i < diffNames.Length; i++)
            {
                if (GUILayout.Toggle(curDiff == i, diffNames[i], "Button", GUILayout.Width(80)))
                {
                    if (curDiff != i) GameSettings.BotDifficultyLevel = i;
                }
            }
            GUILayout.EndHorizontal();

            // Desired Bot Count
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Active Bots: {ArenaBot.AllBots.Count} (Target: {GameSettings.DesiredBotCount})", GUILayout.Width(200));
            int newBotCount = (int)GUILayout.HorizontalSlider(GameSettings.DesiredBotCount, 0, 8);
            if (newBotCount != GameSettings.DesiredBotCount) GameSettings.DesiredBotCount = newBotCount;
            GUILayout.EndHorizontal();

            // Adjust live bots button
            if (GUILayout.Button("Update In-Game Bots Now"))
            {
                AdjustBotsToTarget(GameSettings.DesiredBotCount);
            }

            GUILayout.Space(14);
            if (GUILayout.Button("RESUME MATCH", GUILayout.Height(36)))
            {
                showSettings = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            GUILayout.EndArea();
        }

        private void AdjustBotsToTarget(int targetCount)
        {
            var spawner = FindAnyObjectByType<SharedPlayerSpawner>();
            int diff = targetCount - ArenaBot.AllBots.Count;
            if (diff > 0 && spawner != null)
            {
                spawner.SpawnPracticeBots(diff);
            }
            else if (diff < 0)
            {
                int toRemove = -diff;
                for (int i = 0; i < toRemove && ArenaBot.AllBots.Count > 0; i++)
                {
                    int last = ArenaBot.AllBots.Count - 1;
                    if (ArenaBot.AllBots[last] != null) Destroy(ArenaBot.AllBots[last].gameObject);
                    ArenaBot.AllBots.RemoveAt(last);
                }
            }
        }

        private void DrawEliminationScreen()
        {
            DrawFilledRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.2f, 0.02f, 0.02f, 0.65f));

            var rect = new Rect(Screen.width * 0.5f - 190f, Screen.height * 0.5f - 65f, 380f, 130f);
            DrawFilledRect(rect, new Color(0.05f, 0.07f, 0.12f, 0.95f));
            DrawOutlineRect(rect, new Color(1f, 0.25f, 0.2f, 0.9f), 2);

            GUI.Label(new Rect(rect.x, rect.y + 16f, rect.width, 36f), "ELIMINATED", LargeLabel(new Color(1f, 0.25f, 0.2f), TextAnchor.MiddleCenter));
            GUI.Label(new Rect(rect.x, rect.y + 60f, rect.width, 28f), $"RESPAWNING IN {avatar.RespawnTimeRemaining:0.0}s...", BoldLabel(Color.white, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(rect.x, rect.y + 90f, rect.width, 24f), "Starting Pistol will be equipped on respawn", SmallLabel(new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter));
        }

        private void DrawVictoryBanner()
        {
            var rect = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 90f, 440f, 180f);
            DrawFilledRect(rect, new Color(0.04f, 0.06f, 0.12f, 0.95f));
            DrawOutlineRect(rect, new Color(1f, 0.85f, 0.2f, 0.9f), 2);

            GUI.Label(new Rect(rect.x, rect.y + 18f, rect.width, 40f), "MATCH FINISHED!", LargeLabel(new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter));
            GUI.Label(new Rect(rect.x, rect.y + 65f, rect.width, 32f), $"WINNER: {SharedMatchManager.Instance.WinnerName}", BoldLabel(Color.cyan, TextAnchor.MiddleCenter));

            float restartIn = Mathf.Max(0f, (float)SharedMatchManager.Instance.RestartTimer.RemainingTime(avatar.Runner));
            GUI.Label(new Rect(rect.x, rect.y + 115f, rect.width, 28f), $"Next match starting in {restartIn:0}s...", SmallLabel(Color.white, TextAnchor.MiddleCenter));
        }

        private void DrawScoreboard()
        {
            float w = 540f;
            float h = 330f;
            var rect = new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.5f - h * 0.5f, w, h);

            DrawFilledRect(rect, new Color(0.04f, 0.06f, 0.11f, 0.96f));
            DrawOutlineRect(rect, new Color(0.2f, 0.8f, 1.0f, 0.9f), 2);

            GUI.Label(new Rect(rect.x + 20f, rect.y + 12f, w - 40f, 26f), "RUSH ARENA - SCOREBOARD", BoldLabel(new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter));

            GUI.Label(new Rect(rect.x + 30f, rect.y + 44f, 180f, 25f), "COMBATANT", BoldLabel(Color.cyan));
            GUI.Label(new Rect(rect.x + 230f, rect.y + 44f, 70f, 25f), "KILLS", BoldLabel(Color.cyan));
            GUI.Label(new Rect(rect.x + 320f, rect.y + 44f, 70f, 25f), "DEATHS", BoldLabel(Color.cyan));
            GUI.Label(new Rect(rect.x + 410f, rect.y + 44f, 80f, 25f), "SCORE", BoldLabel(Color.cyan));

            var avatars = FindObjectsByType<SharedPlayerAvatar>();
            float rowY = rect.y + 74f;
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
            if (GUI.Button(new Rect(Screen.width - 100f, 15f, 85f, 35f), "SCORES"))
            {
                showScoreboard = !showScoreboard;
            }
        }

        private void DrawFilledRect(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private void DrawOutlineRect(Rect rect, Color color, int thickness)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = old;
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
