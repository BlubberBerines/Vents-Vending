using UnityEngine;

namespace VentsAndVending.World
{
    /// <summary>Pure rules for pushing a crate one step at a time (station layers spec §6.2).</summary>
    public static class CratePushLogic
    {
        /// <summary>The world cardinal axis (±X or ±Z) closest to where the player is looking, ignoring pitch.</summary>
        public static Vector3 PushAxis(Vector3 playerForward)
        {
            float x = playerForward.x, z = playerForward.z;
            if (Mathf.Abs(x) > Mathf.Abs(z))
            {
                return x >= 0f ? Vector3.right : Vector3.left;
            }

            return z >= 0f ? Vector3.forward : Vector3.back;
        }

        /// <summary>A push only happens into free space with floor under it (never into a wall, a crate or off an edge).</summary>
        public static bool CanPush(bool destinationOverlaps, bool groundUnderDestination)
        {
            return !destinationOverlaps && groundUnderDestination;
        }
    }
}
