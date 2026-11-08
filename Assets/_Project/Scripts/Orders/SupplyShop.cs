using System;
using VentsAndVending.Economy;
using VentsAndVending.Inventory;

namespace VentsAndVending.Orders
{
    /// <summary>Something you can order on VentNet's Supplies page and have dropped in your den locker.</summary>
    [Serializable]
    public struct SupplyListing
    {
        public ItemDefinition Item;
        public int Price;
        public string Blurb;
    }

    public enum PurchaseResult
    {
        Delivered,
        CannotAfford,
        LockerFull
    }

    /// <summary>
    /// VentNet Supplies (underpass design §7): pay from the wallet, delivered straight into the den locker. Checks room
    /// first and money second, so a refused order never charges you.
    /// </summary>
    public static class SupplyShop
    {
        public static PurchaseResult TryBuy(SupplyListing listing, Wallet wallet, InventoryModel locker)
        {
            if (listing.Item == null || wallet == null || locker == null || !locker.CanAdd(listing.Item, 1))
            {
                return PurchaseResult.LockerFull;
            }
            if (!wallet.TrySpend(listing.Price))
            {
                return PurchaseResult.CannotAfford;
            }
            locker.Add(listing.Item, 1);
            return PurchaseResult.Delivered;
        }

        public static string Message(PurchaseResult result) => result switch
        {
            PurchaseResult.Delivered => "Package dropped in your locker.",
            PurchaseResult.LockerFull => "Locker's full. Make room for the delivery.",
            _ => "Not enough credits."
        };
    }
}
