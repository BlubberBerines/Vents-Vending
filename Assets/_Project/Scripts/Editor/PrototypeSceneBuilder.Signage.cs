using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using VentsAndVending.UI;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Wayfinding signage (Docs/Design/TunnelNetwork.md §9 "navigation language"): physical plates with an ink edge,
    /// corner screws and a slight hand-hung lean, colour-coded by district — Old Maintenance yellow-on-charcoal with a
    /// hazard stripe, Industrial black-on-orange, Plaza Network backlit white with a cyan accent, red for warnings.
    /// Text uses the UI theme fonts so the world and the HUD speak the same type.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        private enum SignStyle { Maintenance, Industrial, Plaza, Warning }

        private struct SignLook
        {
            public Color Plate, Edge, Title, Sub;
            public bool Lit, Stripe;
        }

        private static SignLook LookFor(SignStyle style) => style switch
        {
            SignStyle.Industrial => new SignLook { Plate = new Color(0.93f, 0.45f, 0.12f), Edge = new Color(0.08f, 0.07f, 0.06f), Title = new Color(0.07f, 0.05f, 0.03f), Sub = new Color(0.2f, 0.1f, 0.03f) },
            SignStyle.Plaza => new SignLook { Plate = new Color(0.88f, 0.91f, 0.94f), Edge = new Color(0.06f, 0.07f, 0.09f), Title = new Color(0.06f, 0.07f, 0.09f), Sub = new Color(0.05f, 0.5f, 0.62f), Lit = true },
            SignStyle.Warning => new SignLook { Plate = new Color(0.82f, 0.18f, 0.14f), Edge = new Color(0.08f, 0.03f, 0.03f), Title = Color.white, Sub = new Color(1f, 0.88f, 0.85f), Stripe = true },
            _ => new SignLook { Plate = new Color(0.13f, 0.14f, 0.15f), Edge = new Color(0.03f, 0.03f, 0.03f), Title = new Color(1f, 0.82f, 0.3f), Sub = new Color(0.86f, 0.88f, 0.86f), Stripe = true },
        };

        /// <summary>
        /// A sign plate hung on a wall. <paramref name="wallPoint"/> is on the wall surface; <paramref name="yaw"/> follows
        /// CreateSign (the readable face points along rotation × back). Built outside "Level" so it never touches the NavMesh.
        /// </summary>
        private static GameObject SignPlate(string name, string title, string sub, Vector3 wallPoint, float yaw, SignStyle style,
            float width, float height, float roll = 0f)
        {
            SignLook look = LookFor(style);
            var root = new GameObject($"Sign {name}");
            root.transform.SetPositionAndRotation(wallPoint, Quaternion.Euler(0f, yaw, roll));
            Transform t = root.transform;

            Material plate = look.Lit
                ? GetOrCreateMaterial($"Sign_{style}_Plate", look.Plate, 0.5f, look.Plate * 0.35f)
                : Worn(GetOrCreateMaterial($"Sign_{style}_Plate", look.Plate, 0.35f), 0.45f, 0.4f, 0.6f);
            Material edge = GetOrCreateMaterial($"Sign_{style}_Edge", look.Edge, 0.3f);
            Material screw = ArtLibrary.Get("M_BrushedSilver");

            // Ink edge (slightly larger plate behind) and the plate itself, both rounded and a touch wonky
            var wonk = new Wonk { Twist = 1.2f, Bulge = 0.01f };
            SignSlab("Edge", t, new Vector3(0f, 0f, -0.012f), new Vector3(width + 0.05f, height + 0.05f, 0.022f), 0.035f, wonk, edge);
            SignSlab("Plate", t, new Vector3(0f, 0f, -0.028f), new Vector3(width, height, 0.02f), 0.03f, wonk, plate);
            foreach (var (sx, sy) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
            {
                var s = Part("Screw", t, new Vector3(sx * (width * 0.5f - 0.04f), sy * (height * 0.5f - 0.04f), -0.04f), Vector3.one * 0.022f, screw, PrimitiveType.Sphere);
                s.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            float textTop = height * 0.5f - 0.05f;
            float textHeight = height - 0.1f;
            if (look.Stripe)
            {
                // Hazard stripe along the bottom edge
                float band = Mathf.Min(0.07f, height * 0.16f);
                Material yellow = GetOrCreateMaterial("Sign_StripeYellow", new Color(1f, 0.8f, 0.2f), 0.4f);
                Material black = GetOrCreateMaterial("Sign_StripeBlack", new Color(0.04f, 0.04f, 0.04f), 0.3f);
                float by = -height * 0.5f + 0.035f + band * 0.5f;
                Part("Stripe", t, new Vector3(0f, by, -0.04f), new Vector3(width - 0.12f, band, 0.004f), style == SignStyle.Warning ? black : yellow);
                int count = Mathf.Max(3, Mathf.RoundToInt((width - 0.16f) / 0.09f));
                for (int i = 0; i < count; i++)
                {
                    float x = -width * 0.5f + 0.1f + (width - 0.2f) * (i + 0.5f) / count;
                    Part("StripeBar", t, new Vector3(x, by, -0.043f), new Vector3(band * 0.38f, band * 0.95f, 0.003f), style == SignStyle.Warning ? yellow : black)
                        .transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
                }
                textHeight -= band + 0.02f;
            }

            bool hasSub = !string.IsNullOrEmpty(sub);
            float titleHeight = hasSub ? textHeight * 0.55f : textHeight;
            SignText("Title", t, title, new Vector3(0f, textTop - titleHeight * 0.5f, -0.045f), new Vector2(width - 0.12f, titleHeight), look.Title,
                UiTheme.Current != null ? UiTheme.Current.Display : null);
            if (hasSub)
            {
                float subHeight = textHeight - titleHeight;
                SignText("Sub", t, sub, new Vector3(0f, textTop - titleHeight - subHeight * 0.5f, -0.045f), new Vector2(width - 0.12f, subHeight), look.Sub,
                    UiTheme.Current != null ? UiTheme.Current.Mono : null);
            }

            if (look.Lit)
            {
                var light = new GameObject("Backlight").AddComponent<Light>();
                light.transform.SetParent(t, false);
                light.transform.localPosition = new Vector3(0f, 0f, -0.6f);
                light.type = LightType.Point;
                light.color = new Color(0.8f, 0.92f, 1f);
                light.range = Mathf.Max(2f, width * 1.4f);
                light.intensity = 0.9f;
            }
            return root;
        }

        private static void SignSlab(string name, Transform parent, Vector3 localPos, Vector3 size, float radius, Wonk wonk, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<MeshFilter>().sharedMesh = RoundedMeshFactory.Get(size, radius, 4, wonk);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void SignText(string name, Transform parent, string text, Vector3 localPos, Vector2 size, Color colour, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tmp = go.AddComponent<TextMeshPro>();
            if (font != null)
            {
                tmp.font = font;
            }
            tmp.text = text;
            tmp.color = colour;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.1f;
            tmp.fontSizeMax = 3f;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.rectTransform.sizeDelta = size;
        }
    }
}
