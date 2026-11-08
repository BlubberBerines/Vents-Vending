using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Interaction;

namespace VentsAndVending.UI
{
    /// <summary>
    /// Minimal HUD (GDD §17.2): crosshair, interaction prompt, feedback toasts and the inventory shortcut hint.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private InteractionController _interactor;
        [SerializeField] private float _toastSeconds = 2.2f;

        private Image _crosshair;
        private TextMeshProUGUI _prompt;
        private TextMeshProUGUI _toast;
        private TextMeshProUGUI _hint;
        private float _toastTimer;

        private void Awake()
        {
            if (_interactor == null)
            {
                _interactor = FindAnyObjectByType<InteractionController>();
            }

            Canvas canvas = UiFactory.CreateCanvas(transform, "HUD Canvas", 0);
            Transform root = canvas.transform;

            _crosshair = UiFactory.Image(root, "Crosshair", new Color(1f, 1f, 1f, 0.75f));
            UiFactory.Place(_crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 5f));
            _crosshair.raycastTarget = false;

            _prompt = UiFactory.Text(root, "Prompt", string.Empty, 30f, TextAlignmentOptions.Center);
            UiFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 50f));
            _prompt.fontStyle = FontStyles.Bold;

            _toast = UiFactory.Text(root, "Toast", string.Empty, 26f, TextAlignmentOptions.Center, UiFactory.Accent);
            UiFactory.Place(_toast.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(900f, 44f));

            _hint = UiFactory.Text(root, "Hint", "[ Tab ] Inventory", 20f, TextAlignmentOptions.Right, UiFactory.MutedText);
            UiFactory.Place(_hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 24f), new Vector2(400f, 30f));
        }

        private void OnEnable()
        {
            if (_interactor != null)
            {
                _interactor.Notified += ShowToast;
            }
        }

        private void OnDisable()
        {
            if (_interactor != null)
            {
                _interactor.Notified -= ShowToast;
            }
        }

        private void Update()
        {
            if (_interactor == null)
            {
                return;
            }

            bool busy = _interactor.State != null && _interactor.State.IsInteracting;
            _crosshair.enabled = !busy;
            _hint.enabled = !busy;

            string prompt = busy ? null : _interactor.CurrentPrompt;
            _prompt.text = string.IsNullOrEmpty(prompt) ? string.Empty : $"<color=#19E5FF>[ {_interactor.InteractKeyLabel} ]</color>  {prompt}";

            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.unscaledDeltaTime;
                _toast.alpha = Mathf.Clamp01(_toastTimer / 0.4f);
            }
        }

        private void ShowToast(string message)
        {
            _toast.text = message;
            _toast.alpha = 1f;
            _toastTimer = _toastSeconds;
        }
    }
}
