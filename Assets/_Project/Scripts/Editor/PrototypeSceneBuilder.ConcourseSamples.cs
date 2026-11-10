using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VentsAndVending.Editor
{
    public static partial class PrototypeSceneBuilder
    {
        private const string SampleArt = "Assets/_Project/Art/Environment/ConcourseSamples";
        private const string SamplePrefabs = "Assets/_Project/Prefabs/Environment/ConcourseSamples";

        private static void PrepareConcourseSamples()
        {
            EnsureFolder(SamplePrefabs);
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { SampleArt }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.globalScale = 1f;
                foreach (string name in new[] { "CS_Frame", "CS_Amber", "CS_Glass", "CS_CeramicPurple", "CS_BrushedSteel", "CS_RubberDeck" })
                {
                    Material mat;
                    if (name == "CS_Glass") mat = ArtLibrary.Get("M_Env_GlassTint");
                    else
                    {
                        string matPath = $"{SampleArt}/{name}.mat";
                        mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if (mat == null)
                        {
                            mat = new Material(Shader.Find("VentsAndVending/Grunge Triplanar"));
                            mat.SetColor("_BaseColor", name == "CS_Frame" ? new Color(.15f,.17f,.21f) : name == "CS_Amber" ? new Color(.85f,.4f,.07f) : Color.white);
                            mat.SetFloat("_Metallic", name == "CS_RubberDeck" ? .05f : .5f);
                            mat.SetFloat("_Smoothness", .3f);
                            mat.SetFloat("_GrimeAmount", .15f);
                            mat.SetFloat("_TextureStrength", 1f);
                            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{SampleArt}/{name}_BaseColor.png"));
                            AssetDatabase.CreateAsset(mat, matPath);
                        }
                    }
                    if (mat != null) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), mat);
                }
                importer.SaveAndReimport();
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                string modelName = Path.GetFileNameWithoutExtension(path);
                var box = instance.AddComponent<BoxCollider>();
                if (modelName.Contains("Floor")) { box.size = new Vector3(2,.2f,2); box.center = new Vector3(0,-.1f,0); }
                else if (modelName.Contains("Ceiling")) { box.size = new Vector3(2,.2f,2); box.center = new Vector3(0,.1f,0); }
                else { box.size = new Vector3(modelName.Contains("Viewport") ? 6 : 2,3,.32f); box.center = new Vector3(0,1.5f,-.05f); }
                PrefabUtility.SaveAsPrefabAsset(instance, $"{SamplePrefabs}/{modelName}.prefab");
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static GameObject PlaceSample(Transform parent, string name, Vector3 position, float yaw)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{SamplePrefabs}/{name}.prefab");
            if (asset == null) throw new InvalidOperationException("Missing Blender sample: " + name);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0,yaw,0));
            return go;
        }

        private static void BuildViewportBackdrop(Transform root)
        {
            var backdrop=RingGroup("Exterior_Starfield",root);
            const string path=SampleArt+"/CS_Stars.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null)
            {
                mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.SetColor("_BaseColor",new Color(2,2.4f,3,1));
                AssetDatabase.CreateAsset(mat,path);
            }
            var random=new System.Random(712);
            for(int i=0;i<180;i++)
            {
                float a=(float)random.NextDouble()*360;
                var star=GameObject.CreatePrimitive(PrimitiveType.Sphere);star.name="Distant Star";
                star.transform.SetParent(backdrop,false);
                star.transform.position=RingPoint(125+(float)random.NextDouble()*25,a,-20+(float)random.NextDouble()*60);
                star.transform.localScale=Vector3.one*(.08f+(float)random.NextDouble()*.18f);
                star.GetComponent<MeshRenderer>().sharedMaterial=mat;
                UnityEngine.Object.DestroyImmediate(star.GetComponent<Collider>());
            }
        }
    }
}
