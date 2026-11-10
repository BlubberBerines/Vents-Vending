using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Customers;
using VentsAndVending.Crafting;
using VentsAndVending.Inventory;
using VentsAndVending.Orders;
using VentsAndVending.Saving;

namespace VentsAndVending.Tests
{
    public sealed class CustomerRelationshipTests
    {
        private ProductDefinition _product;
        private CustomerDefinition _definition;
        private CustomerLedger _ledger;
        private InventoryModel _stock;
        [SetUp] public void Setup()
        {
            _product=ScriptableObject.CreateInstance<ProductDefinition>();_product.Configure("sample","Sample","",ItemCategory.Product,20,12,Color.green);
            _definition=ScriptableObject.CreateInstance<CustomerDefinition>();_definition.Configure("alien","Alien",new[]{"Name"},null,new[]{_product},Vector2Int.one,Vector2.zero,ItemQuality.Standard,false,2,45,new CustomerDefinition.DialogueLines());
            _ledger=new CustomerLedger();_stock=new InventoryModel(4);_stock.Add(_product,5,ItemQuality.Premium);
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(_product);Object.DestroyImmediate(_definition);}
        private string Name(bool likes)=>Enumerable.Range(0,100).Select(i=>"Passenger "+i).First(n=>CustomerLedger.LikesProduct(n,_definition.Id,_product.Id)==likes);
        [Test] public void ConversationAloneDoesNotRecruit()
        {var contact=_ledger.Meet("Passenger",_definition.Id);Assert.IsFalse(contact.Recruited);Assert.AreEqual(0,contact.Trust);Assert.AreSame(contact,_ledger.Meet("Passenger",_definition.Id));Assert.AreEqual(1,_ledger.Records.Count);}
        [Test] public void LikedSampleConsumesOneAndRecruitsNamedRegular()
        {string name=Name(true);Assert.IsTrue(_ledger.OfferSample(_definition,name,_product,ItemQuality.Premium,_stock,out _));Assert.AreEqual(4,_stock.Count(_product));Assert.IsTrue(_ledger.Find(name).Recruited);Assert.AreEqual(25,_ledger.Find(name).Trust);CollectionAssert.Contains(_ledger.Find(name).LikedProducts,_product.Id);}
        [Test] public void RepeatedSampleCannotFarmTrustOrConsumeStock()
        {string name=Name(true);_ledger.OfferSample(_definition,name,_product,ItemQuality.Premium,_stock,out _);Assert.IsFalse(_ledger.OfferSample(_definition,name,_product,ItemQuality.Premium,_stock,out _));Assert.AreEqual(4,_stock.Count(_product));Assert.AreEqual(25,_ledger.Find(name).Trust);}
        [Test] public void DislikedProductRemainsAContactNotARegular()
        {string name=Name(false);_ledger.OfferSample(_definition,name,_product,ItemQuality.Premium,_stock,out _);Assert.IsFalse(_ledger.Find(name).Recruited);CollectionAssert.Contains(_ledger.Find(name).DislikedProducts,_product.Id);}
        [Test] public void AContactCanBeRecruitedLaterWithADifferentLikedProduct()
        {
            string name=Name(false);_ledger.OfferSample(_definition,name,_product,ItemQuality.Premium,_stock,out _);
            var alternative=ScriptableObject.CreateInstance<ProductDefinition>();
            try
            {
                string id=Enumerable.Range(0,100).Select(i=>"future_product_"+i).First(p=>CustomerLedger.LikesProduct(name,_definition.Id,p));
                alternative.Configure(id,"Alternative","",ItemCategory.Product,10,12,Color.blue);_stock.Add(alternative,1,ItemQuality.Premium);
                Assert.IsTrue(_ledger.OfferSample(_definition,name,alternative,ItemQuality.Premium,_stock,out _));Assert.IsTrue(_ledger.Find(name).Recruited);CollectionAssert.Contains(_ledger.Find(name).DislikedProducts,_product.Id);
            }
            finally{Object.DestroyImmediate(alternative);}
        }
        [Test] public void BetterQualityCanPersuadeAfterPoorSample()
        {string name=Name(true);_stock.Add(_product,1,ItemQuality.Poor);_ledger.OfferSample(_definition,name,_product,ItemQuality.Poor,_stock,out _);Assert.IsFalse(_ledger.Find(name).Recruited);_ledger.OfferSample(_definition,name,_product,ItemQuality.Premium,_stock,out _);Assert.IsTrue(_ledger.Find(name).Recruited);}
        [Test] public void TasteIsStableAndVariesAcrossPassengers()
        {Assert.AreEqual(CustomerLedger.LikesProduct("A","B","C"),CustomerLedger.LikesProduct("A","B","C"));Assert.AreNotEqual(Name(true),Name(false));}
        [Test] public void RelationshipsAndTrackedDealSurviveJson()
        {
            string name=Name(true);_ledger.OfferSample(_definition,name,_product,ItemQuality.Premium,_stock,out _);
            var save=new SaveData();save.Orders.Customers.AddRange(_ledger.Records);save.Orders.TrackedOrderId="ORD1";
            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(save),out var parsed,out _));var restored=new CustomerLedger();restored.Restore(parsed.Orders.Customers);
            Assert.IsTrue(restored.Find(name).Recruited);Assert.AreEqual(25,restored.Find(name).Trust);Assert.AreEqual("ORD1",parsed.Orders.TrackedOrderId);
        }
        [Test] public void PlayerChoosesExactTimeAndCustomerKeepsLocation()
        {
            var board=new OrderBoard();var order=board.AddOffer(new CustomerOrder{SpotId="LOUNGE",RoomMeeting=true});
            Assert.IsTrue(board.Schedule(order.Id,360));Assert.AreEqual(360,order.WindowOpensAt);Assert.AreEqual(510,order.WindowClosesAt);Assert.AreEqual("LOUNGE",order.SpotId);
            board.Tick(359,new System.Random(1));Assert.AreEqual(OrderStatus.Accepted,order.Status);board.Tick(1,new System.Random(1));Assert.AreEqual(OrderStatus.Waiting,order.Status);
        }
        [Test] public void PastAndInvalidAppointmentsAreRejected()
        {var board=new OrderBoard();var o=board.AddOffer(new CustomerOrder());Assert.IsFalse(board.Schedule(o.Id,0));Assert.IsFalse(board.Schedule(o.Id,float.NaN));Assert.IsFalse(board.Schedule(o.Id,StationTime.DaySeconds+1));Assert.AreEqual(OrderStatus.Offered,o.Status);}
        [Test] public void StationClockCrossesMidnightAndDistinguishesNight()
        {Assert.AreEqual("Day 1 / 08:00",StationTime.Label(0));Assert.IsFalse(StationTime.IsNight(0));Assert.IsTrue(StationTime.IsNight(12*60));Assert.AreEqual("Day 2 / 00:00",StationTime.Label(16*60));Assert.AreEqual("Day 2 / 08:00",StationTime.Label(StationTime.DaySeconds));}
        [Test] public void TrackingOnlyAllowsOneActiveDealAndCannotTrackOffers()
        {
            var go=new GameObject("Tracking test");try{var m=go.AddComponent<OrderManager>();var a=m.Board.AddOffer(new CustomerOrder{RoomMeeting=true});var b=m.Board.AddOffer(new CustomerOrder{RoomMeeting=true});m.Track(a.Id);Assert.IsNull(m.TrackedOrder);m.Board.Schedule(a.Id,60);m.Board.Schedule(b.Id,90);m.Track(a.Id);m.Track(b.Id);Assert.AreSame(b,m.TrackedOrder);m.Track(null);Assert.IsNull(m.TrackedOrder);}finally{Object.DestroyImmediate(go);}
        }
    }
}
