using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Inventory;

namespace VentsAndVending.Tests
{
    public class InventoryModelTests
    {
        private ItemDefinition _glarb;
        private ItemDefinition _gel;

        [SetUp]
        public void SetUp()
        {
            _glarb = MakeItem("glarb_extract", 10);
            _gel = MakeItem("lumina_gel", 5);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_glarb);
            Object.DestroyImmediate(_gel);
        }

        private static ItemDefinition MakeItem(string id, int maxStack)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Configure(id, id, string.Empty, ItemCategory.Ingredient, maxStack, 1, Color.white);
            return item;
        }

        [Test]
        public void Add_StacksIntoOneSlot()
        {
            var inv = new InventoryModel(4);
            Assert.AreEqual(0, inv.Add(_glarb, 3));
            Assert.AreEqual(0, inv.Add(_glarb, 4));
            Assert.AreEqual(7, inv.Count(_glarb));
            Assert.AreEqual(1, inv.UsedSlots);
        }

        [Test]
        public void Add_OverflowsIntoNewSlots()
        {
            var inv = new InventoryModel(4);
            inv.Add(_gel, 12); // max stack 5 → 5, 5, 2
            Assert.AreEqual(3, inv.UsedSlots);
            Assert.AreEqual(12, inv.Count(_gel));
        }

        [Test]
        public void Add_WhenFull_ReturnsRemainder()
        {
            var inv = new InventoryModel(2);
            int notAdded = inv.Add(_gel, 13);
            Assert.AreEqual(3, notAdded);
            Assert.AreEqual(10, inv.Count(_gel));
            Assert.IsFalse(inv.CanAdd(_glarb, 1));
        }

        [Test]
        public void SpaceFor_CountsPartialStacksAndEmptySlots()
        {
            var inv = new InventoryModel(3);
            inv.Add(_gel, 2);
            inv.Add(_glarb, 1);
            Assert.AreEqual(3 + 5, inv.SpaceFor(_gel)); // top up 3 + one empty slot of 5
        }

        [Test]
        public void Remove_TakesAcrossStacks()
        {
            var inv = new InventoryModel(4);
            inv.Add(_gel, 12);
            Assert.AreEqual(8, inv.Remove(_gel, 8));
            Assert.AreEqual(4, inv.Count(_gel));
            Assert.AreEqual(1, inv.UsedSlots);
        }

        [Test]
        public void Remove_MoreThanOwned_RemovesWhatExists()
        {
            var inv = new InventoryModel(2);
            inv.Add(_glarb, 2);
            Assert.AreEqual(2, inv.Remove(_glarb, 10));
            Assert.IsTrue(inv.IsEmpty);
        }

        [Test]
        public void TransferSlot_MovesWholeStackWhenItFits()
        {
            var from = new InventoryModel(2);
            var to = new InventoryModel(2);
            from.Add(_glarb, 6);
            Assert.AreEqual(6, from.TransferSlotTo(0, to));
            Assert.IsTrue(from.IsEmpty);
            Assert.AreEqual(6, to.Count(_glarb));
        }

        [Test]
        public void TransferSlot_PartialWhenTargetNearlyFull()
        {
            var from = new InventoryModel(1);
            var to = new InventoryModel(1);
            from.Add(_gel, 5);
            to.Add(_gel, 3);
            Assert.AreEqual(2, from.TransferSlotTo(0, to));
            Assert.AreEqual(3, from.Count(_gel));
            Assert.AreEqual(5, to.Count(_gel));
        }

        [Test]
        public void TransferSlot_ToSelf_DoesNothing()
        {
            var inv = new InventoryModel(2);
            inv.Add(_glarb, 3);
            Assert.AreEqual(0, inv.TransferSlotTo(0, inv));
            Assert.AreEqual(3, inv.Count(_glarb));
        }

        [Test]
        public void TakeSlot_EmptiesAndReturnsStack()
        {
            var inv = new InventoryModel(2);
            inv.Add(_glarb, 4);
            ItemStack taken = inv.TakeSlot(0);
            Assert.AreEqual(_glarb, taken.Item);
            Assert.AreEqual(4, taken.Count);
            Assert.IsTrue(inv.IsEmpty);
        }

        [Test]
        public void Changed_FiresOnlyOnRealChanges()
        {
            var inv = new InventoryModel(1);
            int changes = 0;
            inv.Changed += () => changes++;

            inv.Add(_gel, 5);       // change
            inv.Add(_gel, 1);       // full — no change
            inv.Remove(_glarb, 1);  // not present — no change
            inv.Remove(_gel, 2);    // change

            Assert.AreEqual(2, changes);
        }

        [Test]
        public void Constructor_RejectsZeroCapacity()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new InventoryModel(0));
        }
    }
}
