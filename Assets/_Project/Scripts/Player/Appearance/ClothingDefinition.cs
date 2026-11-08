using UnityEngine;

namespace VentsAndVending.Player.Appearance
{
    public enum OutfitSlot
    {
        Head,
        Face,
        Torso,
        Hands,
        Legs,
        Feet,
        Back
    }

    /// <summary>
    /// A wearable item (player personalisation; buying and unlocking comes later). <see cref="StyleId"/> names the
    /// prebuilt variant on the player body that this item switches on; colours tint it. Stable <see cref="Id"/> for saves.
    /// </summary>
    [CreateAssetMenu(menuName = "Vents & Vending/Clothing", fileName = "Clothing_")]
    public sealed class ClothingDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private OutfitSlot _slot;
        [SerializeField] private string _styleId;
        [SerializeField] private Color _primary = Color.white;
        [SerializeField] private Color _secondary = Color.gray;
        [SerializeField, Min(0)] private int _price;
        [SerializeField] private bool _ownedFromStart;
        [Tooltip("Hides the player's own sleeves (short-sleeve items) / similar hints for the body.")]
        [SerializeField] private bool _bareArms;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public OutfitSlot Slot => _slot;
        public string StyleId => _styleId;
        public Color Primary => _primary;
        public Color Secondary => _secondary;
        public int Price => _price;
        public bool OwnedFromStart => _ownedFromStart;
        public bool BareArms => _bareArms;

        internal void Configure(string id, string displayName, string description, OutfitSlot slot, string styleId,
            Color primary, Color secondary, int price, bool ownedFromStart, bool bareArms = false)
        {
            _id = id;
            _displayName = displayName;
            _description = description;
            _slot = slot;
            _styleId = styleId;
            _primary = primary;
            _secondary = secondary;
            _price = price;
            _ownedFromStart = ownedFromStart;
            _bareArms = bareArms;
        }
    }
}
