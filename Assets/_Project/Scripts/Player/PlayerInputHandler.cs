using UnityEngine;
using UnityEngine.InputSystem;

namespace VentsAndVending.Player
{
    /// <summary>
    /// Reads the "Player" action map (project-wide actions by default) and exposes per-frame input values.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PlayerInputHandler : MonoBehaviour
    {
        [Tooltip("Optional. Leave empty to use the project-wide input actions.")]
        [SerializeField] private InputActionAsset _actionsOverride;

        [Tooltip("Accessibility: hold to crouch/crawl instead of toggling.")]
        [SerializeField] private bool _holdStance;

        private InputActionMap _map;
        private InputAction _move;
        private InputAction _look;
        private InputAction _jump;
        private InputAction _sprint;
        private InputAction _crouch;
        private InputAction _crawl;
        private InputAction _interact;
        private InputAction _inventory;
        private InputAction _cancel;

        public Vector2 Move { get; private set; }
        public bool InventoryPressed { get; private set; }
        public bool CancelPressed { get; private set; }

        /// <summary>Display string of the interact key, e.g. "E".</summary>
        public string InteractBindingLabel { get; private set; } = "E";
        public Vector2 Look { get; private set; }
        /// <summary>True when Look came from a mouse/pointer delta (pixels), false for sticks (-1..1).</summary>
        public bool LookIsPointer { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool CrouchRequested { get; private set; }
        public bool CrawlRequested { get; private set; }

        public bool HoldStance
        {
            get => _holdStance;
            set => _holdStance = value;
        }

        private void Awake()
        {
            InputActionAsset asset = _actionsOverride != null ? _actionsOverride : InputSystem.actions;
            if (asset == null)
            {
                Debug.LogError("[PlayerInputHandler] No input actions found. Assign an override or set project-wide actions.", this);
                enabled = false;
                return;
            }

            _map = asset.FindActionMap("Player", throwIfNotFound: true);
            _move = _map.FindAction("Move", throwIfNotFound: true);
            _look = _map.FindAction("Look", throwIfNotFound: true);
            _jump = _map.FindAction("Jump", throwIfNotFound: true);
            _sprint = _map.FindAction("Sprint", throwIfNotFound: true);
            _crouch = _map.FindAction("Crouch", throwIfNotFound: true);
            _interact = _map.FindAction("Interact", throwIfNotFound: true);
            _crawl = _map.FindAction("Crawl", throwIfNotFound: false);
            _inventory = _map.FindAction("Inventory", throwIfNotFound: false);
            _cancel = asset.FindAction("UI/Cancel", throwIfNotFound: false);
            if (_crawl == null || _inventory == null)
            {
                Debug.LogWarning("[PlayerInputHandler] 'Crawl' or 'Inventory' action missing from the Player map. Run Vents & Vending > Prototype > Build Movement Greybox Scene to add them.", this);
            }

            string label = _interact.GetBindingDisplayString(InputBinding.MaskByGroup("Keyboard&Mouse"),
                InputBinding.DisplayStringOptions.DontIncludeInteractions);
            if (!string.IsNullOrEmpty(label))
            {
                InteractBindingLabel = label;
            }
        }

        private void OnEnable()
        {
            _map?.Enable();
            _cancel?.actionMap?.Enable();
        }

        private void Update()
        {
            Move = _move.ReadValue<Vector2>();
            Look = _look.ReadValue<Vector2>();
            LookIsPointer = _look.activeControl?.device is Pointer;
            SprintHeld = _sprint.IsPressed();
            JumpPressed = _jump.WasPressedThisFrame();
            InteractPressed = _interact.WasPressedThisFrame();
            InventoryPressed = _inventory != null && _inventory.WasPressedThisFrame();
            CancelPressed = _cancel != null && _cancel.WasPressedThisFrame();

            if (_holdStance)
            {
                CrouchRequested = _crouch.IsPressed();
                CrawlRequested = _crawl != null && _crawl.IsPressed();
                return;
            }

            if (_crouch.WasPressedThisFrame())
            {
                CrouchRequested = !CrouchRequested;
                if (CrouchRequested)
                {
                    CrawlRequested = false;
                }
            }

            if (_crawl != null && _crawl.WasPressedThisFrame())
            {
                CrawlRequested = !CrawlRequested;
                if (CrawlRequested)
                {
                    CrouchRequested = false;
                }
            }

            // Starting to sprint cancels a toggled stance (headroom permitting — see PlayerStateLogic).
            if (_sprint.WasPressedThisFrame())
            {
                CrouchRequested = false;
                CrawlRequested = false;
            }
        }
    }
}
