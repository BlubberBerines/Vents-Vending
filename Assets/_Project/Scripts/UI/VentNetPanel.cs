using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Interaction;
using VentsAndVending.Orders;

namespace VentsAndVending.UI
{
    /// <summary>
    /// The VentNet inbox on the den PC (GDD §10.3): customer messages with accept/decline, your booked deals with
    /// live countdowns, and a plaza map so you can see where each meet spot is and how exposed it is.
    /// </summary>
    public sealed class VentNetPanel : ModalScreen
    {
        // Plaza footprint shown on the map (world XZ)
        private static readonly Rect PlazaArea = new Rect(-16f, 20.5f, 32f, 30f);
        private const float MapWidth = 470f;
        private const float MapHeight = MapWidth * 30f / 32f;

        private TextMeshProUGUI _trust;
        private RectTransform _inbox;
        private RectTransform _deals;
        private RectTransform _map;
        private TextMeshProUGUI _spotInfo;
        private readonly List<(TextMeshProUGUI text, CustomerOrder order)> _timers = new List<(TextMeshProUGUI, CustomerOrder)>();
        private readonly Dictionary<string, Image> _spotDots = new Dictionary<string, Image>();
        private bool _dirty;

        private void OnEnable() => VentNetTerminal.PanelRequested += OnPanelRequested;

        private void OnDisable()
        {
            VentNetTerminal.PanelRequested -= OnPanelRequested;
            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.Board.Changed -= MarkDirty;
            }
        }

        private void OnPanelRequested(VentNetTerminal terminal, InteractionController interactor)
        {
            if (interactor != Interactor || OrderManager.Instance == null || !BeginOpen())
            {
                return;
            }
            OrderManager.Instance.Board.Changed += MarkDirty;
            BuildMap();
            Rebuild();
        }

