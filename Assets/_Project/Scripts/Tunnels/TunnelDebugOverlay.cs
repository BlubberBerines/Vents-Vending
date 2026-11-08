using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VentsAndVending.Tunnels
{
    /// <summary>
    /// F3 debug overlay (Docs/Design/TunnelNetwork.md §10.4): current room, plane, connections and gate state,
    /// switches and fan state. Development aid; harmless in builds.
    /// </summary>
    public sealed class TunnelDebugOverlay : MonoBehaviour
    {
        private bool _visible;
        private GUIStyle _style;
        private FanHazard[] _fans = new FanHazard[0];
        private readonly StringBuilder _text = new StringBuilder();

        private void Start() => _fans = FindObjectsByType<FanHazard>(FindObjectsSortMode.None);

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                _visible = !_visible;
            }
        }

        private void OnGUI()
        {
            TunnelManager manager = TunnelManager.Instance;
            if (!_visible || manager == null)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, richText = true, padding = new RectOffset(10, 10, 8, 8) };

            _text.Clear();
            _text.AppendLine("<b>TUNNELS</b>  (F3)");
            VentRoomDefinition room = manager.CurrentRoom;
            if (room != null)
            {
                _text.AppendLine($"Room: <b>{room.RoomId}</b> {room.DisplayName}   P{room.Plane}   {room.DistrictId}");
                foreach (VentRoomDefinition.Connection c in room.Connections)
                {
                    string gate = "open";
                    if (!string.IsNullOrEmpty(c.GateId))
                    {
                        bool on = manager.GetSwitch(c.GateId, c.GateOpensWhenOff);
                        gate = $"{c.GateId}={(on ? "on" : "off")} → {(on != c.GateOpensWhenOff ? "OPEN" : "closed")}";
                    }
                    _text.AppendLine($"   → {c.DestinationRoomId}{(c.OneWay ? " (one-way)" : "")}   [{gate}]");
                }
            }
            else
            {
                _text.AppendLine("Room: —");
            }
            _text.AppendLine($"Discovered: {manager.State.Discovered.Count}/{manager.Rooms.Count}  {string.Join(" ", manager.State.Discovered)}");
            foreach (var pair in manager.State.Switches)
            {
                _text.AppendLine($"Switch {pair.Key} = {(pair.Value ? "ON" : "OFF")}");
            }
            foreach (FanHazard fan in _fans)
            {
                if (fan != null)
                {
                    _text.AppendLine($"Fan {fan.SystemId}: {fan.State}  {fan.Speed:0}°/s  powered={fan.Powered}");
                }
            }

            GUI.Box(new Rect(12f, 12f, 460f, 26f + 17f * CountLines()), _text.ToString(), _style);
        }

        private int CountLines()
        {
            int n = 0;
            for (int i = 0; i < _text.Length; i++)
            {
                if (_text[i] == '\n')
                {
                    n++;
                }
            }
            return n;
        }
    }
}
