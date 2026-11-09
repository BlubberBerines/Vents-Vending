using UnityEngine;

namespace VentsAndVending.Player
{
    /// <summary>
    /// Moves the CharacterController based on input and the current movement state.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(PlayerStateController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private const float GroundedStickVelocity = -2f;

        private CharacterController _controller;
        private PlayerInputHandler _input;
        private PlayerStateController _state;
        private PlayerClimb _climb;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;

        public Vector3 Velocity => _horizontalVelocity + Vector3.up * _verticalVelocity;
        public bool IsGrounded => _controller != null && _controller.isGrounded;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputHandler>();
            _state = GetComponent<PlayerStateController>();
            _climb = GetComponent<PlayerClimb>();
        }

        private void Update()
        {
            if (_climb != null && _climb.IsClimbing)
            {
                _horizontalVelocity = Vector3.zero;   // the climb owns the transform (controller disabled)
                _verticalVelocity = 0f;
                return;
            }

            float dt = Time.deltaTime;
            PlayerTuning tuning = _state.Tuning;
            PlayerMovementState state = _state.Current;
            bool grounded = _controller.isGrounded;

            Vector2 move = Vector2.ClampMagnitude(_input.Move, 1f);
            Vector3 wishVelocity = (transform.right * move.x + transform.forward * move.y) * tuning.SpeedFor(state);
            float acceleration = grounded ? tuning.Acceleration : tuning.Acceleration * tuning.AirControl;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, wishVelocity, acceleration * dt);

            if (grounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = GroundedStickVelocity;
            }

            bool climbed = _climb != null && _climb.JumpConsumedThisFrame;
            if (_input.JumpPressed && !climbed && PlayerStateLogic.CanJump(state, grounded))
            {
                _verticalVelocity = tuning.JumpVelocity;
            }

            _verticalVelocity += tuning.Gravity * dt;

            CollisionFlags flags = _controller.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
            {
                _verticalVelocity = 0f;
            }
        }

        /// <summary>Teleport safely (CharacterController must be disabled to set position).</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (_climb != null)
            {
                _climb.Cancel();   // a teleport (escort, load) ends any climb in progress
            }

            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _controller.enabled = true;
        }
    }
}
