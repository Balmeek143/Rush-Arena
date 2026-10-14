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
        WeaponScattergun = 5,
        WeaponSniperRifle = 6,
        WeaponMachineGun = 7
    }

    // Floating 3D holographic weapon drops and tactical powerups.
    // Dynamically manages active count = total players in arena - 1, with a dismiss timer and weighted drops.
    public sealed class ArenaPowerup : MonoBehaviour
    {
        public static readonly List<ArenaPowerup> Active = new List<ArenaPowerup>();

        [SerializeField] private PowerupType type = PowerupType.RapidFire;
        [SerializeField] private float respawnDuration = 14f;
        private float availableAt;
        private float dismissAt;
        private bool isSpawnedActive;
        private Transform iconRoot;
        private Material baseMaterial;
        private Material accentMat;

        public PowerupType Type => type;
        public bool IsAvailable => Time.time >= availableAt;
        public bool IsSpawnedActive => isSpawnedActive;

        public string DisplayName => type switch
        {
            PowerupType.WeaponAssaultRifle => "ASSAULT RIFLE",
            PowerupType.WeaponPlasmaCannon => "PLASMA CANNON",
            PowerupType.WeaponScattergun => "SCATTERGUN",
            PowerupType.WeaponSniperRifle => "SNIPER RIFLE",
            PowerupType.WeaponMachineGun => "HEAVY MACHINE GUN",
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
            PowerupType.WeaponSniperRifle => new Color(0.35f, 1f, 0.5f),
            PowerupType.WeaponMachineGun => new Color(1f, 0.25f, 0.1f),
            PowerupType.ShieldBoost => new Color(0.25f, 0.75f, 1f),
            PowerupType.SpeedBoost => new Color(0.3f, 1f, 0.4f),
            PowerupType.RapidFire => new Color(1f, 0.3f, 0.9f),
            _ => Color.white
        };

        private static readonly Vector3[] ArenaSpawnWaypoints = new Vector3[]
        {
            new Vector3(0f, 4.2f, 0f),       // Center platform
            new Vector3(14f, 0.8f, 0f),      // East lane
            new Vector3(-14f, 0.8f, 0f),     // West lane
            new Vector3(0f, 0.8f, 14f),      // North courtyard
            new Vector3(0f, 0.8f, -14f),     // South courtyard
            new Vector3(12f, 0.8f, 12f),     // NE sector
            new Vector3(-12f, 0.8f, 12f),    // NW sector
            new Vector3(12f, 0.8f, -12f),    // SE sector
            new Vector3(-12f, 0.8f, -12f),   // SW sector
            new Vector3(0f, 2.0f, 8f),       // North ramp
            new Vector3(0f, 2.0f, -8f)       // South ramp
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

            var powerup = root.AddComponent<ArenaPowerup>();
            powerup.type = type;
            powerup.iconRoot = icon.transform;
            powerup.baseMaterial = material;
            powerup.accentMat = accentMaterial ?? material;

            powerup.BuildHoloVisuals(icon.transform, type);

            return powerup;
        }

        private void BuildHoloVisuals(Transform parent, PowerupType pType)
        {
            // Clear existing visuals
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                SafeDestroy(parent.GetChild(i).gameObject);
            }

            if (pType == PowerupType.WeaponAssaultRifle)
            {
                ProceduralVisuals.BuildWeaponPickupModel(parent, WeaponType.AssaultRifle, baseMaterial, accentMat);
            }
            else if (pType == PowerupType.WeaponPlasmaCannon)
            {
                ProceduralVisuals.BuildWeaponPickupModel(parent, WeaponType.PlasmaCannon, baseMaterial, accentMat);
            }
            else if (pType == PowerupType.WeaponScattergun)
            {
                ProceduralVisuals.BuildWeaponPickupModel(parent, WeaponType.Scattergun, baseMaterial, accentMat);
            }
            else if (pType == PowerupType.WeaponSniperRifle)
            {
                ProceduralVisuals.BuildWeaponPickupModel(parent, WeaponType.SniperRifle, baseMaterial, accentMat);
            }
            else if (pType == PowerupType.WeaponMachineGun)
            {
                ProceduralVisuals.BuildWeaponPickupModel(parent, WeaponType.MachineGun, baseMaterial, accentMat);
            }
            else if (pType == PowerupType.ShieldBoost)
            {
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.transform.SetParent(parent, false);
                disc.transform.localScale = new Vector3(0.65f, 0.08f, 0.65f);
                disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                SafeDestroy(disc.GetComponent<Collider>());
                if (baseMaterial != null) disc.GetComponent<Renderer>().sharedMaterial = baseMaterial;

                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.transform.SetParent(parent, false);
                ring.transform.localScale = new Vector3(0.85f, 0.03f, 0.85f);
                ring.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                SafeDestroy(ring.GetComponent<Collider>());
                if (accentMat != null) ring.GetComponent<Renderer>().sharedMaterial = accentMat;
            }
            else if (pType == PowerupType.SpeedBoost)
            {
                for (int i = 0; i < 3; i++)
                {
                    var chev = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    chev.transform.SetParent(parent, false);
                    chev.transform.localPosition = new Vector3(0f, (i - 1f) * 0.22f, 0f);
                    chev.transform.localScale = new Vector3(0.6f, 0.12f, 0.14f);
                    chev.transform.localRotation = Quaternion.Euler(0f, 0f, 25f * (i - 1f));
                    SafeDestroy(chev.GetComponent<Collider>());
                    if (accentMat != null) chev.GetComponent<Renderer>().sharedMaterial = accentMat;
                    else if (baseMaterial != null) chev.GetComponent<Renderer>().sharedMaterial = baseMaterial;
                }
            }
            else // RapidFire
            {
                for (int i = -1; i <= 1; i++)
                {
                    var bolt = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bolt.transform.SetParent(parent, false);
                    bolt.transform.localPosition = new Vector3(i * 0.22f, 0f, 0f);
                    bolt.transform.localScale = new Vector3(0.12f, 0.55f, 0.12f);
                    SafeDestroy(bolt.GetComponent<Collider>());
                    if (accentMat != null) bolt.GetComponent<Renderer>().sharedMaterial = accentMat;
                    else if (baseMaterial != null) bolt.GetComponent<Renderer>().sharedMaterial = baseMaterial;
                }
            }
        }

        public static PowerupType GetRandomPowerupType()
        {
            // Distribution:
            // Machine Gun: 8% (Rare drop)
            // Sniper Rifle: 16% (Readily accessible)
            // Assault Rifle: 16%
            // Plasma Cannon: 16%
            // Scattergun: 16%
            // Shield Boost: 10%
            // Speed Boost: 10%
            // RapidFire/Overcharge: 8%
            int roll = Random.Range(0, 100);
            if (roll < 8) return PowerupType.WeaponMachineGun;
            if (roll < 24) return PowerupType.WeaponSniperRifle;
            if (roll < 40) return PowerupType.WeaponAssaultRifle;
            if (roll < 56) return PowerupType.WeaponPlasmaCannon;
            if (roll < 72) return PowerupType.WeaponScattergun;
            if (roll < 82) return PowerupType.ShieldBoost;
            if (roll < 92) return PowerupType.SpeedBoost;
            return PowerupType.RapidFire;
        }

        public static int GetTotalCombatants()
        {
            int players = SharedPlayerAvatar.AllAvatars.Count > 0 ? SharedPlayerAvatar.AllAvatars.Count : 1;
            int bots = ArenaBot.AllBots != null ? ArenaBot.AllBots.Count : 0;
            return players + bots;
        }

        public static int GetTargetActiveCount()
        {
            return Mathf.Max(1, GetTotalCombatants() - 1);
        }

        public static bool TryCollect(Vector3 playerPosition, out PowerupType collectedType)
        {
            for (int i = 0; i < Active.Count; i++)
            {
                var p = Active[i];
                if (p != null && p.isSpawnedActive && p.IsAvailable && (p.transform.position - playerPosition).sqrMagnitude <= 2.6f)
                {
                    p.isSpawnedActive = false;
                    p.availableAt = Time.time + Mathf.Max(6f, p.respawnDuration + Random.Range(-2f, 4f));
                    p.SetVisualActive(false);
                    collectedType = p.Type;

                    if (collectedType == PowerupType.WeaponAssaultRifle ||
                        collectedType == PowerupType.WeaponPlasmaCannon ||
                        collectedType == PowerupType.WeaponScattergun ||
                        collectedType == PowerupType.WeaponSniperRifle ||
                        collectedType == PowerupType.WeaponMachineGun)
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
            Active.Add(this);
            isSpawnedActive = false;
            SetVisualActive(false);
        }

        private void OnDestroy()
        {
            Active.Remove(this);
        }

        private void Update()
        {
            if (iconRoot != null && isSpawnedActive && IsAvailable)
            {
                iconRoot.Rotate(0f, 75f * Time.deltaTime, 0f, Space.World);
                iconRoot.localPosition = new Vector3(0f, 0.45f + Mathf.Sin(Time.time * 2.8f) * 0.12f, 0f);
            }

            // The first active powerup coordinates global spawning to enforce:
            // Active powerups = total players in arena - 1 (min 1)
            if (Active.Count > 0 && Active[0] == this)
            {
                UpdateManager();
            }

            // Check dismiss timer: uncollected item expires, plays chime, and relocates to random place
            if (isSpawnedActive && IsAvailable)
            {
                if (Time.time >= dismissAt)
                {
                    ProceduralAudio.PlayPowerupDismiss();
                    RelocateToRandomWaypoint();
                    type = GetRandomPowerupType();
                    if (iconRoot != null)
                    {
                        BuildHoloVisuals(iconRoot, type);
                    }
                    dismissAt = Time.time + 25f;
                }
            }
        }

        private static void UpdateManager()
        {
            int target = GetTargetActiveCount();
            int currentActive = 0;
            for (int i = 0; i < Active.Count; i++)
            {
                if (Active[i] != null && Active[i].isSpawnedActive && Active[i].IsAvailable)
                {
                    currentActive++;
                }
            }

            if (currentActive < target)
            {
                for (int i = 0; i < Active.Count && currentActive < target; i++)
                {
                    var p = Active[i];
                    if (p != null && !p.isSpawnedActive && p.IsAvailable)
                    {
                        p.ActivateItem();
                        currentActive++;
                    }
                }
            }
            else if (currentActive > target)
            {
                for (int i = Active.Count - 1; i >= 0 && currentActive > target; i--)
                {
                    var p = Active[i];
                    if (p != null && p.isSpawnedActive && p.IsAvailable)
                    {
                        p.DeactivateItem();
                        currentActive--;
                    }
                }
            }
        }

        public void ActivateItem()
        {
            isSpawnedActive = true;
            type = GetRandomPowerupType();
            RelocateToRandomWaypoint();
            dismissAt = Time.time + 25f;
            if (iconRoot != null)
            {
                BuildHoloVisuals(iconRoot, type);
            }
            SetVisualActive(true);
        }

        public void DeactivateItem()
        {
            isSpawnedActive = false;
            availableAt = Time.time + respawnDuration;
            SetVisualActive(false);
        }

        private void RelocateToRandomWaypoint()
        {
            // Pick a random location that doesn't have an available powerup within 4 meters
            int startIdx = Random.Range(0, ArenaSpawnWaypoints.Length);
            for (int i = 0; i < ArenaSpawnWaypoints.Length; i++)
            {
                Vector3 candidate = ArenaSpawnWaypoints[(startIdx + i) % ArenaSpawnWaypoints.Length];
                bool occupied = false;
                for (int j = 0; j < Active.Count; j++)
                {
                    var other = Active[j];
                    if (other != null && other != this && other.isSpawnedActive && other.IsAvailable && Vector3.Distance(other.transform.position, candidate) < 3.5f)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    transform.position = candidate;
                    break;
                }
            }
        }

        private void SetVisualActive(bool active)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = active;
            }
        }

        private void OnGUI()
        {
            if (!isSpawnedActive || !IsAvailable) return;

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
