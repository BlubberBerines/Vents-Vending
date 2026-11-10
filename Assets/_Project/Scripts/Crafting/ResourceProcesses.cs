using System;
using VentsAndVending.Inventory;

namespace VentsAndVending.Crafting
{
    [Serializable]
    public sealed class DepositSave
    {
        public string Id;
        public int Hits;
        public float RegrowRemaining;
    }

    /// <summary>Three deliberate strikes per harvest. Full pockets never consume a harvest.</summary>
    public sealed class CrystalDeposit
    {
        public const int Strikes = 3, Yield = 3;
        public const float RegrowSeconds = 180;
        public int Hits { get; private set; }
        public float RegrowRemaining { get; private set; }
        public bool Available => RegrowRemaining <= 0;

        public bool Strike(InventoryModel inventory, ItemDefinition pickaxe, ItemDefinition raw, out string message)
        {
            if (inventory == null || pickaxe == null || raw == null || inventory.Count(pickaxe) == 0)
            { message = "Take the mining pickaxe beside the core first."; return false; }
            if (!Available) { message = "Crystals are regrowing."; return false; }
            if (Hits == Strikes - 1 && !inventory.CanAdd(raw, Yield))
            { message = "Make room for 3 raw core crystals. This vein is not consumed."; return false; }
            Hits++;
            if (Hits < Strikes) { message = $"Crystal fractured ({Hits}/{Strikes})"; return true; }
            inventory.Add(raw, Yield);
            Hits = 0;
            RegrowRemaining = RegrowSeconds;
            message = "+3 Raw Core Crystal. Refine these in the Den.";
            return true;
        }

        public void Tick(float seconds) => RegrowRemaining = Math.Max(0, RegrowRemaining - Math.Max(0, seconds));
        public DepositSave Capture(string id) => new DepositSave { Id = id, Hits = Hits, RegrowRemaining = RegrowRemaining };
        public void Restore(DepositSave save)
        {
            Hits = Math.Max(0, Math.Min(Strikes - 1, save?.Hits ?? 0));
            float remaining = save?.RegrowRemaining ?? 0;
            RegrowRemaining = float.IsNaN(remaining) || float.IsInfinity(remaining) ? 0 : Math.Max(0, Math.Min(RegrowSeconds, remaining));
            if (!Available) Hits = 0;
        }
    }

    [Serializable]
    public sealed class RefiningSave
    {
        public string Id;
        public bool HasBatch;
        public float Remaining;
    }

    /// <summary>Input is held by the machine until an entire output batch can be collected.</summary>
    public sealed class RefiningProcess
    {
        public const int InputCount = 3, OutputCount = 2;
        public const float Duration = 8;
        public bool HasBatch { get; private set; }
        public float Remaining { get; private set; }
        public bool Ready => HasBatch && Remaining <= 0;
        public bool Start(InventoryModel inventory, ItemDefinition raw)
        {
            if (HasBatch || inventory == null || raw == null || inventory.Count(raw) < InputCount) return false;
            inventory.Remove(raw, InputCount);
            HasBatch = true; Remaining = Duration;
            return true;
        }
        public void Tick(float seconds)
        {
            if (HasBatch) Remaining = Math.Max(0, Remaining - Math.Max(0, seconds));
        }
        public bool Collect(InventoryModel inventory, ItemDefinition output)
        {
            if (!Ready || inventory == null || output == null || !inventory.CanAdd(output, OutputCount)) return false;
            inventory.Add(output, OutputCount);
            HasBatch = false; Remaining = 0;
            return true;
        }
        public RefiningSave Capture(string id) => new RefiningSave { Id = id, HasBatch = HasBatch, Remaining = Remaining };
        public void Restore(RefiningSave save)
        {
            HasBatch = save?.HasBatch ?? false;
            float remaining = save?.Remaining ?? 0;
            Remaining = !HasBatch || float.IsNaN(remaining) || float.IsInfinity(remaining) ? 0 : Math.Max(0, Math.Min(Duration, remaining));
        }
    }
}
