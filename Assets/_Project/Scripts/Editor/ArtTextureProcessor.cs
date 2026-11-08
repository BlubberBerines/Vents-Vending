using System.IO;
using UnityEditor;
using UnityEngine;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Turns raw generated textures (Art/Textures/Library/_source/T_X_src.png) into library-ready maps:
    /// T_X_Base.png — desaturated detail map normalised to mean 0.5 (the material's base colour sets the palette), and
    /// T_X_Normal.png — tangent-space normal map derived from luminance with wrapped (seamless) sampling.
    /// </summary>
    public static class ArtTextureProcessor
    {
        private const string SourceFolder = ArtLibrary.TextureFolder + "/_source";

        /// <summary>Per-texture normal strength (height-to-slope multiplier).</summary>
        private static float NormalStrengthFor(string name) => name switch
        {
            "T_FloorPlate" => 5f,
            "T_Concrete" => 3f,
            "T_PlazaTile" => 1.5f,
            "T_PlazaPanel" => 1.2f,
            _ => 2.5f
        };

        [MenuItem("Vents & Vending/Art/Process Source Textures")]
        public static string ProcessAll()
        {
            if (!Directory.Exists(SourceFolder))
            {
                return "no source folder";
            }

            int count = 0;
            foreach (string src in Directory.GetFiles(SourceFolder, "*_src.png"))
            {
                string name = Path.GetFileNameWithoutExtension(src).Replace("_src", string.Empty);
                Process(src, name);
                count++;
            }

            AssetDatabase.Refresh();
            foreach (string src in Directory.GetFiles(SourceFolder, "*_src.png"))
            {
                string name = Path.GetFileNameWithoutExtension(src).Replace("_src", string.Empty);
                ConfigureImporter($"{ArtLibrary.TextureFolder}/{name}_Base.png", normal: false);
                ConfigureImporter($"{ArtLibrary.TextureFolder}/{name}_Normal.png", normal: true);
            }

            ArtLibrary.CreateOrUpdateAll();
            return $"processed {count} textures";
        }

        private static void Process(string sourcePath, string name)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            source.LoadImage(File.ReadAllBytes(sourcePath));
            int w = source.width, h = source.height;
            Color32[] pixels = source.GetPixels32();

            // Luminance + mean
            var lum = new float[w * h];
            double sum = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 c = pixels[i];
                float l = (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
                lum[i] = l;
                sum += l;
            }
            float mean = (float)(sum / lum.Length);
            float gain = 0.5f / Mathf.Max(mean, 0.01f);

            // Base: neutral grey detail map, mean 0.5
            var basePixels = new Color32[w * h];
            for (int i = 0; i < lum.Length; i++)
            {
                byte v = (byte)Mathf.Clamp(Mathf.RoundToInt(lum[i] * gain * 255f), 0, 255);
                basePixels[i] = new Color32(v, v, v, 255);
            }

            // Normal: slight blur of height to avoid noise, then Sobel with wrap-around
            float[] height = Blur(lum, w, h);
            float strength = NormalStrengthFor(name);
            var normalPixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float tl = H(height, w, h, x - 1, y + 1), t = H(height, w, h, x, y + 1), tr = H(height, w, h, x + 1, y + 1);
                    float l = H(height, w, h, x - 1, y), r = H(height, w, h, x + 1, y);
                    float bl = H(height, w, h, x - 1, y - 1), b = H(height, w, h, x, y - 1), br = H(height, w, h, x + 1, y - 1);
                    float dx = (tr + 2f * r + br) - (tl + 2f * l + bl);
                    float dy = (tl + 2f * t + tr) - (bl + 2f * b + br);
                    var n = new Vector3(-dx * strength, -dy * strength, 1f).normalized;
                    normalPixels[y * w + x] = new Color32(
                        (byte)Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f), 255);
                }
            }

            Write($"{ArtLibrary.TextureFolder}/{name}_Base.png", basePixels, w, h);
            Write($"{ArtLibrary.TextureFolder}/{name}_Normal.png", normalPixels, w, h);
            Object.DestroyImmediate(source);
        }

        private static float H(float[] height, int w, int h, int x, int y)
        {
            x = (x % w + w) % w;
            y = (y % h + h) % h;
            return height[y * w + x];
        }

        private static float[] Blur(float[] src, int w, int h)
        {
            var dst = new float[src.Length];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float s = 0f;
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            s += H(src, w, h, x + ox, y + oy);
                        }
                    }
                    dst[y * w + x] = s / 9f;
                }
            }
            return dst;
        }

        private static void Write(string path, Color32[] pixels, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureImporter(string path, bool normal)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
    }
}
