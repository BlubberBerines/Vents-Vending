using System;
using System.Collections.Generic;

namespace VentsAndVending.Player.Appearance
{
    /// <summary>
    /// What the player owns and wears, plus skin colour. Plain C#, stable string IDs, saved with the game. Only owned
    /// items can be worn; one item per slot; any slot can be empty.
    /// </summary>
    public sealed class AppearanceState
    {
        public const int SkinTones = 8;

        private readonly Dictionary<OutfitSlot, string> _equipped = new Dictionary<OutfitSlot, string>();
        private readonly HashSet<string> _owned = new HashSet<string>();

        public event Action Changed;

        public int SkinIndex { get; private set; }
        public IReadOnlyCollection<string> Owned => _owned;

        public bool IsOwned(string id) => !string.IsNullOrEmpty(id) && _owned.Contains(id);

        public string Equipped(OutfitSlot slot) => _equipped.TryGetValue(slot, out string id) ? id : null;

        /// <summary>Adds to the wardrobe. Returns true if it's new.</summary>
        public bool Own(string id)
        {
            if (string.IsNullOrEmpty(id) || !_owned.Add(id))
            {
                return false;
            }
            Changed?.Invoke();
            return true;
        }

        /// <summary>Wears an owned item in its slot (replacing what was there). Returns false if not owned.</summary>
        public bool Equip(OutfitSlot slot, string id)
        {
            if (!IsOwned(id))
            {
                return false;
            }
            if (Equipped(slot) == id)
            {
                return true;
            }
            _equipped[slot] = id;
            Changed?.Invoke();
            return true;
        }

        public void Unequip(OutfitSlot slot)
        {
            if (_equipped.Remove(slot))
            {
                Changed?.Invoke();
            }
        }

        public void SetSkin(int index)
        {
            int clamped = ((index % SkinTones) + SkinTones) % SkinTones;
            if (clamped != SkinIndex)
            {
                SkinIndex = clamped;
                Changed?.Invoke();
            }
        }

        public AppearanceSave Capture()
        {
            var save = new AppearanceSave { SkinIndex = SkinIndex };
            save.Owned.AddRange(_owned);
            save.Owned.Sort(StringComparer.Ordinal);
            foreach (var pair in _equipped)
            {
                save.Equipped.Add(new EquippedSave { Slot = pair.Key.ToString(), Id = pair.Value });
            }
            save.Equipped.Sort((a, b) => string.CompareOrdinal(a.Slot, b.Slot));
            return save;
        }

        /// <summary>Replaces the state. Unknown slots and items that aren't owned are dropped.</summary>
        public void Restore(AppearanceSave save)
        {
            _owned.Clear();
            _equipped.Clear();
            SkinIndex = 0;
            if (save != null)
            {
                SkinIndex = ((save.SkinIndex % SkinTones) + SkinTones) % SkinTones;
                foreach (string id in save.Owned ?? new List<string>())
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        _owned.Add(id);
                    }
                }
                foreach (EquippedSave e in save.Equipped ?? new List<EquippedSave>())
                {
                    if (e != null && Enum.TryParse(e.Slot, out OutfitSlot slot) && _owned.Contains(e.Id))
                    {
                        _equipped[slot] = e.Id;
                    }
                }
            }
            Changed?.Invoke();
        }

        /// <summary>True if the save has never been set up (fresh game or a save from before appearances existed).</summary>
        public static bool IsBlank(AppearanceSave save) => save == null || ((save.Owned == null || save.Owned.Count == 0) && (save.Equipped == null || save.Equipped.Count == 0));
    }

    [Serializable]
    public sealed class AppearanceSave
    {
        public int SkinIndex;
        public List<string> Owned = new List<string>();
        public List<EquippedSave> Equipped = new List<EquippedSave>();
    }

    [Serializable]
    public sealed class EquippedSave
    {
        public string Slot;
        public string Id;
    }
}
