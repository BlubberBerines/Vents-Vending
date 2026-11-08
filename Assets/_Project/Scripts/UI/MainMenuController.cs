using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Core;
using VentsAndVending.Player;
using VentsAndVending.Saving;

namespace VentsAndVending.UI
{
    /// <summary>Main menu (GDD §17.1): Continue, New Game, Settings, Quit — over a small diorama of the station.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private GameObject _menu;
        private SettingsView _settings;
        private TextMeshProUGUI _note;

        private void Awake()
        {
            Time.timeScale = 1f;
            Canvas canvas = UiFactory.CreateCanvas(transform, "Main Menu Canvas", 0);
            Transform root = canvas.transform;

            var shade = UiFactory.Image(root, "LeftShade", new Color(0.02f, 0.02f, 0.05f, 0.72f));
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = new Vector2(0.42f, 1f);
            shade.rectTransform.offsetMin = Vector2.zero;
            shade.rectTransform.offsetMax = Vector2.zero;

            var title = UiFactory.Text(root, "Title", "VENTS &\nVENDING", 110f, TextAlignmentOptions.Left, UiFactory.Accent);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(90f, -90f), new Vector2(800f, 270f));
            title.fontStyle = FontStyles.Bold;
            title.lineSpacing = -20f;

            var tagline = UiFactory.Text(root, "Tagline", "Business is booming. Unfortunately, so is the station.", 26f, TextAlignmentOptions.Left, new Color(1f, 0.6f, 0.95f));
            UiFactory.Place(tagline.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(92f, -370f), new Vector2(800f, 40f));

            _menu = UiFactory.Rect("Buttons", root).gameObject;
            Vector2 a = new Vector2(0f, 0.5f);
            bool hasSave = SaveManager.HasSave();
            Button cont = UiFactory.Button(_menu.transform, "Continue", "Continue", SceneFlow.ContinueGame, a, a, new Vector2(90f, 20f), new Vector2(420f, 64f));
            cont.interactable = hasSave;
            UiFactory.Button(_menu.transform, "NewGame", "New game", NewGame, a, a, new Vector2(90f, -60f), new Vector2(420f, 64f));
            UiFactory.Button(_menu.transform, "Settings", "Settings", () => ShowSettings(true), a, a, new Vector2(90f, -140f), new Vector2(420f, 64f));
            UiFactory.Button(_menu.transform, "Quit", "Quit", SceneFlow.Quit, a, a, new Vector2(90f, -220f), new Vector2(420f, 64f));

            _note = UiFactory.Text(_menu.transform, "Note",
                hasSave ? "<color=#FFB040>New game will overwrite your save the next time it saves.</color>" : string.Empty,
                18f, TextAlignmentOptions.Left, UiFactory.MutedText);
            UiFactory.Place(_note.rectTransform, a, a, new Vector2(92f, -290f), new Vector2(700f, 30f));

            var controls = UiFactory.Text(root, "Controls",
                "WASD move · Mouse look · Shift sprint · Space jump · C crouch · Z crawl · E interact · Tab inventory · Esc pause",
                18f, TextAlignmentOptions.Left, UiFactory.MutedText);
            UiFactory.Place(controls.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(92f, 60f), new Vector2(1400f, 30f));

            var version = UiFactory.Text(root, "Version", $"Prototype build {Application.version}", 16f, TextAlignmentOptions.Right, UiFactory.MutedText);
            UiFactory.Place(version.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(400f, 24f));

            _settings = SettingsView.Create(root, new Vector2(0.5f, 0.5f), new Vector2(240f, 0f), () => ShowSettings(false));
            _settings.gameObject.SetActive(false);
        }

        private void Start() => PlayerCamera.SetCursorLocked(false);

        private static void NewGame() => SceneFlow.NewGame();

        private void ShowSettings(bool show)
        {
            _settings.gameObject.SetActive(show);
            _menu.SetActive(!show);
        }
    }
}
