using System.Collections.Generic;
using UnityEngine;

namespace VentsAndVending.Customers
{
    /// <summary>
    /// Sends customers in through a door to free meeting points at intervals (GDD §9.1 "spawn in the district").
    /// </summary>
    public sealed class CustomerSpawner : MonoBehaviour
    {
        [SerializeField] private List<CustomerDefinition> _definitions = new List<CustomerDefinition>();
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _exitPoint;
        [SerializeField] private List<Transform> _meetingPoints = new List<Transform>();
        [SerializeField] private float _firstDelay = 4f;
        [SerializeField] private Vector2 _intervalRange = new Vector2(8f, 16f);

        private readonly Dictionary<Transform, Customer> _occupants = new Dictionary<Transform, Customer>();
        private float _timer;

        public int ActiveCustomers
        {
            get
            {
                int n = 0;
                foreach (Customer c in _occupants.Values)
                {
                    if (c != null)
                    {
                        n++;
                    }
                }
                return n;
            }
        }

        private void Start() => _timer = _firstDelay;

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f)
            {
                return;
            }

            _timer = Random.Range(_intervalRange.x, _intervalRange.y);
            TrySpawn();
        }

        /// <summary>Spawns one customer if a meeting point is free. Returns it, or null.</summary>
        public Customer TrySpawn()
        {
            Transform point = FreeMeetingPoint();
            if (point == null || _definitions.Count == 0 || _spawnPoint == null || _exitPoint == null)
            {
                return null;
            }

            CustomerDefinition definition = _definitions[Random.Range(0, _definitions.Count)];
            if (definition == null || definition.Prefab == null)
            {
                return null;
            }

            GameObject go = Instantiate(definition.Prefab, _spawnPoint.position, _spawnPoint.rotation);
            var customer = go.GetComponent<Customer>();
            customer.Begin(definition, point, _exitPoint);
            customer.Departed += OnDeparted;
            _occupants[point] = customer;
            return customer;
        }

        private Transform FreeMeetingPoint()
        {
            foreach (Transform point in _meetingPoints)
            {
                if (point != null && (!_occupants.TryGetValue(point, out Customer c) || c == null))
                {
                    return point;
                }
            }
            return null;
        }

        private void OnDeparted(Customer customer)
        {
            customer.Departed -= OnDeparted;
            Transform key = null;
            foreach (var pair in _occupants)
            {
                if (pair.Value == customer)
                {
                    key = pair.Key;
                    break;
                }
            }

            if (key != null)
            {
                _occupants.Remove(key);
            }
        }
    }
}
