using System.Collections.Generic;
using RushArena.Core;
using RushArena.Runtime.Audio;
using UnityEngine;

namespace RushArena.Multiplayer
{
    public enum PowerupType
    {
        RapidFire = 0,
        ShieldBoost = 1,
        SpeedBoost = 2,
        WeaponAssaultRifle = 3,
        WeaponPlasmaCannon = 4,
        WeaponScattergun = 5
    }

    // Floating 3D holographic weapon drops and tactical powerups on pedestals with automatic respawn timers.
    public sealed class ArenaPowerup : MonoBehaviour
    {
        public static readonly List<ArenaPowerup> Active = new List<ArenaPowerup>();

        [SerializeField] private PowerupType type = PowerupType.RapidFire;
        [SerializeField] private float respawnDuration = 18f;
        private float availableAt;
        private Transform iconRoot;
        private Renderer[] visualRenderers;

        public PowerupType Type => type;
        public bool IsAvailable => Time.time >= availableAt;

        public string DisplayName => type switch
        {
            PowerupType.WeaponAssaultRifle => "ASSAULT RIFLE",
            PowerupType.WeaponPlasmaCannon => "PLASMA CANNON",
            PowerupType.WeaponScattergun => "SCATTERGUN",
            PowerupType.ShieldBoost => "SHIELD +50",
            PowerupType.SpeedBoost => "SUPER SPEED",
            PowerupType.RapidFire => "OVERCHARGE",
            _ => "POWERUP"
        };

        public Color AccentColor => type switch
        {
            PowerupType.WeaponAssaultRifle => new Color(1f, 0.8f, 0.2f),
            PowerupType.WeaponPlasmaCannon => new Color(0.2f, 0.85f, 1f),
            PowerupType.WeaponScattergun => new Color(1f, 0.45f, 0.15f),
            PowerupType.ShieldBoost => new Color(0.25f, 0.75f, 1f),
            PowerupType.SpeedBoost => new Color(0.3f, 1f, 0.4f),
            PowerupType.RapidFire => new Color(1f, 0.3f, 0.9f),
            _ => Color.white
        };

        public static ArenaPowerup Create(Vector3 position, PowerupType type, Material material, Material accentMaterial = null)
        {
            var root = new GameObject($"Powerup_{type}");
            root.transform.position = position;

            // Pedestal base
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Pedestal";
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localPosition = new Vector3(0f, -0.65f, 0f);
            baseObj.transform.localScale = new Vector3(1.35f, 0.18f, 1.35f);
            SafeDestroy(baseObj.GetComponent<Collider>());
            if (material != null) baseObj.GetComponent<Renderer>().sharedMaterial = material;

            // Pedestal rim ring
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "PedestalRim";
            rim.transform.SetParent(root.transform, false);
            rim.transform.localPosition = new Vector3(0f, -0.56f, 0f);
            rim.transform.localScale = new Vector3(1.42f, 0.04f, 1.42f);
            SafeDestroy(rim.GetComponent<Collider>());
            if (accentMaterial != null) rim.GetComponent<Renderer>().sharedMaterial = accentMaterial;
            else if (material != null) rim.GetComponent<Renderer>().sharedMaterial = material;

            // Hologram icon root (rotates & bobs)
            var icon = new GameObject("HoloIcon");
            icon.transform.SetParent(root.transform, false);
            icon.transform.localPosition = Vector3.up * 0.45f;

            // Vertical beacon beam
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "BeaconBeam";
            beam.transform.SetParent(icon.transform, false);
            beam.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            beam.transform.localScale = new Vector3(0.08f, 1.8f, 0.08f);
            SafeDestroy(beam.GetComponent<Collider>());
            if (accentMaterial != null) beam.GetComponent<Renderer>().sharedMaterial = accentMaterial;

            // 3D Visuals per type
            if (type == PowerupType.WeaponAssaultRifle)
            {
                ProceduralVisuals.BuildWeaponPickupModel(icon.transform, WeaponType.AssaultRifle, material, accentMaterial ?? material);
            }
            else if (type == PowerupType.WeaponPlasmaCannon)
            {
                ProceduralVisuals.BuildWeaponPickupModel(icon.transform, WeaponType.PlasmaCannon, material, accentMaterial ?? material);
            }
            else if (type == PowerupType.WeaponScattergun)
            {
                ProceduralVisuals.BuildWeaponPickupModel(icon.transform, WeaponType.Scattergun, material, accentMaterial ?? material);
            }
            else if (type == PowerupType.ShieldBoost)
            {
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.transform.SetParent(icon.transform, false);
                disc.transform.localScale = new Vector3(0.65f, 0.08f, 0.65f);
                disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                SafeDestroy(disc.GetComponent<Collider>());
                if (material != null) disc.GetComponent<Renderer>().sharedMaterial = material;

                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.transform.SetParent(icon.transform, false);
                ring.transform.localScale = new Vector3(0.85f, 0.03f, 0.85f);
                ring.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                SafeDestroy(ring.GetComponent<Collider>());
                if (accentMaterial != null) ring.GetComponent<Renderer>().sharedMaterial = accentMaterial;
            }
            else if (type == PowerupType.SpeedBoost)
            {
                for (int i = 0; i < 3; i++)
                {
                    var chev = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    chev.transform.SetParent(icon.transform, false);
                    chev.transform.localPosition = new Vector3(0f, (i - 1f) * 0.22f, 0f);
                    chev.transform.localScale = new Vector3(0.6f, 0.12f, 0.14f);
                    chev.transform.localRotation = Quaternion.Euler(0f, 0f, 25f * (i - 1f));
                    SafeDestroy(chev.GetComponent<Collider>());
                    if (accentMaterial != null) chev.GetComponent<Renderer>().sharedMaterial = accentMaterial;
                    else if (material != null) chev.GetComponent<Renderer>().sharedMaterial = material;
                }
            }
            else // RapidFire
            {
                for (int i = -1; i <= 1; i++)
                {
                    var bolt = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bolt.transform.SetParent(icon.transform, false);
                    bolt.transform.localPosition = new Vector3(i * 0.22f, 0f, 0f);
                    bolt.transform.localScale = new Vector3(0.12f, 0.55f, 0.12f);
                    SafeDestroy(bolt.GetComponent<Collider>());
                    if (accentMaterial != null) bolt.GetComponent<Renderer>().sharedMaterial = accentMaterial;
                    else if (material != null) bolt.GetComponent<Renderer>().sharedMaterial = material;
                }
            }

            var powerup = root.AddComponent<ArenaPowerup>();
            powerup.type = type;
            powerup.iconRoot = icon.transform;
            powerup.visualRenderers = root.GetComponentsInChildren<Renderer>();
            return powerup;
        }

