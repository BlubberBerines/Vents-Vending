using System;
using UnityEngine;
using VentsAndVending.Inventory;
using VentsAndVending.Player;

namespace VentsAndVending.Interaction
{
    /// <summary>
    /// Raycasts from the camera, tracks the current <see cref="IInteractable"/> and its prompt, and executes it.
    /// Also the "interactor" handed to interactables, giving them access to the player's inventory and state.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputHandler), typeof(PlayerStateController))]
    public sealed class InteractionController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _interactionMask = ~0;
        [Tooltip("Sphere-cast radius used when the precise ray misses — makes small items easier to target.")]
        [SerializeField] private float _assistRadius = 0.12f;

        private readonly RaycastHit[] _hits = new RaycastHit[16];

        public PlayerInputHandler Input { get; private set; }
        public PlayerStateController State { get; private set; }
        public InventoryComponent Inventory { get; private set; }
        public Transform View => _camera != null ? _camera.transform : transform;

        public IInteractable Current { get; private set; }
        /// <summary>Prompt for the current target, or null.</summary>
        public string CurrentPrompt { get; private set; }
        public string InteractKeyLabel => Input != null ? Input.InteractBindingLabel : "E";

        /// <summary>Short feedback messages for the HUD ("+2 Glarb Extract", "Inventory full").</summary>
        public event Action<string> Notified;

        /// <summary>Request to show the player's inventory alongside another one (or alone when null).</summary>
        public event Action<InventoryComponent> InventoryViewRequested;

        private void Awake()
        {
            Input = GetComponent<PlayerInputHandler>();
            State = GetComponent<PlayerStateController>();
            Inventory = GetComponent<InventoryComponent>();
            if (_camera == null)
            {
                _camera = GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            if (State.IsInteracting || _camera == null)
            {
                SetTarget(null, null);
                return;
            }

            FindTarget();

            if (Current != null && Input.InteractPressed)
            {
                Current.Interact(this);
                FindTarget(); // refresh prompt immediately (e.g. "Open" → "Close")
            }
        }

        public void Notify(string message) => Notified?.Invoke(message);

        public void RequestInventoryView(InventoryComponent other) => InventoryViewRequested?.Invoke(other);

        private void FindTarget()
        {
            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            float distance = State.Tuning.InteractionDistance;

            int count = Physics.RaycastNonAlloc(ray, _hits, distance, _interactionMask, QueryTriggerInteraction.Ignore);
            if (TryNearest(count, out Collider first) && TryResolve(first, out IInteractable target, out string prompt))
            {
                SetTarget(target, prompt);
                return;
            }

            if (_assistRadius > 0f)
            {
                count = Physics.SphereCastNonAlloc(ray, _assistRadius, _hits, distance, _interactionMask, QueryTriggerInteraction.Ignore);
                if (TryNearest(count, out first) && TryResolve(first, out target, out prompt))
                {
                    SetTarget(target, prompt);
                    return;
                }
            }

            SetTarget(null, null);
        }

        /// <summary>Nearest hit that isn't part of the player (the camera sits inside the player's capsule).</summary>
        private bool TryNearest(int count, out Collider nearest)
        {
            nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (hit.collider.transform.IsChildOf(transform) || hit.distance >= best)
                {
                    continue;
                }

                // Sphere casts report distance 0 for colliders already overlapping the start — skip those too.
                if (hit.distance <= 0f && hit.point == Vector3.zero)
                {
                    continue;
                }

                best = hit.distance;
                nearest = hit.collider;
            }

            return nearest != null;
        }

        private bool TryResolve(Collider collider, out IInteractable target, out string prompt)
        {
            target = collider.GetComponentInParent<IInteractable>();
            prompt = target?.GetPrompt(this);
            return target != null && !string.IsNullOrEmpty(prompt);
        }

        private void SetTarget(IInteractable target, string prompt)
        {
            Current = target;
            CurrentPrompt = prompt;
        }
    }
}
