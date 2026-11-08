using UnityEngine;

namespace VentsAndVending.Economy
{
    /// <summary>The player's Cosmic Credits.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerWallet : MonoBehaviour
    {
        [Tooltip("GDD §1.1: the player starts with practically no money.")]
        [SerializeField, Min(0)] private int _startingCredits = 20;

        private Wallet _wallet;

        public Wallet Wallet => _wallet ??= new Wallet(_startingCredits);

        private void Awake() => _ = Wallet;
    }
}
