using UnityEngine;

namespace VentsAndVending.World
{
    /// <summary>
    /// Ambient punctuation (pipe ticks, distant knocks): plays a random clip every few seconds from a random point
    /// inside a box, so the sound wanders around the space rather than coming from one speaker.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class RandomOneShots : MonoBehaviour
    {
        [SerializeField] private AudioClip[] _clips = new AudioClip[0];
        [SerializeField] private Vector2 _interval = new Vector2(6f, 14f);
        [SerializeField] private Vector2 _volume = new Vector2(0.4f, 0.9f);
        [SerializeField] private Vector2 _pitch = new Vector2(0.85f, 1.15f);
        [SerializeField] private Vector3 _extents = new Vector3(1f, 0.5f, 3f);

        private AudioSource _source;
        private Vector3 _origin;
        private float _timer;

        internal void Configure(AudioClip[] clips, Vector2 interval, Vector2 volume, Vector3 extents)
        {
            _clips = clips;
            _interval = interval;
            _volume = volume;
            _extents = extents;
        }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _origin = transform.localPosition;
            _timer = Random.Range(_interval.x * 0.3f, _interval.y);
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f || _clips.Length == 0)
            {
                return;
            }

            _timer = Random.Range(_interval.x, _interval.y);
            transform.localPosition = _origin + Vector3.Scale(Random.insideUnitSphere, _extents);
            _source.pitch = Random.Range(_pitch.x, _pitch.y);
            _source.PlayOneShot(_clips[Random.Range(0, _clips.Length)], Random.Range(_volume.x, _volume.y));
        }
    }
}
