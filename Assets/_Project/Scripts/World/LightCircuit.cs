using System;
using UnityEngine;
using VentsAndVending.Tunnels;

namespace VentsAndVending.World
{
    /// <summary>
    /// A lighting circuit (e.g. "L3_CEILING"): its lights and the glowing panels of its fixtures switch together.
    /// Off = lights disabled and the emissive material slots of the listed renderers blacked out with a property block
    /// (the shared materials are never modified). Toggled by a <see cref="LightSwitch"/>; the position is saved as a
    /// tunnel switch with the circuit's ID when a <see cref="TunnelManager"/> exists (default: on).
    /// </summary>
    public sealed class LightCircuit : MonoBehaviour
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private string _circuitId = "L3_CEILING";
        [SerializeField] private Light[] _lights = new Light[0];
        [SerializeField] private Renderer[] _emissives = new Renderer[0];
        [SerializeField] private bool _on = true;

        private MaterialPropertyBlock _block;

        public string CircuitId => _circuitId;
        public bool IsOn => _on;

        /// <summary>Raised after the circuit changes (new state).</summary>
        public event Action<bool> Changed;

        internal void Configure(string circuitId, Light[] lights, Renderer[] emissives)
        {
            _circuitId = circuitId;
            _lights = lights;
            _emissives = emissives;
        }

        private void Start()
        {
            if (TunnelManager.Instance != null)
            {
                TunnelManager.Instance.Restored += Restore;
            }

            Restore();
        }

        private void OnDestroy()
        {
            if (TunnelManager.Instance != null)
            {
                TunnelManager.Instance.Restored -= Restore;
            }
        }

        private void Restore()
        {
            bool on = TunnelManager.Instance != null ? TunnelManager.Instance.GetSwitch(_circuitId, true) : _on;
            Apply(on);
        }

        public void Toggle() => SetOn(!_on);

        public void SetOn(bool on)
        {
            if (TunnelManager.Instance != null)
            {
                TunnelManager.Instance.SetSwitch(_circuitId, on, true);
            }

            Apply(on);
        }

        /// <summary>Toggles every circuit with this ID in the open scenes. False if there is none.</summary>
        public static bool Toggle(string circuitId)
        {
            bool found = false;
            foreach (LightCircuit c in FindObjectsByType<LightCircuit>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c._circuitId == circuitId)
                {
                    c.Toggle();
                    found = true;
                }
            }

            return found;
        }

        private void Apply(bool on)
        {
            bool changed = on != _on;
            _on = on;
            foreach (Light l in _lights)
            {
                if (l != null)
                {
                    l.enabled = on;
                }
            }

            _block ??= new MaterialPropertyBlock();
            foreach (Renderer r in _emissives)
            {
                if (r == null)
                {
                    continue;
                }

                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (!IsEmissive(mats[i]))
                    {
                        continue;
                    }

                    _block.Clear();
                    if (!on)
                    {
                        _block.SetColor(EmissionId, Color.black);
                    }

                    r.SetPropertyBlock(_block, i);
                }
            }

            if (changed)
            {
                Changed?.Invoke(on);
            }
        }

        private static bool IsEmissive(Material m) =>
            m != null && m.HasProperty(EmissionId) && m.GetColor(EmissionId).maxColorComponent > 0.01f;
    }
}
