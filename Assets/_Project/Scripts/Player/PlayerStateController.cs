using System;
using UnityEngine;

namespace VentsAndVending.Player
{
    /// <summary>
    /// Owns the player's movement state and tuning. Other player components read from here.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputHandler))]
    public sealed class PlayerStateController : MonoBehaviour
    {
        [SerializeField] private PlayerTuning _tuning = new PlayerTuning();

        private PlayerInputHandler _input;
        private PlayerCrouch _crouch;
        private VentCrawler _ventCrawler;
        private PlayerClimb _climb;

        public PlayerTuning Tuning => _tuning;

        /// <summary>Gameplay state (may be Interacting).</summary>
        public PlayerMovementState Current { get; private set; } = PlayerMovementState.Normal;

        /// <summary>Body posture — never Interacting. Drives capsule and camera height.</summary>
        public PlayerMovementState Posture { get; private set; } = PlayerMovementState.Normal;

        /// <summary>Set by the interaction system while the player is operating something.</summary>
        public bool IsInteracting { get; set; }

        /// <summary>(previous, next)</summary>
        public event Action<PlayerMovementState, PlayerMovementState> StateChanged;

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            _crouch = GetComponent<PlayerCrouch>();
            _ventCrawler = GetComponent<VentCrawler>();
            _climb = GetComponent<PlayerClimb>();
        }

        private void Update()
        {
            var input = new PlayerStateInput
            {
                WantsSprint = _input.SprintHeld,
                WantsCrouch = _input.CrouchRequested,
                WantsCrawl = _input.CrawlRequested,
                IsMoving = _input.Move.sqrMagnitude > 0.01f,
                InVent = _ventCrawler != null && _ventCrawler.InVent,
                IsInteracting = IsInteracting,
                CanStand = _crouch == null || _crouch.CanFit(_tuning.NormalCapsuleHeight),
                CanCrouch = _crouch == null || _crouch.CanFit(_tuning.CrouchCapsuleHeight),
                IsClimbing = _climb != null && _climb.IsClimbing,
                ClimbPosture = _climb != null ? _climb.ClimbPosture : PlayerMovementState.Normal
            };

            Posture = PlayerStateLogic.ResolvePosture(input);
            PlayerMovementState next = PlayerStateLogic.Resolve(input);
            if (next != Current)
            {
                PlayerMovementState previous = Current;
                Current = next;
                StateChanged?.Invoke(previous, next);
            }
        }
    }
}
