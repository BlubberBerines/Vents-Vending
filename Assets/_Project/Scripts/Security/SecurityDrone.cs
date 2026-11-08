using UnityEngine;

namespace VentsAndVending.Security
{
    /// <summary>
    /// Patrol Drone (GDD §11.1): flies a predefined route, scans with a spotlight, investigates suspicious activity
    /// and pursues identified players. Flies above obstacles, so it needs no NavMesh. Sight uses raycasts (GDD §11.2).
    /// </summary>
    public sealed class SecurityDrone : MonoBehaviour
    {
        [Header("Route")]
        [SerializeField] private Transform[] _patrolPoints = new Transform[0];
        [SerializeField] private float _altitude = 2.7f;
        [SerializeField] private float _pursuitAltitude = 2.2f;
        [SerializeField] private float _waitAtPoint = 1.5f;

        [Header("Movement")]
        [Tooltip("Faster than walking (4.2) but slower than sprinting (6.2) so running is a real escape.")]
        [SerializeField] private float _pursuitSpeed = 5.4f;
        [SerializeField] private float _patrolSpeed = 2.4f;
        [SerializeField] private float _turnSpeed = 150f;

        [Header("Sensors")]
        [SerializeField] private float _viewRange = 13f;
        [SerializeField, Range(10f, 360f)] private float _fieldOfView = 110f;
        [SerializeField] private float _catchRadius = 1.6f;
        [SerializeField] private float _scanSweep = 50f;
        [SerializeField] private LayerMask _sightMask = ~0;
        [SerializeField] private float _cooldownAfterCatch = 8f;

        [Header("Visuals")]
        [SerializeField] private Transform _eye;
        [SerializeField] private Renderer _eyeRenderer;
        [SerializeField] private Light _spotlight;
        [SerializeField] private Transform[] _rotors = new Transform[0];
        [SerializeField] private Transform _hoverRoot;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly DroneAlert _alert = new DroneAlert();
        private PlayerExposure _player;
        private MaterialPropertyBlock _block;
        private Vector3 _lastKnown;
        private int _patrolIndex;
        private float _waitTimer;
        private float _cooldown;
        private float _scanPhase;
        private float _bobPhase;

        public DroneAlert Alert => _alert;
        public bool CanSeePlayer { get; private set; }
        public float CurrentGainRate { get; private set; }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _alert.Caught += OnCaught;
            _bobPhase = Random.value * 10f;
        }

        private void OnEnable()
        {
            if (SecurityManager.Instance != null)
            {
                SecurityManager.Instance.Register(this);
            }
        }

        private void Start()
        {
            // Register again in case the manager woke after us.
            if (SecurityManager.Instance != null)
            {
                SecurityManager.Instance.Register(this);
            }
        }

        private void OnDisable()
        {
            if (SecurityManager.Instance != null)
            {
                SecurityManager.Instance.Unregister(this);
            }
        }

