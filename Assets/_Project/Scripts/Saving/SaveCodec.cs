using System;
using System.Collections.Generic;
using UnityEngine;
using VentsAndVending.Inventory;

namespace VentsAndVending.Saving
{
    /// <summary>Pure capture/restore and JSON (de)serialisation with validation. No file IO here.</summary>
    public static class SaveCodec
    {
        public const int CurrentVersion = 3; // v2 adds Tunnels, v3 adds Orders (older saves load with empty state)

        public static List<StackSave> CaptureInventory(InventoryModel inventory)
        {
            var stacks = new List<StackSave>();
            foreach (ItemStack stack in inventory.Slots)
            {
                if (!stack.IsEmpty && !string.IsNullOrEmpty(stack.Item.Id))
                {
                    stacks.Add(new StackSave { Item = stack.Item.Id, Count = stack.Count, Quality = stack.Quality.ToString() });
                }
            }
            return stacks;
        }

        /// <summary>
        /// Replaces the inventory's contents. Unknown item IDs and bad counts are skipped (data validation).
        /// Returns how many entries were skipped.
        /// </summary>
        public static int RestoreInventory(InventoryModel inventory, IEnumerable<StackSave> stacks, Func<string, ItemDefinition> lookup)
        {
            inventory.Clear();
            int skipped = 0;
            if (stacks == null)
            {
                return 0;
            }

            foreach (StackSave stack in stacks)
            {
                ItemDefinition item = stack != null ? lookup(stack.Item) : null;
                if (item == null || stack.Count <= 0)
                {
                    skipped++;
                    continue;
                }

                if (!Enum.TryParse(stack.Quality, out ItemQuality quality))
                {
                    quality = ItemQuality.Standard;
                }

                skipped += inventory.Add(item, stack.Count, quality) > 0 ? 1 : 0;
            }
            return skipped;
        }

        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, true);

        /// <summary>Parses and validates a save. Returns false (with a reason) for corrupt, empty or too-new files.</summary>
        public static bool TryParse(string json, out SaveData data, out string error)
        {
            data = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "empty file";
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                error = $"unreadable: {e.Message}";
                return false;
            }

            if (data == null || data.Version < 1)
            {
                error = "not a save file";
                data = null;
                return false;
            }

            if (data.Version > CurrentVersion)
            {
                error = $"made by a newer version (v{data.Version})";
                data = null;
                return false;
            }

            data.Upgrades ??= new List<string>();
            data.Inventories ??= new List<InventorySave>();
            data.Player ??= new PlayerSave();
            data.Tunnels ??= new VentsAndVending.Tunnels.TunnelSave();
            data.Tunnels.DiscoveredRooms ??= new List<string>();
            data.Tunnels.Switches ??= new List<VentsAndVending.Tunnels.SwitchSave>();
            data.Orders ??= new VentsAndVending.Orders.OrderBoardSave();
            data.Orders.Orders ??= new List<VentsAndVending.Orders.CustomerOrder>();
            data.Version = CurrentVersion; // migrated in memory
            data.Credits = Math.Max(0, data.Credits);
            return true;
        }
    }
}
