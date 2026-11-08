using System;
using System.Collections.Generic;

namespace VentsAndVending.Orders
{
    /// <summary>
    /// The VentNet inbox and deal book (GDD §10.3). Pure logic on its own clock so it can be tested and saved:
    /// messages arrive on a timer, expire if ignored, accepted deals open a meeting window after a lead time, and a
    /// window that closes without a hand-off is a miss. Trust rises with completed deals and falls with misses.
    /// </summary>
    public sealed class OrderBoard
    {
        private readonly List<CustomerOrder> _orders = new List<CustomerOrder>();
        private float _nextOfferAt = OrderRules.FirstOfferDelay;
        private int _nextId = 1;

        /// <summary>Builds a new offer (customer, product, quantity, quality, markup, spot). Return null to skip.</summary>
        public Func<OrderBoard, CustomerOrder> OfferFactory;

        public event Action<CustomerOrder> OfferReceived;
        public event Action<CustomerOrder> OfferExpired;
        public event Action<CustomerOrder> WindowOpened;
        public event Action<CustomerOrder> Missed;
        public event Action<CustomerOrder> Completed;
        /// <summary>Anything in the list changed (for UI refresh).</summary>
        public event Action Changed;

        public float Clock { get; private set; }
        public int Trust { get; private set; } = OrderRules.StartTrust;
        public float NextOfferAt => _nextOfferAt;
        public IReadOnlyList<CustomerOrder> Orders => _orders;

        public int OfferCount => Count(o => o.Status == OrderStatus.Offered);
        public int ActiveCount => Count(o => o.IsActive);

        public CustomerOrder Find(string id) => _orders.Find(o => o.Id == id);

        public bool IsSpotBusy(string spotId) => _orders.Exists(o => o.IsActive && o.SpotId == spotId);

        public void Tick(float dt, Random rng)
        {
            Clock += Math.Max(0f, dt);
            bool changed = false;

            foreach (CustomerOrder o in _orders.ToArray())
            {
                if (o.Status == OrderStatus.Offered && Clock >= o.OfferExpiresAt)
                {
                    o.Status = OrderStatus.Expired;
                    changed = true;
                    OfferExpired?.Invoke(o);
                }
                else if (o.Status == OrderStatus.Accepted && Clock >= o.WindowOpensAt)
                {
                    o.Status = OrderStatus.Waiting;
                    changed = true;
                    WindowOpened?.Invoke(o);
                }
                else if (o.Status == OrderStatus.Waiting && Clock >= o.WindowClosesAt)
                {
                    o.Status = OrderStatus.Missed;
                    Trust = OrderRules.Clamp(Trust + OrderRules.TrustForMissing);
                    changed = true;
                    Missed?.Invoke(o);
                }
            }

            if (Clock >= _nextOfferAt)
            {
                float interval = Lerp(OrderRules.MinOfferInterval, OrderRules.MaxOfferInterval, (float)rng.NextDouble());
                _nextOfferAt = Clock + interval * OrderRules.IntervalScale(Trust);
                if (OfferCount < OrderRules.MaxPendingOffers && OfferFactory != null)
                {
                    CustomerOrder offer = OfferFactory(this);
                    if (offer != null)
                    {
                        AddOffer(offer);
                        changed = true;
                    }
                }
            }

            changed |= PruneHistory();
            if (changed)
            {
                Changed?.Invoke();
            }
        }

        /// <summary>Puts an offer in the inbox (normally via <see cref="OfferFactory"/>; public for tests and debug).</summary>
        public CustomerOrder AddOffer(CustomerOrder offer)
        {
            offer.Id = $"ORD{_nextId++:000}";
            offer.Status = OrderStatus.Offered;
            offer.OfferedAt = Clock;
            offer.OfferExpiresAt = Clock + OrderRules.OfferLifetime;
            _orders.Add(offer);
            OfferReceived?.Invoke(offer);
            return offer;
        }

        /// <summary>Why an offer can't be accepted right now, or null if it can.</summary>
        public string AcceptBlocker(CustomerOrder order)
        {
            if (order == null || order.Status != OrderStatus.Offered)
            {
                return "No longer available";
            }
            if (ActiveCount >= OrderRules.MaxActiveOrders)
            {
                return $"Already juggling {OrderRules.MaxActiveOrders} deals";
            }
            if (IsSpotBusy(order.SpotId))
            {
                return $"Spot {order.SpotId} is already booked";
            }
            return null;
        }

        public bool Accept(string id, Random rng)
        {
            CustomerOrder o = Find(id);
            if (AcceptBlocker(o) != null)
            {
                return false;
            }
            o.Status = OrderStatus.Accepted;
            o.WindowOpensAt = Clock + Lerp(OrderRules.MinLeadTime, OrderRules.MaxLeadTime, (float)rng.NextDouble());
            o.WindowClosesAt = o.WindowOpensAt + OrderRules.WindowLength;
            Changed?.Invoke();
            return true;
        }

        public bool Decline(string id)
        {
            CustomerOrder o = Find(id);
            if (o == null || o.Status != OrderStatus.Offered)
            {
                return false;
            }
            o.Status = OrderStatus.Declined;
            Changed?.Invoke();
            return true;
        }

        /// <summary>The hand-off happened (only while the customer is there).</summary>
        public bool Complete(string id, int payout)
        {
            CustomerOrder o = Find(id);
            if (o == null || o.Status != OrderStatus.Waiting)
            {
                return false;
            }
            o.Status = OrderStatus.Completed;
            o.Payout = Math.Max(0, payout);
            Trust = OrderRules.Clamp(Trust + OrderRules.TrustForCompleting);
            Completed?.Invoke(o);
            Changed?.Invoke();
            return true;
        }

        public OrderBoardSave Capture()
        {
            var save = new OrderBoardSave { Clock = Clock, Trust = Trust, NextOfferAt = _nextOfferAt, NextId = _nextId };
            save.Orders.AddRange(_orders);
            return save;
        }

        public void Restore(OrderBoardSave save)
        {
            _orders.Clear();
            save ??= new OrderBoardSave();
            Clock = Math.Max(0f, save.Clock);
            Trust = OrderRules.Clamp(save.Trust);
            _nextOfferAt = save.NextOfferAt;
            _nextId = Math.Max(1, save.NextId);
            foreach (CustomerOrder o in save.Orders ?? new List<CustomerOrder>())
            {
                if (o != null && !string.IsNullOrEmpty(o.Id))
                {
                    _orders.Add(o);
                }
            }
            Changed?.Invoke();
        }

        private bool PruneHistory()
        {
            int finished = Count(o => o.IsFinished);
            bool removed = false;
            for (int i = 0; i < _orders.Count && finished > OrderRules.HistoryLength; i++)
            {
                if (_orders[i].IsFinished)
                {
                    _orders.RemoveAt(i--);
                    finished--;
                    removed = true;
                }
            }
            return removed;
        }

        private int Count(Predicate<CustomerOrder> match)
        {
            int n = 0;
            foreach (CustomerOrder o in _orders)
            {
                if (match(o))
                {
                    n++;
                }
            }
            return n;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
