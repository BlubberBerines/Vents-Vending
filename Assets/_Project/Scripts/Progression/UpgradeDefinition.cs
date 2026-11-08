using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VentsAndVending.Progression
{
    /// <summary>GDD §12.3 upgrade categories.</summary>
    public enum UpgradeCategory
    {
        Production,
        Storage,
        Security,
        Mobility,
        Business,
        Hideout
    }

    /// <summary>Well-known effect keys read by game systems. Add new ones freely — systems ignore unknown keys.</summary>
    public static class UpgradeKeys
    {
        /// <summary>Lights extra hideout lamps (presence, amount ignored).</summary>
        public const string HideoutLighting = "hideout.lighting";
        /// <summary>Extra player inventory slots.</summary>
        public const string PocketsCapacity = "capacity.pockets";
        /// <summary>Extra storage locker slots.</summary>
        public const string LockerCapacity = "capacity.locker";
        /// <summary>Widens the mixer's premium window on each side (progress units, 0.05 = 5%).</summary>
        public const string MixerWindow = "mixer.window";
        /// <summary>Extra settle speed (1 = twice as fast).</summary>
        public const string MixerSettleSpeed = "mixer.settleSpeed";
    }

    /// <summary>A purchasable upgrade (GDD §5.2, §12.3). Effects are key/amount pairs summed across owned upgrades.</summary>
    [CreateAssetMenu(menuName = "Vents & Vending/Upgrade Definition", fileName = "Upgrade_New")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [Serializable]
        public struct Effect
        {
            public string Key;
            public float Amount;
        }

        [Tooltip("Stable ID used by save files.")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 3)] private string _description;
        [SerializeField] private UpgradeCategory _category;
        [SerializeField, Min(1)] private int _tier = 1;
        [SerializeField, Min(0)] private int _cost;
        [SerializeField] private UpgradeDefinition _prerequisite;
        [SerializeField] private List<Effect> _effects = new List<Effect>();

        public string Id => _id;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string Description => _description;
        public UpgradeCategory Category => _category;
        public int Tier => _tier;
        public int Cost => _cost;
        public UpgradeDefinition Prerequisite => _prerequisite;
        public IReadOnlyList<Effect> Effects => _effects;

        internal void Configure(string id, string displayName, string description, UpgradeCategory category, int tier, int cost,
            UpgradeDefinition prerequisite, params (string key, float amount)[] effects)
        {
            _id = id;
            _displayName = displayName;
            _description = description;
            _category = category;
            _tier = Mathf.Max(1, tier);
            _cost = Mathf.Max(0, cost);
            _prerequisite = prerequisite;
            _effects = effects.Select(e => new Effect { Key = e.key, Amount = e.amount }).ToList();
        }
    }
}
