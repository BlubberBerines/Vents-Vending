using NUnit.Framework;
using VentsAndVending.Player.Appearance;
using VentsAndVending.Saving;

namespace VentsAndVending.Tests.EditMode
{
    public sealed class AppearanceTests
    {
        [Test]
        public void Equip_RequiresOwnership()
        {
            var a = new AppearanceState();
            Assert.IsFalse(a.Equip(OutfitSlot.Head, "beanie"));
            a.Own("beanie");
            Assert.IsTrue(a.Equip(OutfitSlot.Head, "beanie"));
            Assert.AreEqual("beanie", a.Equipped(OutfitSlot.Head));
        }

        [Test]
        public void Equip_ReplacesTheSlot_AndUnequipEmptiesIt()
        {
            var a = new AppearanceState();
            a.Own("beanie");
            a.Own("cap");
            a.Equip(OutfitSlot.Head, "beanie");
            a.Equip(OutfitSlot.Head, "cap");
            Assert.AreEqual("cap", a.Equipped(OutfitSlot.Head));
            a.Unequip(OutfitSlot.Head);
            Assert.IsNull(a.Equipped(OutfitSlot.Head));
        }

        [Test]
        public void Own_ReportsOnlyNewItems_AndRaisesChanged()
        {
            var a = new AppearanceState();
            int changes = 0;
            a.Changed += () => changes++;
            Assert.IsTrue(a.Own("boots"));
            Assert.IsFalse(a.Own("boots"));
            Assert.IsFalse(a.Own(""));
            Assert.AreEqual(1, changes);
        }

        [TestCase(3, 3)]
        [TestCase(8, 0)]
        [TestCase(-1, 7)]
        public void SetSkin_WrapsIntoThePalette(int input, int expected)
        {
            var a = new AppearanceState();
            a.SetSkin(input);
            Assert.AreEqual(expected, a.SkinIndex);
        }

        [Test]
        public void CaptureRestore_RoundTripsThroughJson_AndDropsUnownedOrBadSlots()
        {
            var a = new AppearanceState();
            a.Own("jumpsuit");
            a.Own("boots");
            a.Equip(OutfitSlot.Torso, "jumpsuit");
            a.Equip(OutfitSlot.Feet, "boots");
            a.SetSkin(5);

            AppearanceSave save = a.Capture();
            save.Equipped.Add(new EquippedSave { Slot = "Head", Id = "crown" });   // not owned
            save.Equipped.Add(new EquippedSave { Slot = "Tail", Id = "boots" });   // no such slot

            var data = new SaveData { Appearance = save };
            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(data), out SaveData parsed, out string error), error);
            var b = new AppearanceState();
            b.Restore(parsed.Appearance);

            Assert.AreEqual(5, b.SkinIndex);
            Assert.AreEqual("jumpsuit", b.Equipped(OutfitSlot.Torso));
            Assert.AreEqual("boots", b.Equipped(OutfitSlot.Feet));
            Assert.IsNull(b.Equipped(OutfitSlot.Head));
            Assert.IsTrue(b.IsOwned("boots"));
        }

        [Test]
        public void OlderSaves_HaveABlankAppearance()
        {
            Assert.IsTrue(SaveCodec.TryParse("{\"Version\":3,\"Credits\":1}", out SaveData data, out string error), error);
            Assert.IsTrue(AppearanceState.IsBlank(data.Appearance));
        }
    }
}
