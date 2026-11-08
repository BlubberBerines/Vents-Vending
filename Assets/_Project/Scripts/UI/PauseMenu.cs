using TMPro;
using UnityEngine;
using VentsAndVending.Core;
using VentsAndVending.Saving;

namespace VentsAndVending.UI
{
    /// <summary>Esc / Start pause menu: freezes time; resume, save, settings, main menu, quit.</summary>
    public sealed class PauseMenu : ModalScreen
    {
        private GameObject _buttons;
        private SettingsView _settings;
        private TextMeshProUGUI _status;

        protected override void UpdateWhileClosed()
        {
            if (Interactor.Input.PausePressed && Time.frameCount != LastClosedFrame && !Interactor.State.IsInteracting && BeginOpen())
            {
                Time.timeScale = 0f;
                _status.text = string.Empty;
                ShowSettings(false);
            }
        }

        protected override void OnClosing() => Time.timeScale = 1f;

        private void ShowSettings(bool show)
        {
            _settings.gameObject.SetActive(show);
            _buttons.SetActive(!show);
        }

        private void SaveNow()
        {
            bool ok = SaveManager.Instance != null && SaveManager.Instance.Save(null);
            _status.text = ok ? "<color=#5CFF7A>Game saved</color>" : "<color=#FF6060>Couldn't save</color>";
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
            var dim = UiFactory.Image(canvas.transform, "Dim", new Color(0.02f, 0.02f, 0.05f, 0.75f));
            UiFactory.Stretch(dim.rectTransform);

            var title = UiFactory.Text(canvas.transform, "Title", "PAUSED", 64f, TextAlignmentOptions.Center, UiFactory.Accent);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(800f, 90f));
            title.fontStyle = FontStyles.Bold;

            _buttons = UiFactory.Rect("Buttons", canvas.transform).gameObject;
            var b = _buttons.transform;
            Vector2 c = new Vector2(0.5f, 0.5f);
            UiFactory.Button(b, "Resume", "Resume", Close, c, c, new Vector2(0f, 150f), new Vector2(380f, 60f));
            UiFactory.Button(b, "Save", "Save game", SaveNow, c, c, new Vector2(0f, 75f), new Vector2(380f, 60f));
            UiFactory.Button(b, "Settings", "Settings", () => ShowSettings(true), c, c, new Vector2(0f, 0f), new Vector2(380f, 60f));
            UiFactory.Button(b, "MainMenu", "Save & main menu", ToMainMenu, c, c, new Vector2(0f, -75f), new Vector2(380f, 60f));
            UiFactory.Button(b, "Quit", "Save & quit", QuitGame, c, c, new Vector2(0f, -150f), new Vector2(380f, 60f));

            _status = UiFactory.Text(canvas.transform, "Status", string.Empty, 24f, TextAlignmentOptions.Center);
            UiFactory.Place(_status.rectTransform, c, c, new Vector2(0f, -230f), new Vector2(600f, 40f));

            var hint = UiFactory.Text(canvas.transform, "Hint", "[ Esc ] Resume   ·   Autosaves every 2 minutes", 20f, TextAlignmentOptions.Center, UiFactory.MutedText);
            UiFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(800f, 30f));

            _settings = SettingsView.Create(canvas.transform, c, new Vector2(0f, -10f), () => ShowSettings(false));
            _settings.gameObject.SetActive(false);
            return canvas.gameObject;
        }
    }
}
