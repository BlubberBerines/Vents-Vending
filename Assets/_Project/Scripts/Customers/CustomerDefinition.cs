using System;
using System.Collections.Generic;
using UnityEngine;
using VentsAndVending.Crafting;
using VentsAndVending.Inventory;

namespace VentsAndVending.Customers
{
    /// <summary>
    /// Data for a customer archetype (GDD §8.7, §9.2): species, wants, spending, quality expectations, patience and
    /// dialogue. Species differences are data, not separate AI.
    /// </summary>
    [CreateAssetMenu(menuName = "Vents & Vending/Customer Definition", fileName = "Customer_New")]
    public sealed class CustomerDefinition : ScriptableObject
    {
        /// <summary>Dialogue pools. Tokens: {name} {product} {qty} {price} {counter}.</summary>
        [Serializable]
        public struct DialogueLines
        {
            [TextArea] public string[] Greeting;
            [TextArea] public string[] Accept;
            [TextArea] public string[] TooExpensive;
            [TextArea] public string[] QualityRejected;
            [TextArea] public string[] WalkAway;
            [TextArea] public string[] Impatient;
        }

        [SerializeField] private string _id;
        [SerializeField] private string _speciesName;
        [SerializeField] private string[] _names = Array.Empty<string>();
        [Tooltip("Prefab with Customer, NavMeshAgent, collider and visuals.")]
        [SerializeField] private GameObject _prefab;

        [Header("Buying")]
        [SerializeField] private List<ProductDefinition> _wants = new List<ProductDefinition>();
        [SerializeField] private Vector2Int _quantityRange = new Vector2Int(1, 1);
        [Tooltip("Hidden markup they'll accept over the fair price (0.2 = +20%). Rolled per visit.")]
        [SerializeField] private Vector2 _priceToleranceRange = new Vector2(0.1f, 0.3f);
        [SerializeField] private ItemQuality _minimumQuality = ItemQuality.Standard;
        [Tooltip("GDD §7.5: some aliens specifically prefer unstable products and pay full price for Poor.")]
        [SerializeField] private bool _likesPoorQuality;
        [SerializeField, Min(1)] private int _haggleAttempts = 2;
        [Tooltip("Seconds they'll wait at the meeting point before giving up.")]
        [SerializeField, Min(5f)] private float _patience = 45f;

        [Header("Dialogue")]
        [SerializeField] private DialogueLines _lines;

        public string Id => _id;
        public string SpeciesName => _speciesName;
        public GameObject Prefab => _prefab;
        public IReadOnlyList<ProductDefinition> Wants => _wants;
        public Vector2Int QuantityRange => _quantityRange;
        public Vector2 PriceToleranceRange => _priceToleranceRange;
        public ItemQuality MinimumQuality => _minimumQuality;
        public bool LikesPoorQuality => _likesPoorQuality;
        public int HaggleAttempts => _haggleAttempts;
        public float Patience => _patience;
        public DialogueLines Lines => _lines;

        public string PickName() =>
            _names != null && _names.Length > 0 ? _names[UnityEngine.Random.Range(0, _names.Length)] : _speciesName;

        public static string Pick(string[] options) =>
            options != null && options.Length > 0 ? options[UnityEngine.Random.Range(0, options.Length)] : string.Empty;

        internal void Configure(string id, string speciesName, string[] names, GameObject prefab, IEnumerable<ProductDefinition> wants,
            Vector2Int quantityRange, Vector2 toleranceRange, ItemQuality minimumQuality, bool likesPoorQuality,
            int haggleAttempts, float patience, DialogueLines lines)
        {
            _id = id;
            _speciesName = speciesName;
            _names = names;
            _prefab = prefab;
            _wants = new List<ProductDefinition>(wants);
            _quantityRange = quantityRange;
            _priceToleranceRange = toleranceRange;
            _minimumQuality = minimumQuality;
            _likesPoorQuality = likesPoorQuality;
            _haggleAttempts = Mathf.Max(1, haggleAttempts);
            _patience = Mathf.Max(5f, patience);
            _lines = lines;
        }
    }
}
