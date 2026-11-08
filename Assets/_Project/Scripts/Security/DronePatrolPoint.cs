using UnityEngine;

namespace VentsAndVending.Security
{
    /// <summary>
    /// What a patrol drone does at a waypoint: how long it hovers, how wide it sweeps its light, and what it pays
    /// attention to (a meet spot, a stall, a door). Lets patrols read as "checking places" instead of pacing a square.
    /// </summary>
    public sealed class DronePatrolPoint : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _wait = 1.2f;
        [SerializeField, Range(0f, 180f)] private float _sweep = 50f;
        [Tooltip("Optional: what the drone faces while it waits.")]
        [SerializeField] private Transform _lookAt;
        [SerializeField] private float _altitudeOffset;

        public float Wait => _wait;
        public float Sweep => _sweep;
        public Transform LookAt => _lookAt;
        public float AltitudeOffset => _altitudeOffset;

        internal void Configure(float wait, float sweep, Transform lookAt, float altitudeOffset = 0f)
        {
            _wait = wait;
            _sweep = sweep;
            _lookAt = lookAt;
            _altitudeOffset = altitudeOffset;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            if (_lookAt != null)
            {
                Gizmos.DrawLine(transform.position, _lookAt.position);
            }
        }
    }
}
