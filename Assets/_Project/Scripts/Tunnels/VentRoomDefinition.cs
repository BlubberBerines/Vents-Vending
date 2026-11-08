using System;
using UnityEngine;

namespace VentsAndVending.Tunnels
{
    /// <summary>
    /// Static data for one authored tunnel room (Docs/Design/TunnelNetwork.md §10.3). Geometry lives in the scene; this
    /// holds identity and topology so discovery, the debug overlay and a future map terminal share one graph.
    /// </summary>
    [CreateAssetMenu(menuName = "Vents & Vending/Tunnels/Vent Room", fileName = "Room_")]
    public sealed class VentRoomDefinition : ScriptableObject
    {
        [Serializable]
        public struct Connection
        {
            public string DestinationRoomId;
            [Tooltip("Switch that must be in its 'open' position, empty if always open.")]
            public string GateId;
            [Tooltip("The passage opens when the switch is OFF (e.g. a fan behind its breaker).")]
            public bool GateOpensWhenOff;
            public bool OneWay;
        }

        [SerializeField] private string _roomId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _districtId = "old_maintenance";
        [SerializeField, Range(1, 5)] private int _plane = 3;
        [SerializeField] private Connection[] _connections = Array.Empty<Connection>();

        public string RoomId => _roomId;
        public string DisplayName => _displayName;
        public string DistrictId => _districtId;
        /// <summary>1 = upper service … 5 = legacy/deep.</summary>
        public int Plane => _plane;
        public Connection[] Connections => _connections;

        /// <summary>"04 · FAN JUNCTION" style label used by toasts and signage.</summary>
        public string Label => _roomId != null && _roomId.StartsWith("R")
            ? $"{_roomId.Substring(1)} · {_displayName.ToUpperInvariant()}"
            : _displayName.ToUpperInvariant(); // secrets have no number on the old station plans

        internal void Configure(string roomId, string displayName, string districtId, int plane, Connection[] connections)
        {
            _roomId = roomId;
            _displayName = displayName;
            _districtId = districtId;
            _plane = plane;
            _connections = connections ?? Array.Empty<Connection>();
        }
    }
}
