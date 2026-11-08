using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Project-wide "slightly cartoony" rendering setup (Docs/Design/ArtStyleGuide.md):
    /// ink-outline full-screen pass on the URP renderers and a gentle saturation/contrast boost in the global volume.
    /// Safe to re-run; it updates existing settings instead of duplicating them.
    /// </summary>
    public static class ToonRenderingSetup
    {
        private const string OutlineShaderName = "VentsAndVending/Outline Fullscreen";
        private const string OutlineMaterialPath = "Assets/_Project/Art/Materials/Library/M_Outline.mat";
        private const string FeatureName = "Toon Outline";
        private static readonly string[] RendererPaths =
        {
            "Assets/_Project/Settings/Rendering/PC_Renderer.asset",
            "Assets/_Project/Settings/Rendering/Mobile_Renderer.asset"
        };
        private const string VolumeProfilePath = "Assets/_Project/Settings/Rendering/GlobalVolumeProfile.asset";

        [MenuItem("Vents & Vending/Art/Apply Toon Rendering Setup")]
        public static string Apply()
        {
            Material outline = GetOrCreateOutlineMaterial();
            int renderers = 0;
            foreach (string path in RendererPaths)
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (data != null && EnsureOutlineFeature(data, outline))
                {
                    renderers++;
                }
            }

            bool volume = EnsureColourGrade();
            AssetDatabase.SaveAssets();
            return $"outline on {renderers} renderer(s), colour grade={(volume ? "ok" : "missing profile")}";
        }

        private static Material GetOrCreateOutlineMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find(OutlineShaderName));
                AssetDatabase.CreateAsset(mat, OutlineMaterialPath);
            }

            mat.SetColor("_OutlineColor", new Color(0.06f, 0.05f, 0.1f));
            mat.SetFloat("_Thickness", 2.3f);
            mat.SetFloat("_DepthSensitivity", 2.5f);
            mat.SetFloat("_NormalSensitivity", 1.2f);
            mat.SetFloat("_Strength", 1f);
            mat.SetFloat("_FadeStart", 18f);
            mat.SetFloat("_FadeEnd", 55f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static bool EnsureOutlineFeature(UniversalRendererData data, Material outline)
        {
            var feature = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f => f.name == FeatureName);
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feature.name = FeatureName;
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.SaveAssets();
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

                var so = new SerializedObject(data);
                SerializedProperty features = so.FindProperty("m_RendererFeatures");
                SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            feature.passMaterial = outline;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.fetchColorBuffer = true;
            feature.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            feature.SetActive(true);
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(data);
            return true;
        }

        /// <summary>A little extra saturation and contrast — Rick & Morty colour without going neon-everything.</summary>
        private static bool EnsureColourGrade()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                return false;
            }

            if (!profile.TryGet(out ColorAdjustments adjustments))
            {
                adjustments = profile.Add<ColorAdjustments>(true);
                adjustments.name = nameof(ColorAdjustments);
                AssetDatabase.AddObjectToAsset(adjustments, profile);
            }

            adjustments.active = true;
            adjustments.saturation.Override(18f);
            adjustments.contrast.Override(8f);
            EditorUtility.SetDirty(adjustments);
            EditorUtility.SetDirty(profile);
            return true;
        }
    }
}
