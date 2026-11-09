using NUnit.Framework;
using VentsAndVending.World;

namespace VentsAndVending.Tests
{
    /// <summary>Full-size sliding door rules (den door, staff doors, sector bulkheads — station layers spec §6.3).</summary>
    public class SlidingDoorTests
    {
        [Test]
        public void Near_Opens() => Assert.IsTrue(SlidingDoorLogic.WantsOpen(false, true, false));

        [Test]
        public void Away_Closes() => Assert.IsFalse(SlidingDoorLogic.WantsOpen(false, false, false));

        [Test]
        public void Occupied_StaysOpen() => Assert.IsTrue(SlidingDoorLogic.WantsOpen(false, false, true));

        [Test]
        public void Locked_NeverOpens() => Assert.IsFalse(SlidingDoorLogic.WantsOpen(true, true, true));

        [Test]
        public void Step_ReachesOpenInOpenSeconds()
        {
            float a = 0f;
            for (int i = 0; i < 60; i++)
            {
                a = SlidingDoorLogic.Step(a, true, 0.6f, 0.01f);
            }

            Assert.AreEqual(1f, a, 1e-4);
        }

        [Test]
        public void Step_HalfwayAfterHalfTime()
        {
            Assert.AreEqual(0.5f, SlidingDoorLogic.Step(0f, true, 0.6f, 0.3f), 1e-4);
            Assert.AreEqual(0.5f, SlidingDoorLogic.Step(1f, false, 0.6f, 0.3f), 1e-4);
        }

        [Test]
        public void Step_Clamped()
        {
            Assert.AreEqual(0f, SlidingDoorLogic.Step(0f, false, 0.6f, 1f));
            Assert.AreEqual(1f, SlidingDoorLogic.Step(1f, true, 0.6f, 1f));
        }
    }
}
