using UnityEngine;

namespace VentsAndVending.World
{
    /// <summary>Pure rules for the automatic full-size sliding door (station layers spec §6.3).</summary>
    public static class SlidingDoorLogic
    {
        /// <summary>Unseal first, then accelerate and cushion the slide; reversing preserves position continuity.</summary>
        public static float LeafTravel(float amount)
        {
            float t=Mathf.Clamp01((amount-.14f)/.86f);
            return t*t*t*(t*(t*6f-15f)+10f);
        }
        /// <summary>Opens for a nearby player, stays open while someone stands in the doorway, never opens when locked.</summary>
        public static bool WantsOpen(bool locked, bool playerNear, bool occupied)
        {
            return !locked && (playerNear || occupied);
        }

        /// <summary>Moves the 0..1 open amount toward the target, taking <paramref name="openSeconds"/> for a full stroke.</summary>
        public static float Step(float openAmount, bool open, float openSeconds, float dt)
        {
            float rate = dt / Mathf.Max(0.01f, openSeconds);
            return Mathf.Clamp01(openAmount + (open ? rate : -rate));
        }
    }
}
