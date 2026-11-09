using UnityEngine;

namespace VentsAndVending.Player
{
    /// <summary>
    /// Pure climb (mantle) rules: crates and ledges up to ~1.1 m, and head-height vent mouths from on top of a crate
    /// (Docs/superpowers/specs/2026-11-09-station-ring-layers-design.md §6.1). No stamina, can't fail.
    /// </summary>
    public static class ClimbLogic
    {
        /// <summary>A climb can only start on the ground, upright or crouched, and not while operating something.</summary>
        public static bool CanStartClimb(PlayerMovementState state, bool grounded, bool interacting)
        {
            return grounded && !interacting &&
                   (state == PlayerMovementState.Normal || state == PlayerMovementState.Sprinting ||
                    state == PlayerMovementState.Crouching);
        }

        /// <summary>
        /// Decides whether a ledge can be climbed and the posture the player ends in on top of it.
        /// </summary>
        /// <param name="ledgeHeight">Ledge top above the player's feet (m).</param>
        /// <param name="clearance">Free height above the ledge top (m).</param>
        /// <param name="depth">Free depth on top of the ledge (m).</param>
        public static bool TryResolve(float ledgeHeight, float clearance, float depth, PlayerTuning tuning,
                                      out PlayerMovementState posture)
        {
            posture = PlayerMovementState.Normal;
            if (ledgeHeight < tuning.MinClimbHeight || ledgeHeight > tuning.MaxClimbHeight ||
                clearance < tuning.CrawlCapsuleHeight || depth < tuning.MinClimbDepth)
            {
                return false;
            }

            if (clearance >= tuning.NormalCapsuleHeight)
            {
                posture = PlayerMovementState.Normal;
            }
            else if (clearance >= tuning.CrouchCapsuleHeight)
            {
                posture = PlayerMovementState.Crouching;
            }
            else
            {
                posture = PlayerMovementState.Crawling;
            }

            return true;
        }

        /// <summary>How long the pull-up takes: short and readable, 0.4-0.6 s.</summary>
        public static float Duration(float ledgeHeight, PlayerTuning tuning)
        {
            return Mathf.Clamp(tuning.ClimbBaseTime + tuning.ClimbTimePerMetre * ledgeHeight, 0.4f, 0.6f);
        }
    }
}
