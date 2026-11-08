using System;
using UnityEngine;
using VentsAndVending.Interaction;

namespace VentsAndVending.Player.Appearance
{
    /// <summary>A cracked mirror in the den: opens the wardrobe (change into owned clothes, pick a skin colour).</summary>
    public sealed class WardrobeMirror : MonoBehaviour, IInteractable
    {
        public static event Action<WardrobeMirror, InteractionController> PanelRequested;

        public string GetPrompt(InteractionController interactor) => "Change clothes";

        public void Interact(InteractionController interactor) => PanelRequested?.Invoke(this, interactor);
    }
}
