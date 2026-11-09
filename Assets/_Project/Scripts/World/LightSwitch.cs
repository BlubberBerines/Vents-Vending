using UnityEngine;
using VentsAndVending.Interaction;

namespace VentsAndVending.World
{
    /// <summary>
    /// A wall button that toggles a <see cref="LightCircuit"/> by ID (the den's ceiling-light switch). The indicator lamp
    /// glows green while the circuit is on and red while it's off, so the switch is findable in the dark.
    /// </summary>
    public sealed class LightSwitch : MonoBehaviour, IInteractable
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private string _circuitId = "L3_CEILING";
        [SerializeField] private Renderer _indicator;
        [SerializeField] private Color _onGlow = new Color(0.2f, 1.6f, 0.4f);
        [SerializeField] private Color _offGlow = new Color(1.8f, 0.15f, 0.1f);
        [SerializeField] private AudioClip _click;

        private LightCircuit _circuit;
        private MaterialPropertyBlock _block;

        internal void Configure(string circuitId, Renderer indicator)
        {
            _circuitId = circuitId;
            _indicator = indicator;
            _circuit = null;
        }

        private LightCircuit Circuit
        {
            get
            {
                if (_circuit == null)
                {
                    foreach (LightCircuit c in FindObjectsByType<LightCircuit>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        if (c.CircuitId == _circuitId)
                        {
                            _circuit = c;
                            break;
                        }
                    }
                }

                return _circuit;
            }
        }

        private void Start()
        {
            if (Circuit != null)
            {
                Circuit.Changed += _ => UpdateIndicator();
            }

            UpdateIndicator();
        }

        public string GetPrompt(InteractionController interactor)
        {
            if (Circuit == null)
            {
                return null;
            }

            return Circuit.IsOn ? "Lights off" : "Lights on";
        }

        public void Interact(InteractionController interactor) => Press();

        /// <summary>Toggles every circuit with this switch's ID.</summary>
        public void Press()
        {
            if (Circuit == null)
            {
                return;
            }

            LightCircuit.Toggle(_circuitId);
            Sfx.PlayAt(_click, transform.position, 0.7f);
            UpdateIndicator();
        }

        private void UpdateIndicator()
        {
            if (_indicator == null || Circuit == null)
            {
                return;
            }

            _block ??= new MaterialPropertyBlock();
            _indicator.GetPropertyBlock(_block);
            _block.SetColor(EmissionId, Circuit.IsOn ? _onGlow : _offGlow);
            _indicator.SetPropertyBlock(_block);
        }
    }
}
