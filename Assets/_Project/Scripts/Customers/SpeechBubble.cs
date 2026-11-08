using TMPro;
using UnityEngine;

namespace VentsAndVending.Customers
{
    /// <summary>World-space text above a character's head. Shows a temporary line, otherwise a persistent one.</summary>
    public sealed class SpeechBubble : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _maxVisibleDistance = 14f;

        private string _persistent = string.Empty;
        private string _temporary = string.Empty;
        private float _temporaryUntil;
        private Camera _camera;

        public void Say(string line, float seconds = 4f)
        {
            _temporary = line;
            _temporaryUntil = Time.time + seconds;
        }

        public void SetPersistent(string line) => _persistent = line ?? string.Empty;

        private void LateUpdate()
        {
            if (_text == null)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    return;
                }
            }

            Vector3 toBubble = transform.position - _camera.transform.position;
            bool visible = toBubble.sqrMagnitude < _maxVisibleDistance * _maxVisibleDistance;
            string line = Time.time < _temporaryUntil ? _temporary : _persistent;
            _text.enabled = visible && !string.IsNullOrEmpty(line);
            if (!_text.enabled)
            {
                return;
            }

            _text.text = $"<mark=#000000B0 padding=\"18,18,8,8\">{line}</mark>";
            transform.rotation = Quaternion.LookRotation(toBubble.normalized, Vector3.up);
        }
    }
}
