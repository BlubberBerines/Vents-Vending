using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VentsAndVending.Inventory;
using VentsAndVending.Security;
using VentsAndVending.Tunnels;
using VentsAndVending.UI;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// The Maintenance Underpass (Docs/superpowers/specs/2026-11-08-maintenance-underpass-design.md): a crowbar grate in
    /// the Long Crawl drops you into a patrolled sublevel that leads, upright, to an inside-only exit into the plaza.
    ///
    /// Layout (floor y −3.6, ceiling −0.6, corridors 3 m wide):
    ///   L1  x[−7.5, 2]   z[−1, 2]      landing under the grate at (0.7, 0.5)
    ///   L2  x[−7.5, −4.5] z[2, 10.5]
    ///   L3  x[−7.5, 4.5] z[10.5, 13.5]
    ///   Stair x[1.5, 4.5] z[13.5, 19.2] ramps up to y 0; landing z[19.2, 20]; exit door in the plaza wall at x 3.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        private const float UpFloor = -3.6f;
        private const float UpCeil = -0.6f;
        private const string UnderpassVolumePath = "Assets/_Project/Settings/Rendering/UnderpassVolumeProfile.asset";

        /// <summary>The grate hole in gallery D1 (x, z extents). The D1 floor is built around it.</summary>
        internal static readonly Rect GrateHole = new Rect(0.3f, 0.1f, 0.8f, 0.8f);

        private static Transform _underpassExit;

        // ------------------------------------------------------------------ Geometry

        private static void BuildUnderpass(Transform level, Material floor)
        {
            var root = Group("Underpass", level);
            Material wall = Worn(ArtLibrary.Get("M_PaintedMetal_Wall"), 0.45f, 0.35f, 0.4f);
            Material lower = Worn(ArtLibrary.Get("M_GreenPanel"), 0.5f, 0.4f, 0.5f);
            Material ceil = ArtLibrary.Get("M_DarkMetal");
            Material hazard = ArtLibrary.Get("M_HazardPaint");
            Material duct = ArtLibrary.Get("M_DuctMetal");
            Material pipeRed = GetOrCreateMaterial("Up_PipeRed", new Color(0.55f, 0.13f, 0.1f), 0.45f);
            Material pipeBlue = GetOrCreateMaterial("Up_PipeBlue", new Color(0.14f, 0.3f, 0.5f), 0.45f);
            Material pipeYellow = GetOrCreateMaterial("Up_PipeYellow", new Color(0.8f, 0.62f, 0.12f), 0.45f);
            Material puddle = GetOrCreateMaterial("Up_Puddle", new Color(0.2f, 0.26f, 0.3f), 0.98f);

            BuildCorridors(Group("Corridors", root), floor, wall, lower, ceil, hazard);
            BuildStairExit(Group("StairExit", root), floor, wall, ceil, hazard);
            BuildSideRooms(Group("SideRooms", root), floor, wall, lower, ceil, duct);
            BuildGrate(root);

            // Service runs: colour-coded pipes and a cable tray under the ceiling
            var runs = Group("Runs", root);
            PipeX(runs, -7.5f, 2f, -0.9f, 1.72f, 0.09f, pipeRed, ceil);
            PipeX(runs, -7.5f, 2f, -0.95f, -0.72f, 0.07f, pipeBlue, ceil);
            PipeZ(runs, -1f, 13.5f, -0.9f, -7.22f, 0.09f, pipeRed, ceil);
            PipeZ(runs, 2f, 10.5f, -1.05f, -4.78f, 0.06f, pipeYellow, ceil);
            PipeX(runs, -7.5f, 4.5f, -0.9f, 13.22f, 0.09f, pipeRed, ceil);
            PipeX(runs, -4.5f, 4.5f, -1.0f, 10.78f, 0.07f, pipeBlue, ceil);
            NoCollider(Box("CableTray_L2", runs, new Vector3(-5.6f, -0.78f, -0.9f), new Vector3(-5.2f, -0.74f, 13.4f), ceil));

            // Floor dressing: hazard edge lines and puddles under the drips
            foreach (var (a, b) in new[]
            {
                (new Vector3(-7.4f, UpFloor, -0.9f), new Vector3(1.9f, UpFloor + 0.005f, -0.8f)),
                (new Vector3(-4.6f, UpFloor, 1.8f), new Vector3(1.9f, UpFloor + 0.005f, 1.9f)),
                (new Vector3(-7.4f, UpFloor, -0.9f), new Vector3(-7.3f, UpFloor + 0.005f, 13.4f)),
                (new Vector3(-4.7f, UpFloor, 1.8f), new Vector3(-4.6f, UpFloor + 0.005f, 10.6f)),
                (new Vector3(-7.4f, UpFloor, 13.3f), new Vector3(1.4f, UpFloor + 0.005f, 13.4f)),
                (new Vector3(-4.6f, UpFloor, 10.6f), new Vector3(4.4f, UpFloor + 0.005f, 10.7f)),
            })
            {
                NoCollider(Box("HazardLine", runs, a, b, hazard));
            }
            foreach (Vector3 p in new[] { new Vector3(-2.2f, 0f, 0.9f), new Vector3(-6.3f, 0f, 7.6f), new Vector3(-0.8f, 0f, 11.6f) })
            {
                NoCollider(Part("Puddle", runs, new Vector3(p.x, UpFloor + 0.004f, p.z), new Vector3(1.1f, 0.002f, 0.7f), puddle, PrimitiveType.Cylinder));
            }

            BuildUnderpassLights(Group("Lights", root));
            BuildUnderpassSigns();
            BuildUnderpassAudio(Group("Audio", root));
            BuildUnderpassVolume(root);
        }

        private static void BuildCorridors(Transform g, Material floor, Material wall, Material lower, Material ceil, Material hazard)
        {
            const float t = 0.2f;
            // Floors (one slab per leg, no overlaps)
            Box("L1_Floor", g, new Vector3(-7.5f, UpFloor - 0.5f, -1f), new Vector3(2f, UpFloor, 2f), floor);
            Box("L2_Floor", g, new Vector3(-7.5f, UpFloor - 0.5f, 2f), new Vector3(-4.5f, UpFloor, 10.5f), floor);
            Box("L3_Floor", g, new Vector3(-7.5f, UpFloor - 0.5f, 10.5f), new Vector3(4.5f, UpFloor, 13.5f), floor);

            // Ceilings, with the grate shaft cut out of L1
            float hx0 = GrateHole.xMin, hx1 = GrateHole.xMax, hz0 = GrateHole.yMin, hz1 = GrateHole.yMax;
            Box("L1_Ceil_W", g, new Vector3(-7.5f, UpCeil, -1f), new Vector3(hx0, UpCeil + 0.1f, 2f), ceil);
            Box("L1_Ceil_E", g, new Vector3(hx1, UpCeil, -1f), new Vector3(2f, UpCeil + 0.1f, 2f), ceil);
            Box("L1_Ceil_S", g, new Vector3(hx0, UpCeil, -1f), new Vector3(hx1, UpCeil + 0.1f, hz0), ceil);
            Box("L1_Ceil_N", g, new Vector3(hx0, UpCeil, hz1), new Vector3(hx1, UpCeil + 0.1f, 2f), ceil);
            Box("L2_Ceil", g, new Vector3(-7.5f, UpCeil, 2f), new Vector3(-4.5f, UpCeil + 0.1f, 10.5f), ceil);
            Box("L3_Ceil", g, new Vector3(-7.5f, UpCeil, 10.5f), new Vector3(1.5f, UpCeil + 0.1f, 13.5f), ceil);
            // Shaft lining between the underpass ceiling and the gallery floor
            Box("Shaft_W", g, new Vector3(hx0 - 0.05f, UpCeil, hz0 - 0.05f), new Vector3(hx0, -0.5f, hz1 + 0.05f), ceil);
            Box("Shaft_E", g, new Vector3(hx1, UpCeil, hz0 - 0.05f), new Vector3(hx1 + 0.05f, -0.5f, hz1 + 0.05f), ceil);

            // Walls (inner faces on the corridor edges); open side rooms get doorway gaps
            WallAlongX("L1_Wall_S", g, -1f - t, -1f, -7.5f - t, 2f + t, new[] { (-2.5f, 0.9f) }, wall, lower);
            WallAlongZ("L1_Wall_E", g, 2f, 2f + t, -1f, 2f, null, wall, lower);
            WallAlongX("L1_Wall_N", g, 2f, 2f + t, -4.5f, 2f + t, null, wall, lower);
            WallAlongZ("L2_Wall_W", g, -7.5f - t, -7.5f, -1f - t, 13.5f + t, new[] { (4f, 1.6f) }, wall, lower);
            WallAlongZ("L2_Wall_E", g, -4.5f, -4.5f + t, 2f, 10.5f, new[] { (6f, 1.6f) }, wall, lower);
            WallAlongX("L3_Wall_S", g, 10.5f - t, 10.5f, -4.5f, 4.5f + t, null, wall, lower);
            WallAlongZ("L3_Wall_E", g, 4.5f, 4.5f + t, 10.5f - t, 13.5f, null, wall, lower);
            WallAlongX("L3_Wall_N", g, 13.5f, 13.5f + t, -7.5f - t, 1.5f, null, wall, lower);

            // Locked stores: door slabs on the walls (scenery, future storerooms)
            LockedDoor(g, "STORE B-01", new Vector3(-6f, UpFloor, -1f), 0f);
            LockedDoor(g, "STORE B-02", new Vector3(-2f, UpFloor, 2f), 180f);
            LockedDoor(g, "STORE B-03", new Vector3(-7.5f, UpFloor, 9f), 90f);
            LockedDoor(g, "STORE B-04", new Vector3(-5f, UpFloor, 13.5f), 180f);
            LockedDoor(g, "WORKSHOP B-05", new Vector3(-1f, UpFloor, 13.5f), 180f);
        }

        /// <summary>Ramp up to street level, a short landing and the push-bar emergency exit into the plaza.</summary>
        private static void BuildStairExit(Transform g, Material floor, Material wall, Material ceil, Material hazard)
        {
            const float top = 3f;
            Box("Stair_Wall_W", g, new Vector3(1.3f, UpFloor, 13.5f), new Vector3(1.5f, top, 20f), wall);
            Box("Stair_Wall_E", g, new Vector3(4.5f, UpFloor, 13.5f), new Vector3(4.7f, top, 20f), wall);
            Box("Stair_Ceiling", g, new Vector3(1.3f, top, 13.5f), new Vector3(4.7f, top + 0.2f, 20f), ceil);
            Box("Stair_Landing", g, new Vector3(1.5f, -0.5f, 19.2f), new Vector3(4.5f, 0f, 20f), floor);

            // The ramp: a tilted slab whose top surface runs from (z 13.5, floor) to (z 19.2, y 0)
            float run = 19.2f - 13.5f, rise = -UpFloor;
            float length = Mathf.Sqrt(run * run + rise * rise);
            Quaternion tilt = Quaternion.Euler(-Mathf.Atan2(rise, run) * Mathf.Rad2Deg, 0f, 0f);
            Vector3 topMid = new Vector3(3f, UpFloor * 0.5f, 13.5f + run * 0.5f);
            var ramp = Block("Stair_Ramp", g, topMid - tilt * Vector3.up * 0.15f, new Vector3(3f, 0.3f, length), floor);
            ramp.transform.rotation = tilt;
            foreach (float x in new[] { 1.62f, 4.38f })
            {
                var stripe = NoCollider(Block("Ramp_Edge", g, topMid + tilt * Vector3.up * 0.003f + new Vector3(x - 3f, 0f, 0f), new Vector3(0.12f, 0.01f, length), hazard));
                stripe.transform.rotation = tilt;
            }
            // Handrail
            var rail = Part("Handrail", g, topMid + new Vector3(-1.38f, 0.95f, 0f), new Vector3(0.05f, length * 0.5f, 0.05f), ArtLibrary.Get("M_BrushedSilver"), PrimitiveType.Cylinder);
            rail.transform.rotation = tilt * Quaternion.Euler(90f, 0f, 0f);

            // Exit door in the shared plaza wall (the wall builder leaves the gap): opens from the stair side only,
            // swings out into the plaza and closes itself behind you.
            var door = new GameObject("Underpass Exit Door");
            door.transform.SetParent(g, false);
            door.transform.position = new Vector3(3f, 0f, 20.25f);
            var hinge = Group("Hinge", door.transform);
            hinge.localPosition = new Vector3(-0.6f, 0f, 0f);
            Material steel = Worn(GetOrCreateMaterial("Up_DoorSteel", new Color(0.36f, 0.4f, 0.44f), 0.5f), 0.4f, 0.3f, 0.5f);
            var slab = Block("Slab", hinge, new Vector3(0.6f, 1.15f, 0f), new Vector3(1.2f, 2.3f, 0.08f), steel);
            slab.transform.localPosition = new Vector3(0.6f, 1.15f, 0f);
            var bar = Part("PushBar", hinge, new Vector3(0.6f, 1.0f, -0.09f), new Vector3(0.9f, 0.05f, 0.06f), hazard);
            bar.transform.localPosition = new Vector3(0.6f, 1.0f, -0.09f);
            var latch = door.AddComponent<LatchDoor>();
            latch.Configure(TunnelIds.UnderpassExit, hinge, -95f, Vector3.back,
                "STAFF ONLY. No handle on this side.", "Push the bar", ProceduralAudio.Clunk, 4f);

            // Where security deposits you after a catch: just outside, facing into the plaza
            var exit = new GameObject("Underpass Escort Exit").transform;
            exit.SetParent(g, false);
            exit.SetPositionAndRotation(new Vector3(3f, 0.02f, 21.8f), Quaternion.identity);
            _underpassExit = exit;
        }

        private static void BuildSideRooms(Transform g, Material floor, Material wall, Material lower, Material ceil, Material duct)
        {
            // Supply closet (guaranteed hiding spot): small, dim, shelves, door half shut
            RoomShell(g, "Closet", new Vector3(-3.6f, UpFloor, -3.4f), new Vector3(-1.4f, UpCeil, -1.2f), floor, wall, ceil, skipNorth: true);
            Material shelf = ArtLibrary.Get("M_WorkshopOrange");
            for (int i = 0; i < 3; i++)
            {
                Box($"Closet_Shelf_{i}", g, new Vector3(-3.4f, UpFloor + 0.5f + i * 0.6f, -3.2f), new Vector3(-1.6f, UpFloor + 0.54f + i * 0.6f, -2.8f), shelf);
            }
            Box("Closet_ShelfPost_W", g, new Vector3(-3.4f, UpFloor, -3.2f), new Vector3(-3.36f, UpFloor + 1.8f, -2.8f), shelf);
            Box("Closet_ShelfPost_E", g, new Vector3(-1.64f, UpFloor, -3.2f), new Vector3(-1.6f, UpFloor + 1.8f, -2.8f), shelf);
            var closetDoor = Block("Closet_Door", g, new Vector3(-2.74f, UpFloor + 1.05f, -1.42f), new Vector3(0.85f, 2.1f, 0.05f),
                Worn(GetOrCreateMaterial("Up_DoorSteel", new Color(0.36f, 0.4f, 0.44f), 0.5f), 0.4f, 0.3f, 0.5f));
            // Hinged on the west jamb, swung 60° into the closet: still leaves a gap you can slip through
            closetDoor.transform.SetPositionAndRotation(new Vector3(-2.74f, UpFloor + 1.05f, -1.42f), Quaternion.Euler(0f, 60f, 0f));
            HideZone("Closet Hiding Spot", new Vector3(-3.5f, UpFloor, -3.3f), new Vector3(-1.5f, UpFloor + 2.4f, -1.3f));

            // Pump room (open, sweepable): noisy pump, pipes, door propped wide with warm light spilling out
            RoomShell(g, "PumpRoom", new Vector3(-11f, UpFloor, 2.2f), new Vector3(-7.7f, UpCeil, 5.8f), floor, wall, ceil, skipEast: true);
            Part("Pump", g, new Vector3(-9.8f, UpFloor + 0.6f, 4f), new Vector3(1.1f, 0.6f, 1.1f), duct, PrimitiveType.Cylinder);
            Part("PumpMotor", g, new Vector3(-9.8f, UpFloor + 1.35f, 4f), new Vector3(0.6f, 0.25f, 0.6f), ArtLibrary.Get("M_HazardPaint"), PrimitiveType.Cylinder);
            PipeX(g, -11f, -9.8f, UpFloor + 0.6f, 4f, 0.12f, duct, ceil);
            PipeZ(g, 2.2f, 5.8f, -1.2f, -10.6f, 0.12f, duct, ceil);
            // Door swung right back against the inside wall: wide open, light spilling out (a room drones check)
            Block("PumpRoom_Door", g, new Vector3(-7.78f, UpFloor + 1.05f, 5.3f), new Vector3(0.05f, 2.1f, 0.95f), lower);

            // Break room (open, sweepable): vending machine (naturally), a table, door propped
            RoomShell(g, "BreakRoom", new Vector3(-4.3f, UpFloor, 4.2f), new Vector3(-1f, UpCeil, 7.8f), floor, wall, ceil, skipWest: true);
            var vend = Block("VendingMachine", g, new Vector3(-1.5f, UpFloor + 0.95f, 6.6f), new Vector3(0.7f, 1.9f, 0.9f), GetOrCreateMaterial("Up_Vend", new Color(0.7f, 0.12f, 0.18f), 0.5f));
            _ = vend;
            NoCollider(Block("VendingFront", g, new Vector3(-1.86f, UpFloor + 1.1f, 6.6f), new Vector3(0.02f, 1.2f, 0.7f),
                GetOrCreateMaterial("Up_VendGlow", new Color(0.4f, 0.9f, 1f), 0.8f, new Color(0.5f, 1.2f, 1.5f))));
            Block("Table", g, new Vector3(-2.8f, UpFloor + 0.72f, 5.6f), new Vector3(1.2f, 0.06f, 0.8f), ArtLibrary.Get("M_WorkshopOrange"));
            Block("TableLeg", g, new Vector3(-2.8f, UpFloor + 0.36f, 5.6f), new Vector3(0.08f, 0.72f, 0.08f), ceil);
            Block("BreakRoom_Door", g, new Vector3(-4.22f, UpFloor + 1.05f, 7.3f), new Vector3(0.05f, 2.1f, 0.95f), lower);
        }

        /// <summary>The see-through grate over the hole in gallery D1: walkable and sight-blocking until pried off.</summary>
        private static void BuildGrate(Transform parent)
        {
            Material iron = Worn(ArtLibrary.Get("M_RustyMetal"), 0.6f, 0.5f, 0.6f);
            var root = new GameObject("Crowbar Grate");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(GrateHole.center.x, 0f, GrateHole.center.y);

            var blocker = root.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0f, -0.05f, 0f);
            blocker.size = new Vector3(GrateHole.width + 0.1f, 0.1f, GrateHole.height + 0.1f);

            var panel = Group("Panel", root.transform);
            float w = GrateHole.width;
            foreach (float s in new[] { -0.5f, 0.5f })
            {
                Part("Frame", panel, new Vector3(s * w, 0.01f, 0f), new Vector3(0.05f, 0.03f, w + 0.05f), iron);
                Part("Frame", panel, new Vector3(0f, 0.01f, s * w), new Vector3(w + 0.05f, 0.03f, 0.05f), iron);
            }
            for (int i = 1; i < 8; i++)
            {
                Part("Bar", panel, new Vector3(-w * 0.5f + i * w / 8f, 0.005f, 0f), new Vector3(0.025f, 0.02f, w), iron);
            }
            Part("CrossBar", panel, new Vector3(0f, 0.012f, 0f), new Vector3(w, 0.02f, 0.03f), iron);
            foreach (Vector2 c in new[] { new Vector2(-0.36f, -0.36f), new Vector2(0.36f, 0.36f) })
            {
                Part("RustBolt", panel, new Vector3(c.x * w / 0.8f, 0.025f, c.y * w / 0.8f), new Vector3(0.05f, 0.015f, 0.05f), ArtLibrary.Get("M_DarkMetal"), PrimitiveType.Cylinder);
            }

            var grate = root.AddComponent<CrowbarGrate>();
            grate.Configure(TunnelIds.GrateGate, Crowbar, panel, blocker, ProceduralAudio.PryScreech);
            // Rest pose: leaning against the gallery's west wall, clear of the hole and the walkway
            var so = new SerializedObject(grate);
            so.FindProperty("_restPosition").vector3Value = new Vector3(-0.6f - GrateHole.center.x + 0.1f, 0.4f, 0f);
            so.FindProperty("_restEuler").vector3Value = new Vector3(0f, 0f, -78f);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Soft landing below (no fall damage in this game, but it should look survivable)
            Material mattress = GetOrCreateMaterial("Up_Mattress", new Color(0.55f, 0.5f, 0.38f), 0.2f);
            Material crate = Worn(ArtLibrary.Get("M_WorkshopOrange"), 0.5f, 0.4f, 0.5f);
            var land = Group("Landing", parent);
            Block("Mattress", land, new Vector3(GrateHole.center.x, UpFloor + 0.11f, GrateHole.center.y), new Vector3(1.5f, 0.22f, 1.0f), mattress)
                .transform.rotation = Quaternion.Euler(0f, 8f, 0f);
            Block("Crate", land, new Vector3(1.55f, UpFloor + 0.35f, -0.55f), new Vector3(0.7f, 0.7f, 0.7f), crate);
            Block("Crate", land, new Vector3(1.6f, UpFloor + 0.95f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f), crate).transform.rotation = Quaternion.Euler(0f, 20f, 0f);
        }

        // ------------------------------------------------------------------ Light, signs, sound, grade

        private static void BuildUnderpassLights(Transform g)
        {
            Color cold = new Color(0.72f, 0.88f, 1f);
            void Tube(Vector3 p, bool alongX, Color c, float range = 7.5f, float intensity = 1.7f)
            {
                var fixture = Group("TubeLight", g);
                fixture.position = p;
                Part("Housing", fixture, Vector3.zero, alongX ? new Vector3(1.3f, 0.06f, 0.18f) : new Vector3(0.18f, 0.06f, 1.3f), ArtLibrary.Get("M_DarkMetal"));
                Part("Tube", fixture, new Vector3(0f, -0.04f, 0f), alongX ? new Vector3(1.2f, 0.03f, 0.08f) : new Vector3(0.08f, 0.03f, 1.2f),
                    GetOrCreateMaterial("Up_Tube", c, 0.8f, c * 2.2f));
                var light = new GameObject("Light").AddComponent<Light>();
                light.transform.SetParent(fixture, false);
                light.transform.localPosition = new Vector3(0f, -0.3f, 0f);
                light.type = LightType.Point;
                light.color = c;
                light.range = range;
                light.intensity = intensity;
                light.shadows = LightShadows.None;
            }
            Tube(new Vector3(1.1f, UpCeil - 0.05f, 0.5f), true, cold);       // right under the grate: visible from above
            Tube(new Vector3(-3.5f, UpCeil - 0.05f, 0.5f), true, cold);
            Tube(new Vector3(-6f, UpCeil - 0.05f, 4f), false, cold);
            Tube(new Vector3(-6f, UpCeil - 0.05f, 9f), false, cold, 7f, 1.2f);
            Tube(new Vector3(-4.5f, UpCeil - 0.05f, 12f), true, cold);
            Tube(new Vector3(0.5f, UpCeil - 0.05f, 12f), true, cold);
            Tube(new Vector3(3f, 2.9f, 18.5f), true, cold, 8f, 1.8f);
            Tube(new Vector3(-9.4f, UpCeil - 0.05f, 4f), false, new Color(1f, 0.7f, 0.4f), 6f, 1.6f);  // pump room: warm spill
            Tube(new Vector3(-2.6f, UpCeil - 0.05f, 6f), true, new Color(1f, 0.85f, 0.6f), 6f, 1.4f);  // break room
        }

        private static void BuildUnderpassSigns()
        {
            SignPlate("Up_Welcome", "SUBLEVEL B", "MAINTENANCE · AUTHORISED PERSONNEL ONLY", new Vector3(2f, UpFloor + 2.1f, 0.5f), 90f, SignStyle.Industrial, 1.6f, 0.5f);
            SignPlate("Up_Exit1", "EXIT", "→ ALONG THE RED PIPE", new Vector3(-7.5f, UpFloor + 2.0f, 1.0f), -90f, SignStyle.Industrial, 1.1f, 0.4f, -1.5f);
            SignPlate("Up_Exit2", "EXIT →", "PLAZA LEVEL", new Vector3(-6f, UpFloor + 2.0f, 13.5f), 0f, SignStyle.Industrial, 1.0f, 0.36f, 1f);
            SignPlate("Up_Ramp", "EMERGENCY EXIT", "PUSH BAR · DOOR CLOSES BEHIND YOU", new Vector3(3f, 2.55f, 20f), 0f, SignStyle.Warning, 1.3f, 0.4f);
            SignPlate("Up_Closet", "SUPPLIES", null, new Vector3(-1.85f, UpFloor + 2.2f, -1f), 180f, SignStyle.Maintenance, 0.6f, 0.24f, 3f);
            SignPlate("Up_Pump", "PUMP ROOM", "EAR DEFENDERS", new Vector3(-7.5f, UpFloor + 2.35f, 5.3f), -90f, SignStyle.Industrial, 0.8f, 0.3f);
            SignPlate("Up_Break", "STAFF BREAK", "NO SLEEPING (AGAIN)", new Vector3(-4.5f, UpFloor + 2.35f, 6.8f), 90f, SignStyle.Maintenance, 0.8f, 0.3f);
            SignPlate("Up_PlazaSide", "STAFF ONLY", null, new Vector3(3f, 2.55f, 20.5f), 180f, SignStyle.Warning, 1.0f, 0.3f);
        }

        private static void BuildUnderpassAudio(Transform g)
        {
            Loop(g, "RoomTone_Underpass_L1", new Vector3(-3f, UpFloor + 1.6f, 0.5f), ProceduralAudio.RoomHum, 0.3f * TunnelAmbienceScale, 2f, 11f, 0.85f);
            Loop(g, "RoomTone_Underpass_L3", new Vector3(-1f, UpFloor + 1.6f, 12f), ProceduralAudio.RoomHum, 0.3f * TunnelAmbienceScale, 2f, 11f, 0.8f);
            Loop(g, "Pump", new Vector3(-9.8f, UpFloor + 1f, 4f), ProceduralAudio.BigFan, 0.35f * TunnelAmbienceScale, 1.5f, 10f, 0.55f);
            Loop(g, "VendingHum", new Vector3(-1.6f, UpFloor + 1f, 6.6f), ProceduralAudio.Buzz, 0.12f * TunnelAmbienceScale, 0.8f, 5f, 1.4f);
        }

        private static void BuildUnderpassVolume(Transform parent)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(UnderpassVolumePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, UnderpassVolumePath);
            }
            Vignette vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(new Color(0.01f, 0.02f, 0.04f));
            ColorAdjustments grade = GetOrAdd<ColorAdjustments>(profile);
            grade.postExposure.Override(-0.1f);
            grade.colorFilter.Override(new Color(0.88f, 0.95f, 1f));
            grade.saturation.Override(-12f);
            EditorUtility.SetDirty(profile);

            var go = new GameObject("Underpass Volume");
            go.transform.SetParent(parent, false);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(-3.5f, -2f, 8f);
            box.size = new Vector3(16f, 4f, 24f);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.blendDistance = 1f;
            volume.priority = 20f;
            volume.sharedProfile = profile;
        }

        // ------------------------------------------------------------------ Security

        /// <summary>Restricted zone (trespass + escort-out) and the two underpass drones. Runs after BuildSecurity.</summary>
        private static void BuildUnderpassSecurity()
        {
            var zones = new GameObject("Underpass Security Zones").transform;
            foreach (var (name, min, max) in new[]
            {
                ("L1", new Vector3(-7.5f, UpFloor, -1f), new Vector3(2f, UpCeil, 2f)),
                ("L2", new Vector3(-7.5f, UpFloor, 2f), new Vector3(-4.5f, UpCeil, 10.5f)),
                ("L3", new Vector3(-7.5f, UpFloor, 10.5f), new Vector3(4.5f, UpCeil, 13.5f)),
                ("Stair", new Vector3(1.5f, UpFloor, 13.5f), new Vector3(4.5f, 2.6f, 20f)),
                ("PumpRoom", new Vector3(-11f, UpFloor, 2.2f), new Vector3(-7.5f, UpCeil, 5.8f)),
                ("BreakRoom", new Vector3(-4.5f, UpFloor, 4.2f), new Vector3(-1f, UpCeil, 7.8f)),
            })
            {
                var go = new GameObject($"Restricted {name}");
                go.transform.SetParent(zones, false);
                go.transform.position = (min + max) * 0.5f;
                go.AddComponent<BoxCollider>().size = max - min;
                go.AddComponent<SecurityZone>().Configure(SecurityZoneKind.Restricted, TunnelIds.Underpass, _underpassExit);
            }

            // Drone A: the drop and the long leg. Parks under the grate (visible from the gallery on day one) and
            // sometimes sweeps the pump room or the break room. Never the closet.
            BuildDrone("Underpass Drone A", 2.2f,
                new[]
                {
                    new Stop(0.7f, 0.5f, 2.5f, 90f, -3f, 0.5f),
                    new Stop(-3.5f, 0.5f, 1.0f, 60f, -2.5f, -1f),
                    new Stop(-6f, 0.5f, 1.0f, 60f, -6f, 6f),
                    new Stop(-6f, 6f, 1.2f, 70f, -3f, 6f),
                    new Stop(-6f, 9.8f, 1.4f, 60f, -6f, 12f),
                },
                new (int from, Stop[] points, float chance)[]
                {
                    (2, new[] { new Stop(-6.4f, 4f, 0.6f, 20f, -9.5f, 4f), new Stop(-9.2f, 4f, 1.6f, 90f, -10f, 3f) }, 0.35f),
                    (3, new[] { new Stop(-3.2f, 6f, 1.6f, 80f, -1.5f, 6.6f) }, 0.3f),
                },
                new Bounds(new Vector3(-4.5f, 0f, 4.75f), new Vector3(13f, 10f, 11.5f)), false, UpFloor);

            // Drone B: the last leg, eyeing the ramp up to the exit.
            BuildDrone("Underpass Drone B", 2.2f,
                new[]
                {
                    new Stop(-6f, 12f, 1.2f, 70f, -6f, 8f),
                    new Stop(-1.5f, 12f, 1.0f, 60f, -1f, 13.5f),
                    new Stop(3f, 12f, 2.2f, 50f, 3f, 17f),
                    new Stop(-1.5f, 12f, 0.8f, 40f, -4f, 11f),
                },
                new (int from, Stop[] points, float chance)[0],
                new Bounds(new Vector3(-1.5f, 0f, 12f), new Vector3(12f, 10f, 3f)), false, UpFloor);
        }

        private static void HideZone(string name, Vector3 min, Vector3 max)
        {
            var go = new GameObject(name);
            go.transform.position = (min + max) * 0.5f;
            go.AddComponent<BoxCollider>().size = max - min;
            go.AddComponent<SecurityZone>().Configure(SecurityZoneKind.Hideout, "closet_u01", null);
        }

        // ------------------------------------------------------------------ Helpers

        /// <summary>A wall running along X (between z0 and z1) from x0 to x1, with doorway gaps (centre x, width).</summary>
        private static void WallAlongX(string name, Transform g, float z0, float z1, float x0, float x1, (float c, float w)[] gaps, Material wall, Material lower)
        {
            var cuts = new List<(float, float)>();
            if (gaps != null)
            {
                foreach (var (c, w) in gaps)
                {
                    cuts.Add((c - w * 0.5f, c + w * 0.5f));
                    Box($"{name}_Lintel", g, new Vector3(c - w * 0.5f, UpFloor + 2.2f, z0), new Vector3(c + w * 0.5f, UpCeil, z1), wall);
                }
            }
            float from = x0;
            int i = 0;
            foreach (var (a, b) in cuts)
            {
                Segment(new Vector3(from, 0f, z0), new Vector3(a, 0f, z1), i++);
                from = b;
            }
            Segment(new Vector3(from, 0f, z0), new Vector3(x1, 0f, z1), i);

            void Segment(Vector3 a, Vector3 b, int n)
            {
                if (b.x - a.x < 0.01f)
                {
                    return;
                }
                Box($"{name}_{n}", g, new Vector3(a.x, UpFloor + 1.1f, a.z), new Vector3(b.x, UpCeil, b.z), wall);
                Box($"{name}_{n}_Lower", g, new Vector3(a.x, UpFloor, a.z), new Vector3(b.x, UpFloor + 1.1f, b.z), lower);
            }
        }

        /// <summary>A wall running along Z (between x0 and x1) from z0 to z1, with doorway gaps (centre z, width).</summary>
        private static void WallAlongZ(string name, Transform g, float x0, float x1, float z0, float z1, (float c, float w)[] gaps, Material wall, Material lower)
        {
            var cuts = new List<(float, float)>();
            if (gaps != null)
            {
                foreach (var (c, w) in gaps)
                {
                    cuts.Add((c - w * 0.5f, c + w * 0.5f));
                    Box($"{name}_Lintel", g, new Vector3(x0, UpFloor + 2.2f, c - w * 0.5f), new Vector3(x1, UpCeil, c + w * 0.5f), wall);
                }
            }
            float from = z0;
            int i = 0;
            foreach (var (a, b) in cuts)
            {
                Segment(from, a, i++);
                from = b;
            }
            Segment(from, z1, i);

            void Segment(float a, float b, int n)
            {
                if (b - a < 0.01f)
                {
                    return;
                }
                Box($"{name}_{n}", g, new Vector3(x0, UpFloor + 1.1f, a), new Vector3(x1, UpCeil, b), wall);
                Box($"{name}_{n}_Lower", g, new Vector3(x0, UpFloor, a), new Vector3(x1, UpFloor + 1.1f, b), lower);
            }
        }

        /// <summary>Floor, ceiling and walls of a side room; the side facing the corridor is skipped (the corridor wall has the doorway).</summary>
        private static void RoomShell(Transform g, string name, Vector3 min, Vector3 max, Material floor, Material wall, Material ceil,
            bool skipNorth = false, bool skipEast = false, bool skipWest = false)
        {
            const float t = 0.2f;
            // The floor runs on under the corridor wall on the open side, so the doorway has no gap (NavMesh + footing)
            Box($"{name}_Floor", g, new Vector3(min.x - (skipWest ? t : 0f), UpFloor - 0.5f, min.z),
                new Vector3(max.x + (skipEast ? t : 0f), UpFloor, max.z + (skipNorth ? t : 0f)), floor);
            Box($"{name}_Ceil", g, new Vector3(min.x, UpCeil, min.z), new Vector3(max.x, UpCeil + 0.1f, max.z), ceil);
            Box($"{name}_Wall_S", g, new Vector3(min.x - t, UpFloor, min.z - t), new Vector3(max.x + t, UpCeil, min.z), wall);
            if (!skipNorth)
            {
                Box($"{name}_Wall_N", g, new Vector3(min.x - t, UpFloor, max.z), new Vector3(max.x + t, UpCeil, max.z + t), wall);
            }
            if (!skipWest)
            {
                Box($"{name}_Wall_W", g, new Vector3(min.x - t, UpFloor, min.z), new Vector3(min.x, UpCeil, max.z), wall);
            }
            if (!skipEast)
            {
                Box($"{name}_Wall_E", g, new Vector3(max.x, UpFloor, min.z), new Vector3(max.x + t, UpCeil, max.z), wall);
            }
        }

        /// <summary>A locked store door on a corridor wall: slab, handle and a door plate. <paramref name="foot"/> is on the wall face at floor level.</summary>
        private static void LockedDoor(Transform g, string label, Vector3 foot, float yaw)
        {
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 outward = rot * Vector3.forward; // direction into the corridor
            Material steel = Worn(GetOrCreateMaterial("Up_StoreDoor", new Color(0.3f, 0.33f, 0.36f), 0.45f), 0.5f, 0.4f, 0.5f);
            var slab = NoCollider(Block($"Door {label}", g, foot + Vector3.up * 1.05f + outward * 0.03f, new Vector3(1.0f, 2.1f, 0.05f), steel));
            slab.transform.rotation = rot;
            var handle = NoCollider(Part("Handle", g, foot + Vector3.up * 1.0f + outward * 0.07f + rot * Vector3.right * 0.38f, new Vector3(0.04f, 0.14f, 0.04f), ArtLibrary.Get("M_BrushedSilver")));
            handle.transform.rotation = rot;
            SignPlate($"Up_{label}", label, "LOCKED", foot + Vector3.up * 2.3f + outward * 0.01f, yaw + 180f, SignStyle.Industrial, 0.7f, 0.24f);
        }

        /// <summary>VentNet Supplies: the crowbar for 60 CC, delivered to the den's storage locker.</summary>
        private static void ConfigureSupplies(VentNetPanel panel)
        {
            InventoryComponent locker = null;
            foreach (InventoryComponent inv in Object.FindObjectsByType<InventoryComponent>(FindObjectsSortMode.None))
            {
                if (inv.SaveId == "hideout.locker")
                {
                    locker = inv;
                }
            }

            var so = new SerializedObject(panel);
            SerializedProperty list = so.FindProperty("_supplies");
            list.arraySize = 1;
            SerializedProperty crowbar = list.GetArrayElementAtIndex(0);
            crowbar.FindPropertyRelative("Item").objectReferenceValue = Crowbar;
            crowbar.FindPropertyRelative("Price").intValue = 60;
            crowbar.FindPropertyRelative("Blurb").stringValue = "Lever it. Pry it. Don't ask.";
            so.FindProperty("_locker").objectReferenceValue = locker;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
