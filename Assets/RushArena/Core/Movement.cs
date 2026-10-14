using System;

namespace RushArena.Core
{
    [Serializable]
    public sealed class MovementTuning
    {
        public float WalkSpeed = 7f;
        public float SprintSpeed = 10.5f;
        public float GroundAcceleration = 65f;
        public float AirAcceleration = 18f;
        public float Gravity = 25f;
        public float JumpHeight = 1.4f;
        public float CoyoteSeconds = 0.10f;
        public float JumpBufferSeconds = 0.12f;
        public float GroundStickSpeed = 2f;
        public float TerminalSpeed = 45f;
    }

    // Look is already converted to degrees by each input device.
    public struct PlayerCommand
    {
        public float MoveX, MoveY, LookX, LookY;
        public bool Sprint, JumpPressed, Fire, SwitchWeapon;
    }

    public readonly struct MotionDelta
    {
        public readonly float X, Y, Z;
        public MotionDelta(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    // Integration rules only. Collision resolution belongs to the Unity motor.
    public sealed class MovementModel
    {
        private readonly MovementTuning tuning;
        private float velocityX, velocityZ, coyoteLeft, jumpBufferLeft;
        private bool jumpConsumed;
        public float VerticalSpeed { get; private set; }
        public float HorizontalSpeed => (float)Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
        public bool JumpedThisStep { get; private set; }

        public MovementModel(MovementTuning tuning) { this.tuning = tuning ?? throw new ArgumentNullException(nameof(tuning)); }

        public void Reset()
        {
            velocityX = velocityZ = VerticalSpeed = coyoteLeft = jumpBufferLeft = 0f;
            jumpConsumed = JumpedThisStep = false;
        }

        public MotionDelta Step(PlayerCommand input, float seconds, bool grounded, bool hitCeiling, float yawDegrees)
        {
            if (seconds <= 0f || seconds > 0.05f || float.IsNaN(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds), "Motor must substep at <= 50 ms.");

            JumpedThisStep = false;
            coyoteLeft = Math.Max(0f, coyoteLeft - seconds);
            jumpBufferLeft = input.JumpPressed ? tuning.JumpBufferSeconds : Math.Max(0f, jumpBufferLeft - seconds);
            if (hitCeiling && VerticalSpeed > 0f) VerticalSpeed = 0f;
            if (grounded && VerticalSpeed <= 0f)
            {
                coyoteLeft = tuning.CoyoteSeconds;
                jumpConsumed = false;
                VerticalSpeed = -tuning.GroundStickSpeed;
            }
            if (!jumpConsumed && coyoteLeft > 0f && jumpBufferLeft > 0f)
            {
                VerticalSpeed = (float)Math.Sqrt(2f * tuning.Gravity * tuning.JumpHeight);
                coyoteLeft = jumpBufferLeft = 0f;
                jumpConsumed = JumpedThisStep = true;
            }
            if (!grounded || VerticalSpeed > 0f)
                VerticalSpeed = Math.Max(-tuning.TerminalSpeed, VerticalSpeed - tuning.Gravity * seconds);

            float length = (float)Math.Sqrt(input.MoveX * input.MoveX + input.MoveY * input.MoveY);
            float divisor = Math.Max(1f, length);
            float x = input.MoveX / divisor, z = input.MoveY / divisor;
            double angle = yawDegrees * Math.PI / 180.0;
            float speed = input.Sprint ? tuning.SprintSpeed : tuning.WalkSpeed;
            float targetX = (x * (float)Math.Cos(angle) + z * (float)Math.Sin(angle)) * speed;
            float targetZ = (z * (float)Math.Cos(angle) - x * (float)Math.Sin(angle)) * speed;
            float dx = targetX - velocityX, dz = targetZ - velocityZ;
            float distance = (float)Math.Sqrt(dx * dx + dz * dz);
            float change = (grounded ? tuning.GroundAcceleration : tuning.AirAcceleration) * seconds;
            float fraction = distance > change ? change / distance : 1f;
            velocityX += dx * fraction;
            velocityZ += dz * fraction;
            return new MotionDelta(velocityX * seconds, VerticalSpeed * seconds, velocityZ * seconds);
        }
    }

    public static class LookModel
    {
        public static float Pitch(float current, float delta) => Math.Max(-85f, Math.Min(85f, current - delta));
        public static float Yaw(float current, float delta)
        {
            float result = (current + delta) % 360f;
            return result < 0f ? result + 360f : result;
        }
    }
}
