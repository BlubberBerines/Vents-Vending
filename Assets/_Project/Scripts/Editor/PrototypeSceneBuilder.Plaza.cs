using TMPro;
using UnityEditor;
using UnityEngine;
using VentsAndVending.Security;
using VentsAndVending.World;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// The Central Plaza (GDD §2.2, §19.1 Location 2): a brighter, cleaner public space north of the hideout,
    /// reachable through a vent duct. Customers trade here under the eye of a patrol drone (GDD Part 11).
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        private const float PlazaVentX = -8f;
        private const float PlazaSouthZ = 20.5f;
        private const float PlazaFarZ = 50.5f;
        private const float PlazaHalfWidth = 16f;
        private const float PlazaWallHeight = 6f;

        private static Transform BuildPlaza(Transform level, Material hideoutWall, Material accent, Material vent, Material glow)
        {
            var floorMat = GetOrCreateMaterial("Plaza_Floor", new Color(0.22f, 0.23f, 0.28f), 0.65f);
            var wallMat = GetOrCreateMaterial("Plaza_Wall", new Color(0.17f, 0.14f, 0.28f), 0.45f);
            var cyan = GetOrCreateMaterial("Plaza_NeonCyan", new Color(0.02f, 0.2f, 0.25f), 0.6f, new Color(0.2f, 1.6f, 2f));
            var magenta = GetOrCreateMaterial("Plaza_NeonMagenta", new Color(0.25f, 0.02f, 0.2f), 0.6f, new Color(2f, 0.2f, 1.5f));
            var orange = GetOrCreateMaterial("Plaza_NeonOrange", new Color(0.3f, 0.12f, 0.02f), 0.6f, new Color(2f, 0.8f, 0.15f));
            var silver = GetOrCreateMaterial("Plaza_Silver", new Color(0.55f, 0.57f, 0.62f), 0.85f);

            var plaza = Group("Plaza", level);
            float midZ = (PlazaSouthZ + PlazaFarZ) * 0.5f;
            float length = PlazaFarZ - PlazaSouthZ;

            // Shell
            var shell = Group("Shell", plaza);
            Block("Floor", shell, new Vector3(0f, -0.5f, midZ - 0.25f), new Vector3(PlazaHalfWidth * 2f, 1f, length + 0.5f), floorMat);
            Block("Wall_E", shell, new Vector3(PlazaHalfWidth + 0.25f, PlazaWallHeight * 0.5f, midZ), new Vector3(0.5f, PlazaWallHeight, length + 1f), wallMat);
            Block("Wall_W", shell, new Vector3(-PlazaHalfWidth - 0.25f, PlazaWallHeight * 0.5f, midZ), new Vector3(0.5f, PlazaWallHeight, length + 1f), wallMat);
            Block("Wall_N", shell, new Vector3(0f, PlazaWallHeight * 0.5f, PlazaFarZ + 0.25f), new Vector3(PlazaHalfWidth * 2f + 1f, PlazaWallHeight, 0.5f), wallMat);
            Block("Wall_S_Upper", shell, new Vector3(0f, 5f, 20.25f), new Vector3(PlazaHalfWidth * 2f + 1f, 2f, 0.5f), wallMat);

            // Neon trim
            var trim = Group("NeonTrim", plaza);
            NoCollider(Block("Trim_E", trim, new Vector3(PlazaHalfWidth - 0.03f, 5.3f, midZ), new Vector3(0.06f, 0.08f, length), cyan));
            NoCollider(Block("Trim_W", trim, new Vector3(-PlazaHalfWidth + 0.03f, 5.3f, midZ), new Vector3(0.06f, 0.08f, length), cyan));
            NoCollider(Block("Trim_N", trim, new Vector3(0f, 5.3f, PlazaFarZ - 0.03f), new Vector3(PlazaHalfWidth * 2f, 0.08f, 0.06f), magenta));
            NoCollider(Block("Trim_S", trim, new Vector3(0f, 5.3f, PlazaSouthZ + 0.03f), new Vector3(PlazaHalfWidth * 2f, 0.08f, 0.06f), magenta));

            // Pillars (line-of-sight blockers)
            var pillars = Group("Pillars", plaza);
            foreach (var p in new[] { new Vector2(-10f, 29f), new Vector2(10f, 29f), new Vector2(-10.5f, 47f), new Vector2(10.5f, 47f) })
            {
                Block($"Pillar_{p.x}_{p.y}", pillars, new Vector3(p.x, PlazaWallHeight * 0.5f, p.y), new Vector3(1f, PlazaWallHeight, 1f), wallMat);
                NoCollider(Block("Band", pillars, new Vector3(p.x, 2.6f, p.y), new Vector3(1.04f, 0.1f, 1.04f), p.x < 0 ? cyan : magenta));
            }

            // Market stalls — cover from the drone (GDD §11.4 "hide", "break line of sight")
            var stalls = Group("Stalls", plaza);
            BuildStall(stalls, "Stall_GlorpMart", new Vector3(-13.5f, 0f, 36f), 90f, magenta, wallMat, silver, "GLORP-MART");
            BuildStall(stalls, "Stall_Noodles", new Vector3(13.5f, 0f, 36f), -90f, orange, wallMat, silver, "NEBULA NOODLES");
            BuildStall(stalls, "Stall_Legal", new Vector3(6f, 0f, 48.6f), 180f, cyan, wallMat, silver, "TOTALLY LEGAL GOODS");

            // Benches
            Block("Bench_W", plaza, new Vector3(-3.5f, 0.25f, 29.5f), new Vector3(2.2f, 0.5f, 0.6f), silver);
            Block("Bench_E", plaza, new Vector3(3.5f, 0.25f, 29.5f), new Vector3(2.2f, 0.5f, 0.6f), silver);

            // Holographic advert (GDD §15.7)
            var holo = Group("Hologram", plaza);
            Block("Pedestal", holo, new Vector3(0f, 0.3f, 35f), new Vector3(1.4f, 0.6f, 1.4f), silver);
            var ring = Part("HoloRing", holo, new Vector3(0f, 2.3f, 35f), new Vector3(1.8f, 0.03f, 1.8f), cyan, PrimitiveType.Cylinder);
            ring.AddComponent<Spinner>().Configure(new Vector3(0f, 40f, 20f), 0f, 1f);
            var orb = Part("HoloOrb", holo, new Vector3(0f, 2.3f, 35f), Vector3.one * 0.55f, magenta, PrimitiveType.Sphere);
            orb.AddComponent<Spinner>().Configure(new Vector3(0f, 90f, 0f), 0.15f, 1.5f);
            // Two-sided advert: a thin dark backing panel separates the front and back text so they never overlap.
            NoCollider(Block("Holo_AdvertBacking", holo, new Vector3(0f, 3.4f, 35f), new Vector3(3.2f, 0.95f, 0.04f),
                GetOrCreateMaterial("Holo_Backing", new Color(0.02f, 0.03f, 0.05f), 0.3f, new Color(0.02f, 0.08f, 0.1f))));
            CreateSign("Holo_Advert_South", "GLARB-JUICE\n<size=55%>Now 40% less sentient!</size>", new Vector3(0f, 3.4f, 34.96f), 0f, 1.4f, new Color(0.5f, 1f, 1f), 3.2f);
            CreateSign("Holo_Advert_North", "GLARB-JUICE\n<size=55%>Now 40% less sentient!</size>", new Vector3(0f, 3.4f, 35.04f), 180f, 1.4f, new Color(0.5f, 1f, 1f), 3.2f);

            // Federation notice (GDD §2.3 humour)
            CreateSign("Federation_Notice",
                "<color=#FFD24D>GALACTIC FEDERATION NOTICE</color>\nLoitering, venting and unlicensed vending\nare prohibited under Ordinance 84-B.\n<size=70%>Patrol drones are watching. Probably.</size>",
                new Vector3(PlazaHalfWidth - 0.05f, 3.2f, 33f), 90f, 0.9f, Color.white, 5f);

            // Lighting — brighter and more colourful than the maintenance areas (GDD §15.4)
            CreatePointLight("Plaza Cyan", new Vector3(-8f, 4.6f, 29f), new Color(0.2f, 0.9f, 1f), 16f, 5f, plaza);
            CreatePointLight("Plaza Magenta", new Vector3(8f, 4.6f, 29f), new Color(1f, 0.25f, 0.85f), 16f, 5f, plaza);
            CreatePointLight("Plaza Violet", new Vector3(-8f, 4.6f, 44f), new Color(0.6f, 0.35f, 1f), 16f, 5f, plaza);
            CreatePointLight("Plaza Amber", new Vector3(8f, 4.6f, 44f), new Color(1f, 0.6f, 0.2f), 16f, 5f, plaza);
            CreatePointLight("Plaza Fill", new Vector3(0f, 5.5f, 37f), new Color(0.85f, 0.88f, 1f), 22f, 3.5f, plaza);

            BuildPlazaVent(level, vent, accent, glow);
            return plaza;
        }

        private static void BuildStall(Transform parent, string name, Vector3 position, float yaw, Material awning, Material body, Material counter, string label)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            Solid("Counter", root, new Vector3(0f, 0.5f, 0.2f), new Vector3(2.6f, 1f, 0.8f), counter);
            Solid("BackPanel", root, new Vector3(0f, 1.2f, -0.55f), new Vector3(2.6f, 2.4f, 0.12f), body);
            Solid("Post_L", root, new Vector3(-1.3f, 1.2f, 0.6f), new Vector3(0.08f, 2.4f, 0.08f), counter);
            Solid("Post_R", root, new Vector3(1.3f, 1.2f, 0.6f), new Vector3(0.08f, 2.4f, 0.08f), counter);
            Solid("Awning", root, new Vector3(0f, 2.45f, 0.15f), new Vector3(3f, 0.08f, 1.7f), awning);

            // Goods on the counter: a crate and a row of glowing product jars in the stall's neon colour
            Part("Goods_Crate", root, new Vector3(-0.8f, 1.14f, 0.25f), new Vector3(0.36f, 0.28f, 0.32f), body);
            for (int j = 0; j < 4; j++)
            {
                Part($"Goods_Jar_{j}", root, new Vector3(-0.3f + j * 0.22f, 1.09f, 0.3f), new Vector3(0.12f, 0.09f, 0.12f), awning, PrimitiveType.Cylinder);
            }
            Part("Goods_PriceTag", root, new Vector3(0.75f, 1.08f, 0.45f), new Vector3(0.2f, 0.16f, 0.02f), counter);

            Vector3 signPos = root.TransformPoint(new Vector3(0f, 2.75f, 0.95f));
            CreateSign($"{name}_Sign", label, signPos, yaw + 180f, 1.2f, Color.white, 3f);
        }

        /// <summary>Straight duct from the hideout (z 15.5) through the shared wall into the plaza (z 25), hatch at each end.</summary>
        private static void BuildPlazaVent(Transform level, Material vent, Material accent, Material glow)
        {
            var g = Group("PlazaVent", level);
            const float w = 0.9f, h = 0.85f, t = 0.1f, half = w * 0.5f;
            const float z0 = 15.5f, z1 = 25f;
            float x = PlazaVentX, len = z1 - z0, zc = (z0 + z1) * 0.5f;

            Block("Wall_L", g, new Vector3(x - half - t * 0.5f, h * 0.5f, zc), new Vector3(t, h, len), vent);
            Block("Wall_R", g, new Vector3(x + half + t * 0.5f, h * 0.5f, zc), new Vector3(t, h, len), vent);
            Block("Roof", g, new Vector3(x, h + t * 0.5f, zc), new Vector3(w + t * 2f, t, len), vent);

            for (int i = 0; i < 3; i++)
            {
                NoCollider(Block($"LightStrip_{i}", g, new Vector3(x, h - 0.01f, z0 + 1.5f + i * 3.2f), new Vector3(0.3f, 0.02f, 0.6f), glow));
            }
            CreatePointLight("Plaza Vent Light", new Vector3(x, 0.7f, zc), new Color(1f, 0.7f, 0.35f), 3f, 1.2f, g);

            const float lip = 0.6f;
            VentTrigger("VentZone_PlazaDuct", g, new Vector3(x, h * 0.5f, zc), new Vector3(w, h, len + lip * 2f), "duct_plaza");

            CreateHatch("Hideout Hatch", g, new Vector3(x, h, z0 - 0.04f), new Vector3(100f, 0f, 0f), vent, accent);
            CreateHatch("Plaza Grate", g, new Vector3(x, h, z1 + 0.04f), new Vector3(-100f, 0f, 0f), vent, accent);
            NoCollider(Block("Hatch_Frame_Hideout", g, new Vector3(x, h + t + 0.05f, z0 + 0.05f), new Vector3(w + t * 2f + 0.2f, 0.1f, 0.1f), accent));
            NoCollider(Block("Hatch_Frame_Plaza", g, new Vector3(x, h + t + 0.05f, z1 - 0.05f), new Vector3(w + t * 2f + 0.2f, 0.1f, 0.1f), accent));

            CreateSign("Vent_Sign_Hideout", "TO PLAZA →", new Vector3(x, 1.25f, z0 - 0.06f), 0f, 0.7f, new Color(1f, 0.85f, 0.4f), 1.6f, -3f);
        }

        private static GameObject CreateHatch(string name, Transform parent, Vector3 hingePosition, Vector3 openRotation, Material panelMat, Material grilleMat)
        {
            var hatch = new GameObject(name);
            hatch.transform.SetParent(parent, false);
            hatch.transform.position = hingePosition;
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(hatch.transform, false);
            var panel = Block("Panel", pivot, Vector3.zero, new Vector3(0.94f, 0.85f, 0.04f), panelMat);
            panel.isStatic = false;
            panel.transform.localPosition = new Vector3(0f, -0.425f, 0f);
            var grille = Part("Grille", pivot, new Vector3(0f, -0.45f, 0f), new Vector3(0.6f, 0.3f, 0.06f), grilleMat);
            grille.isStatic = false;

            var ventHatch = hatch.AddComponent<VentHatch>();
            var so = new SerializedObject(ventHatch);
            so.FindProperty("_pivot").objectReferenceValue = pivot;
            so.FindProperty("_openRotation").vector3Value = openRotation;
            so.ApplyModifiedPropertiesWithoutUndo();
            return hatch;
        }

        // ------------------------------------------------------------------ Security

        private static void BuildSecurity()
        {
            new GameObject("Security").AddComponent<SecurityManager>();

            var bodyMat = GetOrCreateMaterial("Drone_Body", new Color(0.12f, 0.13f, 0.16f), 0.8f);
            var trimMat = GetOrCreateMaterial("Drone_Trim", new Color(0.8f, 0.82f, 0.86f), 0.9f);
            var eyeMat = GetOrCreateMaterial("Drone_Eye", new Color(0.1f, 0.5f, 0.6f), 0.9f, new Color(0.2f, 1.5f, 2f));

            var patrol = new Transform[4];
            Vector2[] corners = { new Vector2(-10f, 32f), new Vector2(10f, 32f), new Vector2(10f, 46f), new Vector2(-10f, 46f) };
            var patrolRoot = new GameObject("Drone Patrol Route").transform;
            for (int i = 0; i < corners.Length; i++)
            {
                var p = new GameObject($"Patrol {i + 1}").transform;
                p.SetParent(patrolRoot, false);
                p.position = new Vector3(corners[i].x, 2.7f, corners[i].y);
                patrol[i] = p;
            }

            var root = new GameObject("Security Drone");
            root.transform.position = new Vector3(corners[0].x, 2.7f, corners[0].y);
            root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var hover = Group("Hover", root.transform);

            Part("Body", hover, Vector3.zero, new Vector3(0.6f, 0.45f, 0.6f), bodyMat, PrimitiveType.Sphere);
            Part("Ring", hover, Vector3.zero, new Vector3(0.95f, 0.03f, 0.95f), trimMat, PrimitiveType.Cylinder);
            Part("Antenna", hover, new Vector3(0f, 0.32f, -0.1f), new Vector3(0.015f, 0.2f, 0.015f), trimMat);
            Part("FederationBadge", hover, new Vector3(0f, 0.12f, -0.29f), new Vector3(0.14f, 0.1f, 0.02f), GetOrCreateMaterial("Char_Badge", new Color(0.9f, 0.75f, 0.2f), 0.7f, new Color(0.6f, 0.45f, 0.05f)));

            var rotors = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var armPos = new Vector3(Mathf.Cos(a) * 0.45f, 0.05f, Mathf.Sin(a) * 0.45f);
                Part($"Arm_{i}", hover, armPos * 0.6f, new Vector3(0.04f, 0.03f, 0.04f), trimMat);
                rotors[i] = Part($"Rotor_{i}", hover, armPos, new Vector3(0.3f, 0.008f, 0.06f), trimMat).transform;
            }

            var eye = Part("Eye", hover, new Vector3(0f, -0.13f, 0.24f), Vector3.one * 0.2f, eyeMat, PrimitiveType.Sphere);

            // Character details: a lens ring, a permanently disapproving V-brow and a Federation stripe
            var badgeMat = GetOrCreateMaterial("Char_Badge", new Color(0.95f, 0.78f, 0.2f), 0.7f, new Color(0.4f, 0.3f, 0.03f));
            Part("LensRing", hover, new Vector3(0f, -0.13f, 0.25f), new Vector3(0.25f, 0.015f, 0.25f), trimMat, PrimitiveType.Cylinder)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part("Brow_L", hover, new Vector3(-0.075f, -0.035f, 0.285f), new Vector3(0.14f, 0.035f, 0.05f), bodyMat).transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            Part("Brow_R", hover, new Vector3(0.075f, -0.035f, 0.285f), new Vector3(0.14f, 0.035f, 0.05f), bodyMat).transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            Part("FederationStripe", hover, new Vector3(0f, 0.04f, 0f), new Vector3(0.615f, 0.05f, 0.615f), badgeMat, PrimitiveType.Sphere);
            var spotGo = new GameObject("Spotlight");
            spotGo.transform.SetParent(eye.transform, false);
            spotGo.transform.localRotation = Quaternion.Euler(32f, 0f, 0f);
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 16f;
            spot.spotAngle = 75f;
            spot.innerSpotAngle = 35f;
            spot.intensity = 5f;
            spot.color = new Color(0.15f, 0.85f, 1f);
            spot.shadows = LightShadows.None;

            var drone = root.AddComponent<SecurityDrone>();
            drone.SetPatrol(patrol);
            var so = new SerializedObject(drone);
            so.FindProperty("_eye").objectReferenceValue = eye.transform;
            so.FindProperty("_eyeRenderer").objectReferenceValue = eye.GetComponent<Renderer>();
            so.FindProperty("_spotlight").objectReferenceValue = spot;
            so.FindProperty("_hoverRoot").objectReferenceValue = hover;
            SetList(so.FindProperty("_rotors"), rotors);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ Helpers

        /// <summary>World-space text, kept outside "Level" so the NavMesh bake never scans its mesh. TMP faces -Z: yaw 0 reads when looking +Z.</summary>
        private static void CreateSign(string name, string text, Vector3 position, float yaw, float fontSize, Color color, float width, float roll = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, roll));
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.rectTransform.sizeDelta = new Vector2(width, 1.5f);
        }

        /// <summary>A primitive that keeps its collider (cover, LOS blocking), placed in local space.</summary>
        private static GameObject Solid(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        private static GameObject NoCollider(GameObject go)
        {
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
    }
}
