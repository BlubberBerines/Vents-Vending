using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VentsAndVending.Interaction;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Station layers, Plan 1 test bay (Docs/superpowers/plans/2026-11-09-station-layers-plan1-mechanics-l3.md, Task 6):
    /// an L3 maintenance block built from the environment kit to prove the layer mechanics before the full assembly.
    /// Den (sliding door) → corridor → crate → head-height vent mouth → riser → L2 crawl duct → opening into a dummy L1
    /// concourse. Not in the build; the playable map stays Greybox_Movement until the assembly replaces it.
    ///
    /// Layout (+Z north, kit walls face +Z at yaw 0, room faces are the grid lines):
    ///   Den        x[-3, 3]   z[-6.3, -0.3]  floor 0, ceiling 3   (door module on z = 0 facing the corridor)
    ///   Corridor   x[-3, 13]  z[0, 4]        floor 0, ceiling 3   vent mouths on z = 4 at x 4 (fixed crate) and 10 (pushable)
    ///   L2 ducts   floor y 3.3: risers z 4.3 → 6.3, runs north to z 12.3, risers up to y 4.8 at z 14.3
    ///   L1 stub    x[-2, 12]  z[14.3, 22]    floor 4.6
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        public const string StationTestBayPath = "Assets/_Project/Scenes/LookDev/Station_TestBay.unity";
        private const string Kit = EnvironmentKitSetup.PrefabFolder;

        [MenuItem("Vents & Vending/Prototype/Build Station Test Bay")]
        public static string BuildStationTestBay()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new System.InvalidOperationException("An open scene has unsaved changes; save or discard them first.");
                }
            }

            EnsureInputActions();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnvironmentKitSetup.Lighting();
            // the shared preview rig's accents sit where the preview corridor needed them; here they belong on the utility
            // wall's screens (cyan) and the hydroponics rack (magenta)
            GameObject.Find("Tech Cyan").transform.position = new Vector3(8f, 1.7f, 3.2f);
            GameObject.Find("Tech Magenta").transform.position = new Vector3(12f, 1.0f, 0.9f);
            var root = new GameObject("Station_TestBay").transform;
            var l3 = Group("L3_Maintenance", root);
            var l2 = Group("L2_Crawlspace", root);
            var l1 = Group("L1_Concourse_Stub", root);

            // ---------------- L3: den
            float[] denZ = { -1.3f, -3.3f, -5.3f };
            foreach (float x in new[] { -2f, 0f, 2f })
            {
                foreach (float z in denZ)
                {
                    Kitp(l3, "ENV_Floor_Solid_A", x, 0f, z, 0f);
                    Kitp(l3, x == 0f && z == -3.3f ? "ENV_Ceiling_Light_A" : "ENV_Ceiling_Standard_A", x, 3f, z, 0f);
                }

                Kitp(l3, x == 0f ? "ENV_Wall_Utility_A" : "ENV_Wall_Standard_A", x, 0f, -6.3f, 0f);   // south, faces in
            }

            foreach (float z in denZ)
            {
                Kitp(l3, "ENV_Wall_Braced_A", -3f, 0f, z, 90f);    // west, faces +X
                Kitp(l3, "ENV_Wall_Standard_A", 3f, 0f, z, -90f);  // east, faces -X
            }

            CreatePointLight("Den Light", new Vector3(0f, 2.6f, -3.3f), new Color(1f, 0.8f, 0.55f), 6f, 1.6f, l3);

            // ---------------- L3: corridor (south wall line z = 0 incl. the den door; north wall z = 4 with two vent mouths)
            for (float x = -2f; x <= 12f; x += 2f)
            {
                foreach (float z in new[] { 1f, 3f })
                {
                    Kitp(l3, "ENV_Floor_Solid_A", x, 0f, z, 0f);
                    bool light = (x == 2f || x == 8f) && z == 3f;
                    Kitp(l3, light ? "ENV_Ceiling_Light_A" : "ENV_Ceiling_Standard_A", x, 3f, z, 0f);
                    if (light)
                    {
                        CreatePointLight("Corridor Light", new Vector3(x, 2.65f, z), new Color(1f, 0.8f, 0.55f), 6f, 1.5f, l3);
                    }
                }

                string south = x == 0f ? "ENV_Wall_DoorSliding_A" : (x == 6f || x == 10f ? "ENV_Wall_Viewport_A" : (x == 2f ? "ENV_Wall_Window_A" : "ENV_Wall_Standard_A"));
                Kitp(l3, south, x, 0f, 0f, 0f);
                string north = x == 4f || x == 10f ? "ENV_Wall_VentMouth_A" : (x == 8f ? "ENV_Wall_Utility_A" : "ENV_Wall_Braced_A");
                Kitp(l3, north, x, 0f, 4f, 180f);
            }

            foreach (float z in new[] { 1f, 3f })
            {
                Kitp(l3, "ENV_Wall_Standard_A", -3f, 0f, z, 90f);
                Kitp(l3, "ENV_Wall_Standard_A", 13f, 0f, z, -90f);
            }

            // crates: a fixed one under mouth A (the route always works), a pushable one for mouth B (2 pushes east, 2 north)
            Kitp(l3, "ENV_Crate_Fixed_A", 4f, 0f, 3.42f, 0f);
            Kitp(l3, "ENV_Crate_Pushable_A", 9f, 0f, 2.42f, 0f);

            // ---------------- L2: risers + crawl ducts (floor y 3.3)
            Kitp(l2, "ENV_Duct_Riser_A", 4f, 1.8f, 4.3f, 180f);
            Kitp(l2, "ENV_Duct_Riser_A", 10f, 1.8f, 4.3f, 180f);
            foreach (float z in new[] { 7.3f, 9.3f, 11.3f })
            {
                Kitp(l2, "ENV_Duct_Straight_A", 4f, 3.3f, z, 90f);
            }

            Kitp(l2, "ENV_Duct_Corner_A", 10f, 3.3f, 7.3f, 90f);    // from the riser (south) turning west
            Kitp(l2, "ENV_Duct_Straight_A", 8f, 3.3f, 7.3f, 0f);
            Kitp(l2, "ENV_Duct_Corner_A", 6f, 3.3f, 7.3f, -90f);    // from the east turning north
            foreach (float z in new[] { 9.3f, 11.3f })
            {
                Kitp(l2, "ENV_Duct_Straight_A", 6f, 3.3f, z, 90f);
            }

            Kitp(l2, "ENV_Duct_Riser_A", 4f, 3.3f, 12.3f, 180f);    // up to the concourse wall (opening floor y 4.8)
            Kitp(l2, "ENV_Duct_Riser_A", 6f, 3.3f, 12.3f, 180f);
            foreach (float z in new[] { 6.3f, 10.3f })
            {
                CreatePointLight("Duct Glow", new Vector3(4f, 3.8f, z), new Color(1f, 0.65f, 0.35f), 2.5f, 0.6f, l2);
                CreatePointLight("Duct Glow", new Vector3(6f, 3.8f, z + 1f), new Color(1f, 0.65f, 0.35f), 2.5f, 0.6f, l2);
            }

            // ---------------- L1: dummy concourse stub (floor 4.6), duct openings in its south wall
            Material floorMat = ArtLibrary.Get("M_PlazaTile");
            Material wallMat = ArtLibrary.Get("M_PlazaPanel");
            Block("Concourse Floor", l1, new Vector3(5f, 4.5f, 18.15f), new Vector3(14f, 0.2f, 7.7f), floorMat);
            // south wall (y 4.6-9.2) with the two 1 m duct openings at x 3.5-4.5 and 5.5-6.5, y 4.8-5.8
            Block("Concourse South Wall W", l1, new Vector3(0.75f, 6.9f, 14.4f), new Vector3(5.5f, 4.6f, 0.2f), wallMat);
            Block("Concourse South Wall Mid", l1, new Vector3(5f, 6.9f, 14.4f), new Vector3(1f, 4.6f, 0.2f), wallMat);
            Block("Concourse South Wall E", l1, new Vector3(9.25f, 6.9f, 14.4f), new Vector3(5.5f, 4.6f, 0.2f), wallMat);
            Block("Concourse Wall Over Vents", l1, new Vector3(5f, 7.5f, 14.4f), new Vector3(3f, 3.4f, 0.2f), wallMat);
            Block("Concourse Wall Under Vents", l1, new Vector3(5f, 4.7f, 14.4f), new Vector3(3f, 0.2f, 0.2f), wallMat);
            Block("Concourse North Wall", l1, new Vector3(5f, 6.9f, 22f), new Vector3(14f, 4.6f, 0.2f), wallMat);
            Block("Concourse West Wall", l1, new Vector3(-2f, 6.9f, 18.15f), new Vector3(0.2f, 4.6f, 7.7f), wallMat);
            Block("Concourse East Wall", l1, new Vector3(12f, 6.9f, 18.15f), new Vector3(0.2f, 4.6f, 7.7f), wallMat);
            CreatePointLight("Concourse Cyan", new Vector3(1f, 8f, 18f), new Color(0.2f, 0.9f, 1f), 12f, 3f, l1);
            CreatePointLight("Concourse Magenta", new Vector3(9f, 8f, 18f), new Color(1f, 0.25f, 0.85f), 12f, 3f, l1);
            var customer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            customer.name = "Customer Placeholder";
            customer.transform.SetParent(l1, false);
            customer.transform.localPosition = new Vector3(5f, 5.6f, 16.5f);
            customer.GetComponent<Renderer>().sharedMaterial = ArtLibrary.Get("M_Env_TrimOrange");

            // ---------------- dressing, depth, lighting circuit
            DressTestBayDen(l3);
            DressTestBayCorridor(l3);
            BuildTestBayShaft(root);
            BuildTestBayLightCircuit(root, l3);

            // ---------------- player, HUD, review views
            var player = CreatePlayer(new Vector3(0f, 0f, -3.5f), Quaternion.identity);
            CreateHud(player.GetComponent<InteractionController>());
            TestBayView(root, "View_1_DenDoor", new Vector3(-1.2f, 1.6f, -4.6f), Quaternion.Euler(4f, 18f, 0f));
            TestBayView(root, "View_2_CrateAndMouth", new Vector3(2.6f, 1.7f, 0.9f), Quaternion.Euler(-8f, 28f, 0f));
            TestBayView(root, "View_3_Riser", new Vector3(4f, 2.25f, 4.15f), Quaternion.Euler(-24f, 0f, 0f));
            TestBayView(root, "View_4_DuctToConcourse", new Vector3(4f, 3.85f, 9.0f), Quaternion.Euler(-6f, 0f, 0f));
            TestBayView(root, "View_5_DenWorkstation", new Vector3(1.6f, 1.65f, -1.4f), Quaternion.Euler(12f, 215f, 0f));
            TestBayView(root, "View_6_DenFromDesk", new Vector3(-1.6f, 1.6f, -5.0f), Quaternion.Euler(6f, 35f, 0f));
            TestBayView(root, "View_7_CorridorEast", new Vector3(-2.2f, 1.7f, 2f), Quaternion.Euler(3f, 90f, 0f));
            TestBayView(root, "View_8_ViewportShaft", new Vector3(6f, 1.75f, 1.3f), Quaternion.Euler(18f, 180f, 0f));

            Directory.CreateDirectory(Path.GetDirectoryName(StationTestBayPath)!);
            EditorSceneManager.SaveScene(scene, StationTestBayPath);
            return StationTestBayPath;
        }

        private static GameObject Kitp(Transform parent, string prefab, float x, float y, float z, float yaw)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Kit}/{prefab}.prefab");
            if (asset == null)
            {
                throw new FileNotFoundException($"Kit prefab missing: {prefab} (run Environment Kit/Configure Imports + Prefabs)");
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.SetLocalPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        private static void TestBayView(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.SetLocalPositionAndRotation(position, rotation);
        }
    }
}
