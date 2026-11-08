using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Crafting;
using VentsAndVending.Inventory;

namespace VentsAndVending.Tests
{
    public class CraftingTests
    {
        private ItemDefinition _glarb;
        private ItemDefinition _lumina;
        private ItemDefinition _stabiliser;
        private ItemDefinition _nebula;
        private ProductDefinition _juice;
        private RecipeDefinition _recipe;

        [SetUp]
        public void SetUp()
        {
            _glarb = Item("glarb_extract");
            _lumina = Item("lumina_gel");
            _stabiliser = Item("stabiliser_gel");
            _nebula = Item("nebula_sap");

            _juice = ScriptableObject.CreateInstance<ProductDefinition>();
            _juice.Configure("glarb_juice", "Glarb-Juice", string.Empty, ItemCategory.Product, 10, 12, Color.green);
            _juice.ConfigureProduct("Energy stimulant", ProductDifficulty.Beginner, DemandLevel.High, 12, 24);

            _recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            _recipe.Configure("recipe_glarb_juice", _juice, 1, CraftingEquipment.MixingStation, 6f, 2.5f,
                (_glarb, 1), (_lumina, 1), (_stabiliser, 1));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in new Object[] { _glarb, _lumina, _stabiliser, _nebula, _juice, _recipe })
            {
                Object.DestroyImmediate(o);
            }
        }

