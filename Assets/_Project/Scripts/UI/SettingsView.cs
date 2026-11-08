using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Core;

namespace VentsAndVending.UI
{
    /// <summary>Settings rows (GDD §17.5 subset) shared by the main menu and pause menu. Changes apply immediately.</summary>
    internal sealed class SettingsView : MonoBehaviour
    {
        private sealed class ValueRow
        {
            public TextMeshProUGUI Value;
            public Image Fill;
        }

        private ValueRow _sensitivity;
        private ValueRow _fov;
        private ValueRow _volume;
        private (Image bg, TextMeshProUGUI label) _invert;
        private (Image bg, TextMeshProUGUI label) _holdCrouch;

        public static SettingsView Create(Transform parent, Vector2 anchor, Vector2 position, Action onBack)
        {
            Image bg = UiFactory.Panel(parent, "Settings", null, 8f, 18f);
            UiFactory.Place(bg.rectTransform, anchor, new Vector2(0.5f, 0.5f), position, new Vector2(820f, 600f));
            var view = bg.gameObject.AddComponent<SettingsView>();

            TextMeshProUGUI title = UiFactory.Heading(bg.transform, "Title", "Settings", 34f);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -24f), new Vector2(500f, 44f));
            TextMeshProUGUI sub = UiFactory.Label(bg.transform, "Sub", "Changes apply instantly");
            UiFactory.Place(sub.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -36f), new Vector2(360f, 24f));
            sub.alignment = TextAlignmentOptions.Right;

            view._sensitivity = view.Slider(bg.transform, 0, "Mouse sensitivity",
                () => GameSettings.MouseSensitivity -= 0.02f, () => GameSettings.MouseSensitivity += 0.02f);
            view._fov = view.Slider(bg.transform, 1, "Field of view",
                () => GameSettings.FieldOfView -= 5f, () => GameSettings.FieldOfView += 5f);
            view._volume = view.Slider(bg.transform, 2, "Master volume",
                () => GameSettings.MasterVolume -= 0.1f, () => GameSettings.MasterVolume += 0.1f);
            view._invert = view.Toggle(bg.transform, 3, "Invert Y", "Flip vertical look", () => GameSettings.InvertY = !GameSettings.InvertY);
            view._holdCrouch = view.Toggle(bg.transform, 4, "Hold to crouch / crawl", "Off = tap to toggle", () => GameSettings.HoldToCrouch = !GameSettings.HoldToCrouch);

            UiFactory.Button(bg.transform, "Defaults", "Reset to defaults", GameSettings.ResetToDefaults,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32f, 28f), new Vector2(260f, 52f));
            UiFactory.Button(bg.transform, "Back", "Back", () => onBack?.Invoke(),
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 28f), new Vector2(180f, 52f), ButtonStyle.Primary);

            GameSettings.Changed += view.Refresh;
            view.Refresh();
            return view;
        }

        private void OnDestroy() => GameSettings.Changed -= Refresh;

        private void Refresh()
        {
            _sensitivity.Value.text = $"{GameSettings.MouseSensitivity:0.00}";
            UiFactory.SetBar(_sensitivity.Fill, Mathf.InverseLerp(0.02f, 0.6f, GameSettings.MouseSensitivity));
            _fov.Value.text = $"{GameSettings.FieldOfView:0}°";
            UiFactory.SetBar(_fov.Fill, Mathf.InverseLerp(60f, 100f, GameSettings.FieldOfView));
            _volume.Value.text = $"{GameSettings.MasterVolume * 100f:0}%";
            UiFactory.SetBar(_volume.Fill, GameSettings.MasterVolume);
            SetToggle(_invert, GameSettings.InvertY);
            SetToggle(_holdCrouch, GameSettings.HoldToCrouch);
        }

        private static void SetToggle((Image bg, TextMeshProUGUI label) t, bool on)
        {
            t.bg.color = on ? UiFactory.Go : new Color(0.09f, 0.102f, 0.125f);
            t.label.text = on ? "ON" : "OFF";
            t.label.color = on ? new Color(0.03f, 0.075f, 0.047f) : UiFactory.MutedText;
        }

        private static float RowY(int index) => -104f - index * 74f;

        private ValueRow Slider(Transform parent, int index, string label, Action minus, Action plus)
        {
            float y = RowY(index);
            TextMeshProUGUI name = UiFactory.Text(parent, label, label, 22f);
            UiFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, y), new Vector2(260f, 48f));
            name.alignment = TextAlignmentOptions.MidlineLeft;

            Image fill = UiFactory.Bar(parent, $"{label} bar", 0.5f, UiFactory.Hazard);
            UiFactory.Place((RectTransform)fill.transform.parent, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(300f, y - 24f), new Vector2(250f, 16f));

            UiFactory.Button(parent, $"{label} -", "−", () => minus(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-196f, y), new Vector2(52f, 48f));
            TextMeshProUGUI value = UiFactory.Mono(parent, $"{label} value", string.Empty, 22f, TextAlignmentOptions.Center, UiFactory.Credit);
            UiFactory.Place(value.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-88f, y), new Vector2(100f, 48f));
            value.alignment = TextAlignmentOptions.Center;
            UiFactory.Button(parent, $"{label} +", "+", () => plus(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, y), new Vector2(52f, 48f));
            return new ValueRow { Value = value, Fill = fill };
        }

        private (Image, TextMeshProUGUI) Toggle(Transform parent, int index, string label, string help, Action toggle)
        {
            float y = RowY(index);
            TextMeshProUGUI name = UiFactory.Text(parent, label, label, 22f);
            UiFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, y), new Vector2(300f, 48f));
            name.alignment = TextAlignmentOptions.MidlineLeft;
            TextMeshProUGUI helpText = UiFactory.Text(parent, $"{label} help", help, 17f, TextAlignmentOptions.MidlineLeft, UiFactory.MutedText);
            UiFactory.Place(helpText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340f, y), new Vector2(250f, 48f));
            Button button = UiFactory.Button(parent, $"{label} toggle", "OFF", () => toggle(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, y), new Vector2(120f, 48f));
            return (button.GetComponent<Image>(), button.GetComponentInChildren<TextMeshProUGUI>());
        }
    }
}
