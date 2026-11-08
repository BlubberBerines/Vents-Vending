namespace VentsAndVending.Inventory
{
    /// <summary>An amount of one item in a slot. <c>default</c> is an empty slot.</summary>
    public readonly struct ItemStack
    {
        public readonly ItemDefinition Item;
        public readonly int Count;

        public ItemStack(ItemDefinition item, int count)
        {
            Item = item;
            Count = count;
        }

        public bool IsEmpty => Item == null || Count <= 0;

        public override string ToString() => IsEmpty ? "(empty)" : $"{Item.DisplayName} x{Count}";
    }
}
