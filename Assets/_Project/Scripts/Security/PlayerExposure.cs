using UnityEngine;
using VentsAndVending.Inventory;
using VentsAndVending.Player;

namespace VentsAndVending.Security
{
    /// <summary>How visible and how suspicious the player currently is, for security units to read.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerExposure : MonoBehaviour
    {
        private PlayerStateController _state;
        private VentCrawler _vent;
        private InventoryComponent _inventory;
        private Transform _eye;

        /// <summary>Set while a deal is happening (sale panel open).</summary>
        public bool IsDealing { get; set; }

        public Vector3 EyePoint => _eye != null ? _eye.position : transform.position + Vector3.up * 1.6f;
        public Vector3 TorsoPoint => transform.position + Vector3.up * 0.8f;
        public bool InVent => _vent != null && _vent.InVent;
        public int ContrabandUnits => _inventory != null ? SecurityRules.CountContraband(_inventory.Model) : 0;
        public InventoryComponent Inventory => _inventory;

        public float Visibility => SecurityRules.Visibility(_state != null ? _state.Posture : PlayerMovementState.Normal, InVent);

        public float Suspiciousness => SecurityRules.Suspiciousness(ContrabandUnits, IsDealing,
            _state != null && _state.Current == PlayerMovementState.Sprinting);

        private void Awake()
        {
            _state = GetComponent<PlayerStateController>();
            _vent = GetComponent<VentCrawler>();
            _inventory = GetComponent<InventoryComponent>();
            Camera cam = GetComponentInChildren<Camera>();
            _eye = cam != null ? cam.transform : null;
        }
    }
}
