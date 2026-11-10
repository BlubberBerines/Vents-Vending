using UnityEngine;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;

namespace VentsAndVending.World
{
    /// <summary>A recoverable loaner avoids permanently blocking mining if a tool is lost.</summary>
    public sealed class MiningToolRack : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition _pickaxe;
        internal void Configure(ItemDefinition pickaxe) => _pickaxe = pickaxe;
        public string GetPrompt(InteractionController interactor) => interactor.Inventory != null && interactor.Inventory.Model.Count(_pickaxe) > 0
            ? "Mining pickaxe already carried" : "Take spare mining pickaxe";
        public void Interact(InteractionController interactor)
        {
            var inventory = interactor?.Inventory?.Model;
            if (inventory == null || _pickaxe == null) return;
            if (inventory.Count(_pickaxe) > 0) { interactor.Notify("You already have a pickaxe. Aim at a crystal and press E to strike."); return; }
            interactor.Notify(inventory.Add(_pickaxe, 1) == 0 ? "Mining pickaxe acquired. Three E strikes harvest a crystal vein." : "Inventory full - make room for the pickaxe.");
        }
    }
}
