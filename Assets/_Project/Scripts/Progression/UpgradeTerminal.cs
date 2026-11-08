using System;
using UnityEngine;
using VentsAndVending.Interaction;

namespace VentsAndVending.Progression
{
    /// <summary>Wall terminal that opens the upgrade shop (GDD §3.3 "upgrade stations").</summary>
    public sealed class UpgradeTerminal : MonoBehaviour, IInteractable
    {
        public static event Action<UpgradeTerminal, InteractionController> PanelRequested;

        [SerializeField] private string _displayName = "Upgrade Terminal";

        public string DisplayName => _displayName;

        public string GetPrompt(InteractionController interactor) => $"Use {_displayName}";

        public void Interact(InteractionController interactor) => PanelRequested?.Invoke(this, interactor);
    }
}
