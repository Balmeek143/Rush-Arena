using System;
using System.IO;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RushArena.Multiplayer
{
    // Small developer room UI. Public/private matchmaking belongs to a later slice.
    public sealed class SharedRoomMenu : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject matchManagerPrefab;
        [SerializeField] private string roomName = "rush-dev-room";
        private NetworkRunner runner;
        private string status = "Enter a development room name.";
        private bool busy;
        private bool smokeMode;
        private string smokeResultPath;
        private float smokeDeadline;
        private float nextSmokeLog;
        private bool joinOnly;

        private void Start()
        {
            RapidFirePowerup.Create(new Vector3(-8f, 1.2f, 2f));
            RapidFirePowerup.Create(new Vector3(8f, 1.2f, 2f));
            RapidFirePowerup.Create(new Vector3(0f, 1.2f, 14f));
            string[] args = Environment.GetCommandLineArgs();
            smokeMode = Array.IndexOf(args, "-rush-shared-smoke") >= 0;
            if (!smokeMode) return;

            roomName = ReadArgument(args, "-rush-room", roomName);
            smokeResultPath = ReadArgument(args, "-rush-result", string.Empty);
            joinOnly = Array.IndexOf(args, "-rush-join-only") >= 0;
            smokeDeadline = Time.realtimeSinceStartup + 45f;
            Debug.Log($"RUSH_SHARED_SMOKE starting room={roomName}");
            Join();
        }

        private void Update()
        {
            if (!smokeMode) return;
            if (Time.realtimeSinceStartup >= nextSmokeLog)
            {
                nextSmokeLog = Time.realtimeSinceStartup + 5f;
                int playerCount = runner != null && runner.IsRunning ? runner.SessionInfo.PlayerCount : 0;
                Debug.Log($"RUSH_SHARED_SMOKE status={status};running={runner != null && runner.IsRunning};players={playerCount}");
            }
            if (runner != null && runner.IsRunning && runner.SessionInfo.PlayerCount >= 2)
            {
                int avatars = FindObjectsByType<SharedPlayerAvatar>().Length;
                if (avatars >= 2)
                {
                    FinishSmoke(true, $"players={runner.SessionInfo.PlayerCount};avatars={avatars}");
                    return;
                }
            }

            if (Time.realtimeSinceStartup >= smokeDeadline)
                FinishSmoke(false, "Timed out. " + status);
        }

        private static string ReadArgument(string[] args, string key, string fallback)
        {
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }

        private void FinishSmoke(bool passed, string detail)
        {
            smokeMode = false;
            string result = (passed ? "PASS " : "FAIL ") + detail;
            Debug.Log("RUSH_SHARED_SMOKE " + result);
            if (!string.IsNullOrWhiteSpace(smokeResultPath))
            {
                string directory = Path.GetDirectoryName(smokeResultPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(smokeResultPath, result + Environment.NewLine);
            }
            Application.Quit(passed ? 0 : 1);
        }

        private async void Join()
        {
            if (busy || runner != null) return;
            roomName = roomName.Trim();
            if (roomName.Length < 3 || roomName.Length > 32)
            {
                status = "Room name must be 3–32 characters."; return;
            }
            if (playerPrefab == null) { status = "Player prefab missing."; return; }
            var playerNetworkObject = playerPrefab.GetComponent<NetworkObject>();
            if (playerNetworkObject == null) { status = "Player NetworkObject missing."; return; }
            int sceneIndex = SceneManager.GetActiveScene().buildIndex;
            if (sceneIndex < 0) { status = "Add the M2 scene to Build Settings."; return; }
            busy = true; status = "Connecting to Shared Mode...";
            Debug.Log($"RUSH Shared Mode connecting to room {roomName}.");
            var obj = new GameObject("RUSH Shared Mode runner");
            runner = obj.AddComponent<NetworkRunner>();
            var sceneManager = obj.AddComponent<NetworkSceneManagerDefault>();
            var matchNetworkObject = matchManagerPrefab != null ? matchManagerPrefab.GetComponent<NetworkObject>() : null;
            obj.AddComponent<SharedPlayerSpawner>().Configure(playerNetworkObject, matchNetworkObject);
            var scene = new NetworkSceneInfo();
            scene.AddSceneRef(SceneRef.FromIndex(sceneIndex), LoadSceneMode.Single);
            try
            {
                var result = await runner.StartGame(new StartGameArgs {
                    GameMode = GameMode.Shared, SessionName = roomName, PlayerCount = 10,
                    Scene = scene, SceneManager = sceneManager, IsVisible = false, IsOpen = true,
                    EnableClientSessionCreation = !joinOnly
                });
                status = result.Ok ? "Connected to " + roomName : "Connection failed: " + result.ShutdownReason;
                if (!result.Ok)
                {
                    Destroy(obj); runner = null;
                    if (smokeMode) FinishSmoke(false, status);
                }
            }
            catch (Exception error)
            {
                status = "Connection failed: " + error.GetType().Name;
                Debug.LogException(error);
                if (obj != null) Destroy(obj);
                runner = null;
                if (smokeMode) FinishSmoke(false, status);
            }
            finally { busy = false; }
        }

        private async void Leave()
        {
            if (busy || runner == null) return;
            busy = true; status = "Leaving room...";
            var old = runner; runner = null;
            try { await old.Shutdown(); status = "Left room."; }
            catch (Exception error) { status = "Leave failed: " + error.GetType().Name; Debug.LogException(error); }
            finally { if (old != null) Destroy(old.gameObject); busy = false; }
        }

        private void OnGUI()
        {
            if (runner != null && runner.IsRunning)
            {
                // In-match: small minimal room indicator at top-right
                return;
            }

            GUILayout.BeginArea(new Rect(15, 15, 390, 290), GUI.skin.box);
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 15,
                normal = { textColor = Color.cyan }
            };
            GUILayout.Label("RUSH ARENA | MULTIPLAYER LOBBY", titleStyle);
            GUILayout.Space(6);

            GUILayout.Label("Room Name:");
            roomName = GUILayout.TextField(roomName, 32);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Desired AI Bots: {RushArena.Runtime.GameSettings.DesiredBotCount}", GUILayout.Width(180));
            int newBotCount = (int)GUILayout.HorizontalSlider(RushArena.Runtime.GameSettings.DesiredBotCount, 0, 8);
            if (newBotCount != RushArena.Runtime.GameSettings.DesiredBotCount) RushArena.Runtime.GameSettings.DesiredBotCount = newBotCount;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Bot Difficulty:", GUILayout.Width(110));
            string[] diffs = new[] { "Easy", "Medium", "Hard" };
            int curDiff = RushArena.Runtime.GameSettings.BotDifficultyLevel;
            for (int i = 0; i < diffs.Length; i++)
            {
                if (GUILayout.Toggle(curDiff == i, diffs[i], "Button", GUILayout.Width(75)))
                {
                    if (curDiff != i) RushArena.Runtime.GameSettings.BotDifficultyLevel = i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Mouse Sensitivity: {RushArena.Runtime.GameSettings.MouseSensitivity:0.0}x", GUILayout.Width(180));
            float newSens = GUILayout.HorizontalSlider(RushArena.Runtime.GameSettings.MouseSensitivity, 0.2f, 3.5f);
            if (Mathf.Abs(newSens - RushArena.Runtime.GameSettings.MouseSensitivity) > 0.02f) RushArena.Runtime.GameSettings.MouseSensitivity = newSens;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            if (GUILayout.Button("ENTER SCI-FI ARENA", GUILayout.Height(38))) Join();

            GUILayout.Space(4);
            GUILayout.Label(status);
            GUILayout.EndArea();
        }
    }
}
