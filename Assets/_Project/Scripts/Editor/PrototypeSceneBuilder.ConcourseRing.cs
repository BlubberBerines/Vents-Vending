using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Builds the complete public L1 ring from the existing environment kit.
    /// The ring is intentionally a review/playtest scene: it proves the physical top-down
    /// layout before the four-layer assembly replaces Greybox_Movement.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        public const string ConcourseRingPath = "Assets/_Project/Scenes/Districts/Station_Concourse_Ring.unity";

        private const int RingSectorCount = 24;
        private const int ModulesAlongSector = 10;
        private const int ModulesAcrossRing = 6;
        private const int WallHeightModules = 3;
        private const float SectorDegrees = 360f / RingSectorCount;
        private const float InnerRadius = 66f;
        private const float OuterRadius = 78f;
        private const float FloorY = 4.6f;
        private const float CeilingY = FloorY + WallHeightModules * 3f;

        private readonly struct District
        {
            public readonly string Id;
            public readonly string Name;
            public readonly int StartSector;
            public readonly int SectorCount;
            public readonly Color Accent;

            public District(string id, string name, int startSector, int sectorCount, Color accent)
            {
                Id = id;
                Name = name;
                StartSector = startSector;
                SectorCount = sectorCount;
                Accent = accent;
            }
        }

        private static readonly District[] ConcourseDistricts =
        {
            new("S01-S04", "Freight Quarter", 0, 4, new Color(0.52f, 0.18f, 0.85f)),
            new("S05-S07", "Leisure Deck", 4, 3, new Color(0.95f, 0.12f, 0.72f)),
            new("S08-S11", "Habitation Ring", 7, 4, new Color(0.32f, 0.82f, 0.42f)),
            new("S12-S15", "Grand Concourse", 11, 4, new Color(0.18f, 0.62f, 0.95f)),
            new("S16-S18", "Medical & Research", 15, 3, new Color(0.20f, 0.82f, 0.92f)),
            new("S19-S21", "Engineering Deck", 18, 3, new Color(0.95f, 0.48f, 0.10f)),
            new("S22-S24", "Security & Administration", 21, 3, new Color(0.08f, 0.32f, 0.92f)),
        };

        [MenuItem("Vents & Vending/Prototype/Build Complete Concourse Ring")]
        public static string BuildCompleteConcourseRing()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new InvalidOperationException("An open scene has unsaved changes; save or discard them first.");
                }
            }

            EnsureFolder("Assets/_Project/Scenes/Districts");
            PrepareConcourseSamples();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnvironmentKitSetup.Lighting();

            var root = new GameObject("Station_Concourse_Ring").transform;
            var structure = RingGroup("L1_Concourse_Structure", root);
            var spaceWindows = RingGroup("Outer_Wall_Space_Windows", root);
            var ceilingWindows = RingGroup("Ceiling_Space_Windows", root);
            var joints = RingGroup("Sector_Joints", root);
            var bulkheads = RingGroup("District_Bulkheads", root);
            var signage = RingGroup("District_Signage", root);
            var lighting = RingGroup("District_Lighting", root);

            BuildRingStructure(structure, spaceWindows, ceilingWindows, joints, bulkheads);
            BuildDistrictSignage(signage);
            BuildDistrictLighting(lighting);
            BuildReviewCameras(root);
            BuildStationLowerLayers(root);
            BuildViewportBackdrop(root);

            // A real player makes the ring immediately explorable while remaining isolated from the current map.
            EnsureInputActions();
            var den = _stationFrame.Find("L3_01_Den");
            var spawn = den.TransformPoint(new Vector3(0,0,-1.5f));
            var player = CreatePlayer(spawn, den.rotation * Quaternion.Euler(0,180,0));
            player.name = "Player_L1_Concourse";
            CreateHud(player.GetComponent<VentsAndVending.Interaction.InteractionController>());
            CreateStationSaveSystem();

            EditorSceneManager.SaveScene(scene, ConcourseRingPath);
            AssetDatabase.SaveAssets();
            return ConcourseRingPath;
        }

        public static void PlayConcourseRing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(ConcourseRingPath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new FileNotFoundException($"Unable to open concourse ring scene: {ConcourseRingPath}");
            }

            EditorApplication.isPlaying = true;
        }

        private static void BuildRingStructure(Transform structure, Transform spaceWindows, Transform ceilingWindows,
            Transform joints, Transform bulkheads)
        {
            for (int sector = 0; sector < RingSectorCount; sector++)
            {
                float start = sector * SectorDegrees;
                float step = SectorDegrees / ModulesAlongSector;

                for (int along = 0; along < ModulesAlongSector; along++)
                {
                    float angle = start + (along + 0.5f) * step;
                    float yaw = -angle;

                    for (int across = 0; across < ModulesAcrossRing; across++)
                    {
                        float radius = InnerRadius + 1f + across * 2f;
                        var floor = IsStationObservationAngle(angle) && (across == 2 || across == 3)
                            ? StationPublicGrate(structure,RingPoint(radius,angle,FloorY),yaw)
                            : across == 2 || across == 3
                            ? PlaceSample(structure, sector % 2 == 0 ? "CS_Floor_Rubber" : "CS_Floor_Steel", RingPoint(radius, angle, FloorY), yaw)
                            : PlaceKit(structure, "ENV_Floor_Solid_A", RingPoint(radius, angle, FloorY), yaw);
                        float tangentScale = Mathf.Max(1f, radius * step * Mathf.Deg2Rad / 2f + 0.015f);
                        floor.transform.localScale = new Vector3(1f, 1f, tangentScale);

                        // One overhead window panel per sector gives the ring a readable public-level rhythm.
                        string ceiling = (along == 4 || along == 9) && across == 2
                            ? "ENV_Ceiling_Light_A"
                            : "ENV_Ceiling_Standard_A";
                        var roof = ceiling == "ENV_Ceiling_Standard_A" && across == 3
                            ? PlaceSample(structure, sector % 2 == 0 ? "CS_Ceiling_Baffles" : "CS_Ceiling_Cassette", RingPoint(radius, angle, CeilingY), yaw)
                            : PlaceKit(structure, ceiling, RingPoint(radius, angle, CeilingY), yaw);
                        roof.transform.localScale = new Vector3(1f, 1f, tangentScale);
                    }

                    string outer = OuterWallFor(sector, along);
                    for (int wallRow = 0; wallRow < WallHeightModules; wallRow++)
                    {
                        float wallY = FloorY + wallRow * 3f;
                        bool panoramic = sector % 3 == 0 && wallRow == 0 && along >= 3 && along <= 5;
                        if (panoramic && along == 4)
                            PlaceSample(spaceWindows, "CS_Viewport_Panoramic6m", RingPoint(OuterRadius, angle, wallY), 90f-angle);
                        if (!panoramic) PlaceKit(spaceWindows, wallRow == 0 ? outer : "ENV_Wall_Standard_A",
                            RingPoint(OuterRadius, angle, wallY), 90f - angle)
                            .transform.localScale = new Vector3(OuterRadius * step * Mathf.Deg2Rad / 2f + 0.015f, 1f, 1f);
                        bool serviceOpening = wallRow == 0 && Mathf.Abs(Mathf.DeltaAngle(angle,ServiceAngle)) < 2.2f;
                        if (!serviceOpening)
                        {
                            if (wallRow == 0 && along % 3 == 0)
                                PlaceSample(structure,sector % 2 == 0 ? "CS_Wall_Ceramic" : "CS_Wall_ServiceRibs",RingPoint(InnerRadius,angle,wallY),270f-angle);
                            else PlaceKit(structure, "ENV_Wall_Standard_A", RingPoint(InnerRadius, angle, wallY), 270f - angle);
                        }
                    }
                }

                // A short angled joint at every 15-degree sector boundary preserves the ring silhouette
                // while keeping each interior run straight and kit-aligned.
                float boundary = start;
                for (int wallRow = 0; wallRow < WallHeightModules; wallRow++)
                {
                    float wallY = FloorY + wallRow * 3f;
                    PlaceKit(joints, "ENV_Wall_Braced_A", RingPoint(InnerRadius, boundary, wallY), 270f - boundary);
                    PlaceKit(joints, "ENV_Wall_Braced_A", RingPoint(OuterRadius, boundary, wallY), 90f - boundary);
                }
                PlaceKit(ceilingWindows, "ENV_Ceiling_Light_A", RingPoint(InnerRadius + 5f, boundary, CeilingY), -boundary);
            }

            // District boundaries are full-height, powered bulkheads. The sliding door component keeps them
            // traversable for review while preserving the intended future security gate locations.
            foreach (District district in ConcourseDistricts)
            {
                int boundarySector = district.StartSector;
                float angle = boundarySector * SectorDegrees;
                for (int across = 0; across < ModulesAcrossRing; across++)
                {
                    float radius = InnerRadius + 1f + across * 2f;
                    for (int wallRow = 0; wallRow < WallHeightModules; wallRow++)
                    {
                        float wallY = FloorY + wallRow * 3f;
                        string prefab = wallRow == 0 && across == ModulesAcrossRing / 2
                            ? "ENV_Wall_DoorSliding_A"
                            : "ENV_Wall_Standard_A";
                        PlaceKit(bulkheads, prefab, RingPoint(radius, angle, wallY), -angle);
                    }
                }
            }
        }

        private static string OuterWallFor(int sector, int along)
        {
            // Windows and viewports face space; ordinary wall modules fill the structural rhythm.
            if (along == 4 && sector % 3 == 0)
            {
                return "ENV_Wall_Viewport_A";
            }

            if (along % 3 == 1 || (sector + along) % 11 == 0)
            {
                return "ENV_Wall_Window_A";
            }

            return "ENV_Wall_Standard_A";
        }

        private static void BuildDistrictSignage(Transform parent)
        {
            foreach (District district in ConcourseDistricts)
            {
                float angle = (district.StartSector + district.SectorCount * 0.5f) * SectorDegrees;
                Vector3 position = RingPoint(InnerRadius + 0.4f, angle, FloorY + 3.3f);
                var go = new GameObject($"DistrictSign_{district.Id}_{district.Name.Replace(" ", "_")}");
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 270f - angle, 0f));

                var text = go.AddComponent<TextMesh>();
                text.text = $"{district.Name.ToUpperInvariant()}\n{district.Id}";
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.characterSize = 0.06f;
                text.fontSize = 48;
                text.color = district.Accent;
                text.richText = false;
                const string signMaterialPath = "Assets/_Project/Settings/ConcourseWorldText.mat";
                var signMaterial = AssetDatabase.LoadAssetAtPath<Material>(signMaterialPath);
                if (signMaterial == null)
                {
                    signMaterial = new Material(Shader.Find("VentsAndVending/Concourse World Text"));
                    signMaterial.mainTexture = text.font.material.mainTexture;
                    AssetDatabase.CreateAsset(signMaterial, signMaterialPath);
                }
                go.GetComponent<MeshRenderer>().sharedMaterial = signMaterial;
            }
        }

        private static void BuildDistrictLighting(Transform parent)
        {
            foreach (District district in ConcourseDistricts)
            {
                for (int sector = district.StartSector; sector < district.StartSector + district.SectorCount; sector++)
                {
                    float angle = (sector + 0.5f) * SectorDegrees;
                    var lightObject = new GameObject($"DistrictLight_{district.Id}_{sector + 1:00}");
                    lightObject.transform.SetParent(parent, false);
                    lightObject.transform.position = RingPoint(InnerRadius + 6f, angle, FloorY + 5f);

                    var light = lightObject.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = Color.Lerp(Color.white, district.Accent, 0.25f);
                    light.range = 22f;
                    light.intensity = 20f;
                    light.shadows = LightShadows.None;
                }
            }
        }

        private static void BuildReviewCameras(Transform parent)
        {
            AddReviewCamera(parent, "View_Ring_Overview", new Vector3(0f, 43f, -0.1f), Quaternion.Euler(90f, 0f, 0f));
            AddReviewCamera(parent, "View_Grand_Concourse", RingPoint(InnerRadius + 4f, 180f, FloorY + 1.7f), Quaternion.Euler(8f, 180f, 0f));
            AddReviewCamera(parent, "View_Outer_Window", RingPoint(OuterRadius - 4f, 330f, FloorY + 2f), Quaternion.Euler(6f, 120f, 0f));
        }

        private static Vector3 RingPoint(float radius, float angleDegrees, float y)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians) * radius, y, Mathf.Sin(radians) * radius);
        }

        private static GameObject PlaceKit(Transform parent, string prefabName, Vector3 position, float yaw)
        {
            string path = $"{EnvironmentKitSetup.PrefabFolder}/{prefabName}.prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                throw new FileNotFoundException($"Missing environment kit prefab: {path}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return instance;
        }

        private static Transform RingGroup(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static void AddReviewCamera(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            var camera = new GameObject(name).transform;
            camera.SetParent(parent, false);
            camera.SetPositionAndRotation(position, rotation);
            var component = camera.gameObject.AddComponent<Camera>();
            component.enabled = false;
            component.clearFlags = CameraClearFlags.Skybox;
            component.fieldOfView = 70f;
            component.nearClipPlane = 0.05f;
            component.farClipPlane = 300f;
        }
    }
}
