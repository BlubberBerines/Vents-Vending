using System;

namespace VentsAndVending.Orders
{
    /// <summary>
    /// Tuning for VentNet orders. Real-time seconds for now (there is no station clock yet); keep the loop brisk so
    /// "accept → craft → walk → hand over" fits in a few minutes (GDD §11 checks: not a walking simulator).
    /// </summary>
    public static class OrderRules
    {
        public const float FirstOfferDelay = 12f;
        public const float MinOfferInterval = 35f;
        public const float MaxOfferInterval = 70f;
        public const int MaxPendingOffers = 3;
        public const int MaxActiveOrders = 2;
        /// <summary>How long a message waits for an answer.</summary>
        public const float OfferLifetime = 120f;
        /// <summary>Time from accepting to the customer turning up.</summary>
        public const float MinLeadTime = 60f;
        public const float MaxLeadTime = 120f;
        /// <summary>How long the customer waits at the meet spot.</summary>
        public const float WindowLength = 150f;
        /// <summary>Finished orders kept for the inbox history.</summary>
        public const int HistoryLength = 5;

        public const int StartTrust = 50;
        public const int TrustForCompleting = 6;
        public const int TrustForMissing = -12;

        /// <summary>Trust 0…100 moves payment by ±15% and message frequency by ±30%.</summary>
        public static float MarkupBonus(int trust) => (Clamp(trust) - 50) / 50f * 0.15f;

        public static float IntervalScale(int trust) => 1.3f - Clamp(trust) / 100f * 0.6f;

        /// <summary>What the customer pays per unit for the quality actually delivered.</summary>
        public static int UnitPayout(int salePriceForQuality, float markup) =>
            Math.Max(1, (int)Math.Round(salePriceForQuality * (1f + markup), MidpointRounding.AwayFromZero));

        public static int Clamp(int trust) => Math.Max(0, Math.Min(100, trust));

        /// <summary>"1:05" style countdown.</summary>
        public static string Clock(float seconds)
        {
            int s = Math.Max(0, (int)Math.Ceiling(seconds));
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
