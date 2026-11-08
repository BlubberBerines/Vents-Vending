using UnityEngine;

namespace VentsAndVending.World
{
    /// <summary>Gentle idle sway (hanging bulbs, cables, signs): rotates about local X and Z with a random phase.</summary>
    public sealed class Pendulum : MonoBehaviour
    {
        [SerializeField] private float _amplitude = 2f;  // degrees
        [SerializeField] private float _period = 4f;     // seconds

        private Quaternion _rest;
        private float _phase;

        internal void Configure(float amplitude, float period)
        {
            _amplitude = amplitude;
            _period = period;
        }

        private void Awake()
        {
            _rest = transform.localRotation;
            _phase = Random.value * 100f;
        }

        private void Update()
        {
            float t = (Time.time + _phase) * Mathf.PI * 2f / Mathf.Max(0.1f, _period);
            // Two slightly detuned axes trace a lazy ellipse instead of a metronome
            transform.localRotation = _rest * Quaternion.Euler(Mathf.Sin(t) * _amplitude, 0f, Mathf.Sin(t * 0.83f + 1.3f) * _amplitude * 0.7f);
        }
    }
}
