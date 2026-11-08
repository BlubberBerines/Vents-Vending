using System;
using NUnit.Framework;
using VentsAndVending.Security;

namespace VentsAndVending.Tests.EditMode
{
    public sealed class PatrolPlannerTests
    {
        [Test]
        public void WithoutDetours_CyclesTheMainLoop()
        {
            var p = new PatrolPlanner(4);
            var rng = new Random(1);
            int[] seen = new int[8];
            for (int i = 0; i < 8; i++)
            {
                seen[i] = p.Advance(rng).Index;
            }
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 0, 1, 2, 3, 0 }, seen);
        }

        [Test]
        public void CertainDetour_VisitsItsPointsThenRejoinsAfterTheBranch()
        {
            var p = new PatrolPlanner(4, new[] { new PatrolPlanner.Detour(1, 2, 1f) });
            var rng = new Random(2);
            Assert.AreEqual(1, p.Advance(rng).Index);           // main 1
            PatrolPlanner.Waypoint a = p.Advance(rng);           // detour 0, point 0
            PatrolPlanner.Waypoint b = p.Advance(rng);           // detour 0, point 1
            PatrolPlanner.Waypoint c = p.Advance(rng);           // back to main 2
            Assert.IsFalse(a.IsMain);
            Assert.AreEqual(0, a.Index);
            Assert.AreEqual(1, b.Index);
            Assert.IsTrue(c.IsMain);
            Assert.AreEqual(2, c.Index);
        }

        [Test]
        public void ZeroChanceDetour_NeverTaken()
        {
            var p = new PatrolPlanner(3, new[] { new PatrolPlanner.Detour(0, 3, 0f) });
            var rng = new Random(3);
            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(p.Advance(rng).IsMain);
            }
        }

        [Test]
        public void Detours_NeverChain_AndAppearAtRoughlyTheirChance()
        {
            var p = new PatrolPlanner(2, new[] { new PatrolPlanner.Detour(0, 1, 0.3f), new PatrolPlanner.Detour(1, 1, 0.3f) });
            var rng = new Random(4);
            int detours = 0, branchVisits = 0;
            bool previousWasDetour = false;
            for (int i = 0; i < 4000; i++)
            {
                PatrolPlanner.Waypoint before = p.Current;
                PatrolPlanner.Waypoint w = p.Advance(rng);
                if (before.IsMain)
                {
                    branchVisits++;
                }
                if (!w.IsMain)
                {
                    Assert.IsFalse(previousWasDetour, "a detour must rejoin the main loop before another starts");
                    detours++;
                }
                previousWasDetour = !w.IsMain;
            }
            float rate = detours / (float)branchVisits;
            Assert.That(rate, Is.InRange(0.25f, 0.35f));
        }

        [Test]
        public void ResumeAt_WrapsAndContinuesFromThere()
        {
            var p = new PatrolPlanner(5);
            p.ResumeAt(7);
            Assert.AreEqual(2, p.Current.Index);
            Assert.AreEqual(3, p.Advance(new Random(5)).Index);
            p.ResumeAt(-1);
            Assert.AreEqual(4, p.Current.Index);
        }
    }
}
