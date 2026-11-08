using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using VentsAndVending.Orders;
using VentsAndVending.World;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Central Plaza refresh to match the Plaza Network brief (Docs/Design/TunnelNetwork.md §4B, §9): clean pale panels
    /// and glossy floor under a lit ceiling (with the vent grilles you live behind), cyan/violet/orange ad light,
    /// planters and street furniture, muffled crowd + music bleed + PA chimes — and the three VentNet meet spots.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        private static readonly List<MeetSpot> _meetSpots = new List<MeetSpot>();

        private static Material PlazaWallClean => GetOrCreateMaterial("Plaza_WallClean", new Color(0.74f, 0.77f, 0.84f), 0.6f);

        private static void BuildPlazaRefresh(Transform plaza)
        {
            var g = Group("Refresh", plaza);
            BuildPlazaCeiling(g);
            BuildPlazaAds();
            BuildPlazaFurniture(g);
            BuildPlazaAudio(g);
            BuildMeetSpots();
        }

        // ------------------------------------------------------------------ Ceiling

        private static void BuildPlazaCeiling(Transform g)
        {
            Material ceiling = GetOrCreateMaterial("Plaza_Ceiling", new Color(0.5f, 0.53f, 0.6f), 0.4f);
            Material panel = GetOrCreateMaterial("Plaza_LightPanel", new Color(0.85f, 0.95f, 1f), 0.5f, new Color(1.2f, 1.45f, 1.6f));
            Material grille = ArtLibrary.Get("M_DarkMetal");
            Box("Ceiling", g, new Vector3(-PlazaHalfWidth - 0.5f, PlazaWallHeight, PlazaSouthZ - 0.5f), new Vector3(PlazaHalfWidth + 0.5f, PlazaWallHeight + 0.3f, PlazaFarZ + 0.5f), ceiling);
            foreach (float x in new[] { -9f, 0f, 9f })
            {
                foreach (float z in new[] { 26.5f, 35.5f, 44.5f })
                {
                    NoCollider(Block("LightPanel", g, new Vector3(x, PlazaWallHeight - 0.02f, z), new Vector3(3.2f, 0.04f, 1.1f), panel));
                }
            }
            // The vents you live in, seen from the public side
            foreach (Vector3 p in new[] { new Vector3(-12.5f, 0f, 40f), new Vector3(12.5f, 0f, 27f), new Vector3(4.5f, 0f, 22.5f) })
            {
                NoCollider(Block("CeilingVent", g, new Vector3(p.x, PlazaWallHeight - 0.03f, p.z), new Vector3(1.3f, 0.04f, 0.8f), grille));
                for (int i = 0; i < 5; i++)
                {
                    NoCollider(Block("VentSlat", g, new Vector3(p.x, PlazaWallHeight - 0.06f, p.z - 0.32f + i * 0.16f), new Vector3(1.2f, 0.03f, 0.04f), PlazaWallClean));
                }
            }
            CreatePointLight("Plaza Ceiling Fill", new Vector3(0f, 5.6f, 40f), new Color(0.92f, 0.96f, 1f), 26f, 5f, g);
            CreatePointLight("Plaza Ceiling Fill South", new Vector3(0f, 5.6f, 25f), new Color(0.92f, 0.96f, 1f), 20f, 3.8f, g);
        }

        // ------------------------------------------------------------------ Ads (GDD §15.7 holographic adverts)

        private static void BuildPlazaAds()
        {
            var root = new GameObject("Plaza Ads").transform; // outside Level: text never touches the NavMesh bake
            Ad(root, "Ad_Spa", new Vector3(-PlazaHalfWidth + 0.04f, 3.5f, 28.5f), -90f, new Color(1f, 0.25f, 0.85f),
                "ZENTHARI SPA\n<size=55%>you deserve more tentacles</size>");
            Ad(root, "Ad_Noodles", new Vector3(-PlazaHalfWidth + 0.04f, 3.5f, 43.5f), -90f, new Color(1f, 0.6f, 0.15f),
                "NEBULA NOODLES\n<size=55%>now with noodles!</size>");
            Ad(root, "Ad_GlorpMart", new Vector3(-8.5f, 4.1f, PlazaFarZ - 0.04f), 0f, new Color(0.2f, 0.9f, 1f),
                "GLORP-MART\n<size=55%>everything must glorp</size>");
            Ad(root, "Ad_Transit", new Vector3(8.5f, 4.1f, PlazaFarZ - 0.04f), 0f, new Color(0.65f, 0.45f, 1f),
                "CITADEL-9 TRANSIT\n<size=55%>delays: yes</size>");
            Ad(root, "Ad_Federation", new Vector3(PlazaHalfWidth - 0.04f, 3.6f, 42f), 90f, new Color(1f, 0.85f, 0.3f),
                "SEE SOMETHING?\n<size=55%>SAY SOMETHING.\nespecially about vents</size>");
        }

        /// <summary>Glowing ad screen on a wall. Yaw follows CreateSign (the text reads from the room side).</summary>
        private static void Ad(Transform root, string name, Vector3 pos, float yaw, Color colour, string text)
        {
            Material screen = GetOrCreateMaterial($"Ad_{name}", colour * 0.15f, 0.7f, colour * 0.9f);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name + "_Screen";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = new Vector3(3.2f, 1.5f, 0.05f);
            go.GetComponent<MeshRenderer>().sharedMaterial = screen;
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Vector3 front = Quaternion.Euler(0f, yaw, 0f) * Vector3.back;
            CreateSign($"{name}_Text", text, pos + front * 0.035f, yaw, 1.1f, Color.Lerp(colour, Color.white, 0.6f), 3f);
            var light = new GameObject($"{name}_Spill").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.position = pos + front * 1.2f;
            light.type = LightType.Point;
            light.color = colour;
            light.range = 6f;
            light.intensity = 1.6f;
        }

        // ------------------------------------------------------------------ Street furniture

        private static void BuildPlazaFurniture(Transform g)
        {
            Material silver = ArtLibrary.Get("M_BrushedSilver");
            Material soil = GetOrCreateMaterial("Plaza_Soil", new Color(0.18f, 0.12f, 0.1f), 0.1f);
            Color[] flora = { new Color(0.3f, 0.95f, 0.75f), new Color(0.95f, 0.4f, 0.9f), new Color(0.55f, 0.9f, 0.3f), new Color(1f, 0.7f, 0.25f) };

            int k = 0;
            foreach (Vector3 p in new[] { new Vector3(-6.6f, 0f, 29.5f), new Vector3(6.6f, 0f, 29.5f), new Vector3(-6f, 0f, 46.2f), new Vector3(13.6f, 0f, 44f) })
            {
                Block("Planter", g, p + new Vector3(0f, 0.3f, 0f), new Vector3(1.6f, 0.6f, 0.7f), silver);
                NoCollider(Block("PlanterSoil", g, p + new Vector3(0f, 0.6f, 0f), new Vector3(1.45f, 0.02f, 0.55f), soil));
                for (int i = 0; i < 3; i++)
                {
                    Color c = flora[(k + i) % flora.Length];
                    Material plant = GetOrCreateMaterial($"Plaza_Plant_{(k + i) % flora.Length}", c * 0.7f, 0.5f, c * 0.35f);
                    float h = 0.5f + ((k * 3 + i) % 3) * 0.25f;
                    var stalk = Part("PlantStalk", g, p + new Vector3(-0.45f + i * 0.45f, 0.6f + h * 0.5f, 0f), new Vector3(0.06f, h * 0.5f, 0.06f), plant, PrimitiveType.Cylinder);
                    stalk.transform.rotation = Quaternion.Euler(0f, 0f, (i - 1) * 9f);
                    Part("PlantBulb", g, p + new Vector3(-0.45f + i * 0.45f + (i - 1) * 0.08f, 0.62f + h, 0f), Vector3.one * (0.2f + 0.06f * i), plant, PrimitiveType.Sphere);
                }
                k++;
            }

            // Vending machine by the staff door (the joke writes itself)
            Material vendBody = GetOrCreateMaterial("Plaza_VendingBody", new Color(0.85f, 0.2f, 0.3f), 0.5f);
            Material vendGlass = GetOrCreateMaterial("Plaza_VendingGlass", new Color(0.1f, 0.2f, 0.25f), 0.9f, new Color(0.4f, 0.9f, 1f) * 0.6f);
            Block("VendingMachine", g, new Vector3(15.4f, 1f, 29f), new Vector3(0.9f, 2f, 1f), vendBody);
            NoCollider(Block("VendingGlass", g, new Vector3(14.94f, 1.15f, 29f), new Vector3(0.02f, 1.3f, 0.75f), vendGlass));
            CreateSign("Vending_Label", "<b>TOTALLY LEGAL</b>\n<size=70%>VENDING</size>", new Vector3(14.92f, 1.95f, 29f), 90f, 0.45f, Color.white, 0.8f);

            // Info kiosk near the customer door
            Material kioskScreen = GetOrCreateMaterial("Plaza_KioskScreen", new Color(0.05f, 0.1f, 0.2f), 0.7f, new Color(0.3f, 0.7f, 1f) * 0.7f);
            Block("Kiosk", g, new Vector3(-3.4f, 0.6f, 48.4f), new Vector3(0.6f, 1.2f, 0.45f), silver);
            NoCollider(Block("KioskScreen", g, new Vector3(-3.4f, 1.0f, 48.17f), new Vector3(0.45f, 0.35f, 0.02f), kioskScreen));
            CreateSign("Kiosk_Text", "YOU ARE\nHERE\n<size=60%>(probably)</size>", new Vector3(-3.4f, 1.0f, 48.155f), 0f, 0.25f, Color.white, 0.45f);
        }

        // ------------------------------------------------------------------ Sound: crowd, music bleed, PA

        private static void BuildPlazaAudio(Transform g)
        {
            Loop(g, "Plaza_Crowd", new Vector3(0f, 2f, 38f), ProceduralAudio.PlazaCrowd, 0.5f, 7f, 36f, 1f).spread = 180f;
            Loop(g, "Plaza_CrowdSouth", new Vector3(-3f, 2f, 26f), ProceduralAudio.PlazaCrowd, 0.35f, 5f, 26f, 0.94f).spread = 180f;
            Loop(g, "Plaza_MusicBleed", new Vector3(13.2f, 1.6f, 36f), ProceduralAudio.PlazaMusic, 0.32f, 2f, 20f, 1f);
            Loop(g, "Lounge_Music", new Vector3(22f, 2f, 40f), ProceduralAudio.PlazaMusic, 0.35f, 3f, 14f, 0.85f);
            OneShots(g, "Plaza_PA", new Vector3(0f, 5.2f, 36f), new Vector3(1f, 0.1f, 1f), new[] { ProceduralAudio.PaChime },
                new Vector2(45f, 90f), new Vector2(0.45f, 0.6f), 45f);
        }

        // ------------------------------------------------------------------ VentNet meet spots

        private static void BuildMeetSpots()
        {
            _meetSpots.Clear();
            var root = new GameObject("Meet Spots").transform;
            Material mark = GetOrCreateMaterial("Meet_Mark", new Color(0.03f, 0.08f, 0.1f), 0.6f, new Color(0.2f, 0.95f, 1f) * 0.25f);
            Material beam = GetLightBladeMaterial(new Color(0.55f, 0.75f, 1f));

            _meetSpots.Add(MeetSpotAt(root, "A", "Behind Glorp-Mart", MeetRisk.Low, new Vector3(-15.05f, 0f, 36f),
                "Tucked behind the stall's back panel — out of the drone's patrol sightlines.", new Vector3(-PlazaHalfWidth + 0.03f, 1.5f, 36f), -90f, mark, beam));
            _meetSpots.Add(MeetSpotAt(root, "B", "Under the Hologram", MeetRisk.High, new Vector3(2.2f, 0f, 33.2f),
                "Dead centre, in front of everyone. The patrol drone passes right overhead — be quick.", null, 0f, mark, beam));
            _meetSpots.Add(MeetSpotAt(root, "C", "Quiet Corner", MeetRisk.Medium, new Vector3(-12.6f, 0f, 23.2f),
                "Right by your vent. A pillar hides you from part of the patrol loop.", new Vector3(-12.6f, 1.5f, PlazaSouthZ + 0.02f), 180f, mark, beam));
        }

        private static MeetSpot MeetSpotAt(Transform root, string id, string name, MeetRisk risk, Vector3 pos, string hint,
            Vector3? wallTag, float tagYaw, Material markMat, Material beamMat)
        {
            var go = new GameObject($"Meet Spot {id}");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var marks = new List<Renderer>();

            // Floor ring + painted letter (hand-sprayed, a little crooked)
            var ring = Part("Ring", go.transform, new Vector3(0f, 0.006f, 0f), new Vector3(1.3f, 0.004f, 1.3f), markMat, PrimitiveType.Cylinder);
            ring.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            marks.Add(ring.GetComponent<Renderer>());
            var letter = new GameObject("Letter");
            letter.transform.SetParent(go.transform, false);
            letter.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            letter.transform.localRotation = Quaternion.Euler(90f, Hash(pos) % 25 - 12, 0f);
            var tmp = letter.AddComponent<TextMeshPro>();
            tmp.text = $"<b>{id}</b>";
            tmp.fontSize = 6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.55f, 1f, 1f, 0.85f);
            tmp.rectTransform.sizeDelta = new Vector2(1f, 1f);

            if (wallTag.HasValue)
            {
                CreateSign($"MeetTag_{id}", $"<b>{id}</b>", wallTag.Value, tagYaw, 3f, new Color(0.55f, 1f, 1f, 0.8f), 1f, Hash(pos) % 16 - 8);
            }

            // Beacon (only when a deal is booked): crossed light blades and a soft glow
            var beacon = new GameObject("Beacon");
            beacon.transform.SetParent(go.transform, false);
            for (int i = 0; i < 2; i++)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Beam";
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.transform.SetParent(beacon.transform, false);
                quad.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                quad.transform.localRotation = Quaternion.Euler(180f, i * 90f, 0f); // flipped: the fade texture now brightens toward the floor
                quad.transform.localScale = new Vector3(0.9f, 3.2f, 1f);
                var r = quad.GetComponent<MeshRenderer>();
                r.sharedMaterial = beamMat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            beacon.SetActive(false);
            var glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            glow.type = LightType.Point;
            glow.color = new Color(0.3f, 0.95f, 1f);
            glow.range = 3.5f;
            glow.intensity = 1.5f;
            glow.enabled = false;

            var spot = go.AddComponent<MeetSpot>();
            spot.Configure(id, name, risk, hint, marks.ToArray(), beacon, glow);
            return spot;
        }
    }
}
