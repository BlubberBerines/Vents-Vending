using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VentsAndVending.UI
{
    public enum ButtonStyle
    {
        /// <summary>Neutral ink button.</summary>
        Ghost,
        /// <summary>Hazard yellow: the one main action on a screen.</summary>
        Primary,
        /// <summary>VentNet green: accept / go.</summary>
        Go,
        /// <summary>Alert red: destructive.</summary>
        Danger
    }

    /// <summary>
    /// Builds the code-made UI in the "Ink &amp; Hazard" style (UI mockup, Docs/Design/ArtStyleGuide.md): dark ink panels
    /// with a hard black outline and a chunky drop shadow, Rubik for words, JetBrains Mono for labels and numbers, one
    /// loud accent per screen. Falls back to flat rectangles if the theme hasn't been generated.
    /// </summary>
    internal static class UiFactory
    {
        // Palette
        public static readonly Color Ink = new Color(0.055f, 0.063f, 0.078f, 0.95f);
        public static readonly Color InkSolid = new Color(0.055f, 0.063f, 0.078f, 1f);
        public static readonly Color Paper = new Color(0.925f, 0.937f, 0.953f, 1f);
        public static readonly Color Hazard = new Color(1f, 0.824f, 0.302f, 1f);
        public static readonly Color Credit = new Color(0.247f, 0.878f, 1f, 1f);
        public static readonly Color Go = new Color(0.361f, 1f, 0.541f, 1f);
        public static readonly Color Alert = new Color(1f, 0.42f, 0.369f, 1f);
        public static readonly Color Row = new Color(0.078f, 0.09f, 0.11f, 1f);
        public static readonly Color Track = new Color(0.133f, 0.149f, 0.176f, 1f);

        // Back-compat names used by the screens
        public static readonly Color PanelColor = Ink;
        public static readonly Color RowColor = Row;
        public static readonly Color RowHoverColor = new Color(0.063f, 0.133f, 0.165f, 1f);
        public static readonly Color Accent = Credit;
        public static readonly Color TextColor = Paper;
        public static readonly Color MutedText = new Color(0.604f, 0.639f, 0.678f, 1f);

        private static UiTheme Theme => UiTheme.Current;

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

        /// <summary>A soft rounded fill (8 px corners), or a flat rect if <paramref name="rounded"/> is false.</summary>
        public static Image Image(Transform parent, string name, Color color, bool rounded = true)
        {
            RectTransform rt = Rect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            if (rounded && Theme != null && Theme.PanelFill != null)
            {
                image.sprite = Theme.PanelFill;
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 2f; // 8 px corners
            }
            return image;
        }

        /// <summary>Full-screen dim (flat, no corners).</summary>
        public static Image Dim(Transform parent, float alpha) => Stretched(Image(parent, "Dim", new Color(0.02f, 0.025f, 0.035f, alpha), false));

        private static Image Stretched(Image image)
        {
            Stretch(image.rectTransform);
            return image;
        }

        /// <summary>
        /// Ink panel: 16 px rounded corners, 2 px black outline, hard drop shadow (<paramref name="shadow"/> px down).
        /// <paramref name="radius"/> scales the corners (16 = default, 12 for buttons, 8 for small chips).
        /// </summary>
        public static Image Panel(Transform parent, string name, Color? color = null, float shadow = 6f, float radius = 16f)
        {
            RectTransform rt = Rect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color ?? Ink;
            if (Theme != null && Theme.Panel != null)
            {
                image.sprite = Theme.Panel;
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 16f / Mathf.Max(2f, radius);
            }
            if (shadow > 0f)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.5f);
                s.effectDistance = new Vector2(0f, -shadow);
                s.useGraphicAlpha = false;
            }
            return image;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left, Color? color = null)
        {
            RectTransform rt = Rect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (Theme != null && Theme.Body != null)
            {
                tmp.font = Theme.Body;
            }
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = color ?? TextColor;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>Rubik SemiBold heading.</summary>
        public static TextMeshProUGUI Heading(Transform parent, string name, string text, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left, Color? color = null)
        {
            TextMeshProUGUI t = Text(parent, name, text, size, alignment, color);
            SetFont(t, Theme != null ? Theme.Bold : null, FontStyles.Bold);
            return t;
        }

        /// <summary>Rubik ExtraBold with a fat ink outline: logos and screen titles.</summary>
        public static TextMeshProUGUI Display(Transform parent, string name, string text, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left, Color? color = null)
        {
            TextMeshProUGUI t = Text(parent, name, text, size, alignment, color);
            SetFont(t, Theme != null ? Theme.Display : null, FontStyles.Bold);
            t.outlineWidth = 0.22f;
            t.outlineColor = Color.black;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(0f, -5f);
            return t;
        }

        /// <summary>JetBrains Mono: numbers, timers, terminal text.</summary>
        public static TextMeshProUGUI Mono(Transform parent, string name, string text, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left, Color? color = null)
        {
            TextMeshProUGUI t = Text(parent, name, text, size, alignment, color);
            SetFont(t, Theme != null ? Theme.Mono : null, FontStyles.Normal);
            return t;
        }

        /// <summary>Small upper-case mono label ("YOUR STOCK").</summary>
        public static TextMeshProUGUI Label(Transform parent, string name, string text, Color? color = null,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            TextMeshProUGUI t = Mono(parent, name, text.ToUpperInvariant(), 15f, alignment, color ?? MutedText);
            t.characterSpacing = 12f;
            return t;
        }

        private static void SetFont(TextMeshProUGUI t, TMP_FontAsset font, FontStyles fallbackStyle)
        {
            if (font != null)
            {
                t.font = font;
            }
            else
            {
                t.fontStyle = fallbackStyle;
            }
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

        /// <summary>Chunky outlined button with a hard shadow. Returns the Button so callers can toggle interactable.</summary>
        public static Button Button(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick,
            Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, ButtonStyle style = ButtonStyle.Ghost)
        {
            (Color bg, Color fg) = style switch
            {
                ButtonStyle.Primary => (Hazard, new Color(0.08f, 0.075f, 0.05f)),
                ButtonStyle.Go => (Go, new Color(0.03f, 0.075f, 0.047f)),
                ButtonStyle.Danger => (Alert, new Color(0.1f, 0.035f, 0.03f)),
                _ => (new Color(0.09f, 0.102f, 0.125f, 1f), Paper),
            };
            Image bgImage = Panel(parent, name, bg, 4f, 12f);
            Place(bgImage.rectTransform, anchor, pivot, position, size);
            var button = bgImage.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            colors.colorMultiplier = 1.2f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(onClick);

            TextMeshProUGUI text = Heading(bgImage.transform, "Label", label, 22f, TextAlignmentOptions.Center, fg);
            Stretch(text.rectTransform, 4f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return button;
        }

        /// <summary>Rounded pill chip ("Premium", "B · High risk").</summary>
        public static Image Chip(Transform parent, string name, string text, Color bg, Color fg, float fontSize = 16f)
        {
            RectTransform rt = Rect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = bg;
            if (Theme != null && Theme.Pill != null)
            {
                image.sprite = Theme.Pill;
                image.type = UnityEngine.UI.Image.Type.Sliced;
            }
            TextMeshProUGUI t = Heading(rt, "Text", text, fontSize, TextAlignmentOptions.Center, fg);
            Stretch(t.rectTransform);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var fit = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            fit.padding = new RectOffset(14, 14, 4, 5);
            fit.childControlWidth = true;
            fit.childControlHeight = true;
            fit.childForceExpandWidth = false;
            fit.childForceExpandHeight = false;
            var size = rt.gameObject.AddComponent<ContentSizeFitter>();
            size.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            size.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return image;
        }

        /// <summary>Outlined keycap ("E", "Tab", "Esc").</summary>
        public static RectTransform Keycap(Transform parent, string key, Color? color = null)
        {
            Color c = color ?? Paper;
            Image cap = Image(parent, $"Key {key}", c, false);
            if (Theme != null && Theme.Panel != null)
            {
                cap.sprite = Theme.Panel;
                cap.type = UnityEngine.UI.Image.Type.Sliced;
                cap.pixelsPerUnitMultiplier = 2.4f;
            }
            cap.color = new Color(0.07f, 0.08f, 0.1f, 0.95f);
            var outline = cap.gameObject.AddComponent<Outline>();
            outline.effectColor = c;
            outline.effectDistance = new Vector2(1.5f, 1.5f);
            TextMeshProUGUI t = Mono(cap.transform, "Text", key, 16f, TextAlignmentOptions.Center, c);
            Stretch(t.rectTransform);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var fit = cap.gameObject.AddComponent<HorizontalLayoutGroup>();
            fit.padding = new RectOffset(8, 8, 3, 3);
            fit.childControlWidth = true;
            fit.childControlHeight = true;
            fit.childForceExpandWidth = false;
            fit.childForceExpandHeight = false;
            var size = cap.gameObject.AddComponent<ContentSizeFitter>();
            size.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            size.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return cap.rectTransform;
        }

        /// <summary>Keycap + label row ("[Tab] Inventory").</summary>
        public static RectTransform KeyHint(Transform parent, string key, string label, Color? color = null)
        {
            RectTransform row = Rect($"Hint {key}", parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            var fit = row.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Keycap(row, key, color);
            TextMeshProUGUI t = Text(row, "Label", label, 19f, TextAlignmentOptions.Left, color ?? Paper);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return row;
        }

        /// <summary>Outlined track with a fill (0–1). Returns the fill so callers can resize/recolour it.</summary>
        public static Image Bar(Transform parent, string name, float fraction, Color fill)
        {
            RectTransform rt = Rect(name, parent);
            var track = rt.gameObject.AddComponent<Image>();
            track.color = Track;
            if (Theme != null && Theme.Pill != null)
            {
                track.sprite = Theme.Pill;
                track.type = UnityEngine.UI.Image.Type.Sliced;
                track.pixelsPerUnitMultiplier = 2f;
            }
            var f = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
            f.transform.SetParent(rt, false);
            f.color = fill;
            if (Theme != null && Theme.PillFill != null)
            {
                f.sprite = Theme.PillFill;
                f.type = UnityEngine.UI.Image.Type.Sliced;
                f.pixelsPerUnitMultiplier = 2f;
            }
            f.rectTransform.anchorMin = Vector2.zero;
            f.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            f.rectTransform.offsetMin = new Vector2(2f, 2f);
            f.rectTransform.offsetMax = new Vector2(-2f, -2f);
            return f;
        }

        public static void SetBar(Image fill, float fraction) => fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);

        public static HorizontalLayoutGroup HRow(RectTransform rt, float spacing, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        public static LayoutElement Layout(GameObject go, float preferredHeight = -1f, float preferredWidth = -1f, float flexibleWidth = -1f, float flexibleHeight = 0f)
        {
            if (!go.TryGetComponent(out LayoutElement element))
            {
                element = go.AddComponent<LayoutElement>();
            }

            element.preferredHeight = preferredHeight;
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight; // rows never balloon to fill a list
            return element;
        }

        /// <summary>Quality → chip colours (matches the style guide).</summary>
        public static (Color bg, Color fg) QualityColours(Inventory.ItemQuality quality) => quality switch
        {
            Inventory.ItemQuality.Poor => (new Color(0.165f, 0.18f, 0.21f), new Color(0.79f, 0.82f, 0.85f)),
            Inventory.ItemQuality.Standard => (new Color(0.9f, 0.914f, 0.93f), new Color(0.08f, 0.09f, 0.106f)),
            Inventory.ItemQuality.Premium => (Credit, new Color(0.016f, 0.133f, 0.165f)),
            _ => (new Color(1f, 0.76f, 0.24f), new Color(0.165f, 0.106f, 0f)),
        };

        /// <summary>Meet-spot / danger risk → chip colours.</summary>
        public static (Color bg, Color fg) RiskColours(int risk) => risk switch
        {
            0 => (new Color(0.17f, 0.37f, 0.215f), new Color(0.87f, 1f, 0.9f)),
            1 => (new Color(0.42f, 0.33f, 0.08f), new Color(1f, 0.95f, 0.78f)),
            _ => (new Color(0.43f, 0.14f, 0.125f), new Color(1f, 0.88f, 0.866f)),
        };
    }
}
