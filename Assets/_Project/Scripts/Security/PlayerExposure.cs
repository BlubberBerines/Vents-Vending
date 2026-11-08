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

        /// <summary>The restricted zone the player is standing in, if any.</summary>
        public SecurityZone RestrictedZone { get; private set; }

        /// <summary>Inside a hiding spot: invisible to security.</summary>
        public bool IsHidden { get; private set; }

        public float Visibility => SecurityRules.Visibility(_state != null ? _state.Posture : PlayerMovementState.Normal, InVent, IsHidden);

        public float Suspiciousness => SecurityRules.Suspiciousness(ContrabandUnits, IsDealing,
            _state != null && _state.Current == PlayerMovementState.Sprinting, RestrictedZone != null);

        private readonly Collider[] _zoneHits = new Collider[8];

        private void Update()
        {
            // Probe at the torso, the same way VentCrawler finds vent zones (triggers + CharacterController are unreliable).
            int count = Physics.OverlapSphereNonAlloc(TorsoPoint, 0.25f, _zoneHits, ~0, QueryTriggerInteraction.Collide);
            SecurityZone restricted = null;
            bool hidden = false;
            for (int i = 0; i < count; i++)
            {
                SecurityZone zone = _zoneHits[i].GetComponent<SecurityZone>();
                if (zone == null)
                {
                    continue;
                }
                if (zone.Kind == SecurityZoneKind.Hideout)
                {
                    hidden = true;
                }
                else if (restricted == null)
                {
                    restricted = zone;
                }
            }
            RestrictedZone = restricted;
            IsHidden = hidden;
        }

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
