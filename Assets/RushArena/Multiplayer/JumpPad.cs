using System.Collections.Generic;
using RushArena.Runtime.Audio;
using UnityEngine;

namespace RushArena.Multiplayer
{
    // Interactive jump pad that launches players vertically and in the pad's forward direction.
    public sealed class JumpPad : MonoBehaviour
    {
        public static readonly List<JumpPad> All = new List<JumpPad>();

        [SerializeField] private float verticalVelocity = 14f;
        [SerializeField] private float forwardVelocity = 8f;
        private Renderer visualRenderer;
        private Material padMaterial;
        private float activeUntil;

        public static JumpPad Create(Vector3 position, Quaternion rotation, Material material)
        {
            var root = new GameObject("JumpPad");
            root.transform.SetPositionAndRotation(position, rotation);
            
            // Base plate
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "PadBase";
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localScale = new Vector3(2.4f, 0.2f, 2.4f);
            SafeDestroy(baseObj.GetComponent<Collider>());
            if (material != null) baseObj.GetComponent<Renderer>().sharedMaterial = material;

            // Glowing launch platform
            var platformObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platformObj.name = "LaunchRing";
            platformObj.transform.SetParent(root.transform, false);
            platformObj.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            platformObj.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
            SafeDestroy(platformObj.GetComponent<Collider>());
            if (material != null) platformObj.GetComponent<Renderer>().sharedMaterial = material;

            // Trigger collider
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(2.2f, 0.8f, 2.2f);
            col.isTrigger = true;

            var pad = root.AddComponent<JumpPad>();
            pad.padMaterial = material;
            pad.visualRenderer = platformObj.GetComponent<Renderer>();
            return pad;
        }

        private void Awake()
        {
            All.Add(this);
            if (visualRenderer == null) visualRenderer = GetComponentInChildren<Renderer>();
        }

        private void OnDestroy()
        {
            All.Remove(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            var avatar = other.GetComponentInParent<SharedPlayerAvatar>();
            if (avatar != null && avatar.Object != null && avatar.Object.HasStateAuthority)
            {
                Vector3 launchDir = transform.forward * forwardVelocity + Vector3.up * verticalVelocity;
                avatar.ApplyLaunchImpulse(launchDir);
                ProceduralAudio.PlayJumpPad();
                activeUntil = Time.time + 0.35f;
            }
        }

        private void Update()
        {
            if (visualRenderer == null) return;
            float pulse = Time.time < activeUntil
                ? 1.8f
                : 1f + 0.25f * Mathf.Sin(Time.time * 6f);
            transform.localScale = Vector3.one * (Time.time < activeUntil ? 1.05f : 1.0f);
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }
}
