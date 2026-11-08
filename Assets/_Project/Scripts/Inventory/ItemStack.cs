namespace VentsAndVending.Inventory
{
    /// <summary>An amount of one item at one quality in a slot. <c>default</c> is an empty slot.</summary>
    public readonly struct ItemStack
    {
        public readonly ItemDefinition Item;
        public readonly int Count;
        public readonly ItemQuality Quality;

        public ItemStack(ItemDefinition item, int count, ItemQuality quality = ItemQuality.Standard)
        {
            Item = item;
            Count = count;
            Quality = quality;
        }

        public bool IsEmpty => Item == null || Count <= 0;

        public bool CanStackWith(ItemDefinition item, ItemQuality quality) => !IsEmpty && Item == item && Quality == quality;

        public override string ToString()
        {
            if (IsEmpty)
            {
                return "(empty)";
            }

            return Quality == ItemQuality.Standard ? $"{Item.DisplayName} x{Count}" : $"{Item.DisplayName} ({Quality}) x{Count}";
        }
    }
}
