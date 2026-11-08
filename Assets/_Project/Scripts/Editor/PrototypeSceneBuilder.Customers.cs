using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using VentsAndVending.Customers;
using VentsAndVending.Inventory;

namespace VentsAndVending.Editor
{
    /// <summary>Plaza door, customer archetypes (GDD Part 8), spawner and NavMesh for the greybox.</summary>
    public static partial class PrototypeSceneBuilder
    {
        private const string CharacterPrefabFolder = "Assets/_Project/Prefabs/Characters";
        private const string CustomerDefinitionFolder = "Assets/_Project/ScriptableObjects/Characters";
        private const string NavMeshPath = "Assets/_Project/Scenes/Prototype/Greybox_Movement_NavMesh.asset";
        private static CustomerDefinition _blorbDefinition;
        private static CustomerDefinition _gromflomiteDefinition;

        /// <summary>Customer entrance at the far end of the plaza, trade spots and the spawner.</summary>
        private static void BuildPlazaDoorAndCustomers(Transform plaza, Material wall, Material accent)
        {
            EnsureFolder(CharacterPrefabFolder);
            EnsureFolder(CustomerDefinitionFolder);

            var g = Group("Customers", plaza);
            var doorGlow = GetOrCreateMaterial("Plaza_DoorGlow", new Color(0.2f, 0.02f, 0.18f), 0.6f, new Color(1.6f, 0.2f, 1.2f));
            var doorDark = GetOrCreateMaterial("Plaza_DoorDark", new Color(0.02f, 0.02f, 0.03f), 0.2f, new Color(0.08f, 0.02f, 0.1f));

            // Door in the plaza's far wall (inner face at z = PlazaFarZ)
            float z = PlazaFarZ;
            Block("Door_PillarL", g, new Vector3(-1.25f, 1.5f, z - 0.15f), new Vector3(0.3f, 3f, 0.3f), wall);
            Block("Door_PillarR", g, new Vector3(1.25f, 1.5f, z - 0.15f), new Vector3(0.3f, 3f, 0.3f), wall);
            Block("Door_Header", g, new Vector3(0f, 3.15f, z - 0.15f), new Vector3(2.8f, 0.3f, 0.3f), wall);
            var doorway = Block("Doorway", g, new Vector3(0f, 1.5f, z - 0.03f), new Vector3(2.2f, 3f, 0.04f), doorDark);
            Object.DestroyImmediate(doorway.GetComponent<Collider>());
            var strip = Block("Door_Neon", g, new Vector3(0f, 3.02f, z - 0.32f), new Vector3(2.3f, 0.05f, 0.05f), doorGlow);
            Object.DestroyImmediate(strip.GetComponent<Collider>());
            CreatePointLight("Door Glow", new Vector3(0f, 2.6f, z - 1.2f), new Color(1f, 0.2f, 0.85f), 6f, 2f, g);
            CreateSign("Door_Sign", "CITADEL-9 TRANSIT CONCOURSE", new Vector3(0f, 3.5f, z - 0.32f), 0f, 1.6f, new Color(1f, 0.6f, 0.95f), 6f);

            // Spawn/exit and three trade spots marked on the floor
            var spawn = new GameObject("Customer Spawn").transform;
            spawn.SetParent(g, false);
            spawn.position = new Vector3(0f, 0f, z - 1.4f);
            spawn.rotation = Quaternion.Euler(0f, 180f, 0f);

            CustomerDefinition blorb = CreateBlorbDefinition();
            CustomerDefinition gromflomite = CreateGromflomiteDefinition();
            _blorbDefinition = blorb;
            _gromflomiteDefinition = gromflomite;

            // Plaza customers come by appointment: VentNet orders bring them through this door to the A/B/C meet spots
            // (built by BuildPlazaRefresh). The Concourse Lounge keeps its own walk-in spawner.
            var managerGo = new GameObject("VentNet Orders");
            managerGo.AddComponent<VentsAndVending.Orders.OrderManager>()
                .Configure(new[] { blorb, gromflomite }, _meetSpots, spawn, spawn, ProceduralAudio.Message);
        }

