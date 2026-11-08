using System;
using UnityEngine;
using UnityEngine.AI;
using VentsAndVending.Crafting;
using VentsAndVending.Economy;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;

namespace VentsAndVending.Customers
{
    public enum CustomerState
    {
        Arriving,
        Waiting,
        Trading,
        Leaving
    }

    public enum TradeOutcome
    {
        Sold,
        WalkedAway,
        Cancelled
    }

    /// <summary>
    /// A customer's visit (GDD §9.1): arrive → wait → trade → react → leave. Driven by a NavMeshAgent and the
    /// <see cref="CustomerDefinition"/> data.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Customer : MonoBehaviour, IInteractable
    {
        /// <summary>Raised when the player starts talking to a customer.</summary>
        public static event Action<Customer, InteractionController> TradeRequested;

        [SerializeField] private CustomerDefinition _definition;
        [SerializeField] private SpeechBubble _bubble;
        [SerializeField] private CustomerVisual _visual;
        [SerializeField] private float _faceSpeed = 360f;

        private NavMeshAgent _agent;
        private Transform _meetingPoint;
        private Transform _exitPoint;
        private Transform _player;
        private float _patienceLeft;
        private float _leaveDelay;

        public CustomerDefinition Definition => _definition;
        public string DisplayName { get; private set; }
        public ProductDefinition WantedProduct { get; private set; }
        public int WantedQuantity { get; private set; }
        public float PriceTolerance { get; private set; }
        public CustomerState State { get; private set; } = CustomerState.Arriving;
        /// <summary>0–1, how much patience is left while waiting.</summary>
        public float PatienceFraction => _definition != null ? Mathf.Clamp01(_patienceLeft / _definition.Patience) : 0f;

        public event Action<Customer> Departed;

        private void Awake() => _agent = GetComponent<NavMeshAgent>();

        /// <summary>Starts the visit. Call right after spawning.</summary>
        public void Begin(CustomerDefinition definition, Transform meetingPoint, Transform exitPoint)
        {
            _definition = definition;
            _meetingPoint = meetingPoint;
            _exitPoint = exitPoint;

            DisplayName = definition.PickName();
            WantedProduct = definition.Wants.Count > 0 ? definition.Wants[UnityEngine.Random.Range(0, definition.Wants.Count)] : null;
            WantedQuantity = UnityEngine.Random.Range(definition.QuantityRange.x, definition.QuantityRange.y + 1);
            PriceTolerance = UnityEngine.Random.Range(definition.PriceToleranceRange.x, definition.PriceToleranceRange.y);

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }

            State = CustomerState.Arriving;
            _agent.isStopped = false;
            _agent.SetDestination(_meetingPoint.position);
            SetBubble(string.Empty);
        }

        // ------------------------------------------------------------------ IInteractable

        public string GetPrompt(InteractionController interactor)
        {
            return State == CustomerState.Waiting && WantedProduct != null
                ? $"Talk to {DisplayName} <color=#9AA3AD>({_definition.SpeciesName} · wants {WantedProduct.DisplayName} x{WantedQuantity})</color>"
                : null;
        }

        public void Interact(InteractionController interactor)
        {
            if (State != CustomerState.Waiting)
            {
                return;
            }

            State = CustomerState.Trading;
            _player = interactor.transform;
            _agent.isStopped = true;
            Say(Format(CustomerDefinition.Pick(_definition.Lines.Greeting)));
            TradeRequested?.Invoke(this, interactor);
        }

        // ------------------------------------------------------------------ Trading

        /// <summary>Fair unit price for a quality. Customers who like unstable goods pay standard price for Poor.</summary>
        public int FairUnitPrice(ItemQuality quality)
        {
            if (WantedProduct == null)
            {
                return 0;
            }

            return _definition.LikesPoorQuality && quality == ItemQuality.Poor
                ? WantedProduct.StandardSalePrice
                : WantedProduct.SalePriceFor(quality);
        }

        public SaleNegotiation CreateNegotiation() => new SaleNegotiation(WantedQuantity, PriceTolerance,
            _definition.MinimumQuality, _definition.LikesPoorQuality, _definition.HaggleAttempts);

        public string Line(string[] pool, int price = 0, int counter = 0) => Format(CustomerDefinition.Pick(pool), price, counter);

        public void EndTrade(TradeOutcome outcome, string line)
        {
            if (State != CustomerState.Trading)
            {
                return;
            }

            switch (outcome)
            {
                case TradeOutcome.Sold:
                    Say(line);
                    if (_visual != null) { _visual.PlayHappy(); }
                    _leaveDelay = 1.6f;
                    State = CustomerState.Leaving;
                    break;
                case TradeOutcome.WalkedAway:
                    Leave(line);
                    break;
                default:
                    State = CustomerState.Waiting;
                    _agent.isStopped = false;
                    break;
            }
        }

        private void Leave(string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                Say(line);
            }

            SetBubble(string.Empty);
            State = CustomerState.Leaving;
            _agent.isStopped = false;
            _agent.SetDestination(_exitPoint.position);
        }

        // ------------------------------------------------------------------ Update

        private void Update()
        {
            switch (State)
            {
                case CustomerState.Arriving:
                    if (Reached())
                    {
                        State = CustomerState.Waiting;
                        _patienceLeft = _definition.Patience;
                        SetBubble(WantedProduct != null ? $"{WantedProduct.DisplayName} x{WantedQuantity}?" : "…");
                    }
                    break;

                case CustomerState.Waiting:
                    _patienceLeft -= Time.deltaTime;
                    FacePlayerIfNear(5f);
                    if (_patienceLeft <= 0f)
                    {
                        Leave(Line(_definition.Lines.Impatient));
                    }
                    break;

                case CustomerState.Trading:
                    FacePlayerIfNear(10f);
                    break;

                case CustomerState.Leaving:
                    if (_leaveDelay > 0f)
                    {
                        _leaveDelay -= Time.deltaTime;
                        if (_leaveDelay <= 0f)
                        {
                            Leave(null);
                        }
                    }
                    else if (Reached())
                    {
                        Departed?.Invoke(this);
                        Destroy(gameObject);
                    }
                    break;
            }
        }

        private bool Reached() =>
            !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f;

        private void FacePlayerIfNear(float range)
        {
            if (_player == null)
            {
                var found = FindAnyObjectByType<InteractionController>();
                _player = found != null ? found.transform : null;
                if (_player == null)
                {
                    return;
                }
            }

            Vector3 to = _player.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > range * range || to.sqrMagnitude < 0.01f)
            {
                return;
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), _faceSpeed * Time.deltaTime);
        }

        private void SetBubble(string line)
        {
            if (_bubble != null)
            {
                _bubble.SetPersistent(line);
            }
        }

        private void Say(string line)
        {
            if (_bubble != null && !string.IsNullOrEmpty(line))
            {
                _bubble.Say(line);
            }
        }

        private string Format(string template, int price = 0, int counter = 0)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            return template
                .Replace("{name}", DisplayName)
                .Replace("{product}", WantedProduct != null ? WantedProduct.DisplayName : "stuff")
                .Replace("{qty}", WantedQuantity.ToString())
                .Replace("{price}", price.ToString())
                .Replace("{counter}", counter.ToString());
        }
    }
}
