using System.Collections.Generic;
using RushArena.Runtime.Audio;
using UnityEngine;

namespace RushArena.Multiplayer
{
    public enum PowerupType
    {
        RapidFire = 0,
        ShieldBoost = 1,
        SpeedBoost = 2
    }

    // Floating 3D holographic powerups on pedestals with automatic respawn timers.
    public sealed class ArenaPowerup : MonoBehaviour
    {
        public static readonly List<ArenaPowerup> Active = new List<ArenaPowerup>();

        [SerializeField] private PowerupType type = PowerupType.RapidFire;
        [SerializeField] private float respawnDuration = 15f;
        private float availableAt;
        private Transform iconRoot;
        private Renderer[] visualRenderers;

        public PowerupType Type => type;
        public bool IsAvailable => Time.time >= availableAt;

        public static ArenaPowerup Create(Vector3 position, PowerupType type, Material material)
        {
            var root = new GameObject($"Powerup_{type}");
            root.transform.position = position;

            // Pedestal base
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Pedestal";
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localPosition = new Vector3(0f, -0.7f, 0f);
            baseObj.transform.localScale = new Vector3(1.2f, 0.15f, 1.2f);
            SafeDestroy(baseObj.GetComponent<Collider>());
            if (material != null) baseObj.GetComponent<Renderer>().sharedMaterial = material;

            // Hologram icon
            var icon = new GameObject("HoloIcon");
            icon.transform.SetParent(root.transform, false);
            icon.transform.localPosition = Vector3.up * 0.4f;

            if (type == PowerupType.RapidFire)
            {
                // Triple bolt icon
                for (int i = -1; i <= 1; i++)
                {
                    var bolt = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bolt.transform.SetParent(icon.transform, false);
                    bolt.transform.localPosition = new Vector3(i * 0.22f, 0f, 0f);
                    bolt.transform.localScale = new Vector3(0.12f, 0.55f, 0.12f);
                    SafeDestroy(bolt.GetComponent<Collider>());
                    if (material != null) bolt.GetComponent<Renderer>().sharedMaterial = material;
                }
            }
            else if (type == PowerupType.ShieldBoost)
            {
                // Shield disc icon
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.transform.SetParent(icon.transform, false);
                disc.transform.localScale = new Vector3(0.6f, 0.08f, 0.6f);
                disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                SafeDestroy(disc.GetComponent<Collider>());
                if (material != null) disc.GetComponent<Renderer>().sharedMaterial = material;
            }
            else // SpeedBoost
            {
                // Chevrons icon
                for (int i = 0; i < 2; i++)
                {
                    var chev = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    chev.transform.SetParent(icon.transform, false);
                    chev.transform.localPosition = new Vector3(0f, (i - 0.5f) * 0.28f, 0f);
                    chev.transform.localScale = new Vector3(0.55f, 0.14f, 0.14f);
                    SafeDestroy(chev.GetComponent<Collider>());
                    if (material != null) chev.GetComponent<Renderer>().sharedMaterial = material;
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
                if (p != null && p.IsAvailable && (p.transform.position - playerPosition).sqrMagnitude <= 2.2f)
                {
                    p.availableAt = Time.time + p.respawnDuration;
                    p.SetVisualActive(false);
                    collectedType = p.Type;
                    ProceduralAudio.PlayPowerup();
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
                iconRoot.Rotate(0f, 80f * Time.deltaTime, 0f, Space.World);
                iconRoot.localPosition = new Vector3(0f, 0.4f + Mathf.Sin(Time.time * 3f) * 0.1f, 0f);
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

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }
}
