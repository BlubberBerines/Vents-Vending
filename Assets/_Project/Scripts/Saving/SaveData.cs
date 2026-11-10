using System;
using System.Collections.Generic;
using VentsAndVending.Inventory;

namespace VentsAndVending.Saving
{
    /// <summary>
    /// Serialised save file (GDD §18.5). Only stable string IDs and plain values — never Unity references —
    /// plus a version number for future migrations.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int Version = SaveCodec.CurrentVersion;
        public string SavedAtUtc;
        public string SceneName;
        public List<VentsAndVending.Crafting.MixingSave> Mixers = new List<VentsAndVending.Crafting.MixingSave>();
        public List<VentsAndVending.Crafting.DepositSave> Deposits = new List<VentsAndVending.Crafting.DepositSave>();
        public List<VentsAndVending.Crafting.RefiningSave> Refineries = new List<VentsAndVending.Crafting.RefiningSave>();
        public VentsAndVending.Orders.DeliverySave Deliveries = new VentsAndVending.Orders.DeliverySave();
        public PlayerSave Player = new PlayerSave();
        public int Credits;
        public int SecurityIncidents;
        public List<string> Upgrades = new List<string>();
        public List<InventorySave> Inventories = new List<InventorySave>();
        public List<WorldDropSave> WorldDrops = new List<WorldDropSave>();
        /// <summary>v2: discovered tunnel rooms and infrastructure switches (breakers, latches).</summary>
        public VentsAndVending.Tunnels.TunnelSave Tunnels = new VentsAndVending.Tunnels.TunnelSave();
        /// <summary>v3: VentNet inbox, accepted deals and trust.</summary>
        public VentsAndVending.Orders.OrderBoardSave Orders = new VentsAndVending.Orders.OrderBoardSave();
        /// <summary>v4: what the player owns, wears and their skin colour.</summary>
        public VentsAndVending.Player.Appearance.AppearanceSave Appearance = new VentsAndVending.Player.Appearance.AppearanceSave();
    }

    [Serializable]
    public sealed class WorldDropSave
    {
        public string Item;
        public int Count;
        public ItemQuality Quality;
        public float X,Y,Z,Pitch,Yaw,Roll;
    }

    [Serializable]
    public sealed class PlayerSave
    {
        public float X;
        public float Y;
        public float Z;
        public float Yaw;
    }

    [Serializable]
    public sealed class InventorySave
    {
        public string Id;
        public List<StackSave> Stacks = new List<StackSave>();
    }

    [Serializable]
    public sealed class StackSave
    {
        public string Item;
        public int Count;
        public string Quality;
    }
}
