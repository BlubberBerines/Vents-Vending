using UnityEngine;
using VentsAndVending.Interaction;

namespace VentsAndVending.Tunnels
{
    /// <summary>
    /// A labelled breaker on a power distribution wall (Docs/Design/TunnelNetwork.md §6 "Fan isolation"). The state is
    /// a switch in <see cref="TunnelManager"/>; this component is the handle, lamp and label. Jammed breakers are jokes.
    /// </summary>
    public sealed class BreakerSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _breakerId = TunnelIds.FanBreaker;
        [SerializeField] private string _controls = "FAN 17";
        [SerializeField] private bool _startsOn = true;
        [SerializeField] private bool _jammed;
        [SerializeField] private string _jammedMessage = "Jammed. Probably for the best.";
        [SerializeField] private Transform _lever;
        [SerializeField] private Renderer _lamp;
        [SerializeField] private float _leverOnAngle = -35f;
        [SerializeField] private float _leverOffAngle = 35f;
        [SerializeField] private AudioClip _clunk;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _block;
        private float _leverAngle;

        public string BreakerId => _breakerId;
        public bool IsOn => TunnelManager.Instance == null ? _startsOn : TunnelManager.Instance.GetSwitch(_breakerId, _startsOn);

        private void Start()
        {
            _leverAngle = IsOn ? _leverOnAngle : _leverOffAngle;
            ApplyVisuals();
        }

        private void Update()
        {
            float target = IsOn ? _leverOnAngle : _leverOffAngle;
            if (!Mathf.Approximately(_leverAngle, target))
            {
                _leverAngle = Mathf.MoveTowards(_leverAngle, target, 400f * Time.deltaTime);
                ApplyVisuals();
            }
        }

        public string GetPrompt(InteractionController interactor)
        {
            if (_jammed)
            {
                return $"Breaker: {_controls}";
            }
            return IsOn ? $"Switch off {_controls}" : $"Switch on {_controls}";
        }

        public void Interact(InteractionController interactor)
        {
            if (_jammed)
            {
                interactor.Notify(_jammedMessage);
                PlayClunk(0.6f, 0.5f);
                return;
            }
            if (TunnelManager.Instance == null)
            {
                return;
            }

            PlayClunk(on: !IsOn);
            bool on = !IsOn;
            TunnelManager.Instance.SetSwitch(_breakerId, on, _startsOn);
            interactor.Notify($"{_breakerId}: {(on ? "<color=#7CFF8A>ON</color>" : "<color=#FFB040>OFF</color>")}  ({_controls})");
            ApplyVisuals();
        }

        private void PlayClunk(float pitch = 1f, float volume = 1f, bool on = true) =>
            VentsAndVending.World.Sfx.PlayAt(_clunk, transform.position, volume, pitch * (on ? 1.08f : 0.92f));

        private void ApplyVisuals()
        {
            if (_lever != null)
            {
                _lever.localRotation = Quaternion.Euler(_leverAngle, 0f, 0f);
            }
            if (_lamp != null)
            {
                _block ??= new MaterialPropertyBlock();
                Color c = _jammed ? new Color(0.25f, 0.22f, 0.2f) : IsOn ? new Color(0.2f, 1f, 0.35f) : new Color(1f, 0.55f, 0.1f);
                _lamp.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, c * 0.6f);
                _block.SetColor(EmissionId, _jammed ? Color.black : c * 2f);
                _lamp.SetPropertyBlock(_block);
            }
        }
    }
}
