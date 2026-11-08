using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using VentsAndVending.Progression;

namespace VentsAndVending.Editor
{
    /// <summary>Upgrades (GDD §5.2, §12.3): catalogue assets, ProgressionManager, terminal and the lighting upgrade.</summary>
    public static partial class PrototypeSceneBuilder
    {
        private const string UpgradeFolder = "Assets/_Project/ScriptableObjects/Upgrades";

        private static void BuildProgression(Transform level, Material metal, Material accent)
        {
            EnsureFolder(UpgradeFolder);
            List<UpgradeDefinition> catalogue = CreateUpgrades();
            _upgradeCatalogue = catalogue;

            var managerGo = new GameObject("Progression");
            managerGo.AddComponent<ProgressionManager>().SetCatalogue(catalogue);

            BuildUpgradeTerminal(metal, accent);
            BuildLightingUpgrade(level, accent);
        }

        private static List<UpgradeDefinition> CreateUpgrades()
        {
            var lighting = GetOrCreateUpgrade("Upgrade_BetterLighting", "upg_better_lighting", "Better Lighting",
                "Proper work lamps for the hideout. You can finally see what you're mixing.",
                UpgradeCategory.Hideout, 1, 25, null, (UpgradeKeys.HideoutLighting, 1f));
            var locker = GetOrCreateUpgrade("Upgrade_LockerExpansion", "upg_locker_expansion", "Locker Expansion",
                "+8 slots in the storage locker. More room for questionable ingredients.",
                UpgradeCategory.Storage, 1, 30, null, (UpgradeKeys.LockerCapacity, 8f));
            var settle = GetOrCreateUpgrade("Upgrade_QuickSettleCoil", "upg_quick_settle", "Quick-Settle Coil",
                "Batches settle twice as fast. The coil hums ominously.",
                UpgradeCategory.Production, 1, 35, null, (UpgradeKeys.MixerSettleSpeed, 1f));
            var pockets = GetOrCreateUpgrade("Upgrade_DeeperPockets", "upg_deeper_pockets", "Deeper Pockets",
                "+4 inventory slots. Sewn from a decommissioned cargo net.",
                UpgradeCategory.Storage, 1, 40, null, (UpgradeKeys.PocketsCapacity, 4f));
            var gauge = GetOrCreateUpgrade("Upgrade_PrecisionGauge", "upg_precision_gauge", "Precision Gauge",
                "Widens the mixer's green zone by 5% on each side. Premium batches get much easier.",
                UpgradeCategory.Production, 2, 60, null, (UpgradeKeys.MixerWindow, 0.05f));
            var stirrer = GetOrCreateUpgrade("Upgrade_IndustrialStirrer", "upg_industrial_stirrer", "Industrial Stirrer",
                "Widens the green zone by another 4%. Smells faintly of hot metal and ambition.",
                UpgradeCategory.Production, 2, 120, gauge, (UpgradeKeys.MixerWindow, 0.04f));

            return new List<UpgradeDefinition> { lighting, locker, settle, pockets, gauge, stirrer };
        }

        private static UpgradeDefinition GetOrCreateUpgrade(string assetName, string id, string displayName, string description,
            UpgradeCategory category, int tier, int cost, UpgradeDefinition prerequisite, params (string key, float amount)[] effects)
        {
            string path = $"{UpgradeFolder}/{assetName}.asset";
            var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path);
            if (upgrade == null)
            {
                upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
                AssetDatabase.CreateAsset(upgrade, path);
            }

            upgrade.Configure(id, displayName, description, category, tier, cost, prerequisite, effects);
            EditorUtility.SetDirty(upgrade);
            return upgrade;
        }

        /// <summary>Wall terminal between the locker and the mixing station. Outside "Level" so its text isn't NavMesh-baked.</summary>
        private static void BuildUpgradeTerminal(Material metal, Material accent)
        {
            var screenMat = GetOrCreateMaterial("Terminal_Screen", new Color(0.01f, 0.05f, 0.03f), 0.2f, new Color(0.02f, 0.14f, 0.07f));

            var root = new GameObject("Upgrade Terminal");
            root.transform.position = new Vector3(-0.4f, 0f, -19.55f);
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.size = new Vector3(0.9f, 1.8f, 0.45f);

            Part("Post", root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.22f, 1.2f, 0.22f), metal);
            var housing = Part("Housing", root.transform, new Vector3(0f, 1.45f, 0.02f), new Vector3(0.85f, 0.58f, 0.1f), metal);
            housing.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            var screen = Part("Screen", root.transform, new Vector3(0f, 1.46f, 0.08f), new Vector3(0.74f, 0.46f, 0.02f), screenMat);
            screen.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            Part("Keypad", root.transform, new Vector3(0f, 1.08f, 0.14f), new Vector3(0.5f, 0.04f, 0.2f), accent);

            var label = new GameObject("Screen Text");
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0f, 1.465f, 0.1f);
            label.transform.localRotation = Quaternion.Euler(-12f, 180f, 0f); // TMP faces -Z; flip to face the room
            var tmp = label.AddComponent<TextMeshPro>();
            tmp.text = "UPGRADES";
            tmp.fontSize = 1.1f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.45f, 1f, 0.65f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.rectTransform.sizeDelta = new Vector2(0.7f, 0.4f);

            root.AddComponent<UpgradeTerminal>();
            CreatePointLight("Terminal Glow", root.transform.position + new Vector3(0f, 1.5f, 0.6f), new Color(0.3f, 1f, 0.5f), 2.5f, 0.8f, root.transform);
        }

        /// <summary>Warm work lamps over the workshop corner, switched on by the Better Lighting upgrade.</summary>
        private static void BuildLightingUpgrade(Transform level, Material accent)
        {
            var barMat = GetOrCreateMaterial("Lamp_Bar", new Color(0.3f, 0.25f, 0.15f), 0.5f, new Color(1.3f, 1.05f, 0.6f));

            var holder = Group("Upgrade_Lighting", level);
            var lamps = Group("WorkshopLamps", holder);
            foreach (float x in new[] { -2.5f, 1.6f })
            {
                var bar = Part("LampBar", lamps, new Vector3(x, 3.4f, -16.6f), new Vector3(1.4f, 0.06f, 0.18f), barMat);
                bar.transform.position = new Vector3(x, 3.4f, -16.6f);
                Part("LampMount", lamps, new Vector3(x, 3.75f, -16.6f), new Vector3(0.04f, 0.65f, 0.04f), accent).transform.position = new Vector3(x, 3.72f, -16.6f);
                CreatePointLight("Work Light", new Vector3(x, 3.2f, -16.4f), new Color(1f, 0.85f, 0.6f), 10f, 4.5f, lamps);
            }
            lamps.gameObject.SetActive(false);

            var activator = holder.gameObject.AddComponent<UpgradeActivator>();
            var so = new SerializedObject(activator);
            so.FindProperty("_key").stringValue = UpgradeKeys.HideoutLighting;
            SetList(so.FindProperty("_showWhenOwned"), lamps.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