        protected override void OnClosing()
        {
            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.Board.Changed -= MarkDirty;
            }
        }

        private void MarkDirty() => _dirty = true;

        protected override void UpdateWhileOpen()
        {
            if (_dirty)
            {
                _dirty = false;
                Rebuild();
            }
            float now = OrderManager.Instance != null ? OrderManager.Instance.Board.Clock : 0f;
            foreach (var (text, order) in _timers)
            {
                if (text != null)
                {
                    text.text = TimerText(order, now);
                }
            }
            PulseSpots();
        }

        // ------------------------------------------------------------------ Content

        private void Rebuild()
        {
            OrderManager manager = OrderManager.Instance;
            OrderBoard board = manager.Board;
            _timers.Clear();
            ClearChildren(_inbox);
            ClearChildren(_deals);

            int trust = board.Trust;
            string trustColour = trust >= 65 ? "#5CFF7A" : trust >= 35 ? "#FFD24D" : "#FF7060";
            _trust.text = $"Trust <color={trustColour}><b>{trust}</b></color><color=#9AA3AD>/100</color>";

            bool anyOffer = false;
            foreach (CustomerOrder o in board.Orders)
            {
                if (o.Status == OrderStatus.Offered)
                {
                    anyOffer = true;
                    OfferRow(manager, o);
                }
            }
            if (!anyOffer)
            {
                Note(_inbox, "No new messages. Customers ping you every minute or so — better rep, more pings.");
            }

            bool anyDeal = false;
            foreach (CustomerOrder o in board.Orders)
            {
                if (o.IsActive)
                {
                    anyDeal = true;
                    DealRow(manager, o);
                }
            }
            foreach (CustomerOrder o in board.Orders)
            {
                if (o.IsFinished && o.Status != OrderStatus.Declined)
                {
                    HistoryRow(o);
                }
            }
            if (!anyDeal)
            {
                Note(_deals, "No deals booked. Accept a message to set a meeting.");
            }
        }

        private void OfferRow(OrderManager manager, CustomerOrder o)
        {
            Image row = UiFactory.Image(_inbox, o.Id, UiFactory.RowColor);
            UiFactory.Layout(row.gameObject, preferredHeight: 128f);
            MeetSpot spot = manager.FindSpot(o.SpotId);
            string product = manager.FindProduct(o.ProductId)?.DisplayName ?? o.ProductId;
            string species = manager.FindCustomer(o.CustomerId)?.SpeciesName ?? "?";
            int unit = manager.UnitPayout(o, o.MinimumQuality);
            string risk = spot != null ? $"<color=#{ColorUtility.ToHtmlStringRGB(MeetSpot.RiskColor(spot.Risk))}>{spot.Risk} risk</color>" : "";

            TextMeshProUGUI from = UiFactory.Text(row.transform, "From", $"<b>{o.CustomerName}</b> <color=#9AA3AD><size=75%>{species}</size></color>", 24f);
            UiFactory.Place(from.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -8f), new Vector2(560f, 32f));

            TextMeshProUGUI body = UiFactory.Text(row.transform, "Body",
                $"Need <b>{o.Quantity}× {product}</b>, {o.MinimumQuality} or better. <color=#19E5FF>{unit} CC each</color> ({unit * o.Quantity} total).\n" +
                $"Meet at <b>{(spot != null ? spot.Label : o.SpotId)}</b> {risk} · I'll be there {OrderRules.Clock(OrderRules.MinLeadTime)}–{OrderRules.Clock(OrderRules.MaxLeadTime)} after you say yes, and wait {OrderRules.Clock(OrderRules.WindowLength)}.",
                18f, TextAlignmentOptions.TopLeft);
            UiFactory.Place(body.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -42f), new Vector2(570f, 80f));

            TextMeshProUGUI timer = UiFactory.Text(row.transform, "Timer", string.Empty, 16f, TextAlignmentOptions.Right, UiFactory.MutedText);
            UiFactory.Place(timer.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -10f), new Vector2(200f, 24f));
            _timers.Add((timer, o));

            string blocker = manager.Board.AcceptBlocker(o);
            Button accept = UiFactory.Button(row.transform, "Accept", "Accept", () => { manager.Accept(o); _dirty = true; },
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 12f), new Vector2(130f, 44f));
            accept.interactable = blocker == null;
            UiFactory.Button(row.transform, "Decline", "Decline", () => { manager.Decline(o); _dirty = true; },
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-156f, 12f), new Vector2(120f, 44f));
            if (blocker != null)
            {
                TextMeshProUGUI why = UiFactory.Text(row.transform, "Blocked", blocker, 15f, TextAlignmentOptions.Right, new Color(1f, 0.6f, 0.4f));
                UiFactory.Place(why.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 60f), new Vector2(280f, 22f));
            }

            AddHover(row.gameObject, o.SpotId);
        }

        private void DealRow(OrderManager manager, CustomerOrder o)
        {
            bool here = o.Status == OrderStatus.Waiting;
            Image row = UiFactory.Image(_deals, o.Id, here ? new Color(0.1f, 0.9f, 1f, 0.14f) : UiFactory.RowColor);
            UiFactory.Layout(row.gameObject, preferredHeight: 62f);
            MeetSpot spot = manager.FindSpot(o.SpotId);
            string product = manager.FindProduct(o.ProductId)?.DisplayName ?? o.ProductId;
            TextMeshProUGUI text = UiFactory.Text(row.transform, "Deal",
                $"<b>Spot {o.SpotId}</b>  {o.CustomerName} — {o.Quantity}× {product} ({o.MinimumQuality}+)\n<size=80%><color=#9AA3AD>{(spot != null ? spot.DisplayName : "")}</color></size>",
                19f, TextAlignmentOptions.Left);
            UiFactory.Place(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(440f, 56f));
            TextMeshProUGUI timer = UiFactory.Text(row.transform, "Timer", string.Empty, 19f, TextAlignmentOptions.Right, here ? UiFactory.Accent : UiFactory.TextColor);
            UiFactory.Place(timer.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(240f, 30f));
            timer.fontStyle = FontStyles.Bold;
            _timers.Add((timer, o));
            AddHover(row.gameObject, o.SpotId);
        }

        private void HistoryRow(CustomerOrder o)
        {
            string line = o.Status switch
            {
                OrderStatus.Completed => $"<color=#5CFF7A>+</color> {o.CustomerName} paid {o.Payout} CC",
                OrderStatus.Missed => $"<color=#FF7060>x</color> Missed {o.CustomerName} at spot {o.SpotId}",
                _ => $"<color=#9AA3AD>·</color> {o.CustomerName}'s message expired",
            };
            Note(_deals, line, 26f);
        }

        private static void Note(RectTransform parent, string text, float height = 44f)
        {
            TextMeshProUGUI t = UiFactory.Text(parent, "Note", text, 17f, TextAlignmentOptions.Left, UiFactory.MutedText);
            UiFactory.Layout(t.gameObject, preferredHeight: height);
        }

        private static string TimerText(CustomerOrder o, float now) => o.Status switch
        {
            OrderStatus.Offered => $"expires in {OrderRules.Clock(o.OfferExpiresAt - now)}",
            OrderStatus.Accepted => $"arrives in {OrderRules.Clock(o.WindowOpensAt - now)}",
            OrderStatus.Waiting => $"WAITING · {OrderRules.Clock(o.WindowClosesAt - now)} left",
            _ => string.Empty,
        };

        // ------------------------------------------------------------------ Map

        private void BuildMap()
        {
            ClearChildren(_map);
            _spotDots.Clear();
            OrderManager manager = OrderManager.Instance;

            // Landmarks (rough plaza layout)
            Block(new Rect(-14f, 34.7f, 1.2f, 2.6f), new Color(1f, 0.3f, 0.85f, 0.5f), "GLORP");
            Block(new Rect(12.9f, 34.7f, 1.2f, 2.6f), new Color(1f, 0.6f, 0.15f, 0.5f), "NOODLES");
            Block(new Rect(4.7f, 47.9f, 2.6f, 1.2f), new Color(0.2f, 0.9f, 1f, 0.5f), "LEGAL");
            Block(new Rect(-0.7f, 34.3f, 1.4f, 1.4f), new Color(0.6f, 0.4f, 1f, 0.6f), "HOLO");
            foreach (Vector2 p in new[] { new Vector2(-10f, 29f), new Vector2(10f, 29f), new Vector2(-10.5f, 47f), new Vector2(10.5f, 47f) })
            {
                Block(new Rect(p.x - 0.5f, p.y - 0.5f, 1f, 1f), new Color(1f, 1f, 1f, 0.25f), null);
            }
            // Drone patrol loop
            Outline(new Rect(-10f, 32f, 20f, 14f), new Color(1f, 0.35f, 0.3f, 0.45f), 2f);
            Label(new Vector2(0f, 46.6f), "<color=#FF7060>drone patrol</color>", 14f);
            // Your way in, and the customers' door
            Block(new Rect(-8.5f, 20.5f, 1f, 0.8f), new Color(0.35f, 1f, 0.45f, 0.9f), null);
            Label(new Vector2(-8f, 22.2f), "<color=#5CFF7A>your vent</color>", 14f);
            Block(new Rect(-1.1f, 49.7f, 2.2f, 0.8f), new Color(1f, 0.3f, 0.85f, 0.9f), null);
            Label(new Vector2(0f, 48.9f), "<color=#FF60D0>customer door</color>", 14f);

            foreach (MeetSpot spot in manager.Spots)
            {
                if (spot == null)
                {
                    continue;
                }
                Vector2 p = MapPoint(new Vector2(spot.transform.position.x, spot.transform.position.z));
                Image dot = UiFactory.Image(_map, $"Spot {spot.SpotId}", MeetSpot.RiskColor(spot.Risk));
                UiFactory.Place(dot.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), p, new Vector2(30f, 30f));
                TextMeshProUGUI letter = UiFactory.Text(dot.transform, "Letter", spot.SpotId, 20f, TextAlignmentOptions.Center, Color.black);
                UiFactory.Stretch(letter.rectTransform);
                letter.fontStyle = FontStyles.Bold;
                _spotDots[spot.SpotId] = dot;
            }
            ShowSpotInfo(null);
        }

        private void PulseSpots()
        {
            OrderManager manager = OrderManager.Instance;
            foreach (var pair in _spotDots)
            {
                bool booked = manager.Board.IsSpotBusy(pair.Key);
                float s = booked ? 30f + Mathf.Sin(Time.unscaledTime * 6f) * 5f : 26f;
                pair.Value.rectTransform.sizeDelta = new Vector2(s, s);
            }
        }

        private void ShowSpotInfo(string spotId)
        {
            OrderManager manager = OrderManager.Instance;
            MeetSpot spot = spotId != null ? manager.FindSpot(spotId) : null;
            if (spot == null)
            {
                var lines = new System.Text.StringBuilder("<b>Meet spots</b>\n");
                foreach (MeetSpot s in manager.Spots)
                {
                    if (s != null)
                    {
                        lines.Append($"<color=#{ColorUtility.ToHtmlStringRGB(MeetSpot.RiskColor(s.Risk))}>●</color> <b>{s.SpotId}</b> {s.DisplayName} <color=#9AA3AD>· {s.Risk} risk</color>\n");
                    }
                }
                _spotInfo.text = lines.ToString();
                return;
            }
            _spotInfo.text = $"<b>{spot.Label}</b>  <color=#{ColorUtility.ToHtmlStringRGB(MeetSpot.RiskColor(spot.Risk))}>{spot.Risk} risk</color>\n<color=#9AA3AD>{spot.Hint}</color>";
        }

        private void AddHover(GameObject row, string spotId)
        {
            var trigger = row.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowSpotInfo(spotId));
            var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => ShowSpotInfo(null));
            trigger.triggers.Add(enter);
            trigger.triggers.Add(exit);
        }

        private static Vector2 MapPoint(Vector2 world) => new Vector2(
            (world.x - PlazaArea.xMin) / PlazaArea.width * MapWidth,
            (world.y - PlazaArea.yMin) / PlazaArea.height * MapHeight);

        private void Block(Rect world, Color colour, string label)
        {
            Vector2 a = MapPoint(world.min), b = MapPoint(world.max);
            Image img = UiFactory.Image(_map, "Block", colour);
            UiFactory.Place(img.rectTransform, Vector2.zero, Vector2.zero, a, b - a);
            if (label != null)
            {
                TextMeshProUGUI t = UiFactory.Text(img.transform, "Label", label, 11f, TextAlignmentOptions.Center, Color.white);
                UiFactory.Stretch(t.rectTransform, -20f);
            }
        }

        private void Outline(Rect world, Color colour, float width)
        {
            Vector2 a = MapPoint(world.min), b = MapPoint(world.max);
            void Edge(Vector2 pos, Vector2 size)
            {
                Image e = UiFactory.Image(_map, "Edge", colour);
                UiFactory.Place(e.rectTransform, Vector2.zero, Vector2.zero, pos, size);
            }
            Edge(a, new Vector2(b.x - a.x, width));
            Edge(new Vector2(a.x, b.y - width), new Vector2(b.x - a.x, width));
            Edge(a, new Vector2(width, b.y - a.y));
            Edge(new Vector2(b.x - width, a.y), new Vector2(width, b.y - a.y));
        }

        private void Label(Vector2 world, string text, float size)
        {
            TextMeshProUGUI t = UiFactory.Text(_map, "MapLabel", text, size, TextAlignmentOptions.Center);
            UiFactory.Place(t.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), MapPoint(world), new Vector2(160f, 20f));
        }

        // ------------------------------------------------------------------ Layout

        protected override GameObject BuildUi()
        {
            Canvas canvas = UiFactory.CreateCanvas(transform, "VentNet Canvas", 13);
            var dim = UiFactory.Image(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.5f));
            UiFactory.Stretch(dim.rectTransform);

            var panel = UiFactory.Image(canvas.transform, "Panel", new Color(0.02f, 0.06f, 0.04f, 0.94f));
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 880f));
            var accent = UiFactory.Image(panel.transform, "AccentBar", new Color(0.35f, 1f, 0.5f));
            UiFactory.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1500f, 4f));

            TextMeshProUGUI title = UiFactory.Text(panel.transform, "Title", "<b>VENTNET</b> <color=#5C8A6A><size=70%>// burner inbox · relay 3-C (unlicensed)</size></color>", 34f, TextAlignmentOptions.Left, new Color(0.55f, 1f, 0.65f));
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -22f), new Vector2(900f, 46f));
            _trust = UiFactory.Text(panel.transform, "Trust", string.Empty, 26f, TextAlignmentOptions.Right);
            UiFactory.Place(_trust.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -26f), new Vector2(300f, 40f));

            TextMeshProUGUI inboxTitle = UiFactory.Text(panel.transform, "InboxTitle", "Messages", 24f, TextAlignmentOptions.Left, UiFactory.MutedText);
            UiFactory.Place(inboxTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -84f), new Vector2(400f, 32f));
            _inbox = UiFactory.Rect("Inbox", panel.transform);
            UiFactory.Place(_inbox, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -120f), new Vector2(900f, 420f));
            UiFactory.VerticalList(_inbox, 8f).childAlignment = TextAnchor.UpperLeft;

            TextMeshProUGUI dealsTitle = UiFactory.Text(panel.transform, "DealsTitle", "Deals", 24f, TextAlignmentOptions.Left, UiFactory.MutedText);
            UiFactory.Place(dealsTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -552f), new Vector2(400f, 32f));
            _deals = UiFactory.Rect("Deals", panel.transform);
            UiFactory.Place(_deals, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -588f), new Vector2(900f, 240f));
            UiFactory.VerticalList(_deals, 6f).childAlignment = TextAnchor.UpperLeft;

            TextMeshProUGUI mapTitle = UiFactory.Text(panel.transform, "MapTitle", "Central Plaza", 24f, TextAlignmentOptions.Left, UiFactory.MutedText);
            UiFactory.Place(mapTitle.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -84f), new Vector2(MapWidth, 32f));
            var mapBg = UiFactory.Image(panel.transform, "MapFrame", new Color(0.55f, 1f, 0.65f, 0.06f));
            UiFactory.Place(mapBg.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -120f), new Vector2(MapWidth, MapHeight));
            // Map draws with +Y = north (the far end of the plaza, where customers come in)
            _map = UiFactory.Rect("Map", mapBg.transform);
            _map.anchorMin = Vector2.zero;
            _map.anchorMax = Vector2.zero;
            _map.pivot = Vector2.zero;
            _map.anchoredPosition = Vector2.zero;
            _map.sizeDelta = new Vector2(MapWidth, MapHeight);

            _spotInfo = UiFactory.Text(panel.transform, "SpotInfo", string.Empty, 18f, TextAlignmentOptions.TopLeft);
            UiFactory.Place(_spotInfo.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -130f - MapHeight), new Vector2(MapWidth, 160f));

            TextMeshProUGUI hint = UiFactory.Text(panel.transform, "Hint",
                "Accept a message, make the product, then meet them at the lit spot while they wait   ·   [ E ] [ Esc ] Close",
                18f, TextAlignmentOptions.Center, UiFactory.MutedText);
            UiFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(1400f, 30f));
            return canvas.gameObject;
        }
    }
}
