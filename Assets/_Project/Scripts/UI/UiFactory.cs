using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VentsAndVending.UI
{
    /// <summary>
    /// Helpers for building placeholder UI in code (GDD §15.8: dark translucent panels, bright accents, compact type).
    /// </summary>
    internal static class UiFactory
    {
        public static readonly Color PanelColor = new Color(0.04f, 0.05f, 0.07f, 0.88f);
        public static readonly Color RowColor = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color RowHoverColor = new Color(0.1f, 0.9f, 1f, 0.18f);
        public static readonly Color Accent = new Color(0.1f, 0.9f, 1f, 1f);
        public static readonly Color TextColor = new Color(0.88f, 0.9f, 0.92f, 1f);
        public static readonly Color MutedText = new Color(0.6f, 0.64f, 0.68f, 1f);

        public static Canvas CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        public static Image Image(Transform parent, string name, Color color)
        {
            RectTransform rt = Rect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left, Color? color = null)
        {
            RectTransform rt = Rect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = color ?? TextColor;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static VerticalLayoutGroup VerticalList(RectTransform rt, float spacing)
        {
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <summary>Flat button with a centred label. Returns the Button so callers can toggle interactable.</summary>
        public static Button Button(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick,
            Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            Image bg = Image(parent, name, new Color(0.1f, 0.9f, 1f, 0.16f));
            Place(bg.rectTransform, anchor, pivot, position, size);
            var button = bg.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(onClick);

            TextMeshProUGUI text = Text(bg.transform, "Label", label, 22f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            text.fontStyle = FontStyles.Bold;
            return button;
        }

        public static LayoutElement Layout(GameObject go, float preferredHeight = -1f, float preferredWidth = -1f, float flexibleWidth = -1f)
        {
            if (!go.TryGetComponent(out LayoutElement element))
            {
                element = go.AddComponent<LayoutElement>();
            }

            element.preferredHeight = preferredHeight;
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
            return element;
        }
    }
}
