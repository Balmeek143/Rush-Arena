using RushArena.Diagnostics;
using RushArena.Input;
using RushArena.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace RushArena.Arena
{
    public sealed class ArenaBootstrap : MonoBehaviour
    {
        [SerializeField] private bool editorTouchPreview;
        [SerializeField] private bool spawnLocalPlayer = true;
        [SerializeField] private Shader arenaShader;
        private Material floorMaterial, wallMaterial, accentMaterial;
        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Application.isMobilePlatform ? 60 : 144;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.65f, 0.75f);
            Shader shader = arenaShader != null ? arenaShader : Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new System.InvalidOperationException("URP shader missing. Run RUSH Arena > Configure Milestone 1.");
            floorMaterial = CreateMaterial(shader, new Color(0.17f, 0.22f, 0.28f));
            wallMaterial = CreateMaterial(shader, new Color(0.38f, 0.44f, 0.52f));
            accentMaterial = CreateMaterial(shader, new Color(0.1f, 0.75f, 0.65f));
            CreateArena();
            var sun = new GameObject("Arena directional light").AddComponent<Light>();
            sun.transform.SetParent(transform);
            sun.type = LightType.Directional; sun.intensity = 1.2f; sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            if (spawnLocalPlayer) CreatePlayer();
        }
        private Material CreateMaterial(Shader shader, Color color)
        {
            var material = new Material(shader) { color = color };
            material.SetFloat("_Smoothness", 0f);
            return material;
        }
        private void Block(string name, Vector3 position, Vector3 scale, Material material, float tilt = 0f)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name; block.transform.SetParent(transform);
            block.transform.SetPositionAndRotation(position, Quaternion.Euler(tilt, 0f, 0f));
            block.transform.localScale = scale;
            var renderer = block.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        private void CreateArena()
        {
            Block("48m arena floor", new Vector3(0,-0.5f,0), new Vector3(48,1,48), floorMaterial);
            Block("North boundary", new Vector3(0,3,24), new Vector3(49,6,1), wallMaterial);
            Block("South boundary", new Vector3(0,3,-24), new Vector3(49,6,1), wallMaterial);
            Block("East boundary", new Vector3(24,3,0), new Vector3(1,6,48), wallMaterial);
            Block("West boundary", new Vector3(-24,3,0), new Vector3(1,6,48), wallMaterial);
            Block("Central cover", new Vector3(0,1,0), new Vector3(6,2,4), accentMaterial);
            for (int i = -1; i <= 1; i += 2)
            {
                Block("Lane cover", new Vector3(i*12,0.8f,4), new Vector3(3,1.6f,5), wallMaterial);
                Block("Jump platform", new Vector3(i*8,0.45f,-6), new Vector3(3,0.9f,3), accentMaterial);
            }
            for (int i = 0; i < 5; i++)
                Block("Step " + i, new Vector3(8,0.1f*(i+1),9+i*0.6f), new Vector3(3,0.2f*(i+1),0.6f), accentMaterial);
            Block("Ramp 20 degrees", new Vector3(-8,0.8f,10), new Vector3(4,0.4f,6), accentMaterial, -20f);
            Block("Low ceiling test", new Vector3(-16,2.65f,-8), new Vector3(4,0.3f,4), wallMaterial);
        }
        private void CreatePlayer()
        {
            var player = new GameObject("Local player");
            player.SetActive(false);
            player.transform.SetParent(transform);
            player.transform.position = new Vector3(0,0.08f,-18);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = 0.35f; controller.center = new Vector3(0,0.9f,0);
            controller.stepOffset = 0.3f; controller.slopeLimit = 45f; controller.skinWidth = 0.035f; controller.minMoveDistance = 0f;
            var input = player.AddComponent<PlayerInputSource>(); input.ConfigureTouch(editorTouchPreview);
            var pivot = new GameObject("Eye camera"); pivot.transform.SetParent(player.transform);
            pivot.transform.localPosition = new Vector3(0,1.62f,0);
            pivot.tag = "MainCamera";
            var camera = pivot.AddComponent<Camera>();
            camera.fieldOfView = 85f; camera.nearClipPlane = 0.05f; camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.08f,0.11f,0.16f);
            pivot.AddComponent<AudioListener>();
            var motor = player.AddComponent<FirstPersonMotor>(); motor.Configure(pivot.transform);
            player.SetActive(true);
            var diagnostics = player.AddComponent<PerformanceHud>(); diagnostics.Configure(motor, input);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (PlayerSmokeProbe.Requested) player.AddComponent<PlayerSmokeProbe>();
#endif
        }
        private void OnDestroy()
        {
            if (floorMaterial != null) Destroy(floorMaterial);
            if (wallMaterial != null) Destroy(wallMaterial);
            if (accentMaterial != null) Destroy(accentMaterial);
        }
    }
}
