using NUnit.Framework;
using VentsAndVending.Economy;
using VentsAndVending.Inventory;

namespace VentsAndVending.Tests
{
    public class EconomyTests
    {
        // ------------------------------------------------------------------ Wallet

        [Test]
        public void Wallet_AddAndSpend()
        {
            var w = new Wallet(20);
            w.Add(24);
            Assert.AreEqual(44, w.Balance);
            Assert.IsTrue(w.TrySpend(30));
            Assert.AreEqual(14, w.Balance);
        }

        [Test]
        public void Wallet_CannotOverspend()
        {
            var w = new Wallet(10);
            Assert.IsFalse(w.TrySpend(11));
            Assert.AreEqual(10, w.Balance);
        }

        [Test]
        public void Wallet_ChangedReportsDelta_IgnoresNonPositiveAdds()
        {
            var w = new Wallet(5);
            int lastDelta = 0, events = 0;
            w.Changed += (_, d) => { lastDelta = d; events++; };
            w.Add(0);
            w.Add(-3);
            w.Add(7);
            w.TrySpend(2);
            Assert.AreEqual(2, events);
            Assert.AreEqual(-2, lastDelta);
            Assert.AreEqual(10, w.Balance);
        }

        // ------------------------------------------------------------------ Negotiation

        private static SaleNegotiation Haggle(int quantity = 1, float tolerance = 0.25f, int attempts = 2,
            ItemQuality minQuality = ItemQuality.Standard, bool likesPoor = false) =>
            new SaleNegotiation(quantity, tolerance, minQuality, likesPoor, attempts);

        [Test]
        public void FairPrice_IsAccepted()
        {
            OfferResult r = Haggle(quantity: 2).Offer(24, ItemQuality.Standard, 24);
            Assert.AreEqual(OfferOutcome.Accepted, r.Outcome);
            Assert.AreEqual(48, r.Total);
        }

        [Test]
        public void MarkupWithinTolerance_IsAccepted()
        {
            // 24 × 1.25 = 30
            Assert.AreEqual(OfferOutcome.Accepted, Haggle().Offer(30, ItemQuality.Standard, 24).Outcome);
        }

        [Test]
        public void TooExpensive_CountersWithTheirMax_ThenWalksAway()
        {
            SaleNegotiation n = Haggle(attempts: 2);
            OfferResult first = n.Offer(40, ItemQuality.Standard, 24);
            Assert.AreEqual(OfferOutcome.TooExpensive, first.Outcome);
            Assert.AreEqual(30, first.CounterOffer);
            Assert.AreEqual(1, first.AttemptsLeft);

            OfferResult second = n.Offer(35, ItemQuality.Standard, 24);
            Assert.AreEqual(OfferOutcome.WalkedAway, second.Outcome);
            Assert.IsTrue(n.IsClosed);
            Assert.AreEqual(OfferOutcome.Closed, n.Offer(10, ItemQuality.Standard, 24).Outcome);
        }

        [Test]
        public void AcceptingTheCounterOffer_Works()
        {
            SaleNegotiation n = Haggle();
            int counter = n.Offer(50, ItemQuality.Standard, 24).CounterOffer;
            Assert.AreEqual(OfferOutcome.Accepted, n.Offer(counter, ItemQuality.Standard, 24).Outcome);
        }

        [Test]
        public void BelowMinimumQuality_IsRejected_WithoutCostingAnAttempt()
        {
            SaleNegotiation n = Haggle(attempts: 1);
            OfferResult r = n.Offer(5, ItemQuality.Poor, 14);
            Assert.AreEqual(OfferOutcome.QualityRejected, r.Outcome);
            Assert.AreEqual(1, n.AttemptsLeft);
            Assert.IsFalse(n.IsClosed);
        }

        [Test]
        public void SomeCustomers_PreferPoorQuality()
        {
            SaleNegotiation n = Haggle(minQuality: ItemQuality.Premium, likesPoor: true);
            Assert.IsTrue(n.AcceptsQuality(ItemQuality.Poor));
            Assert.IsFalse(n.AcceptsQuality(ItemQuality.Standard));
            Assert.AreEqual(OfferOutcome.Accepted, n.Offer(24, ItemQuality.Poor, 24).Outcome);
        }

        [TestCase(24, 0.0f, 24)]
        [TestCase(24, 0.1f, 26)]   // 26.4 → 26
        [TestCase(34, 0.25f, 43)]  // 42.5 → 43 (away from zero)
        public void MaxAcceptable_Rounds(int fair, float tolerance, int expected)
        {
            Assert.AreEqual(expected, SaleNegotiation.MaxAcceptable(fair, tolerance));
        }
    }
}
