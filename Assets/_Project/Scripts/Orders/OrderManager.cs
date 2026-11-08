using System.Collections.Generic;
using UnityEngine;
using VentsAndVending.Crafting;
using VentsAndVending.Customers;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;

namespace VentsAndVending.Orders
{
    /// <summary>
    /// Runs VentNet in the scene: writes customer messages (from <see cref="CustomerDefinition"/> data), spawns the
    /// customer at the plaza door when a deal's window opens, lights up the meet spot, and settles hand-offs and misses.
    /// The rules live in <see cref="OrderBoard"/>; this is the glue to the world.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OrderManager : MonoBehaviour
    {
        [SerializeField] private List<CustomerDefinition> _customers = new List<CustomerDefinition>();
        [SerializeField] private List<MeetSpot> _spots = new List<MeetSpot>();
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _exitPoint;
        [SerializeField] private AudioClip _messageSound;

        private readonly System.Random _rng = new System.Random();
        private readonly Dictionary<string, Customer> _visitors = new Dictionary<string, Customer>();
        private AudioSource _pager;

        public static OrderManager Instance { get; private set; }

        public OrderBoard Board { get; } = new OrderBoard();
        public IReadOnlyList<MeetSpot> Spots => _spots;

        internal void Configure(IEnumerable<CustomerDefinition> customers, IEnumerable<MeetSpot> spots, Transform spawn, Transform exit, AudioClip messageSound)
        {
            _customers = new List<CustomerDefinition>(customers);
            _spots = new List<MeetSpot>(spots);
            _spawnPoint = spawn;
            _exitPoint = exit;
            _messageSound = messageSound;
        }

        private void Awake()
        {
            Instance = this;
            Board.OfferFactory = MakeOffer;
            Board.OfferReceived += OnOfferReceived;
            Board.OfferExpired += o => Notify($"<color=#9AA3AD>Message from {o.CustomerName} expired.</color>");
            Board.WindowOpened += OnWindowOpened;
            Board.Missed += OnMissed;

            _pager = gameObject.AddComponent<AudioSource>();
            _pager.spatialBlend = 0f; // the burner pager is in your pocket
            _pager.playOnAwake = false;
            _pager.volume = 0.45f;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            Board.Tick(Time.deltaTime, _rng);
            UpdateSpotMarks();
        }

        // ------------------------------------------------------------------ Lookups

        public MeetSpot FindSpot(string id) => _spots.Find(s => s != null && s.SpotId == id);

        public CustomerDefinition FindCustomer(string id) => _customers.Find(c => c != null && c.Id == id);

        public ProductDefinition FindProduct(string id)
        {
            foreach (CustomerDefinition c in _customers)
            {
                if (c == null)
                {
                    continue;
                }
                foreach (ProductDefinition p in c.Wants)
                {
                    if (p != null && p.Id == id)
                    {
                        return p;
                    }
                }
            }
            return null;
        }

        /// <summary>Per-unit pay for a delivered quality (better than asked pays more).</summary>
        public int UnitPayout(CustomerOrder order, ItemQuality delivered)
        {
            ProductDefinition product = FindProduct(order.ProductId);
            return product != null ? OrderRules.UnitPayout(product.SalePriceFor(delivered), order.Markup) : 0;
        }

        // ------------------------------------------------------------------ Player actions (VentNet UI, sale panel)

        public bool Accept(CustomerOrder order)
        {
            if (!Board.Accept(order.Id, _rng))
            {
                return false;
            }
            MeetSpot spot = FindSpot(order.SpotId);
            Notify($"Deal on: meet <color=#19E5FF>{order.CustomerName}</color> at {(spot != null ? spot.Label : order.SpotId)} in {OrderRules.Clock(order.WindowOpensAt - Board.Clock)}");
            return true;
        }

        public void Decline(CustomerOrder order) => Board.Decline(order.Id);

        /// <summary>Called by the sale panel after the goods and credits have changed hands.</summary>
        public void CompleteHandoff(CustomerOrder order, int payout)
        {
            if (Board.Complete(order.Id, payout))
            {
                _visitors.Remove(order.Id);
            }
        }

        // ------------------------------------------------------------------ Board events

        private CustomerOrder MakeOffer(OrderBoard board)
        {
            var candidates = _customers.FindAll(c => c != null && c.Prefab != null && c.Wants.Count > 0);
            var freeSpots = _spots.FindAll(s => s != null && !board.IsSpotBusy(s.SpotId));
            if (candidates.Count == 0 || freeSpots.Count == 0)
            {
                return null;
            }

            CustomerDefinition who = candidates[_rng.Next(candidates.Count)];
            ProductDefinition what = who.Wants[_rng.Next(who.Wants.Count)];
            MeetSpot where = freeSpots[_rng.Next(freeSpots.Count)];
            float tolerance = Mathf.Lerp(who.PriceToleranceRange.x, who.PriceToleranceRange.y, (float)_rng.NextDouble());
            // Riskier spots pay a little danger money
            float danger = where.Risk == MeetRisk.High ? 0.1f : where.Risk == MeetRisk.Medium ? 0.05f : 0f;

            return new CustomerOrder
            {
                CustomerId = who.Id,
                CustomerName = who.PickName(),
                ProductId = what.Id,
                Quantity = _rng.Next(who.QuantityRange.x, who.QuantityRange.y + 1),
                MinimumQuality = who.LikesPoorQuality ? ItemQuality.Poor : who.MinimumQuality,
                Markup = Mathf.Max(0f, tolerance * 0.8f + danger + OrderRules.MarkupBonus(board.Trust)),
                SpotId = where.SpotId,
            };
        }

        private void OnOfferReceived(CustomerOrder order)
        {
            if (_messageSound != null)
            {
                _pager.PlayOneShot(_messageSound);
            }
            Notify($"<color=#FFD24D>New VentNet message</color> from {order.CustomerName} <color=#9AA3AD>(check the PC in the den)</color>");
        }

        private void OnWindowOpened(CustomerOrder order)
        {
            SpawnVisitor(order);
            MeetSpot spot = FindSpot(order.SpotId);
            Notify($"{order.CustomerName} is heading to {(spot != null ? spot.Label : order.SpotId)} <color=#9AA3AD>(waits {OrderRules.Clock(order.WindowClosesAt - Board.Clock)})</color>");
        }

        private void OnMissed(CustomerOrder order)
        {
            if (_visitors.TryGetValue(order.Id, out Customer c) && c != null)
            {
                c.GiveUp();
            }
            _visitors.Remove(order.Id);
            Notify($"<color=#FF8060>{order.CustomerName} gave up waiting.</color> <color=#9AA3AD>Trust {OrderRules.TrustForMissing}</color>");
        }

        private void SpawnVisitor(CustomerOrder order)
        {
            CustomerDefinition def = FindCustomer(order.CustomerId);
            ProductDefinition product = FindProduct(order.ProductId);
            MeetSpot spot = FindSpot(order.SpotId);
            if (def == null || def.Prefab == null || product == null || spot == null || _spawnPoint == null)
            {
                Debug.LogWarning($"[OrderManager] Can't spawn the customer for {order.Id} (missing data).");
                return;
            }

            GameObject go = Instantiate(def.Prefab, _spawnPoint.position, _spawnPoint.rotation);
            var customer = go.GetComponent<Customer>();
            customer.BeginOrder(def, order, product, spot.transform, _exitPoint != null ? _exitPoint : _spawnPoint);
            _visitors[order.Id] = customer;
        }

        private void UpdateSpotMarks()
        {
            foreach (MeetSpot spot in _spots)
            {
                if (spot == null)
                {
                    continue;
                }
                var mark = MeetSpot.Mark.Idle;
                foreach (CustomerOrder o in Board.Orders)
                {
                    if (o.SpotId != spot.SpotId)
                    {
                        continue;
                    }
                    if (o.Status == OrderStatus.Waiting)
                    {
                        mark = MeetSpot.Mark.CustomerWaiting;
                    }
                    else if (o.Status == OrderStatus.Accepted && mark == MeetSpot.Mark.Idle)
                    {
                        mark = MeetSpot.Mark.Booked;
                    }
                }
                spot.SetMark(mark);
            }
        }

        // ------------------------------------------------------------------ Save

        public OrderBoardSave Capture() => Board.Capture();

        /// <summary>Restores the board and brings back anyone who should be standing at a spot right now.</summary>
        public void Restore(OrderBoardSave save)
        {
            foreach (Customer c in _visitors.Values)
            {
                if (c != null)
                {
                    Destroy(c.gameObject);
                }
            }
            _visitors.Clear();
            Board.Restore(save);
            foreach (CustomerOrder o in Board.Orders)
            {
                if (o.Status == OrderStatus.Waiting)
                {
                    SpawnVisitor(o);
                }
            }
        }

        private static void Notify(string message)
        {
            var interactor = FindAnyObjectByType<InteractionController>();
            if (interactor != null)
            {
                interactor.Notify(message);
            }
        }
    }
}
