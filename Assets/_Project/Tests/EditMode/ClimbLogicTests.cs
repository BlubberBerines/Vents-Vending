using NUnit.Framework;
using VentsAndVending.Player;

namespace VentsAndVending.Tests
{
    /// <summary>Climb rules (Docs/superpowers/specs/2026-11-09-station-ring-layers-design.md §6.1).</summary>
    public class ClimbLogicTests
    {
        private static readonly PlayerTuning T = new PlayerTuning();

        [Test]
        public void CrateHeight_StandsOnTop()
        {
            Assert.IsTrue(ClimbLogic.TryResolve(1.0f, 3f, 1f, T, out var posture));
            Assert.AreEqual(PlayerMovementState.Normal, posture);
        }

        [Test]
        public void VentMouthLedge_ResolvesCrawl()
        {
            // standing on a 1.01 m crate, vent sill at 1.8 m, 0.85-0.9 m opening
            Assert.IsTrue(ClimbLogic.TryResolve(0.79f, 0.9f, 2f, T, out var posture));
            Assert.AreEqual(PlayerMovementState.Crawling, posture);
        }

        [Test]
        public void LowCeilingLedge_ResolvesCrouch()
        {
            Assert.IsTrue(ClimbLogic.TryResolve(0.8f, 1.4f, 1f, T, out var posture));
            Assert.AreEqual(PlayerMovementState.Crouching, posture);
        }

        [Test]
        public void TooHigh_Refused() => Assert.IsFalse(ClimbLogic.TryResolve(1.8f, 3f, 1f, T, out _)); // vent mouth from the floor

        [Test]
        public void TooLow_Refused() => Assert.IsFalse(ClimbLogic.TryResolve(0.2f, 3f, 1f, T, out _));

        [Test]
        public void NoClearance_Refused() => Assert.IsFalse(ClimbLogic.TryResolve(1.0f, 0.5f, 1f, T, out _));

        [Test]
        public void TooShallow_Refused() => Assert.IsFalse(ClimbLogic.TryResolve(1.0f, 3f, 0.3f, T, out _));

        [Test]
        public void Duration_InRange()
        {
            Assert.AreEqual(0.4f, ClimbLogic.Duration(0.2f, T), 1e-4);
            Assert.AreEqual(0.55f, ClimbLogic.Duration(1.0f, T), 1e-4);
            Assert.AreEqual(0.6f, ClimbLogic.Duration(5f, T), 1e-4);
        }

        [Test]
        public void CanStartClimb_Grounded_Upright() => Assert.IsTrue(ClimbLogic.CanStartClimb(PlayerMovementState.Normal, true, false));

        [Test]
        public void CanStartClimb_Crouched() => Assert.IsTrue(ClimbLogic.CanStartClimb(PlayerMovementState.Crouching, true, false));

        [Test]
        public void CanStartClimb_NotInAir() => Assert.IsFalse(ClimbLogic.CanStartClimb(PlayerMovementState.Normal, false, false));

        [Test]
        public void CanStartClimb_NotCrawlingOrInteracting()
        {
            Assert.IsFalse(ClimbLogic.CanStartClimb(PlayerMovementState.Crawling, true, false));
            Assert.IsFalse(ClimbLogic.CanStartClimb(PlayerMovementState.Normal, true, true));
        }
    }
}
