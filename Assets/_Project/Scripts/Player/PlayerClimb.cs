using System.Collections;
using UnityEngine;

namespace VentsAndVending.Player
{
    /// <summary>
    /// Mantle onto crates and ledges, and up into head-height vent mouths (station layers spec §6.1). Press Jump while
    /// walking into a climbable edge: the player pulls up in ~0.5 s, already in the posture the space on top allows
    /// (crawling for a vent mouth), so the capsule never pokes into a duct ceiling. Rules: <see cref="ClimbLogic"/>;
    /// world checks: <see cref="ClimbProbe"/>. Interrupted climbs (teleport, disable) are cancelled cleanly.
    /// </summary>
    [DefaultExecutionOrder(-45)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(PlayerStateController), typeof(PlayerInputHandler))]
    public sealed class PlayerClimb : MonoBehaviour
    {
        [SerializeField] private LayerMask _climbMask = ~0;

        private CharacterController _controller;
        private PlayerStateController _state;
        private PlayerInputHandler _input;
        private Coroutine _running;

        public bool IsClimbing { get; private set; }
        public PlayerMovementState ClimbPosture { get; private set; } = PlayerMovementState.Normal;

        /// <summary>True on the frame a Jump press started a climb, so <see cref="PlayerMovement"/> doesn't also jump.</summary>
        public bool JumpConsumedThisFrame { get; private set; }

        private CharacterController Controller => _controller != null ? _controller : _controller = GetComponent<CharacterController>();

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _state = GetComponent<PlayerStateController>();
            _input = GetComponent<PlayerInputHandler>();
        }

        private void OnDisable() => Cancel();

        private void OnDestroy() => Cancel();

        private void Update()
        {
            JumpConsumedThisFrame = false;
            if (IsClimbing || !_input.JumpPressed || _input.Move.y < 0.3f)
            {
                return;
            }

            if (ClimbLogic.CanStartClimb(_state.Current, Controller.isGrounded, _state.IsInteracting) && TryClimb())
            {
                JumpConsumedThisFrame = true;
            }
        }

        /// <summary>Looks for a climbable edge straight ahead and, if there is one, starts the climb.</summary>
        public bool TryClimb()
        {
            if (IsClimbing || !isActiveAndEnabled)
            {
                return false;
            }

            if (!ClimbProbe.TryFind(transform.position, transform.forward, Controller.radius, _state.Tuning, _state.Posture,
                                    _climbMask, transform, out ClimbPlan plan))
            {
                return false;
            }

            _running = StartCoroutine(Climb(plan));
            return true;
        }

        /// <summary>Stops a climb in progress and hands control back (teleports, disabling, destruction).</summary>
        public void Cancel()
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }

            if (Controller != null)
            {
                Controller.enabled = true;
            }

            IsClimbing = false;
            JumpConsumedThisFrame = false;
        }

        private IEnumerator Climb(ClimbPlan plan)
        {
            IsClimbing = true;
            ClimbPosture = plan.Posture;
            Controller.enabled = false;

            Vector3 start = transform.position;
            Vector3 up = plan.Rise;   // straight up, sidestepping into line with an opening if needed
            float t = 0f;
            while (t < plan.Duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / plan.Duration);
                transform.position = k < 0.6f
                    ? Vector3.Lerp(start, up, Mathf.SmoothStep(0f, 1f, k / 0.6f))
                    : Vector3.Lerp(up, plan.Target, Mathf.SmoothStep(0f, 1f, (k - 0.6f) / 0.4f));
                yield return null;
            }

            transform.position = plan.Target;
            // Keep the forced posture until the capsule has reached its final size, then hand back control.
            float targetHeight = _state.Tuning.CapsuleHeightFor(plan.Posture);
            for (float w = 0f; w < 0.5f && !Mathf.Approximately(Controller.height, targetHeight); w += Time.deltaTime)
            {
                yield return null;
            }

            _running = null;
            Controller.enabled = true;
            IsClimbing = false;
        }
    }
}
