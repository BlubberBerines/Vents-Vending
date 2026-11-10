using UnityEngine;

namespace VentsAndVending.Player
{
    /// <summary>
    /// Resizes the CharacterController and moves the camera between standing, crouching and crawling heights,
    /// and answers headroom queries.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(PlayerStateController))]
    public sealed class PlayerCrouch : MonoBehaviour
    {
        [Tooltip("The transform that holds the camera (moved vertically). Auto-detected if empty.")]
        [SerializeField] private Transform _cameraRoot;
        [SerializeField] private LayerMask _obstructionMask = ~0;

        private readonly Collider[] _overlaps = new Collider[16];
        private CharacterController _controller;
        private PlayerStateController _state;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _state = GetComponent<PlayerStateController>();
            if (_cameraRoot == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    _cameraRoot = cam.transform.parent == transform ? cam.transform : cam.transform.parent;
                }
            }
        }

        /// <summary>True if the capsule could grow to <paramref name="height"/> without hitting anything.</summary>
        public bool CanFit(float height)
        {
            if (_controller == null || height <= _controller.height + 0.001f)
            {
                return true;
            }

            return HasClearance(height);
        }

        /// <summary>Resize before re-enabling the controller after loading/teleporting into a low passage.</summary>
        public void FitAfterTeleport()
        {
            var tuning=_state.Tuning;
            float height=HasClearance(tuning.NormalCapsuleHeight)?tuning.NormalCapsuleHeight:
                HasClearance(tuning.CrouchCapsuleHeight)?tuning.CrouchCapsuleHeight:tuning.CrawlCapsuleHeight;
            _controller.height=height;
            _controller.center=new Vector3(0,height*.5f,0);
            if(_cameraRoot!=null)
            {
                var position=_cameraRoot.localPosition;
                position.y=height==tuning.NormalCapsuleHeight?tuning.NormalCameraHeight:
                    height==tuning.CrouchCapsuleHeight?tuning.CrouchCameraHeight:tuning.CrawlCameraHeight;
                _cameraRoot.localPosition=position;
            }
        }

        private bool HasClearance(float height)
        {

            float radius = _controller.radius * 0.95f;
            Vector3 up = transform.up;
            Vector3 bottom = transform.position + up * (_controller.radius + 0.1f);
            Vector3 top = transform.position + up * Mathf.Max(height - _controller.radius, _controller.radius + 0.1f);

            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, _overlaps, _obstructionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hit = _overlaps[i];
                if (hit != _controller && !hit.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }

        private void Update()
        {
            PlayerTuning tuning = _state.Tuning;
            float step = tuning.StanceTransitionSpeed * Time.deltaTime;

            float targetHeight = tuning.CapsuleHeightFor(_state.Posture);
            if (!Mathf.Approximately(_controller.height, targetHeight))
            {
                float height = Mathf.MoveTowards(_controller.height, targetHeight, step);
                _controller.height = height;
                _controller.center = new Vector3(0f, height * 0.5f, 0f);
            }

            if (_cameraRoot != null)
            {
                Vector3 local = _cameraRoot.localPosition;
                local.y = Mathf.MoveTowards(local.y, tuning.CameraHeightFor(_state.Posture), step);
                _cameraRoot.localPosition = local;
            }
        }
    }
}
