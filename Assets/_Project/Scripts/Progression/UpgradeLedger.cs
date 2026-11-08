using System;
using System.Collections.Generic;

namespace VentsAndVending.Progression
{
    public enum PurchaseCheck
    {
        Available,
        AlreadyOwned,
        MissingPrerequisite,
        CannotAfford
    }

    /// <summary>Which upgrades are owned and the summed effect bonuses. Plain C# for tests and save data.</summary>
    public sealed class UpgradeLedger
    {
        private readonly HashSet<UpgradeDefinition> _owned = new HashSet<UpgradeDefinition>();

        public event Action<UpgradeDefinition> Purchased;

        public IReadOnlyCollection<UpgradeDefinition> Owned => _owned;

        public bool IsOwned(UpgradeDefinition upgrade) => upgrade != null && _owned.Contains(upgrade);

        public PurchaseCheck Check(UpgradeDefinition upgrade, int balance)
        {
            if (IsOwned(upgrade))
            {
                return PurchaseCheck.AlreadyOwned;
            }

            if (upgrade.Prerequisite != null && !IsOwned(upgrade.Prerequisite))
            {
                return PurchaseCheck.MissingPrerequisite;
            }

            return balance >= upgrade.Cost ? PurchaseCheck.Available : PurchaseCheck.CannotAfford;
        }

        /// <summary>Marks an upgrade owned (payment is the caller's job). Returns false if already owned.</summary>
        public bool Grant(UpgradeDefinition upgrade)
        {
            if (upgrade == null || !_owned.Add(upgrade))
            {
                return false;
            }

            Purchased?.Invoke(upgrade);
            return true;
        }

        /// <summary>Sum of all owned effects with this key.</summary>
        public float Bonus(string key)
        {
            float total = 0f;
            foreach (UpgradeDefinition upgrade in _owned)
            {
                foreach (UpgradeDefinition.Effect effect in upgrade.Effects)
                {
                    if (effect.Key == key)
                    {
                        total += effect.Amount;
                    }
                }
            }
            return total;
        }

        public bool Has(string key)
        {
            foreach (UpgradeDefinition upgrade in _owned)
            {
                foreach (UpgradeDefinition.Effect effect in upgrade.Effects)
                {
                    if (effect.Key == key)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
