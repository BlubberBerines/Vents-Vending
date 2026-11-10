using System;
using System.Collections.Generic;
using VentsAndVending.Inventory;

namespace VentsAndVending.Orders
{
    public enum OrderStatus
    {
        /// <summary>In the VentNet inbox, waiting for accept/decline.</summary>
        Offered,
        /// <summary>Accepted; the meeting window hasn't opened yet.</summary>
        Accepted,
        /// <summary>The window is open: the customer is on their way to / standing at the meet spot.</summary>
        Waiting,
        Completed,
        Declined,
        /// <summary>Never answered.</summary>
        Expired,
        /// <summary>Accepted but the player didn't show up in time.</summary>
        Missed
    }

    /// <summary>
    /// A customer request (GDD §10.3): customer, product, quantity, required quality, deadline (the meeting window),
    /// payment (markup over the product's sale price) and where to meet. Plain data, keyed by stable string IDs.
    /// Times are on the <see cref="OrderBoard"/> clock (seconds).
    /// </summary>
    [Serializable]
    public sealed class CustomerOrder
    {
        public string Id;
        public string CustomerId;
        public string CustomerName;
        public string ProductId;
        public int Quantity = 1;
        public ItemQuality MinimumQuality = ItemQuality.Standard;
        /// <summary>Fraction above the product's sale price the customer pays (0.3 = +30%).</summary>
        public float Markup;
        public string SpotId;
        public bool RoomMeeting;
        public OrderStatus Status = OrderStatus.Offered;
        public float OfferedAt;
        public float OfferExpiresAt;
        public float WindowOpensAt;
        public float WindowClosesAt;
        public int Payout;

        public bool IsActive => Status == OrderStatus.Accepted || Status == OrderStatus.Waiting;
        public bool IsFinished => Status >= OrderStatus.Completed;
    }

    [Serializable]
    public sealed class OrderBoardSave
    {
        public float Clock;
        public int Trust = OrderRules.StartTrust;
        public float NextOfferAt = OrderRules.FirstOfferDelay;
        public int NextId = 1;
        public List<CustomerOrder> Orders = new List<CustomerOrder>();
    }
}
