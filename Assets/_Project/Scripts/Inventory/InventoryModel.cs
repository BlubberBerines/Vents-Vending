using System;
using System.Collections.Generic;

namespace VentsAndVending.Inventory
{
    /// <summary>
    /// Slot-based inventory with stacking (GDD §17.3, §18.3). Plain C# so it is unit-testable and reusable by the
    /// player, storage containers, machines and save data.
    /// </summary>
    public sealed class InventoryModel
    {
        private readonly ItemStack[] _slots;

        public InventoryModel(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");
            }

            _slots = new ItemStack[capacity];
        }

        /// <summary>Raised after any change to the contents.</summary>
        public event Action Changed;

        public int Capacity => _slots.Length;
        public IReadOnlyList<ItemStack> Slots => _slots;

        public int UsedSlots
        {
            get
            {
                int used = 0;
                foreach (ItemStack s in _slots)
                {
                    if (!s.IsEmpty)
                    {
                        used++;
                    }
                }
                return used;
            }
        }

        public bool IsEmpty => UsedSlots == 0;

        public int Count(ItemDefinition item)
        {
            int total = 0;
            foreach (ItemStack s in _slots)
            {
                if (!s.IsEmpty && s.Item == item)
                {
                    total += s.Count;
                }
            }
            return total;
        }

        /// <summary>How many more of <paramref name="item"/> would fit.</summary>
        public int SpaceFor(ItemDefinition item)
        {
            if (item == null)
            {
                return 0;
            }

            int space = 0;
            foreach (ItemStack s in _slots)
            {
                if (s.IsEmpty)
                {
                    space += item.MaxStack;
                }
                else if (s.Item == item)
                {
                    space += item.MaxStack - s.Count;
                }
            }
            return space;
        }

        public bool CanAdd(ItemDefinition item, int amount) => SpaceFor(item) >= amount;

        /// <summary>Adds as much as fits, topping up existing stacks first. Returns the amount that did NOT fit.</summary>
        public int Add(ItemDefinition item, int amount)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (amount <= 0)
            {
                return 0;
            }

            int remaining = amount;
            int max = item.MaxStack;

            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                ItemStack s = _slots[i];
                if (!s.IsEmpty && s.Item == item && s.Count < max)
                {
                    int add = Math.Min(max - s.Count, remaining);
                    _slots[i] = new ItemStack(item, s.Count + add);
                    remaining -= add;
                }
            }

            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    int add = Math.Min(max, remaining);
                    _slots[i] = new ItemStack(item, add);
                    remaining -= add;
                }
            }

            if (remaining != amount)
            {
                Changed?.Invoke();
            }

            return remaining;
        }

        /// <summary>Removes up to <paramref name="amount"/>, from the last stacks first. Returns the amount removed.</summary>
        public int Remove(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0)
            {
                return 0;
            }

            int remaining = amount;
            for (int i = _slots.Length - 1; i >= 0 && remaining > 0; i--)
            {
                ItemStack s = _slots[i];
                if (s.IsEmpty || s.Item != item)
                {
                    continue;
                }

                int take = Math.Min(s.Count, remaining);
                _slots[i] = s.Count - take > 0 ? new ItemStack(item, s.Count - take) : default;
                remaining -= take;
            }

            int removed = amount - remaining;
            if (removed > 0)
            {
                Changed?.Invoke();
            }

            return removed;
        }

        /// <summary>Empties a slot and returns what was in it.</summary>
        public ItemStack TakeSlot(int index)
        {
            ItemStack s = _slots[index];
            if (s.IsEmpty)
            {
                return default;
            }

            _slots[index] = default;
            Changed?.Invoke();
            return s;
        }

        /// <summary>Moves as much of a slot as fits into <paramref name="target"/>. Returns the amount moved.</summary>
        public int TransferSlotTo(int index, InventoryModel target)
        {
            if (target == null || ReferenceEquals(target, this))
            {
                return 0;
            }

            ItemStack s = _slots[index];
            if (s.IsEmpty)
            {
                return 0;
            }

            int notMoved = target.Add(s.Item, s.Count);
            int moved = s.Count - notMoved;
            if (moved > 0)
            {
                _slots[index] = notMoved > 0 ? new ItemStack(s.Item, notMoved) : default;
                Changed?.Invoke();
            }

            return moved;
        }

        public void Clear()
        {
            Array.Clear(_slots, 0, _slots.Length);
            Changed?.Invoke();
        }
    }
}
