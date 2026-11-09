namespace VentsAndVending.Player
{
    /// <summary>Central player movement states (GDD §3.2).</summary>
    public enum PlayerMovementState
    {
        Normal,
        Sprinting,
        Crouching,
        Crawling,
        Interacting,
        /// <summary>Pulling up onto a crate, ledge or into a vent mouth (station layers spec §6.1).</summary>
        Climbing
    }
}
