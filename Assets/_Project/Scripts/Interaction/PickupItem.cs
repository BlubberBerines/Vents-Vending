using UnityEngine;
using VentsAndVending.Inventory;

namespace VentsAndVending.Interaction
{
    /// <summary>A world object that goes into the player's inventory when picked up.</summary>
    public sealed class PickupItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition _item;
        [SerializeField, Min(1)] private int _count = 1;

        public ItemDefinition Item => _item;
        public int Count => _count;

        public string GetPrompt(InteractionController interactor)
        {
            if (_item == null)
            {
                return null;
            }

            return _count > 1 ? $"Pick up {_item.DisplayName} x{_count}" : $"Pick up {_item.DisplayName}";
        }

        public void Interact(InteractionController interactor)
        {
            if (_item == null || interactor.Inventory == null)
            {
                return;
            }

            int notAdded = interactor.Inventory.Model.Add(_item, _count);
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
        public static PickupItem Spawn(ItemDefinition item, int count, Vector3 position, Quaternion rotation)
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

            pickup.Configure(item, count);
            return pickup;
        }

        internal void Configure(ItemDefinition item, int count)
        {
            _item = item;
            _count = Mathf.Max(1, count);
        }
    }
}
