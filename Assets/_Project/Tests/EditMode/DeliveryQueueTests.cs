using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Economy;
using VentsAndVending.Inventory;
using VentsAndVending.Orders;
using VentsAndVending.Saving;

namespace VentsAndVending.Tests
{
    public sealed class DeliveryQueueTests
    {
        private ItemDefinition _a,_b,_junk;
        private Wallet _wallet;
        private InventoryModel _bay;
        private DeliveryQueue _queue;
        private static ItemDefinition Item(string id,int stack)
        {var item=ScriptableObject.CreateInstance<ItemDefinition>();item.Configure(id,id,"",ItemCategory.Ingredient,stack,1,Color.white);return item;}
        [SetUp] public void Setup()
        {_a=Item("a",10);_b=Item("b",10);_junk=Item("junk",1);_wallet=new Wallet(100);_bay=new InventoryModel(24);_queue=new DeliveryQueue();}
        [TearDown] public void Cleanup(){Object.DestroyImmediate(_a);Object.DestroyImmediate(_b);Object.DestroyImmediate(_junk);}
        private SupplyListing Listing(ItemDefinition item)=>new SupplyListing{Item=item,Price=4};
        private void Order(ItemDefinition item)=>Assert.AreEqual(PurchaseResult.OrderQueued,_queue.Order(Listing(item),_wallet,_bay));
        [Test] public void OrdersChargeButDoNotInstantlyDeliver()
        {Order(_a);Assert.AreEqual(96,_wallet.Balance);Assert.AreEqual(0,_bay.Count(_a));Assert.AreEqual(1,_queue.Orders.Count);Assert.AreEqual(46,_queue.NextWindowSeconds);}
        [Test] public void RefusedOrdersDoNotCharge()
        {_wallet.Restore(3);Assert.AreEqual(PurchaseResult.CannotAfford,_queue.Order(Listing(_a),_wallet,_bay));Assert.AreEqual(3,_wallet.Balance);Assert.AreEqual(0,_queue.Orders.Count);}
        [Test] public void OrderRejectsNegativePrice()
        {Assert.AreEqual(PurchaseResult.QueueFull,_queue.Order(new SupplyListing{Item=_a,Price=-1},_wallet,_bay));Assert.AreEqual(100,_wallet.Balance);}
        [Test] public void ManifestLimitIsEnforced()
        {for(int i=0;i<12;i++)Order(_a);int balance=_wallet.Balance;Assert.AreEqual(PurchaseResult.QueueFull,_queue.Order(Listing(_a),_wallet,_bay));Assert.AreEqual(balance,_wallet.Balance);}
        [Test] public void MixedGoodsReserveActualSharedBaySpace()
        {_bay=new InventoryModel(1);Order(_a);int balance=_wallet.Balance;Assert.AreEqual(PurchaseResult.LockerFull,_queue.Order(Listing(_b),_wallet,_bay));Assert.AreEqual(balance,_wallet.Balance);}
        [Test] public void DispatchTravelAndUnloadAllTakeTime()
        {Order(_a);_queue.Tick(24,_bay);Assert.AreEqual(DeliveryPhase.Waiting,_queue.Phase);_queue.Tick(1,_bay);Assert.AreEqual(DeliveryPhase.Outbound,_queue.Phase);_queue.Tick(18,_bay);Assert.AreEqual(DeliveryPhase.Unloading,_queue.Phase);Assert.AreEqual(0,_bay.Count(_a));_queue.Tick(3,_bay);Assert.AreEqual(1,_bay.Count(_a));Assert.AreEqual(DeliveryPhase.Returning,_queue.Phase);}
        [Test] public void BeforeWindowEndsThereIsNoParcel()
        {Order(_a);_queue.Tick(45,_bay);Assert.AreEqual(0,_bay.Count(_a));_queue.Tick(1,_bay);Assert.AreEqual(1,_bay.Count(_a));}
        [Test] public void LateOrdersWaitForNextRun()
        {Order(_a);_queue.Tick(30,_bay);Order(_b);Assert.Greater(_queue.NextWindowSeconds,46);_queue.Tick(16,_bay);Assert.AreEqual(1,_bay.Count(_a));Assert.AreEqual(0,_bay.Count(_b));_queue.Tick(18+46,_bay);Assert.AreEqual(1,_bay.Count(_b));}
        [Test] public void CourierReturnsBeforeNextDispatch()
        {Order(_a);_queue.Tick(46,_bay);_queue.Tick(18,_bay);Assert.AreEqual(DeliveryPhase.Waiting,_queue.Phase);Assert.AreEqual(25,_queue.Remaining);}
        [Test] public void FullBayRetainsPaidManifest()
        {_bay=new InventoryModel(1);Order(_a);_bay.Add(_junk,1);_queue.Tick(46,_bay);Assert.IsTrue(_queue.DropBlocked);Assert.AreEqual(1,_queue.Orders.Count);Assert.AreEqual(0,_bay.Count(_a));_bay.Remove(_junk,1);_queue.Tick(.1f,_bay);Assert.AreEqual(1,_bay.Count(_a));Assert.AreEqual(0,_queue.Orders.Count);}
        [Test] public void BatchCapacityChecksAreAtomic()
        {_bay=new InventoryModel(2);Order(_a);Order(_b);_bay.Add(_junk,1);_queue.Tick(46,_bay);Assert.IsTrue(_queue.DropBlocked);Assert.AreEqual(0,_bay.Count(_a));Assert.AreEqual(0,_bay.Count(_b));}
        [Test] public void PauseDoesNotAdvanceCourier()
        {Order(_a);_queue.Tick(0,_bay);Assert.AreEqual(25,_queue.Remaining);_queue.Tick(-1,_bay);Assert.AreEqual(25,_queue.Remaining);}
        [Test] public void MidTransitJsonSaveResumesWithoutDuplication()
        {Order(_a);_queue.Tick(30,_bay);var save=new SaveData{Deliveries=_queue.Capture()};Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(save),out var parsed,out _));var restored=new DeliveryQueue();Assert.AreEqual(0,restored.Restore(parsed.Deliveries,id=>id==_a.Id?_a:null));Assert.AreEqual(DeliveryPhase.Outbound,restored.Phase);Assert.AreEqual(13,restored.Remaining);restored.Tick(16,_bay);Assert.AreEqual(1,_bay.Count(_a));restored.Tick(300,_bay);Assert.AreEqual(1,_bay.Count(_a));}
        [Test] public void DeliveredManifestDoesNotRespawnOnLoad()
        {Order(_a);_queue.Tick(46,_bay);var restored=new DeliveryQueue();restored.Restore(_queue.Capture(),id=>_a);restored.Tick(200,_bay);Assert.AreEqual(1,_bay.Count(_a));}
        [Test] public void UnavailableSavedGoodsAreRefundable()
        {Order(_a);var restored=new DeliveryQueue();Assert.AreEqual(4,restored.Restore(_queue.Capture(),id=>null));Assert.AreEqual(0,restored.Orders.Count);}
        [Test] public void LegacySaveHasEmptyCourierManifest()
        {Assert.IsTrue(SaveCodec.TryParse("{\"Version\":6}",out var save,out _));Assert.NotNull(save.Deliveries);Assert.IsEmpty(save.Deliveries.Orders);}
        [Test] public void IdleCourierDoesNotCountDownBeforeAnOrder()
        {_queue.Tick(500,_bay);Assert.AreEqual(25,_queue.Remaining);Order(_a);Assert.AreEqual(46,_queue.NextWindowSeconds);}
        [Test] public void CombinedCartChargesOnceAndArrivesAsOneBatch()
        {
            var cart=new SupplyCart();cart.Add(Listing(_a));cart.Add(Listing(_a));cart.Add(Listing(_b));
            Assert.AreEqual(12,cart.Total);Assert.AreEqual(PurchaseResult.OrderQueued,_queue.OrderCart(cart.Items,_wallet,_bay));
            Assert.AreEqual(88,_wallet.Balance);Assert.AreEqual(0,_bay.Count(_a));
            _queue.Tick(46,_bay);Assert.AreEqual(2,_bay.Count(_a));Assert.AreEqual(1,_bay.Count(_b));Assert.AreEqual(1,_queue.DeliveredBatches);
        }
        [Test] public void UnaffordableCombinedCartDoesNotPartiallyChargeOrQueue()
        {_wallet.Restore(6);Assert.AreEqual(PurchaseResult.CannotAfford,_queue.OrderCart(new[]{Listing(_a),Listing(_b)},_wallet,_bay));Assert.AreEqual(6,_wallet.Balance);Assert.IsEmpty(_queue.Orders);}
        [Test] public void CombinedCartCapacityFailureIsAtomic()
        {_bay=new InventoryModel(1);Assert.AreEqual(PurchaseResult.LockerFull,_queue.OrderCart(new[]{Listing(_a),Listing(_b)},_wallet,_bay));Assert.AreEqual(100,_wallet.Balance);Assert.IsEmpty(_queue.Orders);}
        [Test] public void CombinedCartLimitIncludesExistingManifest()
        {Order(_a);var cart=new SupplyCart();for(int i=0;i<12;i++)Assert.IsTrue(cart.Add(Listing(_a)));Assert.IsFalse(cart.Add(Listing(_a)));Assert.AreEqual(PurchaseResult.QueueFull,_queue.OrderCart(cart.Items,_wallet,_bay));Assert.AreEqual(96,_wallet.Balance);Assert.AreEqual(1,_queue.Orders.Count);}
        [Test] public void DraftQuantitiesCanBeRemovedAndClearedWithoutPayment()
        {var cart=new SupplyCart();cart.Add(Listing(_a));cart.Add(Listing(_a));cart.Add(Listing(_b));cart.Remove(_a);Assert.AreEqual(2,cart.Count);Assert.AreEqual(8,cart.Total);cart.Clear();Assert.AreEqual(0,cart.Count);Assert.AreEqual(100,_wallet.Balance);}
        [Test] public void EmptyCheckoutIsRejectedWithoutPayment()
        {Assert.AreEqual(PurchaseResult.QueueFull,_queue.OrderCart(new SupplyListing[0],_wallet,_bay));Assert.AreEqual(100,_wallet.Balance);}
    }
}
