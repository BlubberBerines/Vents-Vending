using UnityEngine;

namespace VentsAndVending.Tunnels
{
    /// <summary>
    /// Industrial extraction fan (Docs/Design/TunnelNetwork.md §7 "Fan shutdown"). Powered by a breaker; while it runs
    /// a blocker fills the housing. Cut the power and it winds down, coasts to a rest angle that leaves a crouch-height
    /// gap under the hub, and the warning lamp goes red → amber. It never hurts the player (§0.5).
    /// </summary>
    public sealed class FanHazard : MonoBehaviour
    {
        public enum FanState { Running, WindingDown, Stopped, SpinningUp }

        [SerializeField] private string _systemId = TunnelIds.Fan;
        [SerializeField] private string _breakerId = TunnelIds.FanBreaker;
        [SerializeField] private bool _breakerStartsOn = true;
        [Tooltip("Rotates about its local Z axis.")]
        [SerializeField] private Transform _rotor;
        [Tooltip("Solid collider covering the housing while the blades turn.")]
        [SerializeField] private Collider _blocker;
        [SerializeField] private Renderer[] _warningLamps = new Renderer[0];
        [SerializeField] private Light _warningLight;
        [SerializeField] private ParticleSystem _suction;
        [Tooltip("Looping rumble; volume and pitch follow the rotor speed so the wind-down is audible.")]
        [SerializeField] private AudioSource _hum;
        [SerializeField] private float _humVolume = 0.9f;
        [SerializeField] private float _maxSpeed = 620f;          // deg/s
        [SerializeField] private float _spinUpAccel = 260f;      // deg/s²
        [SerializeField] private float _windDownDecel = 190f;    // deg/s² — a readable coast (~3 s), then a short creep to rest
        [Tooltip("Blade spacing in degrees; the fan settles on a multiple of this so a blade points straight up.")]
        [SerializeField] private float _restStep = 120f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color Red = new Color(1f, 0.12f, 0.08f);
        private static readonly Color Amber = new Color(1f, 0.6f, 0.1f);

        private MaterialPropertyBlock _block;
        private float _speed;
        private float _angle;
        private float _settleTarget = float.NaN;

        public string SystemId => _systemId;
        public string BreakerId => _breakerId;
        public bool PassageOccupied
        {
            get
            {
                var player=GameObject.FindWithTag("Player");
                if(player==null)return false;
                var local=transform.InverseTransformPoint(player.transform.position+Vector3.up*.9f);
                return Mathf.Abs(local.x)<1.8f && Mathf.Abs(local.y)<1.8f && Mathf.Abs(local.z)<.9f;
            }
        }
        public FanState State { get; private set; }
        public float Speed => _speed;
        public bool Powered => TunnelManager.Instance == null ? _breakerStartsOn : TunnelManager.Instance.GetSwitch(_breakerId, _breakerStartsOn);
        /// <summary>True when the housing can be crawled through.</summary>
        public bool Passable => State == FanState.Stopped;

        private void Start()
        {
            if (TunnelManager.Instance != null)
            {
                TunnelManager.Instance.Restored += Snap;
            }
            Snap();
        }

        private void OnDestroy()
        {
            if (TunnelManager.Instance != null)
            {
                TunnelManager.Instance.Restored -= Snap;
            }
        }

        /// <summary>Jump straight to the steady state for the current power (scene start, save loaded).</summary>
        private void Snap()
        {
            _settleTarget = float.NaN;
            if (Powered)
            {
                _speed = _maxSpeed;
                State = FanState.Running;
            }
            else
            {
                _speed = 0f;
                _angle = Mathf.Round(_angle / _restStep) * _restStep;
                State = FanState.Stopped;
            }
            Apply();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (Powered)
            {
                _settleTarget = float.NaN;
                _speed = Mathf.MoveTowards(_speed, _maxSpeed, _spinUpAccel * dt);
                State = _speed >= _maxSpeed ? FanState.Running : FanState.SpinningUp;
                _angle += _speed * dt;
            }
            else if (State != FanState.Stopped)
            {
                State = FanState.WindingDown;
                if (_speed > 40f)
                {
                    _speed = Mathf.MoveTowards(_speed, 40f, _windDownDecel * dt);
                    _angle += _speed * dt;
                }
                else
                {
                    // Creep to the next rest angle so a blade ends up pointing straight up
                    if (float.IsNaN(_settleTarget))
                    {
                        _settleTarget = Mathf.Ceil((_angle + 5f) / _restStep) * _restStep;
                    }
                    _speed = Mathf.Clamp((_settleTarget - _angle) * 3f, 18f, 40f);
                    _angle = Mathf.MoveTowards(_angle, _settleTarget, _speed * dt);
                    if (Mathf.Approximately(_angle, _settleTarget))
                    {
                        _speed = 0f;
                        State = FanState.Stopped;
                    }
                }
            }
            _angle %= 360f * 100f;
            Apply();
        }

        private void Apply()
        {
            if (_rotor != null)
            {
                _rotor.localRotation = Quaternion.Euler(0f, 0f, _angle);
            }
            if (_blocker != null)
            {
                _blocker.enabled = State != FanState.Stopped;
            }

            Color lamp = State == FanState.Stopped ? Amber : Red;
            _block ??= new MaterialPropertyBlock();
            foreach (Renderer r in _warningLamps)
            {
                if (r == null)
                {
                    continue;
                }
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, lamp * 0.5f);
                _block.SetColor(EmissionId, lamp * 2.5f);
                r.SetPropertyBlock(_block);
            }
            if (_warningLight != null)
            {
                _warningLight.color = lamp;
            }

            if (_hum != null)
            {
                float k = _speed / _maxSpeed;
                _hum.volume = _humVolume * Mathf.Clamp01(k * 1.2f);
                _hum.pitch = 0.35f + 0.65f * k;
                if (k > 0.01f && !_hum.isPlaying)
                {
                    _hum.Play();
                }
                else if (k <= 0.01f && _hum.isPlaying)
                {
                    _hum.Stop();
                }
            }

            if (_suction != null)
            {
                var emission = _suction.emission;
                emission.rateOverTimeMultiplier = 25f * (_speed / _maxSpeed);
                if (_speed > 1f && !_suction.isPlaying)
                {
                    _suction.Play();
                }
            }
        }
    }
}