        public static bool TryCollect(Vector3 playerPosition, out PowerupType collectedType)
        {
            for (int i = 0; i < Active.Count; i++)
            {
                var p = Active[i];
                if (p != null && p.IsAvailable && (p.transform.position - playerPosition).sqrMagnitude <= 2.6f)
                {
                    p.availableAt = Time.time + p.respawnDuration;
                    p.SetVisualActive(false);
                    collectedType = p.Type;

                    if (collectedType == PowerupType.WeaponAssaultRifle ||
                        collectedType == PowerupType.WeaponPlasmaCannon ||
                        collectedType == PowerupType.WeaponScattergun)
                    {
                        ProceduralAudio.PlayWeaponPickup();
                    }
                    else
                    {
                        ProceduralAudio.PlayPowerup();
                    }
                    return true;
                }
            }
            collectedType = PowerupType.RapidFire;
            return false;
        }

        private void Awake()
        {
            if (iconRoot == null) iconRoot = transform.Find("HoloIcon");
            if (visualRenderers == null || visualRenderers.Length == 0) visualRenderers = GetComponentsInChildren<Renderer>();
            Active.Add(this);
        }

        private void OnDestroy()
        {
            Active.Remove(this);
        }

        private void Update()
        {
            if (iconRoot != null)
            {
                iconRoot.Rotate(0f, 75f * Time.deltaTime, 0f, Space.World);
                iconRoot.localPosition = new Vector3(0f, 0.45f + Mathf.Sin(Time.time * 2.8f) * 0.12f, 0f);
            }

            bool available = IsAvailable;
            if (iconRoot != null && iconRoot.gameObject.activeSelf != available)
            {
                SetVisualActive(available);
            }
        }

        private void SetVisualActive(bool active)
        {
            if (iconRoot != null) iconRoot.gameObject.SetActive(active);
        }

        private void OnGUI()
        {
            if (!IsAvailable) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPos = transform.position + Vector3.up * 1.5f;
            Vector3 camPos = cam.transform.position;
            float dist = Vector3.Distance(camPos, worldPos);

            if (dist > 28f) return;

            // Linecast occlusion check: don't show floating billboard through walls/obstacles!
            if (Physics.Linecast(camPos, worldPos, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(transform)) return;
            }

            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z > 0.5f)
            {
                float w = 150f;
                float h = 24f;
                float x = screenPos.x - w * 0.5f;
                float y = Screen.height - screenPos.y - h * 0.5f;

                var style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = AccentColor }
                };

                // Translucent background
                GUI.color = new Color(0.04f, 0.06f, 0.12f, 0.85f);
                GUI.DrawTexture(new Rect(x - 4f, y - 2f, w + 8f, h + 4f), Texture2D.whiteTexture);

                // Accent border line
                GUI.color = AccentColor;
                GUI.DrawTexture(new Rect(x - 4f, y + h + 1f, w + 8f, 2f), Texture2D.whiteTexture);

                GUI.color = Color.white;
                GUI.Label(new Rect(x, y, w, h), $"[ {DisplayName} ]", style);
            }
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }
}
