using System;
using NUnit.Framework;
using VentsAndVending.Inventory;
using VentsAndVending.Orders;
using VentsAndVending.Saving;

namespace VentsAndVending.Tests.EditMode
{
    public sealed class OrderTests
    {
        [Test] public void VisitorJourneySurvivesSaveJsonWithoutChangingDeadline()
        {
            var order=Offer();order.HasVisitorPosition=true;order.VisitorX=58.2f;order.VisitorY=4.6f;order.VisitorZ=12.3f;order.MeetingX=59;order.MeetingY=4.6f;order.MeetingZ=14;order.WindowClosesAt=200;
            var data=new SaveData();data.Orders.Orders.Add(order);
            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(data),out var parsed,out _));var restored=parsed.Orders.Orders[0];
            Assert.IsTrue(restored.HasVisitorPosition);Assert.AreEqual(58.2f,restored.VisitorX);Assert.AreEqual(14,restored.MeetingZ);Assert.AreEqual(200,restored.WindowClosesAt);
        }
        [Test] public void OldSavesHaveNoInventedVisitorOrWorldDrops()
        {Assert.IsTrue(SaveCodec.TryParse("{\"Version\":7,\"Orders\":{\"Orders\":[{\"Id\":\"ORD001\"}]}}",out var data,out _));Assert.IsFalse(data.Orders.Orders[0].HasVisitorPosition);Assert.IsEmpty(data.WorldDrops);}
        [Test] public void LargeClockStepMissesExpiredMeetingWithoutSpawningCustomer()
        {
            var board=new OrderBoard();var order=board.AddOffer(Offer());board.Accept(order.Id,new Random(1));
            int spawned=0;board.WindowOpened+=_=>spawned++;
            board.Tick(order.WindowClosesAt+1,new Random(1));
            Assert.AreEqual(OrderStatus.Missed,order.Status);Assert.AreEqual(0,spawned);Assert.IsFalse(board.Complete(order.Id,100));
        }
        [Test] public void CompletedDealCannotPayOrAwardTrustAgain()
        {
            var board=new OrderBoard();var order=board.AddOffer(Offer());board.Accept(order.Id,new Random(1));
            board.Tick(order.WindowOpensAt,new Random(1));Assert.IsTrue(board.Complete(order.Id,10));int trust=board.Trust;
            Assert.IsFalse(board.Complete(order.Id,100));Assert.AreEqual(10,order.Payout);Assert.AreEqual(trust,board.Trust);
        }
        /// <summary>Always the shortest interval / lead time: makes timing tests deterministic.</summary>
        private sealed class ZeroRandom : Random
        {
            public override double NextDouble() => 0.0;
        }

        private static CustomerOrder Offer(string spot = "A") => new CustomerOrder
        {
            CustomerId = "blorb", CustomerName = "Glorbo", ProductId = "glarb_juice", Quantity = 2,
            MinimumQuality = ItemQuality.Standard, Markup = 0.25f, SpotId = spot
        };

        private static OrderBoard BoardWithFactory(Func<string> spot = null)
        {
            var board = new OrderBoard();
            board.OfferFactory = b => Offer(spot?.Invoke() ?? "A");
            return board;
        }

        [Test]
        public void FirstOffer_ArrivesAfterTheInitialDelay()
        {
            var board = BoardWithFactory();
            var rng = new Random(1);
            board.Tick(OrderRules.FirstOfferDelay - 0.5f, rng);
            Assert.AreEqual(0, board.OfferCount);
            board.Tick(1f, rng);
            Assert.AreEqual(1, board.OfferCount);
            Assert.AreEqual("ORD001", board.Orders[0].Id);
        }

        [Test]
        public void Offers_AreCappedAtMaxPending()
        {
            var board = BoardWithFactory();
            var rng = new ZeroRandom();
            for (int i = 0; i < 6; i++)
            {
                board.Tick(OrderRules.MinOfferInterval, rng); // one arrival per tick, none old enough to expire yet
            }
            Assert.AreEqual(OrderRules.MaxPendingOffers, board.OfferCount);
            board.Tick(OrderRules.MaxOfferInterval + 1f, rng);
            Assert.LessOrEqual(board.OfferCount, OrderRules.MaxPendingOffers);
        }

        [Test]
        public void UnansweredOffer_Expires()
        {
            var board = new OrderBoard();
            CustomerOrder o = board.AddOffer(Offer());
            bool expired = false;
            board.OfferExpired += _ => expired = true;
            board.Tick(OrderRules.OfferLifetime + 0.1f, new Random(3));
            Assert.AreEqual(OrderStatus.Expired, o.Status);
            Assert.IsTrue(expired);
        }

        [Test]
        public void Accept_SchedulesWindowWithinLeadTimeRange()
        {
            var board = new OrderBoard();
            CustomerOrder o = board.AddOffer(Offer());
            Assert.IsTrue(board.Accept(o.Id, new Random(4)));
            Assert.AreEqual(OrderStatus.Accepted, o.Status);
            float lead = o.WindowOpensAt - board.Clock;
            Assert.That(lead, Is.InRange(OrderRules.MinLeadTime, OrderRules.MaxLeadTime));
            Assert.AreEqual(OrderRules.WindowLength, o.WindowClosesAt - o.WindowOpensAt, 1e-4);
        }

        [Test]
        public void AcceptedOrder_OpensThenMissesAndCostsTrust()
        {
            var board = new OrderBoard();
            CustomerOrder o = board.AddOffer(Offer());
            board.Accept(o.Id, new Random(5));
            CustomerOrder opened = null, missed = null;
            board.WindowOpened += x => opened = x;
            board.Missed += x => missed = x;

            board.Tick(o.WindowOpensAt - board.Clock + 0.01f, new Random(5));
            Assert.AreSame(o, opened);
            Assert.AreEqual(OrderStatus.Waiting, o.Status);

            board.Tick(OrderRules.WindowLength, new Random(5));
            Assert.AreSame(o, missed);
            Assert.AreEqual(OrderStatus.Missed, o.Status);
            Assert.AreEqual(OrderRules.StartTrust + OrderRules.TrustForMissing, board.Trust);
        }

        [Test]
        public void Complete_OnlyWhileWaiting_AndRaisesTrust()
        {
            var board = new OrderBoard();
            CustomerOrder o = board.AddOffer(Offer());
            Assert.IsFalse(board.Complete(o.Id, 50), "can't hand over before accepting");
            board.Accept(o.Id, new Random(6));
            Assert.IsFalse(board.Complete(o.Id, 50), "customer hasn't arrived yet");
            board.Tick(OrderRules.MaxLeadTime + 0.1f, new Random(6));
            Assert.IsTrue(board.Complete(o.Id, 50));
            Assert.AreEqual(OrderStatus.Completed, o.Status);
            Assert.AreEqual(50, o.Payout);
            Assert.AreEqual(OrderRules.StartTrust + OrderRules.TrustForCompleting, board.Trust);
            board.Tick(OrderRules.WindowLength + 1f, new Random(6));
            Assert.AreEqual(OrderStatus.Completed, o.Status, "a completed deal can't later be missed");
        }

        [Test]
        public void Decline_RemovesFromInboxWithoutPenalty()
        {
            var board = new OrderBoard();
            CustomerOrder o = board.AddOffer(Offer());
            Assert.IsTrue(board.Decline(o.Id));
            Assert.AreEqual(OrderStatus.Declined, o.Status);
            Assert.AreEqual(OrderRules.StartTrust, board.Trust);
            Assert.IsFalse(board.Accept(o.Id, new Random(7)));
        }

        [Test]
        public void Accept_BlockedByActiveLimitAndBookedSpot()
        {
            var board = new OrderBoard();
            var rng = new Random(8);
            CustomerOrder a = board.AddOffer(Offer("A"));
            CustomerOrder a2 = board.AddOffer(Offer("A"));
            CustomerOrder b = board.AddOffer(Offer("B"));
            CustomerOrder c = board.AddOffer(Offer("C"));

            Assert.IsTrue(board.Accept(a.Id, rng));
            StringAssert.Contains("Spot A", board.AcceptBlocker(a2));
            Assert.IsFalse(board.Accept(a2.Id, rng));
            Assert.IsTrue(board.Accept(b.Id, rng));
            StringAssert.Contains("juggling", board.AcceptBlocker(c));
            Assert.IsFalse(board.Accept(c.Id, rng));
        }

        [Test]
        public void Trust_IsClampedAndShapesPayAndFrequency()
        {
            Assert.AreEqual(0f, OrderRules.MarkupBonus(50), 1e-5);
            Assert.AreEqual(0.15f, OrderRules.MarkupBonus(100), 1e-5);
            Assert.AreEqual(-0.15f, OrderRules.MarkupBonus(-40), 1e-5);
            Assert.Less(OrderRules.IntervalScale(100), OrderRules.IntervalScale(0), "trusted dealers get more messages");
        }

        [TestCase(40, 0.25f, 50)]
        [TestCase(40, 0f, 40)]
        [TestCase(1, -0.9f, 1)]
        public void UnitPayout_AppliesMarkupAndNeverDropsBelowOne(int sale, float markup, int expected)
        {
            Assert.AreEqual(expected, OrderRules.UnitPayout(sale, markup));
        }

        [Test]
        public void History_IsPrunedToTheLastFew()
        {
            var board = new OrderBoard();
            for (int i = 0; i < OrderRules.HistoryLength + 4; i++)
            {
                board.Decline(board.AddOffer(Offer()).Id);
            }
            board.Tick(0.01f, new Random(9));
            Assert.AreEqual(OrderRules.HistoryLength, board.Orders.Count);
        }

        [Test]
        public void CaptureRestore_RoundTripsThroughJson()
        {
            var board = new OrderBoard();
            var rng = new Random(10);
            CustomerOrder o = board.AddOffer(Offer("B"));
            board.Accept(o.Id, rng);
            board.AddOffer(Offer("C"));
            board.Tick(5f, rng);

            var data = new SaveData { Orders = board.Capture() };
            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(data), out SaveData parsed, out string error), error);

            var restored = new OrderBoard();
            restored.Restore(parsed.Orders);
            Assert.AreEqual(board.Clock, restored.Clock, 1e-4);
            Assert.AreEqual(2, restored.Orders.Count);
            CustomerOrder r = restored.Find(o.Id);
            Assert.AreEqual(OrderStatus.Accepted, r.Status);
            Assert.AreEqual("B", r.SpotId);
            Assert.AreEqual(ItemQuality.Standard, r.MinimumQuality);
            Assert.AreEqual(o.WindowOpensAt, r.WindowOpensAt, 1e-4);
            Assert.AreEqual("ORD003", restored.AddOffer(Offer()).Id, "ids continue after a load");
        }

        [Test]
        public void OlderSaves_LoadWithAnEmptyBoard()
        {
            Assert.IsTrue(SaveCodec.TryParse("{\"Version\":2,\"Credits\":5}", out SaveData data, out string error), error);
            Assert.IsNotNull(data.Orders);
            Assert.AreEqual(0, data.Orders.Orders.Count);
            Assert.AreEqual(OrderRules.StartTrust, data.Orders.Trust);
        }
    }
}
