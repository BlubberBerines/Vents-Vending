using UnityEngine;
using VentsAndVending.Interaction;

namespace VentsAndVending.World
{
    /// <summary>
    /// Automatic full-size sliding door: the den door, staff doors, and (locked) sector bulkheads
    /// (station layers spec §6.3). Opens when the player comes near, stays open while they stand in the doorway, closes
    /// behind them. Leaves slide by their own offsets; their colliders move with them. Not saved.
    /// </summary>
    public sealed class SlidingDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform[] _leaves = new Transform[0];
        [Tooltip("Local offset of each leaf when fully open (same order as Leaves).")]
        [SerializeField] private Vector3[] _slideOffsets = new Vector3[0];
        [SerializeField] private float _openSeconds = 0.6f;
        [SerializeField] private float _senseRadius = 2.2f;
        [Tooltip("Doorway box (local, centred 1.2 m up): while the player is inside it the door won't close.")]
        [SerializeField] private Vector3 _doorwaySize = new Vector3(1.4f, 2.4f, 1.0f);
        [SerializeField] private bool _locked;
        [SerializeField] private string _lockedMessage = "Sealed. This sector is closed.";
        [SerializeField] private AudioClip _openSound;
        [SerializeField] private AudioClip _closeSound;

        private Vector3[] _closedLocal = new Vector3[0];
        private Transform _player;
        private bool _wasOpen;

        public bool Locked
        {
            get => _locked;
            set => _locked = value;
        }

        /// <summary>0 = shut, 1 = fully open.</summary>
        public float OpenAmount { get; private set; }

        internal void Configure(Transform[] leaves, Vector3[] slideOffsets)
        {
            _leaves = leaves;
            _slideOffsets = slideOffsets;
        }

        private void Awake()
        {
            _closedLocal = new Vector3[_leaves.Length];
            for (int i = 0; i < _leaves.Length; i++)
            {
                _closedLocal[i] = _leaves[i] != null ? _leaves[i].localPosition : Vector3.zero;
            }
        }

        private void Update()
        {
            if (_player == null)
            {
                GameObject p = GameObject.FindWithTag("Player");
                _player = p != null ? p.transform : null;
            }

            bool near = false, occupied = false;
            if (_player != null)
            {
                Vector3 local = transform.InverseTransformPoint(_player.position) - Vector3.up * 1.2f;
                near = (_player.position - transform.position).sqrMagnitude <= _senseRadius * _senseRadius;
                occupied = Mathf.Abs(local.x) <= _doorwaySize.x * 0.5f && Mathf.Abs(local.y) <= _doorwaySize.y * 0.5f &&
                           Mathf.Abs(local.z) <= _doorwaySize.z * 0.5f;
            }

            bool open = SlidingDoorLogic.WantsOpen(_locked, near, occupied);
            if (open != _wasOpen)
            {
                Sfx.PlayAt(open ? _openSound : _closeSound, transform.position, 0.8f);
                _wasOpen = open;
            }

            OpenAmount = SlidingDoorLogic.Step(OpenAmount, open, _openSeconds, Time.deltaTime);
            float eased = Mathf.SmoothStep(0f, 1f, OpenAmount);
            for (int i = 0; i < _leaves.Length && i < _slideOffsets.Length; i++)
            {
                if (_leaves[i] != null)
                {
                    _leaves[i].localPosition = _closedLocal[i] + _slideOffsets[i] * eased;
                }
            }
        }

        public string GetPrompt(InteractionController interactor) => _locked ? "Inspect door" : null;

        public void Interact(InteractionController interactor)
        {
            if (_locked)
            {
                interactor.Notify(_lockedMessage);
            }
        }
    }
}
