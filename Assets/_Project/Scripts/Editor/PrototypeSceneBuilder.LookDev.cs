using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Look-dev scene for art direction: every library material as a swatch wall + sample props, under maintenance
    /// lighting (left bay) and plaza lighting (right bay). Not part of the game build.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        public const string LookDevPath = "Assets/_Project/Scenes/LookDev/LookDev.unity";

        [MenuItem("Vents & Vending/Art/Build Look-Dev Scene")]
        public static string BuildLookDevScene()
        {
            var mats = ArtLibrary.CreateOrUpdateAll();
            EnsureFolder(System.IO.Path.GetDirectoryName(LookDevPath)!.Replace('\\', '/'));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.24f, 0.30f);
            RenderSettings.fog = false;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.045f, 0.06f);
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 1.6f, -7f), Quaternion.Euler(8f, 0f, 0f));

            var neonCyan = GetOrCreateMaterial("Plaza_NeonCyan", new Color(0.02f, 0.2f, 0.25f), 0.6f, new Color(0.2f, 1.6f, 2f));
            var neonMagenta = GetOrCreateMaterial("Plaza_NeonMagenta", new Color(0.25f, 0.02f, 0.2f), 0.6f, new Color(2f, 0.2f, 1.5f));

            // Maintenance bay (x < 0) and plaza bay (x > 0)
            string[] maintenance = { "M_PaintedMetal_Wall", "M_DuctMetal", "M_RustyMetal", "M_GreenPanel", "M_HazardPaint", "M_WorkshopOrange" };
            string[] plaza = { "M_PlazaPanel", "M_BrushedSilver", "M_DarkMetal" };

            BuildBay("Maintenance Bay", -12f, maintenance, mats["M_FloorPlate"], mats["M_Concrete_Dirty"], mats, maintenanceLighting: true, neonCyan, neonMagenta);
            BuildBay("Plaza Bay", 8f, plaza, mats["M_PlazaTile"], mats["M_PlazaPanel"], mats, maintenanceLighting: false, neonCyan, neonMagenta);

            // Camera presets as empty markers (used for renders)
            Marker("View_Maintenance", new Vector3(-6.5f, 1.7f, -6.2f), Quaternion.Euler(10f, 0f, 0f));
            Marker("View_Plaza", new Vector3(11f, 1.7f, -5.4f), Quaternion.Euler(10f, 0f, 0f));
            Marker("View_Closeup", new Vector3(-10.4f, 1.1f, -1.6f), Quaternion.Euler(14f, -10f, 0f));

            EditorSceneManager.SaveScene(scene, LookDevPath);
            return LookDevPath;
        }

        private static void BuildBay(string name, float startX, string[] wallMats, Material floor, Material backWall,
            System.Collections.Generic.Dictionary<string, Material> mats, bool maintenanceLighting, Material neonA, Material neonB)
        {
            var bay = new GameObject(name).transform;
            float width = wallMats.Length * 2.2f + 0.6f;
            float cx = startX + width * 0.5f - 0.3f;

            Block("Floor", bay, new Vector3(cx, -0.25f, 1.5f), new Vector3(width, 0.5f, 7f), floor);
            Block("BackWall", bay, new Vector3(cx, 2f, 3.4f), new Vector3(width, 4f, 0.4f), backWall);

            for (int i = 0; i < wallMats.Length; i++)
            {
                float x = startX + i * 2.2f + 0.8f;
                Material m = mats[wallMats[i]];
                Block($"Swatch_{wallMats[i]}", bay, new Vector3(x, 1.4f, 2.9f), new Vector3(2f, 2.8f, 0.4f), m);
                Block($"Crate_{i}", bay, new Vector3(x - 0.35f, 0.35f, 1.6f), new Vector3(0.7f, 0.7f, 0.7f), m);
                var pipe = Part($"Pipe_{i}", bay, new Vector3(x + 0.55f, 0.6f, 1.9f), new Vector3(0.25f, 0.6f, 0.25f), m, PrimitiveType.Cylinder);
                pipe.isStatic = true;
                CreateSign($"Label_{wallMats[i]}", wallMats[i].Replace("M_", string.Empty), new Vector3(x, 3.05f, 2.68f), 0f, 0.7f, Color.white, 2f);
            }

            if (maintenanceLighting)
            {
                var key = new GameObject("Key Light").AddComponent<Light>();
                key.transform.SetParent(bay, false);
                key.type = LightType.Directional;
                key.color = new Color(1f, 0.88f, 0.72f);
                key.intensity = 0.55f;
                key.shadows = LightShadows.Soft;
                key.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                CreatePointLight("Work Lamp A", new Vector3(startX + 3f, 3.2f, 0.5f), new Color(1f, 0.82f, 0.55f), 8f, 2.6f, bay);
                CreatePointLight("Work Lamp B", new Vector3(startX + 9.5f, 3.2f, 0.5f), new Color(1f, 0.82f, 0.55f), 8f, 2.2f, bay);
                CreatePointLight("Cold Fill", new Vector3(startX + 6f, 1.2f, -2.5f), new Color(0.5f, 0.65f, 0.9f), 9f, 0.6f, bay);
            }
            else
            {
                NoCollider(Block("Neon_Top", bay, new Vector3(cx, 3.7f, 3.18f), new Vector3(width - 0.4f, 0.07f, 0.05f), neonB));
                NoCollider(Block("Neon_Low", bay, new Vector3(cx, 0.15f, 3.18f), new Vector3(width - 0.4f, 0.05f, 0.05f), neonA));
                CreatePointLight("Plaza Cyan", new Vector3(startX + 1.2f, 3.2f, 0.2f), new Color(0.2f, 0.9f, 1f), 9f, 3f, bay);
                CreatePointLight("Plaza Magenta", new Vector3(startX + width - 1.2f, 3.2f, 0.2f), new Color(1f, 0.25f, 0.85f), 9f, 3f, bay);
                CreatePointLight("Plaza Fill", new Vector3(cx, 3.6f, -1.5f), new Color(0.8f, 0.85f, 1f), 10f, 1.6f, bay);
            }
        }

        private static void Marker(string name, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(position, rotation);
        }
    }
}
