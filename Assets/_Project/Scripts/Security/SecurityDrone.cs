using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace VentsAndVending.Security
{
    /// <summary>
    /// Patrol Drone (GDD §11.1): flies a readable main loop with occasional detours (PatrolPlanner), hovers and sweeps
    /// its light at each waypoint's point of interest, investigates suspicious activity, pursues, and searches a few
    /// spots around where it lost you. Follows NavMesh paths at altitude (round pillars, not through them) and never
    /// leaves its zone. Sight uses raycasts (GDD §11.2).
    /// </summary>
    public sealed class SecurityDrone : MonoBehaviour
    {
        [Header("Route")]
        [SerializeField] private Transform[] _patrolPoints = new Transform[0];
        [SerializeField] private float _altitude = 2.7f;
        [Tooltip("World height of the floor this drone patrols over; altitude is measured from here.")]
        [SerializeField] private float _floorY;
        [SerializeField] private float _pursuitAltitude = 2.2f;
        [SerializeField] private float _waitAtPoint = 1.5f;
        [Tooltip("Optional branches off the main loop (FromMain = index of the main point they leave from).")]
        [SerializeField] private DetourRoute[] _detours = new DetourRoute[0];
        [Tooltip("The drone never leaves this box (world space, Y ignored). Zero size = no limit.")]
        [SerializeField] private Bounds _zone;

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

        [Header("Audio")]
        [SerializeField] private AudioSource _hum;
        [SerializeField] private AudioSource _voice;
        [SerializeField] private AudioClip _chirp;
        [SerializeField] private AudioClip _alarm;

        [Serializable]
        public struct DetourRoute
        {
            public int FromMain;
            public Transform[] Points;
            [Range(0f, 1f)] public float Chance;
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly DroneAlert _alert = new DroneAlert();
        private PlayerExposure _player;
        private MaterialPropertyBlock _block;
        private Vector3 _lastKnown;
        private PatrolPlanner _planner;
        private readonly System.Random _rng = new System.Random();
        private readonly List<Vector3> _searchPoints = new List<Vector3>();
        private int _searchIndex;
        private readonly List<Vector3> _corners = new List<Vector3>();
        private NavMeshPath _path;
        private Vector3 _pathTarget = new Vector3(float.MaxValue, 0f, 0f);
        private float _repathTimer;
        private float _sweepCentre;
        private float _speedNow;
        private float _alarmTimer;
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
            _alert.StateChanged += OnAlertChanged;
            _path = new NavMeshPath();
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

        private void OnDestroy()
        {
            _alert.Caught -= OnCaught;
            _alert.StateChanged -= OnAlertChanged;
        }

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
            UpdateAudio(dt);
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

        // ------------------------------------------------------------------ Patrol / search planning

        private void EnsurePlanner()
        {
            if (_planner != null)
            {
                return;
            }
            var detours = new System.Collections.Generic.List<PatrolPlanner.Detour>();
            foreach (DetourRoute d in _detours)
            {
                detours.Add(new PatrolPlanner.Detour(d.FromMain, d.Points != null ? d.Points.Length : 0, d.Chance));
            }
            _planner = new PatrolPlanner(Mathf.Max(1, _patrolPoints.Length), detours);
        }

        private Transform CurrentPatrolPoint()
        {
            EnsurePlanner();
            PatrolPlanner.Waypoint w = _planner.Current;
            if (_patrolPoints.Length == 0)
            {
                return null;
            }
            return w.IsMain ? _patrolPoints[w.Index] : _detours[w.Detour].Points[w.Index];
        }

        private void OnAlertChanged(AlertState from, AlertState to)
        {
            if (to == AlertState.Suspicious && from == AlertState.Unaware)
            {
                Play(_chirp, 1f);
            }
            if (to == AlertState.Searching)
            {
                PlanSearch();
            }
            if (to == AlertState.Unaware && from != AlertState.Unaware)
            {
                ResumeNearest();
            }
            _pathTarget = new Vector3(float.MaxValue, 0f, 0f); // force a re-path
        }

        /// <summary>Three spots around where the player was last seen, inside the zone and on walkable ground.</summary>
        private void PlanSearch()
        {
            _searchPoints.Clear();
            _searchIndex = 0;
            _searchPoints.Add(ClampToZone(Flat(_lastKnown)));
            for (int i = 0; i < 3; i++)
            {
                Vector2 r = Random.insideUnitCircle.normalized * Random.Range(2.5f, 5f);
                Vector3 p = ClampToZone(Flat(_lastKnown) + new Vector3(r.x, 0f, r.y));
                if (NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    p = Flat(hit.position);
                }
                _searchPoints.Add(p);
            }
        }

        private void ResumeNearest()
        {
            EnsurePlanner();
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < _patrolPoints.Length; i++)
            {
                float d = (Flat(_patrolPoints[i].position) - Flat(transform.position)).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }
            _planner.ResumeAt(best);
            _waitTimer = 0f;
        }

        private Vector3 ClampToZone(Vector3 p)
        {
            if (_zone.size.sqrMagnitude < 0.01f)
            {
                return p;
            }
            Vector3 min = _zone.min, max = _zone.max;
            return new Vector3(Mathf.Clamp(p.x, min.x, max.x), _floorY, Mathf.Clamp(p.z, min.z, max.z));
        }

        // ------------------------------------------------------------------ Movement

        private void Move(float dt)
        {
            Vector3 target;
            float speed;
            float altitude = _floorY + _altitude;
            bool faceTarget = false;
            float sweep = _scanSweep;
            Transform lookAt = null;
            bool arrived;

            switch (_alert.State)
            {
                case AlertState.Suspicious:
                    // Hold position-ish and drift towards whatever caught our eye.
                    target = ClampToZone((Flat(_lastKnown) - Flat(transform.position)).magnitude > 4f
                        ? Vector3.MoveTowards(Flat(transform.position), Flat(_lastKnown), 1f)
                        : Flat(transform.position));
                    speed = _patrolSpeed * 0.5f;
                    faceTarget = true;
                    break;

                case AlertState.Pursuing:
                    target = ClampToZone(Flat(_lastKnown));
                    speed = _pursuitSpeed;
                    altitude = _pursuitAltitude;
                    faceTarget = true;
                    break;

                case AlertState.Searching:
                    if (_searchPoints.Count == 0)
                    {
                        PlanSearch();
                    }
                    target = _searchPoints[Mathf.Min(_searchIndex, _searchPoints.Count - 1)];
                    speed = _patrolSpeed * 1.2f;
                    sweep = 160f;
                    if ((Flat(transform.position) - target).sqrMagnitude < 0.25f)
                    {
                        _waitTimer += dt;
                        if (_waitTimer >= 1.3f && _searchIndex < _searchPoints.Count - 1)
                        {
                            _waitTimer = 0f;
                            _searchIndex++;
                        }
                    }
                    break;

                default:
                    Transform point = CurrentPatrolPoint();
                    if (point == null)
                    {
                        target = Flat(transform.position);
                        speed = 0f;
                        break;
                    }

                    var info = point.GetComponent<DronePatrolPoint>();
                    target = Flat(point.position);
                    speed = _patrolSpeed;
                    altitude = _floorY + _altitude + (info != null ? info.AltitudeOffset : 0f);
                    arrived = (Flat(transform.position) - target).sqrMagnitude < 0.09f;
                    if (arrived)
                    {
                        sweep = info != null ? info.Sweep : _scanSweep;
                        lookAt = info != null ? info.LookAt : null;
                        _waitTimer += dt;
                        if (_waitTimer >= (info != null ? info.Wait : _waitAtPoint))
                        {
                            _waitTimer = 0f;
                            _planner.Advance(_rng);
                        }
                    }
                    break;
            }

            // Follow a NavMesh path at altitude so the drone goes round pillars and stalls instead of through them
            Vector3 position = transform.position;
            Vector3 flatPos = Flat(position);
            Vector3 steer = NextCorner(flatPos, target, dt);
            Vector3 next = Vector3.MoveTowards(flatPos, steer, speed * dt);
            _speedNow = (next - flatPos).magnitude / Mathf.Max(dt, 0.0001f);
            float y = Mathf.MoveTowards(position.y, altitude, 1.5f * dt);
            transform.position = new Vector3(next.x, y, next.z);

            // Facing: at the suspect, at the waypoint's point of interest, along the path, or sweeping.
            Vector3 lookDir;
            if (faceTarget)
            {
                lookDir = Flat(_lastKnown) - flatPos;
            }
            else if ((steer - flatPos).sqrMagnitude > 0.09f)
            {
                lookDir = steer - flatPos;
                _sweepCentre = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg;
            }
            else
            {
                if (lookAt != null)
                {
                    Vector3 to = Flat(lookAt.position) - flatPos;
                    _sweepCentre = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                }
                _scanPhase += dt;
                float yaw = _sweepCentre + Mathf.Sin(_scanPhase * (_alert.State == AlertState.Searching ? 1.6f : 0.9f)) * sweep * 0.5f;
                lookDir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            }

            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion goal = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, _turnSpeed * dt);
            }
        }

        /// <summary>Next corner of a (cached) NavMesh path from here to <paramref name="target"/>, or the target itself.</summary>
        private Vector3 NextCorner(Vector3 from, Vector3 target, float dt)
        {
            _repathTimer -= dt;
            if ((target - _pathTarget).sqrMagnitude > 1f || _repathTimer <= 0f)
            {
                _repathTimer = 0.5f;
                _pathTarget = target;
                _corners.Clear();
                if (NavMesh.SamplePosition(from, out NavMeshHit a, 3f, NavMesh.AllAreas)
                    && NavMesh.SamplePosition(target, out NavMeshHit b, 3f, NavMesh.AllAreas)
                    && NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, _path)
                    && _path.status != NavMeshPathStatus.PathInvalid)
                {
                    foreach (Vector3 c in _path.corners)
                    {
                        _corners.Add(ClampToZone(Flat(c)));
                    }
                    if (_corners.Count > 0)
                    {
                        _corners[_corners.Count - 1] = target;
                    }
                }
            }

            while (_corners.Count > 1 && (Flat(_corners[0]) - from).sqrMagnitude < 0.36f)
            {
                _corners.RemoveAt(0);
            }
            return _corners.Count > 0 ? _corners[0] : target;
        }

        /// <summary>Project onto this drone's floor plane (so NavMesh sampling happens on its own level, e.g. underground).</summary>
        private Vector3 Flat(Vector3 v) => new Vector3(v.x, _floorY, v.z);

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null && _voice != null)
            {
                _voice.PlayOneShot(clip, volume);
            }
        }

        private void UpdateAudio(float dt)
        {
            if (_hum != null)
            {
                float k = Mathf.Clamp01(_speedNow / Mathf.Max(0.1f, _pursuitSpeed));
                _hum.pitch = 0.9f + k * 0.45f + (_alert.State == AlertState.Pursuing ? 0.1f : 0f);
            }
            if (_alert.State == AlertState.Pursuing)
            {
                _alarmTimer -= dt;
                if (_alarmTimer <= 0f)
                {
                    _alarmTimer = 1.1f;
                    Play(_alarm, 0.8f);
                }
            }
            else
            {
                _alarmTimer = 0f;
            }
        }

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

        /// <summary>Forget the player and go back to the patrol (after an escort-out). The catch cooldown keeps running.</summary>
        public void ResetToPatrol()
        {
            _searchPoints.Clear();
            bool wasAware = _alert.State != AlertState.Unaware;
            _alert.Reset();
            if (!wasAware && _patrolPoints != null && _patrolPoints.Length > 0)
            {
                ResumeNearest();
            }
            _pathTarget = new Vector3(float.MaxValue, 0f, 0f);
        }

        internal void SetPatrol(Transform[] points, DetourRoute[] detours = null, Bounds zone = default, float floorY = 0f)
        {
            _floorY = floorY;
            _patrolPoints = points;
            _detours = detours ?? new DetourRoute[0];
            _zone = zone;
            _planner = null;
        }
    }
}
