using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using VentsAndVending.UI;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Generates the "Ink &amp; Hazard" UI theme: anti-aliased rounded sprites with an ink outline (9-sliced), TMP font
    /// assets from the bundled OFL fonts (Rubik, JetBrains Mono), and the UiTheme asset in Resources.
    /// </summary>
    public static class UiThemeBuilder
    {
        private const string SpriteFolder = "Assets/_Project/Art/UI/Sprites";
        private const string FontFolder = "Assets/_Project/Art/UI/Fonts";
        private const string ThemePath = "Assets/_Project/Resources/VentsAndVending/UiTheme.asset";
        private const string FallbackFont = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("Vents & Vending/UI/Build UI Theme")]
        public static UiTheme Build()
        {
            Directory.CreateDirectory(SpriteFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ThemePath)!);

            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UiTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            theme.Panel = RoundedSprite("ui_panel", 64, 16f, 2f);
            theme.PanelFill = RoundedSprite("ui_panel_fill", 64, 16f, 0f);
            theme.Pill = RoundedSprite("ui_pill", 32, 15.5f, 2f);
            theme.PillFill = RoundedSprite("ui_pill_fill", 32, 15.5f, 0f);
            theme.Circle = RoundedSprite("ui_circle", 64, 31.5f, 2f, sliced: false);

            theme.Body = FontAsset("Rubik-Regular");
            theme.Bold = FontAsset("Rubik-SemiBold");
            theme.Display = FontAsset("Rubik-ExtraBold");
            theme.Mono = FontAsset("JetBrainsMono-SemiBold");
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }

        // ------------------------------------------------------------------ Sprites

        /// <summary>White rounded rect (or circle) with an optional black outline, anti-aliased, saved as a 9-slice sprite.</summary>
        private static Sprite RoundedSprite(string name, int size, float radius, float outline, bool sliced = true)
        {
            string path = $"{SpriteFolder}/{name}.png";
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Signed distance to a rounded square filling the texture (negative inside)
                    float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    float py = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
                    float d = outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;

                    float alpha = Mathf.Clamp01(0.5f - d);                 // shape coverage
                    float ink = outline > 0f ? Mathf.Clamp01(d + outline + 0.5f) : 0f; // 1 in the outline band
                    Color c = Color.Lerp(Color.white, Color.black, ink);
                    c.a = alpha;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            float b = sliced ? Mathf.Ceil(radius + outline) : 0f;
            importer.spriteBorder = new Vector4(b, b, b, b);
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------------ Fonts

        private static TMP_FontAsset FontAsset(string fileStem)
        {
            string assetPath = $"{FontFolder}/{fileStem} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/{fileStem}.ttf");
            if (font == null)
            {
                Debug.LogError($"[UiThemeBuilder] Missing font {fileStem}.ttf in {FontFolder}");
                return null;
            }

            // Dynamic atlas: glyphs are added on demand, so one asset covers whatever the UI writes
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = $"{fileStem} SDF";
            AssetDatabase.CreateAsset(asset, assetPath);
            asset.material.name = $"{fileStem} SDF Material";
            asset.atlasTexture.name = $"{fileStem} SDF Atlas";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);

            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFont);
            if (fallback != null)
            {
                asset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { fallback };
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
