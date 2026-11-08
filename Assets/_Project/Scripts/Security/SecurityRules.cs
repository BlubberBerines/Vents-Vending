using System;
using System.Collections.Generic;
using VentsAndVending.Inventory;
using VentsAndVending.Player;

namespace VentsAndVending.Security
{
    /// <summary>
    /// Detection and consequence rules (GDD §11.2, §11.5). Pure functions, starting values — tune in playtests.
    /// </summary>
    public static class SecurityRules
    {
        /// <summary>Suspicion per second at point-blank range for a fully visible, maximally suspicious target.</summary>
        public const float BaseSuspicionRate = 0.7f;

        /// <summary>Being somewhere you plainly shouldn't be (a restricted zone such as the maintenance underpass), empty-handed or not.</summary>
        public const float TrespassSuspicion = 0.6f;

        // ------------------------------------------------------------------ Player profile

        /// <summary>How easy the player is to see: crouching and crawling help; vents and hiding spots hide you completely.</summary>
        public static float Visibility(PlayerMovementState posture, bool inVent, bool hidden = false)
        {
            if (inVent || hidden)
            {
                return 0f;
            }

            return posture switch
            {
                PlayerMovementState.Crawling => 0.35f,
                PlayerMovementState.Crouching => 0.6f,
                _ => 1f
            };
        }

        /// <summary>How suspicious the player looks: contraband, dealing in view, sprinting.</summary>
        public static float Suspiciousness(int contrabandUnits, bool dealing, bool sprinting, bool trespassing = false)
        {
            float s = contrabandUnits > 0 ? 0.5f + Math.Min(0.5f, 0.08f * contrabandUnits) : 0f;
            if (dealing)
            {
                s += 1.5f;
            }
            if (sprinting)
            {
                s += 0.25f;
            }
            if (trespassing)
            {
                s += TrespassSuspicion;
            }
            return s;
        }

        /// <summary>Drone flight height in world space: always measured from the floor it patrols (so underground drones stay underground).</summary>
        public static float FlightHeight(bool pursuing, float floorY, float patrolAltitude, float pursuitAltitude, float waypointOffset) =>
            floorY + (pursuing ? pursuitAltitude : patrolAltitude + waypointOffset);

        /// <summary>A catch in a restricted zone with an exit walks the player out (a one-way area must never trap them).</summary>
        public static bool ShouldEscort(bool inRestrictedZone, bool hasExit) => inRestrictedZone && hasExit;

        /// <summary>Security gets stricter after repeated incidents (GDD §11.5 "increased security presence").</summary>
        public static float Vigilance(int incidents) => 1f + 0.15f * Math.Min(Math.Max(0, incidents), 6);

        // ------------------------------------------------------------------ Sight

        public static bool InViewCone(float distance, float viewRange, float angleDegrees, float fieldOfViewDegrees) =>
            distance <= viewRange && angleDegrees <= fieldOfViewDegrees * 0.5f;

        /// <summary>Suspicion gained per second. Zero without line of sight, outside the cone, or when nothing is suspicious.</summary>
        public static float SuspicionRate(float distance, float viewRange, float angleDegrees, float fieldOfViewDegrees,
            bool lineOfSight, float visibility, float suspiciousness, float vigilance)
        {
            if (!lineOfSight || visibility <= 0f || suspiciousness <= 0f || !InViewCone(distance, viewRange, angleDegrees, fieldOfViewDegrees))
            {
                return 0f;
            }

            float proximity = 1f - Math.Min(1f, distance / Math.Max(0.01f, viewRange));
            float distanceFactor = 0.35f + 0.65f * proximity;
            return BaseSuspicionRate * distanceFactor * visibility * suspiciousness * vigilance;
        }

        // ------------------------------------------------------------------ Consequences

        public static int CountContraband(InventoryModel inventory)
        {
            int count = 0;
            foreach (ItemStack stack in inventory.Slots)
            {
                if (!stack.IsEmpty && stack.Item.IsContraband)
                {
                    count += stack.Count;
                }
            }
            return count;
        }

        /// <summary>Removes every contraband stack. Returns what was taken.</summary>
        public static List<ItemStack> ConfiscateContraband(InventoryModel inventory)
        {
            var taken = new List<ItemStack>();
            for (int i = 0; i < inventory.Capacity; i++)
            {
                ItemStack stack = inventory.Slots[i];
                if (!stack.IsEmpty && stack.Item.IsContraband)
                {
                    taken.Add(inventory.TakeSlot(i));
                }
            }
            return taken;
        }

        /// <summary>Fine for being caught. Never more than the player has, so the game can't become unwinnable.</summary>
        public static int Fine(int confiscatedUnits, int balance) => Math.Max(0, Math.Min(balance, 15 + 5 * Math.Max(0, confiscatedUnits)));
    }
}
