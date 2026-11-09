using UnityEngine;

namespace VentsAndVending.Player
{
    /// <summary>Where and how a climb goes: rise (sidestepping if needed) to <see cref="Rise"/>, then move to <see cref="Target"/>.</summary>
    public struct ClimbPlan
    {
        public float TopY;
        public Vector3 Rise;
        public Vector3 Target;
        public PlayerMovementState Posture;
        public float Duration;
    }

    /// <summary>
    /// Finds a climbable edge ahead of the player with physics queries and checks the whole move is safe
    /// (station layers spec §6.1 + final-review fixes): the face is a real wall-like face over level floor (not a
    /// stair or ramp), the top is level, the path up and over is clear (never through a wall), there is a real
    /// surface under the landing (not over a drop), and the capsule fits there in the resolved posture — sliding
    /// sideways up to 0.3 m to fit an opening such as a vent mouth.
    /// </summary>
    public static class ClimbProbe
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[16];
        private static readonly Collider[] Overlaps = new Collider[16];
        private static readonly float[] Offsets = { 0f, 0.1f, -0.1f, 0.2f, -0.2f, 0.3f, -0.3f };

        /// <summary>Why the last probe refused (for debugging and the tunnel debug overlay).</summary>
        public static string LastReject { get; private set; } = "";

        public static bool TryFind(Vector3 feet, Vector3 forward, float radius, PlayerTuning tuning,
                                   PlayerMovementState current, LayerMask mask, Transform self, out ClimbPlan plan)
        {
            plan = default;
            LastReject = "";
            Vector3 fwd = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);

            // 1. A wall-like face ahead, between knee and chest height.
            if (!SphereCast(feet + Vector3.up * 0.45f, radius * 0.6f, fwd, tuning.ClimbProbeDistance + radius, mask, self, out RaycastHit face) ||
                Mathf.Abs(face.normal.y) > 0.3f)
            {
                LastReject = "no wall-like face ahead";
                return false;
            }

            // 2. Level floor right in front of the face (rules out stairs: their lower steps are in the way).
            Vector3 before = face.point - fwd * 0.1f;
            if (!Ray(new Vector3(before.x, feet.y + 0.3f, before.z), Vector3.down, 0.45f, mask, self, out RaycastHit floor) ||
                Mathf.Abs(floor.point.y - feet.y) > 0.06f)
            {
                LastReject = "floor in front of the face is not level with the feet (stairs?)";
                return false;
            }

            // 3. A level ledge top just beyond the face (0.2 m in: a vent mouth's flat stub is only 0.3 m deep).
            Vector3 over = face.point + fwd * 0.2f;
            if (!Ray(new Vector3(over.x, feet.y + tuning.MaxClimbHeight + 0.1f, over.z), Vector3.down, tuning.MaxClimbHeight + 0.1f,
                     mask, self, out RaycastHit ledge) || ledge.normal.y < 0.9f)
            {
                LastReject = "no level ledge top";
                return false;
            }

            float topY = ledge.point.y;
            float ledgeHeight = topY - feet.y;
            Vector3 onTop = ledge.point + Vector3.up * 0.02f;
            float clearance = Ray(onTop, Vector3.up, 2f, mask, self, out RaycastHit roof) ? roof.distance : 2f;
            Vector3 along = onTop + Vector3.up * Mathf.Min(0.3f, clearance * 0.5f);
            float depth = Ray(along - fwd * 0.3f, fwd, 1.8f, mask, self, out RaycastHit end) ? end.distance : 1.8f;
            if (!ClimbLogic.TryResolve(ledgeHeight, clearance, depth, tuning, out PlayerMovementState cap))
            {
                LastReject = $"rules: ledge {ledgeHeight:F2} clearance {clearance:F2} depth {depth:F2}";
                return false;
            }

            cap = Lower(cap, current);   // a crouched player stays crouched on top

            // 4. The rise straight up must be clear.
            if (SphereCast(feet + Vector3.up * (radius + 0.05f), radius * 0.9f, Vector3.up, Mathf.Max(0f, ledgeHeight - 0.05f), mask, self, out RaycastHit riseHit))
            {
                LastReject = $"rise blocked by {riseHit.collider.name}";
                return false;
            }

