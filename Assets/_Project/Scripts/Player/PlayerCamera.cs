using UnityEngine;
using UnityEngine.InputSystem;

namespace VentsAndVending.Player
{
    /// <summary>
    /// First-person look: yaw rotates the player body, pitch rotates the camera root.
    /// No head bob or shake by design (GDD §3.1).
    /// </summary>
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputHandler))]
    public sealed class PlayerCamera : MonoBehaviour
    {
        [Tooltip("Transform that pitches up/down (holds the camera). Auto-detected if empty.")]
        [SerializeField] private Transform _pitchRoot;
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField] private float _mouseSensitivity = 0.12f;
        [Tooltip("Degrees per second at full stick deflection.")]
        [SerializeField] private float _gamepadSensitivity = 160f;
        [SerializeField] private bool _invertY;
        [SerializeField, Range(10f, 89f)] private float _pitchLimit = 85f;
        [SerializeField] private bool _lockCursorOnStart = true;

        private PlayerInputHandler _input;
        private PlayerStateController _state;
        private float _pitch;

        public float MouseSensitivity
        {
            get => _mouseSensitivity;
            set => _mouseSensitivity = Mathf.Max(0.001f, value);
        }

        public bool InvertY
        {
            get => _invertY;
            set => _invertY = value;
        }

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            _state = GetComponent<PlayerStateController>();
            if (_pitchRoot == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    _pitchRoot = cam.transform.parent == transform ? cam.transform : cam.transform.parent;
                }
            }
        }

        private void Start()
        {
            if (_lockCursorOnStart)
            {
                SetCursorLocked(true);
            }

            Core.GameSettings.Changed += ApplySettings;
            ApplySettings();
        }

        private void OnDestroy() => Core.GameSettings.Changed -= ApplySettings;

        private void ApplySettings()
        {
            _mouseSensitivity = Core.GameSettings.MouseSensitivity;
            _invertY = Core.GameSettings.InvertY;
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cam.fieldOfView = Core.GameSettings.FieldOfView;
            }
        }

        private void Update()
        {
            // While a menu/machine owns the cursor, don't look around or grab the cursor back.
            if (_state != null && (_state.IsInteracting || _state.Current == PlayerMovementState.Interacting))
            {
                return;
            }

            HandleCursorLock();
            if (Cursor.lockState != CursorLockMode.Locked || _pitchRoot == null)
            {
                return;
            }

            Vector2 look = _input.Look;
            Vector2 delta = _input.LookIsPointer
                ? look * _mouseSensitivity
                : look * (_gamepadSensitivity * Time.deltaTime);

            transform.Rotate(0f, delta.x, 0f, Space.Self);
            _pitch = Mathf.Clamp(_pitch + (_invertY ? delta.y : -delta.y), -_pitchLimit, _pitchLimit);
            _pitchRoot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        /// <summary>Re-captures the cursor after alt-tab etc. Esc is handled by the pause menu.</summary>
        private static void HandleCursorLock()
        {
            Mouse mouse = Mouse.current;
            if (Cursor.lockState != CursorLockMode.Locked && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                SetCursorLocked(true);
            }
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
