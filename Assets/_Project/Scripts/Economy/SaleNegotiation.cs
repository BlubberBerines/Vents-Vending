using System;
using VentsAndVending.Inventory;

namespace VentsAndVending.Economy
{
    public enum OfferOutcome
    {
        Accepted,
        TooExpensive,
        QualityRejected,
        WalkedAway,
        Closed
    }

    public readonly struct OfferResult
    {
        public readonly OfferOutcome Outcome;
        public readonly int UnitPrice;
        public readonly int Total;
        /// <summary>What the customer would pay per unit instead (TooExpensive only).</summary>
        public readonly int CounterOffer;
        public readonly int AttemptsLeft;

        public OfferResult(OfferOutcome outcome, int unitPrice, int total, int counterOffer, int attemptsLeft)
        {
            Outcome = outcome;
            UnitPrice = unitPrice;
            Total = total;
            CounterOffer = counterOffer;
            AttemptsLeft = attemptsLeft;
        }
    }

    /// <summary>
    /// One haggling session with a customer (GDD §10.2, §10.4). The customer accepts any price up to
    /// fair × (1 + tolerance); above that they counter-offer, and walk away when out of patience.
    /// Deliberately simple and readable (GDD: "the economy should be understandable").
    /// </summary>
    public sealed class SaleNegotiation
    {
        private readonly float _tolerance;
        private readonly ItemQuality _minimumQuality;
        private readonly bool _likesPoorQuality;

        public SaleNegotiation(int quantity, float tolerance, ItemQuality minimumQuality, bool likesPoorQuality, int maxAttempts)
        {
            Quantity = Math.Max(1, quantity);
            _tolerance = Math.Max(0f, tolerance);
            _minimumQuality = minimumQuality;
            _likesPoorQuality = likesPoorQuality;
            AttemptsLeft = Math.Max(1, maxAttempts);
        }

        public int Quantity { get; }
        public int AttemptsLeft { get; private set; }
        public bool IsClosed { get; private set; }

        public static int MaxAcceptable(int fairUnitPrice, float tolerance) =>
            (int)Math.Round(fairUnitPrice * (1f + Math.Max(0f, tolerance)), MidpointRounding.AwayFromZero);

        public int MaxAcceptableFor(int fairUnitPrice) => MaxAcceptable(fairUnitPrice, _tolerance);

        /// <summary>GDD §7.5: some customers specifically prefer unstable (Poor) products.</summary>
        public bool AcceptsQuality(ItemQuality quality) =>
            quality >= _minimumQuality || (_likesPoorQuality && quality == ItemQuality.Poor);

        public OfferResult Offer(int unitPrice, ItemQuality quality, int fairUnitPrice)
        {
            if (IsClosed)
            {
                return new OfferResult(OfferOutcome.Closed, unitPrice, 0, 0, AttemptsLeft);
            }

            if (!AcceptsQuality(quality))
            {
                return new OfferResult(OfferOutcome.QualityRejected, unitPrice, 0, 0, AttemptsLeft);
            }

            int max = MaxAcceptableFor(fairUnitPrice);
            if (unitPrice <= max)
            {
                IsClosed = true;
                return new OfferResult(OfferOutcome.Accepted, unitPrice, unitPrice * Quantity, 0, AttemptsLeft);
            }

            AttemptsLeft--;
            if (AttemptsLeft <= 0)
            {
                IsClosed = true;
                return new OfferResult(OfferOutcome.WalkedAway, unitPrice, 0, 0, 0);
            }

            return new OfferResult(OfferOutcome.TooExpensive, unitPrice, 0, max, AttemptsLeft);
        }
    }
}