            Vector3 rise0 = new Vector3(feet.x, topY + radius + 0.02f, feet.z);
            Vector3 baseTarget = new Vector3(face.point.x, topY + 0.02f, face.point.z) + fwd * Mathf.Max(0.4f, radius + 0.1f);
            foreach (float off in Offsets)
            {
                Vector3 target = baseTarget + right * off;

                // 5. A real surface under the landing (not past the edge of a railing).
                if (!Ray(target + Vector3.up * 0.1f, Vector3.down, 0.2f, mask, self, out RaycastHit under) ||
                    Mathf.Abs(under.point.y - topY) > 0.12f)   // level-ish: allows the start of a duct ramp
                {
                    LastReject += $" | offset {off:F1}: no surface under the landing";
                    continue;
                }

                // 6. The path over the edge is clear (never through a wall or a closed door): sidestep at the top of the
                //    rise (before the wall plane), then straight over.
                Vector3 rise = rise0 + right * off;
                Vector3 to = new Vector3(target.x, rise.y, target.z);
                if (Blocked(rise0, rise, topY, mask, self, out RaycastHit pathHit) || Blocked(rise, to, topY, mask, self, out pathHit))
                {
                    LastReject += $" | offset {off:F1}: path blocked by {pathHit.collider.name} at {pathHit.point}";
                    continue;
                }

                // 7. The capsule fits at the landing in the tallest posture allowed.
                for (PlayerMovementState p = cap; ; p = Lower(Next(p), p))
                {
                    if (Fits(target, radius, tuning.CapsuleHeightFor(p), mask, self))
                    {
                        plan = new ClimbPlan { TopY = topY, Rise = new Vector3(rise.x, topY + 0.02f, rise.z), Target = target, Posture = p, Duration = ClimbLogic.Duration(ledgeHeight, tuning) };
                        return true;
                    }

                    if (p == PlayerMovementState.Crawling)
                    {
                        LastReject += $" | offset {off:F1}: capsule doesn't fit at {target}";
                        break;
                    }
                }
            }

            return false;
        }

        private static int Rank(PlayerMovementState p) => p == PlayerMovementState.Crawling ? 0 : p == PlayerMovementState.Crouching ? 1 : 2;

        private static PlayerMovementState Lower(PlayerMovementState a, PlayerMovementState b) => Rank(a) <= Rank(b) ? Norm(a) : Norm(b);

        private static PlayerMovementState Norm(PlayerMovementState p) =>
            p == PlayerMovementState.Crawling || p == PlayerMovementState.Crouching ? p : PlayerMovementState.Normal;

        private static PlayerMovementState Next(PlayerMovementState p) =>
            p == PlayerMovementState.Crouching || p == PlayerMovementState.Crawling ? PlayerMovementState.Crawling : PlayerMovementState.Crouching;

        internal static bool Fits(Vector3 feet, float radius, float height, LayerMask mask, Transform self)
        {
            float r = radius * 0.95f;
            // bottom raised by a 0.2 m step allowance (CharacterController stepOffset is 0.3) (a duct ramp or a lip under the landing), top at full height
            Vector3 a = feet + Vector3.up * (r + 0.2f);
            Vector3 b = feet + Vector3.up * Mathf.Max(height - r, r + 0.2f);
            // a zero-length capsule gives false overlaps against triangle meshes (PhysX): use a sphere then
            int n = (b - a).sqrMagnitude < 1e-4f
                ? Physics.OverlapSphereNonAlloc(a, r, Overlaps, mask, QueryTriggerInteraction.Ignore)
                : Physics.OverlapCapsuleNonAlloc(a, b, r, Overlaps, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (!IsSelf(Overlaps[i], self))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>A fan of rays (3 across x 2 heights above the ledge top) along a horizontal path: catches any wall or
        /// door between the player and the landing, but not a duct floor ramping up beyond the ledge (the landing
        /// itself is checked by the capsule fit).</summary>
        private static bool Blocked(Vector3 from, Vector3 to, float topY, LayerMask mask, Transform self, out RaycastHit hit)
        {
            hit = default;
            Vector3 d = to - from;
            d.y = 0f;
            if (d.sqrMagnitude < 1e-6f)
            {
                return false;
            }

            Vector3 dir = d.normalized;
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            foreach (float h in new[] { 0.15f, 0.5f })
            {
                foreach (float s in new[] { -0.2f, 0f, 0.2f })
                {
                    Vector3 o = new Vector3(from.x, topY + h, from.z) + side * s;
                    if (Ray(o, dir, d.magnitude, mask, self, out hit))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsSelf(Collider c, Transform self) => self != null && c.transform.IsChildOf(self);

        private static bool SphereCast(Vector3 origin, float radius, Vector3 dir, float distance, LayerMask mask, Transform self, out RaycastHit best)
        {
            int n = Physics.SphereCastNonAlloc(origin, radius, dir, Hits, distance, mask, QueryTriggerInteraction.Ignore);
            return Nearest(n, self, out best);
        }

        private static bool Ray(Vector3 origin, Vector3 dir, float distance, LayerMask mask, Transform self, out RaycastHit best)
        {
            int n = Physics.RaycastNonAlloc(origin, dir, Hits, distance, mask, QueryTriggerInteraction.Ignore);
            return Nearest(n, self, out best);
        }

        private static bool Nearest(int count, Transform self, out RaycastHit best)
        {
            best = default;
            float min = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit h = Hits[i];
                if (IsSelf(h.collider, self) || h.distance <= 0f)
                {
                    continue;
                }

                if (h.distance < min)
                {
                    min = h.distance;
                    best = h;
                }
            }

            return min < float.MaxValue;
        }
    }
}
