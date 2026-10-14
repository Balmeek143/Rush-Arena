using System;
using System.IO;
using Fusion;
using Fusion.Addons.SimpleKCC;
using RushArena.Arena;
using RushArena.Multiplayer;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace RushArena.Editor
{
    public static class MultiplayerSetup
    {
        public const string ScenePath = "Assets/RushArena/Scenes/ArenaShared.unity";
        private const string PrefabPath = "Assets/RushArena/Prefabs/SharedPlayer.prefab";
        private const string MatchManagerPrefabPath = "Assets/RushArena/Prefabs/SharedMatchManager.prefab";
        private const string MaterialFolder = "Assets/RushArena/Materials/";

        [MenuItem("RUSH Arena/Configure Shared Mode Development")]
        public static void Configure()
        {
            if (!Application.isBatchMode && SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene first.");

            Directory.CreateDirectory("Assets/RushArena/Prefabs");
            Directory.CreateDirectory("Assets/RushArena/Materials");
            AssetDatabase.Refresh();

            var matArmor = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "AvatarArmor.mat");
            var matVisor = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "AvatarVisor.mat");
            var matRifle = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "WeaponRifle.mat");
            var matPlasma = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "WeaponPlasma.mat");
            var matLaserTracer = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "LaserTracer.mat");
            var matPlasmaTracer = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "PlasmaTracer.mat");
            var matJumpPad = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "JumpPad.mat");
            var matPowerupRapid = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "PowerupRapidFire.mat");
            var matPowerupShield = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "PowerupShield.mat");
            var matPowerupSpeed = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "PowerupSpeed.mat");
            var matFloor = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "ArenaFloor.mat");
            var matWall = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "ArenaWall.mat");
            var matAccent = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "ArenaAccent.mat");

            // Match Manager Prefab
            var matchManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchManagerPrefabPath);
            if (matchManagerPrefab == null)
            {
                var mgrRoot = new GameObject("SharedMatchManager");
                mgrRoot.AddComponent<NetworkObject>();
                mgrRoot.AddComponent<SharedMatchManager>();
                PrefabUtility.SaveAsPrefabAsset(mgrRoot, MatchManagerPrefabPath);
                UnityEngine.Object.DestroyImmediate(mgrRoot);
                matchManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchManagerPrefabPath);
            }

            // Player Prefab
            var root = new GameObject("Shared player");
            root.AddComponent<NetworkObject>();
            var kcc = root.AddComponent<SimpleKCC>();
            var kccSettings = new SerializedObject(kcc);
            var settings = kccSettings.FindProperty("_settings");
            if (settings == null) throw new InvalidOperationException("Simple KCC settings API changed.");
            settings.FindPropertyRelative("Radius").floatValue = 0.35f;
            settings.FindPropertyRelative("Height").floatValue = 1.8f;
            settings.FindPropertyRelative("StepHeight").floatValue = 0.3f;
            var colMaskProp = settings.FindPropertyRelative("CollisionLayerMask");
            if (colMaskProp != null)
            {
                var bits = colMaskProp.FindPropertyRelative("m_Bits");
                if (bits != null) bits.intValue = -1;
                else colMaskProp.intValue = -1;
            }
            kccSettings.ApplyModifiedPropertiesWithoutUndo();

            var avatar = root.AddComponent<SharedPlayerAvatar>();
            var avatarSettings = new SerializedObject(avatar);
            if (matArmor != null) avatarSettings.FindProperty("armorMaterial").objectReferenceValue = matArmor;
            if (matVisor != null) avatarSettings.FindProperty("visorMaterial").objectReferenceValue = matVisor;
            if (matRifle != null) avatarSettings.FindProperty("rifleMaterial").objectReferenceValue = matRifle;
            if (matPlasma != null) avatarSettings.FindProperty("plasmaMaterial").objectReferenceValue = matPlasma;
            if (matLaserTracer != null) avatarSettings.FindProperty("laserTracerMaterial").objectReferenceValue = matLaserTracer;
            if (matPlasmaTracer != null) avatarSettings.FindProperty("plasmaTracerMaterial").objectReferenceValue = matPlasmaTracer;
            avatarSettings.ApplyModifiedPropertiesWithoutUndo();

            var hitbox = new GameObject("Hitbox");
            hitbox.transform.SetParent(root.transform, false);
            var hitCollider = hitbox.AddComponent<CapsuleCollider>();
            hitCollider.center = new Vector3(0f, 0.95f, 0f);
            hitCollider.height = 1.9f;
            hitCollider.radius = 0.42f;
            hitCollider.isTrigger = true;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            // Create ArenaShared scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.6f, 0.65f, 0.75f);

            // Sci-Fi Arena Construction
            var arenaRoot = new GameObject("SciFi_Arena");

            // Main Floor: 54m x 54m (top surface at y = 0)
            BuildBlock(arenaRoot.transform, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(54f, 1f, 54f), matFloor);
            // Thick safety subfloor (prevents any possible physics tunneling)
            BuildBlock(arenaRoot.transform, "Subfloor_Safety", new Vector3(0f, -2.5f, 0f), new Vector3(100f, 3f, 100f), matFloor);

            // Perimeter Walls: 4.5m high
            BuildBlock(arenaRoot.transform, "Wall_North", new Vector3(0f, 2.25f, 27f), new Vector3(55f, 4.5f, 1f), matWall);
            BuildBlock(arenaRoot.transform, "Wall_South", new Vector3(0f, 2.25f, -27f), new Vector3(55f, 4.5f, 1f), matWall);
            BuildBlock(arenaRoot.transform, "Wall_East", new Vector3(27f, 2.25f, 0f), new Vector3(1f, 4.5f, 54f), matWall);
            BuildBlock(arenaRoot.transform, "Wall_West", new Vector3(-27f, 2.25f, 0f), new Vector3(1f, 4.5f, 54f), matWall);

            // Glowing border trim
            BuildBlock(arenaRoot.transform, "Trim_North", new Vector3(0f, 4.55f, 27f), new Vector3(55f, 0.2f, 1.2f), matAccent);
            BuildBlock(arenaRoot.transform, "Trim_South", new Vector3(0f, 4.55f, -27f), new Vector3(55f, 0.2f, 1.2f), matAccent);
            BuildBlock(arenaRoot.transform, "Trim_East", new Vector3(27f, 4.55f, 0f), new Vector3(1.2f, 0.2f, 54f), matAccent);
            BuildBlock(arenaRoot.transform, "Trim_West", new Vector3(-27f, 4.55f, 0f), new Vector3(1.2f, 0.2f, 54f), matAccent);

            // Central Elevated Combat Platform (14m x 14m at 3.5m height)
            BuildBlock(arenaRoot.transform, "Central_Platform", new Vector3(0f, 1.75f, 0f), new Vector3(14f, 3.5f, 14f), matFloor);
            BuildBlock(arenaRoot.transform, "Central_Trim", new Vector3(0f, 3.55f, 0f), new Vector3(14.4f, 0.1f, 14.4f), matAccent);
            BuildBlock(arenaRoot.transform, "Central_Cover_1", new Vector3(-2.5f, 4.2f, 0f), new Vector3(0.6f, 1.4f, 4.5f), matWall);
            BuildBlock(arenaRoot.transform, "Central_Cover_2", new Vector3(2.5f, 4.2f, 0f), new Vector3(0.6f, 1.4f, 4.5f), matWall);

            // Catwalks & Access Ramps
            BuildBlock(arenaRoot.transform, "Ramp_North", new Vector3(0f, 1.75f, 11f), new Vector3(4f, 0.4f, 8.5f), matFloor, -24f);
            BuildBlock(arenaRoot.transform, "Ramp_South", new Vector3(0f, 1.75f, -11f), new Vector3(4f, 0.4f, 8.5f), matFloor, 24f);

            // Tactical Barriers across the ground floor
            for (int i = -1; i <= 1; i += 2)
            {
                BuildBlock(arenaRoot.transform, $"Cover_East_{i}", new Vector3(i * 14f, 0.9f, 8f), new Vector3(3f, 1.8f, 0.8f), matWall);
                BuildBlock(arenaRoot.transform, $"Cover_West_{i}", new Vector3(i * 14f, 0.9f, -8f), new Vector3(3f, 1.8f, 0.8f), matWall);
            }

            // Interactive Jump Pads (Launch players to high ground!)
            JumpPad.Create(new Vector3(-16f, 0.1f, 16f), Quaternion.Euler(0f, 135f, 0f), matJumpPad);
            JumpPad.Create(new Vector3(16f, 0.1f, 16f), Quaternion.Euler(0f, -135f, 0f), matJumpPad);
            JumpPad.Create(new Vector3(-16f, 0.1f, -16f), Quaternion.Euler(0f, 45f, 0f), matJumpPad);
            JumpPad.Create(new Vector3(16f, 0.1f, -16f), Quaternion.Euler(0f, -45f, 0f), matJumpPad);

            // Tactical Weapon Drops and Powerup Spawners across the Arena
            ArenaPowerup.Create(new Vector3(0f, 4.2f, 0f), PowerupType.WeaponScattergun, matRifle, matAccent);
            ArenaPowerup.Create(new Vector3(0f, 0.8f, 18f), PowerupType.WeaponAssaultRifle, matRifle, matAccent);
            ArenaPowerup.Create(new Vector3(0f, 0.8f, -18f), PowerupType.WeaponPlasmaCannon, matPlasma, matVisor);
            ArenaPowerup.Create(new Vector3(18f, 0.8f, 0f), PowerupType.ShieldBoost, matPowerupShield, matVisor);
            ArenaPowerup.Create(new Vector3(-18f, 0.8f, 0f), PowerupType.SpeedBoost, matPowerupSpeed, matAccent);
            ArenaPowerup.Create(new Vector3(0f, 1.8f, 0f), PowerupType.RapidFire, matPowerupRapid, matAccent);

            // Shared room menu setup
            var menuObj = new GameObject("Shared room menu");
            var menu = menuObj.AddComponent<SharedRoomMenu>();
            var menuSettings = new SerializedObject(menu);
            menuSettings.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            menuSettings.FindProperty("matchManagerPrefab").objectReferenceValue = matchManagerPrefab;
            menuSettings.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(MilestoneSetup.ScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            Debug.Log("RUSH Arena: Full Sci-Fi Arena, Jump Pads, Powerups, and Match Systems configured!");
        }

        private static void BuildBlock(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, float tiltX = 0f)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.SetPositionAndRotation(pos, Quaternion.Euler(tiltX, 0f, 0f));
            cube.transform.localScale = scale;
            if (mat != null) cube.GetComponent<Renderer>().sharedMaterial = mat;
        }

        [MenuItem("RUSH Arena/Build Shared Mode Windows Development")]
        public static void BuildWindows()
        {
            const string output = "Builds/WindowsShared/RushArenaShared.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Shared Mode Windows build failed: " + report.summary.result);
            Debug.Log("RUSH shared build succeeded: " + output);
        }

        [MenuItem("RUSH Arena/Build Shared Mode WebGL Development")]
        public static void BuildWebGL()
        {
            const string output = "Builds/WebGLShared";
            Directory.CreateDirectory(output);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = output,
                target = BuildTarget.WebGL, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Shared Mode WebGL build failed: " + report.summary.result);
            Debug.Log("RUSH WebGL shared build succeeded: " + output);
        }
    }
}
