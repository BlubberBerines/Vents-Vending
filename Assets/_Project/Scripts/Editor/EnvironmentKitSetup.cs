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
    /// Environment kit (custom scenery architecture, built in Blender by Art/Blender/Scripts/vv_build_env_*.py):
    /// configures the FBX imports (Blender VV_MAT_* slots -> M_Env_* toon library materials), makes one prefab per module
    /// with a simple box collider, and builds a look-dev preview corridor under the in-game maintenance lighting.
    /// Module rules: 2 m wide x 3 m tall, pivot at the bottom centre of the room-facing surface.
    /// </summary>
    public static class EnvironmentKitSetup
    {
        public const string KitFolder = "Assets/_Project/Art/Environment/Kit";
        public const string PrefabFolder = "Assets/_Project/Prefabs/Environment/Kit";
        public const string PreviewScene = "Assets/_Project/Scenes/LookDev/EnvKit_Preview.unity";
        public const string ShotFolder = "Docs/Art/Review/EnvKit";
        private const string VolumeProfilePath = "Assets/_Project/Settings/Rendering/GlobalVolumeProfile.asset";
        private const string Station = "Assets/ThirdParty/LowPolySciFiStation/FBX/";
        private const string Props = "Assets/ThirdParty/LowPolySciFiPropsPack/FBX/";
        private const string Cosmic = "Assets/ThirdParty/Cosmic_Retro_Station_Props_FREE/Prefabs/";

        private static readonly Dictionary<string, string> MaterialMap = new()
        {
            { "VV_MAT_StationPurple", "M_Env_StationPurple" },
            { "VV_MAT_FrameDark", "M_Env_FrameDark" },
            { "VV_MAT_TrimOrange", "M_Env_TrimOrange" },
            { "VV_MAT_HazardStripe", "M_Env_HazardStripe" },
            { "VV_MAT_StencilLilac", "M_Env_StencilLilac" },
            { "VV_MAT_EmissiveAmber", "M_Env_EmissiveAmber" },
            { "VV_MAT_IndustrialMetal", "M_BrushedSilver" },
            { "VV_MAT_DarkMetal", "M_DarkMetal" },
            { "VV_MAT_FloorMetal", "M_Env_FloorMetal" },
            { "VV_MAT_Void", "M_Env_Void" },
            { "VV_MAT_WindowGlow", "M_Env_WindowGlow" },
            { "VV_MAT_EmissiveCyanEnv", "M_Env_EmissiveCyan" },
            { "VV_MAT_EmissiveRed", "M_Env_EmissiveRed" },
            { "VV_MAT_GlassTint", "M_Env_GlassTint" },
        };

        /// <summary>Import settings + material remaps for every kit FBX, then (re)creates the prefabs.</summary>
        [MenuItem("Vents & Vending/Art/Environment Kit/Configure Imports + Prefabs")]
        public static string ConfigureAll()
        {
            ArtLibrary.CreateOrUpdateAll();
            EnsureGlassMaterial();
            var log = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { KitFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                imp.globalScale = 1f;
                imp.useFileScale = true;
                imp.importNormals = ModelImporterNormals.Import;
                imp.importAnimation = false;
                imp.animationType = ModelImporterAnimationType.None;
                imp.importCameras = false;
                imp.importLights = false;
                imp.addCollider = false;
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                foreach (var kv in MaterialMap)
                {
                    var mat = ArtLibrary.Get(kv.Value);
                    if (mat != null)
                    {
                        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), mat);
                    }
                }

                imp.SaveAndReimport();
                log.Add(MakePrefab(path));
            }

            log.Add(MakeCratePrefabs());
            log.Add(MakePropPrefabs());

            AssetDatabase.SaveAssets();
            return string.Join("\n", log);
        }

        /// <summary>+1 if the module's room side faces +Z after import, -1 if -Z (found from the emissive lamp lens, which sits proud of the face).</summary>
        public static float RoomSide(GameObject model)
        {
            // Walls are modelled behind their room-facing surface (the pivot), so the mesh centre lies behind it.
            Mesh mesh = model.GetComponentInChildren<MeshFilter>().sharedMesh;
            return -Mathf.Sign(mesh.bounds.center.z);
        }

        private static string MakePrefab(string modelPath)
        {
            EnsureFolder(PrefabFolder);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            string name = Path.GetFileNameWithoutExtension(modelPath);
            float side = RoomSide(go);
            var col = go.AddComponent<BoxCollider>();
            Bounds b = go.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
            if (name.StartsWith("ENV_Floor"))
            {
                col.size = new Vector3(2f, 0.2f, 2f);                 // walkable top at y = 0
                col.center = new Vector3(0f, -0.1f, 0f);
            }
            else if (name.StartsWith("ENV_Ceiling"))
            {
                col.size = new Vector3(2f, 0.3f, 2f);                 // underside at y = 0 (hanging fixtures excluded)
                col.center = new Vector3(0f, 0.15f, 0f);
            }
            else if (name.StartsWith("ENV_Wall_DoorSliding"))
            {
                Object.DestroyImmediate(col);
                WireSlidingDoor(go, -side * 0.15f);
            }
            else if (name.StartsWith("ENV_Wall_VentMouth"))
            {
                Object.DestroyImmediate(col);
                WireVentMouth(go, side);
            }
            else if (name.StartsWith("ENV_Duct"))
            {
                Object.DestroyImmediate(col);
                go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                AddVentZone(go.transform, "VentZone", b.center, b.size);
            }
            else
            {
                // walls: the body only, not the few cm of brackets / lamps / boxes that sit proud of the face
                col.size = new Vector3(2f, 3f, 0.3f);
                col.center = new Vector3(0f, 1.5f, -side * 0.15f);
            }
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI |
                                                       StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic |
                                                       StaticEditorFlags.ReflectionProbeStatic);
            string prefabPath = $"{PrefabFolder}/{Path.GetFileNameWithoutExtension(modelPath)}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return $"{prefabPath} (room side {(side > 0 ? "+Z" : "-Z")}, mesh {b.size})";
        }

        /// <summary>Door frame: two jambs + header (the 1.3 x 2.4 m opening stays passable); each leaf gets its own collider and
        /// slides outward into the jamb pocket (telescoping: outer leaves *1 by 0.335 m, inner leaves *2 by 0.655 m).</summary>
        private static void WireSlidingDoor(GameObject go, float bodyZ)
        {
            Box(go, new Vector3(-0.825f, 1.5f, bodyZ), new Vector3(0.35f, 3f, 0.3f));
            Box(go, new Vector3(0.825f, 1.5f, bodyZ), new Vector3(0.35f, 3f, 0.3f));
            Box(go, new Vector3(0f, 2.7f, bodyZ), new Vector3(1.3f, 0.6f, 0.3f));
            var leaves = new List<Transform>();
            var offsets = new List<Vector3>();
            foreach (string leafName in new[] { "Leaf_L1", "Leaf_L2", "Leaf_R1", "Leaf_R2" })
            {
                Transform leaf = go.transform.Find(leafName);
                if (leaf == null)
                {
                    continue;
                }

                leaf.gameObject.AddComponent<BoxCollider>();
                float sign = Mathf.Sign(leaf.GetComponent<MeshFilter>().sharedMesh.bounds.center.x);
                leaves.Add(leaf);
                offsets.Add(new Vector3(sign * (leafName.EndsWith("1") ? 0.335f : 0.655f), 0f, 0f));
            }

            go.AddComponent<VentsAndVending.World.SlidingDoor>().Configure(leaves.ToArray(), offsets.ToArray());
        }

        /// <summary>Wall around a 0.9 x 1.0 m opening with its sill at 1.8 m (the climb ledge: the top of the lower box is the
        /// stub floor), plus a crawl zone filling the mouth so the player crawls the moment they're in.</summary>
        private static void WireVentMouth(GameObject go, float side)
        {
            float z = -side * 0.15f;
            Box(go, new Vector3(-0.725f, 1.5f, z), new Vector3(0.55f, 3f, 0.3f));
            Box(go, new Vector3(0.725f, 1.5f, z), new Vector3(0.55f, 3f, 0.3f));
            Box(go, new Vector3(0f, 0.9f, z), new Vector3(0.9f, 1.8f, 0.3f));
            Box(go, new Vector3(0f, 2.9f, -side * 0.05f), new Vector3(0.9f, 0.2f, 0.1f));   // thin lintel: the riser roof takes over behind it
            AddVentZone(go.transform, "VentZone_Mouth", new Vector3(0f, 2.3f, z + side * 0.05f), new Vector3(0.9f, 1.0f, 0.4f));
        }

        private static void Box(GameObject go, Vector3 center, Vector3 size)
        {
            var c = go.AddComponent<BoxCollider>();
            c.center = center;
            c.size = size;
        }

        private static void AddVentZone(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var zone = new GameObject(name);
            zone.transform.SetParent(parent, false);
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = center;
            box.size = size;
            var vz = zone.AddComponent<VentsAndVending.World.VentZone>();
            var so = new SerializedObject(vz);
            so.FindProperty("_sectionId").stringValue = "L2";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Crates from the props arsenal: a pushable one and a fixed one (the fixed crate guarantees a route).</summary>
        private static string MakeCratePrefabs()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/LowPolySciFiPropsPack/FBX/crate.fbx");
            if (model == null)
            {
                return "crate model missing";
            }

            foreach (bool pushable in new[] { true, false })
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
                go.AddComponent<BoxCollider>();
                if (pushable)
                {
                    go.AddComponent<VentsAndVending.World.PushableCrate>();
                }

                PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/ENV_Crate_{(pushable ? "Pushable" : "Fixed")}_A.prefab");
                Object.DestroyImmediate(go);
            }

            return $"{PrefabFolder}/ENV_Crate_Pushable_A.prefab + ENV_Crate_Fixed_A.prefab";
        }

        /// <summary>Viewport glass: the one transparent kit material (URP Lit, transparent, tinted lilac).</summary>
        private static Material EnsureGlassMaterial()
        {
            string path = $"{ArtLibrary.Folder}/M_Env_GlassTint.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetFloat("_Surface", 1f);                      // transparent
            mat.SetFloat("_Blend", 0f);                        // alpha
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", new Color(0.42f, 0.36f, 0.56f, 0.22f));
            mat.SetFloat("_Smoothness", 0.95f);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ props arsenal (repainted pack props)

        public const string PropFolder = "Assets/_Project/Prefabs/Environment/Props";
        private const string StationFbx = "Assets/ThirdParty/LowPolySciFiStation/FBX/";
        private const string PropsFbx = "Assets/ThirdParty/LowPolySciFiPropsPack/FBX/";

        /// <summary>Pack material name -> toon library material (anything not listed keeps the pack material: glass,
        /// plants, force fields, green glows).</summary>
        private static readonly Dictionary<string, string> PropRepaint = new()
        {
            { "M_Hull", "M_Env_StationPurple" }, { "M_HullLight", "M_Env_StationPurple" }, { "M_HullWhite", "M_Env_StationPurple" },
            { "hull", "M_Env_StationPurple" }, { "hull2", "M_Env_StationPurple" },
            { "M_Metal", "M_BrushedSilver" }, { "steel", "M_BrushedSilver" },
            { "M_HullDark", "M_Env_FrameDark" }, { "M_Gunmetal", "M_Env_FrameDark" }, { "M_Black", "M_Env_FrameDark" },
            { "M_Navy", "M_Env_FrameDark" }, { "M_Rubber", "M_Env_FrameDark" }, { "M_Blue", "M_DarkMetal" },
            { "dark", "M_Env_FrameDark" }, { "black", "M_Env_FrameDark" },
            { "M_Hazard", "M_Env_HazardStripe" },
            { "M_Orange", "M_Env_TrimOrange" }, { "M_Olive", "M_Env_TrimOrange" },
            { "olive", "M_Env_TrimOrange" }, { "orangeS", "M_Env_TrimOrange" }, { "yellowS", "M_Env_TrimOrange" },
            { "M_Linen", "M_Cardboard" }, { "M_Cushion", "M_Cardboard" }, { "M_Carpet", "M_Cardboard" },
            { "M_GlowCyan", "M_Env_EmissiveCyan" }, { "M_Screen", "M_Env_EmissiveCyan" }, { "glow_cyan", "M_Env_EmissiveCyan" },
            { "screen_glow", "M_Env_EmissiveCyan" }, { "holo", "M_Env_EmissiveCyan" },
            { "M_ScreenAmber", "M_Env_EmissiveAmber" }, { "M_GlowOrange", "M_Env_EmissiveAmber" }, { "M_LightStrip", "M_Env_EmissiveAmber" },
            { "M_GlowRed", "M_Env_EmissiveRed" }, { "glow_red", "M_Env_EmissiveRed" },
        };

        private static readonly string[] PropModels =
        {
            // workstations & furniture
            StationFbx + "SM_Desk_Terminal", StationFbx + "SM_Console_Standing", StationFbx + "SM_Eng_Control_Panel",
            StationFbx + "SM_Bunk_Bed", StationFbx + "SM_Stool", StationFbx + "SM_Mess_Table", StationFbx + "SM_Mess_Bench",
            StationFbx + "SM_Locker", StationFbx + "SM_Locker_Row", StationFbx + "SM_Holo_Projector",
            PropsFbx + "computer_terminal", PropsFbx + "control_panel", PropsFbx + "server_rack", PropsFbx + "console_chair",
            PropsFbx + "robot_arm", PropsFbx + "hologram_table",
            // storage & clutter
            StationFbx + "SM_Crate_Small", StationFbx + "SM_Crate_Large", StationFbx + "SM_Crate_Stack", StationFbx + "SM_Fuel_Barrel",
            StationFbx + "SM_Cable_Spool", StationFbx + "SM_Toolbox", StationFbx + "SM_Tool_Cart", StationFbx + "SM_Datapad",
            StationFbx + "SM_Plant_Pot", StationFbx + "SM_Floor_Lamp", StationFbx + "SM_Hydro_Rack",
            PropsFbx + "crate_small", PropsFbx + "barrel", PropsFbx + "energy_cell", PropsFbx + "ammo_box", PropsFbx + "med_kit",
            // engineering & wall-mounted
            StationFbx + "SM_Generator", StationFbx + "SM_Coolant_Tank", StationFbx + "SM_Power_Conduit", StationFbx + "SM_Reactor_Core",
            StationFbx + "SM_Fuse_Box", StationFbx + "SM_Medkit_Wall", StationFbx + "SM_Fire_Extinguisher", StationFbx + "SM_Comm_Panel",
            PropsFbx + "pipe_straight", PropsFbx + "pipe_corner", PropsFbx + "fuel_tank", PropsFbx + "wall_light",
        };

        /// <summary>The props arsenal: pack props repainted with the toon library (so they share the scenery's lighting
        /// model and edge wear) with a simple box collider, as PROP_&lt;name&gt; prefabs. Cosmic Retro props are used as-is.</summary>
        [MenuItem("Vents & Vending/Art/Environment Kit/Make Prop Prefabs")]
        public static string MakePropPrefabs()
        {
            EnsureFolder(PropFolder);
            int made = 0;
            foreach (string model in PropModels)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(model + ".fbx");
                if (asset == null)
                {
                    Debug.LogWarning($"Props: missing {model}.fbx");
                    continue;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] != null && PropRepaint.TryGetValue(mats[i].name, out string lib) && ArtLibrary.Get(lib) is Material m)
                        {
                            mats[i] = m;
                        }
                    }

                    r.sharedMaterials = mats;
                }

                Bounds b = default;
                bool first = true;
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    if (first) { b = r.bounds; first = false; } else { b.Encapsulate(r.bounds); }
                }

                var col = go.AddComponent<BoxCollider>();
                col.center = go.transform.InverseTransformPoint(b.center);
                col.size = b.size;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
                string name = "PROP_" + Path.GetFileNameWithoutExtension(model).Replace("SM_", "");
                PrefabUtility.SaveAsPrefabAsset(go, $"{PropFolder}/{name}.prefab");
                Object.DestroyImmediate(go);
                made++;
            }

            return $"{made} prop prefabs in {PropFolder}";
        }

        // ------------------------------------------------------------------ preview corridor

        [MenuItem("Vents & Vending/Art/Environment Kit/Build Preview Corridor")]
        public static string BuildPreview()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new System.InvalidOperationException("An open scene has unsaved changes; save or discard them first.");
                }
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Lighting();
            var root = new GameObject("EnvKit_Corridor").transform;
            GameObject Kit(string n) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{n}.prefab");
            float faceIn = RoomSide(Kit("ENV_Wall_Standard_A")) < 0 ? 0f : 180f;   // yaw turning a wall's room side to -Z
            string[] left = { "ENV_Wall_Standard_A", "ENV_Wall_Window_A", "ENV_Wall_Standard_A", "ENV_Wall_Braced_A", "ENV_Wall_Utility_A" };
            string[] right = { "ENV_Wall_Braced_A", "ENV_Wall_Utility_A", "ENV_Wall_Window_A", "ENV_Wall_Standard_A", "ENV_Wall_Braced_A" };

            // walls: 5 modules each side (inner faces at z = +-2), 2 at the far end (inner face at x = 10)
            for (int i = 0; i < 5; i++)
            {
                float x = 1f + 2f * i;
                Place(root, Kit(left[i]), new Vector3(x, 0f, 2f), faceIn);
                Place(root, Kit(right[i]), new Vector3(x, 0f, -2f), faceIn + 180f);
                if (left[i] == "ENV_Wall_Standard_A")
                {
                    Lamp(root, new Vector3(x, 2.5f, 1.8f));
                }

                if (right[i] == "ENV_Wall_Standard_A")
                {
                    Lamp(root, new Vector3(x, 2.5f, -1.8f));
                }
            }

            Place(root, Kit("ENV_Wall_Window_A"), new Vector3(10f, 0f, 1f), faceIn + 90f);
            Place(root, Kit("ENV_Wall_Standard_A"), new Vector3(10f, 0f, -1f), faceIn + 90f);
            Lamp(root, new Vector3(9.8f, 2.5f, -1f));

            // floor + ceiling modules on the 2 m grid (one extra row behind the camera)
            for (int i = -1; i < 5; i++)
            {
                float x = 1f + 2f * i;
                foreach (float z in new[] { -1f, 1f })
                {
                    bool grate = (i == 1 && z < 0) || (i == 3 && z > 0);
                    Place(root, Kit(grate ? "ENV_Floor_Grate_A" : "ENV_Floor_Solid_A"), new Vector3(x, 0f, z), 0f);
                    bool light = (i == 0 || i == 2 || i == 4) && z > 0;
                    Place(root, Kit(light ? "ENV_Ceiling_Light_A" : "ENV_Ceiling_Standard_A"), new Vector3(x, 3f, z), 0f);
                    if (light)
                    {
                        Point(root, "Ceiling Panel Light", new Vector3(x, 2.7f, z), new Color(1f, 0.78f, 0.5f), 5.5f, 1.3f, i == 2);
                    }
                }
            }

            // props from the arsenal (Station/Props packs face -Z at yaw 0, Cosmic faces +Z)
            Prop(root, Cosmic + "CR_Locker_Emergency_Red.prefab", 2.6f, 0f, 1.6f, 180f);
            Prop(root, Station + "SM_Crate_Stack.fbx", 3.4f, 0f, -1.35f, 180f);
            Prop(root, Props + "barrel.fbx", 4.4f, 0f, -1.5f, 180f);
            var table = Prop(root, Cosmic + "CR_Table_Small_2.prefab", 6.2f, 0f, 1.35f, 180f);
            Prop(root, Cosmic + "CR_Monitor_Small_1.prefab", 6.1f, Top(table, root), 1.45f, 180f);
            Prop(root, Cosmic + "CR_Chair_Medium.prefab", 6.2f, 0f, 0.5f, 0f);
            Prop(root, Station + "SM_Tool_Cart.fbx", 7.4f, 0f, -1.4f, 180f);
            Prop(root, Props + "server_rack.fbx", 8.6f, 0f, 1.55f, 0f);
            Prop(root, Station + "SM_Generator.fbx", 8.9f, 0f, -1.2f, 180f);
            Prop(root, Cosmic + "CR_StorageCrate_Large_1.prefab", 5.2f, 0f, -1.5f, 0f);

            View(root, "View_1_Corridor", new Vector3(0.3f, 1.7f, 0f), Quaternion.Euler(4f, 90f, 0f));
            View(root, "View_2_WallCloseup", new Vector3(3.3f, 1.65f, -0.7f), Quaternion.Euler(6f, 38f, 0f));
            View(root, "View_3_PropsRight", new Vector3(2.2f, 1.6f, 0.9f), Quaternion.Euler(12f, 128f, 0f));
            View(root, "View_4_EndWall", new Vector3(6.2f, 1.6f, -0.2f), Quaternion.Euler(2f, 80f, 0f));

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

            EnsureFolder(Path.GetDirectoryName(PreviewScene)!.Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, PreviewScene);
            return PreviewScene;
        }

        [MenuItem("Vents & Vending/Art/Environment Kit/Capture Preview Shots")]
        public static string Capture()
        {
            Directory.CreateDirectory(ShotFolder);
            const int w = 1600, h = 900;
            var cam = Camera.main;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            cam.targetTexture = rt;
            cam.Render();
            cam.Render();                                   // warm-up: the first render after a scene load is black
            var shots = new List<string>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!t.name.StartsWith("View_"))
                {
                    continue;
                }

                cam.transform.SetPositionAndRotation(t.position, t.rotation);
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                string path = $"{ShotFolder}/ENV_Kit_Unity_{t.name.Substring(5)}.png";
                File.WriteAllBytes(path, tex.EncodeToPNG());
                shots.Add(path);
            }

            cam.targetTexture = null;
            Object.DestroyImmediate(tex);
            rt.Release();
            return string.Join("\n", shots);
        }

        // ------------------------------------------------------------------ helpers

        internal static void Lighting()
        {
            // In-game maintenance tunnel values (Greybox_Movement) + art-direction accents
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.24f, 0.24f, 0.30f);
            RenderSettings.ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f);
            RenderSettings.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.08f);
            RenderSettings.fogDensity = 0.018f;
            var vol = new GameObject("Global Volume").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);

            var rig = new GameObject("Lights").transform;
            Point(rig, "Cold Fill", new Vector3(4f, 1.2f, -0.6f), new Color(0.5f, 0.65f, 0.9f), 6f, 0.6f, false);
            Point(rig, "Tech Cyan", new Vector3(6.2f, 1.3f, 0.6f), new Color(0.2f, 0.9f, 1f), 3.5f, 1f, false);
            Point(rig, "Tech Magenta", new Vector3(8.4f, 0.6f, -0.7f), new Color(1f, 0.25f, 0.85f), 3.5f, 1f, false);
            Point(rig, "Emergency Red", new Vector3(9.4f, 2.6f, 0f), new Color(1f, 0.15f, 0.1f), 4f, 1.4f, false);
        }

        private static void Lamp(Transform root, Vector3 pos) =>
            Point(root, "Wall Lamp Light", pos, new Color(1f, 0.7f, 0.42f), 3f, 0.75f, false);

        private static void Point(Transform parent, string name, Vector3 pos, Color c, float range, float intensity, bool shadows)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.localPosition = pos;
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.intensity = intensity;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }

        private static void Place(Transform root, GameObject prefab, Vector3 pos, float yaw)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            go.transform.SetLocalPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        }

        private static GameObject Prop(Transform root, string path, float x, float y, float z, float yaw)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning($"EnvKit preview: missing {path}");
                return new GameObject("MISSING");
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, root);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * asset.transform.localRotation;
            return go;
        }

        private static float Top(GameObject go, Transform root)
        {
            float top = 0f;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                top = Mathf.Max(top, r.bounds.max.y);
            }

            return top - root.position.y;
        }

        private static void Slab(Transform root, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void View(Transform root, string name, Vector3 pos, Quaternion rot)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.SetLocalPositionAndRotation(pos, rot);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
