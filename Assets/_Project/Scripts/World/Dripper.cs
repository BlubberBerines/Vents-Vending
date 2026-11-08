using UnityEngine;

namespace VentsAndVending.World
{
    /// <summary>
    /// A leaky pipe: lets a drop go every few seconds (a single particle under gravity) and plays a drip sound at the
    /// puddle when it should land. Purely cosmetic.
    /// </summary>
    public sealed class Dripper : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _drops;
        [SerializeField] private AudioSource _impact;
        [SerializeField] private AudioClip[] _clips = new AudioClip[0];
        [SerializeField] private Vector2 _interval = new Vector2(1.6f, 4.5f);
        [SerializeField] private float _fallHeight = 2f;

        private float _timer;
        private float _landIn = -1f;

        internal void Configure(ParticleSystem drops, AudioSource impact, AudioClip[] clips, float fallHeight, Vector2 interval)
        {
            _drops = drops;
            _impact = impact;
            _clips = clips;
            _fallHeight = fallHeight;
            _interval = interval;
        }

        private void Start() => _timer = Random.Range(0f, _interval.y);

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = Random.Range(_interval.x, _interval.y);
                if (_drops != null)
                {
                    _drops.Emit(1);
                }
                _landIn = Mathf.Sqrt(2f * _fallHeight / Mathf.Abs(Physics.gravity.y));
            }

            if (_landIn >= 0f)
            {
                _landIn -= Time.deltaTime;
                if (_landIn < 0f && _impact != null && _clips.Length > 0)
                {
                    _impact.pitch = Random.Range(0.85f, 1.2f);
                    _impact.PlayOneShot(_clips[Random.Range(0, _clips.Length)], Random.Range(0.6f, 1f));
                }
            }
        }
    }
}
