using UnityEngine;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Prop pass: detail dressing for the hideout workshop (GDD §5.1 starting hideout: sleeping area, storage,
    /// mixing station, packaging surface) and plaza stall goods. Static detail only — gameplay objects are unchanged,
    /// details are placed beside them in world space so prefabs and colliders stay simple.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        private static void BuildProps(Transform level)
        {
            var g = Group("Props", level);

            Material orange = ArtLibrary.Get("M_WorkshopOrange");
            Material hazard = ArtLibrary.Get("M_HazardPaint");
            Material dark = ArtLibrary.Get("M_DarkMetal");
            Material green = ArtLibrary.Get("M_GreenPanel");
            Material duct = ArtLibrary.Get("M_DuctMetal");
            Material silver = ArtLibrary.Get("M_BrushedSilver");
            Material cardboard = ArtLibrary.Get("M_Cardboard");
            Material seam = GetOrCreateMaterial("Prop_Seam", new Color(0.05f, 0.04f, 0.06f), 0.2f);
            Material tape = GetOrCreateMaterial("Prop_Tape", new Color(0.85f, 0.72f, 0.38f), 0.5f);
            Material mattress = GetOrCreateMaterial("Prop_Mattress", new Color(0.78f, 0.74f, 0.62f), 0.2f);
            Material stain = GetOrCreateMaterial("Prop_Stain", new Color(0.55f, 0.48f, 0.3f), 0.2f);
            Material blanket = GetOrCreateMaterial("Prop_Blanket", new Color(0.42f, 0.3f, 0.55f), 0.2f);
            Material pallet = GetOrCreateMaterial("Prop_Pallet", new Color(0.5f, 0.36f, 0.22f), 0.2f);
            Material cable = GetOrCreateMaterial("Prop_Cable", new Color(0.1f, 0.1f, 0.12f), 0.4f);
            Material cableRed = GetOrCreateMaterial("Prop_CableRed", new Color(0.7f, 0.15f, 0.12f), 0.4f);
            Material statusGreen = GetOrCreateMaterial("Prop_StatusGreen", new Color(0.1f, 0.6f, 0.2f), 0.5f, new Color(0.3f, 2f, 0.5f));
            Material statusAmber = GetOrCreateMaterial("Prop_StatusAmber", new Color(0.6f, 0.4f, 0.1f), 0.5f, new Color(2f, 1.2f, 0.2f));

            DressLocker(g, new Vector3(-2.5f, 0f, -16.5f), seam, silver, dark);
            DressMixingStation(g, new Vector3(1.6f, 0f, -16.6f), seam, silver, dark);
            DressTerminal(g, new Vector3(-0.4f, 0f, -19.55f), cable, statusGreen, statusAmber);
            BuildSleepingCorner(g, new Vector3(-7.2f, 0f, -18.6f), pallet, mattress, stain, blanket);
            BuildPackagingTable(g, new Vector3(5.6f, 0f, -17.8f), dark, cardboard, tape, silver);
            BuildCrateStack(g, new Vector3(-5f, 0f, -16f), green, hazard, cardboard);
            BuildWallRuns(g, duct, dark, cable, cableRed, hazard);
        }

        // ------------------------------------------------------------------ Workshop pieces

        /// <summary>Locker front faces +Z: door seam, vent slats, handles, feet and a sticker.</summary>
        private static void DressLocker(Transform parent, Vector3 basePos, Material seam, Material handle, Material feet)
        {
            var g = Group("LockerDetails", parent);
            float front = basePos.z + 0.305f;
            NoCollider(Block("DoorSeam", g, new Vector3(basePos.x, 0.95f, front), new Vector3(0.02f, 1.6f, 0.012f), seam));
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    NoCollider(Block("VentSlat", g, new Vector3(basePos.x + side * 0.3f, 1.6f - i * 0.06f, front), new Vector3(0.36f, 0.022f, 0.012f), seam));
                }
                NoCollider(Block("Handle", g, new Vector3(basePos.x + side * 0.07f, 1.0f, front + 0.02f), new Vector3(0.03f, 0.16f, 0.03f), handle));
                NoCollider(Block("Foot", g, new Vector3(basePos.x + side * 0.5f, 0.02f, basePos.z + 0.22f), new Vector3(0.08f, 0.04f, 0.08f), feet));
                NoCollider(Block("Foot", g, new Vector3(basePos.x + side * 0.5f, 0.02f, basePos.z - 0.22f), new Vector3(0.08f, 0.04f, 0.08f), feet));
            }
            CreateSign("Locker_Sticker", "<color=#FFD24D>TOTALLY NOT</color>\nCONTRABAND", new Vector3(basePos.x + 0.3f, 0.6f, front + 0.01f), 180f, 0.45f, Color.white, 0.5f);
        }

        /// <summary>Bench front faces +Z: drawer fronts, handles, an ingredient bottle rack and a warning plate.</summary>
        private static void DressMixingStation(Transform parent, Vector3 basePos, Material seam, Material handle, Material rack)
        {
            var g = Group("MixingStationDetails", parent);
            float front = basePos.z + 0.355f;
            for (int i = 0; i < 2; i++)
            {
                float x = basePos.x - 0.35f + i * 0.7f;
                NoCollider(Block("DrawerSeamTop", g, new Vector3(x, 0.72f, front), new Vector3(0.6f, 0.015f, 0.012f), seam));
                NoCollider(Block("DrawerSeamBottom", g, new Vector3(x, 0.38f, front), new Vector3(0.6f, 0.015f, 0.012f), seam));
                NoCollider(Block("DrawerHandle", g, new Vector3(x, 0.62f, front + 0.02f), new Vector3(0.18f, 0.025f, 0.03f), handle));
            }

            // Bottle rack on the back edge of the bench (ingredient colours from the GDD palette)
            NoCollider(Block("RackShelf", g, new Vector3(basePos.x + 0.35f, 1.0f, basePos.z - 0.28f), new Vector3(0.5f, 0.03f, 0.12f), rack));
            Color[] colors = { new Color(0.35f, 1f, 0.2f), new Color(0.1f, 0.95f, 1f), new Color(1f, 0.93f, 0.55f), new Color(1f, 0.4f, 0.8f) };
            for (int i = 0; i < colors.Length; i++)
            {
                Material glass = GetOrCreateMaterial($"Prop_Bottle_{i}", colors[i] * 0.6f, 0.8f, colors[i] * 0.9f);
                Part($"Bottle_{i}", g, new Vector3(basePos.x + 0.17f + i * 0.12f, 1.09f, basePos.z - 0.28f), new Vector3(0.07f, 0.07f, 0.07f), glass, PrimitiveType.Cylinder);
            }

            CreateSign("Station_Warning", "<color=#FFD24D>!</color> DO NOT OVERHEAT\n<size=70%>(again)</size>", new Vector3(basePos.x, 0.82f, front + 0.01f), 180f, 0.5f, new Color(1f, 0.85f, 0.4f), 1.2f);
        }

        /// <summary>Terminal: status lights and a cable run down into the wall.</summary>
        private static void DressTerminal(Transform parent, Vector3 basePos, Material cable, Material green, Material amber)
        {
            var g = Group("TerminalDetails", parent);
            Part("StatusLight_A", g, basePos + new Vector3(-0.36f, 1.74f, 0.08f), Vector3.one * 0.04f, green, PrimitiveType.Sphere);
            Part("StatusLight_B", g, basePos + new Vector3(-0.28f, 1.74f, 0.08f), Vector3.one * 0.04f, amber, PrimitiveType.Sphere);
            var c1 = Part("Cable_Down", g, basePos + new Vector3(0.06f, 0.6f, -0.13f), new Vector3(0.05f, 0.6f, 0.05f), cable, PrimitiveType.Cylinder);
            var c2 = Part("Cable_Floor", g, basePos + new Vector3(0.06f, 0.03f, -0.28f), new Vector3(0.05f, 0.15f, 0.05f), cable, PrimitiveType.Cylinder);
            c2.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        /// <summary>GDD §5.1 "one sleeping area": a mattress on a pallet, pillow and a rumpled blanket.</summary>
        private static void BuildSleepingCorner(Transform parent, Vector3 basePos, Material pallet, Material mattress, Material stain, Material blanket)
        {
            var g = Group("SleepingCorner", parent);
            Block("Pallet", g, basePos + new Vector3(0f, 0.07f, 0f), new Vector3(2.1f, 0.14f, 1.1f), pallet);
            for (int i = -1; i <= 1; i++)
            {
                NoCollider(Block("PalletSlatGap", g, basePos + new Vector3(i * 0.7f, 0.141f, 0f), new Vector3(0.04f, 0.002f, 1.1f), GetOrCreateMaterial("Prop_Seam", new Color(0.05f, 0.04f, 0.06f), 0.2f)));
            }
            var bed = Block("Mattress", g, basePos + new Vector3(0f, 0.24f, 0f), new Vector3(1.95f, 0.2f, 0.95f), mattress);
            NoCollider(Block("MattressStain", g, basePos + new Vector3(0.35f, 0.342f, 0.1f), new Vector3(0.4f, 0.004f, 0.3f), stain));
            Part("Pillow", g, basePos + new Vector3(-0.72f, 0.4f, 0f), new Vector3(0.4f, 0.13f, 0.62f), mattress, PrimitiveType.Sphere);
            var cover = Part("Blanket", g, basePos + new Vector3(0.3f, 0.37f, 0.03f), new Vector3(1.2f, 0.08f, 1.0f), blanket, PrimitiveType.Cube);
            cover.transform.localRotation = Quaternion.Euler(0f, 8f, 3f);
            var fold = Part("BlanketFold", g, basePos + new Vector3(-0.25f, 0.42f, -0.2f), new Vector3(0.35f, 0.14f, 0.5f), blanket, PrimitiveType.Sphere);
            fold.transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
            _ = bed;
        }

        /// <summary>GDD §5.1 "one manual packaging surface": table with boxes, tape roll and a stack of flat-packs.</summary>
        private static void BuildPackagingTable(Transform parent, Vector3 basePos, Material frame, Material cardboard, Material tape, Material top)
        {
            var g = Group("PackagingTable", parent);
            Block("Tabletop", g, basePos + new Vector3(0f, 0.86f, 0f), new Vector3(1.6f, 0.06f, 0.8f), top);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Block("Leg", g, basePos + new Vector3(sx * 0.74f, 0.42f, sz * 0.34f), new Vector3(0.06f, 0.84f, 0.06f), frame);
                }
            }
            Block("Box_Open", g, basePos + new Vector3(-0.4f, 1.05f, 0f), new Vector3(0.4f, 0.32f, 0.35f), cardboard);
            NoCollider(Block("Box_Flap", g, basePos + new Vector3(-0.4f, 1.22f, 0.24f), new Vector3(0.4f, 0.02f, 0.18f), cardboard)).transform.localRotation = Quaternion.Euler(-40f, 0f, 0f);
            Block("Box_Taped", g, basePos + new Vector3(0.25f, 1.0f, -0.1f), new Vector3(0.32f, 0.22f, 0.3f), cardboard);
            NoCollider(Block("TapeStrip", g, basePos + new Vector3(0.25f, 1.112f, -0.1f), new Vector3(0.06f, 0.004f, 0.3f), tape));
            var roll = Part("TapeRoll", g, basePos + new Vector3(0.6f, 0.93f, 0.2f), new Vector3(0.12f, 0.03f, 0.12f), tape, PrimitiveType.Cylinder);
            Block("FlatPacks", g, basePos + new Vector3(0f, 0.08f, 0f), new Vector3(0.9f, 0.16f, 0.6f), cardboard);
            _ = roll;
        }

        private static void BuildCrateStack(Transform parent, Vector3 basePos, Material crate, Material band, Material cardboard)
        {
            var g = Group("CrateStack", parent);
            Crate(g, basePos + new Vector3(0f, 0.4f, 0f), 0.8f, 8f, crate, band);
            Crate(g, basePos + new Vector3(0.85f, 0.35f, 0.1f), 0.7f, -12f, crate, band);
            Crate(g, basePos + new Vector3(0.35f, 1.1f, 0.05f), 0.6f, 25f, crate, band);
            Block("LooseBox", g, basePos + new Vector3(-0.7f, 0.18f, 0.3f), new Vector3(0.4f, 0.36f, 0.4f), cardboard).transform.rotation = Quaternion.Euler(0f, 30f, 0f);
        }

        /// <summary>Metal crate with hazard bands round the middle and corners.</summary>
        private static void Crate(Transform parent, Vector3 center, float size, float yaw, Material body, Material band)
        {
            var root = new GameObject("Crate").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));
            Solid("Body", root, Vector3.zero, Vector3.one * size, body);
            var b1 = Part("Band", root, Vector3.zero, new Vector3(size * 1.02f, size * 0.14f, size * 1.02f), band);
            var b2 = Part("BandV", root, Vector3.zero, new Vector3(size * 0.14f, size * 1.02f, size * 1.02f), band);
            _ = b1;
            _ = b2;
        }

        /// <summary>Pipes, brackets and cable bundles along the hideout's south and west walls.</summary>
        private static void BuildWallRuns(Transform parent, Material pipe, Material bracket, Material cable, Material cableRed, Material hazard)
        {
            var g = Group("WallRuns", parent);

            // South wall (inner face z = -20): two pipes and a cable bundle
            PipeX(g, -19.5f, 19.5f, 3.3f, -19.75f, 0.12f, pipe, bracket);
            PipeX(g, -19.5f, 19.5f, 2.95f, -19.8f, 0.08f, pipe, bracket);
            PipeX(g, -19.5f, 19.5f, 2.6f, -19.86f, 0.035f, cable, null);
            PipeX(g, -19.5f, 19.5f, 2.55f, -19.86f, 0.03f, cableRed, null);
            // Valve wheel on the big pipe near the workshop
            var valve = Part("ValveWheel", g, new Vector3(2.8f, 3.3f, -19.55f), new Vector3(0.35f, 0.03f, 0.35f), hazard, PrimitiveType.Cylinder);
            valve.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // West wall (inner face x = -20): a vertical drop and a horizontal run
            PipeZ(g, -19.5f, 15f, 3.1f, -19.75f, 0.1f, pipe, bracket);
            var drop = Part("PipeDrop", g, new Vector3(-19.75f, 1.55f, -12f), new Vector3(0.2f, 1.55f, 0.2f), pipe, PrimitiveType.Cylinder);
            _ = drop;
        }

        private static void PipeX(Transform parent, float x0, float x1, float y, float z, float radius, Material mat, Material bracket)
        {
            var p = Part("PipeX", parent, new Vector3((x0 + x1) * 0.5f, y, z), new Vector3(radius * 2f, (x1 - x0) * 0.5f, radius * 2f), mat, PrimitiveType.Cylinder);
            p.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            if (bracket == null)
            {
                return;
            }
            for (float x = x0 + 1f; x < x1; x += 3f)
            {
                Part("Bracket", parent, new Vector3(x, y, z - radius * 0.2f), new Vector3(0.06f, radius * 2.6f, radius * 2.4f + 0.1f), bracket);
            }
        }

        private static void PipeZ(Transform parent, float z0, float z1, float y, float x, float radius, Material mat, Material bracket)
        {
            var p = Part("PipeZ", parent, new Vector3(x, y, (z0 + z1) * 0.5f), new Vector3(radius * 2f, (z1 - z0) * 0.5f, radius * 2f), mat, PrimitiveType.Cylinder);
            p.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            for (float z = z0 + 1f; z < z1; z += 3f)
            {
                Part("Bracket", parent, new Vector3(x - radius * 0.2f, y, z), new Vector3(radius * 2.4f + 0.1f, radius * 2.6f, 0.06f), bracket);
            }
        }
    }
}
