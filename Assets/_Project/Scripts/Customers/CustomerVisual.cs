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

        [Header("Limbs")]
        [Tooltip("Shoulder/hip pivots that swing around their local X axis while walking.")]
        [SerializeField] private Transform[] _swingPivots = new Transform[0];
        [Tooltip("Per pivot: phase in radians (0 or PI makes arms/legs alternate).")]
        [SerializeField] private float[] _swingPhases = new float[0];
        [SerializeField] private float _swingAngle = 28f;
        [SerializeField] private float _swingSpeed = 6f;
        [SerializeField] private float _idleSway = 4f;
        [Tooltip("Per pivot: the knee/elbow joint under it (or empty).")]
        [SerializeField] private Transform[] _swingJoints = new Transform[0];
        [Tooltip("Per pivot: max extra bend in degrees. Knees positive (shin folds back), elbows negative (forearm folds forward).")]
        [SerializeField] private float[] _jointBends = new float[0];
        [Tooltip("Per pivot: 0 = knee (bends while the leg swings forward), 1 = elbow (bends while the arm swings forward).")]
        [SerializeField] private int[] _jointModes = new int[0];

        private Quaternion[] _swingBase;
        private Quaternion[] _jointBase;
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

        internal void ConfigureLimbs(Transform[] pivots, float[] phases, float angle, float speed,
            Transform[] joints = null, float[] bends = null, int[] modes = null)
        {
            _swingPivots = pivots;
            _swingPhases = phases;
            _swingAngle = angle;
            _swingSpeed = speed;
            _swingJoints = joints ?? new Transform[0];
            _jointBends = bends ?? new float[0];
            _jointModes = modes ?? new int[0];
        }

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
            _swingBase = new Quaternion[_swingPivots.Length];
            for (int i = 0; i < _swingPivots.Length; i++)
            {
                _swingBase[i] = _swingPivots[i] != null ? _swingPivots[i].localRotation : Quaternion.identity;
            }
            _jointBase = new Quaternion[_swingJoints.Length];
            for (int i = 0; i < _swingJoints.Length; i++)
            {
                _jointBase[i] = _swingJoints[i] != null ? _swingJoints[i].localRotation : Quaternion.identity;
            }
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

            // Limb swing: full stride when walking, a lazy sway when standing about
            float stride = (Time.time + _phase) * _swingSpeed;
            for (int i = 0; i < _swingPivots.Length; i++)
            {
                if (_swingPivots[i] == null)
                {
                    continue;
                }
                float phase = i < _swingPhases.Length ? _swingPhases[i] : 0f;
                float angle = Mathf.Sin(stride + phase) * _swingAngle * moving
                            + Mathf.Sin((Time.time + _phase) * 1.3f + phase) * _idleSway * (1f - moving);
                _swingPivots[i].localRotation = _swingBase[i] * Quaternion.Euler(angle, 0f, 0f);

                // Knee/elbow: positive pivot angle = limb swung back, so the limb moves forward while the
                // angle is falling (cos < 0 for legs). Knees fold during that swing; elbows flex as the arm comes forward.
                if (i < _swingJoints.Length && _swingJoints[i] != null)
                {
                    float bend = i < _jointBends.Length ? _jointBends[i] : 0f;
                    int mode = i < _jointModes.Length ? _jointModes[i] : 0;
                    float cycle = mode == 0
                        ? Mathf.Max(0f, -Mathf.Cos(stride + phase))
                        : 0.35f + 0.65f * Mathf.Max(0f, -Mathf.Sin(stride + phase));
                    float rest = mode == 0 ? 0.08f : 0.3f; // a little natural bend when standing
                    float amount = Mathf.Lerp(rest, cycle, moving);
                    _swingJoints[i].localRotation = _jointBase[i] * Quaternion.Euler(bend * amount, 0f, 0f);
                }
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
