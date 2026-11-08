using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Core;
using VentsAndVending.Player;
using VentsAndVending.Saving;

namespace VentsAndVending.UI
{
    /// <summary>Main menu (GDD §17.4) in the Ink &amp; Hazard style: logo, continue (with what's in your save), new game, settings, quit.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private GameObject _menu;
        private SettingsView _settings;

        private void Awake()
        {
            Time.timeScale = 1f;
            Canvas canvas = UiFactory.CreateCanvas(transform, "Main Menu Canvas", 0);
            Transform root = canvas.transform;

            Image shade = UiFactory.Image(root, "LeftShade", new Color(0.03f, 0.035f, 0.047f, 0.74f), false);
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = new Vector2(0.44f, 1f);
            shade.rectTransform.offsetMin = Vector2.zero;
            shade.rectTransform.offsetMax = Vector2.zero;

            Vector2 tl = new Vector2(0f, 1f);
            TextMeshProUGUI logo = UiFactory.Display(root, "Logo", "VENTS <color=#FFD24D>&</color>\nVENDING", 118f);
            UiFactory.Place(logo.rectTransform, tl, tl, new Vector2(108f, -96f), new Vector2(800f, 240f));
            logo.lineSpacing = -14f;
            TextMeshProUGUI tagline = UiFactory.Label(root, "Tagline", "black-market logistics · from the ducts", new Color(0.79f, 0.82f, 0.85f));
            UiFactory.Place(tagline.rectTransform, tl, tl, new Vector2(112f, -350f), new Vector2(700f, 26f));

            _menu = UiFactory.Rect("Buttons", root).gameObject;
            var menuRect = (RectTransform)_menu.transform;
            UiFactory.Place(menuRect, tl, tl, new Vector2(108f, -414f), new Vector2(560f, 440f));
            var list = UiFactory.VerticalList(menuRect, 18f);
            list.childAlignment = TextAnchor.UpperLeft;

            bool hasSave = SaveManager.HasSave();
            Button cont = UiFactory.Button(menuRect, "Continue", "Continue", SceneFlow.ContinueGame, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero,
                hasSave ? ButtonStyle.Primary : ButtonStyle.Ghost);
            cont.interactable = hasSave;
            UiFactory.Layout(cont.gameObject, preferredHeight: 84f);
            var contLabel = cont.GetComponentInChildren<TextMeshProUGUI>();
            contLabel.alignment = TextAlignmentOptions.MidlineLeft;
            contLabel.rectTransform.offsetMin = new Vector2(26f, 4f);
            contLabel.textWrappingMode = TextWrappingModes.Normal;
            contLabel.text = hasSave ? $"Continue\n<size=60%><color=#4A3D10>{SaveSummary()}</color></size>" : "Continue\n<size=60%>no save yet</size>";

            Button newGame = UiFactory.Button(menuRect, "NewGame", "New game", NewGame, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, hasSave ? ButtonStyle.Ghost : ButtonStyle.Primary);
            Button settings = UiFactory.Button(menuRect, "Settings", "Settings", () => ShowSettings(true), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Button quit = UiFactory.Button(menuRect, "Quit", "Quit to desktop", SceneFlow.Quit, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            foreach (Button b in new[] { newGame, settings, quit })
            {
                UiFactory.Layout(b.gameObject, preferredHeight: 64f);
                var t = b.GetComponentInChildren<TextMeshProUGUI>();
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.rectTransform.offsetMin = new Vector2(26f, 4f);
            }
            if (hasSave)
            {
                TextMeshProUGUI note = UiFactory.Text(menuRect, "Note", "New game replaces your save the next time it saves.", 17f, TextAlignmentOptions.Left, new Color(1f, 0.69f, 0.31f));
                UiFactory.Layout(note.gameObject, preferredHeight: 28f);
            }

            // What's new card (slightly crooked, like a sticker)
            Image card = UiFactory.Panel(root, "WhatsNew", null, 6f, 16f);
            UiFactory.Place(card.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-96f, 96f), new Vector2(470f, 210f));
            card.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -1.5f);
            TextMeshProUGUI head = UiFactory.Label(card.transform, "Head", "New this build", UiFactory.Hazard);
            UiFactory.Place(head.rectTransform, tl, tl, new Vector2(26f, -22f), new Vector2(400f, 24f));
            TextMeshProUGUI items = UiFactory.Text(card.transform, "Items",
                "<b>VentNet orders</b> <color=#9AA3AD>— customers message the den PC</color>\n" +
                "<b>Concourse Lounge</b> <color=#9AA3AD>— richer clients, behind Fan 17</color>\n" +
                "<b>Meet spots A · B · C</b> <color=#9AA3AD>— pick your risk</color>", 20f, TextAlignmentOptions.TopLeft);
            UiFactory.Place(items.rectTransform, tl, tl, new Vector2(26f, -58f), new Vector2(420f, 140f));
            items.lineSpacing = 18f;

            RectTransform hints = UiFactory.Rect("Hints", root);
            UiFactory.Place(hints, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-96f, -80f), new Vector2(600f, 34f));
            UiFactory.HRow(hints, 26f, TextAnchor.MiddleRight);
            UiFactory.KeyHint(hints, "Mouse", "Select");
            UiFactory.KeyHint(hints, "Esc", "Back");

            TextMeshProUGUI version = UiFactory.Label(root, "Version", $"v{Application.version} · Ordinance 84-B compliant (allegedly)");
            UiFactory.Place(version.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(112f, 52f), new Vector2(900f, 24f));

            _settings = SettingsView.Create(root, new Vector2(0.5f, 0.5f), new Vector2(330f, 0f), () => ShowSettings(false));
            _settings.gameObject.SetActive(false);
        }

        private void Start() => PlayerCamera.SetCursorLocked(false);

        private void Update()
        {
            if (_settings.gameObject.activeSelf && UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ShowSettings(false);
            }
        }

        private static void NewGame() => SceneFlow.NewGame();

        private void ShowSettings(bool show)
        {
            _settings.gameObject.SetActive(show);
            _menu.SetActive(!show);
        }

        /// <summary>"106 CC · 1 deal booked · saved 2 min ago" from the save file (best effort).</summary>
        private static string SaveSummary()
        {
            try
            {
                string path = SaveManager.PathFor(0);
                if (!File.Exists(path) || !SaveCodec.TryParse(File.ReadAllText(path), out SaveData data, out _))
                {
                    return "saved game";
                }
                int deals = 0;
                foreach (var o in data.Orders.Orders)
                {
                    if (o.IsActive)
                    {
                        deals++;
                    }
                }
                string when = "";
                if (DateTime.TryParse(data.SavedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime saved))
                {
                    TimeSpan ago = DateTime.UtcNow - saved.ToUniversalTime();
                    when = ago.TotalMinutes < 1 ? " · saved just now" : ago.TotalHours < 1 ? $" · saved {(int)ago.TotalMinutes} min ago"
                        : ago.TotalDays < 1 ? $" · saved {(int)ago.TotalHours} h ago" : $" · saved {saved.ToLocalTime():d MMM}";
                }
                return $"{data.Credits} CC{(deals > 0 ? $" · {deals} deal{(deals == 1 ? "" : "s")} booked" : "")}{when}";
            }
            catch (Exception)
            {
                return "saved game";
            }
        }
    }
}
