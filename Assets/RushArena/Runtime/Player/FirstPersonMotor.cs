using RushArena.Core;
using RushArena.Input;
using Unity.Profiling;
using UnityEngine;

namespace RushArena.Player
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputSource))]
    public sealed class FirstPersonMotor : MonoBehaviour
    {
        [SerializeField] private MovementTuning tuning = new MovementTuning();
        [SerializeField] private Transform cameraPivot;
        private CharacterController controller;
        private IPlayerInput input;
        private MovementModel motion;
        private bool ceiling;
        private float yaw, pitch;
        private Vector3 spawn;
        private static readonly ProfilerMarker MovementMarker = new ProfilerMarker("RUSH.Movement");
        public bool Grounded => controller != null && controller.isGrounded;
        public float Speed => controller != null ? new Vector2(controller.velocity.x,controller.velocity.z).magnitude : 0f;

        public void Configure(Transform pivot) { cameraPivot = pivot; }
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputSource>();
            motion = new MovementModel(tuning);
            spawn = transform.position;
            yaw = transform.eulerAngles.y;
        }
        private void Update() { Simulate(input.Sample(), Time.deltaTime, input.IsActive); }

        // Public seam for physics integration tests; does not require hardware input.
        public void Simulate(PlayerCommand command, float frameSeconds, bool active = true)
        {
            if (!active || frameSeconds <= 0f) { motion.Reset(); return; }
            using (MovementMarker.Auto())
            {
                yaw = LookModel.Yaw(yaw, command.LookX);
                pitch = LookModel.Pitch(pitch, command.LookY);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                // At most 100 ms is simulated after a stall. Input look is applied once.
                float remaining = Mathf.Min(frameSeconds, 0.1f);
                while (remaining > 0f)
                {
                    float seconds = Mathf.Min(remaining, 1f / 120f);
                    var delta = motion.Step(command, seconds, controller.isGrounded, ceiling, yaw);
                    CollisionFlags flags = controller.Move(new Vector3(delta.X, delta.Y, delta.Z));
                    ceiling = (flags & CollisionFlags.Above) != 0;
                    command.JumpPressed = false;
                    remaining -= seconds;
                }
                if (transform.position.y < -10f) Respawn();
            }
        }
        public void Respawn()
        {
            controller.enabled = false;
            transform.position = spawn;
            controller.enabled = true;
            motion.Reset(); ceiling = false;
        }
        private void OnApplicationFocus(bool focused) { if (!focused) motion.Reset(); }
        private void OnApplicationPause(bool paused) { if (paused) motion.Reset(); }
    }
}
