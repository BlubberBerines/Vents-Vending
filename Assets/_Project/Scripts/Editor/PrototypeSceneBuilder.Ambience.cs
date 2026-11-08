using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VentsAndVending.Tunnels;
using VentsAndVending.World;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Hideout pass 2 (Docs/Production/Backlog.md): sound, idle motion and lived-in clutter.
    /// - Audio (GDD §16 "hideout sounds"; TunnelNetwork.md §9 Old Maintenance identity: knocks, pipe ticks, fan rumble):
    ///   placeholder clips from <see cref="ProceduralAudio"/>, all positional.
    /// - Motion: swaying bulbs (HangingBulb), a couple of tired flickering bulbs, a wall fan, leaky pipes.
    /// - Clutter: the stuff someone actually living here would leave lying around.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        /// <summary>Mix level for the tunnel bed (room tone, duct air, fans). 0.75 = the requested −25%.</summary>
        private const float TunnelAmbienceScale = 0.75f;

        private static void BuildAmbience(Transform level)
        {
            var g = Group("Ambience", level);
            WireInteractableSounds();

            // Room tone and moving air
            Loop(g, "RoomTone_Chamber", new Vector3(0f, 1.6f, -17.2f), ProceduralAudio.RoomHum, 0.32f * TunnelAmbienceScale, 2f, 10f, 1f);
            Loop(g, "RoomTone_East", new Vector3(11f, 1.6f, -9f), ProceduralAudio.RoomHum, 0.3f * TunnelAmbienceScale, 2f, 11f, 0.9f);
            Loop(g, "RoomTone_Hub", new Vector3(20f, 1.4f, 26f), ProceduralAudio.RoomHum, 0.18f * TunnelAmbienceScale, 2f, 9f, 1.1f);
            foreach (var (pos, vol) in new[]
            {
                (new Vector3(0.2f, 0.5f, -12.4f), 0.25f), (new Vector3(0.3f, 1.6f, -4f), 0.3f), (new Vector3(-4f, 1.6f, 3.4f), 0.3f),
                (new Vector3(-8f, 1.6f, 10f), 0.28f), (new Vector3(-8f, 0.5f, 20f), 0.25f), (new Vector3(-4.8f, 0.5f, 10f), 0.2f),
                (new Vector3(5f, 1.6f, -9f), 0.22f), (new Vector3(18.5f, 2f, 9f), 0.3f), (new Vector3(18.5f, 3.6f, 20f), 0.25f),
                (new Vector3(20f, 0.5f, 33f), 0.2f),
            })
            {
                Loop(g, "DuctAir", pos,  ProceduralAudio.DuctAir, vol * TunnelAmbienceScale, 1.5f, 7.5f, 0.92f + Mathf.Repeat(pos.x * 0.37f + pos.z * 0.11f, 0.16f));
            }
            Loop(g, "Buzz_Breakers", new Vector3(16.7f, 2f, -8.8f), ProceduralAudio.Buzz, 0.14f, 0.8f, 5f, 1f);
            Loop(g, "Buzz_HubTube", new Vector3(20.2f, 2.2f, 27f), ProceduralAudio.Buzz, 0.1f, 0.8f, 6f, 1.2f);

            // Pipe knocks and ticks that wander around each space
            OneShots(g, "PipeKnocks_Gallery", new Vector3(-3f, 1.8f, 1f), new Vector3(5f, 0.4f, 7f),
                new[] { ProceduralAudio.KnockA, ProceduralAudio.KnockB, ProceduralAudio.Tick }, new Vector2(6f, 15f), new Vector2(0.25f, 0.6f), 14f);
            OneShots(g, "PipeTicks_Chamber", new Vector3(0f, 2.2f, -17.5f), new Vector3(3f, 0.2f, 2.5f),
                new[] { ProceduralAudio.Tick, ProceduralAudio.KnockB }, new Vector2(10f, 24f), new Vector2(0.12f, 0.3f), 9f);
            OneShots(g, "PipeKnocks_East", new Vector3(11f, 2.4f, -4f), new Vector3(3f, 0.5f, 6f),
                new[] { ProceduralAudio.KnockA, ProceduralAudio.KnockB }, new Vector2(8f, 18f), new Vector2(0.25f, 0.55f), 14f);

            // Leaky pipes
            Material puddle = GetOrCreateMaterial("Prop_Puddle", new Color(0.06f, 0.08f, 0.1f), 0.95f);
            Drip(g, new Vector3(2.5f, 2.22f, -19.78f), puddle, new Vector2(2f, 5f));
            Drip(g, new Vector3(1.0f, 1.96f, -0.2f), puddle, new Vector2(1.4f, 3.5f));
            Drip(g, new Vector3(-9.0f, 2.05f, 12f), puddle, new Vector2(2.5f, 6f));

            BuildWallFan(g, new Vector3(3.2f, 1.6f, -18.3f));
            AddFlicker(new Vector3(-3.9f, 2.7f, 3.4f));
            AddFlicker(new Vector3(10.6f, 3f, -9.6f));
            BuildClutter(Group("Clutter", g));
        }

        // ------------------------------------------------------------------ Sounds on interactables

        private static void WireInteractableSounds()
        {
            AudioClip clunk = ProceduralAudio.Clunk;
            foreach (VentHatch h in Object.FindObjectsByType<VentHatch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SetClip(h, "_sound", clunk);
            }
            foreach (BreakerSwitch b in Object.FindObjectsByType<BreakerSwitch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SetClip(b, "_clunk", clunk);
            }
            foreach (LatchDoor d in Object.FindObjectsByType<LatchDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SetClip(d, "_sound", clunk);
            }
            foreach (FanHazard fan in Object.FindObjectsByType<FanHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var src = fan.gameObject.AddComponent<AudioSource>();
                ConfigureSource(src, ProceduralAudio.BigFan, 0f, 2.5f, 24f);
                src.playOnAwake = false;
                var so = new SerializedObject(fan);
                so.FindProperty("_hum").objectReferenceValue = src;
                so.FindProperty("_humVolume").floatValue = 0.9f * TunnelAmbienceScale;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetClip(Object target, string field, AudioClip clip)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = clip;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ Builders

        private static void ConfigureSource(AudioSource src, AudioClip clip, float volume, float minDistance, float maxDistance)
        {
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = true;
            src.volume = volume;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.dopplerLevel = 0f;
            src.spread = 60f;
        }

        private static AudioSource Loop(Transform parent, string name, Vector3 position, AudioClip clip, float volume, float minDistance, float maxDistance, float pitch)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            ConfigureSource(src, clip, volume, minDistance, maxDistance);
            src.pitch = pitch;
            return src;
        }

        private static void OneShots(Transform parent, string name, Vector3 centre, Vector3 extents, AudioClip[] clips, Vector2 interval, Vector2 volume, float maxDistance)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            var src = go.AddComponent<AudioSource>();
            ConfigureSource(src, null, 1f, 1.5f, maxDistance);
            src.loop = false;
            src.playOnAwake = false;
            go.AddComponent<RandomOneShots>().Configure(clips, interval, volume, extents);
        }

        /// <summary>Drop from a pipe at <paramref name="top"/> to a puddle on the floor below, with a drip sound on landing.</summary>
        private static void Drip(Transform parent, Vector3 top, Material puddleMat, Vector2 interval)
        {
            var g = Group("Drip", parent);
            g.position = top;
            float height = top.y;

            var puddle = Part("Puddle", g, Vector3.zero, new Vector3(0.55f, 0.002f, 0.42f), puddleMat, PrimitiveType.Cylinder);
            puddle.transform.position = new Vector3(top.x, 0.004f, top.z);
            puddle.transform.rotation = Quaternion.Euler(0f, Hash(top) % 180, 0f);
            Part("PuddleSplash", g, Vector3.zero, new Vector3(0.22f, 0.002f, 0.3f), puddleMat, PrimitiveType.Cylinder)
                .transform.position = new Vector3(top.x + 0.22f, 0.005f, top.z - 0.08f);

            var psGo = new GameObject("Drops");
            psGo.transform.SetParent(g, false);
            var ps = psGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startSpeed = 0f;
            main.gravityModifier = 1f;
            main.startLifetime = Mathf.Sqrt(2f * height / 9.81f);
            main.startSize = 0.03f;
            main.startColor = new Color(0.7f, 0.85f, 1f, 0.8f);
            main.maxParticles = 6;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.02f;
            renderer.lengthScale = 1.5f;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);

            var impactGo = new GameObject("Impact");
            impactGo.transform.SetParent(g, false);
            impactGo.transform.position = new Vector3(top.x, 0.05f, top.z);
            var impact = impactGo.AddComponent<AudioSource>();
            ConfigureSource(impact, null, 0.7f, 0.8f, 7f);
            impact.loop = false;
            impact.playOnAwake = false;

            g.gameObject.AddComponent<Dripper>().Configure(ps, impact, new[] { ProceduralAudio.Drip, ProceduralAudio.DripLow }, height, interval);
        }

        /// <summary>A small extractor in the chamber wall: housing, spinning blades behind a grille, and its whir.</summary>
        private static void BuildWallFan(Transform parent, Vector3 wallPoint)
        {
            var g = Group("WallFan", parent);
            Material dark = ArtLibrary.Get("M_DarkMetal");
            Material housing = ArtLibrary.Get("M_HazardPaint");
            Material blade = GetOrCreateMaterial("Fan_Blade", new Color(0.62f, 0.6f, 0.55f), 0.45f);
            float x = wallPoint.x, y = wallPoint.y, z = wallPoint.z;
            const float s = 0.32f; // half size

            NoCollider(Box("Back", g, new Vector3(x - 0.02f, y - s, z - s), new Vector3(x, y + s, z + s), dark));
            Box("Housing_T", g, new Vector3(x - 0.24f, y + s - 0.05f, z - s), new Vector3(x, y + s, z + s), housing);
            Box("Housing_B", g, new Vector3(x - 0.24f, y - s, z - s), new Vector3(x, y - s + 0.05f, z + s), housing);
            Box("Housing_S", g, new Vector3(x - 0.24f, y - s, z - s), new Vector3(x, y + s, z - s + 0.05f), housing);
            Box("Housing_N", g, new Vector3(x - 0.24f, y - s, z + s - 0.05f), new Vector3(x, y + s, z + s), housing);
            for (int i = 0; i < 6; i++)
            {
                float yy = y - s + 0.09f + i * 0.1f;
                NoCollider(Box("Grille", g, new Vector3(x - 0.25f, yy - 0.012f, z - s + 0.04f), new Vector3(x - 0.23f, yy + 0.012f, z + s - 0.04f), dark));
            }

            var rotor = Group("Rotor", g);
            rotor.position = new Vector3(x - 0.1f, y, z);
            rotor.rotation = Quaternion.LookRotation(Vector3.left); // local Z faces into the room
            for (int i = 0; i < 4; i++)
            {
                var arm = Group($"Arm_{i}", rotor);
                arm.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
                Part($"Blade_{i}", arm, new Vector3(0f, 0.13f, 0f), new Vector3(0.09f, 0.22f, 0.012f), blade).transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
            }
            Part("Hub", rotor, Vector3.zero, new Vector3(0.07f, 0.03f, 0.07f), dark, PrimitiveType.Cylinder).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rotor.gameObject.AddComponent<Spinner>().Configure(new Vector3(0f, 0f, 540f), 0f, 1f);

            Loop(g, "WallFan_Whir", new Vector3(x - 0.1f, y, z), ProceduralAudio.SmallFan, 0.4f * TunnelAmbienceScale, 0.8f, 7f, 1f);
        }

        private static void AddFlicker(Vector3 anchor)
        {
            Transform best = null;
            float bestDist = 1f;
            foreach (Pendulum p in Object.FindObjectsByType<Pendulum>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(p.transform.position, anchor);
                if (d < bestDist)
                {
                    best = p.transform;
                    bestDist = d;
                }
            }
            if (best == null)
            {
                Debug.LogWarning($"[PrototypeSceneBuilder] No hanging bulb near {anchor} to flicker.");
                return;
            }
            var buzz = best.gameObject.AddComponent<AudioSource>();
            ConfigureSource(buzz, ProceduralAudio.FlickerBuzz, 0.35f, 0.8f, 7f);
            buzz.loop = false;
            buzz.playOnAwake = false;
            best.gameObject.AddComponent<LightFlicker>().Configure(best.GetComponentInChildren<Light>(), best.Find("Bulb").GetComponent<Renderer>(), buzz);
        }

        // ------------------------------------------------------------------ Clutter

        private static void BuildClutter(Transform g)
        {
            Material cardboard = ArtLibrary.Get("M_Cardboard");
            Material dark = ArtLibrary.Get("M_DarkMetal");
            Material rug = Worn(GetOrCreateMaterial("Prop_Rug", new Color(0.36f, 0.2f, 0.17f), 0.15f), 0.6f, 0.55f);
            Material paper = Worn(GetOrCreateMaterial("Prop_Paper", new Color(0.9f, 0.88f, 0.8f), 0.2f), 0.3f, 0.3f);
            Material jacket = Worn(GetOrCreateMaterial("Prop_Jacket", new Color(0.62f, 0.5f, 0.2f), 0.25f), 0.4f, 0.4f);
            Material boot = Worn(GetOrCreateMaterial("Prop_Boot", new Color(0.25f, 0.16f, 0.1f), 0.35f), 0.4f, 0.3f);
            Material toolbox = Worn(GetOrCreateMaterial("Prop_Toolbox", new Color(0.72f, 0.14f, 0.1f), 0.45f), 0.35f, 0.3f, 0.7f);
            Material terracotta = GetOrCreateMaterial("Prop_Pot", new Color(0.7f, 0.36f, 0.2f), 0.2f);
            Material leaf = GetOrCreateMaterial("Prop_Leaf", new Color(0.35f, 0.55f, 0.22f), 0.3f);
            Material bag = GetOrCreateMaterial("Prop_TrashBag", new Color(0.08f, 0.12f, 0.09f), 0.85f);
            Material yellow = Worn(GetOrCreateMaterial("Prop_WetSign", new Color(0.95f, 0.78f, 0.1f), 0.4f), 0.35f, 0.3f, 0.5f);
            Material mug = GetOrCreateMaterial("Prop_Mug", new Color(0.85f, 0.85f, 0.8f), 0.6f);
            Color[] canColours = { new Color(0.85f, 0.15f, 0.12f), new Color(0.1f, 0.7f, 0.65f), new Color(0.95f, 0.8f, 0.15f) };

            // Chamber: a rug where you stand, pizza boxes, cans, paper, the mug, notes on the locker
            NoCollider(Block("Rug", g, new Vector3(0.3f, 0.006f, -17.3f), new Vector3(1.6f, 0.012f, 1.05f), rug)).transform.rotation = Quaternion.Euler(0f, 6f, 0f);
            NoCollider(Block("PizzaBox", g, new Vector3(-2.6f, 0.025f, -17.1f), new Vector3(0.42f, 0.05f, 0.42f), cardboard)).transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            NoCollider(Block("PizzaBox", g, new Vector3(-2.56f, 0.076f, -17.06f), new Vector3(0.42f, 0.05f, 0.42f), cardboard)).transform.rotation = Quaternion.Euler(0f, -8f, 1.5f);
            var cans = new (Vector3 pos, Vector3 euler)[]
            {
                (new Vector3(-2.15f, 0.06f, -16.7f), Vector3.zero), (new Vector3(1.95f, 0.06f, -16.85f), Vector3.zero),
                (new Vector3(-1.5f, 0.033f, -18.6f), new Vector3(90f, 30f, 0f)), (new Vector3(-3.0f, 0.033f, -18.3f), new Vector3(90f, -70f, 0f)),
            };
            for (int i = 0; i < cans.Length; i++)
            {
                Material can = GetOrCreateMaterial($"Prop_Can_{i % 3}", canColours[i % 3], 0.7f);
                Part("Can", g, cans[i].pos, new Vector3(0.066f, 0.06f, 0.066f), can, PrimitiveType.Cylinder).transform.rotation = Quaternion.Euler(cans[i].euler);
            }
            foreach (Vector3 p in new[] { new Vector3(-3.0f, 0.04f, -17.6f), new Vector3(-1.75f, 0.04f, -18.9f), new Vector3(1.2f, 0.04f, -15.4f), new Vector3(0.9f, 0.04f, -18.5f) })
            {
                Part("Paper", g, p, new Vector3(0.1f, 0.075f, 0.09f), paper, PrimitiveType.Sphere).transform.rotation = Quaternion.Euler(Hash(p) % 40, Hash(p) % 360, 0f);
            }
            Part("Mug", g, new Vector3(1.62f, 0.945f, -15.75f), new Vector3(0.08f, 0.05f, 0.08f), mug, PrimitiveType.Cylinder);
            Part("MugHandle", g, new Vector3(1.67f, 0.945f, -15.75f), new Vector3(0.02f, 0.05f, 0.012f), mug);
            Material note = GetOrCreateMaterial("Prop_StickyNote", new Color(1f, 0.9f, 0.35f), 0.2f);
            Material noteP = GetOrCreateMaterial("Prop_StickyNotePink", new Color(1f, 0.55f, 0.7f), 0.2f);
            float lockerFront = LockerPos.z + 0.302f;
            var notes = new (float x, float y, float roll, Material m)[] { (-2.78f, 1.36f, 8f, note), (-2.66f, 1.27f, -6f, noteP), (-2.22f, 1.46f, 3f, note) };
            foreach (var (nx, ny, roll, m) in notes)
            {
                Part("StickyNote", g, new Vector3(nx, ny, lockerFront), new Vector3(0.075f, 0.075f, 0.003f), m).transform.rotation = Quaternion.Euler(0f, 0f, roll);
            }

            // Jacket on a hook and a poster on the west wall; boots by the bed nook
            Part("Hook", g, new Vector3(-3.15f, 1.78f, -16.6f), new Vector3(0.02f, 0.05f, 0.02f), dark, PrimitiveType.Cylinder).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            NoCollider(Block("Jacket", g, new Vector3(-3.13f, 1.43f, -16.6f), new Vector3(0.12f, 0.62f, 0.46f), jacket)).transform.rotation = Quaternion.Euler(4f, 0f, -3f);
            Material posterBg = GetOrCreateMaterial("Prop_Poster", new Color(0.18f, 0.5f, 0.55f), 0.2f);
            NoCollider(Block("Poster", g, new Vector3(-3.195f, 1.5f, -15.35f), new Vector3(0.006f, 0.72f, 0.52f), posterBg)).transform.rotation = Quaternion.Euler(-4f, 0f, 0f);
            CreateSign("Poster_Text", "<b>HANG IN\nTHERE</b>\n<size=45%>(the ceiling is low)</size>", new Vector3(-3.19f, 1.52f, -15.35f), -90f, 0.55f, new Color(1f, 0.92f, 0.6f), 0.5f, -4f);
            NoCollider(Block("Boot", g, new Vector3(-3.55f, 0.07f, -17.85f), new Vector3(0.12f, 0.14f, 0.28f), boot)).transform.rotation = Quaternion.Euler(0f, 12f, 0f);
            NoCollider(Block("Boot", g, new Vector3(-3.38f, 0.06f, -17.98f), new Vector3(0.12f, 0.14f, 0.28f), boot)).transform.rotation = Quaternion.Euler(0f, -16f, 78f);

            // Toolbox by the packing table, a floor cable to the mixer, a sad plant on the crates
            Block("Toolbox", g, new Vector3(3.0f, 0.1f, -16.3f), new Vector3(0.45f, 0.2f, 0.22f), toolbox).transform.rotation = Quaternion.Euler(0f, 80f, 0f);
            Part("ToolboxHandle", g, new Vector3(3.0f, 0.22f, -16.3f), new Vector3(0.03f, 0.03f, 0.2f), dark).transform.rotation = Quaternion.Euler(0f, 80f, 0f);
            Material cable = GetOrCreateMaterial("Prop_Cable", new Color(0.1f, 0.1f, 0.12f), 0.4f);
            CylinderBetween("FloorCable", g, new Vector3(2.75f, 0.35f, -19.97f), new Vector3(2.75f, 0.015f, -19.92f), 0.012f, cable);
            CylinderBetween("FloorCable", g, new Vector3(2.75f, 0.015f, -19.92f), new Vector3(2.25f, 0.015f, -19.4f), 0.012f, cable);
            CylinderBetween("FloorCable", g, new Vector3(2.25f, 0.015f, -19.4f), new Vector3(1.9f, 0.015f, -19.45f), 0.012f, cable);
            Vector3 pot = CratePos + new Vector3(1.05f, 0.75f, 0.12f);
            Part("PlantPot", g, pot, new Vector3(0.14f, 0.06f, 0.14f), terracotta, PrimitiveType.Cylinder);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 20f;
                var l = Part("Leaf", g, pot + Quaternion.Euler(0f, a, 0f) * new Vector3(0.07f, 0.06f, 0f), new Vector3(0.05f, 0.012f, 0.13f), leaf, PrimitiveType.Sphere);
                l.transform.rotation = Quaternion.Euler(25f, a + 90f, 0f); // drooping
            }

            // Gallery: bin bags, a leaning board, a WET FLOOR sign by the drip
            foreach (Vector3 p in new[] { new Vector3(0.8f, 0.22f, 3.95f), new Vector3(-8.85f, 0.2f, 14.85f), new Vector3(-7.2f, 0.2f, 5.0f) })
            {
                var b = Part("TrashBag", g, p, new Vector3(0.5f, 0.44f, 0.46f), bag, PrimitiveType.Sphere);
                b.transform.rotation = Quaternion.Euler(0f, Hash(p) % 360, 6f);
            }
            NoCollider(Block("OldBoard", g, new Vector3(-9.08f, 0.6f, 7.2f), new Vector3(0.04f, 1.2f, 0.6f), ArtLibrary.Get("M_RustyMetal"))).transform.rotation = Quaternion.Euler(0f, 0f, -9f);
            var wet = Group("WetFloorSign", g);
            wet.position = new Vector3(0.85f, 0f, -1.0f);
            Part("Front", wet, new Vector3(0f, 0.3f, -0.08f), new Vector3(0.32f, 0.6f, 0.02f), yellow).transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            Part("Back", wet, new Vector3(0f, 0.3f, 0.08f), new Vector3(0.32f, 0.6f, 0.02f), yellow).transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            wet.rotation = Quaternion.Euler(0f, -12f, 0f);
            CreateSign("WetFloor_Text", "<b>WET\nFLOOR</b>\n<size=60%>(always)</size>", wet.TransformPoint(new Vector3(0f, 0.34f, -0.11f)), -12f, 0.32f, new Color(0.1f, 0.08f, 0.05f), 0.3f);
        }
    }
}
