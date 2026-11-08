using System;
using TMPro;
using UnityEngine;
using VentsAndVending.Core;

namespace VentsAndVending.UI
{
    /// <summary>Settings rows (GDD §17.5 subset) shared by the main menu and pause menu. Changes apply immediately.</summary>
    internal sealed class SettingsView : MonoBehaviour
    {
        private TextMeshProUGUI _sensitivity;
        private TextMeshProUGUI _fov;
        private TextMeshProUGUI _invert;
        private TextMeshProUGUI _holdCrouch;
        private TextMeshProUGUI _volume;

        public static SettingsView Create(Transform parent, Vector2 anchor, Vector2 position, Action onBack)
        {
            var bg = UiFactory.Image(parent, "Settings", UiFactory.PanelColor);
            UiFactory.Place(bg.rectTransform, anchor, new Vector2(0.5f, 0.5f), position, new Vector2(760f, 540f));
            var view = bg.gameObject.AddComponent<SettingsView>();

            var title = UiFactory.Text(bg.transform, "Title", "Settings", 32f);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(500f, 44f));
            title.fontStyle = FontStyles.Bold;

            view._sensitivity = view.Row(bg.transform, 0, "Mouse sensitivity",
                () => GameSettings.MouseSensitivity -= 0.02f, () => GameSettings.MouseSensitivity += 0.02f);
            view._fov = view.Row(bg.transform, 1, "Field of view",
                () => GameSettings.FieldOfView -= 5f, () => GameSettings.FieldOfView += 5f);
            view._invert = view.Toggle(bg.transform, 2, "Invert Y", () => GameSettings.InvertY = !GameSettings.InvertY);
            view._holdCrouch = view.Toggle(bg.transform, 3, "Hold to crouch / crawl", () => GameSettings.HoldToCrouch = !GameSettings.HoldToCrouch);
            view._volume = view.Row(bg.transform, 4, "Master volume",
                () => GameSettings.MasterVolume -= 0.1f, () => GameSettings.MasterVolume += 0.1f);

            UiFactory.Button(bg.transform, "Defaults", "Reset to defaults", GameSettings.ResetToDefaults,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 24f), new Vector2(260f, 48f));
            UiFactory.Button(bg.transform, "Back", "Back", () => onBack?.Invoke(),
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(200f, 48f));

            GameSettings.Changed += view.Refresh;
            view.Refresh();
            return view;
        }

        private void OnDestroy() => GameSettings.Changed -= Refresh;

        private void Refresh()
        {
            _sensitivity.text = $"{GameSettings.MouseSensitivity:0.00}";
            _fov.text = $"{GameSettings.FieldOfView:0}°";
            _invert.text = GameSettings.InvertY ? "On" : "Off";
            _holdCrouch.text = GameSettings.HoldToCrouch ? "On" : "Off";
            _volume.text = $"{GameSettings.MasterVolume * 100f:0}%";
        }

        private TextMeshProUGUI Row(Transform parent, int index, string label, Action minus, Action plus)
        {
            float y = -92f - index * 70f;
            var name = UiFactory.Text(parent, label, label, 24f);
            UiFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, y), new Vector2(360f, 44f));

            UiFactory.Button(parent, $"{label} -", "-", () => minus(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-250f, y), new Vector2(60f, 44f));
            var value = UiFactory.Text(parent, $"{label} value", string.Empty, 26f, TextAlignmentOptions.Center, UiFactory.Accent);
            UiFactory.Place(value.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-104f, y), new Vector2(140f, 44f));
            UiFactory.Button(parent, $"{label} +", "+", () => plus(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, y), new Vector2(60f, 44f));
            return value;
        }

        private TextMeshProUGUI Toggle(Transform parent, int index, string label, Action toggle)
        {
            float y = -92f - index * 70f;
            var name = UiFactory.Text(parent, label, label, 24f);
            UiFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, y), new Vector2(420f, 44f));
            var button = UiFactory.Button(parent, $"{label} toggle", string.Empty, () => toggle(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, y), new Vector2(280f, 44f));
            return button.GetComponentInChildren<TextMeshProUGUI>();
        }
    }
}
