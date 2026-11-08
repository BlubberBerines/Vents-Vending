using System;

namespace VentsAndVending.Economy
{
    /// <summary>A Cosmic Credits balance (GDD §12.1). Plain C# for testing and save data.</summary>
    public sealed class Wallet
    {
        public Wallet(int startingBalance = 0)
        {
            Balance = Math.Max(0, startingBalance);
        }

        public int Balance { get; private set; }

        /// <summary>(new balance, delta)</summary>
        public event Action<int, int> Changed;

        public bool CanAfford(int amount) => amount <= Balance;

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Balance += amount;
            Changed?.Invoke(Balance, amount);
        }

        /// <summary>Sets the balance when loading a save (raises Changed with delta 0).</summary>
        public void Restore(int balance)
        {
            Balance = Math.Max(0, balance);
            Changed?.Invoke(Balance, 0);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > Balance)
            {
                return false;
            }

            if (amount == 0)
            {
                return true;
            }

            Balance -= amount;
            Changed?.Invoke(Balance, -amount);
            return true;
        }
    }
}
