using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Crafting;
using VentsAndVending.Inventory;
using VentsAndVending.Saving;

namespace VentsAndVending.Tests
{
    public sealed class ResourceLoopTests
    {
        private ItemDefinition _axe, _raw, _gel, _junk;
        private InventoryModel _inventory;
        [SetUp] public void Setup()
        {
            _axe=Item("axe",1);_raw=Item("raw",20);_gel=Item("gel",10);_junk=Item("junk",1);
            _inventory=new InventoryModel(4);_inventory.Add(_axe,1);
        }
        private static ItemDefinition Item(string id,int stack)
        {var item=ScriptableObject.CreateInstance<ItemDefinition>();item.Configure(id,id,"",ItemCategory.Misc,stack,1,Color.white);return item;}
        [TearDown] public void Cleanup()
        {Object.DestroyImmediate(_axe);Object.DestroyImmediate(_raw);Object.DestroyImmediate(_gel);Object.DestroyImmediate(_junk);}
        [Test] public void MiningNeedsCarriedPickaxe()
        {var d=new CrystalDeposit();_inventory.Remove(_axe,1);Assert.IsFalse(d.Strike(_inventory,_axe,_raw,out _));Assert.AreEqual(0,d.Hits);}
        [Test] public void ThreeStrikesAwardOneHarvest()
        {var d=new CrystalDeposit();for(int i=0;i<2;i++)Assert.IsTrue(d.Strike(_inventory,_axe,_raw,out _));Assert.AreEqual(0,_inventory.Count(_raw));Assert.IsTrue(d.Strike(_inventory,_axe,_raw,out _));Assert.AreEqual(3,_inventory.Count(_raw));Assert.IsFalse(d.Available);Assert.IsFalse(d.Strike(_inventory,_axe,_raw,out _));}
        [Test] public void FullInventoryDoesNotConsumeFinalStrike()
        {var d=new CrystalDeposit();d.Strike(_inventory,_axe,_raw,out _);d.Strike(_inventory,_axe,_raw,out _);_inventory.Add(_junk,3);Assert.IsFalse(d.Strike(_inventory,_axe,_raw,out _));Assert.AreEqual(2,d.Hits);Assert.IsTrue(d.Available);_inventory.Remove(_junk,1);Assert.IsTrue(d.Strike(_inventory,_axe,_raw,out _));Assert.AreEqual(3,_inventory.Count(_raw));}
        [Test] public void DepositsRegrowOnlyAfterGameplayTime()
        {var d=new CrystalDeposit();for(int i=0;i<3;i++)d.Strike(_inventory,_axe,_raw,out _);d.Tick(0);Assert.AreEqual(180,d.RegrowRemaining);d.Tick(179);Assert.IsFalse(d.Available);d.Tick(1);Assert.IsTrue(d.Available);}
        [Test] public void NegativeTimeCannotAdvanceDeposit()
        {var d=new CrystalDeposit();d.Restore(new DepositSave{RegrowRemaining=30});d.Tick(-10);Assert.AreEqual(30,d.RegrowRemaining);}
        [Test] public void PartialStrikeProgressRestores()
        {var d=new CrystalDeposit();d.Restore(new DepositSave{Hits=2});Assert.IsTrue(d.Strike(_inventory,_axe,_raw,out _));Assert.AreEqual(3,_inventory.Count(_raw));}
        [Test] public void InvalidDepositStateIsClamped()
        {var d=new CrystalDeposit();d.Restore(new DepositSave{Hits=100,RegrowRemaining=float.NaN});Assert.AreEqual(2,d.Hits);Assert.IsTrue(d.Available);d.Restore(new DepositSave{Hits=2,RegrowRemaining=1000});Assert.AreEqual(0,d.Hits);Assert.AreEqual(180,d.RegrowRemaining);}
        [Test] public void RefineryRequiresThreeRawCrystals()
        {var p=new RefiningProcess();_inventory.Add(_raw,2);Assert.IsFalse(p.Start(_inventory,_raw));Assert.AreEqual(2,_inventory.Count(_raw));Assert.IsFalse(p.HasBatch);}
        [Test] public void RefineryConsumesExactlyOneBatchAndRejectsRestart()
        {var p=new RefiningProcess();_inventory.Add(_raw,6);Assert.IsTrue(p.Start(_inventory,_raw));Assert.AreEqual(3,_inventory.Count(_raw));Assert.IsFalse(p.Start(_inventory,_raw));Assert.AreEqual(3,_inventory.Count(_raw));}
        [Test] public void RefineryCannotCollectEarly()
        {var p=new RefiningProcess();_inventory.Add(_raw,3);p.Start(_inventory,_raw);p.Tick(7);Assert.IsFalse(p.Collect(_inventory,_gel));Assert.AreEqual(0,_inventory.Count(_gel));}
        [Test] public void RefineryProducesExistingIngredientExactlyOnce()
        {var p=new RefiningProcess();_inventory.Add(_raw,3);p.Start(_inventory,_raw);p.Tick(8);Assert.IsTrue(p.Collect(_inventory,_gel));Assert.AreEqual(2,_inventory.Count(_gel));Assert.IsFalse(p.Collect(_inventory,_gel));}
        [Test] public void FullOutputInventoryKeepsBatchSafe()
        {var p=new RefiningProcess();_inventory.Add(_raw,3);p.Start(_inventory,_raw);p.Tick(8);_inventory.Add(_junk,3);Assert.IsFalse(p.Collect(_inventory,_gel));Assert.IsTrue(p.Ready);_inventory.Remove(_junk,1);Assert.IsTrue(p.Collect(_inventory,_gel));}
        [Test] public void PauseDoesNotAdvanceRefinery()
        {var p=new RefiningProcess();_inventory.Add(_raw,3);p.Start(_inventory,_raw);p.Tick(0);Assert.AreEqual(8,p.Remaining);p.Tick(-5);Assert.AreEqual(8,p.Remaining);}
        [Test] public void ResourceStateSurvivesJsonRoundTrip()
        {var d=new CrystalDeposit();for(int i=0;i<3;i++)d.Strike(_inventory,_axe,_raw,out _);d.Tick(23);var p=new RefiningProcess();p.Start(_inventory,_raw);p.Tick(3);var save=new SaveData();save.Deposits.Add(d.Capture("vein"));save.Refineries.Add(p.Capture("den"));Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(save),out var parsed,out _));var restored=new CrystalDeposit();restored.Restore(parsed.Deposits[0]);Assert.AreEqual(157,restored.RegrowRemaining);var refinery=new RefiningProcess();refinery.Restore(parsed.Refineries[0]);Assert.AreEqual(5,refinery.Remaining);refinery.Tick(5);Assert.IsTrue(refinery.Collect(_inventory,_gel));Assert.IsFalse(refinery.Collect(_inventory,_gel));}
        [Test] public void OldSaveGetsEmptyResourceLists()
        {Assert.IsTrue(SaveCodec.TryParse("{\"Version\":5}",out var save,out _));Assert.NotNull(save.Deposits);Assert.NotNull(save.Refineries);Assert.AreEqual(SaveCodec.CurrentVersion,save.Version);}
        [Test] public void ReadyBatchRestoresAndCanBeCollectedOnce()
        {var p=new RefiningProcess();p.Restore(new RefiningSave{HasBatch=true,Remaining=0});Assert.IsTrue(p.Collect(_inventory,_gel));Assert.IsFalse(p.Collect(_inventory,_gel));}
    }
}
