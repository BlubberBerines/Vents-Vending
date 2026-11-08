using UnityEngine;
using VentsAndVending.Inventory;

namespace VentsAndVending.Interaction
{
    /// <summary>A world object that goes into the player's inventory when picked up.</summary>
    public sealed class PickupItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition _item;
        [SerializeField, Min(1)] private int _count = 1;
        [SerializeField] private ItemQuality _quality = ItemQuality.Standard;

        public ItemDefinition Item => _item;
        public int Count => _count;
        public ItemQuality Quality => _quality;

        public string GetPrompt(InteractionController interactor)
        {
            if (_item == null)
            {
                return null;
            }

            string name = _item.Category == ItemCategory.Product ? $"{_item.DisplayName} ({_quality})" : _item.DisplayName;
            return _count > 1 ? $"Pick up {name} x{_count}" : $"Pick up {name}";
        }

        public void Interact(InteractionController interactor)
        {
            if (_item == null || interactor.Inventory == null)
            {
                return;
            }

            int notAdded = interactor.Inventory.Model.Add(_item, _count, _quality);
            int taken = _count - notAdded;
            if (taken == 0)
            {
                interactor.Notify("Inventory full");
                return;
            }

            interactor.Notify($"+{taken} {_item.DisplayName}");
            if (notAdded == 0)
            {
                Destroy(gameObject);
            }
            else
            {
                _count = notAdded;
                interactor.Notify("Inventory full");
            }
        }

        /// <summary>Spawns the item's world prefab. Returns null if the item has no prefab.</summary>
        public static PickupItem Spawn(ItemDefinition item, int count, ItemQuality quality, Vector3 position, Quaternion rotation)
        {
            if (item == null || item.WorldPrefab == null)
            {
                return null;
            }

            GameObject go = Instantiate(item.WorldPrefab, position, rotation);
            if (!go.TryGetComponent(out PickupItem pickup))
            {
                pickup = go.AddComponent<PickupItem>();
            }

            pickup.Configure(item, count, quality);
            return pickup;
        }

        internal void Configure(ItemDefinition item, int count, ItemQuality quality = ItemQuality.Standard)
        {
            _item = item;
            _count = Mathf.Max(1, count);
            _quality = quality;
        }
    }
}
