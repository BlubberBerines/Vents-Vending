using UnityEngine;
using UnityEngine.AI;

namespace VentsAndVending.Customers
{
    /// <summary>
    /// Cheap procedural animation (GDD §15.6 "strong idle animations"): squash-and-stretch bob scaled by walking
    /// speed, orbiting floating appendages, and a happy hop.
    /// </summary>
    public sealed class CustomerVisual : MonoBehaviour
    {
        [SerializeField] private Transform _body;
        [SerializeField] private float _bobHeight = 0.05f;
        [SerializeField] private float _bobSpeed = 7f;
        [SerializeField] private float _squash = 0.07f;
        [SerializeField] private Transform[] _floaters = new Transform[0];
        [SerializeField] private float _floatRadius = 0.6f;
        [SerializeField] private float _floatHeight = 1.05f;
        [SerializeField] private float _floatSpeed = 1.6f;

        private NavMeshAgent _agent;
        private Vector3 _bodyOrigin;
        private float _hopTimer;
        private float _phase;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_body != null)
            {
                _bodyOrigin = _body.localPosition;
            }
            _phase = Random.value * 10f;
        }

        public void PlayHappy() => _hopTimer = 0.9f;

        private void LateUpdate()
        {
            float moving = _agent != null && _agent.enabled && _agent.speed > 0f ? Mathf.Clamp01(_agent.velocity.magnitude / _agent.speed) : 0f;
            float t = (Time.time + _phase) * _bobSpeed;

            if (_body != null)
            {
                float squash = Mathf.Sin(t) * _squash * (0.35f + moving);
                _body.localScale = new Vector3(1f + squash * 0.5f, 1f - squash, 1f + squash * 0.5f);

                float hop = 0f;
                if (_hopTimer > 0f)
                {
                    _hopTimer -= Time.deltaTime;
                    hop = Mathf.Abs(Mathf.Sin(_hopTimer * Mathf.PI * 3f)) * 0.3f;
                }
                _body.localPosition = _bodyOrigin + Vector3.up * (Mathf.Abs(Mathf.Sin(t)) * _bobHeight * moving + hop);
            }

            for (int i = 0; i < _floaters.Length; i++)
            {
                if (_floaters[i] == null)
                {
                    continue;
                }

                float angle = (Time.time + _phase) * _floatSpeed + i * Mathf.PI * 2f / _floaters.Length;
                _floaters[i].localPosition = new Vector3(Mathf.Cos(angle) * _floatRadius,
                    _floatHeight + Mathf.Sin(angle * 2f) * 0.08f, Mathf.Sin(angle) * _floatRadius);
            }
        }
    }
}
