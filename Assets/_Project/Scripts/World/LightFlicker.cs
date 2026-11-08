using UnityEngine;

namespace VentsAndVending.World
{
    /// <summary>
    /// A tired bulb (Docs/Design/TunnelNetwork.md §4 "warm flickering work lights"): steady most of the time, then a short
    /// burst of stutters every so often. Dims the bulb's emissive renderer with the light.
    /// </summary>
    public sealed class LightFlicker : MonoBehaviour
    {
        [SerializeField] private Light _light;
        [SerializeField] private Renderer _bulb;
        [SerializeField] private Vector2 _calmSeconds = new Vector2(4f, 11f);
        [SerializeField] private AudioSource _buzz;

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _block;
        private Color _emission;
        private float _intensity;
        private float _timer;
        private int _stutters;
        private float _stutterTimer;

        internal void Configure(Light light, Renderer bulb, AudioSource buzz)
        {
            _light = light;
            _bulb = bulb;
            _buzz = buzz;
        }

        private void Awake()
        {
            if (_light != null)
            {
                _intensity = _light.intensity;
            }
            if (_bulb != null && _bulb.sharedMaterial != null && _bulb.sharedMaterial.HasProperty(EmissionId))
            {
                _emission = _bulb.sharedMaterial.GetColor(EmissionId);
            }
            _timer = Random.Range(_calmSeconds.x, _calmSeconds.y);
        }

        private void Update()
        {
            if (_stutters > 0)
            {
                _stutterTimer -= Time.deltaTime;
                if (_stutterTimer <= 0f)
                {
                    _stutters--;
                    _stutterTimer = Random.Range(0.03f, 0.12f);
                    Set(_stutters % 2 == 1 ? Random.Range(0.05f, 0.35f) : 1f);
                    if (_stutters == 0)
                    {
                        Set(1f);
                    }
                }
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = Random.Range(_calmSeconds.x, _calmSeconds.y);
                _stutters = 2 * Random.Range(2, 5);
                _stutterTimer = 0f;
                if (_buzz != null)
                {
                    _buzz.pitch = Random.Range(0.9f, 1.15f);
                    _buzz.Play();
                }
            }
        }

        private void Set(float level)
        {
            if (_light != null)
            {
                _light.intensity = _intensity * level;
            }
            if (_bulb != null)
            {
                _block ??= new MaterialPropertyBlock();
                _bulb.GetPropertyBlock(_block);
                _block.SetColor(EmissionId, _emission * level);
                _bulb.SetPropertyBlock(_block);
            }
        }
    }
}
