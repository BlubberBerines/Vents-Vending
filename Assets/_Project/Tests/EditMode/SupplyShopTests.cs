using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Economy;
using VentsAndVending.Inventory;
using VentsAndVending.Orders;

namespace VentsAndVending.Tests.EditMode
{
    public sealed class SupplyShopTests
    {
        private static ItemDefinition Item(string id, int maxStack)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Configure(id, id, string.Empty, ItemCategory.Equipment, maxStack, 1, Color.white);
            return item;
        }

        private static SupplyListing Crowbar(out ItemDefinition item)
        {
            item = Item("crowbar", 1);
            return new SupplyListing { Item = item, Price = 60, Blurb = "test" };
        }

        [Test]
        public void Buy_Delivers_AndCharges()
        {
            SupplyListing listing = Crowbar(out ItemDefinition item);
            var wallet = new Wallet(100);
            var locker = new InventoryModel(8);
            Assert.AreEqual(PurchaseResult.Delivered, SupplyShop.TryBuy(listing, wallet, locker));
            Assert.AreEqual(40, wallet.Balance);
            Assert.AreEqual(1, locker.Count(item));
        }

        [Test]
        public void Buy_CannotAfford_ChangesNothing()
        {
            SupplyListing listing = Crowbar(out ItemDefinition item);
            var wallet = new Wallet(59);
            var locker = new InventoryModel(8);
            Assert.AreEqual(PurchaseResult.CannotAfford, SupplyShop.TryBuy(listing, wallet, locker));
            Assert.AreEqual(59, wallet.Balance);
            Assert.AreEqual(0, locker.Count(item));
        }

        [Test]
        public void Buy_LockerFull_ChargesNothing()
        {
            SupplyListing listing = Crowbar(out ItemDefinition item);
            var wallet = new Wallet(100);
            var locker = new InventoryModel(1);
            locker.Add(Item("junk", 1), 1);
            Assert.AreEqual(PurchaseResult.LockerFull, SupplyShop.TryBuy(listing, wallet, locker));
            Assert.AreEqual(100, wallet.Balance);
            Assert.AreEqual(0, locker.Count(item));
        }

        [Test]
        public void Messages_UseSpecCopy()
        {
            Assert.AreEqual("Package dropped in your locker.", SupplyShop.Message(PurchaseResult.Delivered));
            Assert.AreEqual("Locker's full. Make room for the delivery.", SupplyShop.Message(PurchaseResult.LockerFull));
        }
    }
}
