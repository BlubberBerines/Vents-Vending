using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Crafting;
using VentsAndVending.Inventory;
using VentsAndVending.Progression;

namespace VentsAndVending.Tests
{
    public class ProgressionTests
    {
        private UpgradeDefinition _gauge;
        private UpgradeDefinition _stirrer;
        private UpgradeDefinition _pockets;

        [SetUp]
        public void SetUp()
        {
            _gauge = Upgrade("precision_gauge", 60, null, (UpgradeKeys.MixerWindow, 0.05f));
            _stirrer = Upgrade("industrial_stirrer", 120, _gauge, (UpgradeKeys.MixerWindow, 0.04f));
            _pockets = Upgrade("deeper_pockets", 40, null, (UpgradeKeys.PocketsCapacity, 4f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gauge);
            Object.DestroyImmediate(_stirrer);
            Object.DestroyImmediate(_pockets);
        }

        private static UpgradeDefinition Upgrade(string id, int cost, UpgradeDefinition prerequisite, params (string, float)[] effects)
        {
            var u = ScriptableObject.CreateInstance<UpgradeDefinition>();
            u.Configure(id, id, string.Empty, UpgradeCategory.Production, 1, cost, prerequisite, effects);
            return u;
        }

        [Test]
        public void Check_ReportsAffordability()
        {
            var ledger = new UpgradeLedger();
            Assert.AreEqual(PurchaseCheck.CannotAfford, ledger.Check(_gauge, 59));
            Assert.AreEqual(PurchaseCheck.Available, ledger.Check(_gauge, 60));
        }

        [Test]
        public void Check_RequiresPrerequisite()
        {
            var ledger = new UpgradeLedger();
            Assert.AreEqual(PurchaseCheck.MissingPrerequisite, ledger.Check(_stirrer, 999));
            ledger.Grant(_gauge);
            Assert.AreEqual(PurchaseCheck.Available, ledger.Check(_stirrer, 999));
        }

        [Test]
        public void Grant_IsIdempotent_AndReportsOwned()
        {
            var ledger = new UpgradeLedger();
            int events = 0;
            ledger.Purchased += _ => events++;
            Assert.IsTrue(ledger.Grant(_pockets));
            Assert.IsFalse(ledger.Grant(_pockets));
            Assert.AreEqual(1, events);
            Assert.AreEqual(PurchaseCheck.AlreadyOwned, ledger.Check(_pockets, 999));
        }

        [Test]
        public void Bonus_SumsOwnedEffectsByKey()
        {
            var ledger = new UpgradeLedger();
            Assert.AreEqual(0f, ledger.Bonus(UpgradeKeys.MixerWindow));
            ledger.Grant(_gauge);
            ledger.Grant(_stirrer);
            ledger.Grant(_pockets);
            Assert.AreEqual(0.09f, ledger.Bonus(UpgradeKeys.MixerWindow), 1e-5f);
            Assert.AreEqual(4f, ledger.Bonus(UpgradeKeys.PocketsCapacity));
            Assert.IsTrue(ledger.Has(UpgradeKeys.PocketsCapacity));
            Assert.IsFalse(ledger.Has(UpgradeKeys.HideoutLighting));
        }

        [Test]
        public void Inventory_Resize_GrowsAndKeepsItems_NeverShrinks()
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Configure("x", "x", string.Empty, ItemCategory.Ingredient, 1, 1, Color.white);
            var inv = new InventoryModel(2);
            inv.Add(item, 2);
            Assert.AreEqual(0, inv.SpaceFor(item));

            inv.Resize(4);
            Assert.AreEqual(4, inv.Capacity);
            Assert.AreEqual(2, inv.Count(item));
            Assert.AreEqual(2, inv.SpaceFor(item));

            inv.Resize(1);
            Assert.AreEqual(4, inv.Capacity);
            Object.DestroyImmediate(item);
        }

        [TestCase(0.82f, 0f, ItemQuality.Standard)]
        [TestCase(0.82f, 0.05f, ItemQuality.Premium)]     // wider window catches an early stop
        [TestCase(1.04f, 0.05f, ItemQuality.Premium)]     // …and a late one
        [TestCase(0.88f, 0.05f, ItemQuality.Exceptional)] // sweet spot widens by half the bonus
        [TestCase(1.20f, 0.09f, ItemQuality.Poor)]        // never protects against overheating
        public void Grade_WithWindowBonus(float progress, float bonus, ItemQuality expected)
        {
            Assert.AreEqual(expected, MixingRules.Grade(progress, true, bonus));
        }

        [Test]
        public void PremiumEnd_StaysBelowOverheat()
        {
            Assert.Less(MixingRules.PremiumEnd(1f), MixingRules.OverheatFrom);
        }
    }
}
