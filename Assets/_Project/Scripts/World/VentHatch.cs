using UnityEngine;
using VentsAndVending.Interaction;

namespace VentsAndVending.World
{
    /// <summary>A hinged ventilation hatch or grate that the player opens and closes (GDD §4.3).</summary>
    public sealed class VentHatch : MonoBehaviour, IInteractable
    {
        [Tooltip("The transform that rotates (hinge). Its children are the hatch panel and its collider.")]
        [SerializeField] private Transform _pivot;
        [SerializeField] private Vector3 _openRotation = new Vector3(100f, 0f, 0f);
        [SerializeField, Min(0.01f)] private float _duration = 0.35f;
        [SerializeField] private bool _startOpen;
        [SerializeField] private bool _locked;
        [SerializeField] private AudioClip _sound;

        private Quaternion _closed;
        private Quaternion _open;
        private float _t;

        public bool IsOpen { get; private set; }

        public bool Locked
        {
            get => _locked;
            set => _locked = value;
        }

        private void Awake()
        {
            if (_pivot == null)
            {
                _pivot = transform;
            }

            _closed = _pivot.localRotation;
            _open = _closed * Quaternion.Euler(_openRotation);
            IsOpen = _startOpen;
            _t = IsOpen ? 1f : 0f;
            Apply();
        }

        public string GetPrompt(InteractionController interactor)
        {
            if (_locked)
            {
                return "Hatch is sealed";
            }

            return IsOpen ? "Close hatch" : "Open hatch";
        }

        public void Interact(InteractionController interactor)
        {
            if (_locked)
            {
                interactor.Notify("It won't budge. Sealed from the other side.");
                return;
            }

            IsOpen = !IsOpen;
            Sfx.PlayAt(_sound, transform.position, 0.8f, IsOpen ? Random.Range(1.0f, 1.12f) : Random.Range(0.82f, 0.92f));
        }

        private void Update()
        {
            float target = IsOpen ? 1f : 0f;
            if (Mathf.Approximately(_t, target))
            {
                return;
            }

            _t = Mathf.MoveTowards(_t, target, Time.deltaTime / _duration);
            Apply();
        }

        private void Apply()
        {
            _pivot.localRotation = Quaternion.Slerp(_closed, _open, Mathf.SmoothStep(0f, 1f, _t));
        }
    }
}
