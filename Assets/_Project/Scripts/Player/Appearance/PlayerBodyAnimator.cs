using UnityEngine;

namespace VentsAndVending.Player.Appearance
{
    /// <summary>
    /// Cheap procedural animation for the player body: legs and arms swing with walking speed (knees and elbows bend),
    /// and the whole rig eases between standing, a deep crouch and a prone crawl so what you see when you look down
    /// matches the camera height of each state.
    /// </summary>
    public sealed class PlayerBodyAnimator : MonoBehaviour
    {
        [SerializeField] private Transform _root;
        [SerializeField] private Transform _hipL, _hipR, _kneeL, _kneeR;
        [SerializeField] private Transform _shoulderL, _shoulderR, _elbowL, _elbowR;
        [SerializeField] private float _strideSpeed = 7f;
        [SerializeField] private float _swing = 26f;

        private CharacterController _controller;
        private PlayerStateController _state;
        private float _phase;
        private float _crouch;  // 0 stand → 1 crouch
        private float _crawl;   // 0 → 1 prone

        internal void Configure(Transform root, Transform hipL, Transform hipR, Transform kneeL, Transform kneeR,
            Transform shoulderL, Transform shoulderR, Transform elbowL, Transform elbowR)
        {
            _root = root;
            _hipL = hipL;
            _hipR = hipR;
            _kneeL = kneeL;
            _kneeR = kneeR;
            _shoulderL = shoulderL;
            _shoulderR = shoulderR;
            _elbowL = elbowL;
            _elbowR = elbowR;
        }

        private void Awake()
        {
            _controller = GetComponentInParent<CharacterController>();
            _state = GetComponentInParent<PlayerStateController>();
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            PlayerMovementState s = _state != null ? _state.Current : PlayerMovementState.Normal;
            _crouch = Mathf.MoveTowards(_crouch, s == PlayerMovementState.Crouching ? 1f : 0f, dt * 4f);
            _crawl = Mathf.MoveTowards(_crawl, s == PlayerMovementState.Crawling ? 1f : 0f, dt * 3f);

            Vector3 v = _controller != null ? _controller.velocity : Vector3.zero;
            float speed = new Vector2(v.x, v.z).magnitude;
            float moving = Mathf.Clamp01(speed / 4f);
            _phase += dt * _strideSpeed * Mathf.Lerp(0.6f, 1.2f, Mathf.Clamp01(speed / 6f)) * (moving > 0.05f ? 1f : 0.25f);

            // Root pose: stand (well behind the camera so looking down shows belt, legs and feet rather than a wall of chest), squat, or lie prone
            Vector3 standPos = new Vector3(0f, -0.04f, -0.28f);
            Vector3 crouchPos = new Vector3(0f, -0.6f, -0.24f);
            Vector3 crawlPos = new Vector3(0f, 0.3f, -1.62f);
            Vector3 pos = Vector3.Lerp(Vector3.Lerp(standPos, crouchPos, _crouch), crawlPos, _crawl);
            Quaternion rot = Quaternion.Slerp(Quaternion.Euler(12f * _crouch, 0f, 0f), Quaternion.Euler(90f, 0f, 0f), _crawl);
            if (_root != null)
            {
                _root.localPosition = pos;
                _root.localRotation = rot;
            }

            float swing = Mathf.Sin(_phase) * _swing * moving;
            float idle = Mathf.Sin(Time.time * 1.3f) * 3f * (1f - moving);

            // Legs: walking swing → squat → small crawl kicks
            float hipBase = Mathf.Lerp(0f, -82f, _crouch);
            float kneeBase = Mathf.Lerp(4f, 142f, _crouch);
            float kick = Mathf.Sin(_phase * 1.4f) * 18f * moving * _crawl;
            SetX(_hipL, Mathf.Lerp(hipBase + swing, kick, _crawl));
            SetX(_hipR, Mathf.Lerp(hipBase - swing, -kick, _crawl));
            SetX(_kneeL, Mathf.Lerp(kneeBase + Mathf.Max(0f, -Mathf.Cos(_phase)) * 40f * moving, 20f + Mathf.Max(0f, kick), _crawl));
            SetX(_kneeR, Mathf.Lerp(kneeBase + Mathf.Max(0f, Mathf.Cos(_phase)) * 40f * moving, 20f + Mathf.Max(0f, -kick), _crawl));

            // Arms: opposite swing → forward reach while crawling (hand over hand)
            float reach = Mathf.Sin(_phase * 1.4f) * 40f * moving;
            SetX(_shoulderL, Mathf.Lerp(-swing * 0.8f + idle, -160f + reach, _crawl));
            SetX(_shoulderR, Mathf.Lerp(swing * 0.8f - idle, -160f - reach, _crawl));
            SetX(_elbowL, Mathf.Lerp(-12f - Mathf.Max(0f, swing) * 0.5f, -30f, _crawl));
            SetX(_elbowR, Mathf.Lerp(-12f - Mathf.Max(0f, -swing) * 0.5f, -30f, _crawl));
        }

        private static void SetX(Transform t, float degrees)
        {
            if (t != null)
            {
                t.localRotation = Quaternion.Euler(degrees, 0f, 0f);
            }
        }
    }
}
