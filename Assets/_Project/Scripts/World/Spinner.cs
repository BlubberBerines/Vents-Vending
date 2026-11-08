using UnityEngine;

namespace VentsAndVending.World
{
    /// <summary>Constant rotation with an optional bob — holograms, fans, signs (GDD §15.7 environmental animation).</summary>
    public sealed class Spinner : MonoBehaviour
    {
        [SerializeField] private Vector3 _degreesPerSecond = new Vector3(0f, 45f, 0f);
        [SerializeField] private float _bobHeight;
        [SerializeField] private float _bobSpeed = 1f;

        private Vector3 _origin;

        private void Awake() => _origin = transform.localPosition;

        private void Update()
        {
            transform.Rotate(_degreesPerSecond * Time.deltaTime, Space.Self);
            if (_bobHeight > 0f)
            {
                transform.localPosition = _origin + Vector3.up * (Mathf.Sin(Time.time * _bobSpeed) * _bobHeight);
            }
        }

        internal void Configure(Vector3 degreesPerSecond, float bobHeight, float bobSpeed)
        {
            _degreesPerSecond = degreesPerSecond;
            _bobHeight = bobHeight;
            _bobSpeed = bobSpeed;
        }
    }
}
