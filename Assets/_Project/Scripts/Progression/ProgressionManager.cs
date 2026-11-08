using System;
using System.Collections.Generic;
using UnityEngine;
using VentsAndVending.Economy;

namespace VentsAndVending.Progression
{
    /// <summary>
    /// Scene-level owner of unlocks and upgrades (GDD §18.3). Systems read bonuses by key and listen to
    /// <see cref="Changed"/> to re-apply them.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class ProgressionManager : MonoBehaviour
    {
        [SerializeField] private List<UpgradeDefinition> _catalogue = new List<UpgradeDefinition>();

        private readonly UpgradeLedger _ledger = new UpgradeLedger();

        public static ProgressionManager Instance { get; private set; }

        public IReadOnlyList<UpgradeDefinition> Catalogue => _catalogue;
        public UpgradeLedger Ledger => _ledger;

        /// <summary>Raised after any purchase so systems can re-apply bonuses.</summary>
        public event Action Changed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ProgressionManager] More than one in the scene; ignoring this one.", this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public float Bonus(string key) => _ledger.Bonus(key);
        public bool Has(string key) => _ledger.Has(key);
        public bool IsOwned(UpgradeDefinition upgrade) => _ledger.IsOwned(upgrade);
        public PurchaseCheck Check(UpgradeDefinition upgrade, Wallet wallet) => _ledger.Check(upgrade, wallet?.Balance ?? 0);

        public bool TryPurchase(UpgradeDefinition upgrade, Wallet wallet)
        {
            if (wallet == null || Check(upgrade, wallet) != PurchaseCheck.Available || !wallet.TrySpend(upgrade.Cost))
            {
                return false;
            }

            _ledger.Grant(upgrade);
            ApplyInventoryBonuses();
            Changed?.Invoke();
            return true;
        }

        private void Start() => ApplyInventoryBonuses();

        /// <summary>Grants upgrades from a save without charging for them.</summary>
        public void RestoreOwned(IEnumerable<UpgradeDefinition> upgrades)
        {
            foreach (UpgradeDefinition upgrade in upgrades)
            {
                _ledger.Grant(upgrade);
            }
            ApplyInventoryBonuses();
            Changed?.Invoke();
        }

        /// <summary>Inventories opt in with a capacity upgrade key (e.g. capacity.pockets).</summary>
        private void ApplyInventoryBonuses()
        {
            foreach (var inventory in FindObjectsByType<Inventory.InventoryComponent>(FindObjectsSortMode.None))
            {
                if (!string.IsNullOrEmpty(inventory.CapacityUpgradeKey))
                {
                    inventory.ApplyCapacityBonus(Mathf.RoundToInt(Bonus(inventory.CapacityUpgradeKey)));
                }
            }
        }

        /// <summary>Helper for consumers: subscribe and immediately apply current state.</summary>
        public static void Subscribe(Action apply)
        {
            if (Instance != null)
            {
                Instance.Changed += apply;
            }
            apply();
        }

        public static void Unsubscribe(Action apply)
        {
            if (Instance != null)
            {
                Instance.Changed -= apply;
            }
        }

        internal void SetCatalogue(IEnumerable<UpgradeDefinition> catalogue) => _catalogue = new List<UpgradeDefinition>(catalogue);
    }
}
