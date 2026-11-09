using NUnit.Framework;
using VentsAndVending.Player;

namespace VentsAndVending.Tests
{
    public class PlayerTuningTests
    {
        [Test]
        public void Defaults_MatchGddStartingValues()
        {
            var t = new PlayerTuning();
            Assert.AreEqual(4.2f, t.WalkSpeed);
            Assert.AreEqual(6.2f, t.SprintSpeed);
            Assert.AreEqual(2.2f, t.CrouchSpeed);
            Assert.AreEqual(1.6f, t.CrawlSpeed);
            Assert.AreEqual(1.0f, t.JumpHeight);
            Assert.AreEqual(-20f, t.Gravity);
            Assert.AreEqual(2.5f, t.InteractionDistance);
            Assert.AreEqual(1.65f, t.NormalCameraHeight);
            Assert.AreEqual(1.05f, t.CrouchCameraHeight);
            Assert.AreEqual(0.55f, t.CrawlCameraHeight);
        }

        [Test]
        public void JumpVelocity_ReachesJumpHeight()
        {
            var t = new PlayerTuning();
            // v² = 2gh → peak height = v² / 2g
            float peak = t.JumpVelocity * t.JumpVelocity / (2f * 20f);
            Assert.AreEqual(t.JumpHeight, peak, 1e-4f);
        }

        [TestCase(PlayerMovementState.Normal, 4.2f)]
        [TestCase(PlayerMovementState.Sprinting, 6.2f)]
        [TestCase(PlayerMovementState.Crouching, 2.2f)]
        [TestCase(PlayerMovementState.Crawling, 1.6f)]
        [TestCase(PlayerMovementState.Interacting, 0f)]
        [TestCase(PlayerMovementState.Climbing, 0f)]
        public void SpeedFor_State(PlayerMovementState state, float expected)
        {
            Assert.AreEqual(expected, new PlayerTuning().SpeedFor(state));
        }

        [Test]
        public void CameraFitsInsideCapsule_ForEveryPosture()
        {
            var t = new PlayerTuning();
            foreach (PlayerMovementState s in System.Enum.GetValues(typeof(PlayerMovementState)))
            {
                Assert.Less(t.CameraHeightFor(s), t.CapsuleHeightFor(s), s.ToString());
            }
        }
    }
}