        private void OnDestroy() => _alert.Caught -= OnCaught;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerExposure>();
            }

            _cooldown -= dt;
            Sense(out bool canSee, out float rate, out float horizontalDistance);
            CanSeePlayer = canSee;
            CurrentGainRate = rate;
            if (canSee)
            {
                _lastKnown = _player.transform.position;
            }

            _alert.Tick(dt, rate, canSee, canSee && horizontalDistance <= _catchRadius);
            Move(dt);
            UpdateVisuals(dt);
        }

        // ------------------------------------------------------------------ Sensing

        private void Sense(out bool canSee, out float rate, out float horizontalDistance)
        {
            canSee = false;
            rate = 0f;
            horizontalDistance = float.MaxValue;
            if (_player == null || _cooldown > 0f)
            {
                return;
            }

            Vector3 eye = _eye != null ? _eye.position : transform.position;
            Vector3 toPlayer = _player.TorsoPoint - eye;
            Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
            horizontalDistance = flat.magnitude;
            float distance = toPlayer.magnitude;
            float angle = Vector3.Angle(new Vector3(transform.forward.x, 0f, transform.forward.z), flat);

            float visibility = _player.Visibility;
            bool inCone = SecurityRules.InViewCone(distance, _viewRange, angle, _fieldOfView) || horizontalDistance < 1.2f;
            if (visibility <= 0f || !inCone)
            {
                return;
            }

            bool lineOfSight = HasLineOfSight(eye, _player.EyePoint) || HasLineOfSight(eye, _player.TorsoPoint);
            if (!lineOfSight)
            {
                return;
            }

            canSee = true;
            float vigilance = SecurityManager.Instance != null ? SecurityManager.Instance.Vigilance : 1f;
            rate = SecurityRules.SuspicionRate(distance, _viewRange, Mathf.Min(angle, _fieldOfView * 0.5f), _fieldOfView, true,
                visibility, _player.Suspiciousness, vigilance);
        }

        private bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            if (!Physics.Linecast(from, to, out RaycastHit hit, _sightMask, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.collider.transform.IsChildOf(_player.transform);
        }

        // ------------------------------------------------------------------ Movement

        private void Move(float dt)
        {
            Vector3 target;
            float speed;
            float altitude = _altitude;
            bool faceTarget = false;

            switch (_alert.State)
            {
                case AlertState.Suspicious:
                    // Hold position-ish and drift towards whatever caught our eye.
                    target = (Flat(_lastKnown) - Flat(transform.position)).magnitude > 4f
                        ? Vector3.MoveTowards(Flat(transform.position), Flat(_lastKnown), 1f)
                        : Flat(transform.position);
                    speed = _patrolSpeed * 0.5f;
                    faceTarget = true;
                    break;

                case AlertState.Pursuing:
                    target = Flat(_lastKnown);
                    speed = _pursuitSpeed;
                    altitude = _pursuitAltitude;
                    faceTarget = true;
                    break;

                case AlertState.Searching:
                    target = Flat(_lastKnown);
                    speed = _patrolSpeed;
                    break;

                default:
                    if (_patrolPoints.Length == 0)
                    {
                        target = Flat(transform.position);
                        speed = 0f;
                        break;
                    }

                    target = Flat(_patrolPoints[_patrolIndex].position);
                    speed = _patrolSpeed;
                    if ((Flat(transform.position) - target).sqrMagnitude < 0.04f)
                    {
                        _waitTimer += dt;
                        if (_waitTimer >= _waitAtPoint)
                        {
                            _waitTimer = 0f;
                            _patrolIndex = (_patrolIndex + 1) % _patrolPoints.Length;
                        }
                    }
                    break;
            }

            Vector3 position = transform.position;
            Vector3 flatPos = Flat(position);
            Vector3 next = Vector3.MoveTowards(flatPos, target, speed * dt);
            float y = Mathf.MoveTowards(position.y, altitude, 1.5f * dt);
            transform.position = new Vector3(next.x, y, next.z);

            // Facing: look where we're going, at the suspect, or sweep while scanning.
            Vector3 lookDir;
            if (faceTarget)
            {
                lookDir = Flat(_lastKnown) - flatPos;
            }
            else if ((target - flatPos).sqrMagnitude > 0.04f)
            {
                lookDir = target - flatPos;
            }
            else
            {
                // Scanning: sweep side to side on patrol, turn full circles while searching.
                _scanPhase += dt;
                float yawRate = _alert.State == AlertState.Searching ? 90f : Mathf.Cos(_scanPhase * 1.2f) * _scanSweep * 1.2f;
                lookDir = Quaternion.Euler(0f, yawRate * dt, 0f) * new Vector3(transform.forward.x, 0f, transform.forward.z);
            }

            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion goal = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, _turnSpeed * dt);
            }
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private void OnCaught()
        {
            _cooldown = _cooldownAfterCatch;
            if (SecurityManager.Instance != null && _player != null)
            {
                SecurityManager.Instance.HandleCaught(_player);
            }
        }

        // ------------------------------------------------------------------ Visuals

        private void UpdateVisuals(float dt)
        {
            Color color = _cooldown > 0f
                ? new Color(0.4f, 0.4f, 0.45f)
                : _alert.State switch
                {
                    AlertState.Suspicious => Color.Lerp(new Color(1f, 0.85f, 0.1f), new Color(1f, 0.45f, 0.05f), _alert.Suspicion),
                    AlertState.Searching => new Color(1f, 0.5f, 0.05f),
                    AlertState.Pursuing => Mathf.Repeat(Time.time * 5f, 1f) > 0.5f ? Color.red : new Color(0.4f, 0f, 0.6f),
                    _ => new Color(0.15f, 0.85f, 1f)
                };

            if (_eyeRenderer != null)
            {
                _eyeRenderer.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, color * 0.5f);
                _block.SetColor(EmissionColorId, color * 3f);
                _eyeRenderer.SetPropertyBlock(_block);
            }

            if (_spotlight != null)
            {
                _spotlight.color = color;
                _spotlight.intensity = _alert.State == AlertState.Pursuing ? 9f : 5f;
            }

            foreach (Transform rotor in _rotors)
            {
                if (rotor != null)
                {
                    rotor.Rotate(0f, 1440f * dt, 0f, Space.Self);
                }
            }

            if (_hoverRoot != null)
            {
                _hoverRoot.localPosition = new Vector3(0f, Mathf.Sin((Time.time + _bobPhase) * 2.2f) * 0.06f, 0f);
            }
        }

        internal void SetPatrol(Transform[] points) => _patrolPoints = points;
    }
}
