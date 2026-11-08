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

        private float _yaw;

        public bool IsOpen => TunnelManager.Instance != null && TunnelManager.Instance.GetSwitch(_gateId, false);

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
            return OnOpenableSide(interactor) ? "Lift the latch" : "Locked";
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
                return;
            }
            TunnelManager.Instance.SetSwitch(_gateId, true, false);
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
