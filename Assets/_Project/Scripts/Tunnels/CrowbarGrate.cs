using System.Collections;
using UnityEngine;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;

namespace VentsAndVending.Tunnels
{
    /// <summary>
    /// A rusted floor grate that only a crowbar shifts (underpass design §4). Prying takes a moment and makes a racket;
    /// afterwards the grate leans against the wall for good and the hole is a one-way drop. Open state is a saved switch.
    /// </summary>
    public sealed class CrowbarGrate : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _gateId = TunnelIds.GrateGate;
        [SerializeField] private ItemDefinition _tool;
        [SerializeField] private Transform _panel;
        [Tooltip("Walkable, line-of-sight-blocking cover over the hole; disabled once open.")]
        [SerializeField] private Collider _blocker;
        [SerializeField] private AudioClip _pry;
        [SerializeField] private float _pryTime = 1.2f;
        [Tooltip("Where the panel rests once pried off (local to this object).")]
        [SerializeField] private Vector3 _restPosition = new Vector3(0.85f, 0.45f, 0f);
        [SerializeField] private Vector3 _restEuler = new Vector3(0f, 0f, 72f);

        private Vector3 _closedPosition;
        private Quaternion _closedRotation;
        private bool _prying;

        public bool IsOpen => TunnelManager.Instance != null && TunnelManager.Instance.GetSwitch(_gateId, false);

        internal void Configure(string gateId, ItemDefinition tool, Transform panel, Collider blocker, AudioClip pry)
        {
            _gateId = gateId;
            _tool = tool;
            _panel = panel;
            _blocker = blocker;
            _pry = pry;
        }

        private void Awake()
        {
            if (_panel != null)
            {
                _closedPosition = _panel.localPosition;
                _closedRotation = _panel.localRotation;
            }
        }

        private void Start()
        {
            if (TunnelManager.Instance != null)
            {
                TunnelManager.Instance.Restored += Apply;
            }
            Apply();
        }

        private void OnDestroy()
        {
            if (TunnelManager.Instance != null)
            {
                TunnelManager.Instance.Restored -= Apply;
            }
        }

        private bool HasTool(InteractionController interactor) =>
            _tool != null && interactor.Inventory != null && interactor.Inventory.Model.Count(_tool) > 0;

        public string GetPrompt(InteractionController interactor)
        {
            if (IsOpen || _prying)
            {
                return null;
            }
            return HasTool(interactor) ? "Pry open grate" : "Inspect grate";
        }

        public void Interact(InteractionController interactor)
        {
            if (IsOpen || _prying || TunnelManager.Instance == null)
            {
                return;
            }
            if (!HasTool(interactor))
            {
                interactor.Notify("Rusted solid. You'd need something to lever it.");
                return;
            }
            StartCoroutine(Pry(interactor));
        }

        private IEnumerator Pry(InteractionController interactor)
        {
            _prying = true;
            VentsAndVending.World.Sfx.PlayAt(_pry, transform.position, 1f, 1f, 22f);
            float t = 0f;
            while (t < _pryTime)
            {
                t += Time.deltaTime;
                if (_panel != null)
                {
                    float wiggle = Mathf.Sin(t * 38f) * 2.5f * (t / _pryTime);
                    _panel.localRotation = _closedRotation * Quaternion.Euler(wiggle, 0f, wiggle * 0.6f);
                    _panel.localPosition = _closedPosition + Vector3.up * 0.02f * (t / _pryTime);
                }
                yield return null;
            }
            TunnelManager.Instance.SetSwitch(_gateId, true, false);
            _prying = false;
            Apply();
            interactor.Notify("The grate gives. It's a long way down.");
        }

        private void Apply()
        {
            bool open = IsOpen;
            if (_blocker != null)
            {
                _blocker.enabled = !open;
            }
            if (_panel != null)
            {
                _panel.localPosition = open ? _restPosition : _closedPosition;
                _panel.localRotation = open ? Quaternion.Euler(_restEuler) : _closedRotation;
            }
        }
    }
}