        private static ItemDefinition Item(string id)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Configure(id, id, string.Empty, ItemCategory.Ingredient, 10, 4, Color.white);
            return item;
        }

        // ------------------------------------------------------------------ MixingRules

        [TestCase(0.30f, true, ItemQuality.Poor)]          // under-mixed
        [TestCase(0.70f, true, ItemQuality.Standard)]
        [TestCase(0.86f, true, ItemQuality.Premium)]
        [TestCase(0.93f, true, ItemQuality.Exceptional)]   // sweet spot + correct order
        [TestCase(0.99f, true, ItemQuality.Premium)]
        [TestCase(1.05f, true, ItemQuality.Standard)]      // slightly over
        [TestCase(1.20f, true, ItemQuality.Poor)]          // overheated
        [TestCase(0.93f, false, ItemQuality.Standard)]     // wrong order: no Exceptional, Premium → Standard
        [TestCase(0.70f, false, ItemQuality.Poor)]
        [TestCase(0.30f, false, ItemQuality.Poor)]         // never below Poor
        public void Grade(float progress, bool correctOrder, ItemQuality expected)
        {
            Assert.AreEqual(expected, MixingRules.Grade(progress, correctOrder));
        }

        // ------------------------------------------------------------------ RecipeDefinition

        [Test]
        public void Check_ExactIngredients_Matches()
        {
            RecipeCheck check = _recipe.Check(new List<ItemDefinition> { _glarb, _lumina, _stabiliser });
            Assert.IsTrue(check.Matches);
            Assert.IsTrue(check.CorrectOrder);
        }

        [Test]
        public void Check_AnyOrder_StillMatches_ButFlagsOrder()
        {
            RecipeCheck check = _recipe.Check(new List<ItemDefinition> { _stabiliser, _glarb, _lumina });
            Assert.IsTrue(check.Matches);
            Assert.IsFalse(check.CorrectOrder);
        }

        [Test]
        public void Check_Missing_ReportsWhat()
        {
            RecipeCheck check = _recipe.Check(new List<ItemDefinition> { _glarb, _lumina });
            Assert.IsFalse(check.Matches);
            StringAssert.Contains("Missing stabiliser_gel", check.Problem);
        }

        [Test]
        public void Check_TooMuch_IsIncorrectQuantity()
        {
            RecipeCheck check = _recipe.Check(new List<ItemDefinition> { _glarb, _glarb, _lumina, _stabiliser });
            Assert.IsFalse(check.Matches);
            StringAssert.Contains("Too much glarb_extract", check.Problem);
        }

        [Test]
        public void Check_ForeignIngredient_DoesNotMatch()
        {
            RecipeCheck check = _recipe.Check(new List<ItemDefinition> { _glarb, _lumina, _stabiliser, _nebula });
            Assert.IsFalse(check.Matches);
            StringAssert.Contains("nebula_sap", check.Problem);
        }

        // ------------------------------------------------------------------ MixingProcess

        private MixingProcess LoadedProcess(params ItemDefinition[] items)
        {
            var p = new MixingProcess(6);
            foreach (ItemDefinition i in items)
            {
                p.Load(i);
            }
            return p;
        }

        [Test]
        public void Process_FullCycle_PerfectTiming_GivesExceptional()
        {
            MixingProcess p = LoadedProcess(_glarb, _lumina, _stabiliser);
            Assert.AreSame(_recipe, p.FindMatch(new[] { _recipe }));
            Assert.IsTrue(p.TryStart(_recipe));
            Assert.AreEqual(MixingPhase.Mixing, p.Phase);

            p.Tick(6f * 0.93f);
            p.StopMixing();
            Assert.AreEqual(MixingPhase.Settling, p.Phase);
            Assert.AreEqual(ItemQuality.Exceptional, p.ResultQuality);

            p.Tick(3f);
            Assert.AreEqual(MixingPhase.Ready, p.Phase);

            Assert.IsTrue(p.Collect(out ProductDefinition product, out int count, out ItemQuality quality));
            Assert.AreSame(_juice, product);
            Assert.AreEqual(1, count);
            Assert.AreEqual(ItemQuality.Exceptional, quality);
            Assert.AreEqual(MixingPhase.Idle, p.Phase);
            Assert.AreEqual(0, p.Loaded.Count);
        }

        [Test]
        public void Process_LeftRunning_AutoStopsOverheated_AndIsRecoverable()
        {
            MixingProcess p = LoadedProcess(_glarb, _lumina, _stabiliser);
            bool overheatedEvent = false;
            p.Stopped += (q, overheated) => overheatedEvent = overheated;
            p.TryStart(_recipe);

            p.Tick(60f);
            Assert.IsTrue(overheatedEvent);
            Assert.IsTrue(p.Overheated);
            Assert.AreEqual(ItemQuality.Poor, p.ResultQuality); // still yields a (Poor) product

            p.Tick(3f);
            Assert.IsTrue(p.Collect(out _, out _, out ItemQuality q));
            Assert.AreEqual(ItemQuality.Poor, q);
        }

        [Test]
        public void Process_CannotStart_WithoutMatch()
        {
            MixingProcess p = LoadedProcess(_glarb, _lumina);
            Assert.IsNull(p.FindMatch(new[] { _recipe }));
            Assert.IsFalse(p.TryStart(_recipe));
            Assert.AreEqual(MixingPhase.Idle, p.Phase);
        }

        [Test]
        public void Process_CannotLoadOrEmpty_WhileMixing()
        {
            MixingProcess p = LoadedProcess(_glarb, _lumina, _stabiliser);
            p.TryStart(_recipe);
            Assert.IsFalse(p.Load(_glarb));
            Assert.AreEqual(0, p.EmptyVessel().Count);
            Assert.AreEqual(3, p.Loaded.Count);
        }

        [Test]
        public void Process_RespectsCapacity_AndEmptyReturnsContents()
        {
            var p = new MixingProcess(2);
            Assert.IsTrue(p.Load(_glarb));
            Assert.IsTrue(p.Load(_lumina));
            Assert.IsFalse(p.Load(_stabiliser));
            CollectionAssert.AreEqual(new[] { _glarb, _lumina }, p.EmptyVessel());
            Assert.AreEqual(0, p.Loaded.Count);
        }

        // ------------------------------------------------------------------ Quality-aware inventory & pricing

        [Test]
        public void Inventory_DoesNotStackDifferentQualities()
        {
            var inv = new InventoryModel(4);
            inv.Add(_juice, 1, ItemQuality.Premium);
            inv.Add(_juice, 1, ItemQuality.Standard);
            inv.Add(_juice, 1, ItemQuality.Premium);
            Assert.AreEqual(2, inv.UsedSlots);
            Assert.AreEqual(2, inv.Count(_juice, ItemQuality.Premium));
            Assert.AreEqual(3, inv.Count(_juice));
        }

        [Test]
        public void Inventory_TransferKeepsQuality()
        {
            var a = new InventoryModel(2);
            var b = new InventoryModel(2);
            a.Add(_juice, 2, ItemQuality.Exceptional);
            a.TransferSlotTo(0, b);
            Assert.AreEqual(ItemQuality.Exceptional, b.Slots[0].Quality);
        }

        [Test]
        public void Inventory_RemoveByQuality_OnlyTakesThatQuality()
        {
            var inv = new InventoryModel(4);
            inv.Add(_juice, 2, ItemQuality.Poor);
            inv.Add(_juice, 2, ItemQuality.Premium);
            Assert.AreEqual(2, inv.Remove(_juice, 5, ItemQuality.Poor));
            Assert.AreEqual(2, inv.Count(_juice, ItemQuality.Premium));
        }

        [TestCase(ItemQuality.Poor, 14)]
        [TestCase(ItemQuality.Standard, 24)]
        [TestCase(ItemQuality.Premium, 34)]
        [TestCase(ItemQuality.Exceptional, 48)]
        public void Product_SalePriceScalesWithQuality(ItemQuality quality, int expected)
        {
            Assert.AreEqual(expected, _juice.SalePriceFor(quality));
        }
    }
}
