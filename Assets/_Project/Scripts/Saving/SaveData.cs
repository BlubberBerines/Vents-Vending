using System;
using System.Collections.Generic;

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
        public PlayerSave Player = new PlayerSave();
        public int Credits;
        public int SecurityIncidents;
        public List<string> Upgrades = new List<string>();
        public List<InventorySave> Inventories = new List<InventorySave>();
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
