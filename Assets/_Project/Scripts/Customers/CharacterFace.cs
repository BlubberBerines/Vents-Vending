using System;
using UnityEngine;

namespace VentsAndVending.Customers
{
    public enum CharacterMood
    {
        Neutral,
        Happy,
        Annoyed
    }

    /// <summary>
    /// Cartoon face animation (GDD §15.6 "readable facial expressions"): pupils follow the camera, eyes blink at
    /// random, and the mouth changes shape with mood. Pure transform tweaks — no rig needed.
    /// </summary>
    public sealed class CharacterFace : MonoBehaviour
    {
        [Serializable]
        public struct Eye
        {
            [Tooltip("Unscaled pivot at the eye centre; scaled on Y to blink.")]
            public Transform Root;
            [Tooltip("Pupil (with its catchlight as a child). Slides across the eye towards the camera.")]
            public Transform Pupil;
            [Tooltip("How far the pupil can slide, in the eye root's local units.")]
            public float Range;
        }

        [SerializeField] private Eye[] _eyes = new Eye[0];
        [SerializeField] private Transform _mouth;
        [SerializeField] private Vector2 _blinkInterval = new Vector2(2.2f, 6f);
        [SerializeField] private float _blinkDuration = 0.14f;
        [SerializeField] private float _lookRange = 9f;

        private Vector3[] _pupilBase;
        private Vector3[] _eyeScale;
        private Vector3 _mouthScale;
        private Quaternion _mouthRotation;
        private float _nextBlink;
        private float _blinkTime = -1f;
        private CharacterMood _mood;
        private float _moodUntil;
        private Camera _camera;

        public CharacterMood Mood => _mood;

        private void Awake()
        {
            _pupilBase = new Vector3[_eyes.Length];
            _eyeScale = new Vector3[_eyes.Length];
            for (int i = 0; i < _eyes.Length; i++)
            {
                _pupilBase[i] = _eyes[i].Pupil != null ? _eyes[i].Pupil.localPosition : Vector3.zero;
                _eyeScale[i] = _eyes[i].Root != null ? _eyes[i].Root.localScale : Vector3.one;
            }

            if (_mouth != null)
            {
                _mouthScale = _mouth.localScale;
                _mouthRotation = _mouth.localRotation;
            }

            _nextBlink = Time.time + UnityEngine.Random.Range(_blinkInterval.x, _blinkInterval.y);
        }

        /// <summary>Show a mood for a while, then relax back to neutral.</summary>
        public void SetMood(CharacterMood mood, float seconds = 2.5f)
        {
            _mood = mood;
            _moodUntil = Time.time + seconds;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_mood != CharacterMood.Neutral && Time.time > _moodUntil)
            {
                _mood = CharacterMood.Neutral;
            }

            UpdateLook();
            UpdateBlink();
            UpdateMouth();
        }

        private void UpdateLook()
        {
            bool track = _camera != null && (_camera.transform.position - transform.position).sqrMagnitude < _lookRange * _lookRange;
            for (int i = 0; i < _eyes.Length; i++)
            {
                Eye eye = _eyes[i];
                if (eye.Root == null || eye.Pupil == null)
                {
                    continue;
                }

                Vector3 goal = _pupilBase[i];
                if (track)
                {
                    Vector3 local = eye.Root.InverseTransformPoint(_camera.transform.position).normalized;
                    goal += new Vector3(local.x, local.y, 0f) * eye.Range;
                }
                eye.Pupil.localPosition = Vector3.Lerp(eye.Pupil.localPosition, goal, 1f - Mathf.Exp(-12f * Time.deltaTime));
            }
        }

        private void UpdateBlink()
        {
            if (_blinkTime < 0f && Time.time >= _nextBlink)
            {
                _blinkTime = 0f;
            }

            float squash = 1f;
            if (_blinkTime >= 0f)
            {
                _blinkTime += Time.deltaTime;
                float t = _blinkTime / _blinkDuration;
                squash = Mathf.Lerp(1f, 0.08f, 1f - Mathf.Abs(t * 2f - 1f));
                if (t >= 1f)
                {
                    _blinkTime = -1f;
                    squash = 1f;
                    _nextBlink = Time.time + UnityEngine.Random.Range(_blinkInterval.x, _blinkInterval.y);
                }
            }

            // Annoyed characters squint
            float squint = _mood == CharacterMood.Annoyed ? 0.6f : 1f;
            for (int i = 0; i < _eyes.Length; i++)
            {
                if (_eyes[i].Root != null)
                {
                    Vector3 s = _eyeScale[i];
                    _eyes[i].Root.localScale = new Vector3(s.x, s.y * squash * squint, s.z);
                }
            }
        }

        private void UpdateMouth()
        {
            if (_mouth == null)
            {
                return;
            }

            Vector3 targetScale = _mood switch
            {
                CharacterMood.Happy => Vector3.Scale(_mouthScale, new Vector3(1.45f, 2.2f, 1f)),
                CharacterMood.Annoyed => Vector3.Scale(_mouthScale, new Vector3(0.65f, 0.45f, 1f)),
                _ => _mouthScale
            };
            Quaternion targetRot = _mood == CharacterMood.Annoyed ? _mouthRotation * Quaternion.Euler(0f, 0f, 12f) : _mouthRotation;
            float k = 1f - Mathf.Exp(-10f * Time.deltaTime);
            _mouth.localScale = Vector3.Lerp(_mouth.localScale, targetScale, k);
            _mouth.localRotation = Quaternion.Slerp(_mouth.localRotation, targetRot, k);
        }

        internal void Configure(Eye[] eyes, Transform mouth)
        {
            _eyes = eyes;
            _mouth = mouth;
        }
    }
}
