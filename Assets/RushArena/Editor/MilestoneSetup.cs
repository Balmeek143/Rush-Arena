using System;
using System.IO;
using RushArena.Arena;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace RushArena.Editor
{
    public static class MilestoneSetup
    {
        public const string ScenePath = "Assets/RushArena/Scenes/ArenaM1.unity";
        private const string PipelinePath = "Assets/RushArena/Settings/RushURP.asset";

        [MenuItem("RUSH Arena/Configure Milestone 1")]
        public static void Configure()
        {
            if (!Application.isBatchMode && SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene before configuring RUSH Arena.");
            Directory.CreateDirectory("Assets/RushArena/Settings");
            Directory.CreateDirectory("Assets/RushArena/Scenes");
            AssetDatabase.Refresh();
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            PlayerSettings.companyName = "RushArena";
            PlayerSettings.productName = "RUSH Arena";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.rusharena.prototype");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            RequireProperty(playerSettings,"activeInputHandler").intValue = 1;
            playerSettings.ApplyModifiedPropertiesWithoutUndo();

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create();
                var renderer = pipeline.LoadBuiltinRendererData();
                string error = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(renderer),"Assets/RushArena/Settings/RushRenderer.asset");
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                AssetDatabase.CreateAsset(pipeline,PipelinePath);
            }
            // These URP 17.3 properties expose internal setters, so configure their serialized fields.
            var settings = new SerializedObject(pipeline);
            RequireProperty(settings,"m_MainLightShadowsSupported").boolValue = false;
            RequireProperty(settings,"m_AdditionalLightShadowsSupported").boolValue = false;
            RequireProperty(settings,"m_AdditionalLightsRenderingMode").intValue = 0;
            settings.ApplyModifiedPropertiesWithoutUndo();
            pipeline.supportsHDR = false;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.msaaSampleCount = 1;
            pipeline.renderScale = 1f;
            pipeline.useSRPBatcher = true;
            EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int quality = QualitySettings.GetQualityLevel();
            for (int i=0;i<QualitySettings.names.Length;i++)
            {
                QualitySettings.SetQualityLevel(i,false);
                QualitySettings.renderPipeline = pipeline;
                QualitySettings.vSyncCount = 0;
                QualitySettings.antiAliasing = 0;
            }
            QualitySettings.SetQualityLevel(quality,false);
            if (!File.Exists(ScenePath))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                new GameObject("RUSH Arena bootstrap").AddComponent<ArenaBootstrap>();
            }
            else EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var bootstrap = UnityEngine.Object.FindFirstObjectByType<ArenaBootstrap>();
            if (bootstrap == null) throw new InvalidOperationException("M1 scene is missing ArenaBootstrap.");
            var bootstrapSettings = new SerializedObject(bootstrap);
            RequireProperty(bootstrapSettings,"arenaShader").objectReferenceValue = Shader.Find("Universal Render Pipeline/Lit");
            bootstrapSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
            AssetDatabase.SaveAssets();
            Debug.Log("RUSH M1 configured: URP, Input System, landscape arena and build scene.");
        }

        private static SerializedProperty RequireProperty(SerializedObject owner, string name)
        {
            return owner.FindProperty(name) ?? throw new InvalidOperationException("Installed package changed field: "+name);
        }

        [MenuItem("RUSH Arena/Build Windows Development")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64,"Builds/Windows/RushArena.exe");
        [MenuItem("RUSH Arena/Build Android Development")]
        public static void BuildAndroid() => Build(BuildTarget.Android,"Builds/Android/RushArena.apk");
        private static void Build(BuildTarget target, string output)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target),target))
                throw new InvalidOperationException("Install build support through Unity Hub for "+target);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[]{ScenePath}, locationPathName = output, target = target,
                options = BuildOptions.Development
            });
            if (result.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build failed: "+result.summary.result);
            Debug.Log("RUSH build succeeded: "+output);
        }
    }
}
