namespace VentsAndVending.Player
{
    /// <summary>Everything the state machine needs to decide the player's state for one frame.</summary>
    public struct PlayerStateInput
    {
        public bool WantsSprint;
        public bool WantsCrouch;
        public bool WantsCrawl;
        public bool IsMoving;
        public bool InVent;
        public bool IsInteracting;
        /// <summary>There is room to stand at full height.</summary>
        public bool CanStand;
        /// <summary>There is room to crouch.</summary>
        public bool CanCrouch;

        public static PlayerStateInput Default => new PlayerStateInput { CanStand = true, CanCrouch = true };
    }

    /// <summary>
    /// Pure, engine-independent state rules so they can be unit-tested.
    /// </summary>
    public static class PlayerStateLogic
    {
        /// <summary>Body posture, ignoring interaction. Drives capsule and camera height.</summary>
        public static PlayerMovementState ResolvePosture(in PlayerStateInput input)
        {
            PlayerMovementState desired;
            if (input.InVent || input.WantsCrawl)
            {
                desired = PlayerMovementState.Crawling;
            }
            else if (input.WantsCrouch)
            {
                desired = PlayerMovementState.Crouching;
            }
            else if (input.WantsSprint && input.IsMoving)
            {
                desired = PlayerMovementState.Sprinting;
            }
            else
            {
                desired = PlayerMovementState.Normal;
            }

            // Headroom: never stand up into a ceiling.
            bool wantsStanding = desired == PlayerMovementState.Normal || desired == PlayerMovementState.Sprinting;
            if (wantsStanding && !input.CanStand)
            {
                desired = input.CanCrouch ? PlayerMovementState.Crouching : PlayerMovementState.Crawling;
            }
            else if (desired == PlayerMovementState.Crouching && !input.CanCrouch)
            {
                desired = PlayerMovementState.Crawling;
            }

            return desired;
        }

        /// <summary>Gameplay state. Interacting overrides movement but keeps the posture.</summary>
        public static PlayerMovementState Resolve(in PlayerStateInput input)
        {
            return input.IsInteracting ? PlayerMovementState.Interacting : ResolvePosture(input);
        }

        /// <summary>Jumping is only allowed on the ground while upright — never in vents (GDD §3.2).</summary>
        public static bool CanJump(PlayerMovementState state, bool grounded)
        {
            return grounded && (state == PlayerMovementState.Normal || state == PlayerMovementState.Sprinting);
        }
    }
}
