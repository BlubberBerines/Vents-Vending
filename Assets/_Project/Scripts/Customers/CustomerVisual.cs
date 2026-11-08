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
        [SerializeField] private CharacterFace _face;
        [Tooltip("Renderers recoloured per visit (body colour variety).")]
        [SerializeField] private Renderer[] _tintRenderers = new Renderer[0];

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private NavMeshAgent _agent;
        private Vector3 _bodyOrigin;
        private float _hopTimer;
        private float _shakeTimer;
        private float _phase;

        public CharacterFace Face => _face;

        /// <summary>Recolours the body for this visit. Emission follows the colour faintly (jelly glow).</summary>
        public void ApplyTint(Color color, float glow)
        {
            var block = new MaterialPropertyBlock();
            foreach (Renderer r in _tintRenderers)
            {
                if (r == null)
                {
                    continue;
                }
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                block.SetColor(EmissionColorId, color * glow);
                r.SetPropertyBlock(block);
            }
        }

        public void PlayAnnoyed()
        {
            _shakeTimer = 0.5f;
            if (_face != null)
            {
                _face.SetMood(CharacterMood.Annoyed, 3f);
            }
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_body != null)
            {
                _bodyOrigin = _body.localPosition;
            }
            _phase = Random.value * 10f;
        }

        public void PlayHappy()
        {
            _hopTimer = 0.9f;
            if (_face != null)
            {
                _face.SetMood(CharacterMood.Happy, 3f);
            }
        }

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
                Vector3 shake = Vector3.zero;
                if (_shakeTimer > 0f)
                {
                    _shakeTimer -= Time.deltaTime;
                    shake = new Vector3(Mathf.Sin(Time.time * 60f) * 0.04f * (_shakeTimer / 0.5f), 0f, 0f);
                }
                _body.localPosition = _bodyOrigin + shake + Vector3.up * (Mathf.Abs(Mathf.Sin(t)) * _bobHeight * moving + hop);
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
