using UnityEngine;
using VentsAndVending.Interaction;

namespace VentsAndVending.World
{
    /// <summary>
    /// Full-size sliding door. Automatic doors sense proximity; manual doors open on interaction and close after
    /// a clear-doorway delay. Both hold open for the player in the doorway. Leaves carry their own colliders. Not saved.
    /// </summary>
    public sealed class SlidingDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform[] _leaves = new Transform[0];
        [Tooltip("Local offset of each leaf when fully open (same order as Leaves).")]
        [SerializeField] private Vector3[] _slideOffsets = new Vector3[0];
        [SerializeField] private float _openSeconds = 0.6f;
        [SerializeField] private float _closeSeconds = .9f;
        [SerializeField] private float _unsealDepth = .035f;
        [SerializeField] private AudioClip _latchSound;
        [SerializeField] private float _senseRadius = 2.2f;
        [SerializeField] private bool _automatic = true;
        [SerializeField] private bool _securedOpen;
        [SerializeField, Min(.5f)] private float _holdOpenSeconds = 4f;
        private float _manualTimer;
        internal void ConfigureManual(float holdOpenSeconds = 4f)
        { _automatic=false;_holdOpenSeconds=Mathf.Max(.5f,holdOpenSeconds); }
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
            if(_securedOpen)
            {
                OpenAmount=1;
                for(int i=0;i<_leaves.Length && i<_slideOffsets.Length;i++)if(_leaves[i]!=null)_leaves[i].localPosition=_closedLocal[i]+_slideOffsets[i]+Vector3.forward*_unsealDepth;
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

            if (!_automatic)
            {
                if(occupied && OpenAmount>0) _manualTimer=_holdOpenSeconds;
                else _manualTimer=Mathf.Max(0,_manualTimer-Time.deltaTime);
            }
            bool open = _automatic ? SlidingDoorLogic.WantsOpen(_locked, near, occupied)
                : !_locked && (_securedOpen || _manualTimer>0 || (occupied && OpenAmount>0));
            if (open != _wasOpen)
            {
                Sfx.PlayAt(open ? _openSound : _closeSound, transform.position, open ? .7f : .35f);
                _wasOpen = open;
            }

            float previous = OpenAmount;
            OpenAmount = SlidingDoorLogic.Step(OpenAmount, open, open ? _openSeconds : _closeSeconds, Time.deltaTime);
            float eased = SlidingDoorLogic.LeafTravel(OpenAmount);
            float unseal = Mathf.SmoothStep(0f,1f,OpenAmount/.14f) * _unsealDepth;
            if(previous>0 && OpenAmount==0) Sfx.PlayAt(_latchSound,transform.position,.75f);
            for (int i = 0; i < _leaves.Length && i < _slideOffsets.Length; i++)
            {
                if (_leaves[i] != null)
                {
                    _leaves[i].localPosition = _closedLocal[i] + _slideOffsets[i] * eased + Vector3.forward * unseal;
                }
            }
        }

        public string GetPrompt(InteractionController interactor) => _locked ? "Inspect door" : _automatic || _securedOpen ? null : OpenAmount>.5f ? "Keep door open" : "Open door";

        public void Interact(InteractionController interactor)
        {
            if (_locked)
            {
                interactor.Notify(_lockedMessage);
            }
            else if(!_automatic) _manualTimer=_holdOpenSeconds;
        }
    }
}