        private static void BakeNavMesh(GameObject level)
        {
            var surface = level.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();

            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshPath) != null)
            {
                AssetDatabase.DeleteAsset(NavMeshPath);
            }
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
            EditorUtility.SetDirty(surface);
        }

        // ------------------------------------------------------------------ Blorb (GDD §8.2)

        private static CustomerDefinition CreateBlorbDefinition()
        {
            GameObject prefab = BuildBlorbPrefab();
            var lines = new CustomerDefinition.DialogueLines
            {
                Greeting = new[] { "Heyyy friend! Got any {product}?", "Ooh, are you the vent person? I need {product}!", "Hi! I'm {name}! I forgot why I'm here. Oh right — {product}!" },
                Accept = new[] { "Glorp! Pleasure doing business!", "Ooh it's so shiny! Byeee!", "My wobble approves!" },
                TooExpensive = new[] { "Whoa whoa, {price}?! How about {counter}?", "My wobble says no. {counter} maybe?", "That's like, a lot of credits. {counter}?" },
                QualityRejected = new[] { "Ew, it looks all… flat. Got better stuff?", "That one's sad-looking. Something nicer?" },
                WalkAway = new[] { "Forget it, I'll go wobble somewhere else.", "Rude! …Bye!" },
                Impatient = new[] { "I forgot what I was waiting for. Bye!", "Oooh, a shiny thing over there!" }
            };

            return GetOrCreateCustomer("Customer_Blorb", "blorb", "Blorb",
                new[] { "Glorbo", "Squishkin", "Blibble", "Wubbsy", "Jellbert", "Gloopa" },
                prefab, new Vector2Int(1, 2), new Vector2(0.15f, 0.4f), ItemQuality.Standard, false, 2, 50f, lines,
                new[]
                {
                    new Color(0.9f, 0.36f, 0.88f),  // bubblegum pink
                    new Color(0.2f, 0.78f, 0.75f),  // teal
                    new Color(0.55f, 0.85f, 0.25f), // lime
                    new Color(1f, 0.55f, 0.2f),     // tangerine
                    new Color(0.62f, 0.5f, 1f)      // lilac
                }, 0.14f);
        }

        /// <summary>
        /// Blorb (GDD §8.2): big glossy jelly, three expressive eyes, blush, open mouth, stubby feet, orbiting floaters.
        /// </summary>
        private static GameObject BuildBlorbPrefab()
        {
            var bodyMat = GetOrCreateMaterial("Char_Blorb", new Color(0.85f, 0.35f, 0.9f), 0.85f, new Color(0.12f, 0.03f, 0.14f));
            var eyeMat = GetOrCreateMaterial("Char_EyeWhite", new Color(0.97f, 0.96f, 0.92f), 0.6f);
            var pupilMat = GetOrCreateMaterial("Char_Pupil", new Color(0.03f, 0.02f, 0.05f), 0.9f);
            var shineMat = GetOrCreateMaterial("Char_Shine", Color.white, 0.2f, new Color(1.2f, 1.2f, 1.2f));
            var mouthMat = GetOrCreateMaterial("Char_Mouth", new Color(0.22f, 0.04f, 0.1f), 0.3f);
            var tongueMat = GetOrCreateMaterial("Char_Tongue", new Color(1f, 0.42f, 0.55f), 0.5f);
            var blushMat = GetOrCreateMaterial("Char_Blush", new Color(1f, 0.45f, 0.6f), 0.3f, new Color(0.25f, 0.05f, 0.1f));
            var floaterMat = GetOrCreateMaterial("Char_BlorbFloater", new Color(0.9f, 0.6f, 1f), 0.8f, new Color(0.6f, 0.2f, 0.8f));

            var root = new GameObject("Customer_Blorb");
            ConfigureAgent(root, radius: 0.45f, height: 1.2f, speed: 1.6f);
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.6f, 0f);
            col.radius = 0.45f;
            col.height = 1.2f;

            var body = Group("Body", root.transform);
            var jelly = Part("Jelly", body, new Vector3(0f, 0.52f, 0f), new Vector3(1.0f, 0.95f, 1.0f), bodyMat, PrimitiveType.Sphere);
            var footL = Part("Foot_L", body, new Vector3(-0.22f, 0.07f, 0.12f), new Vector3(0.28f, 0.14f, 0.34f), bodyMat, PrimitiveType.Sphere);
            var footR = Part("Foot_R", body, new Vector3(0.22f, 0.07f, 0.12f), new Vector3(0.28f, 0.14f, 0.34f), bodyMat, PrimitiveType.Sphere);
            Part("Shine", body, new Vector3(-0.22f, 0.86f, 0.3f), new Vector3(0.14f, 0.09f, 0.05f), shineMat, PrimitiveType.Sphere)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 35f);

            var eyes = new[]
            {
                FaceEye(body, new Vector3(-0.2f, 0.74f, 0.41f), 0.24f, eyeMat, pupilMat, shineMat),
                FaceEye(body, new Vector3(0.2f, 0.74f, 0.41f), 0.24f, eyeMat, pupilMat, shineMat),
                FaceEye(body, new Vector3(0f, 0.95f, 0.34f), 0.18f, eyeMat, pupilMat, shineMat),
            };
            Part("Blush_L", body, new Vector3(-0.33f, 0.55f, 0.36f), new Vector3(0.12f, 0.06f, 0.04f), blushMat, PrimitiveType.Sphere);
            Part("Blush_R", body, new Vector3(0.33f, 0.55f, 0.36f), new Vector3(0.12f, 0.06f, 0.04f), blushMat, PrimitiveType.Sphere);

            var mouth = Group("Mouth", body);
            mouth.localPosition = new Vector3(0f, 0.44f, 0.46f);
            Part("MouthHole", mouth, Vector3.zero, new Vector3(0.26f, 0.09f, 0.08f), mouthMat, PrimitiveType.Sphere);
            Part("Tongue", mouth, new Vector3(0f, -0.022f, 0.012f), new Vector3(0.12f, 0.04f, 0.07f), tongueMat, PrimitiveType.Sphere);

            var f1 = Part("Floater_1", root.transform, new Vector3(0.6f, 1.05f, 0f), Vector3.one * 0.14f, floaterMat, PrimitiveType.Sphere).transform;
            var f2 = Part("Floater_2", root.transform, new Vector3(-0.6f, 1.05f, 0f), Vector3.one * 0.12f, floaterMat, PrimitiveType.Sphere).transform;

            // Stubby jelly arms attached to the sides, angled out, little round hands
            var tint = new System.Collections.Generic.List<Renderer> { jelly.GetComponent<Renderer>(), footL.GetComponent<Renderer>(), footR.GetComponent<Renderer>() };
            var armL = Limb(body, "Arm_L", new Vector3(-0.43f, 0.55f, 0.04f), Quaternion.Euler(0f, 0f, -38f), 0.2f, 0.13f, 0.14f, 0.115f, -20f,
                bodyMat, bodyMat, Vector3.one * 0.13f, Vector3.zero, tint, out Transform elbowBL);
            var armR = Limb(body, "Arm_R", new Vector3(0.43f, 0.55f, 0.04f), Quaternion.Euler(0f, 0f, 38f), 0.2f, 0.13f, 0.14f, 0.115f, -20f,
                bodyMat, bodyMat, Vector3.one * 0.13f, Vector3.zero, tint, out Transform elbowBR);
            // Every arm part (including hands) gets the body colour
            foreach (Renderer r in armL.GetComponentsInChildren<Renderer>()) { if (!tint.Contains(r)) tint.Add(r); }
            foreach (Renderer r in armR.GetComponentsInChildren<Renderer>()) { if (!tint.Contains(r)) tint.Add(r); }

            return FinishCustomerPrefab(root, body, new[] { f1, f2 }, bubbleHeight: 1.65f, bobSpeed: 7f, squash: 0.08f,
                $"{CharacterPrefabFolder}/Customer_Blorb.prefab", eyes, mouth, tint.ToArray(),
                new[] { armL, armR }, new[] { 0f, Mathf.PI }, 32f, 7f,
                new[] { elbowBL, elbowBR }, new[] { -28f, -28f }, new[] { 1, 1 });
        }

        // ------------------------------------------------------------------ Gromflomite (GDD §8.1)

        private static CustomerDefinition CreateGromflomiteDefinition()
        {
            GameObject prefab = BuildGromflomitePrefab();
            var lines = new CustomerDefinition.DialogueLines
            {
                Greeting = new[] { "Citizen. I require {qty} unit(s) of {product}. Off the record.", "This interaction is not occurring. {product}, please.", "Please remain stationary while I purchase {product}." },
                Accept = new[] { "Acceptable. I will not be requesting a receipt. This time.", "Transaction filed under 'nothing happened'.", "Adequate. Efficient. Mildly illegal." },
                TooExpensive = new[] { "{price} violates subsection 12-F of my personal budget. {counter}.", "Form 88-C permits a maximum of {counter}.", "Unacceptable. I am authorised for {counter}." },
                QualityRejected = new[] { "This fails the minimum-standards inspection.", "Substandard. I have a form for this." },
                WalkAway = new[] { "I am filing this interaction under 'regrettable'.", "Your vendor licence has been mentally revoked." },
                Impatient = new[] { "My lunch break has concluded. Good day.", "Waiting exceeds regulation 4-D. Departing." }
            };

            return GetOrCreateCustomer("Customer_Gromflomite", "gromflomite", "Gromflomite",
                new[] { "Clerk Zix", "Sub-Inspector Vrell", "Auditor Kkt", "Deputy Registrar Plom" },
                prefab, new Vector2Int(1, 1), new Vector2(0.05f, 0.2f), ItemQuality.Standard, false, 2, 40f, lines,
                new[]
                {
                    new Color(0.2f, 0.25f, 0.42f),  // regulation navy
                    new Color(0.45f, 0.16f, 0.2f),  // maroon
                    new Color(0.3f, 0.34f, 0.3f),   // civil-service grey-green
                    new Color(0.36f, 0.24f, 0.46f)  // senior-clerk plum
                }, 0f);
        }

        /// <summary>
        /// Gromflomite (GDD §8.1): insectoid bureaucrat — big compound eyes, mandibles, collar and tie, oversized badge,
        /// a clipboard for the receipts it will absolutely request, and a slight forward hunch.
        /// </summary>
        private static GameObject BuildGromflomitePrefab()
        {
            var suitMat = GetOrCreateMaterial("Char_GromSuit", new Color(0.2f, 0.25f, 0.42f), 0.4f);
            var skinMat = GetOrCreateMaterial("Char_GromSkin", new Color(0.45f, 0.62f, 0.27f), 0.5f);
            var eyeMat = GetOrCreateMaterial("Char_GromEye", new Color(0.75f, 0.12f, 0.16f), 0.9f, new Color(0.18f, 0.02f, 0.03f));
            var pupilMat = GetOrCreateMaterial("Char_Pupil", new Color(0.03f, 0.02f, 0.05f), 0.9f);
            var shineMat = GetOrCreateMaterial("Char_Shine", Color.white, 0.2f, new Color(1.2f, 1.2f, 1.2f));
            var mouthMat = GetOrCreateMaterial("Char_Mouth", new Color(0.22f, 0.04f, 0.1f), 0.3f);
            var badgeMat = GetOrCreateMaterial("Char_Badge", new Color(0.95f, 0.78f, 0.2f), 0.7f, new Color(0.4f, 0.3f, 0.03f));
            var shirtMat = GetOrCreateMaterial("Char_Shirt", new Color(0.92f, 0.92f, 0.88f), 0.3f);
            var tieMat = GetOrCreateMaterial("Char_Tie", new Color(0.85f, 0.15f, 0.18f), 0.4f);
            var boardMat = GetOrCreateMaterial("Char_Clipboard", new Color(0.55f, 0.36f, 0.2f), 0.3f);
            var paperMat = GetOrCreateMaterial("Char_Paper", new Color(0.96f, 0.95f, 0.88f), 0.2f);

            var root = new GameObject("Customer_Gromflomite");
            ConfigureAgent(root, radius: 0.35f, height: 1.9f, speed: 1.9f);
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.radius = 0.35f;
            col.height = 1.9f;

            var body = Group("Body", root.transform);
            var tint = new System.Collections.Generic.List<Renderer>();

            // Legs: hip → thigh → knee → shin → shoe (noodle limbs, attached at the hips)
            var hipL = Limb(body, "Leg_L", new Vector3(-0.1f, 0.72f, 0f), Quaternion.identity, 0.34f, 0.095f, 0.32f, 0.085f, 0f,
                suitMat, pupilMat, new Vector3(0.11f, 0.065f, 0.21f), new Vector3(0f, -0.005f, 0.05f), tint, out Transform kneeL);
            var hipR = Limb(body, "Leg_R", new Vector3(0.1f, 0.72f, 0f), Quaternion.identity, 0.34f, 0.095f, 0.32f, 0.085f, 0f,
                suitMat, pupilMat, new Vector3(0.11f, 0.065f, 0.21f), new Vector3(0f, -0.005f, 0.05f), tint, out Transform kneeR);
            tint.Add(Part("Hips", body, new Vector3(0f, 0.74f, 0f), new Vector3(0.3f, 0.12f, 0.22f), suitMat, PrimitiveType.Sphere).GetComponent<Renderer>());

            var torso = Part("Torso", body, new Vector3(0f, 1.05f, 0f), new Vector3(0.45f, 0.4f, 0.32f), suitMat, PrimitiveType.Capsule);
            tint.Add(torso.GetComponent<Renderer>());
            Part("ShirtFront", body, new Vector3(0f, 1.22f, 0.13f), new Vector3(0.14f, 0.18f, 0.05f), shirtMat);
            Part("Tie", body, new Vector3(0f, 1.12f, 0.16f), new Vector3(0.05f, 0.24f, 0.02f), tieMat);
            Part("TieKnot", body, new Vector3(0f, 1.26f, 0.165f), new Vector3(0.06f, 0.05f, 0.03f), tieMat);
            Part("OversizedBadge", body, new Vector3(0.14f, 1.1f, 0.165f), new Vector3(0.14f, 0.18f, 0.02f), badgeMat);

            // Arms: shoulder → upper arm → elbow → forearm → hand. Left elbow bent to carry the clipboard.
            var shoulders = Part("Shoulders", body, new Vector3(0f, 1.33f, 0f), new Vector3(0.12f, 0.26f, 0.24f), suitMat, PrimitiveType.Capsule);
            shoulders.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tint.Add(shoulders.GetComponent<Renderer>());
            var shoulderL = Limb(body, "Arm_L", new Vector3(-0.23f, 1.33f, 0f), Quaternion.Euler(0f, 0f, -8f), 0.3f, 0.08f, 0.27f, 0.07f, -75f,
                suitMat, skinMat, Vector3.one * 0.09f, Vector3.zero, tint, out Transform elbowL);
            var shoulderR = Limb(body, "Arm_R", new Vector3(0.23f, 1.33f, 0f), Quaternion.Euler(0f, 0f, 8f), 0.3f, 0.08f, 0.27f, 0.07f, -10f,
                suitMat, skinMat, Vector3.one * 0.09f, Vector3.zero, tint, out Transform elbowR);

            // Clipboard held in the left hand (in the forearm's frame, so it moves with the arm)
            var board = Part("Clipboard", elbowL, new Vector3(0.02f, -0.3f, 0.05f), new Vector3(0.22f, 0.02f, 0.3f), boardMat);
            board.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            Part("Paper", board.transform, new Vector3(0f, -0.6f, -0.05f), new Vector3(0.85f, 0.5f, 0.75f), paperMat);
            Part("Clip", board.transform, new Vector3(0f, -0.9f, 0.42f), new Vector3(0.4f, 1.2f, 0.12f), pupilMat);

            // Head group leans forward: the hunch of someone who has stamped forms for 300 rotations
            var head = Group("Head", body);
            head.localPosition = new Vector3(0f, 1.45f, 0.02f);
            head.localRotation = Quaternion.Euler(10f, 0f, 0f);
            Part("Neck", head, new Vector3(0f, 0.02f, 0f), new Vector3(0.07f, 0.12f, 0.07f), skinMat);
            Part("Skull", head, new Vector3(0f, 0.18f, 0.02f), new Vector3(0.36f, 0.3f, 0.34f), skinMat, PrimitiveType.Sphere);
            var eyes = new[]
            {
                FaceEye(head, new Vector3(-0.13f, 0.22f, 0.13f), 0.21f, eyeMat, pupilMat, shineMat),
                FaceEye(head, new Vector3(0.13f, 0.22f, 0.13f), 0.21f, eyeMat, pupilMat, shineMat),
            };
            Part("Mandible_L", head, new Vector3(-0.05f, 0.06f, 0.16f), new Vector3(0.025f, 0.08f, 0.025f), pupilMat).transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            Part("Mandible_R", head, new Vector3(0.05f, 0.06f, 0.16f), new Vector3(0.025f, 0.08f, 0.025f), pupilMat).transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            var mouth = Group("Mouth", head);
            mouth.localPosition = new Vector3(0f, 0.09f, 0.17f);
            Part("MouthLine", mouth, Vector3.zero, new Vector3(0.07f, 0.018f, 0.02f), mouthMat, PrimitiveType.Sphere);
            Part("Antenna_L", head, new Vector3(-0.08f, 0.42f, 0.02f), new Vector3(0.015f, 0.2f, 0.015f), skinMat).transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            Part("Antenna_R", head, new Vector3(0.08f, 0.42f, 0.02f), new Vector3(0.015f, 0.2f, 0.015f), skinMat).transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            Part("AntennaTip_L", head, new Vector3(-0.115f, 0.52f, 0.02f), Vector3.one * 0.04f, badgeMat, PrimitiveType.Sphere);
            Part("AntennaTip_R", head, new Vector3(0.115f, 0.52f, 0.02f), Vector3.one * 0.04f, badgeMat, PrimitiveType.Sphere);

            // Gait: right arm swings with the left leg and vice versa
            return FinishCustomerPrefab(root, body, new Transform[0], bubbleHeight: 2.25f, bobSpeed: 9f, squash: 0.015f,
                $"{CharacterPrefabFolder}/Customer_Gromflomite.prefab", eyes, mouth, tint.ToArray(),
                new[] { hipL, hipR, shoulderR, shoulderL }, new[] { 0f, Mathf.PI, 0f, Mathf.PI * 0.5f }, 24f, 6.5f,
                new[] { kneeL, kneeR, elbowR, elbowL }, new[] { 55f, 55f, -32f, -12f }, new[] { 0, 0, 1, 1 });
        }

        // ------------------------------------------------------------------ Shared

        private static void ConfigureAgent(GameObject root, float radius, float height, float speed)
        {
            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = radius;
            agent.height = height;
            agent.speed = speed;
            agent.angularSpeed = 300f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.2f;
            agent.autoBraking = true;
        }

        /// <summary>Eye rig: unscaled root (blinks), white ball, sliding pupil with a cartoon catchlight.</summary>
        private static CharacterFace.Eye FaceEye(Transform parent, Vector3 position, float size, Material white, Material pupil, Material shine)
        {
            var root = Group("Eye", parent);
            root.localPosition = position;
            Part("Ball", root, Vector3.zero, Vector3.one * size, white, PrimitiveType.Sphere);
            var pupilRoot = Group("Pupil", root);
            pupilRoot.localPosition = new Vector3(0f, 0f, size * 0.4f);
            Part("Iris", pupilRoot, Vector3.zero, new Vector3(size * 0.46f, size * 0.46f, size * 0.2f), pupil, PrimitiveType.Sphere);
            Part("Catchlight", pupilRoot, new Vector3(size * 0.09f, size * 0.09f, size * 0.07f), Vector3.one * size * 0.14f, shine, PrimitiveType.Sphere);
            return new CharacterFace.Eye { Root = root, Pupil = pupilRoot, Range = size * 0.17f };
        }

        /// <summary>
        /// Two-segment noodle limb from capsules: pivot (shoulder/hip) → upper → joint (elbow/knee, bent by
        /// <paramref name="bend"/> degrees around X) → lower → end (hand/shoe). Sleeve/trouser renderers go into <paramref name="tint"/>.
        /// Returns the pivot (swung by CustomerVisual).
        /// </summary>
        private static Transform Limb(Transform parent, string name, Vector3 pivotPos, Quaternion pivotRot, float upperLen, float upperThick,
            float lowerLen, float lowerThick, float bend, Material mat, Material endMat, Vector3 endScale, Vector3 endOffset,
            System.Collections.Generic.List<Renderer> tint) => Limb(parent, name, pivotPos, pivotRot, upperLen, upperThick, lowerLen,
            lowerThick, bend, mat, endMat, endScale, endOffset, tint, out _);

        private static Transform Limb(Transform parent, string name, Vector3 pivotPos, Quaternion pivotRot, float upperLen, float upperThick,
            float lowerLen, float lowerThick, float bend, Material mat, Material endMat, Vector3 endScale, Vector3 endOffset,
            System.Collections.Generic.List<Renderer> tint, out Transform joint)
        {
            var pivot = Group(name, parent);
            pivot.localPosition = pivotPos;
            pivot.localRotation = pivotRot;
            var socket = Part($"{name}_Socket", pivot, Vector3.zero, Vector3.one * upperThick * 1.05f, mat, PrimitiveType.Sphere);
            tint?.Add(socket.GetComponent<Renderer>());
            var upper = Part($"{name}_Upper", pivot, new Vector3(0f, -upperLen * 0.5f, 0f), new Vector3(upperThick, upperLen * 0.5f, upperThick), mat, PrimitiveType.Capsule);

            joint = Group($"{name}_Joint", pivot);
            joint.localPosition = new Vector3(0f, -upperLen, 0f);
            joint.localRotation = Quaternion.Euler(bend, 0f, 0f);
            var lower = Part($"{name}_Lower", joint, new Vector3(0f, -lowerLen * 0.5f, 0f), new Vector3(lowerThick, lowerLen * 0.5f, lowerThick), mat, PrimitiveType.Capsule);
            Part($"{name}_End", joint, new Vector3(0f, -lowerLen, 0f) + endOffset, endScale, endMat, PrimitiveType.Sphere);

            tint?.Add(upper.GetComponent<Renderer>());
            tint?.Add(lower.GetComponent<Renderer>());
            return pivot;
        }

        private static GameObject FinishCustomerPrefab(GameObject root, Transform body, Transform[] floaters, float bubbleHeight,
            float bobSpeed, float squash, string path, CharacterFace.Eye[] eyes = null, Transform mouth = null, Renderer[] tint = null,
            Transform[] limbs = null, float[] limbPhases = null, float swingAngle = 28f, float swingSpeed = 6f,
            Transform[] joints = null, float[] jointBends = null, int[] jointModes = null)
        {
            var face = root.AddComponent<CharacterFace>();
            face.Configure(eyes ?? new CharacterFace.Eye[0], mouth);

            var bubbleGo = new GameObject("SpeechBubble");
            bubbleGo.transform.SetParent(root.transform, false);
            bubbleGo.transform.localPosition = new Vector3(0f, bubbleHeight, 0f);
            var text = bubbleGo.AddComponent<TextMeshPro>();
            text.fontSize = 1.6f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.rectTransform.sizeDelta = new Vector2(3.2f, 1f);
            var bubble = bubbleGo.AddComponent<SpeechBubble>();
            SetReference(bubble, "_text", text);

            var visual = root.AddComponent<CustomerVisual>();
            var vso = new SerializedObject(visual);
            vso.FindProperty("_body").objectReferenceValue = body;
            vso.FindProperty("_bobSpeed").floatValue = bobSpeed;
            vso.FindProperty("_squash").floatValue = squash;
            SetList(vso.FindProperty("_floaters"), floaters);
            vso.FindProperty("_face").objectReferenceValue = face;
            SetList(vso.FindProperty("_tintRenderers"), tint ?? new Renderer[0]);
            vso.ApplyModifiedPropertiesWithoutUndo();
            visual.ConfigureLimbs(limbs ?? new Transform[0], limbPhases ?? new float[0], swingAngle, swingSpeed, joints, jointBends, jointModes);

            var customer = root.AddComponent<Customer>();
            var cso = new SerializedObject(customer);
            cso.FindProperty("_bubble").objectReferenceValue = bubble;
            cso.FindProperty("_visual").objectReferenceValue = visual;
            cso.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static CustomerDefinition GetOrCreateCustomer(string assetName, string id, string species, string[] names, GameObject prefab,
            Vector2Int quantity, Vector2 tolerance, ItemQuality minQuality, bool likesPoor, int attempts, float patience,
            CustomerDefinition.DialogueLines lines, Color[] bodyColors = null, float bodyGlow = 0.12f)
        {
            string path = $"{CustomerDefinitionFolder}/{assetName}.asset";
            var def = AssetDatabase.LoadAssetAtPath<CustomerDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<CustomerDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }

            def.Configure(id, species, names, prefab, new[] { _glarbJuice }, quantity, tolerance, minQuality, likesPoor, attempts, patience, lines,
                bodyColors, bodyGlow);
            EditorUtility.SetDirty(def);
            return def;
        }

        private static void SetList(SerializedProperty list, params Object[] values)
        {
            list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
