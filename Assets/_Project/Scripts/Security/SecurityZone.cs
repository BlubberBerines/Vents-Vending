using UnityEngine;

namespace VentsAndVending.Security
{
    public enum SecurityZoneKind
    {
        /// <summary>Somewhere the player plainly shouldn't be: being seen there is suspicious even empty-handed.</summary>
        Restricted,
        /// <summary>A reliable hiding spot: drones cannot see the player while they're inside.</summary>
        Hideout
    }

    /// <summary>
    /// Trigger volume that changes how security reads the player (see <see cref="PlayerExposure"/>). A restricted zone
    /// can carry an escort exit: a catch inside it walks the player out there, so a one-way area can't trap them.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SecurityZone : MonoBehaviour
    {
        [SerializeField] private SecurityZoneKind _kind = SecurityZoneKind.Restricted;
        [SerializeField] private string _zoneId = "zone";
        [SerializeField] private Transform _escortExit;
        [SerializeField] private string _escortMessage = "Escorted off the premises.";

        public SecurityZoneKind Kind => _kind;
        public string ZoneId => _zoneId;
        public Transform EscortExit => _escortExit;
        public string EscortMessage => _escortMessage;

        internal void Configure(SecurityZoneKind kind, string id, Transform escortExit)
        {
            _kind = kind;
            _zoneId = id;
            _escortExit = escortExit;
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                return;
            }
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = _kind == SecurityZoneKind.Restricted ? new Color(1f, 0.25f, 0.2f, 0.12f) : new Color(0.3f, 1f, 0.5f, 0.15f);
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}
