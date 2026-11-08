namespace VentsAndVending.Interaction
{
    /// <summary>
    /// Shared contract for everything the player can aim at and use (GDD §3.3): machines, storage, doors, hatches,
    /// customers, terminals…
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Prompt verb phrase, e.g. "Open storage". Return null or empty when it can't be used right now.</summary>
        string GetPrompt(InteractionController interactor);

        void Interact(InteractionController interactor);
    }
}
