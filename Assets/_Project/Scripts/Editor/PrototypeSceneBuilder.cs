using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using VentsAndVending.Crafting;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;
using VentsAndVending.Player;
using VentsAndVending.UI;
using VentsAndVending.World;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Builds the game scene: the hideout and its service gallery (Hideout.cs), the plaza (Plaza.cs) and all systems.
    /// Re-running it regenerates the scene from scratch.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Prototype/Greybox_Movement.unity";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Greybox";
        private const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
        private const string ItemFolder = "Assets/_Project/ScriptableObjects/Items";
        private const string ItemPrefabFolder = "Assets/_Project/Prefabs/Items";

        private const string ProductFolder = "Assets/_Project/ScriptableObjects/Products";
        private const string RecipeFolder = "Assets/_Project/ScriptableObjects/Recipes";
        private const string MachinePrefabFolder = "Assets/_Project/Prefabs/Machines";
        private const string ParticleMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";

        private static ItemDefinition _glarb;
        private static ItemDefinition _lumina;
        private static ItemDefinition _stabiliser;
        private static ProductDefinition _glarbJuice;
        private static RecipeDefinition _glarbJuiceRecipe;

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
            EnsureFolder(ProductFolder);
            EnsureFolder(RecipeFolder);
            EnsureFolder(MachinePrefabFolder);
            EnsureFolder(Path.GetDirectoryName(ScenePath)!.Replace('\\', '/'));
            CreateItems();
            CreateProductsAndRecipes();

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
            BuildHideout(level, floorMat, wallMat, ventMat, accentMat, tunnelMat, glowMat);
            BuildInteractables(level, propMat, accentMat, ventMat);
            BuildMixingStation(level, wallMat, accentMat);
            BuildProps(level);
            BuildProgression(level, wallMat, accentMat);
            BuildPlazaDoorAndCustomers(BuildPlaza(level, wallMat, accentMat, ventMat, glowMat), wallMat, accentMat);
            BuildTunnelSlice(level, floorMat, wallMat, ventMat, accentMat, tunnelMat, glowMat);
            BuildAmbience(level);
            SoftenProps();
            BakeNavMesh(level.gameObject);
            BuildTunnelSystem();
            BuildSecurity();
            CreateSaveSystem();

            var player = CreatePlayer(PlayerSpawn, Quaternion.Euler(0f, PlayerSpawnYaw, 0f));
            CreateHud(player.GetComponent<InteractionController>());

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            return ScenePath;
        }

        // ------------------------------------------------------------------ Level

        private static void BuildLighting()
        {
            // Indoor station: no sky, dark void beyond the walls.
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.24f, 0.30f); // bright cool fill: cartoony shade, not black
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.08f);
            RenderSettings.fogDensity = 0.018f;

            var sun = new GameObject("Key Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.88f, 0.72f);
            sun.intensity = 0.6f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

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
            pockets.Configure("Pockets", 8, VentsAndVending.Progression.UpgradeKeys.PocketsCapacity, "player.pockets");
            var interaction = player.AddComponent<InteractionController>();
            player.AddComponent<VentsAndVending.Economy.PlayerWallet>();
            player.AddComponent<VentsAndVending.Security.PlayerExposure>();

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
            SetReference(hud.AddComponent<MixingPanel>(), "_interactor", interactor);
            SetReference(hud.AddComponent<SalePanel>(), "_interactor", interactor);
            SetReference(hud.AddComponent<UpgradePanel>(), "_interactor", interactor);
            SetReference(hud.AddComponent<PauseMenu>(), "_interactor", interactor);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        // ------------------------------------------------------------------ Items & interactables

        private static void CreateItems()
        {
            _glarb = GetOrCreateItem("Item_GlarbExtract", "glarb_extract", "Glarb Extract",
                "Thick neon-green biological extract. Smells faintly of regret and citrus.",
                10, 5, new Color(0.35f, 1f, 0.2f));
            _lumina = GetOrCreateItem("Item_LuminaGel", "lumina_gel", "Lumina Gel",
                "Light-sensitive synthetic gel. Glows brighter when you're watching it.",
                10, 4, new Color(0.1f, 0.95f, 1f));
            _stabiliser = GetOrCreateItem("Item_StabiliserGel", "stabiliser_gel", "Stabiliser Gel",
                "General-purpose stabilising gel. Keeps reactions from getting… creative.",
                10, 3, new Color(1f, 0.93f, 0.55f));
        }

        /// <summary>Product 001 Glarb-Juice (GDD §7.3) and its recipe. Ingredient values sum to the §12.4 cost of 12 CC.</summary>
        private static void CreateProductsAndRecipes()
        {
            string productPath = $"{ProductFolder}/Product_GlarbJuice.asset";
            _glarbJuice = AssetDatabase.LoadAssetAtPath<ProductDefinition>(productPath);
            if (_glarbJuice == null)
            {
                _glarbJuice = ScriptableObject.CreateInstance<ProductDefinition>();
                AssetDatabase.CreateAsset(_glarbJuice, productPath);
            }

            var juiceColor = new Color(0.25f, 1f, 0.15f);
            _glarbJuice.Configure("glarb_juice", "Glarb-Juice",
                "A suspiciously luminous green liquid extracted from a multi-eyed biological organism. Popular among interdimensional travellers who require an energy boost.",
                ItemCategory.Product, 10, 12, juiceColor);
            _glarbJuice.ConfigureProduct("Energy stimulant", ProductDifficulty.Beginner, DemandLevel.High, 12, 24);
            _glarbJuice.SetContraband(true);
            _glarbJuice.SetWorldPrefab(CreateItemPrefab(_glarbJuice, juiceColor, 1.35f));
            EditorUtility.SetDirty(_glarbJuice);

            string recipePath = $"{RecipeFolder}/Recipe_GlarbJuice.asset";
            _glarbJuiceRecipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(recipePath);
            if (_glarbJuiceRecipe == null)
            {
                _glarbJuiceRecipe = ScriptableObject.CreateInstance<RecipeDefinition>();
                AssetDatabase.CreateAsset(_glarbJuiceRecipe, recipePath);
            }

            _glarbJuiceRecipe.Configure("recipe_glarb_juice", _glarbJuice, 1, CraftingEquipment.MixingStation, 6f, 2.5f,
                (_glarb, 1), (_lumina, 1), (_stabiliser, 1));
            EditorUtility.SetDirty(_glarbJuiceRecipe);
        }

        /// <summary>
        /// Basic Mixing Station (GDD §6.2): workbench, vessel with liquid, motorised stirrer, progress gauge with the
        /// premium (green) and overheat (red) bands marked, a status lamp and a smoke burst. Saved as a prefab.
        /// </summary>
        private static void BuildMixingStation(Transform parent, Material metal, Material accent)
        {
            var liquidMat = GetOrCreateMaterial("Mix_Liquid", Color.gray, 0.9f, Color.gray);
            var gaugeMat = GetOrCreateMaterial("Mix_Gauge", Color.cyan, 0.5f, Color.cyan);
            var statusMat = GetOrCreateMaterial("Mix_Status", Color.gray, 0.5f, Color.gray);
            var greenMat = GetOrCreateMaterial("Mix_ZoneGreen", new Color(0.1f, 0.6f, 0.2f), 0.4f, new Color(0.2f, 1.5f, 0.4f));
            var redMat = GetOrCreateMaterial("Mix_ZoneRed", new Color(0.6f, 0.1f, 0.1f), 0.4f, new Color(1.5f, 0.15f, 0.1f));
            var darkMat = GetOrCreateMaterial("Mix_DarkMetal", new Color(0.08f, 0.08f, 0.09f), 0.6f);

            var root = new GameObject("Mixing Station");
            root.transform.SetParent(parent, false);
            root.transform.position = MixerPos;
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.8f, 0f);
            collider.size = new Vector3(1.45f, 1.6f, 0.75f);

            Transform shake = Group("Visuals", root.transform);

            Part("Bench", shake, new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.9f, 0.7f), metal);
            Part("BenchTop", shake, new Vector3(0f, 0.925f, 0f), new Vector3(1.45f, 0.05f, 0.75f), accent);

            // Vessel
            var vesselX = -0.25f;
            Part("VesselBase", shake, new Vector3(vesselX, 0.965f, 0f), new Vector3(0.44f, 0.015f, 0.44f), darkMat, PrimitiveType.Cylinder);
            Part("VesselRim", shake, new Vector3(vesselX, 1.58f, 0f), new Vector3(0.46f, 0.012f, 0.46f), darkMat, PrimitiveType.Cylinder);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                Part($"VesselPost_{i}", shake, new Vector3(vesselX + Mathf.Cos(a) * 0.2f, 1.27f, Mathf.Sin(a) * 0.2f), new Vector3(0.025f, 0.62f, 0.025f), darkMat);
            }

            var liquidPivot = Group("LiquidPivot", shake);
            liquidPivot.localPosition = new Vector3(vesselX, 0.98f, 0f);
            var liquid = Part("Liquid", liquidPivot, Vector3.zero, new Vector3(0.36f, 0.29f, 0.36f), liquidMat, PrimitiveType.Cylinder);
            liquid.transform.localPosition = new Vector3(0f, 0.29f, 0f);

            // Stirrer + motor
            Part("Motor", shake, new Vector3(vesselX, 1.66f, 0f), new Vector3(0.16f, 0.07f, 0.16f), metal, PrimitiveType.Cylinder);
            var stirrer = Group("Stirrer", shake);
            stirrer.localPosition = new Vector3(vesselX, 1.0f, 0f);
            var shaft = Part("Shaft", stirrer, Vector3.zero, new Vector3(0.025f, 0.62f, 0.025f), darkMat);
            shaft.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            var paddle = Part("Paddle", stirrer, Vector3.zero, new Vector3(0.26f, 0.07f, 0.02f), accent);
            paddle.transform.localPosition = new Vector3(0f, 0.08f, 0f);

            // Gauge (front faces +Z): 0 → AutoStopAt maps to 0 → 0.6 m
            const float gaugeX = 0.35f, gaugeBottom = 0.97f, gaugeHeight = 0.6f, gaugeFront = 0.05f;
            Part("GaugeBack", shake, new Vector3(gaugeX, gaugeBottom + gaugeHeight * 0.5f, 0f), new Vector3(0.14f, gaugeHeight + 0.04f, 0.08f), darkMat);
            float Y(float progress) => gaugeBottom + gaugeHeight * (progress / MixingRules.AutoStopAt);
            var greenBand = Part("ZoneGreen", shake, new Vector3(gaugeX - 0.085f, (Y(MixingRules.PremiumFrom) + Y(MixingRules.PremiumTo)) * 0.5f, gaugeFront),
                new Vector3(0.03f, Y(MixingRules.PremiumTo) - Y(MixingRules.PremiumFrom), 0.03f), greenMat);
            Part("ZoneRed", shake, new Vector3(gaugeX - 0.085f, (Y(MixingRules.OverheatFrom) + Y(MixingRules.AutoStopAt)) * 0.5f, gaugeFront),
                new Vector3(0.03f, Y(MixingRules.AutoStopAt) - Y(MixingRules.OverheatFrom), 0.03f), redMat);
            var gaugePivot = Group("GaugeFillPivot", shake);
            gaugePivot.localPosition = new Vector3(gaugeX, gaugeBottom, gaugeFront);
            var gaugeFill = Part("GaugeFill", gaugePivot, Vector3.zero, new Vector3(0.08f, gaugeHeight, 0.02f), gaugeMat);
            gaugeFill.transform.localPosition = new Vector3(0f, gaugeHeight * 0.5f, 0f);

            // Status lamp
            var lamp = Part("StatusLamp", shake, new Vector3(0.58f, 1.0f, 0.24f), Vector3.one * 0.08f, statusMat, PrimitiveType.Sphere);

            // Smoke burst
            var smokeGo = new GameObject("Smoke");
            smokeGo.transform.SetParent(shake, false);
            smokeGo.transform.localPosition = new Vector3(vesselX, 1.6f, 0f);
            smokeGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var smoke = smokeGo.AddComponent<ParticleSystem>();
            ConfigureSmoke(smoke);

            var station = root.AddComponent<MixingStation>();
            station.ConfigureRecipes(new[] { _glarbJuiceRecipe });
            var so = new SerializedObject(station);
            so.FindProperty("_liquidPivot").objectReferenceValue = liquidPivot;
            so.FindProperty("_liquidRenderer").objectReferenceValue = liquid.GetComponent<Renderer>();
            so.FindProperty("_stirrer").objectReferenceValue = stirrer;
            so.FindProperty("_gaugeFillPivot").objectReferenceValue = gaugePivot;
            so.FindProperty("_gaugeFillRenderer").objectReferenceValue = gaugeFill.GetComponent<Renderer>();
            so.FindProperty("_statusLight").objectReferenceValue = lamp.GetComponent<Renderer>();
            so.FindProperty("_smoke").objectReferenceValue = smoke;
            so.FindProperty("_shakeRoot").objectReferenceValue = shake;
            so.FindProperty("_premiumBand").objectReferenceValue = greenBand.transform;
            so.FindProperty("_gaugeBottom").floatValue = gaugeBottom;
            so.FindProperty("_gaugeHeight").floatValue = gaugeHeight;
            so.ApplyModifiedPropertiesWithoutUndo();

            CreatePointLight("Mixing Lamp", root.transform.position + new Vector3(-0.25f, 2.2f, 0.4f), new Color(0.6f, 1f, 0.7f), 4f, 1.2f, root.transform);

            PrefabUtility.SaveAsPrefabAssetAndConnect(root, $"{MachinePrefabFolder}/MixingStation.prefab", InteractionMode.AutomatedAction);
        }

        /// <summary>A primitive with no collider (the station root has one box collider for interaction).</summary>
        private static GameObject Part(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material mat,
            PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>GDD §7.6: "the machine releases colourful smoke".</summary>
        private static void ConfigureSmoke(ParticleSystem ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.2f, 0.85f, 0.9f), new Color(0.3f, 1f, 0.3f, 0.9f));
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45), new ParticleSystem.Burst(0.25f, 25) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.12f;

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f, 0.4f, 1f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = fade;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            }
            renderer.sharedMaterial = mat;
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
            if (!(item is ProductDefinition))
            {
                item.SetWorldPrefab(CreateItemPrefab(item, color));
            }
            EditorUtility.SetDirty(item);
            return item;
        }

        /// <summary>Greybox vial: glowing cylinder with a dark cap, physics, and a PickupItem.</summary>
        private static GameObject CreateItemPrefab(ItemDefinition item, Color color, float scale = 1f)
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
            root.transform.localScale = Vector3.one * scale;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
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
                ("Inventory", new[] { ("<Keyboard>/tab", "Keyboard&Mouse"), ("<Keyboard>/i", "Keyboard&Mouse"), ("<Gamepad>/select", "Gamepad") }),
                ("Pause", new[] { ("<Keyboard>/escape", "Keyboard&Mouse"), ("<Gamepad>/start", "Gamepad") })
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

        /// <summary>Greybox surface names → art library materials (Docs/Design/ArtStyleGuide.md). Emissives/characters stay URP Lit.</summary>
        private static readonly System.Collections.Generic.Dictionary<string, string> LibraryRemap = new System.Collections.Generic.Dictionary<string, string>
        {
            { "Greybox_Floor", "M_Concrete_Dirty" },
            { "Greybox_Wall", "M_PaintedMetal_Wall" },
            { "Greybox_Vent", "M_DuctMetal" },
            { "Greybox_Accent", "M_HazardPaint" },
            { "Greybox_Prop", "M_WorkshopOrange" },
            { "Greybox_Tunnel", "M_GreenPanel" },
            { "Mix_DarkMetal", "M_DarkMetal" },
            { "Drone_Body", "M_DarkMetal" },
            { "Plaza_Floor", "M_PlazaTile" },
            { "Plaza_Wall", "M_PlazaPanel" },
            { "Plaza_Silver", "M_BrushedSilver" },
        };

        private static Material GetOrCreateMaterial(string name, Color baseColor, float smoothness, Color? emission = null)
        {
            if (!emission.HasValue && LibraryRemap.TryGetValue(name, out string libraryName))
            {
                Material library = ArtLibrary.Get(libraryName);
                if (library != null)
                {
                    return library;
                }
            }

            // Everything else (characters, props, neon, screens) uses the same stylized shader as a plain toon material:
            // flat colour, no texture or wear, optional emission — so the whole game shares one lighting model.
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader toon = ArtLibrary.Shader;
            if (mat == null)
            {
                mat = new Material(toon);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != toon)
            {
                mat.shader = toon;
            }

            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            mat.SetTexture("_BaseMap", null);
            mat.SetFloat("_TextureStrength", 0f);
            mat.SetTexture("_BumpMap", null);
            mat.SetFloat("_BumpScale", 0f);
            mat.SetFloat("_SeamDarkness", 0f);
            mat.SetFloat("_GrimeAmount", 0f);
            mat.SetFloat("_GrimePatches", 0f);
            mat.SetFloat("_RustAmount", 0f);
            mat.SetFloat("_EdgeWear", 0f);
            mat.SetColor("_EmissionColor", emission ?? Color.black);
            mat.globalIlluminationFlags = emission.HasValue ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            ArtLibrary.ApplyToonLook(mat);

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
