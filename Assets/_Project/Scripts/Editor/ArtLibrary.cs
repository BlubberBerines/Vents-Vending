using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// The project's material library (Docs/Design/ArtStyleGuide.md). Every surface material is a preset of the
    /// "Grunge Triplanar" shader; re-running <see cref="CreateOrUpdateAll"/> re-applies the presets, so tune values here.
    /// </summary>
    public static class ArtLibrary
    {
        public const string Folder = "Assets/_Project/Art/Materials/Library";
        public const string TextureFolder = "Assets/_Project/Art/Textures/Library";
        private const string ShaderName = "VentsAndVending/Grunge Triplanar";

        /// <summary>Normal maps are flattened for the cartoony read: bumps break up the cel bands into noise.</summary>
        public const float ToonNormalScale = 0.35f;

        public static Shader Shader => Shader.Find(ShaderName);

        /// <summary>Shared "slightly cartoony" lighting settings (soft cel ramp, cool tinted shade, bright fill, crisp wear).</summary>
        public static void ApplyToonLook(Material mat)
        {
            mat.SetFloat("_RampThreshold", 0.05f);
            mat.SetFloat("_RampSoftness", 0.12f);
            mat.SetColor("_ShadowTint", new Color(0.8f, 0.76f, 0.95f));
            mat.SetFloat("_AmbientStrength", 2.6f);
            mat.SetFloat("_SpecStrength", 0.45f);
            mat.SetFloat("_RimStrength", 0.18f);
            mat.SetFloat("_WearCrispness", 0.75f);
        }

        /// <summary>One material preset. Colours are linear-ish sRGB picks from the GDD §15.4 palettes.</summary>
        public sealed class Preset
        {
            public string Name;
            public Color Base;
            public float Metallic = 0.2f;
            public float Smoothness = 0.35f;
            public float TileSize = 2f;
            public float PanelSize = 1.5f;
            public float SeamDarkness;
            public float GrimeAmount = 0.6f;
            public float GrimePatches = 0.35f;
            public float RustAmount;
            public float EdgeWear;
            public Color? Grime;
            public Color? Edge;
            public string BaseTexture;   // file name in TextureFolder (optional)
            public string NormalTexture; // file name in TextureFolder (optional)
            public float TextureStrength = 1f;
            public float NormalStrength = 1f;
        }

        // ------------------------------------------------------------------ Presets

        public static readonly Preset[] Presets =
        {
            // Maintenance network & hideout — dark grey, rust brown, dirty yellow, desaturated green, muted orange, industrial blue
            new Preset { Name = "M_PaintedMetal_Wall", Base = new Color(0.30f, 0.39f, 0.48f), Metallic = 0.35f, Smoothness = 0.38f,
                PanelSize = 1.25f, SeamDarkness = 0.55f, GrimeAmount = 0.7f, GrimePatches = 0.35f, RustAmount = 0.22f, EdgeWear = 0.75f,
                BaseTexture = "T_PaintedMetal_Base", NormalTexture = "T_PaintedMetal_Normal", NormalStrength = 0.7f },
            new Preset { Name = "M_FloorPlate", Base = new Color(0.20f, 0.21f, 0.22f), Metallic = 0.55f, Smoothness = 0.42f,
                PanelSize = 1f, SeamDarkness = 0.6f, GrimeAmount = 0f, GrimePatches = 0.55f, RustAmount = 0.12f,
                BaseTexture = "T_FloorPlate_Base", NormalTexture = "T_FloorPlate_Normal", NormalStrength = 0.45f },
            new Preset { Name = "M_Concrete_Dirty", Base = new Color(0.43f, 0.42f, 0.40f), Metallic = 0f, Smoothness = 0.15f,
                TileSize = 3f, PanelSize = 3f, SeamDarkness = 0.35f, GrimeAmount = 0.85f, GrimePatches = 0.5f,
                BaseTexture = "T_Concrete_Base", NormalTexture = "T_Concrete_Normal", NormalStrength = 0.7f },
            new Preset { Name = "M_DuctMetal", Base = new Color(0.44f, 0.31f, 0.22f), Metallic = 0.45f, Smoothness = 0.3f,
                PanelSize = 0.9f, SeamDarkness = 0.55f, GrimeAmount = 0.75f, GrimePatches = 0.45f, RustAmount = 0.45f, EdgeWear = 0.5f,
                BaseTexture = "T_PaintedMetal_Base", NormalTexture = "T_PaintedMetal_Normal", NormalStrength = 0.7f },
            new Preset { Name = "M_RustyMetal", Base = new Color(0.36f, 0.24f, 0.17f), Metallic = 0.5f, Smoothness = 0.25f,
                PanelSize = 1.5f, SeamDarkness = 0.3f, GrimeAmount = 0.6f, RustAmount = 0.7f, EdgeWear = 0.3f,
                BaseTexture = "T_PaintedMetal_Base", NormalTexture = "T_PaintedMetal_Normal", NormalStrength = 0.8f },
            new Preset { Name = "M_HazardPaint", Base = new Color(0.66f, 0.54f, 0.16f), Metallic = 0.2f, Smoothness = 0.35f,
                SeamDarkness = 0f, GrimeAmount = 0.55f, GrimePatches = 0.3f, RustAmount = 0.1f, EdgeWear = 0.9f,
                BaseTexture = "T_PaintedMetal_Base", NormalTexture = "T_PaintedMetal_Normal", TextureStrength = 0.6f, NormalStrength = 0.5f },
            new Preset { Name = "M_WorkshopOrange", Base = new Color(0.64f, 0.36f, 0.16f), Metallic = 0.25f, Smoothness = 0.35f,
                SeamDarkness = 0f, GrimeAmount = 0.5f, GrimePatches = 0.3f, RustAmount = 0.12f, EdgeWear = 0.8f,
                BaseTexture = "T_PaintedMetal_Base", NormalTexture = "T_PaintedMetal_Normal", TextureStrength = 0.6f, NormalStrength = 0.5f },
            new Preset { Name = "M_GreenPanel", Base = new Color(0.32f, 0.45f, 0.30f), Metallic = 0.3f, Smoothness = 0.35f,
                PanelSize = 1.1f, SeamDarkness = 0.45f, GrimeAmount = 0.6f, RustAmount = 0.15f, EdgeWear = 0.6f,
                BaseTexture = "T_PaintedMetal_Base", NormalTexture = "T_PaintedMetal_Normal", NormalStrength = 0.7f },
            new Preset { Name = "M_DarkMetal", Base = new Color(0.09f, 0.09f, 0.10f), Metallic = 0.7f, Smoothness = 0.5f,
                SeamDarkness = 0f, GrimeAmount = 0.3f, GrimePatches = 0.2f, EdgeWear = 0.5f,
                BaseTexture = "T_PaintedMetal_Base", NormalTexture = "T_PaintedMetal_Normal", TextureStrength = 0.7f, NormalStrength = 0.6f },

            new Preset { Name = "M_Cardboard", Base = new Color(0.66f, 0.5f, 0.31f), Metallic = 0f, Smoothness = 0.15f,
                SeamDarkness = 0f, GrimeAmount = 0.35f, GrimePatches = 0.3f, EdgeWear = 0f,
                BaseTexture = "T_PaintedMetal_Base", NormalTexture = "T_PaintedMetal_Normal", TextureStrength = 0.35f, NormalStrength = 0.3f },

            // Public plaza — neon cyan, purple, magenta, warm orange, deep blue, metallic silver (cleaner, polished)
            new Preset { Name = "M_PlazaTile", Base = new Color(0.44f, 0.45f, 0.53f), Metallic = 0.1f, Smoothness = 0.72f,
                TileSize = 2f, PanelSize = 1f, SeamDarkness = 0.45f, GrimeAmount = 0f, GrimePatches = 0.12f,
                BaseTexture = "T_PlazaTile_Base", NormalTexture = "T_PlazaTile_Normal", NormalStrength = 0.25f, TextureStrength = 0.65f },
            new Preset { Name = "M_PlazaPanel", Base = new Color(0.40f, 0.31f, 0.60f), Metallic = 0.3f, Smoothness = 0.55f,
                PanelSize = 2f, SeamDarkness = 0.4f, GrimeAmount = 0.2f, GrimePatches = 0.12f, EdgeWear = 0.15f,
                BaseTexture = "T_PlazaPanel_Base", NormalTexture = "T_PlazaPanel_Normal", NormalStrength = 0.4f },
            new Preset { Name = "M_BrushedSilver", Base = new Color(0.42f, 0.44f, 0.48f), Metallic = 0.85f, Smoothness = 0.5f,
                SeamDarkness = 0f, GrimeAmount = 0.1f, GrimePatches = 0.08f, EdgeWear = 0f,
                BaseTexture = "T_PlazaPanel_Base", NormalTexture = "T_PlazaPanel_Normal", TextureStrength = 0.5f, NormalStrength = 0.3f },
        };

        // ------------------------------------------------------------------ API

        [MenuItem("Vents & Vending/Art/Update Material Library")]
        public static void CreateOrUpdateAllFromMenu() => CreateOrUpdateAll();

        public static Dictionary<string, Material> CreateOrUpdateAll()
        {
            EnsureFolder(Folder);
            var shader = Shader.Find(ShaderName);
            var result = new Dictionary<string, Material>();
            foreach (Preset preset in Presets)
            {
                result[preset.Name] = CreateOrUpdate(preset, shader);
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        public static Material Get(string name)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{name}.mat");
            if (mat != null)
            {
                return mat;
            }

            foreach (Preset preset in Presets)
            {
                if (preset.Name == name)
                {
                    return CreateOrUpdate(preset, Shader.Find(ShaderName));
                }
            }
            return null;
        }

        private static Material CreateOrUpdate(Preset p, Shader shader)
        {
            string path = $"{Folder}/{p.Name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            mat.SetColor("_BaseColor", p.Base);
            mat.SetFloat("_Metallic", p.Metallic);
            mat.SetFloat("_Smoothness", p.Smoothness);
            mat.SetFloat("_TileSize", p.TileSize);
            mat.SetFloat("_PanelSize", p.PanelSize);
            mat.SetFloat("_SeamDarkness", p.SeamDarkness);
            mat.SetFloat("_GrimeAmount", p.GrimeAmount);
            mat.SetFloat("_GrimePatches", p.GrimePatches);
            mat.SetFloat("_RustAmount", p.RustAmount);
            mat.SetFloat("_EdgeWear", p.EdgeWear);
            if (p.Grime.HasValue)
            {
                mat.SetColor("_GrimeColor", p.Grime.Value);
            }
            if (p.Edge.HasValue)
            {
                mat.SetColor("_EdgeColor", p.Edge.Value);
            }

            Texture2D baseTex = LoadTexture(p.BaseTexture);
            Texture2D normalTex = LoadTexture(p.NormalTexture);
            mat.SetTexture("_BaseMap", baseTex);
            mat.SetFloat("_TextureStrength", baseTex != null ? p.TextureStrength : 0f);
            mat.SetTexture("_BumpMap", normalTex);
            mat.SetFloat("_BumpScale", normalTex != null ? p.NormalStrength * ToonNormalScale : 0f);
            ApplyToonLook(mat);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Texture2D LoadTexture(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            foreach (string ext in new[] { ".png", ".jpg", ".tga" })
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{fileName}{ext}");
                if (tex != null)
                {
                    return tex;
                }
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
