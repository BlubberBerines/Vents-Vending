using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// The starting hideout (GDD §5.1): a forgotten maintenance chamber wedged in the guts of the vent network.
    /// Small, low and enclosed. A crawl hatch leads into a cramped service gallery (crouch under a pipe bundle, vault
    /// two pipes, optional side duct with a stash) that ends at the plaza duct.
    ///
    /// Layout (floor y = 0, +Z north). Interior bounds:
    ///   Chamber     x[-3.2, 3.2]  z[-20, -14.5]  ceiling 2.5 (duct across it at 1.95)
    ///   Bed nook    x[-5.8, -3.2] z[-20, -17.6]  ceiling 1.4 (crouch in)
    ///   Exit duct   x 0.2, z[-14.5, -10.5]       crawl
    ///   Gallery D1  x[-0.6, 1.2]  z[-10.5, 2.5]  ceiling 2.3 (crouch section z[-6.5, -3.5])
    ///   Gallery D2  x[-9.2, 1.2]  z[2.5, 4.3]    ceiling 2.7 (pipes to vault)
    ///   Gallery D3  x[-9.2, -6.8] z[4.3, 15.5]   ceiling 2.4, plaza duct at its north end, stash duct east
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        // Gameplay furniture (all fronts face +Z, backs against the south wall).
        private static readonly Vector3 LockerPos = new Vector3(-2.45f, 0f, -19.6f);
        private static readonly Vector3 TerminalPos = new Vector3(-0.95f, 0f, -19.55f);
        private static readonly Vector3 MixerPos = new Vector3(1.15f, 0f, -19.58f);
        private static readonly Vector3 PackTablePos = new Vector3(2.3f, 0f, -15.45f);
        private static readonly Vector3 CratePos = new Vector3(-2.2f, 0f, -15.3f);
        private static readonly Vector3 BedPos = new Vector3(-4.5f, 0f, -19.0f);
        private static readonly Vector3 PlayerSpawn = new Vector3(0.4f, 0f, -16.2f);
        private const float PlayerSpawnYaw = 180f;

        private const float ChamberCeiling = 2.5f;
        private const float WallTop = 2.9f;
        private const float ExitDuctX = 0.2f;

        private const string HideoutVolumePath = "Assets/_Project/Settings/Rendering/HideoutVolumeProfile.asset";

        private static void BuildHideout(Transform level, Material floor, Material wall, Material vent, Material accent, Material panel, Material glow)
        {
            var root = Group("Hideout", level);
            Material dark = ArtLibrary.Get("M_DarkMetal");
            Material cable = GetOrCreateMaterial("Prop_Cable", new Color(0.1f, 0.1f, 0.12f), 0.4f);
            Material cableRed = GetOrCreateMaterial("Prop_CableRed", new Color(0.7f, 0.15f, 0.12f), 0.4f);

            BuildChamber(Group("Chamber", root), floor, wall, vent, accent, dark, cable, cableRed);
            BuildVentNetDesk(root);
            BuildExitDuct(Group("ExitDuct", root), floor, vent, accent, glow);
            BuildGallery(Group("ServiceGallery", root), floor, panel, vent, accent, dark, cable, cableRed);
            BuildStashDuct(Group("StashDuct", root), floor, vent, accent, glow);
            BuildPlazaWall(Group("PlazaWall", root), floor, wall);
            BuildHideoutImperfections(root);
            BuildHideoutVolume(root);
        }

        // ------------------------------------------------------------------ Chamber

        private static void BuildChamber(Transform g, Material floor, Material wall, Material vent, Material accent, Material dark,
            Material cable, Material cableRed)
        {
            const float c = ChamberCeiling;
            Box("Floor", g, new Vector3(-6f, -0.5f, -20.2f), new Vector3(3.4f, 0f, -14.3f), floor);

            // Walls
            Box("Wall_S", g, new Vector3(-6f, 0f, -20.2f), new Vector3(3.4f, WallTop, -20f), wall);
            Box("Wall_E", g, new Vector3(3.2f, 0f, -20.2f), new Vector3(3.4f, WallTop, -14.3f), wall);
            WallWithHatchX("Wall_N", g, -14.5f, -14.3f, -3.4f, 3.4f, ExitDuctX, WallTop, wall);
            Box("Wall_W", g, new Vector3(-3.4f, 0f, -17.6f), new Vector3(-3.2f, WallTop, -14.3f), wall);
            Box("NookSoffit", g, new Vector3(-3.4f, 1.4f, -20f), new Vector3(-3.2f, WallTop, -17.6f), wall);

            // Bed nook: crouch-height alcove
            Box("Nook_Wall_N", g, new Vector3(-6f, 0f, -17.6f), new Vector3(-3.4f, 1.6f, -17.4f), wall);
            Box("Nook_Wall_W", g, new Vector3(-6f, 0f, -20f), new Vector3(-5.8f, 1.6f, -17.4f), wall);
            Box("Nook_Ceiling", g, new Vector3(-5.8f, 1.4f, -20f), new Vector3(-3.2f, 1.6f, -17.6f), vent);

            // Ceiling with a slatted grate that leaks cold light from somewhere above
            CeilingWithGrate(g, new Vector2(-3.4f, -20.2f), new Vector2(3.4f, -14.3f), new Vector2(-1.0f, -18.0f), new Vector2(0.2f, -17.2f),
                c, WallTop, wall, dark, new Color(0.55f, 0.75f, 1f));

            // A fat supply duct crossing the room just above head height (you'll want to duck)
            Box("OverheadDuct", g, new Vector3(-3.2f, 1.95f, -15.85f), new Vector3(3.2f, c, -14.95f), vent);
            for (float x = -2.6f; x < 3f; x += 1.3f)
            {
                NoCollider(Box("DuctFlange", g, new Vector3(x - 0.04f, 1.91f, -15.89f), new Vector3(x + 0.04f, c, -14.91f), dark));
            }
            // ...and a riser dropping through the floor in the north-east corner
            Box("RiserDuct", g, new Vector3(2.55f, 0f, -14.95f - 0.6f), new Vector3(3.2f, 1.95f, -14.95f), vent);
            NoCollider(Box("RiserGrille", g, new Vector3(2.6f, 0.9f, -15.56f), new Vector3(3.15f, 1.5f, -15.54f), dark));

            // Pipe runs along the walls under the ceiling
            PipeX(g, -3.2f, 3.2f, 2.3f, -19.85f, 0.07f, vent, dark);
            PipeX(g, -3.2f, 3.2f, 2.12f, -19.9f, 0.035f, cable, null);
            PipeX(g, -3.2f, 3.2f, 2.07f, -19.9f, 0.03f, cableRed, null);
            PipeZ(g, -20f, -14.5f, 2.3f, 3.05f, 0.09f, vent, dark);
            var drop = Part("PipeDrop", g, new Vector3(3.05f, 1.15f, -19.6f), new Vector3(0.14f, 1.15f, 0.14f), vent, PrimitiveType.Cylinder);
            _ = drop;
            var valve = Part("ValveWheel", g, new Vector3(2.88f, 1.3f, -19.6f), new Vector3(0.28f, 0.025f, 0.28f), accent, PrimitiveType.Cylinder);
            valve.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            // Light: one bare bulb on a cord, a dim amber glow in the bed nook. (Mixer lamp + terminal glow come with those.)
            HangingBulb(g, new Vector3(0.9f, c, -17.6f), 0.45f, new Color(1f, 0.74f, 0.45f), 6.5f, 2.4f, true);
            HangingBulb(g, new Vector3(-4.0f, 1.4f, -18.1f), 0.12f, new Color(1f, 0.55f, 0.3f), 3.2f, 1.5f, false);
        }

        // ------------------------------------------------------------------ The den PC (VentNet)

        /// <summary>
        /// An old beige CRT on a battered desk against the chamber's east wall: the VentNet terminal where customer
        /// messages arrive (the hideout's answer to a phone). Parts are children of the terminal so aiming at any of
        /// them uses it.
        /// </summary>
        private static void BuildVentNetDesk(Transform parent)
        {
            Material dark = ArtLibrary.Get("M_DarkMetal");
            Material top = Worn(GetOrCreateMaterial("Prop_DeskTop", new Color(0.45f, 0.32f, 0.2f), 0.3f), 0.5f, 0.4f, 0.5f);
            Material crt = Worn(GetOrCreateMaterial("Prop_CRT", new Color(0.76f, 0.72f, 0.6f), 0.35f), 0.45f, 0.4f, 0.3f);
            Material screen = GetOrCreateMaterial("Screen_VentNet", new Color(0.02f, 0.08f, 0.04f), 0.8f, new Color(0.06f, 0.35f, 0.14f));
            Material keys = GetOrCreateMaterial("Prop_Keyboard", new Color(0.22f, 0.21f, 0.2f), 0.3f);

            var root = new GameObject("VentNet PC");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(2.9f, 0f, -17.3f);
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(-0.05f, 0.75f, 0f);
            col.size = new Vector3(0.75f, 1.5f, 1.3f);
            Transform t = root.transform;

            Box("DeskTop", t, new Vector3(2.55f, 0.74f, -17.95f), new Vector3(3.2f, 0.79f, -16.65f), top);
            foreach (var (x, z) in new[] { (2.6f, -17.9f), (2.6f, -16.7f), (3.15f, -17.9f), (3.15f, -16.7f) })
            {
                NoCollider(Box("DeskLeg", t, new Vector3(x - 0.025f, 0f, z - 0.025f), new Vector3(x + 0.025f, 0.74f, z + 0.025f), dark));
            }
            var body = NoCollider(Block("CRT", t, new Vector3(2.99f, 1.0f, -17.3f), new Vector3(0.42f, 0.4f, 0.44f), crt));
            body.transform.rotation = Quaternion.Euler(0f, -4f, 0f);
            NoCollider(Block("CRTBack", t, new Vector3(3.12f, 0.98f, -17.3f), new Vector3(0.18f, 0.3f, 0.32f), crt));
            NoCollider(Block("Screen", t, new Vector3(2.775f, 1.02f, -17.3f), new Vector3(0.015f, 0.29f, 0.35f), screen)).transform.rotation = Quaternion.Euler(0f, -4f, 0f);
            NoCollider(Block("Keyboard", t, new Vector3(2.7f, 0.805f, -17.25f), new Vector3(0.17f, 0.025f, 0.42f), keys)).transform.rotation = Quaternion.Euler(0f, 7f, 0f);
            NoCollider(Block("CRTStickyNote", t, new Vector3(2.77f, 1.2f, -17.1f), new Vector3(0.004f, 0.06f, 0.06f),
                GetOrCreateMaterial("Prop_StickyNote", new Color(1f, 0.9f, 0.35f), 0.2f))).transform.rotation = Quaternion.Euler(8f, -4f, 0f);
            CylinderBetween("PCCable", t, new Vector3(3.15f, 0.85f, -17.45f), new Vector3(3.18f, 0.02f, -17.6f), 0.012f, GetOrCreateMaterial("Prop_Cable", new Color(0.1f, 0.1f, 0.12f), 0.4f));
            Block("Stool", parent, new Vector3(2.15f, 0.22f, -17.25f), new Vector3(0.4f, 0.44f, 0.4f), ArtLibrary.Get("M_GreenPanel")).transform.rotation = Quaternion.Euler(0f, 14f, 0f);

            // Screen text and its glow (text outside Level so the NavMesh bake never reads it)
            var text = new GameObject("VentNet Screen Text");
            text.transform.SetPositionAndRotation(new Vector3(2.765f, 1.02f, -17.3f), Quaternion.Euler(0f, 86f, 0f));
            var tmp = text.AddComponent<TextMeshPro>();
            tmp.fontSize = 0.55f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.55f, 1f, 0.65f);
            tmp.rectTransform.sizeDelta = new Vector2(0.32f, 0.26f);
            tmp.text = "<b>VENTNET</b>";
            CreatePointLight("VentNet Screen Glow", new Vector3(2.55f, 1.05f, -17.3f), new Color(0.35f, 1f, 0.5f), 1.8f, 0.7f, t);

            root.AddComponent<VentsAndVending.Orders.VentNetTerminal>().Configure(tmp);
        }

        // ------------------------------------------------------------------ Exit duct (GDD §5.1 "one basic ventilation exit")

        private static void BuildExitDuct(Transform g, Material floor, Material vent, Material accent, Material glow)
        {
            const float z0 = -14.3f, z1 = -10.5f, x = ExitDuctX;
            CrawlDuctZ(g, x, z0, z1, floor, vent);
            NoCollider(Block("LightStrip", g, new Vector3(x, 0.84f, (z0 + z1) * 0.5f), new Vector3(0.3f, 0.02f, 0.6f), glow));
            CreatePointLight("Exit Duct Light", new Vector3(x, 0.7f, (z0 + z1) * 0.5f), new Color(1f, 0.7f, 0.35f), 2.5f, 0.8f, g);
            VentTrigger("VentZone_Exit", g, new Vector3(x, 0.425f, (z0 - 0.8f + z1 + 0.6f) * 0.5f), new Vector3(0.9f, 0.85f, (z1 + 0.6f) - (z0 - 0.8f)), "duct_exit");

            // The hatch players crawl through to leave home
            CreateHatch("Vent Hatch", g, new Vector3(x, 0.85f, -14.54f), new Vector3(100f, 0f, 0f), vent, accent);
            NoCollider(Block("Hatch_Frame", g, new Vector3(x, 0.92f, -14.56f), new Vector3(1.1f, 0.08f, 0.06f), accent));
        }

        // ------------------------------------------------------------------ Service gallery

        private static void BuildGallery(Transform g, Material floor, Material wall, Material vent, Material accent, Material dark,
            Material cable, Material cableRed)
        {
            // D1: north from the exit duct
            Box("D1_Floor", g, new Vector3(-0.8f, -0.5f, -10.7f), new Vector3(1.4f, 0f, 2.5f), floor);
            WallWithHatchX("D1_Wall_S", g, -10.7f, -10.5f, -0.8f, 1.4f, ExitDuctX, WallTop, wall);
            Box("D1_Wall_W", g, new Vector3(-0.8f, 0f, -10.5f), new Vector3(-0.6f, WallTop, 2.5f), wall);
            // East side opens into the corridor toward 04 Fan Junction (02 Service Junction is a three-way hub)
            Box("D1_Wall_E_South", g, new Vector3(1.2f, 0f, -10.5f), new Vector3(1.4f, WallTop, -9.8f), wall);
            Box("D1_Wall_E", g, new Vector3(1.2f, 0f, -8.2f), new Vector3(1.4f, WallTop, 4.5f), wall);
            Box("D1_Wall_E_Lintel", g, new Vector3(1.2f, 2.3f, -9.8f), new Vector3(1.4f, WallTop, -8.2f), wall);
            CeilingWithGrate(g, new Vector2(-0.8f, -10.7f), new Vector2(1.4f, 2.5f), new Vector2(-0.3f, -1.6f), new Vector2(0.9f, -0.9f),
                2.3f, WallTop, vent, dark, new Color(0.55f, 0.75f, 1f));

            // Crouch section: a bundle of mains crossing the gallery at 1.3 m
            Box("PipeBundle", g, new Vector3(-0.6f, 1.3f, -6.5f), new Vector3(1.2f, 2.3f, -3.5f), vent);
            for (int i = 0; i < 4; i++)
            {
                float z = -6.3f + i * 0.85f;
                var main = Part("BundleMain", g, new Vector3(0.3f, 1.42f, z), new Vector3(0.36f, 0.93f, 0.36f), i % 2 == 0 ? vent : dark, PrimitiveType.Cylinder);
                main.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }
            NoCollider(Box("CrouchHazard_S", g, new Vector3(-0.6f, 1.22f, -6.56f), new Vector3(1.2f, 1.3f, -6.5f), accent));
            NoCollider(Box("CrouchHazard_N", g, new Vector3(-0.6f, 1.22f, -3.5f), new Vector3(1.2f, 1.3f, -3.44f), accent));
            CreatePointLight("Crouch Warning", new Vector3(0.3f, 1.15f, -6.75f), new Color(1f, 0.25f, 0.15f), 1.8f, 0.6f, g);
            HangingBulb(g, new Vector3(0.3f, 2.3f, -8.8f), 0.25f, new Color(1f, 0.72f, 0.4f), 4.5f, 1.3f, false);

            PipeZ(g, -10.5f, 2.5f, 2.05f, 1.05f, 0.08f, vent, dark);
            PipeZ(g, -10.5f, 2.5f, 1.8f, 1.12f, 0.035f, cable, null);
            PipeZ(g, -10.5f, 2.5f, 0.35f, -0.47f, 0.1f, vent, dark);

            // D2: west, two pipes to vault
            Box("D2_Floor", g, new Vector3(-9.4f, -0.5f, 2.5f), new Vector3(1.4f, 0f, 4.5f), floor);
            Box("D2_Wall_S", g, new Vector3(-9.4f, 0f, 2.3f), new Vector3(-0.6f, WallTop, 2.5f), wall);
            Box("D2_Wall_N", g, new Vector3(-6.8f, 0f, 4.3f), new Vector3(1.4f, WallTop, 4.5f), wall);
            Box("D2_Ceiling", g, new Vector3(-9.4f, 2.7f, 2.3f), new Vector3(1.4f, WallTop + 0.1f, 4.5f), vent);
            Box("D1_D2_Header", g, new Vector3(-0.6f, 2.3f, 2.4f), new Vector3(1.2f, 2.7f, 2.5f), wall);

            var pipeA = Part("FloorMain_A", g, new Vector3(-2.6f, 0.25f, 3.4f), new Vector3(0.5f, 0.9f, 0.5f), vent, PrimitiveType.Cylinder);
            pipeA.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            pipeA.AddComponent<CapsuleCollider>().direction = 1;
            pipeA.isStatic = true;
            Box("FloorDuct_B", g, new Vector3(-5.5f, 0f, 2.5f), new Vector3(-4.9f, 0.65f, 4.3f), vent);
            NoCollider(Box("FloorDuct_B_Stripe", g, new Vector3(-5.52f, 0.5f, 2.5f), new Vector3(-4.88f, 0.58f, 4.3f), accent));
            PipeX(g, -9.2f, 1.2f, 2.45f, 4.15f, 0.08f, vent, dark);
            PipeX(g, -9.2f, 1.2f, 2.25f, 4.2f, 0.035f, cableRed, null);
            HangingBulb(g, new Vector3(-3.9f, 2.7f, 3.4f), 0.35f, new Color(1f, 0.72f, 0.4f), 5f, 1.5f, false);

            // D3: north to the plaza duct
            Box("D3_Floor", g, new Vector3(-9.4f, -0.5f, 4.3f), new Vector3(-6.6f, 0f, 15.7f), floor);
            Box("D3_Wall_W", g, new Vector3(-9.4f, 0f, 2.3f), new Vector3(-9.2f, WallTop, 15.7f), wall);
            WallWithHatchZ("D3_Wall_E", g, -6.8f, -6.6f, 4.3f, 15.7f, StashDuctZ, WallTop, wall);
            WallWithHatchX("D3_Wall_N", g, 15.5f, 15.7f, -9.4f, -6.6f, PlazaVentX, WallTop, wall);
            CeilingWithGrate(g, new Vector2(-9.4f, 4.3f), new Vector2(-6.6f, 15.7f), new Vector2(-8.6f, 7.6f), new Vector2(-7.4f, 8.4f),
                2.4f, WallTop, vent, dark, new Color(0.55f, 0.75f, 1f));
            PipeZ(g, 4.3f, 15.5f, 2.15f, -9.05f, 0.09f, vent, dark);
            PipeZ(g, 4.3f, 15.5f, 1.9f, -9.1f, 0.035f, cable, null);
            PipeZ(g, 4.3f, 15.5f, 1.85f, -9.1f, 0.03f, cableRed, null);
            HangingBulb(g, new Vector3(-8f, 2.4f, 13.4f), 0.3f, new Color(1f, 0.72f, 0.4f), 5f, 1.4f, false);
        }

        // ------------------------------------------------------------------ Side duct with a stash (reward for poking around)

        private const float StashDuctZ = 10f;

        private static void BuildStashDuct(Transform g, Material floor, Material vent, Material accent, Material glow)
        {
            const float x0 = -6.6f, x1 = -3.0f, z = StashDuctZ, half = 0.45f, h = 0.85f, t = 0.1f;
            Box("Floor", g, new Vector3(x0, -0.5f, z - half - t), new Vector3(x1 + t, 0f, z + half + t), floor);
            Box("Wall_S", g, new Vector3(x0, 0f, z - half - t), new Vector3(x1, h, z - half), vent);
            Box("Wall_N", g, new Vector3(x0, 0f, z + half), new Vector3(x1, h, z + half + t), vent);
            Box("Roof", g, new Vector3(x0, h, z - half - t), new Vector3(x1 + t, h + t, z + half + t), vent);
            Box("EndCap", g, new Vector3(x1, 0f, z - half), new Vector3(x1 + t, h, z + half), vent);
            NoCollider(Block("LightStrip", g, new Vector3(-4f, h - 0.01f, z), new Vector3(0.6f, 0.02f, 0.3f), glow));
            CreatePointLight("Stash Light", new Vector3(-3.6f, 0.6f, z), new Color(1f, 0.7f, 0.35f), 2.2f, 0.9f, g);
            VentTrigger("VentZone_Stash", g, new Vector3((x0 - 0.8f + x1) * 0.5f, h * 0.5f, z), new Vector3(x1 - (x0 - 0.8f), h, 0.9f), "duct_stash");

            var hatch = CreateHatch("Stash Hatch", g, new Vector3(-6.84f, h, z), new Vector3(100f, 0f, 0f), vent, accent);
            hatch.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }

        // ------------------------------------------------------------------ Shared wall with the plaza

        private static void BuildPlazaWall(Transform g, Material floor, Material wall)
        {
            // Floor under the plaza duct between the gallery and the plaza
            Box("DuctFloor", g, new Vector3(-8.6f, -0.5f, 15.5f), new Vector3(-7.4f, 0f, 20.3f), floor);
            float gapL = PlazaVentX - 0.55f, gapR = PlazaVentX + 0.55f;
            Box("Wall_West", g, new Vector3(-PlazaHalfWidth - 0.5f, 0f, 20f), new Vector3(gapL, 4f, 20.5f), wall);
            Box("Wall_East", g, new Vector3(gapR, 0f, 20f), new Vector3(PlazaHalfWidth + 0.5f, 4f, 20.5f), wall);
            Box("Wall_Lintel", g, new Vector3(gapL, 0.95f, 20f), new Vector3(gapR, 4f, 20.5f), wall);
        }

        // ------------------------------------------------------------------ Atmosphere

        /// <summary>Local post volume over the hideout and gallery: darker, heavier vignette. The plaza stays bright.</summary>
        private static void BuildHideoutVolume(Transform parent)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(HideoutVolumePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, HideoutVolumePath);
            }

            Vignette vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.36f);
            vignette.smoothness.Override(0.55f);
            vignette.color.Override(new Color(0.02f, 0.02f, 0.04f));

            ColorAdjustments grade = GetOrAdd<ColorAdjustments>(profile);
            grade.postExposure.Override(-0.15f);
            grade.colorFilter.Override(new Color(0.94f, 0.96f, 1f));
            EditorUtility.SetDirty(profile);

            var go = new GameObject("Hideout Volume");
            go.transform.SetParent(parent, false);
            go.layer = 0;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(-2.2f, 1.5f, -2.5f);
            box.size = new Vector3(13f, 4f, 36f); // chamber → end of the gallery (z -20.5 … 15.5)
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.blendDistance = 1.5f;
            volume.priority = 10f;
            volume.sharedProfile = profile;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>(true);
                component.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            component.active = true;
            EditorUtility.SetDirty(component);
            return component;
        }

        // ------------------------------------------------------------------ Interactables

        private static void BuildInteractables(Transform parent, Material prop, Material accent, Material vent)
        {
            var g = Group("Interactables", parent);

            // Storage locker against the back wall, pre-stocked
            var locker = Block("Storage Locker", g, LockerPos + new Vector3(0f, 0.9f, 0f), new Vector3(1.2f, 1.8f, 0.6f), prop);
            locker.isStatic = false;
            Block("Locker_Trim", g, LockerPos + new Vector3(0f, 1.82f, 0f), new Vector3(1.25f, 0.05f, 0.65f), accent);
            var lockerInv = locker.AddComponent<InventoryComponent>();
            lockerInv.Configure("Storage Locker", 8, VentsAndVending.Progression.UpgradeKeys.LockerCapacity, "hideout.locker");
            lockerInv.AddStartingItem(_lumina, 4);
            lockerInv.AddStartingItem(_stabiliser, 3);
            locker.AddComponent<StorageContainer>();

            // Loose ingredients: on top of the crate stack and by the packing table
            PlacePickup(_glarb, 2, CratePos + new Vector3(0.3f, 1.42f, 0.05f), g);
            PlacePickup(_lumina, 1, new Vector3(1.0f, 0.02f, -16.3f), g);
            PlacePickup(_stabiliser, 1, new Vector3(1.35f, 0.02f, -16.55f), g);

            // Hidden stash at the end of the side duct
            PlacePickup(_glarb, 5, new Vector3(-3.45f, 0.02f, StashDuctZ - 0.15f), g);
            PlacePickup(_stabiliser, 2, new Vector3(-3.55f, 0.02f, StashDuctZ + 0.15f), g);
        }

        // ------------------------------------------------------------------ Builders

        /// <summary>Axis-aligned block from two corners (world space).</summary>
        private static GameObject Box(string name, Transform parent, Vector3 min, Vector3 max, Material mat)
            => Block(name, parent, (min + max) * 0.5f, max - min, mat);

        /// <summary>Wall running along X (z from zMin to zMax) with a crawl-duct opening (0.9 × 0.85 m) centred on hatchX.</summary>
        private static void WallWithHatchX(string name, Transform g, float zMin, float zMax, float x0, float x1, float hatchX, float top, Material mat)
        {
            Box($"{name}_A", g, new Vector3(x0, 0f, zMin), new Vector3(hatchX - 0.45f, top, zMax), mat);
            Box($"{name}_B", g, new Vector3(hatchX + 0.45f, 0f, zMin), new Vector3(x1, top, zMax), mat);
            Box($"{name}_Lintel", g, new Vector3(hatchX - 0.45f, 0.85f, zMin), new Vector3(hatchX + 0.45f, top, zMax), mat);
        }

        private static void WallWithHatchZ(string name, Transform g, float xMin, float xMax, float z0, float z1, float hatchZ, float top, Material mat)
        {
            Box($"{name}_A", g, new Vector3(xMin, 0f, z0), new Vector3(xMax, top, hatchZ - 0.45f), mat);
            Box($"{name}_B", g, new Vector3(xMin, 0f, hatchZ + 0.45f), new Vector3(xMax, top, z1), mat);
            Box($"{name}_Lintel", g, new Vector3(xMin, 0.85f, hatchZ - 0.45f), new Vector3(xMax, top, hatchZ + 0.45f), mat);
        }

        /// <summary>Crawl-only duct along Z: 0.9 m wide, 0.85 m tall inside.</summary>
        private static void CrawlDuctZ(Transform g, float x, float z0, float z1, Material floor, Material vent)
        {
            const float half = 0.45f, h = 0.85f, t = 0.1f;
            Box("Floor", g, new Vector3(x - half - t, -0.5f, z0), new Vector3(x + half + t, 0f, z1), floor);
            Box("Wall_L", g, new Vector3(x - half - t, 0f, z0), new Vector3(x - half, h, z1), vent);
            Box("Wall_R", g, new Vector3(x + half, 0f, z0), new Vector3(x + half + t, h, z1), vent);
            Box("Roof", g, new Vector3(x - half - t, h, z0), new Vector3(x + half + t, h + t, z1), vent);
        }

        /// <summary>
        /// Ceiling slab (bottom at <paramref name="y"/>) with a rectangular hole covered by slats. Above the hole a short
        /// closed shaft hides a cold spot light that casts slat shadows on the floor; faint additive blades sell the shafts.
        /// </summary>
        private static void CeilingWithGrate(Transform parent, Vector2 min, Vector2 max, Vector2 holeMin, Vector2 holeMax,
            float y, float top, Material slab, Material slat, Color lightColor)
        {
            var g = Group("Ceiling", parent);
            float ty = Mathf.Max(top, y + 0.2f);
            Box("Slab_S", g, new Vector3(min.x, y, min.y), new Vector3(max.x, ty, holeMin.y), slab);
            Box("Slab_N", g, new Vector3(min.x, y, holeMax.y), new Vector3(max.x, ty, max.y), slab);
            Box("Slab_W", g, new Vector3(min.x, y, holeMin.y), new Vector3(holeMin.x, ty, holeMax.y), slab);
            Box("Slab_E", g, new Vector3(holeMax.x, y, holeMin.y), new Vector3(max.x, ty, holeMax.y), slab);

            // Shaft above the grate
            const float shaftH = 1.3f, t = 0.1f;
            float sy = ty + shaftH;
            NoCollider(Box("Shaft_S", g, new Vector3(holeMin.x - t, ty, holeMin.y - t), new Vector3(holeMax.x + t, sy, holeMin.y), slab));
            NoCollider(Box("Shaft_N", g, new Vector3(holeMin.x - t, ty, holeMax.y), new Vector3(holeMax.x + t, sy, holeMax.y + t), slab));
            NoCollider(Box("Shaft_W", g, new Vector3(holeMin.x - t, ty, holeMin.y), new Vector3(holeMin.x, sy, holeMax.y), slab));
            NoCollider(Box("Shaft_E", g, new Vector3(holeMax.x, ty, holeMin.y), new Vector3(holeMax.x + t, sy, holeMax.y), slab));
            NoCollider(Box("Shaft_Cap", g, new Vector3(holeMin.x - t, sy, holeMin.y - t), new Vector3(holeMax.x + t, sy + t, holeMax.y + t), slab));

            // Slats (along X) and a frame
            float cx = (holeMin.x + holeMax.x) * 0.5f, w = holeMax.x - holeMin.x, d = holeMax.y - holeMin.y;
            const int slats = 5;
            for (int i = 0; i < slats; i++)
            {
                float z = holeMin.y + d * (i + 0.5f) / slats;
                NoCollider(Box("Slat", g, new Vector3(holeMin.x, y + 0.02f, z - 0.035f), new Vector3(holeMax.x, y + 0.1f, z + 0.035f), slat));
            }
            NoCollider(Box("GrateFrame_S", g, new Vector3(holeMin.x - 0.05f, y - 0.03f, holeMin.y - 0.05f), new Vector3(holeMax.x + 0.05f, y, holeMin.y), slat));
            NoCollider(Box("GrateFrame_N", g, new Vector3(holeMin.x - 0.05f, y - 0.03f, holeMax.y), new Vector3(holeMax.x + 0.05f, y, holeMax.y + 0.05f), slat));

            // Cold light from above, shadowed by the slats
            var spot = new GameObject("Grate Light").AddComponent<Light>();
            spot.transform.SetParent(g, false);
            spot.transform.SetPositionAndRotation(new Vector3(cx, sy - 0.1f, (holeMin.y + holeMax.y) * 0.5f), Quaternion.Euler(90f, 0f, 0f));
            spot.type = LightType.Spot;
            spot.color = lightColor;
            spot.spotAngle = 95f;
            spot.innerSpotAngle = 60f;
            spot.range = y + shaftH + 2f;
            spot.intensity = 9f;
            spot.shadows = LightShadows.Hard;
            spot.shadowBias = 0.02f;
            spot.shadowNormalBias = 0.2f;

            // Visible blades through each gap
            Material blade = GetLightBladeMaterial(lightColor);
            for (int i = 0; i <= slats; i++)
            {
                float z = holeMin.y + d * i / slats;
                if (i == 0 || i == slats)
                {
                    continue;
                }
                float gapZ = z; // slat centres sit at (i + 0.5) / slats, so the gaps between them are at i / slats
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "LightBlade";
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.transform.SetParent(g, false);
                quad.transform.position = new Vector3(cx, y * 0.5f, gapZ);
                quad.transform.localScale = new Vector3(w * 1.15f, y, 1f);
                var r = quad.GetComponent<MeshRenderer>();
                r.sharedMaterial = blade;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        /// <summary>A bulb on a cord hanging from <paramref name="anchor"/> (a ceiling point), with its point light.</summary>
        /// <summary>
        /// A bulb on a cord hanging from <paramref name="anchor"/> (a ceiling point), with its point light. The group
        /// pivots at the anchor and sways gently (pass 2 idle motion), carrying the light and its shadows with it.
        /// </summary>
        private static (Transform group, Light light, Renderer bulb) HangingBulb(Transform parent, Vector3 anchor, float drop, Color color,
            float range, float intensity, bool shadows, float sway = 2.2f)
        {
            var g = Group("HangingBulb", parent);
            g.position = anchor;
            Material cord = GetOrCreateMaterial("Prop_Cable", new Color(0.1f, 0.1f, 0.12f), 0.4f);
            Material socket = ArtLibrary.Get("M_DarkMetal");
            Material bulb = GetOrCreateMaterial("Bulb_Warm", new Color(1f, 0.85f, 0.6f), 0.6f, new Color(1f, 0.75f, 0.45f) * 3f);

            Part("Cord", g, Vector3.down * (drop * 0.5f), new Vector3(0.012f, drop * 0.5f, 0.012f), cord, PrimitiveType.Cylinder);
            Part("Socket", g, Vector3.down * (drop + 0.03f), new Vector3(0.05f, 0.03f, 0.05f), socket, PrimitiveType.Cylinder);
            var b = Part("Bulb", g, Vector3.down * (drop + 0.1f), new Vector3(0.09f, 0.11f, 0.09f), bulb, PrimitiveType.Sphere);
            b.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            var light = new GameObject("Bulb Light").AddComponent<Light>();
            light.transform.SetParent(g, false);
            light.transform.localPosition = Vector3.down * (drop + 0.1f);
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowNearPlane = 0.15f;

            if (sway > 0f)
            {
                var rng = Rng(anchor);
                g.gameObject.AddComponent<VentsAndVending.World.Pendulum>().Configure(sway * (0.8f + 0.4f * (float)rng.NextDouble()), 3.2f + (float)rng.NextDouble() * 1.6f);
            }
            return (g, light, b.GetComponent<Renderer>());
        }

        /// <summary>Additive, double-sided, fading toward the floor. URP Particles/Unlit so it skips depth (no outlines).</summary>
        private static Material GetLightBladeMaterial(Color color)
        {
            const string path = MaterialFolder + "/FX_LightBlade.mat";
            const string texPath = MaterialFolder + "/FX_LightBladeFade.asset";

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null)
            {
                tex = new Texture2D(4, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "FX_LightBladeFade" };
                for (int yy = 0; yy < 64; yy++)
                {
                    float v = yy / 63f;
                    float a = Mathf.Pow(v, 2.2f);
                    for (int xx = 0; xx < 4; xx++)
                    {
                        tex.SetPixel(xx, yy, new Color(1f, 1f, 1f, a));
                    }
                }
                tex.Apply();
                AssetDatabase.CreateAsset(tex, texPath);
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.028f));
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f); // additive
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
