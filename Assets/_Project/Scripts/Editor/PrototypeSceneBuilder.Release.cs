using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using VentsAndVending.Customers;
using VentsAndVending.Saving;
using VentsAndVending.UI;
using VentsAndVending.World;

namespace VentsAndVending.Editor
{
    /// <summary>Main menu scene, save system wiring, build settings and the Windows player build (Appendix B #25).</summary>
    public static partial class PrototypeSceneBuilder
    {
        public const string MainMenuPath = "Assets/_Project/Scenes/MainMenu/MainMenu.unity";
        private const string DatabasePath = "Assets/_Project/ScriptableObjects/GameDatabase.asset";
        private const string BuildFolder = "Builds/Windows";
        private const string ExeName = "VentsAndVending.exe";
        public const string BuildReportPath = "Builds/build_report.txt";

        private static System.Collections.Generic.List<VentsAndVending.Progression.UpgradeDefinition> _upgradeCatalogue;

        [MenuItem("Vents & Vending/Prototype/Build All Scenes")]
        public static void BuildAllScenes()
        {
            UiThemeBuilder.Build();
            Build();
            BuildMainMenuScene();
            SetBuildScenes();
            EditorSceneManager.OpenScene(ScenePath);
        }

        // ------------------------------------------------------------------ Save system (game scene)

        private static void CreateSaveSystem()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<GameDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.Configure(new Inventory.ItemDefinition[] { _glarb, _lumina, _stabiliser, _glarbJuice }, _upgradeCatalogue);
            EditorUtility.SetDirty(database);

            var go = new GameObject("Save System");
            go.AddComponent<SaveManager>().SetDatabase(database);
        }

        // ------------------------------------------------------------------ Main menu scene

        public static void BuildMainMenuScene()
        {
            EnsureFolder(Path.GetDirectoryName(MainMenuPath)!.Replace('\\', '/'));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.19f, 0.27f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.06f);
            RenderSettings.fogDensity = 0.06f;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 1.45f, -4.2f), Quaternion.Euler(6f, 8f, 0f));
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            camGo.AddComponent<AudioListener>();

            var floorMat = GetOrCreateMaterial("Plaza_Floor", new Color(0.22f, 0.23f, 0.28f), 0.65f);
            var cyan = GetOrCreateMaterial("Plaza_NeonCyan", new Color(0.02f, 0.2f, 0.25f), 0.6f, new Color(0.2f, 1.6f, 2f));
            var magenta = GetOrCreateMaterial("Plaza_NeonMagenta", new Color(0.25f, 0.02f, 0.2f), 0.6f, new Color(2f, 0.2f, 1.5f));
            var wall = GetOrCreateMaterial("Plaza_Wall", new Color(0.17f, 0.14f, 0.28f), 0.45f);

            var set = new GameObject("Diorama").transform;
            Block("Floor", set, new Vector3(0f, -0.5f, 2f), new Vector3(20f, 1f, 14f), floorMat);
            Block("BackWall", set, new Vector3(0f, 3f, 6f), new Vector3(20f, 6f, 0.5f), wall);
            NoCollider(Block("Neon_1", set, new Vector3(0f, 3.6f, 5.7f), new Vector3(14f, 0.07f, 0.05f), magenta));
            NoCollider(Block("Neon_2", set, new Vector3(0f, 0.6f, 5.7f), new Vector3(14f, 0.05f, 0.05f), cyan));
            Block("Pedestal", set, new Vector3(2.6f, 0.25f, 2.2f), new Vector3(1.2f, 0.5f, 1.2f), wall);
            var ring = Part("HoloRing", set, new Vector3(2.6f, 2.2f, 2.2f), new Vector3(1.6f, 0.03f, 1.6f), cyan, PrimitiveType.Cylinder);
            ring.AddComponent<Spinner>().Configure(new Vector3(0f, 35f, 18f), 0f, 1f);

            Display("Customer_Blorb", new Vector3(1.7f, 0f, 1.2f), 200f);
            Display("Customer_Gromflomite", new Vector3(3.4f, 0f, 1.6f), 160f);

            CreatePointLight("Key Magenta", new Vector3(-1f, 3f, 0f), new Color(1f, 0.3f, 0.85f), 10f, 3f);
            CreatePointLight("Key Cyan", new Vector3(4.5f, 2.5f, -0.5f), new Color(0.2f, 0.9f, 1f), 10f, 3f);
            CreatePointLight("Rim", new Vector3(2.5f, 3f, 4f), new Color(0.7f, 0.5f, 1f), 8f, 2f);

            new GameObject("Main Menu").AddComponent<MainMenuController>();
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            EditorSceneManager.SaveScene(scene, MainMenuPath);
        }

        /// <summary>Customer prefab as a static display: no AI, no NavMesh agent, no speech bubble.</summary>
        private static void Display(string prefabName, Vector3 position, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharacterPrefabFolder}/{prefabName}.prefab");
            if (prefab == null)
            {
                return;
            }

            var go = Object.Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f));
            go.name = prefabName.Replace("Customer_", "Display_");
            Object.DestroyImmediate(go.GetComponent<Customer>());
            Object.DestroyImmediate(go.GetComponent<UnityEngine.AI.NavMeshAgent>());
            var bubble = go.GetComponentInChildren<SpeechBubble>();
            if (bubble != null)
            {
                Object.DestroyImmediate(bubble.gameObject);
            }
        }

        private static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        // ------------------------------------------------------------------ Player build

        [MenuItem("Vents & Vending/Build/Windows Player (Mono)")]
        public static void BuildWindowsFromMenu()
        {
            string result = BuildWindows();
            EditorUtility.DisplayDialog("Windows build", result, "OK");
        }

        /// <summary>Queues the build for the next editor tick (lets callers return first) and writes a report file when done.</summary>
        public static void BuildWindowsDeferred()
        {
            if (File.Exists(BuildReportPath))
            {
                File.Delete(BuildReportPath);
            }

            EditorApplication.delayCall += () =>
            {
                string result;
                try
                {
                    result = BuildWindows();
                }
                catch (System.Exception e)
                {
                    result = $"EXCEPTION {e}";
                }
                Directory.CreateDirectory(Path.GetDirectoryName(BuildReportPath)!);
                File.WriteAllText(BuildReportPath, result);
            };
        }

        public static string BuildWindows()
        {
            BuildAllScenes();

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = true;

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = Path.Combine(BuildFolder, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            string errors = string.Join("\n", report.steps.SelectMany(step => step.messages)
                .Where(m => m.type == LogType.Error || m.type == LogType.Exception).Select(m => m.content).Take(10));
            return $"{s.result} | {s.totalSize / (1024f * 1024f):0.0} MB | {s.totalTime:mm\\:ss} | errors={s.totalErrors} warnings={s.totalWarnings} | {Path.GetFullPath(options.locationPathName)}" +
                   (string.IsNullOrEmpty(errors) ? string.Empty : $"\n{errors}");
        }
    }
}
