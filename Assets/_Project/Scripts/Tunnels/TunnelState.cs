using System;
using System.Collections.Generic;

namespace VentsAndVending.Tunnels
{
    /// <summary>
    /// Runtime state of the tunnel network (Docs/Design/TunnelNetwork.md §10): which rooms the player has discovered and
    /// the position of every infrastructure switch (breakers, latches, gates). Plain C#, keyed by stable string IDs.
    /// </summary>
    public sealed class TunnelState
    {
        private readonly HashSet<string> _discovered = new HashSet<string>();
        private readonly Dictionary<string, bool> _switches = new Dictionary<string, bool>();

        /// <summary>Raised when a switch actually changes (id, new value).</summary>
        public event Action<string, bool> SwitchChanged;

        public IReadOnlyCollection<string> Discovered => _discovered;
        public IReadOnlyDictionary<string, bool> Switches => _switches;

        /// <summary>Marks a room discovered. Returns true only the first time.</summary>
        public bool Discover(string roomId)
        {
            return !string.IsNullOrEmpty(roomId) && _discovered.Add(roomId);
        }

        public bool IsDiscovered(string roomId) => roomId != null && _discovered.Contains(roomId);

        /// <summary>Switch position, or <paramref name="fallback"/> if it has never been set (e.g. breakers start on).</summary>
        public bool GetSwitch(string id, bool fallback) => id != null && _switches.TryGetValue(id, out bool on) ? on : fallback;

        /// <summary>Sets a switch. Returns true (and raises <see cref="SwitchChanged"/>) if the value changed.</summary>
        public bool SetSwitch(string id, bool on, bool fallback)
        {
            if (string.IsNullOrEmpty(id) || GetSwitch(id, fallback) == on)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    _switches[id] = on; // remember explicitly even when equal to the default
                }
                return false;
            }

            _switches[id] = on;
            SwitchChanged?.Invoke(id, on);
            return true;
        }

        public void Clear()
        {
            _discovered.Clear();
            _switches.Clear();
        }

        public TunnelSave Capture()
        {
            var save = new TunnelSave();
            save.DiscoveredRooms.AddRange(_discovered);
            save.DiscoveredRooms.Sort(StringComparer.Ordinal);
            foreach (KeyValuePair<string, bool> pair in _switches)
            {
                save.Switches.Add(new SwitchSave { Id = pair.Key, On = pair.Value });
            }
            save.Switches.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return save;
        }

        /// <summary>Replaces the state with a save. Unknown or blank IDs are skipped. Does not raise events.</summary>
        public void Restore(TunnelSave save)
        {
            Clear();
            if (save == null)
            {
                return;
            }

            foreach (string room in save.DiscoveredRooms ?? new List<string>())
            {
                Discover(room);
            }
            foreach (SwitchSave s in save.Switches ?? new List<SwitchSave>())
            {
                if (s != null && !string.IsNullOrEmpty(s.Id))
                {
                    _switches[s.Id] = s.On;
                }
            }
        }
    }

    [Serializable]
    public sealed class TunnelSave
    {
        public List<string> DiscoveredRooms = new List<string>();
        public List<SwitchSave> Switches = new List<SwitchSave>();
    }

    [Serializable]
    public sealed class SwitchSave
    {
        public string Id;
        public bool On;
    }
}
