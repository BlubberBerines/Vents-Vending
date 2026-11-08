using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Core;
using VentsAndVending.Saving;

namespace VentsAndVending.UI
{
    /// <summary>Pause menu (GDD §17.4): resume, save, settings, back to the main menu, quit. Freezes time while open.</summary>
    public sealed class PauseMenu : ModalScreen
    {
        private GameObject _settingsRoot;
        private SettingsView _settings;
        private TextMeshProUGUI _status;
        private TextMeshProUGUI _where;
        private Image _settingsButton;

        protected override void UpdateWhileClosed()
        {
            if (Interactor.Input.PausePressed && Time.frameCount != LastClosedFrame && !Interactor.State.IsInteracting && BeginOpen())
            {
                Time.timeScale = 0f;
                _status.text = string.Empty;
                var room = Tunnels.TunnelManager.Instance != null ? Tunnels.TunnelManager.Instance.CurrentRoom : null;
                _where.text = room != null ? room.Label : "Vents & Vending";
                ShowSettings(false);
            }
        }

        protected override void OnClosing() => Time.timeScale = 1f;

        private void ShowSettings(bool show)
        {
            _settings.gameObject.SetActive(show);
            if (_settingsButton != null)
            {
                var outline = _settingsButton.GetComponent<Outline>() ?? _settingsButton.gameObject.AddComponent<Outline>();
                outline.effectColor = UiFactory.Hazard;
                outline.effectDistance = new Vector2(2f, -2f);
                outline.enabled = show;
            }
        }

        private void SaveNow()
        {
            bool ok = SaveManager.Instance != null && SaveManager.Instance.Save(null);
            _status.text = ok ? "<color=#5CFF8A>Game saved</color>" : "<color=#FF6B5E>Couldn't save</color>";
        }

        private void ToMainMenu()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Save(null);
            }
            SceneFlow.ToMainMenu();
        }

        private void QuitGame()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Save(null);
            }
            SceneFlow.Quit();
        }

        protected override GameObject BuildUi()
        {
            Canvas canvas = UiFactory.CreateCanvas(transform, "Pause Canvas", 50);
            UiFactory.Dim(canvas.transform, 0.7f);
            Vector2 tl = new Vector2(0f, 1f);

            TextMeshProUGUI title = UiFactory.Display(canvas.transform, "Title", "Paused", 76f);
            UiFactory.Place(title.rectTransform, tl, tl, new Vector2(110f, -100f), new Vector2(600f, 90f));
            _where = UiFactory.Label(canvas.transform, "Where", string.Empty, new Color(0.79f, 0.82f, 0.85f));
            UiFactory.Place(_where.rectTransform, tl, tl, new Vector2(114f, -196f), new Vector2(600f, 24f));

            RectTransform buttons = UiFactory.Rect("Buttons", canvas.transform);
            UiFactory.Place(buttons, tl, tl, new Vector2(110f, -250f), new Vector2(460f, 420f));
            var list = UiFactory.VerticalList(buttons, 16f);
            list.childAlignment = TextAnchor.UpperLeft;
            Button resume = UiFactory.Button(buttons, "Resume", "Resume", Close, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, ButtonStyle.Primary);
            Button save = UiFactory.Button(buttons, "Save", "Save game", SaveNow, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Button settings = UiFactory.Button(buttons, "Settings", "Settings", () => ShowSettings(!_settings.gameObject.activeSelf), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Button menu = UiFactory.Button(buttons, "MainMenu", "Save & main menu", ToMainMenu, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Button quit = UiFactory.Button(buttons, "Quit", "Save & quit", QuitGame, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            foreach (Button b in new[] { resume, save, settings, menu, quit })
            {
                UiFactory.Layout(b.gameObject, preferredHeight: 64f);
                b.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
                b.GetComponentInChildren<TextMeshProUGUI>().rectTransform.offsetMin = new Vector2(24f, 4f);
            }
            _settingsButton = settings.GetComponent<Image>();

            _status = UiFactory.Text(canvas.transform, "Status", string.Empty, 22f);
            UiFactory.Place(_status.rectTransform, tl, tl, new Vector2(114f, -700f), new Vector2(460f, 36f));

            RectTransform hints = UiFactory.Rect("Hints", canvas.transform);
            UiFactory.Place(hints, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(110f, 50f), new Vector2(900f, 34f));
            UiFactory.HRow(hints, 28f);
            UiFactory.KeyHint(hints, "Esc", "Resume");
            TextMeshProUGUI auto = UiFactory.Text(hints, "Autosave", "Autosaves every 2 minutes", 18f, TextAlignmentOptions.Left, UiFactory.MutedText);
            auto.textWrappingMode = TextWrappingModes.NoWrap;

            _settings = SettingsView.Create(canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(230f, 20f), () => ShowSettings(false));
            _settings.gameObject.SetActive(false);
            return canvas.gameObject;
        }
    }
}
