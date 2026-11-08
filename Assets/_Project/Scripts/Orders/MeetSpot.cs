using UnityEngine;

namespace VentsAndVending.Orders
{
    public enum MeetRisk { Low, Medium, High }

    /// <summary>
    /// A named hand-off point in a public area (A/B/C). Its floor tag is always faintly visible; when a deal is booked
    /// it lights up, and pulses while the customer is there, so you can find it without a map.
    /// </summary>
    public sealed class MeetSpot : MonoBehaviour
    {
        public enum Mark { Idle, Booked, CustomerWaiting }

        [SerializeField] private string _spotId = "A";
        [SerializeField] private string _displayName = "Behind Glorp-Mart";
        [SerializeField] private MeetRisk _risk = MeetRisk.Low;
        [SerializeField, TextArea] private string _hint = "Out of the drone's usual sightlines.";
        [SerializeField] private Renderer[] _marks = new Renderer[0];
        [SerializeField] private GameObject _beacon;
        [SerializeField] private Light _glow;

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _block;
        private Mark _mark = Mark.Idle;

        public string SpotId => _spotId;
        public string DisplayName => _displayName;
        public MeetRisk Risk => _risk;
        public string Hint => _hint;
        public string Label => $"Spot {_spotId} · {_displayName}";

        internal void Configure(string id, string displayName, MeetRisk risk, string hint, Renderer[] marks, GameObject beacon, Light glow)
        {
            _spotId = id;
            _displayName = displayName;
            _risk = risk;
            _hint = hint;
            _marks = marks;
            _beacon = beacon;
            _glow = glow;
        }

        public static Color RiskColor(MeetRisk risk) => risk switch
        {
            MeetRisk.Low => new Color(0.35f, 1f, 0.45f),
            MeetRisk.Medium => new Color(1f, 0.8f, 0.2f),
            _ => new Color(1f, 0.35f, 0.3f),
        };

        private void Start() => SetMark(Mark.Idle, true);

        public void SetMark(Mark mark, bool force = false)
        {
            if (mark == _mark && !force)
            {
                return;
            }
            _mark = mark;
            if (_beacon != null)
            {
                _beacon.SetActive(mark != Mark.Idle);
            }
            if (_glow != null)
            {
                _glow.enabled = mark != Mark.Idle;
            }
            if (mark == Mark.Idle)
            {
                SetEmission(0.25f);
            }
        }

        private void Update()
        {
            if (_mark == Mark.Idle)
            {
                return;
            }
            float level = _mark == Mark.CustomerWaiting ? 1.2f + Mathf.Sin(Time.time * 5f) * 0.8f : 1.2f;
            SetEmission(level);
            if (_glow != null)
            {
                _glow.intensity = level * 1.2f;
            }
        }

        private void SetEmission(float level)
        {
            _block ??= new MaterialPropertyBlock();
            Color c = new Color(0.2f, 0.95f, 1f) * level;
            foreach (Renderer r in _marks)
            {
                if (r == null)
                {
                    continue;
                }
                r.GetPropertyBlock(_block);
                _block.SetColor(EmissionId, c);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
