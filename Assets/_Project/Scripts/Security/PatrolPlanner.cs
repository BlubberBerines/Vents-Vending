using System;
using System.Collections.Generic;

namespace VentsAndVending.Security
{
    /// <summary>
    /// Decides where a patrol drone goes next (GDD Part 11; TunnelNetwork.md §7 "readable patrol cycle"): a main loop
    /// players can learn, plus optional detours that branch off a main waypoint with a chance and rejoin at the next
    /// one. Never chains detours, so the loop always reasserts itself. Pure logic, seeded RNG for tests.
    /// </summary>
    public sealed class PatrolPlanner
    {
        public readonly struct Detour
        {
            public readonly int FromMain;
            public readonly int Length;
            public readonly float Chance;

            public Detour(int fromMain, int length, float chance)
            {
                FromMain = fromMain;
                Length = Math.Max(0, length);
                Chance = Math.Max(0f, Math.Min(1f, chance));
            }
        }

        public readonly struct Waypoint
        {
            /// <summary>-1 for a main-loop point, otherwise the detour index.</summary>
            public readonly int Detour;
            /// <summary>Main-loop index, or the point index within the detour.</summary>
            public readonly int Index;

            public Waypoint(int detour, int index)
            {
                Detour = detour;
                Index = index;
            }

            public bool IsMain => Detour < 0;
        }

        private readonly int _mainCount;
        private readonly List<Detour> _detours;

        public PatrolPlanner(int mainCount, IEnumerable<Detour> detours = null)
        {
            _mainCount = Math.Max(1, mainCount);
            _detours = detours != null ? new List<Detour>(detours) : new List<Detour>();
            Current = new Waypoint(-1, 0);
        }

        public Waypoint Current { get; private set; }

        /// <summary>Call when the drone reaches <see cref="Current"/>; returns the next waypoint.</summary>
        public Waypoint Advance(Random rng)
        {
            if (!Current.IsMain)
            {
                Detour d = _detours[Current.Detour];
                int next = Current.Index + 1;
                Current = next < d.Length
                    ? new Waypoint(Current.Detour, next)
                    : new Waypoint(-1, (d.FromMain + 1) % _mainCount); // rejoin the loop after the branch point
                return Current;
            }

            for (int i = 0; i < _detours.Count; i++)
            {
                Detour d = _detours[i];
                if (d.FromMain == Current.Index && d.Length > 0 && rng.NextDouble() < d.Chance)
                {
                    Current = new Waypoint(i, 0);
                    return Current;
                }
            }

            Current = new Waypoint(-1, (Current.Index + 1) % _mainCount);
            return Current;
        }

        /// <summary>Back on patrol after a chase/search: head for the given main point (usually the nearest).</summary>
        public void ResumeAt(int mainIndex)
        {
            Current = new Waypoint(-1, ((mainIndex % _mainCount) + _mainCount) % _mainCount);
        }
    }
}
