using UnityEngine;

namespace VentsAndVending.Tunnels
{
    /// <summary>Generous trigger box marking a tunnel room for discovery (a room may use several volumes).</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RoomVolume : MonoBehaviour
    {
        [SerializeField] private VentRoomDefinition _room;

        public VentRoomDefinition Room => _room;

        internal void Configure(VentRoomDefinition room) => _room = room;

        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && TunnelManager.Instance != null)
            {
                TunnelManager.Instance.Enter(_room);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && TunnelManager.Instance != null)
            {
                TunnelManager.Instance.Exit(_room);
            }
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                return;
            }
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.08f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
