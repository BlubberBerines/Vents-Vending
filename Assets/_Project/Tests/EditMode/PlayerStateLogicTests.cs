using NUnit.Framework;
using VentsAndVending.Player;

namespace VentsAndVending.Tests
{
    public class PlayerStateLogicTests
    {
        private static PlayerStateInput Input() => PlayerStateInput.Default;

        [Test]
        public void Idle_IsNormal()
        {
            Assert.AreEqual(PlayerMovementState.Normal, PlayerStateLogic.Resolve(Input()));
        }

        [Test]
        public void SprintWhileMoving_IsSprinting()
        {
            var input = Input();
            input.WantsSprint = true;
            input.IsMoving = true;
            Assert.AreEqual(PlayerMovementState.Sprinting, PlayerStateLogic.Resolve(input));
        }

        [Test]
        public void SprintWhileStill_IsNormal()
        {
            var input = Input();
            input.WantsSprint = true;
            Assert.AreEqual(PlayerMovementState.Normal, PlayerStateLogic.Resolve(input));
        }

        [Test]
        public void CrouchBeatsSprint()
        {
            var input = Input();
            input.WantsSprint = true;
            input.IsMoving = true;
            input.WantsCrouch = true;
            Assert.AreEqual(PlayerMovementState.Crouching, PlayerStateLogic.Resolve(input));
        }

        [Test]
        public void InVent_ForcesCrawl_EvenWhenSprinting()
        {
            var input = Input();
            input.InVent = true;
            input.WantsSprint = true;
            input.IsMoving = true;
            Assert.AreEqual(PlayerMovementState.Crawling, PlayerStateLogic.Resolve(input));
        }

        [Test]
        public void NoHeadroomToStand_StaysCrouched()
        {
            var input = Input();
            input.CanStand = false;
            Assert.AreEqual(PlayerMovementState.Crouching, PlayerStateLogic.Resolve(input));
        }

        [Test]
        public void NoHeadroomToCrouch_StaysCrawling()
        {
            var input = Input();
            input.WantsCrouch = true;
            input.CanStand = false;
            input.CanCrouch = false;
            Assert.AreEqual(PlayerMovementState.Crawling, PlayerStateLogic.Resolve(input));
        }

        [Test]
        public void Interacting_OverridesState_ButKeepsPosture()
        {
            var input = Input();
            input.InVent = true;
            input.IsInteracting = true;
            Assert.AreEqual(PlayerMovementState.Interacting, PlayerStateLogic.Resolve(input));
            Assert.AreEqual(PlayerMovementState.Crawling, PlayerStateLogic.ResolvePosture(input));
        }

        [Test]
        public void Climbing_OverridesInteracting_AndForcesPosture()
        {
            var input = Input();
            input.IsClimbing = true;
            input.ClimbPosture = PlayerMovementState.Crawling;
            input.IsInteracting = true;
            Assert.AreEqual(PlayerMovementState.Climbing, PlayerStateLogic.Resolve(input));
            Assert.AreEqual(PlayerMovementState.Crawling, PlayerStateLogic.ResolvePosture(input));
        }

        [Test]
        public void CannotJump_WhileClimbing() => Assert.IsFalse(PlayerStateLogic.CanJump(PlayerMovementState.Climbing, true));

        [TestCase(PlayerMovementState.Normal, true, true)]
        [TestCase(PlayerMovementState.Sprinting, true, true)]
        [TestCase(PlayerMovementState.Normal, false, false)]
        [TestCase(PlayerMovementState.Crouching, true, false)]
        [TestCase(PlayerMovementState.Crawling, true, false)]
        [TestCase(PlayerMovementState.Interacting, true, false)]
        public void CanJump_OnlyUprightAndGrounded(PlayerMovementState state, bool grounded, bool expected)
        {
            Assert.AreEqual(expected, PlayerStateLogic.CanJump(state, grounded));
        }
    }
}
