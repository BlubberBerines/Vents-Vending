using UnityEngine;
using VentsAndVending.World;

namespace VentsAndVending.Player
{
    /// <summary>
    /// Detects when the player is inside a <see cref="VentZone"/>. While in a vent the player is forced to crawl
    /// and cannot sprint or jump (GDD §3.2, §4.3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VentCrawler : MonoBehaviour
    {
        [Tooltip("Height above the feet of the vent probe sphere.")]
        [SerializeField] private float _probeHeight = 0.3f;
        [SerializeField] private float _probeRadius = 0.25f;

        private readonly Collider[] _hits = new Collider[8];
        private CharacterController _body;

        private void Awake()=>_body=GetComponent<CharacterController>();

        public bool InVent { get; private set; }
        public VentZone CurrentZone { get; private set; }

        private void Update()
        {
            Vector3 probe = transform.position + transform.up * _probeHeight;
            // Detect the mouth before the capsule's leading edge hits its low header.
            // A smaller probe otherwise leaves the player standing outside a zone they cannot enter.
            float radius=Mathf.Max(_probeRadius,_body!=null?_body.radius+.12f:_probeRadius);
            int count = Physics.OverlapSphereNonAlloc(probe, radius, _hits, ~0, QueryTriggerInteraction.Collide);

            VentZone zone = null;
            for (int i = 0; i < count && zone == null; i++)
            {
                zone = _hits[i].GetComponentInParent<VentZone>();
            }

            CurrentZone = zone;
            InVent = zone != null;
        }
    }
}
