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

            // Overtide-style Sunlit Modular Town Materials
            var matPavement = GetOrCreateMaterial("TownPavement", new Color(0.74f, 0.72f, 0.68f), 0.15f);
            var matStucco = GetOrCreateMaterial("TownStucco", new Color(0.88f, 0.86f, 0.80f), 0.10f);
            var matTerracotta = GetOrCreateMaterial("TownTerracotta", new Color(0.80f, 0.38f, 0.22f), 0.25f);
            var matContainerRed = GetOrCreateMaterial("ContainerRed", new Color(0.82f, 0.20f, 0.16f), 0.35f);
            var matContainerBlue = GetOrCreateMaterial("ContainerBlue", new Color(0.18f, 0.44f, 0.78f), 0.35f);
            var matContainerYellow = GetOrCreateMaterial("ContainerYellow", new Color(0.92f, 0.74f, 0.18f), 0.35f);
            var matWood = GetOrCreateMaterial("TownWood", new Color(0.55f, 0.40f, 0.25f), 0.10f);
            var matWater = GetOrCreateMaterial("TownWater", new Color(0.20f, 0.65f, 0.85f), 0.90f);

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

            // Vibrant Daylight Sun Lighting
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.35f;
            sun.color = new Color(1.0f, 0.96f, 0.88f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(52f, -38f, 0f);
            RenderSettings.ambientLight = new Color(0.72f, 0.78f, 0.88f);

            // Overtown Modular Arena Construction (62m x 62m)
            var arenaRoot = new GameObject("Overtown_Arena");

            // Main Cobblestone Floor (top surface at y = 0)
            BuildBlock(arenaRoot.transform, "Town_Cobblestone_Floor", new Vector3(0f, -0.5f, 0f), new Vector3(64f, 1f, 64f), matPavement);
            // Thick safety subfloor (prevents physics tunneling)
            BuildBlock(arenaRoot.transform, "Subfloor_Safety", new Vector3(0f, -2.5f, 0f), new Vector3(110f, 3f, 110f), matPavement);

            // Perimeter Stucco Walls: 5m high
            BuildBlock(arenaRoot.transform, "Wall_North", new Vector3(0f, 2.5f, 32f), new Vector3(65f, 5.0f, 1.2f), matStucco);
            BuildBlock(arenaRoot.transform, "Wall_South", new Vector3(0f, 2.5f, -32f), new Vector3(65f, 5.0f, 1.2f), matStucco);
            BuildBlock(arenaRoot.transform, "Wall_East", new Vector3(32f, 2.5f, 0f), new Vector3(1.2f, 5.0f, 65f), matStucco);
            BuildBlock(arenaRoot.transform, "Wall_West", new Vector3(-32f, 2.5f, 0f), new Vector3(1.2f, 5.0f, 65f), matStucco);

            // Terracotta coping border trim
            BuildBlock(arenaRoot.transform, "Trim_North", new Vector3(0f, 5.1f, 32f), new Vector3(65f, 0.25f, 1.5f), matTerracotta);
            BuildBlock(arenaRoot.transform, "Trim_South", new Vector3(0f, 5.1f, -32f), new Vector3(65f, 0.25f, 1.5f), matTerracotta);
            BuildBlock(arenaRoot.transform, "Trim_East", new Vector3(32f, 5.1f, 0f), new Vector3(1.5f, 0.25f, 65f), matTerracotta);
            BuildBlock(arenaRoot.transform, "Trim_West", new Vector3(-32f, 5.1f, 0f), new Vector3(1.5f, 0.25f, 65f), matTerracotta);

            // --- NORTH: 2-Story Town Hall with Walkthrough Arch & Sniper Balcony ---
            // Ground floor building wings
            BuildBlock(arenaRoot.transform, "TownHall_WestWing", new Vector3(-6f, 2.0f, 21f), new Vector3(8f, 4.0f, 8f), matStucco);
            BuildBlock(arenaRoot.transform, "TownHall_EastWing", new Vector3(6f, 2.0f, 21f), new Vector3(8f, 4.0f, 8f), matStucco);
            BuildBlock(arenaRoot.transform, "TownHall_ArchLintel", new Vector3(0f, 3.6f, 21f), new Vector3(4.5f, 0.8f, 8f), matStucco);
            // 2nd Floor Sniper Balcony / Terrace at y = 4.1m
            BuildBlock(arenaRoot.transform, "TownHall_BalconyDeck", new Vector3(0f, 4.1f, 18.5f), new Vector3(16f, 0.25f, 5.0f), matPavement);
            BuildBlock(arenaRoot.transform, "TownHall_BalconyParapet", new Vector3(0f, 4.7f, 16.1f), new Vector3(16f, 0.95f, 0.4f), matTerracotta);
            BuildBlock(arenaRoot.transform, "TownHall_BackWall", new Vector3(0f, 5.5f, 25f), new Vector3(16f, 3.0f, 0.8f), matStucco);
            // Balcony access ramps
            BuildBlock(arenaRoot.transform, "TownHall_WestRamp", new Vector3(-11.5f, 2.05f, 19f), new Vector3(2.8f, 0.35f, 10f), matPavement, 24f);
            BuildBlock(arenaRoot.transform, "TownHall_EastRamp", new Vector3(11.5f, 2.05f, 19f), new Vector3(2.8f, 0.35f, 10f), matPavement, 24f);

            // --- SOUTH: Warehouse / Cargo Depot with Roof Vantage Deck ---
            BuildBlock(arenaRoot.transform, "Warehouse_WestBay", new Vector3(-5.5f, 1.8f, -21f), new Vector3(7f, 3.6f, 8f), matStucco);
            BuildBlock(arenaRoot.transform, "Warehouse_EastBay", new Vector3(5.5f, 1.8f, -21f), new Vector3(7f, 3.6f, 8f), matStucco);
            BuildBlock(arenaRoot.transform, "Warehouse_RoofDeck", new Vector3(0f, 3.7f, -21f), new Vector3(16f, 0.25f, 8.5f), matPavement);
            BuildBlock(arenaRoot.transform, "Warehouse_RoofParapet", new Vector3(0f, 4.25f, -16.8f), new Vector3(16f, 0.9f, 0.4f), matTerracotta);
            BuildBlock(arenaRoot.transform, "Warehouse_Ramp", new Vector3(9.5f, 1.85f, -21f), new Vector3(2.8f, 0.35f, 9f), matPavement, -24f);

            // --- EAST: Stacked Shipping Container Parkour Yard ---
            BuildBlock(arenaRoot.transform, "Container_East_Red", new Vector3(18f, 1.3f, 6f), new Vector3(6.5f, 2.6f, 2.6f), matContainerRed);
            BuildBlock(arenaRoot.transform, "Container_East_Blue", new Vector3(18f, 1.3f, -6f), new Vector3(6.5f, 2.6f, 2.6f), matContainerBlue);
            BuildBlock(arenaRoot.transform, "Container_East_TopYellow", new Vector3(18f, 3.9f, 0f), new Vector3(6.5f, 2.6f, 2.6f), matContainerYellow);
            BuildBlock(arenaRoot.transform, "Container_East_Catwalk", new Vector3(18f, 2.65f, 0f), new Vector3(2.8f, 0.2f, 6.0f), matTerracotta);

            // --- WEST: Storage Yard & Alleys ---
            BuildBlock(arenaRoot.transform, "Container_West_Blue", new Vector3(-18f, 1.3f, 6f), new Vector3(6.5f, 2.6f, 2.6f), matContainerBlue);
            BuildBlock(arenaRoot.transform, "Container_West_Red", new Vector3(-18f, 1.3f, -6f), new Vector3(6.5f, 2.6f, 2.6f), matContainerRed);
            BuildBlock(arenaRoot.transform, "Container_West_TopYellow", new Vector3(-18f, 3.9f, 0f), new Vector3(6.5f, 2.6f, 2.6f), matContainerYellow);
            BuildBlock(arenaRoot.transform, "Crates_West_Yard", new Vector3(-13f, 0.9f, 0f), new Vector3(2.4f, 1.8f, 3.8f), matWood);

            // --- CENTRAL TOWN PLAZA ---
            BuildBlock(arenaRoot.transform, "Plaza_Platform", new Vector3(0f, 0.45f, 0f), new Vector3(12f, 0.9f, 12f), matPavement);
            BuildBlock(arenaRoot.transform, "Plaza_FountainRim", new Vector3(0f, 1.15f, 0f), new Vector3(4.2f, 0.5f, 4.2f), matStucco);
            BuildBlock(arenaRoot.transform, "Plaza_FountainWater", new Vector3(0f, 1.36f, 0f), new Vector3(3.8f, 0.1f, 3.8f), matWater);
            BuildBlock(arenaRoot.transform, "Plaza_MonumentObelisk", new Vector3(0f, 2.4f, 0f), new Vector3(1.2f, 2.2f, 1.2f), matTerracotta);
            // Low stone cover benches in plaza
            BuildBlock(arenaRoot.transform, "Plaza_Bench_North", new Vector3(0f, 1.25f, 4.5f), new Vector3(4.5f, 0.75f, 0.6f), matStucco);
            BuildBlock(arenaRoot.transform, "Plaza_Bench_South", new Vector3(0f, 1.25f, -4.5f), new Vector3(4.5f, 0.75f, 0.6f), matStucco);

            // 4 Town Jump Pads (Launch players to high balconies and container roofs)
            JumpPad.Create(new Vector3(-13f, 0.1f, 13f), Quaternion.Euler(0f, 135f, 0f), matJumpPad);
            JumpPad.Create(new Vector3(13f, 0.1f, 13f), Quaternion.Euler(0f, -135f, 0f), matJumpPad);
            JumpPad.Create(new Vector3(-13f, 0.1f, -13f), Quaternion.Euler(0f, 45f, 0f), matJumpPad);
            JumpPad.Create(new Vector3(13f, 0.1f, -13f), Quaternion.Euler(0f, -45f, 0f), matJumpPad);

            // Strategic Weapon Drops and Powerup Spawners across Overtown
            ArenaPowerup.Create(new Vector3(0f, 3.6f, 0f), PowerupType.WeaponSniperRifle, matRifle, matVisor);
            ArenaPowerup.Create(new Vector3(0f, 4.3f, 18.5f), PowerupType.WeaponAssaultRifle, matRifle, matTerracotta);
            ArenaPowerup.Create(new Vector3(0f, 3.9f, -21f), PowerupType.WeaponMachineGun, matRifle, matTerracotta);
            ArenaPowerup.Create(new Vector3(18f, 5.3f, 0f), PowerupType.WeaponPlasmaCannon, matPlasma, matVisor);
            ArenaPowerup.Create(new Vector3(-18f, 1.0f, 0f), PowerupType.WeaponScattergun, matRifle, matTerracotta);
            ArenaPowerup.Create(new Vector3(0f, 0.8f, 21f), PowerupType.RapidFire, matPowerupRapid, matTerracotta);
            ArenaPowerup.Create(new Vector3(18f, 0.8f, 14f), PowerupType.ShieldBoost, matPowerupShield, matVisor);
            ArenaPowerup.Create(new Vector3(-18f, 0.8f, -14f), PowerupType.SpeedBoost, matPowerupSpeed, matTerracotta);

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
            Debug.Log("RUSH Arena: Full Sunlit Modular Town Arena (Overtown) configured!");
        }

        private static Material GetOrCreateMaterial(string name, Color color, float smoothness = 0.2f)
        {
            string path = MaterialFolder + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = color;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
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

        [MenuItem("RUSH Arena/Build Cloudflare Pages WebGL")]
        public static void BuildCloudflareWebGL()
        {
            Configure();
            const string output = "Builds/WebGLCloudflare";
            Directory.CreateDirectory(output);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = output,
                target = BuildTarget.WebGL, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Cloudflare WebGL build failed: " + report.summary.result);

            string headersContent = @"/*
  Access-Control-Allow-Origin: *

/Build/*.loader.js
  Content-Type: application/javascript

/Build/*.wasm.unityweb
  Content-Type: application/wasm

/Build/*.framework.js.unityweb
  Content-Type: application/javascript

/Build/*.data.unityweb
  Content-Type: application/octet-stream
";
            File.WriteAllText(Path.Combine(output, "_headers"), headersContent.Replace("\r\n", "\n"));
            Debug.Log("RUSH Cloudflare WebGL build succeeded: " + output);
        }
    }
}
