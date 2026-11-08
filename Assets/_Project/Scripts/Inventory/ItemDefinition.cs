using UnityEngine;

namespace VentsAndVending.Inventory
{
    /// <summary>
    /// Static definition of an item (GDD §7.2, §18.4). Runtime quantities live in <see cref="InventoryModel"/>,
    /// and save data refers to items by <see cref="Id"/>, never by asset reference.
    /// </summary>
    [CreateAssetMenu(menuName = "Vents & Vending/Item Definition", fileName = "Item_New")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Stable ID used by save files. Never change it after release.")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 4)] private string _description;
        [SerializeField] private ItemCategory _category;
        [SerializeField] private Sprite _icon;
        [Tooltip("Colour identity used by placeholder UI and greybox models (GDD §15.4).")]
        [SerializeField] private Color _uiColor = Color.white;
        [SerializeField, Min(1)] private int _maxStack = 10;
        [Tooltip("Base purchase value in Cosmic Credits.")]
        [SerializeField, Min(0)] private int _baseValue;
        [Tooltip("Prefab spawned when the item is dropped into the world. Should carry a PickupItem.")]
        [SerializeField] private GameObject _worldPrefab;

        public string Id => _id;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string Description => _description;
        public ItemCategory Category => _category;
        public Sprite Icon => _icon;
        public Color UiColor => _uiColor;
        public int MaxStack => Mathf.Max(1, _maxStack);
        public int BaseValue => _baseValue;
        public GameObject WorldPrefab => _worldPrefab;

        internal void Configure(string id, string displayName, string description, ItemCategory category,
            int maxStack, int baseValue, Color uiColor)
        {
            _id = id;
            _displayName = displayName;
            _description = description;
            _category = category;
            _maxStack = Mathf.Max(1, maxStack);
            _baseValue = Mathf.Max(0, baseValue);
            _uiColor = uiColor;
        }

        internal void SetWorldPrefab(GameObject prefab) => _worldPrefab = prefab;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                _id = name.Replace("Item_", string.Empty).ToLowerInvariant().Replace(' ', '_');
            }
        }
    }
}
