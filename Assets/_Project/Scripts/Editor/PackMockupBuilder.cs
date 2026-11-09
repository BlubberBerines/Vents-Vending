using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// THROWAWAY look-dev spike (environment brief, pack selection): one test corridor per third-party pack under the
    /// in-game maintenance lighting, built twice — as shipped, and repainted with the project's toon library materials —
    /// then captured from the same first-person camera so the packs can be compared side by side.
    /// Not part of the game build. Delete once the pack decision is made.
    /// </summary>
    public static class PackMockupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/LookDev/PackMockup.unity";
        private const string Station = "Assets/ThirdParty/LowPolySciFiStation/FBX/";
        private const string Props = "Assets/ThirdParty/LowPolySciFiPropsPack/FBX/";
        private const string Cosmic = "Assets/ThirdParty/Cosmic_Retro_Station_Props_FREE/Prefabs/";
        private const string VolumeProfilePath = "Assets/_Project/Settings/Rendering/GlobalVolumeProfile.asset";
        public const string ShotFolder = "Docs/Art/Review/PackMockup";

        private const float CorridorSpacing = 14f;  // corridors side by side along Z (lights never reach a neighbour)
        private const float RepaintOffset = 40f;    // repainted row along X

        // pack material name -> project library material (VentsAndVending/Grunge Triplanar)
        private static readonly Dictionary<string, string> Repaint = new()
        {
            { "M_Hull", "M_PlazaPanel" }, { "M_HullLight", "M_PlazaPanel" }, { "M_HullWhite", "M_PlazaPanel" },
            { "hull", "M_PlazaPanel" }, { "hull2", "M_PlazaPanel" },
            { "M_Metal", "M_BrushedSilver" }, { "steel", "M_BrushedSilver" },
            { "M_HullDark", "M_DarkMetal" }, { "M_Gunmetal", "M_DarkMetal" }, { "M_Black", "M_DarkMetal" },
            { "M_Navy", "M_DarkMetal" }, { "M_Blue", "M_DarkMetal" }, { "M_Rubber", "M_DarkMetal" },
            { "dark", "M_DarkMetal" }, { "black", "M_DarkMetal" },
            { "M_Hazard", "M_HazardPaint" }, { "yellowS", "M_HazardPaint" },
            { "M_Orange", "M_WorkshopOrange" }, { "orangeS", "M_WorkshopOrange" },
            { "M_Olive", "M_WorkshopOrange" }, { "olive", "M_WorkshopOrange" },
            { "M_Linen", "M_Cardboard" },
        };

        private sealed class Spec
        {
            public string Name;
            public string[] LeftWalls, RightWalls;
            public Action<Transform> Dress;
        }

        private static readonly Spec[] Corridors =
        {
            new Spec
            {
                Name = "A_LowPolySciFiStation",
                LeftWalls = new[] { "SM_Wall_Panel", "SM_Wall_Pipes", "SM_Wall_Console", "SM_Wall_Vent", "SM_Wall_LightStrip" },
                RightWalls = new[] { "SM_Wall_Panel_Dark", "SM_Wall_Monitor", "SM_Wall_Ribbed", "SM_Wall_Damaged", "SM_Wall_Window" },
                Dress = t =>
                {
                    Put(t, Station + "SM_Crate_Stack.fbx", 2.0f, 0f, -1.2f, 180f);
                    Put(t, Station + "SM_Fuel_Barrel.fbx", 3.1f, 0f, -1.4f, 180f);
                    Put(t, Station + "SM_Tool_Cart.fbx", 4.8f, 0f, 1.25f, 0f);
                    Put(t, Station + "SM_Fire_Extinguisher.fbx", 6.3f, 0f, 1.55f, 0f);
                    Put(t, Station + "SM_Generator.fbx", 7.9f, 0f, -1.15f, 180f);
                    Put(t, Station + "SM_Locker.fbx", 8.9f, 0f, 1.45f, 0f);
                    Put(t, Station + "SM_Cable_Spool.fbx", 5.6f, 0f, -1.4f, 180f);
                },
            },
            new Spec
            {
                Name = "B_LowPolySciFiProps",
                LeftWalls = new[] { "SM_Wall_Panel", "SM_Wall_Ribbed", "SM_Wall_Panel", "SM_Wall_Panel_Dark", "SM_Wall_Panel" },
                RightWalls = new[] { "SM_Wall_Panel_Dark", "SM_Wall_Panel", "SM_Wall_Ribbed", "SM_Wall_Panel", "SM_Wall_Panel_Dark" },
                Dress = t =>
                {
                    var crate = Put(t, Props + "crate.fbx", 2.0f, 0f, -1.2f, 180f);
                    Put(t, Props + "crate_small.fbx", 2.1f, Top(crate), -1.25f, 160f);
                    Put(t, Props + "barrel.fbx", 3.2f, 0f, -1.4f, 180f);
                    Put(t, Props + "server_rack.fbx", 4.5f, 0f, 1.4f, 0f);
                    Put(t, Props + "computer_terminal.fbx", 5.8f, 0f, 1.35f, 0f);
                    Put(t, Props + "control_panel.fbx", 7.3f, 0f, -1.35f, 180f);
                    Put(t, Props + "fuel_tank.fbx", 8.5f, 0f, 1.15f, 0f);
                    Put(t, Props + "light_pillar.fbx", 9.3f, 0f, -1.5f, 180f);
                    Put(t, Props + "pipe_straight.fbx", 5f, 2.45f, 1.6f, 0f);
                    Put(t, Props + "pipe_straight.fbx", 7f, 2.45f, 1.6f, 0f);
                    Put(t, Props + "wall_light.fbx", 3f, 2.1f, -1.76f, 180f);
                    Put(t, Props + "energy_cell.fbx", 6.6f, 0f, -1.5f, 0f);
                },
                // the pack's own door replaces the Station door on the end wall (see Build)
            },
            new Spec
            {
                Name = "C_CosmicRetroStation",
                LeftWalls = new[] { "SM_Wall_Panel", "SM_Wall_Ribbed", "SM_Wall_Panel", "SM_Wall_Panel_Dark", "SM_Wall_Panel" },
                RightWalls = new[] { "SM_Wall_Panel_Dark", "SM_Wall_Panel", "SM_Wall_Ribbed", "SM_Wall_Panel", "SM_Wall_Panel_Dark" },
                Dress = t =>
                {
                    Put(t, Cosmic + "CR_Locker_Emergency_Red.prefab", 2.4f, 0f, 1.55f, 180f);
                    Put(t, Cosmic + "CR_StorageCrate_Large_1.prefab", 3.2f, 0f, -1.45f, 0f);
                    var table = Put(t, Cosmic + "CR_Table_Small_2.prefab", 5.6f, 0f, -1.2f, 0f);
                    Put(t, Cosmic + "CR_Monitor_Small_1.prefab", 5.5f, Top(table), -1.4f, 0f);
                    Put(t, Cosmic + "CR_Tool_Hammer.prefab", 5.95f, Top(table), -1.1f, 120f);
                    Put(t, Cosmic + "CR_Chair_Medium.prefab", 5.6f, 0f, -0.35f, 180f);
                    Put(t, Cosmic + "CR_Computer_PanelOnly.prefab", 7.6f, 0f, 1.4f, 180f);
                    Put(t, Cosmic + "CR_WallPanel_Control_2.prefab", 4.0f, 1.3f, 1.78f, 180f);
                },
            },
        };

        [MenuItem("Vents & Vending/LookDev/Build Pack Mockup (spike)")]
        public static string Build()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new InvalidOperationException("An open scene has unsaved changes; save or discard them first.");
                }
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Lighting();

            for (int row = 0; row < 2; row++)
            {
                bool repaint = row == 1;
                for (int i = 0; i < Corridors.Length; i++)
                {
                    Spec s = Corridors[i];
                    var root = new GameObject($"{s.Name}_{(repaint ? "Repaint" : "AsShipped")}").transform;
                    root.position = new Vector3(repaint ? RepaintOffset : 0f, 0f, i * CorridorSpacing);
                    Shell(root, s, door: i != 1);
                    s.Dress(root);
                    if (i == 1)
                    {
                        Put(root, Props + "sci_fi_door.fbx", 9.75f, 0f, 0f, 90f);
                    }

                    Rig(root);
                    View(root, "View_1_Corridor", new Vector3(-1.6f, 1.7f, 0f), Quaternion.Euler(4f, 90f, 0f));
                    View(root, "View_2_RightWall", new Vector3(3.6f, 1.6f, 0.9f), Quaternion.Euler(16f, 128f, 0f));
                    View(root, "View_3_LeftWall", new Vector3(3.4f, 1.6f, -0.9f), Quaternion.Euler(16f, 52f, 0f));
                    if (repaint)
                    {
                        ApplyRepaint(root);
                    }
                }
            }

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EditorSceneManager.SaveScene(scene, ScenePath);
            return ScenePath;
        }

        /// <summary>Renders every corridor's View marker to PNG (same camera, same settings).</summary>
        [MenuItem("Vents & Vending/LookDev/Capture Pack Mockup (spike)")]
        public static string Capture()
        {
            Directory.CreateDirectory(ShotFolder);
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var shots = new List<string>();
            cam.targetTexture = rt;
            cam.Render();
            cam.Render();
            cam.targetTexture = null;
            foreach (var view in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!view.name.StartsWith("View_") || view.parent == null)
                {
                    continue;
                }

                cam.transform.SetPositionAndRotation(view.position, view.rotation);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                cam.targetTexture = null;
                string path = $"{ShotFolder}/{view.parent.name}_{view.name.Substring(5)}.png";
                File.WriteAllBytes(path, tex.EncodeToPNG());
                shots.Add(path);
            }

            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
            return string.Join("\n", shots);
        }

        // ------------------------------------------------------------------ pieces

        private static void Lighting()
        {
            // Same values as the in-game maintenance tunnels (Greybox_Movement)
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.24f, 0.24f, 0.30f);
            RenderSettings.ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f);
            RenderSettings.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.08f);
            RenderSettings.fogDensity = 0.018f;

            var vol = new GameObject("Global Volume");
            var volume = vol.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        }

        /// <summary>One shared light rig per corridor: amber work lights, cold fill, cyan/magenta tech accents, red emergency.</summary>
        private static void Rig(Transform root)
        {
            Point(root, "Work Light A", new Vector3(3f, 2.7f, 0f), new Color(1f, 0.85f, 0.6f), 6.5f, 3f, true);
            Point(root, "Work Light B", new Vector3(7f, 2.7f, 0f), new Color(1f, 0.85f, 0.6f), 6.5f, 3f, true);
            Point(root, "Cold Fill", new Vector3(5f, 1.2f, -1.2f), new Color(0.5f, 0.65f, 0.9f), 6f, 0.6f, false);
            Point(root, "Tech Cyan", new Vector3(4.2f, 1.3f, 0.7f), new Color(0.2f, 0.9f, 1f), 3.5f, 1f, false);
            Point(root, "Tech Magenta", new Vector3(8.2f, 0.6f, -0.7f), new Color(1f, 0.25f, 0.85f), 3.5f, 1f, false);
            Point(root, "Emergency Red", new Vector3(9.5f, 2.6f, 0f), new Color(1f, 0.15f, 0.1f), 4f, 1.6f, false);
        }

        private static void Shell(Transform root, Spec s, bool door)
        {
            for (int i = 0; i < 6; i++)
            {
                float x = -1f + 2f * i;
                int w = Mathf.Max(0, i - 1);
                foreach (float z in new[] { -1f, 1f })
                {
                    string floor = i == 3 && z < 0 ? "SM_Floor_Grate" : i == 5 ? "SM_Floor_Hazard_Edge" : "SM_Floor_Deck";
                    Put(root, Station + floor + ".fbx", x, 0f, z, z < 0 ? 180f : 0f);
                    bool lightTile = (i == 2 || i == 4) && z > 0;
                    var ceiling = Put(root, Station + (lightTile ? "SM_Ceiling_Light" : "SM_Ceiling_Vent") + ".fbx", x, 3f, z, 0f);
                    ceiling.name += lightTile ? "" : "_Plain";
                }

                Put(root, Station + (i == 0 ? "SM_Wall_Panel_Dark" : s.LeftWalls[w]) + ".fbx", x, 0f, 2f, 0f);
                Put(root, Station + (i == 0 ? "SM_Wall_Panel" : s.RightWalls[w]) + ".fbx", x, 0f, -2f, 180f);
            }

            Put(root, Station + (door ? "SM_Wall_Door_Closed" : "SM_Wall_Panel") + ".fbx", 10f, 0f, door ? 0f : 1f, 90f);
            if (!door)
            {
                Put(root, Station + "SM_Wall_Panel.fbx", 10f, 0f, -1f, 90f);
            }
            else
            {
                Put(root, Station + "SM_Pillar.fbx", 10f, 0f, 1.6f, 90f);
                Put(root, Station + "SM_Pillar.fbx", 10f, 0f, -1.6f, 90f);
            }
        }

        private static GameObject Put(Transform root, string assetPath, float x, float y, float z, float yaw)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                Debug.LogWarning($"PackMockup: missing {assetPath}");
                return new GameObject("MISSING " + Path.GetFileNameWithoutExtension(assetPath));
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, root);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * asset.transform.localRotation;
            return go;
        }

        private static void View(Transform root, string name, Vector3 local, Quaternion rot)
        {
            var view = new GameObject(name).transform;
            view.SetParent(root, false);
            view.localPosition = local;
            view.localRotation = rot;
        }

        private static float Top(GameObject go)
        {
            float top = float.MinValue;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                top = Mathf.Max(top, r.bounds.max.y);
            }

            return top == float.MinValue ? 0f : top - go.transform.parent.position.y;
        }

        private static void Point(Transform root, string name, Vector3 local, Color color, float range, float intensity, bool shadows)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = local;
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }

        private static void ApplyRepaint(Transform root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                Transform piece = r.transform;
                while (piece.parent != null && piece.parent != root)
                {
                    piece = piece.parent;
                }

                bool floor = piece.name.StartsWith("SM_Floor");
                var mats = r.sharedMaterials;
                for (int k = 0; k < mats.Length; k++)
                {
                    if (mats[k] == null || !Repaint.TryGetValue(mats[k].name, out string lib))
                    {
                        continue;
                    }

                    if (floor && lib != "M_HazardPaint" && lib != "M_WorkshopOrange")
                    {
                        lib = "M_FloorPlate";
                    }

                    var m = ArtLibrary.Get(lib);
                    if (m != null)
                    {
                        mats[k] = m;
                    }
                }

                r.sharedMaterials = mats;
            }
        }
    }
}
