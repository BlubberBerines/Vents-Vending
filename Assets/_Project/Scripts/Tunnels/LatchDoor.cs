using UnityEngine;
using VentsAndVending.Interaction;

namespace VentsAndVending.Tunnels
{
    /// <summary>
    /// A door that only unlatches from one side (Docs/Design/TunnelNetwork.md §6 "Plaza service hatch"): once opened it
    /// stays open and becomes a shortcut. Its state is a saved switch.
    /// </summary>
    public sealed class LatchDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _gateId = TunnelIds.StaffDoor;
        [SerializeField] private Transform _pivot;
        [SerializeField] private float _openYaw = 95f;
        [Tooltip("Local axis pointing to the side the latch can be opened from.")]
        [SerializeField] private Vector3 _openableSide = Vector3.right;
        [SerializeField] private string _lockedMessage = "Locked. STAFF ONLY — and the latch is on the other side.";
        [SerializeField] private AudioClip _sound;
        [Tooltip("0 = stays open for good (saved shortcut). Above 0 = a push-bar door: opens for now, swings shut this many seconds later once the player has moved away, and is never saved open.")]
        [SerializeField] private float _autoCloseSeconds;
        [SerializeField] private string _openPrompt = "Lift the latch";

        private float _yaw;
        private bool _tempOpen;
        private float _openTimer;
        private Transform _opener;

        private bool AutoClose => _autoCloseSeconds > 0f;

        public bool IsOpen => AutoClose ? _tempOpen : TunnelManager.Instance != null && TunnelManager.Instance.GetSwitch(_gateId, false);

        internal void Configure(string gateId, Transform pivot, float openYaw, Vector3 openableSide, string lockedMessage, string openPrompt, AudioClip sound, float autoCloseSeconds)
        {
            _gateId = gateId;
            _pivot = pivot;
            _openYaw = openYaw;
            _openableSide = openableSide;
            _lockedMessage = lockedMessage;
            _openPrompt = openPrompt;
            _sound = sound;
            _autoCloseSeconds = autoCloseSeconds;
        }

        private void Start()
        {
            if (TunnelManager.Instance != null)
            {
                TunnelManager.Instance.Restored += Snap;
            }
            Snap();
        }

        private void OnDestroy()
        {
            if (TunnelManager.Instance != null)
            {
                TunnelManager.Instance.Restored -= Snap;
            }
        }

        private void Snap()
        {
            _yaw = IsOpen ? _openYaw : 0f;
            Apply();
        }

        private void Update()
        {
            if (AutoClose && _tempOpen)
            {
                _openTimer += Time.deltaTime;
                bool clear = _opener == null || (_opener.position - transform.position).sqrMagnitude > 1.5f * 1.5f;
                if (_openTimer >= _autoCloseSeconds && clear)
                {
                    _tempOpen = false;
                    VentsAndVending.World.Sfx.PlayAt(_sound, transform.position, 0.8f, 1.1f);
                }
            }
            float target = IsOpen ? _openYaw : 0f;
            if (!Mathf.Approximately(_yaw, target))
            {
                _yaw = Mathf.MoveTowards(_yaw, target, 220f * Time.deltaTime);
                Apply();
            }
        }

        private bool OnOpenableSide(InteractionController interactor)
        {
            Vector3 toPlayer = interactor.transform.position - transform.position;
            return Vector3.Dot(transform.TransformDirection(_openableSide), toPlayer) > 0f;
        }

        public string GetPrompt(InteractionController interactor)
        {
            if (IsOpen)
            {
                return null;
            }
            return OnOpenableSide(interactor) ? _openPrompt : "Locked";
        }

        public void Interact(InteractionController interactor)
        {
            if (IsOpen || TunnelManager.Instance == null)
            {
                return;
            }
            if (!OnOpenableSide(interactor))
            {
                interactor.Notify(_lockedMessage);
                VentsAndVending.World.Sfx.PlayAt(_sound, transform.position, 0.5f, 1.35f);
                return;
            }
            if (AutoClose)
            {
                _tempOpen = true;
                _openTimer = 0f;
                _opener = interactor.transform;
                VentsAndVending.World.Sfx.PlayAt(_sound, transform.position, 1f, 0.9f);
                return;
            }
            TunnelManager.Instance.SetSwitch(_gateId, true, false);
            VentsAndVending.World.Sfx.PlayAt(_sound, transform.position, 1f, 0.9f);
            interactor.Notify("Shortcut opened");
        }

        private void Apply()
        {
            if (_pivot != null)
            {
                _pivot.localRotation = Quaternion.Euler(0f, _yaw, 0f);
            }
        }
    }
}
