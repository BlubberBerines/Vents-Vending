using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VentsAndVending.World;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Station test bay, dressing pass: a lived-in den (workstation, workbench, bunk, storage, clutter), a busy corridor
    /// (engineering kit, storage, pipe runs), depth (two viewports onto a deep machine shaft), and the ceiling lighting
    /// circuit with its den switch plus always-on red emergency glow.
    /// Facing: pack props (PROP_*) face -Z at yaw 0; Cosmic Retro props face +Z at yaw 0.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        private const string Props = EnvironmentKitSetup.PropFolder;
        private const string CosmicP = "Assets/ThirdParty/Cosmic_Retro_Station_Props_FREE/Prefabs/";
        public const string CeilingCircuitId = "L3_CEILING";

        // yaw for a pack prop whose front should face this direction
        private const float FacePlusZ = 180f, FaceMinusZ = 0f, FacePlusX = -90f, FaceMinusX = 90f;

        private static void DressTestBayDen(Transform l3)
        {
            var den = Group("Den_Dressing", l3);
            // workstation against the south wall
            var desk = Prop(den, Props + "/PROP_Desk_Terminal.prefab", -1.2f, 0f, -5.75f, FacePlusZ);
            Prop(den, CosmicP + "CR_Chair_Medium.prefab", -1.2f, 0f, -4.85f, 180f);
            Prop(den, Props + "/PROP_Datapad.prefab", -0.75f, TopOf(desk), -5.85f, 160f);
            Prop(den, Props + "/PROP_Crate_Stack.prefab", -2.45f, 0f, -5.65f, 10f);
            // workbench: table with tools, robot arm, tool cart
            var bench = Prop(den, CosmicP + "CR_Table_Small_2.prefab", 1.25f, 0f, -5.7f, 0f);
            Prop(den, Props + "/PROP_Toolbox.prefab", 1.0f, TopOf(bench), -5.75f, 15f);
            Prop(den, CosmicP + "CR_Tool_Hammer.prefab", 1.55f, TopOf(bench), -5.55f, 70f);
            Prop(den, Props + "/PROP_energy_cell.prefab", 1.6f, TopOf(bench), -5.9f, 0f);
            Prop(den, Props + "/PROP_robot_arm.prefab", 2.45f, 0f, -5.75f, FacePlusZ);
            Prop(den, Props + "/PROP_Tool_Cart.prefab", 2.35f, 0f, -4.3f, FaceMinusX);
            // sleeping corner + storage
            Prop(den, Props + "/PROP_Bunk_Bed.prefab", -2.35f, 0f, -3.1f, FacePlusX);
            Prop(den, Props + "/PROP_Locker_Row.prefab", 2.6f, 0f, -2.7f, FaceMinusX);
            Prop(den, Props + "/PROP_Plant_Pot.prefab", 0.3f, 0f, -5.85f, 0f);
            Prop(den, Props + "/PROP_Floor_Lamp.prefab", -2.6f, 0f, -0.9f, 0f);
            Prop(den, Props + "/PROP_Crate_Small.prefab", 2.45f, 0f, -0.75f, 25f);
            Prop(den, Props + "/PROP_ammo_box.prefab", 2.35f, 0.42f, -0.8f, 40f);
            Prop(den, Props + "/PROP_Fire_Extinguisher.prefab", -1.0f, 0f, -0.6f, FacePlusZ);
            Prop(den, Props + "/PROP_Medkit_Wall.prefab", -2.97f, 1.5f, -4.6f, FacePlusX);
            Prop(den, Props + "/PROP_Cable_Spool.prefab", 0.9f, 0f, -3.6f, 30f);
        }

        private static void DressTestBayCorridor(Transform l3)
        {
            var c = Group("Corridor_Dressing", l3);
            // north side (props face -Z, into the corridor)
            Prop(c, Props + "/PROP_Coolant_Tank.prefab", -2.1f, 0f, 3.35f, FaceMinusZ);
            Prop(c, Props + "/PROP_Locker.prefab", -0.2f, 0f, 3.65f, FaceMinusZ);
            Prop(c, Props + "/PROP_server_rack.prefab", 1.4f, 0f, 3.55f, FaceMinusZ);
            Prop(c, Props + "/PROP_Generator.prefab", 6.9f, 0f, 3.3f, FaceMinusZ);
            Prop(c, Props + "/PROP_Cable_Spool.prefab", 8.2f, 0f, 3.5f, 20f);
            Prop(c, Props + "/PROP_Crate_Stack.prefab", 12.1f, 0f, 3.35f, FaceMinusZ);
            Prop(c, Props + "/PROP_Fuse_Box.prefab", 2f, 1.5f, 3.97f, FaceMinusZ);
            // south side (props face +Z)
            Prop(c, Props + "/PROP_Fuel_Barrel.prefab", -2.3f, 0f, 0.5f, 0f);
            Prop(c, Props + "/PROP_barrel.prefab", -1.7f, 0f, 0.45f, 60f);
            Prop(c, Props + "/PROP_Tool_Cart.prefab", 2.3f, 0f, 0.6f, FacePlusZ);
            Prop(c, Props + "/PROP_Console_Standing.prefab", 4f, 0f, 0.45f, FacePlusZ);
            Prop(c, Props + "/PROP_crate_small.prefab", 7.9f, 0f, 0.45f, 12f);
            Prop(c, Props + "/PROP_Crate_Small.prefab", 8.4f, 0f, 0.5f, -20f);
            Prop(c, Props + "/PROP_Hydro_Rack.prefab", 12.1f, 0f, 0.5f, FacePlusZ);
            Prop(c, Props + "/PROP_med_kit.prefab", 12.3f, 0f, 1.6f, 80f);
            // pipe runs along the north wall just under the ceiling line
            foreach (float x in new[] { -2f, 0f, 2f, 6f, 8f })
            {
                Prop(c, Props + "/PROP_pipe_straight.prefab", x, 2.5f, 3.72f, 0f);
            }
        }

        /// <summary>Ceiling circuit: every ceiling light fixture's glow + the point lights under them; plus an always-on red
        /// emergency glow so "lights off" is dark, not black. The den switch sits on the east wall beside the door.</summary>
        private static void BuildTestBayLightCircuit(Transform root, Transform l3)
        {
            var lights = new List<Light>();
            var glows = new List<Renderer>();
            foreach (Transform t in l3.GetComponentsInChildren<Transform>())
            {
                if (t.name.StartsWith("ENV_Ceiling_Light_A"))
                {
                    glows.AddRange(t.GetComponentsInChildren<Renderer>());
                }
                else if ((t.name == "Corridor Light" || t.name == "Den Light") && t.TryGetComponent(out Light l))
                {
                    lights.Add(l);
                }
            }

            var circuitGo = new GameObject("Circuit_" + CeilingCircuitId);
            circuitGo.transform.SetParent(root, false);
            circuitGo.AddComponent<LightCircuit>().Configure(CeilingCircuitId, lights.ToArray(), glows.ToArray());

            var emergency = Group("Emergency_Glow", root);
            foreach (Vector3 p in new[] { new Vector3(0f, 0.35f, -0.6f), new Vector3(-2f, 0.35f, 2f), new Vector3(4f, 0.35f, 2f), new Vector3(10f, 0.35f, 2f) })
            {
                CreatePointLight("Emergency Red", p, new Color(1f, 0.12f, 0.08f), 4f, 0.35f, emergency);
            }

            // the switch: Cosmic wall control panel + a status lamp; green = on, red = off
            var panel = Prop(l3, CosmicP + "CR_WallPanel_Control_2.prefab", 2.96f, 1.2f, -1.1f, -90f);
            panel.name = "Den Light Switch";
            if (panel.GetComponentInChildren<Collider>() == null)
            {
                var box = panel.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0f, 0.1f);
                box.size = new Vector3(0.5f, 0.7f, 0.2f);
            }

            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "Status Lamp";
            Object.DestroyImmediate(lamp.GetComponent<Collider>());
            lamp.transform.SetParent(panel.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 0.42f, 0.12f);
            lamp.transform.localScale = Vector3.one * 0.06f;
            lamp.GetComponent<Renderer>().sharedMaterial = ArtLibrary.Get("M_Env_EmissiveRed");
            panel.AddComponent<LightSwitch>().Configure(CeilingCircuitId, lamp.GetComponent<Renderer>());
        }

        /// <summary>Depth: a deep machine shaft behind the corridor's viewports (x 6 and 10): towering pipe stacks, a catwalk,
        /// a reactor glowing far below, and lights at several depths so fog reads the distance.</summary>
        private static void BuildTestBayShaft(Transform root)
        {
            var g = Group("Machine_Shaft", root);
            Material dark = ArtLibrary.Get("M_Env_FrameDark");
            Material purple = ArtLibrary.Get("M_Env_StationPurple");
            Material deck = ArtLibrary.Get("M_Env_FloorMetal");
            Material orange = ArtLibrary.Get("M_Env_TrimOrange");
            // shell x[3.6, 14] z[-14, -0.35] y[-16, 10]
            Block("Shaft Floor", g, new Vector3(8.8f, -16.1f, -7.2f), new Vector3(10.6f, 0.2f, 13.7f), deck);
            Block("Shaft Far Wall", g, new Vector3(8.8f, -3f, -14.1f), new Vector3(10.6f, 26f, 0.2f), purple);
            Block("Shaft West Wall", g, new Vector3(3.5f, -3f, -7.2f), new Vector3(0.2f, 26f, 13.7f), dark);
            Block("Shaft East Wall", g, new Vector3(14.1f, -3f, -7.2f), new Vector3(0.2f, 26f, 13.7f), dark);
            Block("Shaft Roof", g, new Vector3(8.8f, 10.1f, -7.2f), new Vector3(10.6f, 0.2f, 13.7f), dark);
            Block("Shaft Near Wall Below", g, new Vector3(8.8f, -8.2f, -0.3f), new Vector3(10.6f, 16f, 0.1f), dark);
            for (int i = 0; i < 6; i++)                                                         // horizontal ribs on the far wall
            {
                Block($"Far Rib {i}", g, new Vector3(8.8f, -14f + i * 4f, -13.95f), new Vector3(10.4f, 0.25f, 0.15f), orange);
            }

            foreach (Vector3 p in new[] { new Vector3(5f, -3f, -9f), new Vector3(6.2f, -3f, -11.5f), new Vector3(12.4f, -3f, -10f), new Vector3(11f, -3f, -4.5f) })
            {
                var pipe = Part("Pipe Stack", g, p, new Vector3(0.9f, 13f, 0.9f), dark, PrimitiveType.Cylinder);
                for (int k = 0; k < 5; k++)
                {
                    Part("Pipe Band", g, p + Vector3.up * (-10f + k * 5f), new Vector3(1.0f, 0.12f, 1.0f), orange, PrimitiveType.Cylinder);
                }
            }

            // catwalk crossing the shaft below the viewports, with rails
            Block("Catwalk", g, new Vector3(8.8f, -1.6f, -5.5f), new Vector3(10.4f, 0.12f, 1.4f), deck);
            Block("Catwalk Rail N", g, new Vector3(8.8f, -0.95f, -4.85f), new Vector3(10.4f, 0.06f, 0.06f), orange);
            Block("Catwalk Rail S", g, new Vector3(8.8f, -0.95f, -6.15f), new Vector3(10.4f, 0.06f, 0.06f), orange);
            Prop(g, Props + "/PROP_Generator.prefab", 6.2f, -1.54f, -5.5f, FacePlusZ);
            Prop(g, Props + "/PROP_Coolant_Tank.prefab", 11.5f, -1.54f, -5.4f, FacePlusZ);
            var reactor = Prop(g, Props + "/PROP_Reactor_Core.prefab", 8.8f, -16f, -8.5f, 0f);
            reactor.transform.localScale = Vector3.one * 3f;

            CreatePointLight("Reactor Glow", new Vector3(8.8f, -12f, -8.5f), new Color(0.3f, 0.9f, 1f), 11f, 6f, g);   // range stops short of the corridor (lights ignore walls)
            CreatePointLight("Catwalk Lamp", new Vector3(8.8f, 0.2f, -5.5f), new Color(1f, 0.7f, 0.4f), 8f, 1.8f, g);
            CreatePointLight("Deep Amber", new Vector3(5f, -7f, -12f), new Color(1f, 0.55f, 0.2f), 10f, 2.5f, g);
            CreatePointLight("High Magenta", new Vector3(12f, 7f, -11f), new Color(1f, 0.25f, 0.85f), 12f, 2.2f, g);
        }

        private static GameObject Prop(Transform parent, string path, float x, float y, float z, float yaw)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning($"Test bay: missing prop {path}");
                var missing = new GameObject("MISSING " + path);
                missing.transform.SetParent(parent, false);
                return missing;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.SetLocalPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, yaw, 0f) * asset.transform.localRotation);
            return go;
        }

        private static float TopOf(GameObject go)
        {
            float top = 0f;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                top = Mathf.Max(top, r.bounds.max.y);
            }

            return top;
        }
    }
}
