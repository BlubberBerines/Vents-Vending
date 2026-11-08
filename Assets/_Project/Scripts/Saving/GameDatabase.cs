using System.Collections.Generic;
using UnityEngine;
using VentsAndVending.Inventory;
using VentsAndVending.Progression;

namespace VentsAndVending.Saving
{
    /// <summary>Registry of every saveable definition so save files can refer to them by stable ID.</summary>
    [CreateAssetMenu(menuName = "Vents & Vending/Game Database", fileName = "GameDatabase")]
    public sealed class GameDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> _items = new List<ItemDefinition>();
        [SerializeField] private List<UpgradeDefinition> _upgrades = new List<UpgradeDefinition>();

        private Dictionary<string, ItemDefinition> _itemsById;
        private Dictionary<string, UpgradeDefinition> _upgradesById;

        public ItemDefinition FindItem(string id)
        {
            if (_itemsById == null)
            {
                _itemsById = new Dictionary<string, ItemDefinition>();
                foreach (ItemDefinition item in _items)
                {
                    if (item != null && !string.IsNullOrEmpty(item.Id))
                    {
                        _itemsById[item.Id] = item;
                    }
                }
            }
            return id != null && _itemsById.TryGetValue(id, out ItemDefinition found) ? found : null;
        }

        public UpgradeDefinition FindUpgrade(string id)
        {
            if (_upgradesById == null)
            {
                _upgradesById = new Dictionary<string, UpgradeDefinition>();
                foreach (UpgradeDefinition upgrade in _upgrades)
                {
                    if (upgrade != null && !string.IsNullOrEmpty(upgrade.Id))
                    {
                        _upgradesById[upgrade.Id] = upgrade;
                    }
                }
            }
            return id != null && _upgradesById.TryGetValue(id, out UpgradeDefinition found) ? found : null;
        }

        private void OnValidate()
        {
            _itemsById = null;
            _upgradesById = null;
        }

        internal void Configure(IEnumerable<ItemDefinition> items, IEnumerable<UpgradeDefinition> upgrades)
        {
            _items = new List<ItemDefinition>(items);
            _upgrades = new List<UpgradeDefinition>(upgrades);
            _itemsById = null;
            _upgradesById = null;
        }
    }
}
