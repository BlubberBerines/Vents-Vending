using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;
using VentsAndVending.Player;
using VentsAndVending.UI;
using VentsAndVending.World;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Builds the movement greybox scene: open floor, jump steps, a ramp, a crouch tunnel and an L-shaped vent duct.
    /// Re-running it regenerates the scene from scratch.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Prototype/Greybox_Movement.unity";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Greybox";
        private const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
        private const string ItemFolder = "Assets/_Project/ScriptableObjects/Items";
        private const string ItemPrefabFolder = "Assets/_Project/Prefabs/Items";

        private static ItemDefinition _glarb;
        private static ItemDefinition _lumina;
        private static ItemDefinition _stabiliser;

        // Maintenance palette (GDD §15.4)
        private static readonly Color DarkGrey = new Color(0.16f, 0.17f, 0.18f);
        private static readonly Color IndustrialBlue = new Color(0.22f, 0.29f, 0.36f);
        private static readonly Color RustBrown = new Color(0.40f, 0.24f, 0.15f);
        private static readonly Color DirtyYellow = new Color(0.62f, 0.53f, 0.22f);
        private static readonly Color MutedOrange = new Color(0.66f, 0.38f, 0.18f);
        private static readonly Color DesatGreen = new Color(0.30f, 0.38f, 0.30f);

        [MenuItem("Vents & Vending/Prototype/Build Movement Greybox Scene")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Build();
            EditorUtility.DisplayDialog("Greybox built",
                $"Scene saved to {ScenePath}.\nPress Play. WASD move, Mouse look, Shift sprint, Space jump, C crouch, Z/Ctrl crawl, E interact, Tab inventory, Esc unlock cursor.",
                "OK");
        }

        /// <summary>Builds and saves the scene. Returns the scene path.</summary>
        public static string Build()
        {
            EnsureInputActions();
            EnsureFolder(MaterialFolder);
            EnsureFolder(ItemFolder);
            EnsureFolder(ItemPrefabFolder);
            EnsureFolder(Path.GetDirectoryName(ScenePath)!.Replace('\\', '/'));
            CreateItems();

            var floorMat = GetOrCreateMaterial("Greybox_Floor", DarkGrey, 0.25f);
            var wallMat = GetOrCreateMaterial("Greybox_Wall", IndustrialBlue, 0.2f);
            var ventMat = GetOrCreateMaterial("Greybox_Vent", RustBrown, 0.35f);
            var accentMat = GetOrCreateMaterial("Greybox_Accent", DirtyYellow, 0.3f);
            var propMat = GetOrCreateMaterial("Greybox_Prop", MutedOrange, 0.2f);
            var tunnelMat = GetOrCreateMaterial("Greybox_Tunnel", DesatGreen, 0.2f);
            var glowMat = GetOrCreateMaterial("Greybox_VentLight", Color.black, 0.5f, new Color(1f, 0.75f, 0.35f) * 2.5f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();

            var level = new GameObject("Level").transform;
            BuildRoom(level, floorMat, wallMat);
            BuildJumpSteps(level, propMat, accentMat);
            BuildRamp(level, accentMat);
            BuildCrouchTunnel(level, tunnelMat, accentMat);
            BuildVentDuct(level, ventMat, accentMat, glowMat);
            BuildInteractables(level, propMat, accentMat, ventMat);

            var player = CreatePlayer(new Vector3(0f, 0f, -14f), Quaternion.identity);
            CreateHud(player.GetComponent<InteractionController>());

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            return ScenePath;
        }

        // ------------------------------------------------------------------ Level

        private static void BuildLighting()
        {
            // Indoor station: no sky, dark void beyond the walls.
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.11f, 0.13f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.08f);
            RenderSettings.fogDensity = 0.03f;

            var sun = new GameObject("Key Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.88f, 0.72f);
            sun.intensity = 0.6f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            CreatePointLight("Neon Cyan", new Vector3(-12f, 3.2f, 8f), new Color(0.1f, 0.9f, 1f), 8f, 3f);
            CreatePointLight("Neon Magenta", new Vector3(0f, 3.2f, 12f), new Color(1f, 0.15f, 0.8f), 8f, 3f);
            CreatePointLight("Work Lamp", new Vector3(4f, 3f, -8f), new Color(1f, 0.8f, 0.5f), 7f, 2f);
        }

        private static void BuildRoom(Transform parent, Material floor, Material wall)
        {
            var room = Group("Room", parent);
            Block("Floor", room, new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f), floor);
            Block("Wall_N", room, new Vector3(0f, 2f, 20.25f), new Vector3(41f, 4f, 0.5f), wall);
            Block("Wall_S", room, new Vector3(0f, 2f, -20.25f), new Vector3(41f, 4f, 0.5f), wall);
            Block("Wall_E", room, new Vector3(20.25f, 2f, 0f), new Vector3(0.5f, 4f, 40f), wall);
            Block("Wall_W", room, new Vector3(-20.25f, 2f, 0f), new Vector3(0.5f, 4f, 40f), wall);
        }

        /// <summary>Steps rising 0.4 m each — taller than the step offset, so they need jumping.</summary>
        private static void BuildJumpSteps(Transform parent, Material prop, Material accent)
        {
            var g = Group("JumpSteps", parent);
            for (int i = 0; i < 4; i++)
            {
                float h = 0.4f * (i + 1);
                Block($"Step_{i + 1}", g, new Vector3(-12f, h * 0.5f, -10f + i * 1.5f), new Vector3(2f, h, 1.5f), i % 2 == 0 ? prop : accent);
            }

            Block("Crate_A", g, new Vector3(-8f, 0.5f, -12f), Vector3.one, prop);
            Block("Crate_B", g, new Vector3(-7f, 0.35f, -12.5f), Vector3.one * 0.7f, accent);
        }

        private static void BuildRamp(Transform parent, Material mat)
        {
            var g = Group("Ramp", parent);
            var ramp = Block("Ramp_20deg", g, new Vector3(-5f, 1.1f, -2f), new Vector3(3f, 0.2f, 7f), mat);
            ramp.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);
            Block("Ramp_Top", g, new Vector3(-5f, 1.1f, 2.5f), new Vector3(3f, 2.2f, 2f), mat);
        }

        /// <summary>1.3 m clearance: too low to stand (1.8 m), fine to crouch (1.2 m).</summary>
        private static void BuildCrouchTunnel(Transform parent, Material mat, Material accent)
        {
            var g = Group("CrouchTunnel", parent);
            const float x = 4f, z0 = -6f, len = 6f, w = 1.6f, h = 1.3f, t = 0.2f;
            float zc = z0 + len * 0.5f;
            Block("Wall_L", g, new Vector3(x - w * 0.5f - t * 0.5f, h * 0.5f, zc), new Vector3(t, h, len), mat);
            Block("Wall_R", g, new Vector3(x + w * 0.5f + t * 0.5f, h * 0.5f, zc), new Vector3(t, h, len), mat);
            Block("Roof", g, new Vector3(x, h + t * 0.5f, zc), new Vector3(w + t * 2f, t, len), mat);
            Block("Frame", g, new Vector3(x, h + t + 0.05f, z0 + 0.1f), new Vector3(w + t * 2f, 0.1f, 0.2f), accent);
        }

        /// <summary>
        /// L-shaped duct, 0.9 m wide and 0.85 m tall: only crawlable (0.7 m). Vent triggers force crawling and
        /// reach 0.6 m outside each opening so the player drops into a crawl as they enter.
        /// </summary>
        private static void BuildVentDuct(Transform parent, Material vent, Material accent, Material glow)
        {
            var g = Group("VentDuct", parent);
            const float x = 12f, zStart = -6f, zTurn = 4f, xEnd = 18f;
            const float w = 0.9f, h = 0.85f, t = 0.1f, half = w * 0.5f;

            // Segment 1 — along +Z
            float seg1End = zTurn + half + t;
            float seg1Len = seg1End - zStart;
            Block("Seg1_Wall_L", g, new Vector3(x - half - t * 0.5f, h * 0.5f, zStart + seg1Len * 0.5f), new Vector3(t, h, seg1Len), vent);
            float rightLen = (zTurn - half) - zStart;
            Block("Seg1_Wall_R", g, new Vector3(x + half + t * 0.5f, h * 0.5f, zStart + rightLen * 0.5f), new Vector3(t, h, rightLen), vent);
            Block("Seg1_Roof", g, new Vector3(x, h + t * 0.5f, zStart + seg1Len * 0.5f), new Vector3(w + t * 2f, t, seg1Len), vent);
            Block("Seg1_EndCap", g, new Vector3(x, h * 0.5f, zTurn + half + t * 0.5f), new Vector3(w + t * 2f, h, t), vent);

            // Segment 2 — along +X
            float seg2Start = x + half;
            float seg2Len = xEnd - seg2Start;
            Block("Seg2_Wall_Back", g, new Vector3(seg2Start + t + (seg2Len - t) * 0.5f, h * 0.5f, zTurn + half + t * 0.5f), new Vector3(seg2Len - t, h, t), vent);
            Block("Seg2_Wall_Front", g, new Vector3(seg2Start + seg2Len * 0.5f, h * 0.5f, zTurn - half - t * 0.5f), new Vector3(seg2Len, h, t), vent);
            Block("Seg2_Roof", g, new Vector3(seg2Start + seg2Len * 0.5f, h + t * 0.5f, zTurn), new Vector3(seg2Len, t, w + t * 2f), vent);

            // Hatch frames at both openings
            Block("Hatch_Entry", g, new Vector3(x, h + t + 0.05f, zStart + 0.05f), new Vector3(w + t * 2f + 0.2f, 0.1f, 0.1f), accent);
            Block("Hatch_Exit", g, new Vector3(xEnd - 0.05f, h + t + 0.05f, zTurn), new Vector3(0.1f, 0.1f, w + t * 2f + 0.2f), accent);

            // Maintenance light strips + a dim warm light inside
            for (int i = 0; i < 3; i++)
            {
                var strip = Block($"LightStrip_{i}", g, new Vector3(x, h - 0.01f, zStart + 2f + i * 3.5f), new Vector3(0.3f, 0.02f, 0.6f), glow);
                Object.DestroyImmediate(strip.GetComponent<Collider>());
            }
            CreatePointLight("Vent Light A", new Vector3(x, 0.7f, -1f), new Color(1f, 0.7f, 0.35f), 3f, 1.2f, g);
            CreatePointLight("Vent Light B", new Vector3(15f, 0.7f, zTurn), new Color(1f, 0.7f, 0.35f), 3f, 1.2f, g);

            // Vent zones (triggers)
            const float lip = 0.6f;
            VentTrigger("VentZone_Seg1", g, new Vector3(x, h * 0.5f, (zStart - lip + zTurn + half) * 0.5f), new Vector3(w, h, (zTurn + half) - (zStart - lip)), "duct_a");
            VentTrigger("VentZone_Seg2", g, new Vector3((seg2Start + xEnd + lip) * 0.5f, h * 0.5f, zTurn), new Vector3((xEnd + lip) - seg2Start, h, w), "duct_a");
        }

        // ------------------------------------------------------------------ Player

        private static GameObject CreatePlayer(Vector3 position, Quaternion rotation)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.SetPositionAndRotation(position, rotation);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;
            cc.skinWidth = 0.04f;
            cc.minMoveDistance = 0f;

            player.AddComponent<PlayerInputHandler>();
            player.AddComponent<PlayerStateController>();
            player.AddComponent<VentCrawler>();
            var crouch = player.AddComponent<PlayerCrouch>();
            var look = player.AddComponent<PlayerCamera>();
            player.AddComponent<PlayerMovement>();
            var pockets = player.AddComponent<InventoryComponent>();
            pockets.Configure("Pockets", 8);
            var interaction = player.AddComponent<InteractionController>();

            var cameraRoot = new GameObject("CameraRoot").transform;
            cameraRoot.SetParent(player.transform, false);
            cameraRoot.localPosition = new Vector3(0f, 1.65f, 0f);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(cameraRoot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            camGo.AddComponent<AudioListener>();

            SetReference(crouch, "_cameraRoot", cameraRoot);
            SetReference(look, "_pitchRoot", cameraRoot);
            SetReference(interaction, "_camera", cam);
            return player;
        }

        private static void CreateHud(InteractionController interactor)
        {
            var hud = new GameObject("HUD");
            SetReference(hud.AddComponent<HudController>(), "_interactor", interactor);
            SetReference(hud.AddComponent<InventoryScreen>(), "_interactor", interactor);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        // ------------------------------------------------------------------ Items & interactables

        private static void CreateItems()
        {
            _glarb = GetOrCreateItem("Item_GlarbExtract", "glarb_extract", "Glarb Extract",
                "Thick neon-green biological extract. Smells faintly of regret and citrus.",
                10, 4, new Color(0.35f, 1f, 0.2f));
            _lumina = GetOrCreateItem("Item_LuminaGel", "lumina_gel", "Lumina Gel",
                "Light-sensitive synthetic gel. Glows brighter when you're watching it.",
                10, 3, new Color(0.1f, 0.95f, 1f));
            _stabiliser = GetOrCreateItem("Item_StabiliserGel", "stabiliser_gel", "Stabiliser Gel",
                "General-purpose stabilising gel. Keeps reactions from getting… creative.",
                10, 2, new Color(1f, 0.93f, 0.55f));
        }

        private static ItemDefinition GetOrCreateItem(string assetName, string id, string displayName, string description,
            int maxStack, int baseValue, Color color)
        {
            string path = $"{ItemFolder}/{assetName}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }

            item.Configure(id, displayName, description, ItemCategory.Ingredient, maxStack, baseValue, color);
            item.SetWorldPrefab(CreateItemPrefab(item, color));
            EditorUtility.SetDirty(item);
            return item;
        }

        /// <summary>Greybox vial: glowing cylinder with a dark cap, physics, and a PickupItem.</summary>
        private static GameObject CreateItemPrefab(ItemDefinition item, Color color)
        {
            string path = $"{ItemPrefabFolder}/Pickup_{item.Id}.prefab";
            var liquid = GetOrCreateMaterial($"Item_{item.Id}", color * 0.6f, 0.8f, color * 1.5f);
            var cap = GetOrCreateMaterial("Item_Cap", new Color(0.12f, 0.12f, 0.13f), 0.4f);

            var root = new GameObject($"Pickup_{item.Id}");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Vial";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.12f, 0.1f, 0.12f);
            body.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            body.GetComponent<MeshRenderer>().sharedMaterial = liquid;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            var lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lid.name = "Cap";
            lid.transform.SetParent(root.transform, false);
            lid.transform.localScale = new Vector3(0.13f, 0.02f, 0.13f);
            lid.transform.localPosition = new Vector3(0f, 0.215f, 0f);
            lid.GetComponent<MeshRenderer>().sharedMaterial = cap;
            Object.DestroyImmediate(lid.GetComponent<Collider>());

            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.12f, 0f);
            box.size = new Vector3(0.13f, 0.24f, 0.13f);
            var body3d = root.AddComponent<Rigidbody>();
            body3d.mass = 0.3f;
            body3d.interpolation = RigidbodyInterpolation.Interpolate;
            body3d.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            root.AddComponent<PickupItem>().Configure(item, 1);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildInteractables(Transform parent, Material prop, Material accent, Material vent)
        {
            var g = Group("Interactables", parent);

            // Storage locker by the spawn, pre-stocked
            var locker = Block("Storage Locker", g, new Vector3(-2.5f, 0.9f, -16.5f), new Vector3(1.2f, 1.8f, 0.6f), prop);
            locker.isStatic = false;
            Block("Locker_Trim", g, new Vector3(-2.5f, 1.82f, -16.5f), new Vector3(1.25f, 0.05f, 0.65f), accent);
            var lockerInv = locker.AddComponent<InventoryComponent>();
            lockerInv.Configure("Storage Locker", 8);
            lockerInv.AddStartingItem(_lumina, 4);
            lockerInv.AddStartingItem(_stabiliser, 3);
            locker.AddComponent<StorageContainer>();

            // Loose ingredients near the spawn
            PlacePickup(_glarb, 2, new Vector3(-8f, 1.02f, -12f), g);   // on Crate_A
            PlacePickup(_lumina, 1, new Vector3(1.5f, 0.02f, -11.5f), g);
            PlacePickup(_stabiliser, 1, new Vector3(2.0f, 0.02f, -11.2f), g);

            // Hidden stash at the bend of the vent duct (reward for crawling in)
            PlacePickup(_glarb, 5, new Vector3(12.05f, 0.02f, 3.85f), g);
            PlacePickup(_stabiliser, 2, new Vector3(12.25f, 0.02f, 4.1f), g);

            // Hinged hatch over the vent entrance (duct: x=12, z=-6, 0.9 x 0.85 m)
            var hatch = new GameObject("Vent Hatch");
            hatch.transform.SetParent(g, false);
            hatch.transform.position = new Vector3(12f, 0.85f, -6.04f);
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(hatch.transform, false);
            var panel = Block("Panel", pivot, Vector3.zero, new Vector3(0.94f, 0.85f, 0.04f), vent);
            panel.isStatic = false;
            panel.transform.localPosition = new Vector3(0f, -0.425f, 0f);
            var grille = Block("Grille", pivot, Vector3.zero, new Vector3(0.6f, 0.3f, 0.02f), accent);
            grille.isStatic = false;
            grille.transform.localPosition = new Vector3(0f, -0.45f, -0.025f);
            Object.DestroyImmediate(grille.GetComponent<Collider>());
            SetReference(hatch.AddComponent<VentHatch>(), "_pivot", pivot);
        }

        private static void PlacePickup(ItemDefinition item, int count, Vector3 position, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(item.WorldPrefab);
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var so = new SerializedObject(go.GetComponent<PickupItem>());
            so.FindProperty("_count").intValue = count;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ Input

        /// <summary>
        /// Adds game-specific actions to the Player map if missing:
        /// Crawl (Z / Left Ctrl / right-stick press) and Inventory (Tab / I / gamepad Select).
        /// </summary>
        public static void EnsureInputActions()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (asset == null)
            {
                Debug.LogWarning($"[PrototypeSceneBuilder] Input actions not found at {InputActionsPath}");
                return;
            }

            var required = new (string name, (string path, string group)[] bindings)[]
            {
                ("Crawl", new[] { ("<Keyboard>/z", "Keyboard&Mouse"), ("<Keyboard>/leftCtrl", "Keyboard&Mouse"), ("<Gamepad>/rightStickPress", "Gamepad") }),
                ("Inventory", new[] { ("<Keyboard>/tab", "Keyboard&Mouse"), ("<Keyboard>/i", "Keyboard&Mouse"), ("<Gamepad>/select", "Gamepad") })
            };

            InputActionMap existing = asset.FindActionMap("Player", throwIfNotFound: true);
            if (required.All(r => existing.FindAction(r.name) != null))
            {
                return;
            }

            // Project-wide actions stay enabled in the Editor and can't be modified in place, so edit a copy.
            var copy = InputActionAsset.FromJson(asset.ToJson());
            try
            {
                InputActionMap map = copy.FindActionMap("Player", throwIfNotFound: true);
                foreach (var (name, bindings) in required)
                {
                    if (map.FindAction(name) != null)
                    {
                        continue;
                    }

                    InputAction action = map.AddAction(name, InputActionType.Button);
                    foreach (var (path, group) in bindings)
                    {
                        action.AddBinding(path, groups: group);
                    }
                    Debug.Log($"[PrototypeSceneBuilder] Added '{name}' action to the Player input map.");
                }

                File.WriteAllText(InputActionsPath, copy.ToJson());
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }

            AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceUpdate);
        }

        // ------------------------------------------------------------------ Helpers

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Block(string name, Transform parent, Vector3 center, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        private static void VentTrigger(string name, Transform parent, Vector3 center, Vector3 size, string sectionId)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var zone = go.AddComponent<VentZone>();
            var so = new SerializedObject(zone);
            so.FindProperty("_sectionId").stringValue = sectionId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePointLight(string name, Vector3 position, Color color, float range, float intensity, Transform parent = null)
        {
            var light = new GameObject(name).AddComponent<Light>();
            if (parent != null)
            {
                light.transform.SetParent(parent, false);
            }
            light.transform.position = position;
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        private static Material GetOrCreateMaterial(string name, Color baseColor, float smoothness, Color? emission = null)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path))
            {
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
