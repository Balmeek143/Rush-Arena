using UnityEngine;

namespace RushArena.Multiplayer
{
    // Backwards-compatible wrapper delegating to ArenaPowerup.
    public sealed class RapidFirePowerup : MonoBehaviour
    {
        public static void Create(Vector3 position)
        {
            ArenaPowerup.Create(position, PowerupType.RapidFire, null);
        }

        public static bool TryCollect(Vector3 position)
        {
            if (ArenaPowerup.TryCollect(position, out var type) && type == PowerupType.RapidFire)
            {
                return true;
            }
            return false;
        }
    }
}
