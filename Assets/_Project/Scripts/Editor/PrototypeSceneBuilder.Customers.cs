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

            var spotMat = GetOrCreateMaterial("Plaza_TradeSpot", new Color(0.05f, 0.25f, 0.3f), 0.3f, new Color(0.1f, 0.9f, 1f) * 0.6f);
            var spotPositions = new[] { new Vector3(-6f, 0f, 38f), new Vector3(6.5f, 0f, 41f), new Vector3(-1f, 0f, 45.5f) };
            var spots = new Transform[spotPositions.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                var spot = new GameObject($"Trade Spot {i + 1}").transform;
                spot.SetParent(g, false);
                spot.position = spotPositions[i];
                var decal = Block("Marker", spot, spot.position + new Vector3(0f, 0.005f, 0f), new Vector3(1.1f, 0.01f, 1.1f), spotMat);
                Object.DestroyImmediate(decal.GetComponent<Collider>());
                decal.isStatic = false;
                spots[i] = spot;
            }

            CustomerDefinition blorb = CreateBlorbDefinition();
            CustomerDefinition gromflomite = CreateGromflomiteDefinition();

            var spawnerGo = new GameObject("Customer Spawner");
            spawnerGo.transform.SetParent(g, false);
            var spawner = spawnerGo.AddComponent<CustomerSpawner>();
            var so = new SerializedObject(spawner);
            SetList(so.FindProperty("_definitions"), blorb, gromflomite);
            so.FindProperty("_spawnPoint").objectReferenceValue = spawn;
            so.FindProperty("_exitPoint").objectReferenceValue = spawn;
            SetList(so.FindProperty("_meetingPoints"), spots);
            so.ApplyModifiedPropertiesWithoutUndo();
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
                prefab, new Vector2Int(1, 2), new Vector2(0.15f, 0.4f), ItemQuality.Standard, false, 2, 50f, lines);
        }

        private static GameObject BuildBlorbPrefab()
        {
            var bodyMat = GetOrCreateMaterial("Char_Blorb", new Color(0.85f, 0.35f, 0.9f), 0.85f, new Color(0.25f, 0.05f, 0.3f));
            var eyeMat = GetOrCreateMaterial("Char_EyeWhite", new Color(0.95f, 0.95f, 0.9f), 0.9f);
            var pupilMat = GetOrCreateMaterial("Char_Pupil", new Color(0.02f, 0.02f, 0.03f), 0.95f);
            var floaterMat = GetOrCreateMaterial("Char_BlorbFloater", new Color(0.9f, 0.6f, 1f), 0.8f, new Color(0.6f, 0.2f, 0.8f));

            var root = new GameObject("Customer_Blorb");
            ConfigureAgent(root, radius: 0.45f, height: 1.2f, speed: 1.6f);
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.6f, 0f);
            col.radius = 0.45f;
            col.height = 1.2f;

            var body = Group("Body", root.transform);
            Part("Jelly", body, new Vector3(0f, 0.5f, 0f), new Vector3(1.0f, 0.95f, 1.0f), bodyMat, PrimitiveType.Sphere);
            Eye(body, new Vector3(-0.2f, 0.72f, 0.4f), 0.22f, eyeMat, pupilMat);
            Eye(body, new Vector3(0.2f, 0.72f, 0.4f), 0.22f, eyeMat, pupilMat);
            Eye(body, new Vector3(0f, 0.92f, 0.34f), 0.17f, eyeMat, pupilMat);
            Part("Mouth", body, new Vector3(0f, 0.43f, 0.47f), new Vector3(0.3f, 0.07f, 0.08f), pupilMat, PrimitiveType.Sphere);

            var f1 = Part("Floater_1", root.transform, new Vector3(0.6f, 1.05f, 0f), Vector3.one * 0.14f, floaterMat, PrimitiveType.Sphere).transform;
            var f2 = Part("Floater_2", root.transform, new Vector3(-0.6f, 1.05f, 0f), Vector3.one * 0.12f, floaterMat, PrimitiveType.Sphere).transform;

            return FinishCustomerPrefab(root, body, new[] { f1, f2 }, bubbleHeight: 1.65f, bobSpeed: 7f, squash: 0.08f,
                $"{CharacterPrefabFolder}/Customer_Blorb.prefab");
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
                prefab, new Vector2Int(1, 1), new Vector2(0.05f, 0.2f), ItemQuality.Standard, false, 2, 40f, lines);
        }

        private static GameObject BuildGromflomitePrefab()
        {
            var suitMat = GetOrCreateMaterial("Char_GromSuit", new Color(0.18f, 0.22f, 0.32f), 0.4f);
            var skinMat = GetOrCreateMaterial("Char_GromSkin", new Color(0.4f, 0.55f, 0.25f), 0.5f);
            var eyeMat = GetOrCreateMaterial("Char_GromEye", new Color(0.25f, 0.02f, 0.05f), 0.95f, new Color(0.5f, 0.05f, 0.08f));
            var badgeMat = GetOrCreateMaterial("Char_Badge", new Color(0.9f, 0.75f, 0.2f), 0.7f, new Color(0.6f, 0.45f, 0.05f));

            var root = new GameObject("Customer_Gromflomite");
            ConfigureAgent(root, radius: 0.35f, height: 1.9f, speed: 1.9f);
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.radius = 0.35f;
            col.height = 1.9f;

            var body = Group("Body", root.transform);
            Part("Legs_L", body, new Vector3(-0.1f, 0.35f, 0f), new Vector3(0.08f, 0.7f, 0.08f), suitMat);
            Part("Legs_R", body, new Vector3(0.1f, 0.35f, 0f), new Vector3(0.08f, 0.7f, 0.08f), suitMat);
            Part("Torso", body, new Vector3(0f, 1.05f, 0f), new Vector3(0.45f, 0.4f, 0.32f), suitMat, PrimitiveType.Capsule);
            Part("Arm_L", body, new Vector3(-0.3f, 1.0f, 0.02f), new Vector3(0.06f, 0.55f, 0.06f), skinMat);
            Part("Arm_R", body, new Vector3(0.3f, 1.0f, 0.02f), new Vector3(0.06f, 0.55f, 0.06f), skinMat);
            Part("Neck", body, new Vector3(0f, 1.45f, 0f), new Vector3(0.07f, 0.12f, 0.07f), skinMat);
            Part("Head", body, new Vector3(0f, 1.62f, 0.02f), new Vector3(0.36f, 0.3f, 0.34f), skinMat, PrimitiveType.Sphere);
            Part("CompoundEye_L", body, new Vector3(-0.13f, 1.66f, 0.13f), new Vector3(0.18f, 0.22f, 0.14f), eyeMat, PrimitiveType.Sphere);
            Part("CompoundEye_R", body, new Vector3(0.13f, 1.66f, 0.13f), new Vector3(0.18f, 0.22f, 0.14f), eyeMat, PrimitiveType.Sphere);
            var antL = Part("Antenna_L", body, new Vector3(-0.08f, 1.88f, 0.02f), new Vector3(0.015f, 0.2f, 0.015f), skinMat);
            antL.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            var antR = Part("Antenna_R", body, new Vector3(0.08f, 1.88f, 0.02f), new Vector3(0.015f, 0.2f, 0.015f), skinMat);
            antR.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            Part("OversizedBadge", body, new Vector3(0.11f, 1.12f, 0.17f), new Vector3(0.16f, 0.2f, 0.02f), badgeMat);

            return FinishCustomerPrefab(root, body, new Transform[0], bubbleHeight: 2.2f, bobSpeed: 9f, squash: 0.015f,
                $"{CharacterPrefabFolder}/Customer_Gromflomite.prefab");
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

        private static void Eye(Transform parent, Vector3 position, float size, Material white, Material pupil)
        {
            Part("Eye", parent, position, Vector3.one * size, white, PrimitiveType.Sphere);
            Part("Pupil", parent, position + new Vector3(0f, 0f, size * 0.38f), Vector3.one * size * 0.45f, pupil, PrimitiveType.Sphere);
        }

        private static GameObject FinishCustomerPrefab(GameObject root, Transform body, Transform[] floaters, float bubbleHeight,
            float bobSpeed, float squash, string path)
        {
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
            vso.ApplyModifiedPropertiesWithoutUndo();

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
            CustomerDefinition.DialogueLines lines)
        {
            string path = $"{CustomerDefinitionFolder}/{assetName}.asset";
            var def = AssetDatabase.LoadAssetAtPath<CustomerDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<CustomerDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }

            def.Configure(id, species, names, prefab, new[] { _glarbJuice }, quantity, tolerance, minQuality, likesPoor, attempts, patience, lines);
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
