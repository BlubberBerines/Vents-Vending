using UnityEngine;

namespace VentsAndVending.World
{
    /// <summary>
    /// Trigger volume marking the inside of a ventilation section. Players inside are forced to crawl.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class VentZone : MonoBehaviour
    {
        [SerializeField] private string _sectionId = "vent";

        public string SectionId => _sectionId;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnValidate()
        {
            var box = GetComponent<BoxCollider>();
            if (box != null && !box.isTrigger)
            {
                box.isTrigger = true;
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
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.15f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
