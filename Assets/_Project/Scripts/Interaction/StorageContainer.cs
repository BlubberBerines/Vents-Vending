using UnityEngine;
using VentsAndVending.Inventory;

namespace VentsAndVending.Interaction
{
    /// <summary>Opens a transfer view between the player's inventory and this container's.</summary>
    [RequireComponent(typeof(InventoryComponent))]
    public sealed class StorageContainer : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _verb = "Open";

        private InventoryComponent _inventory;

        private void Awake() => _inventory = GetComponent<InventoryComponent>();

        public string GetPrompt(InteractionController interactor) => $"{_verb} {_inventory.DisplayName}";

        public void Interact(InteractionController interactor) => interactor.RequestInventoryView(_inventory);
    }
}
