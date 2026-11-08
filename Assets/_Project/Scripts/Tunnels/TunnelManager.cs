using System;
using System.Collections.Generic;
using UnityEngine;
using VentsAndVending.Interaction;

namespace VentsAndVending.Tunnels
{
    /// <summary>
    /// Owns the tunnel network state for the scene: room discovery (with a toast the first time) and infrastructure
    /// switches. Components (breakers, fans, doors) read and write switches here so state never lives in a mesh.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class TunnelManager : MonoBehaviour
    {
        [SerializeField] private List<VentRoomDefinition> _rooms = new List<VentRoomDefinition>();

        private readonly Dictionary<string, VentRoomDefinition> _byId = new Dictionary<string, VentRoomDefinition>();

        public static TunnelManager Instance { get; private set; }

        public TunnelState State { get; } = new TunnelState();
        public VentRoomDefinition CurrentRoom { get; private set; }
        public IReadOnlyList<VentRoomDefinition> Rooms => _rooms;

        /// <summary>(room, firstVisit)</summary>
        public event Action<VentRoomDefinition, bool> RoomEntered;
        /// <summary>A save was loaded: listeners should snap to the restored switch positions without animating.</summary>
        public event Action Restored;

        internal void SetRooms(IEnumerable<VentRoomDefinition> rooms)
        {
            _rooms = new List<VentRoomDefinition>(rooms);
        }

        private void Awake()
        {
            Instance = this;
            foreach (VentRoomDefinition room in _rooms)
            {
                if (room != null && !string.IsNullOrEmpty(room.RoomId))
                {
                    _byId[room.RoomId] = room;
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public VentRoomDefinition Find(string roomId) => roomId != null && _byId.TryGetValue(roomId, out var room) ? room : null;

        public void Enter(VentRoomDefinition room)
        {
            if (room == null || room == CurrentRoom)
            {
                return;
            }

            CurrentRoom = room;
            bool first = State.Discover(room.RoomId);
            if (first)
            {
                var interactor = FindAnyObjectByType<InteractionController>();
                if (interactor != null)
                {
                    interactor.Notify($"<color=#FFD24D>Discovered</color>  {room.Label}");
                }
            }
            RoomEntered?.Invoke(room, first);
        }

        public void Exit(VentRoomDefinition room)
        {
            if (CurrentRoom == room)
            {
                CurrentRoom = null;
            }
        }

        public bool GetSwitch(string id, bool fallback) => State.GetSwitch(id, fallback);

        public bool SetSwitch(string id, bool on, bool fallback) => State.SetSwitch(id, on, fallback);

        public TunnelSave Capture() => State.Capture();

        public void Restore(TunnelSave save)
        {
            State.Restore(save);
            Restored?.Invoke();
        }
    }
}
