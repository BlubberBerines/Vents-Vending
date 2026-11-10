using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Inventory;
using VentsAndVending.Saving;

namespace VentsAndVending.Tests
{
    public class SaveTests
    {
        [Test] public void DroppedStockSurvivesJsonWithCountQualityAndPosition()
        {
            var data=new SaveData();data.WorldDrops.Add(new WorldDropSave{Item="glarb_juice",Count=3,Quality=ItemQuality.Premium,X=12,Y=1.2f,Z=-4,Yaw=90});
            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(data),out var parsed,out _));
            var drop=parsed.WorldDrops[0];Assert.AreEqual(3,drop.Count);Assert.AreEqual(ItemQuality.Premium,drop.Quality);Assert.AreEqual(12,drop.X);Assert.AreEqual(90,drop.Yaw);
        }
        [Test] public void UndefinedNumericQualityRestoresAsStandard()
        {
            var inventory=new InventoryModel(2);SaveCodec.RestoreInventory(inventory,new[]{new StackSave{Item="glarb_juice",Count=1,Quality="999"}},Lookup);
            Assert.AreEqual(1,inventory.Count(_juice,ItemQuality.Standard));
        }
        private ItemDefinition _juice;
        private ItemDefinition _gel;
        private Dictionary<string, ItemDefinition> _db;

        [SetUp]
        public void SetUp()
        {
            _juice = ScriptableObject.CreateInstance<ItemDefinition>();
            _juice.Configure("glarb_juice", "Glarb-Juice", "", ItemCategory.Product, 10, 12, Color.green);
            _gel = ScriptableObject.CreateInstance<ItemDefinition>();
            _gel.Configure("lumina_gel", "Lumina Gel", "", ItemCategory.Ingredient, 10, 4, Color.cyan);
            _db = new Dictionary<string, ItemDefinition> { { _juice.Id, _juice }, { _gel.Id, _gel } };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_juice);
            Object.DestroyImmediate(_gel);
        }

        private ItemDefinition Lookup(string id) => id != null && _db.TryGetValue(id, out var item) ? item : null;

        [Test]
        public void Inventory_RoundTrip_KeepsItemsCountsAndQuality()
        {
            var source = new InventoryModel(6);
            source.Add(_juice, 2, ItemQuality.Exceptional);
            source.Add(_juice, 1, ItemQuality.Poor);
            source.Add(_gel, 7);

            List<StackSave> saved = SaveCodec.CaptureInventory(source);
            var restored = new InventoryModel(6);
            int skipped = SaveCodec.RestoreInventory(restored, saved, Lookup);

            Assert.AreEqual(0, skipped);
            Assert.AreEqual(2, restored.Count(_juice, ItemQuality.Exceptional));
            Assert.AreEqual(1, restored.Count(_juice, ItemQuality.Poor));
            Assert.AreEqual(7, restored.Count(_gel));
        }

        [Test]
        public void Restore_SkipsUnknownIdsAndBadCounts()
        {
            var stacks = new List<StackSave>
            {
                new StackSave { Item = "glarb_juice", Count = 3, Quality = "Premium" },
                new StackSave { Item = "deleted_item", Count = 5, Quality = "Standard" },
                new StackSave { Item = "lumina_gel", Count = -2, Quality = "Standard" },
                null
            };
            var inv = new InventoryModel(4);
            int skipped = SaveCodec.RestoreInventory(inv, stacks, Lookup);
            Assert.AreEqual(3, skipped);
            Assert.AreEqual(3, inv.Count(_juice, ItemQuality.Premium));
        }

        [Test]
        public void Restore_UnknownQuality_FallsBackToStandard()
        {
            var inv = new InventoryModel(2);
            SaveCodec.RestoreInventory(inv, new[] { new StackSave { Item = "lumina_gel", Count = 2, Quality = "Legendary" } }, Lookup);
            Assert.AreEqual(2, inv.Count(_gel, ItemQuality.Standard));
        }

        [Test]
        public void Restore_ReplacesExistingContents()
        {
            var inv = new InventoryModel(2);
            inv.Add(_gel, 5);
            SaveCodec.RestoreInventory(inv, new[] { new StackSave { Item = "glarb_juice", Count = 1, Quality = "Standard" } }, Lookup);
            Assert.AreEqual(0, inv.Count(_gel));
            Assert.AreEqual(1, inv.Count(_juice));
        }

        [Test]
        public void Json_RoundTrip()
        {
            var data = new SaveData { Credits = 123, SecurityIncidents = 2 };
            data.Upgrades.Add("upg_precision_gauge");
            data.Player = new PlayerSave { X = 1f, Y = 0f, Z = -14f, Yaw = 90f };
            data.Inventories.Add(new InventorySave { Id = "player.pockets", Stacks = { new StackSave { Item = "glarb_juice", Count = 2, Quality = "Premium" } } });

            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(data), out SaveData parsed, out string error), error);
            Assert.AreEqual(SaveCodec.CurrentVersion, parsed.Version);
            Assert.AreEqual(123, parsed.Credits);
            Assert.AreEqual(2, parsed.SecurityIncidents);
            CollectionAssert.AreEqual(new[] { "upg_precision_gauge" }, parsed.Upgrades);
            Assert.AreEqual(-14f, parsed.Player.Z);
            Assert.AreEqual("Premium", parsed.Inventories[0].Stacks[0].Quality);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("{ this is not json")]
        [TestCase("{\"Version\":0}")]
        public void TryParse_RejectsCorruptOrEmpty(string json)
        {
            Assert.IsFalse(SaveCodec.TryParse(json, out SaveData data, out string error));
            Assert.IsNull(data);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void TryParse_RejectsSavesFromNewerVersions()
        {
            Assert.IsFalse(SaveCodec.TryParse("{\"Version\":999}", out _, out string error));
            StringAssert.Contains("newer", error);
        }

        [Test]
        public void TryParse_ClampsNegativeCredits_AndFillsMissingLists()
        {
            Assert.IsTrue(SaveCodec.TryParse("{\"Version\":1,\"Credits\":-50}", out SaveData data, out _));
            Assert.AreEqual(0, data.Credits);
            Assert.IsNotNull(data.Upgrades);
            Assert.IsNotNull(data.Inventories);
            Assert.IsNotNull(data.Player);
        }
    }
}
