using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VentsAndVending.Customers;
using VentsAndVending.Inventory;
using VentsAndVending.Tunnels;
using VentsAndVending.World;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// First tunnel slice (Docs/Design/TunnelNetwork.md §0, §11). The plaza is reachable from the start (01 → 02 → 03 → 05);
    /// the fan at 04 instead guards the route to richer customers:
    ///   02 → E1 corridor → 04 Fan Junction ─┬→ 16 Power Distribution (BRK_FAN_04)
    ///                                       └→ fan duct → [FAN 17] → 17 Fan Room → E2 → ramp shaft (06) → drop hatch
    ///   → 07 Plaza Service Hub ─┬→ staff door (one-way latch) → plaza   (the shortcut home)
    ///                           └→ 08 Customer Vent → Concourse Lounge (rich customers)
    ///
    /// Interior bounds (floor y = 0 unless noted):
    ///   E1   x[1.4, 8.5]    z[-9.8, -8.2]  ceiling 2.3
    ///   04   x[8.5, 13.5]   z[-11.5, -6]   ceiling 3.0
    ///   16   x[13.7, 17]    z[-11, -6.5]   ceiling 2.6
    ///   duct x[10, 12]      z[-6, 2]       ceiling 1.6 (crouch); fan in a 3.2 m housing at z 2.0–2.2
    ///   17   x[8.5, 13.5]   z[2.2, 7]      ceiling 3.6
    ///   E2   x[13.7, 17.7]  z[3.8, 5.4]    ceiling 2.3
    ///   06   x[17.7, 19.3]  z[3.8, 24.6]   ramp z 5.4 → 12.54 up to floor 2.6, ceiling 4.8; drop hole z[23, 24.2]
    ///   07   x[16.5, 23]    z[21, 31]      ceiling 2.4 (west wall = plaza east wall, staff door z[25.4, 26.6])
    ///   08   x 20,          z[31.2, 34.6]  crawl
    ///   Lounge x[16.5, 27]  z[34.8, 45]    open top
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        private const float StaffDoorZ0 = 25.4f, StaffDoorZ1 = 26.6f, StaffDoorHeight = 2.3f;
        private const float HighFloor = 2.6f;
        private const string TunnelDataFolder = "Assets/_Project/ScriptableObjects/Tunnels";

        private static readonly Vector3 FanCentre = new Vector3(11f, 1.75f, 2.3f);

        private static void BuildTunnelSlice(Transform level, Material floor, Material wall, Material vent, Material accent, Material panel, Material glow)
        {
            var root = Group("TunnelSlice", level);
            Material dark = ArtLibrary.Get("M_DarkMetal");
            Material orange = ArtLibrary.Get("M_WorkshopOrange");
            Material rust = ArtLibrary.Get("M_RustyMetal");
            Material power = GetOrCreateMaterial("Cable_Power", new Color(0.95f, 0.45f, 0.08f), 0.45f, new Color(0.35f, 0.12f, 0.0f));
            Material lampMat = GetOrCreateMaterial("Fan_WarningLamp", new Color(0.6f, 0.08f, 0.05f), 0.5f, new Color(2f, 0.2f, 0.1f));

            BuildJunctionSigns();
            BuildEastCorridor(Group("E1_Corridor", root), floor, wall, vent, dark, power);
            var lamps = new List<Renderer>();
            Light warning = BuildFanJunction(Group("R04_FanJunction", root), floor, wall, vent, accent, dark, orange, power, lampMat, lamps);
            BuildPowerDistribution(Group("R16_PowerDistribution", root), floor, wall, dark, accent, orange, power);
            BuildFanDuctAndRoom(Group("R17_Fan", root), floor, rust, vent, accent, dark, orange, power, lampMat, lamps, warning);
            BuildDropRoute(Group("R06_DropRoute", root), floor, wall, vent, accent, dark);
            BuildServiceHub(Group("R07_ServiceHub", root), floor, wall, vent, accent, dark);
            BuildCustomerVentAndLounge(Group("R08_Lounge", root), floor, vent, accent, glow);
        }

        // ------------------------------------------------------------------ 02 signage

        private static void BuildJunctionSigns()
        {
            // On the gallery's west wall, facing a player who has just crawled out of the nook
            var plate = GetOrCreateMaterial("Sign_Plate", new Color(0.12f, 0.13f, 0.12f), 0.3f);
            var g = new GameObject("JunctionSign").transform;
            NoCollider(Box("Plate", g, new Vector3(-0.6f, 1.25f, -9.75f), new Vector3(-0.57f, 2.05f, -8.15f), plate));
            CreateSign("Junction_Sign", "<color=#FFD24D>SECTOR 3 · SERVICE JUNCTION</color>\n<size=80%>↑  05 PLAZA ACCESS\n→  04 FAN JUNCTION · 16 POWER</size>",
                new Vector3(-0.565f, 1.65f, -8.95f), -90f, 0.85f, new Color(0.95f, 0.92f, 0.8f), 1.5f, 1.5f);
        }

        // ------------------------------------------------------------------ E1: 02 → 04

        private static void BuildEastCorridor(Transform g, Material floor, Material wall, Material vent, Material dark, Material power)
        {
            Box("Floor", g, new Vector3(1.4f, -0.5f, -10f), new Vector3(8.5f, 0f, -8f), floor);
            Box("Wall_S", g, new Vector3(1.4f, 0f, -10f), new Vector3(8.5f, WallTop, -9.8f), wall);
            Box("Wall_N", g, new Vector3(1.4f, 0f, -8.2f), new Vector3(8.5f, WallTop, -8f), wall);
            Box("Ceiling", g, new Vector3(1.4f, 2.3f, -10f), new Vector3(8.5f, WallTop, -8f), vent);
            PipeX(g, 1.4f, 8.5f, 2.08f, -9.65f, 0.08f, vent, dark);
            PipeX(g, 1.4f, 8.5f, 0.3f, -8.33f, 0.09f, vent, dark);
            SagCable(g, new Vector3(8.5f, 2.18f, -8.35f), new Vector3(1.4f, 2.18f, -8.35f), 0.03f, power);
            HangingBulb(g, new Vector3(5f, 2.3f, -9f), 0.25f, new Color(1f, 0.72f, 0.4f), 4.5f, 1.3f, false);
            CreateSign("E1_Arrow", "→ 04 FAN JUNCTION", new Vector3(2.2f, 1.9f, -9.79f), 180f, 0.6f, new Color(1f, 0.85f, 0.4f), 1.6f, 2f);
        }

        // ------------------------------------------------------------------ 04 Fan Junction

        private static Light BuildFanJunction(Transform g, Material floor, Material wall, Material vent, Material accent, Material dark,
            Material orange, Material power, Material lampMat, List<Renderer> lamps)
        {
            const float top = 3.4f;
            Box("Floor", g, new Vector3(8.3f, -0.5f, -11.7f), new Vector3(13.7f, 0f, -5.8f), floor);
            Box("Wall_S", g, new Vector3(8.3f, 0f, -11.7f), new Vector3(13.7f, top, -11.5f), wall);
            WallWithDoorZ("Wall_W", g, 8.3f, 8.5f, -11.7f, -5.8f, -9.8f, -8.2f, 2.3f, top, wall);
            WallWithDoorZ("Wall_E", g, 13.5f, 13.7f, -11.7f, -5.8f, -9.6f, -8.4f, 2.2f, top, wall);
            Box("Wall_N_A", g, new Vector3(8.3f, 0f, -6f), new Vector3(10f, top, -5.8f), wall);
            Box("Wall_N_B", g, new Vector3(12f, 0f, -6f), new Vector3(13.7f, top, -5.8f), wall);
            Box("Wall_N_Lintel", g, new Vector3(10f, 1.6f, -6f), new Vector3(12f, top, -5.8f), wall);
            Box("Ceiling", g, new Vector3(8.3f, 3f, -11.7f), new Vector3(13.7f, top, -5.8f), vent);

            // Hazard frame round the fan duct mouth, warning lamps and the label that points at the breaker
            NoCollider(Box("DuctFrame_L", g, new Vector3(9.85f, 0f, -6.06f), new Vector3(10f, 1.75f, -5.98f), accent));
            NoCollider(Box("DuctFrame_R", g, new Vector3(12f, 0f, -6.06f), new Vector3(12.15f, 1.75f, -5.98f), accent));
            NoCollider(Box("DuctFrame_T", g, new Vector3(9.85f, 1.6f, -6.06f), new Vector3(12.15f, 1.75f, -5.98f), accent));
            foreach (float x in new[] { 9.6f, 12.4f })
            {
                var lamp = Part("WarningLamp", g, new Vector3(x, 1.95f, -6.08f), Vector3.one * 0.14f, lampMat, PrimitiveType.Sphere);
                lamp.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                lamps.Add(lamp.GetComponent<Renderer>());
                Part("LampCage", g, new Vector3(x, 1.95f, -6.04f), new Vector3(0.18f, 0.18f, 0.06f), dark);
            }
            var warning = new GameObject("Fan Warning Light").AddComponent<Light>();
            warning.transform.SetParent(g, false);
            warning.transform.position = new Vector3(11f, 1.9f, -6.6f);
            warning.type = LightType.Point;
            warning.range = 5f;
            warning.intensity = 2.2f;
            warning.color = new Color(1f, 0.12f, 0.08f);
            CreateSign("Fan_Label", "<color=#FF6040>FAN 17 · INDUSTRIAL EXTRACTION</color>\n<size=75%>KEEP CLEAR WHILE RUNNING\nISOLATE AT BRK_FAN_04 (16 · POWER) →</size>",
                new Vector3(11f, 2.45f, -6.02f), 0f, 1.05f, new Color(1f, 0.9f, 0.75f), 3.6f);
            CreateSign("R04_Sign", "04 · FAN JUNCTION", new Vector3(10.2f, 2.5f, -11.48f), 180f, 0.8f, new Color(1f, 0.85f, 0.4f), 2.4f, -2f);
            CreateSign("R16_Arrow", "16 · POWER →", new Vector3(13.48f, 2.45f, -9f), 90f, 0.65f, new Color(1f, 0.85f, 0.4f), 1.4f);

            // Clutter: an orange cable drum and a crate, pipes overhead
            Part("CableDrum", g, new Vector3(9.2f, 0.35f, -10.8f), new Vector3(0.7f, 0.3f, 0.7f), orange, PrimitiveType.Cylinder).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            Block("Crate_04", g, new Vector3(12.8f, 0.3f, -11f), new Vector3(0.6f, 0.6f, 0.6f), ArtLibrary.Get("M_GreenPanel")).transform.rotation = Quaternion.Euler(0f, 17f, 0f);
            PipeX(g, 8.5f, 13.5f, 2.7f, -11.3f, 0.12f, vent, dark);
            PipeZ(g, -11.5f, -6f, 2.75f, 8.75f, 0.1f, vent, dark);
            HangingBulb(g, new Vector3(10.6f, 3f, -9.6f), 0.5f, new Color(1f, 0.72f, 0.4f), 6f, 1.8f, true);

            // The power cable, traced from 16's door across the ceiling to the fan duct (§9: "power routing legible")
            SagCable(g, new Vector3(13.5f, 2.6f, -9.2f), new Vector3(11.6f, 2.85f, -9.2f), 0.035f, power, 1.2f, 0.08f);
            SagCable(g, new Vector3(11.6f, 2.85f, -9.2f), new Vector3(13.1f, 2.85f, -6.1f), 0.035f, power, 1.2f, 0.08f);
            // Drops beside the warning label, then ducks into the fan duct
            CylinderBetween("PowerDrop", g, new Vector3(13.1f, 2.85f, -6.1f), new Vector3(13.1f, 1.7f, -6.1f), 0.035f, power);
            CylinderBetween("PowerIntoDuct", g, new Vector3(13.1f, 1.7f, -6.1f), new Vector3(11.75f, 1.5f, -6.1f), 0.035f, power);
            return warning;
        }

        // ------------------------------------------------------------------ 16 Power Distribution

        private static void BuildPowerDistribution(Transform g, Material floor, Material wall, Material dark, Material accent, Material orange, Material power)
        {
            const float top = 3f;
            Box("Floor", g, new Vector3(13.7f, -0.5f, -11.2f), new Vector3(17.2f, 0f, -6.3f), floor);
            Box("Wall_S", g, new Vector3(13.7f, 0f, -11.2f), new Vector3(17.2f, top, -11f), wall);
            Box("Wall_N", g, new Vector3(13.7f, 0f, -6.5f), new Vector3(17.2f, top, -6.3f), wall);
            Box("Wall_E", g, new Vector3(17f, 0f, -11f), new Vector3(17.2f, top, -6.5f), wall);
            Box("Ceiling", g, new Vector3(13.7f, 2.6f, -11.2f), new Vector3(17.2f, top, -6.3f), ArtLibrary.Get("M_DuctMetal"));

            // Breaker wall on the east side, facing west
            NoCollider(Box("BreakerBoard", g, new Vector3(16.92f, 0.8f, -10.2f), new Vector3(17f, 2.1f, -7.3f), dark));
            NoCollider(Box("BoardStripe", g, new Vector3(16.9f, 2.06f, -10.2f), new Vector3(16.92f, 2.12f, -7.3f), accent));
            Material breakerBody = GetOrCreateMaterial("Breaker_Body", new Color(0.55f, 0.56f, 0.58f), 0.4f);
            Material lampOff = GetOrCreateMaterial("Breaker_Lamp", new Color(0.3f, 0.3f, 0.3f), 0.5f, new Color(0.2f, 1f, 0.35f));
            var breakers = new (string id, string controls, string label, bool jammed, string joke)[]
            {
                (TunnelIds.FanBreaker, "FAN 17", "<color=#FF8A30>BRK_FAN_04</color>\nFAN 17 · EXTRACTION", false, null),
                ("BRK_LGT_09", "PLAZA FOUNTAIN", "BRK_LGT_09\nPLAZA FOUNTAIN", true, "Jammed. (There is no plaza fountain.)"),
                ("BRK_SNK_12", "SNACK MACHINE", "BRK_SNK_12\nSNACK MACHINE", true, "Jammed. Somewhere, a snack machine sighs."),
                ("BRK_XXX_00", "DO NOT", "BRK_XXX_00\n<color=#FF5050>DO NOT</color>", true, "It says DO NOT. You respect that."),
            };
            for (int i = 0; i < breakers.Length; i++)
            {
                var b = breakers[i];
                float z = -9.75f + i * 0.72f;
                BuildBreaker(g, new Vector3(16.9f, 1.3f, z), b.id, b.controls, b.jammed, b.joke, breakerBody, dark, lampOff, i == 0 ? orange : dark);
                CreateSign($"Breaker_Label_{i}", b.label, new Vector3(16.88f, 1.78f, z), 90f, 0.55f, new Color(0.95f, 0.95f, 0.9f), 0.74f, i == 3 ? 4f : 0f);
            }

            // The orange cable leaves the fan breaker, climbs to the ceiling and heads for 04
            // (kept to the side of the labels so it never hides them)
            CylinderBetween("PowerTail", g, new Vector3(16.85f, 1.3f, -9.75f), new Vector3(16.85f, 1.3f, -10.12f), 0.035f, power);
            CylinderBetween("PowerRiser", g, new Vector3(16.85f, 1.3f, -10.12f), new Vector3(16.85f, 2.5f, -10.12f), 0.035f, power);
            SagCable(g, new Vector3(16.85f, 2.5f, -10.12f), new Vector3(13.7f, 2.5f, -9.2f), 0.035f, power, 1.1f, 0.07f);
            CreateSign("R16_Sign", "16 · POWER DISTRIBUTION", new Vector3(15.3f, 2.3f, -10.98f), 180f, 0.7f, new Color(1f, 0.85f, 0.4f), 2.4f);
            HangingBulb(g, new Vector3(15.2f, 2.6f, -8.6f), 0.3f, new Color(1f, 0.85f, 0.65f), 4.5f, 1.6f, true);
        }

        private static void BuildBreaker(Transform parent, Vector3 face, string id, string controls, bool jammed, string joke,
            Material body, Material dark, Material lamp, Material handle)
        {
            var root = new GameObject($"Breaker {id}");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(face, Quaternion.Euler(0f, -90f, 0f)); // local +Z faces into the room (-X)
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0f, 0.05f);
            col.size = new Vector3(0.3f, 0.4f, 0.16f);

            Part("Housing", root.transform, new Vector3(0f, 0f, 0.04f), new Vector3(0.24f, 0.34f, 0.1f), body);
            var lever = Group("Lever", root.transform);
            lever.localPosition = new Vector3(0f, 0f, 0.09f);
            Part("Stick", lever, new Vector3(0f, 0f, 0.08f), new Vector3(0.035f, 0.08f, 0.035f), dark, PrimitiveType.Cylinder).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part("Knob", lever, new Vector3(0f, 0f, 0.17f), new Vector3(0.11f, 0.06f, 0.06f), handle);
            var lampGo = Part("Lamp", root.transform, new Vector3(0f, -0.13f, 0.1f), Vector3.one * 0.04f, lamp, PrimitiveType.Sphere);

            var breaker = root.AddComponent<BreakerSwitch>();
            var so = new SerializedObject(breaker);
            so.FindProperty("_breakerId").stringValue = id;
            so.FindProperty("_controls").stringValue = controls;
            so.FindProperty("_jammed").boolValue = jammed;
            if (joke != null)
            {
                so.FindProperty("_jammedMessage").stringValue = joke;
            }
            so.FindProperty("_lever").objectReferenceValue = lever;
            so.FindProperty("_lamp").objectReferenceValue = lampGo.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ Fan duct, FAN 17, 17 Fan Room

        private static void BuildFanDuctAndRoom(Transform g, Material floor, Material wallMat, Material vent, Material accent, Material dark,
            Material orange, Material power, Material lampMat, List<Renderer> lamps, Light warning)
        {
            // Crouch-height duct from 04 to the fan
            Box("Duct_Floor", g, new Vector3(9.8f, -0.5f, -5.8f), new Vector3(12.2f, 0f, 2.2f), floor);
            Box("Duct_Wall_W", g, new Vector3(9.8f, 0f, -5.8f), new Vector3(10f, 1.6f, 2f), vent);
            Box("Duct_Wall_E", g, new Vector3(12f, 0f, -5.8f), new Vector3(12.2f, 1.6f, 2f), vent);
            Box("Duct_Roof", g, new Vector3(9.8f, 1.6f, -5.8f), new Vector3(12.2f, 1.8f, 2f), vent);
            for (float z = -5.2f; z < 2f; z += 1.8f)
            {
                NoCollider(Box("DuctRib", g, new Vector3(9.98f, 1.56f, z), new Vector3(12.02f, 1.6f, z + 0.08f), dark));
            }
            SagCable(g, new Vector3(11.75f, 1.5f, -6f), new Vector3(11.75f, 1.5f, 1.8f), 0.03f, power, 1.3f, 0.04f);
            var ductGlow = new GameObject("Fan Duct Light").AddComponent<Light>();
            ductGlow.transform.SetParent(g, false);
            ductGlow.transform.position = new Vector3(11f, 1.35f, -2.5f);
            ductGlow.type = LightType.Point;
            ductGlow.range = 4.5f;
            ductGlow.intensity = 1.1f;
            ductGlow.color = new Color(1f, 0.55f, 0.3f);
            NoCollider(Box("DuctCageLamp", g, new Vector3(10.85f, 1.52f, -2.65f), new Vector3(11.15f, 1.6f, -2.35f), GetOrCreateMaterial("Bulb_Warm", new Color(1f, 0.85f, 0.6f), 0.6f, new Color(1f, 0.75f, 0.45f) * 3f)));

            // Plenum behind the fan housing so the square hole never shows the void
            Box("Plenum_Back_Top", g, new Vector3(9.2f, 1.8f, 1.0f), new Vector3(12.8f, 3.4f, 1.2f), dark);
            Box("Plenum_Back_W", g, new Vector3(9.2f, 0f, 1.0f), new Vector3(9.8f, 1.8f, 1.2f), dark);
            Box("Plenum_Back_E", g, new Vector3(12.2f, 0f, 1.0f), new Vector3(12.8f, 1.8f, 1.2f), dark);
            Box("Plenum_Side_W", g, new Vector3(9.2f, 0f, 1.0f), new Vector3(9.4f, 3.4f, 2f), dark);
            Box("Plenum_Side_E", g, new Vector3(12.6f, 0f, 1.0f), new Vector3(12.8f, 3.4f, 2f), dark);
            Box("Plenum_Top", g, new Vector3(9.2f, 3.2f, 1.0f), new Vector3(12.8f, 3.4f, 2f), dark);
            Box("Plenum_Floor", g, new Vector3(9.2f, -0.5f, 1.0f), new Vector3(12.8f, 0f, 2.2f), floor);

            // 17 Fan Room
            const float top = 4f;
            Box("R17_Floor", g, new Vector3(8.3f, -0.5f, 2.2f), new Vector3(13.7f, 0f, 7.2f), floor);
            Box("R17_FanWall_W", g, new Vector3(8.3f, 0f, 2f), new Vector3(9.4f, top, 2.2f), wallMat);
            Box("R17_FanWall_E", g, new Vector3(12.6f, 0f, 2f), new Vector3(13.7f, top, 2.2f), wallMat);
            Box("R17_FanWall_Top", g, new Vector3(9.4f, 3.2f, 2f), new Vector3(12.6f, top, 2.2f), wallMat);
            Box("R17_Wall_W", g, new Vector3(8.3f, 0f, 2.2f), new Vector3(8.5f, top, 7.2f), wallMat);
            Box("R17_Wall_N", g, new Vector3(8.3f, 0f, 7f), new Vector3(13.7f, top, 7.2f), wallMat);
            WallWithDoorZ("R17_Wall_E", g, 13.5f, 13.7f, 2.2f, 7.2f, 3.8f, 5.4f, 2.3f, top, wallMat);
            Box("R17_Ceiling", g, new Vector3(8.3f, 3.6f, 2f), new Vector3(13.7f, top, 7.2f), vent);

            // Square housing frame
            foreach (var (n, min, max) in new[]
            {
                ("Housing_L", new Vector3(9.4f, 0f, 2.2f), new Vector3(9.6f, 3.2f, 2.45f)),
                ("Housing_R", new Vector3(12.4f, 0f, 2.2f), new Vector3(12.6f, 3.2f, 2.45f)),
                ("Housing_T", new Vector3(9.4f, 3.0f, 2.2f), new Vector3(12.6f, 3.2f, 2.45f)),
            })
            {
                NoCollider(Box(n, g, min, max, accent));
            }

            // FAN 17: three blades, one pointing up at rest, leaving a crouch-height gap under the hub when stopped
            var fanRoot = new GameObject("FAN_17");
            fanRoot.transform.SetParent(g, false);
            fanRoot.transform.position = FanCentre;
            var rotor = Group("Rotor", fanRoot.transform);
            Material blade = GetOrCreateMaterial("Fan_Blade", new Color(0.62f, 0.6f, 0.55f), 0.45f);
            Worn(blade, 0.55f, 0.4f, 0.6f);
            for (int i = 0; i < 3; i++)
            {
                float a = 90f + i * 120f;
                var arm = Group($"BladeArm_{i}", rotor);
                arm.localRotation = Quaternion.Euler(0f, 0f, a - 90f);
                var bladeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bladeGo.name = $"Blade_{i}";
                bladeGo.transform.SetParent(arm, false);
                bladeGo.transform.localPosition = new Vector3(0f, 0.78f, 0f);
                bladeGo.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
                bladeGo.transform.localScale = new Vector3(0.3f, 1.15f, 0.05f);
                bladeGo.GetComponent<MeshRenderer>().sharedMaterial = blade;
            }
            Part("Hub", rotor, Vector3.zero, new Vector3(0.42f, 0.1f, 0.42f), dark, PrimitiveType.Cylinder).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part("HubCap", rotor, new Vector3(0f, 0f, 0.1f), Vector3.one * 0.2f, orange, PrimitiveType.Sphere);
            // Two struts up to the housing corners (none below: that's where you crawl)
            CylinderBetween("Strut_L", fanRoot.transform, FanCentre + new Vector3(0f, 0f, -0.08f), new Vector3(9.6f, 3.0f, 2.22f), 0.04f, dark);
            CylinderBetween("Strut_R", fanRoot.transform, FanCentre + new Vector3(0f, 0f, -0.08f), new Vector3(12.4f, 3.0f, 2.22f), 0.04f, dark);

            var blocker = new GameObject("RunningBlocker").AddComponent<BoxCollider>();
            blocker.transform.SetParent(fanRoot.transform, false);
            blocker.transform.position = new Vector3(11f, 1.6f, 2.25f);
            blocker.size = new Vector3(3.2f, 3.2f, 0.5f);

            // Warning lamp on the room side too
            var lamp17 = Part("WarningLamp", g, new Vector3(12.9f, 2.4f, 2.24f), Vector3.one * 0.14f, lampMat, PrimitiveType.Sphere);
            lamps.Add(lamp17.GetComponent<Renderer>());

            ParticleSystem suction = BuildSuction(g);

            var fan = fanRoot.AddComponent<FanHazard>();
            var so = new SerializedObject(fan);
            so.FindProperty("_rotor").objectReferenceValue = rotor;
            so.FindProperty("_blocker").objectReferenceValue = blocker;
            SetList(so.FindProperty("_warningLamps"), lamps.ToArray());
            so.FindProperty("_warningLight").objectReferenceValue = warning;
            so.FindProperty("_suction").objectReferenceValue = suction;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Room dressing: maintenance lever (for later), sign, light
            Part("LeverBox", g, new Vector3(8.6f, 1.2f, 5.5f), new Vector3(0.14f, 0.4f, 0.3f), dark);
            Part("LeverHandle", g, new Vector3(8.72f, 1.32f, 5.5f), new Vector3(0.04f, 0.3f, 0.04f), orange, PrimitiveType.Cylinder).transform.rotation = Quaternion.Euler(0f, 0f, -30f);
            CreateSign("R17_Sign", "17 · INDUSTRIAL FAN ROOM", new Vector3(11f, 3.35f, 6.98f), 0f, 0.8f, new Color(1f, 0.85f, 0.4f), 3f);
            CreateSign("R17_Lever_Label", "MAINT. OVERRIDE\n<size=70%>(PARTS MISSING)</size>", new Vector3(8.52f, 1.75f, 5.5f), -90f, 0.3f, Color.white, 0.8f, -3f);
            PipeX(g, 8.5f, 13.5f, 3.3f, 6.75f, 0.14f, vent, dark);
            var amber = new GameObject("Fan Room Light").AddComponent<Light>();
            amber.transform.SetParent(g, false);
            amber.transform.position = new Vector3(11f, 3.2f, 5f);
            amber.type = LightType.Point;
            amber.range = 7f;
            amber.intensity = 2f;
            amber.color = new Color(1f, 0.55f, 0.2f);
        }

        /// <summary>Dust sucked along the duct toward the fan while it runs.</summary>
        private static ParticleSystem BuildSuction(Transform parent)
        {
            var go = new GameObject("FanSuction");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(11f, 0.8f, -2f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 1.6f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
            main.startColor = new Color(0.8f, 0.78f, 0.7f, 0.5f);
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.8f, 1.4f, 7.5f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(0f);
            vel.z = new ParticleSystem.MinMaxCurve(4.5f);
            var emission = ps.emission;
            emission.rateOverTime = 25f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.06f;
            renderer.lengthScale = 2f;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
            return ps;
        }

        // ------------------------------------------------------------------ E2, ramp shaft and 06 Drop Hatch

        private static void BuildDropRoute(Transform g, Material floor, Material wall, Material vent, Material accent, Material dark)
        {
            // E2: from 17 east to the shaft
            Box("E2_Floor", g, new Vector3(13.7f, -0.5f, 3.6f), new Vector3(17.7f, 0f, 5.6f), floor);
            Box("E2_Wall_S", g, new Vector3(13.7f, 0f, 3.6f), new Vector3(17.5f, WallTop, 3.8f), wall);
            Box("E2_Wall_N", g, new Vector3(13.7f, 0f, 5.4f), new Vector3(17.5f, WallTop, 5.6f), wall);
            Box("E2_Ceiling", g, new Vector3(13.7f, 2.3f, 3.6f), new Vector3(17.5f, WallTop, 5.6f), vent);
            HangingBulb(g, new Vector3(15.6f, 2.3f, 4.6f), 0.25f, new Color(1f, 0.72f, 0.4f), 4f, 1.2f, false);

            // Shaft walls. North of z 20.8 they start at the high floor so the hub below stays open.
            const float x0 = 17.5f, x1 = 19.5f, top = 5.2f, split = 20.8f;
            Box("Shaft_Wall_S", g, new Vector3(x0, 0f, 3.6f), new Vector3(x1, top, 3.8f), wall);
            Box("Shaft_Wall_W_A", g, new Vector3(x0, 0f, 3.6f), new Vector3(17.7f, top, 3.8f), wall);
            Box("Shaft_Wall_W_Lintel", g, new Vector3(x0, 2.3f, 3.8f), new Vector3(17.7f, top, 5.4f), wall);
            Box("Shaft_Wall_W_B", g, new Vector3(x0, 0f, 5.4f), new Vector3(17.7f, top, split), wall);
            Box("Shaft_Wall_W_High", g, new Vector3(x0, HighFloor, split), new Vector3(17.7f, top, 24.8f), wall);
            Box("Shaft_Wall_E", g, new Vector3(19.3f, 0f, 3.6f), new Vector3(x1, top, split), wall);
            Box("Shaft_Wall_E_High", g, new Vector3(19.3f, HighFloor, split), new Vector3(x1, top, 24.8f), wall);
            Box("Shaft_EndCap", g, new Vector3(x0, HighFloor, 24.6f), new Vector3(x1, top, 24.8f), wall);
            Box("Shaft_Ceiling", g, new Vector3(x0, 4.8f, 3.6f), new Vector3(x1, top, 24.8f), vent);
            Box("Shaft_FloorLow", g, new Vector3(x0, -0.5f, 3.6f), new Vector3(x1, 0f, 5.4f), floor);

            // 20° ramp from z 5.4 (y 0) to z 12.54 (y 2.6)
            float run = HighFloor / Mathf.Tan(20f * Mathf.Deg2Rad);
            float len = HighFloor / Mathf.Sin(20f * Mathf.Deg2Rad);
            var normal = Quaternion.Euler(-20f, 0f, 0f) * Vector3.up;
            Vector3 topMid = new Vector3(18.5f, HighFloor * 0.5f, 5.4f + run * 0.5f);
            var ramp = Block("Ramp", g, topMid - normal * 0.1f, new Vector3(1.6f, 0.2f, len + 0.1f), floor);
            ramp.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);
            float zHigh = 5.4f + run;
            for (int i = 0; i < 6; i++)
            {
                float t = (i + 0.5f) / 6f;
                var cleat = NoCollider(Block("RampCleat", g, new Vector3(18.5f, HighFloor * t + 0.012f, 5.4f + run * t), new Vector3(1.5f, 0.025f, 0.06f), accent));
                cleat.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);
            }

            // High floor with the drop hole at z[23, 24.2], x[17.9, 19.1]
            Box("HighFloor_A", g, new Vector3(x0, HighFloor - 0.2f, zHigh), new Vector3(x1, HighFloor, 23f), floor);
            Box("HighFloor_B", g, new Vector3(x0, HighFloor - 0.2f, 24.2f), new Vector3(x1, HighFloor, 24.8f), floor);
            Box("HighFloor_HoleW", g, new Vector3(x0, HighFloor - 0.2f, 23f), new Vector3(17.9f, HighFloor, 24.2f), floor);
            Box("HighFloor_HoleE", g, new Vector3(19.1f, HighFloor - 0.2f, 23f), new Vector3(x1, HighFloor, 24.2f), floor);
            foreach (var (n, min, max) in new[]
            {
                ("HoleStripe_S", new Vector3(17.85f, HighFloor, 22.9f), new Vector3(19.15f, HighFloor + 0.01f, 23f)),
                ("HoleStripe_W", new Vector3(17.8f, HighFloor, 23f), new Vector3(17.9f, HighFloor + 0.01f, 24.2f)),
                ("HoleStripe_E", new Vector3(19.1f, HighFloor, 23f), new Vector3(19.2f, HighFloor + 0.01f, 24.2f)),
            })
            {
                NoCollider(Box(n, g, min, max, accent));
            }

            // 06 trapdoor: hinged on its south edge, swings down into the hub
            var hatch = new GameObject("Drop Hatch");
            hatch.transform.SetParent(g, false);
            hatch.transform.position = new Vector3(18.5f, HighFloor, 23f);
            var pivot = Group("Pivot", hatch.transform);
            var panel = Block("Panel", pivot, Vector3.zero, new Vector3(1.2f, 0.05f, 1.2f), vent);
            panel.isStatic = false;
            panel.transform.localPosition = new Vector3(0f, -0.025f, 0.6f);
            var grille = Part("Grille", pivot, new Vector3(0f, 0.005f, 0.6f), new Vector3(0.7f, 0.02f, 0.4f), dark);
            grille.isStatic = false;
            var vh = hatch.AddComponent<VentHatch>();
            var so = new SerializedObject(vh);
            so.FindProperty("_pivot").objectReferenceValue = pivot;
            so.FindProperty("_openRotation").vector3Value = new Vector3(95f, 0f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();

            CreateSign("R06_Sign", "<color=#FFD24D>06 · DROP HATCH</color>\n<size=70%>↓ PLAZA SERVICE · ONE WAY</size>", new Vector3(18.5f, 3.9f, 24.58f), 0f, 0.95f, new Color(0.95f, 0.92f, 0.8f), 1.6f);
            HangingBulb(g, new Vector3(18.5f, 4.8f, 9f), 0.6f, new Color(1f, 0.72f, 0.4f), 6f, 1.6f, false);
            HangingBulb(g, new Vector3(18.5f, 4.8f, 21.5f), 0.5f, new Color(1f, 0.72f, 0.4f), 5f, 1.5f, false);
            PipeZ(g, 3.8f, 24.6f, 4.45f, 19.12f, 0.09f, vent, dark);
        }

        // ------------------------------------------------------------------ 07 Plaza Service Hub

        private static void BuildServiceHub(Transform g, Material floor, Material wall, Material vent, Material accent, Material dark)
        {
            Material panel = ArtLibrary.Get("M_PlazaPanel");
            Material silver = ArtLibrary.Get("M_BrushedSilver");
            Material cardboard = ArtLibrary.Get("M_Cardboard");
            Box("Floor", g, new Vector3(16.5f, -0.5f, 20.8f), new Vector3(23.2f, 0f, 31.2f), ArtLibrary.Get("M_FloorPlate"));
            Box("Wall_S", g, new Vector3(16.5f, 0f, 20.8f), new Vector3(23.2f, HighFloor, 21f), panel);
            Box("Wall_E", g, new Vector3(23f, 0f, 21f), new Vector3(23.2f, HighFloor, 31.2f), panel);
            WallWithHatchX("Wall_N", g, 31f, 31.2f, 16.5f, 23.2f, 20f, HighFloor, panel);
            // Ceiling, leaving the shaft's high floor (with the drop hole) to close the gap above x[17.5, 19.5]
            Box("Ceiling_W", g, new Vector3(16.5f, 2.4f, 20.8f), new Vector3(17.5f, HighFloor, 31.2f), vent);
            Box("Ceiling_E", g, new Vector3(19.5f, 2.4f, 20.8f), new Vector3(23.2f, HighFloor, 31.2f), vent);
            Box("Ceiling_N", g, new Vector3(17.5f, 2.4f, 24.8f), new Vector3(19.5f, HighFloor, 31.2f), vent);

            // Staff door into the plaza: only unlatches from this side, then stays open (the shortcut home)
            var door = new GameObject("Staff Door");
            door.transform.SetParent(g, false);
            door.transform.position = new Vector3(PlazaHalfWidth + 0.25f, 0f, (StaffDoorZ0 + StaffDoorZ1) * 0.5f);
            var hinge = Group("Hinge", door.transform);
            hinge.position = new Vector3(PlazaHalfWidth + 0.25f, 0f, StaffDoorZ0);
            var leaf = Block("Leaf", hinge, Vector3.zero, new Vector3(0.06f, StaffDoorHeight - 0.02f, StaffDoorZ1 - StaffDoorZ0 - 0.02f), silver);
            leaf.isStatic = false;
            leaf.transform.localPosition = new Vector3(0f, StaffDoorHeight * 0.5f, (StaffDoorZ1 - StaffDoorZ0) * 0.5f);
            Part("Bar", hinge, new Vector3(0.06f, 1.05f, 0.8f), new Vector3(0.04f, 0.04f, 0.6f), dark).isStatic = false;
            var latch = door.AddComponent<LatchDoor>();
            var lso = new SerializedObject(latch);
            lso.FindProperty("_pivot").objectReferenceValue = hinge;
            lso.ApplyModifiedPropertiesWithoutUndo();
            CreateSign("StaffDoor_Plaza", "<color=#FF6060>STAFF ONLY</color>", new Vector3(PlazaHalfWidth - 0.03f, 2.55f, 26f), 90f, 0.8f, Color.white, 1.4f);
            CreateSign("StaffDoor_Hub", "PLAZA →\n<size=65%>(latch your side)</size>", new Vector3(16.53f, 2.05f, 27.2f), -90f, 0.65f, new Color(1f, 0.85f, 0.4f), 1f);

            // Contents: shelving, stock boxes, a cart. The landing under the hole stays clear.
            for (int s = 0; s < 3; s++)
            {
                float y = 0.3f + s * 0.6f;
                NoCollider(Box("Shelf", g, new Vector3(21.8f, y, 22f), new Vector3(22.9f, y + 0.04f, 25.6f), silver));
            }
            Box("ShelfFrame_A", g, new Vector3(21.8f, 0f, 22f), new Vector3(22.9f, 1.6f, 22.06f), silver);
            Box("ShelfFrame_B", g, new Vector3(21.8f, 0f, 25.54f), new Vector3(22.9f, 1.6f, 25.6f), silver);
            var rng = new System.Random(7);
            for (int i = 0; i < 7; i++)
            {
                int shelf = i % 3;
                float z = 22.4f + (i * 0.47f) % 3f;
                var box = Block("LooseBox", g, new Vector3(22.35f, 0.34f + shelf * 0.6f + 0.13f, z), new Vector3(0.45f, 0.26f, 0.36f), cardboard);
                box.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 20f - 10f, 0f);
                NoCollider(box);
            }
            Block("Crate_Hub", g, new Vector3(21.9f, 0.35f, 29.6f), new Vector3(0.7f, 0.7f, 0.7f), ArtLibrary.Get("M_GreenPanel")).transform.rotation = Quaternion.Euler(0f, -9f, 0f);
            Block("Cart", g, new Vector3(17.4f, 0.45f, 29.8f), new Vector3(0.9f, 0.1f, 0.55f), silver);
            CreateSign("R07_Sign", "<color=#9FE8FF>07 · PLAZA SERVICE HUB</color>\n<size=70%>STAFF ONLY · NO VENT PEOPLE</size>", new Vector3(20f, 1.9f, 21.03f), 180f, 0.95f, Color.white, 3.2f);
            CreateSign("R08_Arrow", "08 · CUSTOMER VENT ↑", new Vector3(20f, 1.3f, 30.97f), 0f, 0.75f, new Color(1f, 0.85f, 0.4f), 1.6f, 2f);

            var tube = new GameObject("Hub Light").AddComponent<Light>();
            tube.transform.SetParent(g, false);
            tube.transform.position = new Vector3(20.2f, 2.2f, 27f);
            tube.type = LightType.Point;
            tube.range = 8f;
            tube.intensity = 2.4f;
            tube.color = new Color(0.85f, 0.95f, 1f);
            NoCollider(Block("TubeLamp", g, new Vector3(20.2f, 2.36f, 27f), new Vector3(0.12f, 0.05f, 1.6f),
                GetOrCreateMaterial("Lamp_Tube", new Color(0.9f, 0.95f, 1f), 0.5f, new Color(1.6f, 1.8f, 2f))));
        }

        // ------------------------------------------------------------------ 08 Customer Vent + Concourse Lounge (richer customers)

        private static void BuildCustomerVentAndLounge(Transform g, Material floor, Material vent, Material accent, Material glow)
        {
            Material tile = ArtLibrary.Get("M_PlazaTile");
            Material panel = ArtLibrary.Get("M_PlazaPanel");
            Material silver = ArtLibrary.Get("M_BrushedSilver");
            Material gold = GetOrCreateMaterial("Lounge_GoldNeon", new Color(0.3f, 0.2f, 0.02f), 0.6f, new Color(2f, 1.3f, 0.3f));
            Material velvet = Worn(GetOrCreateMaterial("Lounge_Velvet", new Color(0.42f, 0.12f, 0.36f), 0.3f), 0.15f, 0.15f);

            // 08: short crawl from the hub into the lounge
            CrawlDuctZ(g, 20f, 31.2f, 34.6f, floor, vent);
            VentTrigger("VentZone_CustomerVent", g, new Vector3(20f, 0.425f, 32.9f), new Vector3(0.9f, 0.85f, 3.4f + 1.4f), "duct_customer");
            CreateHatch("Customer Vent Hatch", g, new Vector3(20f, 0.85f, 30.96f), new Vector3(100f, 0f, 0f), vent, accent);
            CreateHatch("Lounge Grate", g, new Vector3(20f, 0.85f, 34.64f), new Vector3(-100f, 0f, 0f), silver, accent);
            NoCollider(Block("LightStrip", g, new Vector3(20f, 0.84f, 32.9f), new Vector3(0.3f, 0.02f, 0.6f), glow));

            // Lounge shell (west wall is the plaza's east wall)
            const float h = 4.5f;
            Box("Lounge_Floor", g, new Vector3(16.5f, -0.5f, 34.6f), new Vector3(27.2f, 0f, 45.2f), tile);
            WallWithHatchX("Lounge_Wall_S", g, 34.6f, 34.8f, 16.5f, 27.2f, 20f, h, panel);
            Box("Lounge_Wall_N", g, new Vector3(16.5f, 0f, 45f), new Vector3(27.2f, h, 45.2f), panel);
            WallWithDoorZ("Lounge_Wall_E", g, 27f, 27.2f, 34.8f, 45f, 38.9f, 41.1f, 3f, h, panel);
            NoCollider(Box("Lounge_DoorDark", g, new Vector3(27.15f, 0f, 38.9f), new Vector3(27.2f, 3f, 41.1f),
                GetOrCreateMaterial("Plaza_DoorDark", new Color(0.02f, 0.02f, 0.03f), 0.2f, new Color(0.08f, 0.02f, 0.1f))));
            NoCollider(Box("Trim_N", g, new Vector3(16.5f, 3.6f, 44.94f), new Vector3(27f, 3.68f, 45f), gold));
            NoCollider(Box("Trim_S", g, new Vector3(16.5f, 3.6f, 34.8f), new Vector3(27f, 3.68f, 34.86f), gold));
            NoCollider(Box("Trim_Door", g, new Vector3(26.94f, 3.02f, 38.8f), new Vector3(27f, 3.1f, 41.2f), gold));
            CreateSign("Lounge_Sign", "<color=#FFD27A>CONCOURSE LOUNGE</color>\n<size=60%>PLATINUM RESIDENTS ONLY</size>", new Vector3(26.95f, 3.6f, 40f), 90f, 1.7f, Color.white, 5f);

            // Plush furniture
            Block("Bench_W", g, new Vector3(18.2f, 0.25f, 41f), new Vector3(0.7f, 0.5f, 2.4f), velvet);
            Block("Bench_E", g, new Vector3(24.6f, 0.25f, 43.9f), new Vector3(2.4f, 0.5f, 0.7f), velvet);
            Block("Pedestal", g, new Vector3(21.5f, 0.3f, 43.6f), new Vector3(0.9f, 0.6f, 0.9f), silver);
            Part("PlantPot", g, new Vector3(26.3f, 0.3f, 35.6f), new Vector3(0.5f, 0.3f, 0.5f), silver, PrimitiveType.Cylinder);
            Part("Plant", g, new Vector3(26.3f, 0.95f, 35.6f), new Vector3(0.7f, 0.9f, 0.7f),
                GetOrCreateMaterial("Lounge_Plant", new Color(0.25f, 0.75f, 0.55f), 0.4f, new Color(0.05f, 0.3f, 0.2f)), PrimitiveType.Sphere);

            var warm = new[] { (new Vector3(19.5f, 3.6f, 38f), new Color(1f, 0.75f, 0.4f)), (new Vector3(24f, 3.6f, 42f), new Color(0.75f, 0.45f, 1f)) };
            foreach (var (pos, col) in warm)
            {
                var l = new GameObject("Lounge Light").AddComponent<Light>();
                l.transform.SetParent(g, false);
                l.transform.position = pos;
                l.type = LightType.Point;
                l.range = 10f;
                l.intensity = 3.5f;
                l.color = col;
            }

            // Rich customers: own door, own spots, own spawner
            var spawn = new GameObject("Lounge Customer Spawn").transform;
            spawn.SetParent(g, false);
            spawn.SetPositionAndRotation(new Vector3(26.2f, 0f, 40f), Quaternion.Euler(0f, -90f, 0f));
            var spotMat = GetOrCreateMaterial("Lounge_TradeSpot", new Color(0.3f, 0.22f, 0.05f), 0.4f, new Color(1f, 0.75f, 0.2f) * 0.6f);
            var spots = new List<Transform>();
            foreach (Vector3 p in new[] { new Vector3(20f, 0f, 37.6f), new Vector3(22.8f, 0f, 41.2f) })
            {
                var spot = new GameObject($"Lounge Spot {spots.Count + 1}").transform;
                spot.SetParent(g, false);
                spot.position = p;
                var decal = NoCollider(Block("Marker", spot, p + new Vector3(0f, 0.005f, 0f), new Vector3(1.1f, 0.01f, 1.1f), spotMat));
                decal.isStatic = false;
                spots.Add(spot);
            }

            var spawnerGo = new GameObject("Lounge Customer Spawner");
            spawnerGo.transform.SetParent(g, false);
            var spawner = spawnerGo.AddComponent<CustomerSpawner>();
            var so = new SerializedObject(spawner);
            SetList(so.FindProperty("_definitions"), CreateRichCustomers());
            so.FindProperty("_spawnPoint").objectReferenceValue = spawn;
            so.FindProperty("_exitPoint").objectReferenceValue = spawn;
            SetList(so.FindProperty("_meetingPoints"), spots.ToArray());
            so.FindProperty("_firstDelay").floatValue = 3f;
            so.FindProperty("_intervalRange").vector2Value = new Vector2(10f, 18f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The richer tier (§0.1): same species, bigger budgets, bigger orders, pickier about quality.</summary>
        private static Object[] CreateRichCustomers()
        {
            var execLines = new CustomerDefinition.DialogueLines
            {
                Greeting = new[] { "You. Vent person. I require {product}. Discreetly.", "My assistant said you have {product}. My assistant is usually wrong.", "{name}, Regional Director of Paperwork. {product}. Now." },
                Accept = new[] { "Acceptable. Invoice my shell company.", "Adequate. Don't tell the Federation.", "Fine. This meeting never happened." },
                TooExpensive = new[] { "{price}? I've audited people for less. {counter}.", "Absurd. {counter}, and I'll forget your face." },
                QualityRejected = new[] { "This is beneath me. Literally — I'm standing on a better carpet.", "I said premium. Do you know what premium means?" },
                WalkAway = new[] { "I have a three o'clock. Goodbye.", "Wasting my time is a billable offence." },
                Impatient = new[] { "My time is worth more than your entire vent.", "I'm leaving. Write that down." }
            };
            var socialiteLines = new CustomerDefinition.DialogueLines
            {
                Greeting = new[] { "Darling! Everyone's talking about your {product}!", "Mwah! I need {product} for my gala. Lots of it!", "I'm {name}, you've heard of me. {product}, please!" },
                Accept = new[] { "Fabulous! I'll tell absolutely everyone. Discreetly!", "Divine! My wobble is SHIMMERING!", "Ciao, darling!" },
                TooExpensive = new[] { "Darling, {price}? Let's say {counter}, for love.", "Ooh, steep! {counter}, and I'll mention you at brunch." },
                QualityRejected = new[] { "Oh no, darling, that one's tragic.", "My guests would faint. Something sparklier?" },
                WalkAway = new[] { "I simply can't. Kisses!", "Tragic. Toodles!" },
                Impatient = new[] { "Darling, I'm late for being fashionably late!", "Oh! A champagne fountain! Bye!" }
            };

            var exec = GetOrCreateCustomer("Customer_GromflomiteExec", "gromflomite_exec", "Gromflomite Executive",
                new[] { "Director Krell", "Undersecretary Vorp", "Chairman Thrax", "Auditor Zeeb" },
                _gromflomiteDefinition.Prefab, new Vector2Int(2, 3), new Vector2(0.6f, 1.1f), ItemQuality.Premium, false, 1, 40f, execLines,
                new[] { new Color(0.16f, 0.15f, 0.2f), new Color(0.75f, 0.6f, 0.2f), new Color(0.35f, 0.08f, 0.12f) }, 0.08f);
            var socialite = GetOrCreateCustomer("Customer_BlorbSocialite", "blorb_socialite", "Blorb Socialite",
                new[] { "Glimmerella", "Lady Wobblesworth", "Opalisque", "Jellissima" },
                _blorbDefinition.Prefab, new Vector2Int(2, 3), new Vector2(0.5f, 0.9f), ItemQuality.Standard, false, 2, 55f, socialiteLines,
                new[] { new Color(0.95f, 0.85f, 0.95f), new Color(1f, 0.8f, 0.35f), new Color(0.7f, 0.9f, 1f) }, 0.22f);
            return new Object[] { exec, socialite };
        }

        // ------------------------------------------------------------------ Tunnel system objects (data, manager, discovery volumes)

        private static void BuildTunnelSystem()
        {
            EnsureFolder(TunnelDataFolder);
            VentRoomDefinition.Connection C(string to, string gate = null, bool whenOff = false, bool oneWay = false) =>
                new VentRoomDefinition.Connection { DestinationRoomId = to, GateId = gate, GateOpensWhenOff = whenOff, OneWay = oneWay };

            var rooms = new[]
            {
                Room(TunnelIds.Nook, "Starting Nook", "old_maintenance", 3, C(TunnelIds.ServiceJunction)),
                Room(TunnelIds.ServiceJunction, "Service Junction", "old_maintenance", 3, C(TunnelIds.Nook), C(TunnelIds.LongCrawl), C(TunnelIds.FanJunction)),
                Room(TunnelIds.LongCrawl, "The Long Crawl", "old_maintenance", 3, C(TunnelIds.ServiceJunction), C(TunnelIds.PlazaAccess)),
                Room(TunnelIds.PlazaAccess, "Plaza Access", "plaza_network", 2, C(TunnelIds.LongCrawl), C(TunnelIds.StashDuct)),
                Room(TunnelIds.StashDuct, "Forgotten Stash", "old_maintenance", 3, C(TunnelIds.PlazaAccess)),
                Room(TunnelIds.FanJunction, "Fan Junction", "old_maintenance", 3, C(TunnelIds.ServiceJunction), C(TunnelIds.PowerDistribution),
                    C(TunnelIds.FanRoom, TunnelIds.FanBreaker, true)),
                Room(TunnelIds.PowerDistribution, "Power Distribution", "industrial", 3, C(TunnelIds.FanJunction)),
                Room(TunnelIds.FanRoom, "Industrial Fan Room", "industrial", 3, C(TunnelIds.FanJunction, TunnelIds.FanBreaker, true), C(TunnelIds.DropHatch)),
                Room(TunnelIds.DropHatch, "Plaza Drop Hatch", "plaza_network", 2, C(TunnelIds.FanRoom), C(TunnelIds.PlazaServiceHub, oneWay: true)),
                Room(TunnelIds.PlazaServiceHub, "Plaza Service Hub", "plaza_network", 2, C(TunnelIds.CustomerVent), C("PLAZA", TunnelIds.StaffDoor)),
                Room(TunnelIds.CustomerVent, "Customer Vent", "plaza_network", 2, C(TunnelIds.PlazaServiceHub), C("LOUNGE")),
            };

            var root = new GameObject("Tunnel Network");
            var manager = root.AddComponent<TunnelManager>();
            manager.SetRooms(rooms);
            root.AddComponent<TunnelDebugOverlay>();

            var byId = new Dictionary<string, VentRoomDefinition>();
            foreach (var r in rooms)
            {
                byId[r.RoomId] = r;
            }
            var volumes = new (string id, Vector3 min, Vector3 max)[]
            {
                (TunnelIds.Nook, new Vector3(-6f, 0f, -20.2f), new Vector3(3.4f, 2.6f, -14.5f)),
                (TunnelIds.ServiceJunction, new Vector3(-0.6f, 0f, -14.5f), new Vector3(1.4f, 2.3f, -7f)),
                (TunnelIds.LongCrawl, new Vector3(-0.6f, 0f, -7f), new Vector3(1.2f, 2.3f, 2.5f)),
                (TunnelIds.LongCrawl, new Vector3(-9.2f, 0f, 2.5f), new Vector3(1.2f, 2.7f, 4.3f)),
                (TunnelIds.PlazaAccess, new Vector3(-9.2f, 0f, 4.3f), new Vector3(-6.8f, 2.4f, 15.5f)),
                (TunnelIds.PlazaAccess, new Vector3(-8.5f, 0f, 15.5f), new Vector3(-7.5f, 0.85f, 25f)),
                (TunnelIds.StashDuct, new Vector3(-6.8f, 0f, 9.55f), new Vector3(-3f, 0.85f, 10.45f)),
                (TunnelIds.FanJunction, new Vector3(1.4f, 0f, -9.8f), new Vector3(8.5f, 2.3f, -8.2f)),
                (TunnelIds.FanJunction, new Vector3(8.5f, 0f, -11.5f), new Vector3(13.5f, 3f, -6f)),
                (TunnelIds.FanJunction, new Vector3(10f, 0f, -6f), new Vector3(12f, 1.6f, 2f)),
                (TunnelIds.PowerDistribution, new Vector3(13.7f, 0f, -11f), new Vector3(17f, 2.6f, -6.5f)),
                (TunnelIds.FanRoom, new Vector3(8.5f, 0f, 2.4f), new Vector3(13.5f, 3.6f, 7f)),
                (TunnelIds.DropHatch, new Vector3(13.7f, 0f, 3.8f), new Vector3(17.7f, 2.3f, 5.4f)),
                (TunnelIds.DropHatch, new Vector3(17.7f, 0f, 3.8f), new Vector3(19.3f, 4.8f, 20.8f)),
                (TunnelIds.DropHatch, new Vector3(17.7f, HighFloor, 20.8f), new Vector3(19.3f, 4.8f, 24.6f)),
                (TunnelIds.PlazaServiceHub, new Vector3(16.5f, 0f, 21f), new Vector3(23f, 2.4f, 31f)),
                (TunnelIds.CustomerVent, new Vector3(19.55f, 0f, 31.2f), new Vector3(20.45f, 0.85f, 34.6f)),
            };
            var vroot = Group("Room Volumes", root.transform);
            foreach (var (id, min, max) in volumes)
            {
                var go = new GameObject($"Room {id}");
                go.transform.SetParent(vroot, false);
                go.transform.position = (min + max) * 0.5f;
                var box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = max - min;
                go.AddComponent<RoomVolume>().Configure(byId[id]);
            }

            // East tunnels get the same dark, vignetted grade as the hideout
            var vol = new GameObject("East Tunnels Volume");
            vol.transform.SetParent(root.transform, false);
            var vbox = vol.AddComponent<BoxCollider>();
            vbox.isTrigger = true;
            vbox.center = new Vector3(10.5f, 2.5f, 4.5f);
            vbox.size = new Vector3(18f, 5f, 32f); // x 1.5 … 19.5, z -11.5 … 20.5
            var volume = vol.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.blendDistance = 1.5f;
            volume.priority = 10f;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(HideoutVolumePath);
        }

        private static VentRoomDefinition Room(string id, string name, string district, int plane, params VentRoomDefinition.Connection[] connections)
        {
            string path = $"{TunnelDataFolder}/Room_{id}.asset";
            var room = AssetDatabase.LoadAssetAtPath<VentRoomDefinition>(path);
            if (room == null)
            {
                room = ScriptableObject.CreateInstance<VentRoomDefinition>();
                AssetDatabase.CreateAsset(room, path);
            }
            room.Configure(id, name, district, plane, connections);
            EditorUtility.SetDirty(room);
            return room;
        }

        /// <summary>Wall running along Z (x from xMin to xMax) with a doorway between doorZ0 and doorZ1, doorHeight tall.</summary>
        private static void WallWithDoorZ(string name, Transform g, float xMin, float xMax, float z0, float z1, float doorZ0, float doorZ1,
            float doorHeight, float top, Material mat)
        {
            Box($"{name}_A", g, new Vector3(xMin, 0f, z0), new Vector3(xMax, top, doorZ0), mat);
            Box($"{name}_B", g, new Vector3(xMin, 0f, doorZ1), new Vector3(xMax, top, z1), mat);
            Box($"{name}_Lintel", g, new Vector3(xMin, doorHeight, doorZ0), new Vector3(xMax, top, doorZ1), mat);
        }
    }
}
