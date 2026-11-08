using System;
using System.Collections.Generic;
using UnityEngine;

namespace VentsAndVending.Inventory
{
    /// <summary>Gives a GameObject (player, storage, machine) an <see cref="InventoryModel"/>.</summary>
    [DisallowMultipleComponent]
    public sealed class InventoryComponent : MonoBehaviour
    {
        [Serializable]
        private struct StartingItem
        {
            public ItemDefinition Item;
            [Min(1)] public int Count;
        }

        [SerializeField] private string _displayName = "Inventory";
        [SerializeField, Min(1)] private int _capacity = 12;
        [SerializeField] private List<StartingItem> _startingItems = new List<StartingItem>();

        private InventoryModel _inventory;

        public string DisplayName => _displayName;

        public InventoryModel Model
        {
            get
            {
                EnsureCreated();
                return _inventory;
            }
        }

        private void Awake() => EnsureCreated();

        private void EnsureCreated()
        {
            if (_inventory != null)
            {
                return;
            }

            _inventory = new InventoryModel(_capacity);
            foreach (StartingItem start in _startingItems)
            {
                if (start.Item != null && start.Count > 0)
                {
                    _inventory.Add(start.Item, start.Count);
                }
            }
        }

        internal void Configure(string displayName, int capacity)
        {
            _displayName = displayName;
            _capacity = Mathf.Max(1, capacity);
            _startingItems.Clear();
        }

        internal void AddStartingItem(ItemDefinition item, int count)
        {
            _startingItems.Add(new StartingItem { Item = item, Count = count });
        }
    }
}
