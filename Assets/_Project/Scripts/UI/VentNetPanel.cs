using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Customers;
using VentsAndVending.Economy;
using VentsAndVending.Inventory;
using VentsAndVending.Interaction;
using VentsAndVending.Orders;

namespace VentsAndVending.UI
{
    /// <summary>
    /// The VentNet inbox on the den PC (GDD §10.3), styled as a terminal window: customer messages as cards (avatar,
    /// their words, chips for the facts, accept/decline), your booked deals with live timers, and a plaza map with the
    /// meet spots coloured by risk.
    /// </summary>
    public sealed class VentNetPanel : ModalScreen
    {
        private static readonly Rect PlazaArea = new Rect(-16f, 20.5f, 32f, 30f);
        [SerializeField] private Transform _mapFrame;
        [SerializeField] private Rect _mapArea = new Rect(-6.5f,-6.5f,13,13);
        private const float MapWidth = 470f;
        private const float MapHeight = 400f;

        private static readonly Color Term = new Color(0.027f, 0.043f, 0.035f, 0.98f);
        private static readonly Color TermCard = new Color(0.05f, 0.075f, 0.063f, 1f);
        private static readonly Color TermText = new Color(0.867f, 0.922f, 0.882f, 1f);
        private static readonly Color TermMuted = new Color(0.56f, 0.7f, 0.6f, 1f);

        private TextMeshProUGUI _trustValue;
        private Image _trustFill;
        private TextMeshProUGUI _inboxTitle;
        private RectTransform _inbox;
        private RectTransform _deals;
        private RectTransform _map;
        private TextMeshProUGUI _spotTitle;
        private TextMeshProUGUI _spotHint;
        private readonly List<(TextMeshProUGUI text, CustomerOrder order)> _timers = new List<(TextMeshProUGUI, CustomerOrder)>();
        private readonly Dictionary<string, Image> _spotDots = new Dictionary<string, Image>();
        private bool _dirty;
        private bool _showSupplies;
        private Button _tabMessages;
        private Button _tabSupplies;

        [Header("Supplies (courier to ring delivery bay in the station)")]
        [SerializeField] private SupplyListing[] _supplies = new SupplyListing[0];
        [SerializeField] private InventoryComponent _locker;
        private float _nextDeliveryRefresh;
        private TextMeshProUGUI _deliveryStatus;
        private readonly SupplyCart _cart=new SupplyCart();
        private readonly Dictionary<string,float> _meetingTimes=new Dictionary<string,float>();
        private TextMeshProUGUI _clockLabel;

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
            if(_clockLabel!=null && OrderManager.Instance!=null)_clockLabel.text=StationTime.Label(OrderManager.Instance.Board.Clock)+" / "+(StationTime.IsNight(OrderManager.Instance.Board.Clock)?"NIGHT SHIFT":"DAY SHIFT");
            if(_showSupplies && SupplyDeliveryService.Instance!=null && Time.unscaledTime>=_nextDeliveryRefresh)
            {_nextDeliveryRefresh=Time.unscaledTime+1;if(_deliveryStatus!=null)_deliveryStatus.text=SupplyDeliveryService.Instance.StatusText;}
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
            OrderManager manager = OrderManager.Instance;
            foreach (var pair in _spotDots)
            {
                bool booked = manager.Board.IsSpotBusy(pair.Key);
                float s = booked ? 40f + Mathf.Sin(Time.unscaledTime * 6f) * 5f : 36f;
                pair.Value.rectTransform.sizeDelta = new Vector2(s, s);
            }
        }

        // ------------------------------------------------------------------ Content

        private void Rebuild()
        {
            OrderManager manager = OrderManager.Instance;
            OrderBoard board = manager.Board;
            _timers.Clear();
            _deliveryStatus=null;
            ClearChildren(_inbox);
            ClearChildren(_deals);

            _trustValue.text = board.Trust.ToString();
            UiFactory.SetBar(_trustFill, board.Trust / 100f);
            _trustFill.color = board.Trust >= 65 ? UiFactory.Go : board.Trust >= 35 ? UiFactory.Hazard : UiFactory.Alert;

            int offers = 0;
            foreach (CustomerOrder o in board.Orders)
            {
                if (o.Status == OrderStatus.Offered)
                {
                    offers++;
                    if (!_showSupplies)
                    {
                        OfferCard(manager, o);
                    }
                }
            }
            _inboxTitle.text = _showSupplies ? (SupplyDeliveryService.Instance!=null?"SUPPLIES / RING COURIER DELIVERY":"SUPPLIES · DELIVERED TO YOUR LOCKER") : offers > 0 ? $"MESSAGES · {offers} NEW" : "MESSAGES";
            StyleTabs(offers);
            if (_showSupplies)
            {
                if(SupplyDeliveryService.Instance!=null)_deliveryStatus=Note(_inbox,SupplyDeliveryService.Instance.StatusText,64);
                CartCard();
                foreach (SupplyListing listing in _supplies)
                {
                    SupplyCard(listing);
                }
            }
            else if (offers == 0)
            {
                Note(_inbox,manager.RecruitmentRequired?"No new requests. Talk to passengers and offer a sample to recruit regular customers. Your regulars will message you here.":"No new messages. Customers ping you every minute or so — better trust, more pings.",80);
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
            if (!anyDeal)
            {
                Note(_deals, "No deals booked. Accept a message to set a meeting.");
            }
            var history = new System.Text.StringBuilder();
            foreach (CustomerOrder o in board.Orders)
            {
                if (o.Status == OrderStatus.Completed)
                {
                    history.Append($"<color=#5CFF8A>+</color> {o.CustomerName} paid {o.Payout} CC    ");
                }
                else if (o.Status == OrderStatus.Missed)
                {
                    history.Append($"<color=#FF6B5E>×</color> Missed {o.CustomerName} {(o.RoomMeeting ? "in the lounge" : "at spot "+o.SpotId)}    ");
                }
            }
            if (history.Length > 0)
            {
                Note(_deals, history.ToString(), 28f);
            }
        }

        private void ShowSupplies(bool on)
        {
            _showSupplies = on;
            _inbox.anchoredPosition=Vector2.zero;
            Rebuild();
        }

        private void StyleTabs(int offers)
        {
            if (_tabMessages == null)
            {
                return;
            }
            _tabMessages.GetComponentInChildren<TextMeshProUGUI>().text = offers > 0 ? $"Messages ({offers})" : "Messages";
            _tabMessages.interactable = _showSupplies;
            _tabSupplies.interactable = !_showSupplies;
        }

        /// <summary>A supplies listing: what it is, the price, and a Buy button (delivered to the den locker).</summary>
        private void SupplyCard(SupplyListing listing)
        {
            if (listing.Item == null)
            {
                return;
            }
            Image card = UiFactory.Panel(_inbox, listing.Item.Id, TermCard, 0f, 14f);
            UiFactory.Layout(card.gameObject, preferredHeight: 120f);
            Transform c = card.transform;

            Image swatch = UiFactory.Panel(c, "Swatch", listing.Item.UiColor, 0f, 12f);
            UiFactory.Place(swatch.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(52f, 52f));
            TextMeshProUGUI name = UiFactory.Heading(c, "Name", listing.Item.DisplayName, 22f);
            UiFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -14f), new Vector2(460f, 30f));
            TextMeshProUGUI blurb = UiFactory.Text(c, "Blurb", $"“{listing.Blurb}”  <color=#8FB39A>{listing.Item.Description}</color>", 17f, TextAlignmentOptions.TopLeft, TermText);
            UiFactory.Place(blurb.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -46f), new Vector2(560f, 60f));
            blurb.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI price = UiFactory.Mono(c, "Price", $"{listing.Price} CC", 24f, TextAlignmentOptions.Right, UiFactory.Credit);
            UiFactory.Place(price.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-214f, -18f), new Vector2(120f, 30f));
            UiFactory.Button(c, "AddToCart", "Add to cart", () => AddToCart(listing),
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 18f), new Vector2(180f, 46f), ButtonStyle.Go);
        }

        private void AddToCart(SupplyListing listing)
        {
            if(!_cart.Add(listing))Interactor.Notify("Cart limit: 12 items. Checkout or remove an item first.");
            _dirty=true;
        }

        private void CartCard()
        {
            var lines=_cart.Items.GroupBy(l=>l.Item).ToArray();
            var card=UiFactory.Panel(_inbox,"Supply Cart",TermCard,0,12);
            UiFactory.Layout(card.gameObject,preferredHeight:130+lines.Length*42);
            var title=UiFactory.Heading(card.transform,"Cart Title",$"CART / {_cart.Count} ITEMS / {_cart.Total} CC",22);
            UiFactory.Place(title.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(18,-14),new Vector2(640,32));
            if(lines.Length==0)
            {
                var hint=UiFactory.Text(card.transform,"Cart Hint","Add supplies below. Nothing is charged until checkout.",17,TextAlignmentOptions.Left,TermMuted);
                UiFactory.Place(hint.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(18,-48),new Vector2(610,26));
            }
            for(int index=0;index<lines.Length;index++)
            {
                var line=lines[index];var listing=line.First();float y=-52-index*42;
                var label=UiFactory.Text(card.transform,"Cart Line",$"{line.Key.DisplayName} x{line.Count()} / {line.Sum(l=>l.Price)} CC",18,TextAlignmentOptions.Left,TermText);
                UiFactory.Place(label.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(18,y),new Vector2(490,34));
                var remove=UiFactory.Button(card.transform,"Remove "+line.Key.Id,"-",()=>{_cart.Remove(listing.Item);_dirty=true;},new Vector2(1,1),new Vector2(1,1),new Vector2(-76,y),new Vector2(46,34));
                var add=UiFactory.Button(card.transform,"Add "+line.Key.Id,"+",()=>AddToCart(listing),new Vector2(1,1),new Vector2(1,1),new Vector2(-18,y),new Vector2(46,34));
                foreach(var control in new[]{remove,add})
                {var labelText=control.GetComponentInChildren<TextMeshProUGUI>();labelText.fontSize=18;labelText.overflowMode=TextOverflowModes.Overflow;}
            }
            UiFactory.Button(card.transform,"Clear Cart","Clear cart",()=>{_cart.Clear();_dirty=true;},new Vector2(0,0),new Vector2(0,0),new Vector2(18,14),new Vector2(140,40));
            var checkout=UiFactory.Button(card.transform,"Checkout Cart",$"Checkout / {_cart.Total} CC",CheckoutCart,new Vector2(1,0),new Vector2(1,0),new Vector2(-18,14),new Vector2(260,44),ButtonStyle.Go);
            checkout.interactable=_cart.Count>0;
        }

        private void CheckoutCart()
        {
            var wallet = Interactor != null ? Interactor.GetComponent<PlayerWallet>() : null;
            if (wallet == null || _locker == null)
            {
                return;
            }
            var delivery=SupplyDeliveryService.Instance;
            int count=_cart.Count;
            var legacy=delivery==null?new DeliveryQueue():null;
            PurchaseResult result = delivery!=null?delivery.Checkout(_cart.Items,wallet.Wallet):legacy.OrderCart(_cart.Items,wallet.Wallet,_locker.Model);
            if(result==PurchaseResult.OrderQueued)
            {
                if(legacy!=null)legacy.Tick(46,_locker.Model);
                _cart.Clear();
            }
            Interactor.Notify(result==PurchaseResult.OrderQueued?(delivery!=null?$"Combined order booked: {count} items. Courier window {delivery.WindowText}; collect at the ring delivery bay.":"Combined order delivered to your locker."):result==PurchaseResult.QueueFull?"Cart or courier manifest is full. Your cart is unchanged.":result==PurchaseResult.LockerFull?"Delivery storage is full or reserved. Your cart is unchanged.":SupplyShop.Message(result));
            _dirty = true;
        }

        private void OfferCard(OrderManager manager, CustomerOrder o)
        {
            CustomerDefinition who = manager.FindCustomer(o.CustomerId);
            MeetSpot spot = manager.FindSpot(o.SpotId);
            var product = manager.FindProduct(o.ProductId);
            string productName = product != null ? product.DisplayName : o.ProductId;
            int unit = manager.UnitPayout(o, o.MinimumQuality);

            Image card = UiFactory.Panel(_inbox, o.Id, TermCard, 0f, 14f);
            UiFactory.Layout(card.gameObject, preferredHeight: 226f);
            Transform c = card.transform;

            Color avatarColour = who != null ? who.AppearanceColor(o.CustomerName) : UiFactory.Hazard;
            Image avatar = UiFactory.Panel(c, "Avatar", avatarColour, 0f, 12f);
            UiFactory.Place(avatar.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(104f, 104f));
            if(who != null && who.PortraitFor(o.CustomerName) != null){avatar.sprite=who.PortraitFor(o.CustomerName);avatar.color=Color.white;avatar.preserveAspect=true;}
            else {
                TextMeshProUGUI letter = UiFactory.Display(avatar.transform, "Letter", o.CustomerName.Substring(0, 1), 26f, TextAlignmentOptions.Center, Color.white);
                UiFactory.Stretch(letter.rectTransform);
            }

            TextMeshProUGUI name = UiFactory.Heading(c, "Name", $"{o.CustomerName} <size=75%><color=#8FB39A>{(who != null ? who.SpeciesName : "")}</color></size>", 22f);
            UiFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -14f), new Vector2(440f, 30f));
            var identity=UiFactory.Text(c,"Identity",who!=null?who.IdentificationText(o.CustomerName):"Match the face and name",17,TextAlignmentOptions.Left,TermMuted);
            UiFactory.Place(identity.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(140,-46),new Vector2(550,26));
            TextMeshProUGUI timer = UiFactory.Mono(c, "Timer", string.Empty, 16f, TextAlignmentOptions.Right, TermMuted);
            UiFactory.Place(timer.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-214f, -18f), new Vector2(160f, 24f));
            _timers.Add((timer, o));

            string quote = who != null ? CustomerDefinition.Pick(who.Lines.Greeting) : "…";
            quote = quote.Replace("{product}", productName).Replace("{name}", o.CustomerName).Replace("{qty}", o.Quantity.ToString());
            TextMeshProUGUI said = UiFactory.Text(c, "Quote", $"“{quote}”", 18f, TextAlignmentOptions.TopLeft, TermText);
            UiFactory.Place(said.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -78f), new Vector2(600f, 46f));
            said.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform chips = UiFactory.Rect("Chips", c);
            UiFactory.Place(chips, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 66f), new Vector2(710f, 32f));
            UiFactory.HRow(chips, 8f);
            UiFactory.Chip(chips, "Qty", $"{o.Quantity}× {productName}", new Color(0.11f, 0.165f, 0.13f), TermText, 15f);
            var (qbg, qfg) = UiFactory.QualityColours(o.MinimumQuality);
            UiFactory.Chip(chips, "Quality", $"{o.MinimumQuality}+", qbg, qfg, 15f);
            UiFactory.Chip(chips, "Pay", o.Quantity > 1 ? $"{unit} CC each" : $"{unit} CC", UiFactory.Credit, new Color(0.016f, 0.133f, 0.165f), 15f);
            if (spot != null)
            {
                var (rbg, rfg) = UiFactory.RiskColours((int)spot.Risk);
                UiFactory.Chip(chips, "Spot", spot.RoomMeeting ? "Meet me in the lounge" : $"{spot.SpotId} · {spot.Risk} risk", rbg, rfg, 15f);
            }

            string blocker = manager.Board.AcceptBlocker(o);
            string acceptLabel = blocker == null ? "Accept" : blocker.Contains("Spot") ? $"Spot {o.SpotId} booked" : "Max deals";
            if(!_meetingTimes.TryGetValue(o.Id,out float proposed) || proposed<=manager.Board.Clock+1)_meetingTimes[o.Id]=StationTime.NextHalfHour(manager.Board.Clock);
            var meeting=UiFactory.Text(c,"Choose Meeting","Meet: "+StationTime.Label(_meetingTimes[o.Id]),18,TextAlignmentOptions.Left,TermText);
            UiFactory.Place(meeting.rectTransform,new Vector2(0,0),new Vector2(0,0),new Vector2(90,20),new Vector2(430,32));
            UiFactory.Button(c,"Earlier Meeting","-30m",()=>AdjustMeeting(o,-30),new Vector2(0,0),new Vector2(0,0),new Vector2(18,18),new Vector2(66,38)).GetComponentInChildren<TextMeshProUGUI>().fontSize=16;
            UiFactory.Button(c,"Later Meeting","+30m",()=>AdjustMeeting(o,30),new Vector2(0,0),new Vector2(0,0),new Vector2(530,18),new Vector2(66,38)).GetComponentInChildren<TextMeshProUGUI>().fontSize=16;
            Button accept = UiFactory.Button(c, "Accept", blocker==null?"Book time":acceptLabel, () =>
            {
                if(!manager.Schedule(o,_meetingTimes[o.Id]))Interactor.Notify("Choose a future meeting time. You can book up to one station day ahead.");
                _dirty = true;
            },
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 76f), new Vector2(180f, 46f), ButtonStyle.Go);
            accept.interactable = blocker == null;
            UiFactory.Button(c, "Decline", "Decline", () => { manager.Decline(o); _dirty = true; },
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 14f), new Vector2(180f, 44f));

            AddHover(card.gameObject, o.SpotId);
        }

        private void AdjustMeeting(CustomerOrder order,float minutes)
        {
            float now=OrderManager.Instance.Board.Clock;
            _meetingTimes[order.Id]=Mathf.Clamp(_meetingTimes[order.Id]+minutes,now+30,now+StationTime.DaySeconds);
            _dirty=true;
        }

        private void DealRow(OrderManager manager, CustomerOrder o)
        {
            bool here = o.Status == OrderStatus.Waiting;
            MeetSpot spot = manager.FindSpot(o.SpotId);
            var product = manager.FindProduct(o.ProductId);
            Image row = UiFactory.Panel(_deals, o.Id, here ? new Color(0.055f, 0.1f, 0.08f) : TermCard, 0f, 12f);
            UiFactory.Layout(row.gameObject, preferredHeight: 98f);
            if (here)
            {
                var glow = row.gameObject.AddComponent<Outline>();
                glow.effectColor = new Color(0.25f, 0.88f, 1f, 0.6f);
                glow.effectDistance = new Vector2(1.5f, -1.5f);
            }

            var (bg, fg) = UiFactory.RiskColours(spot != null ? (int)spot.Risk : 1);
            Image badge = UiFactory.Panel(row.transform, "Badge", bg, 0f, 9f);
            UiFactory.Place(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(78f, 78f));
            var who=manager.FindCustomer(o.CustomerId);
            if(who!=null && who.PortraitFor(o.CustomerName)!=null){badge.sprite=who.PortraitFor(o.CustomerName);badge.color=Color.white;badge.preserveAspect=true;}
            else {
                TextMeshProUGUI letter = UiFactory.Heading(badge.transform, "Letter", o.SpotId, 22f, TextAlignmentOptions.Center, fg);
                UiFactory.Stretch(letter.rectTransform);
            }

            TextMeshProUGUI text = UiFactory.Text(row.transform, "Deal",
                $"<b>{o.CustomerName}</b> <color=#8FB39A>— {o.Quantity}× {(product != null ? product.DisplayName : o.ProductId)} {o.MinimumQuality}+ · {(spot != null ? spot.DisplayName : "")}</color>",
                19f, TextAlignmentOptions.Left, TermText);
            UiFactory.Place(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(106f, 0f), new Vector2(590f, 72f));
            text.text += "\n"+(who!=null?who.IdentificationText(o.CustomerName):"");
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            TextMeshProUGUI timer = UiFactory.Mono(row.transform, "Timer", string.Empty, 19f, TextAlignmentOptions.Right, here ? UiFactory.Credit : TermText);
            UiFactory.Place(timer.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(230f, 30f));
            _timers.Add((timer, o));
            AddHover(row.gameObject, o.SpotId);
        }

        private static TextMeshProUGUI Note(RectTransform parent, string text, float height = 44f)
        {
            TextMeshProUGUI t = UiFactory.Text(parent, "Note", text, 17f, TextAlignmentOptions.Left, TermMuted);
            UiFactory.Layout(t.gameObject, preferredHeight: height);
            return t;
        }

        private static string TimerText(CustomerOrder o, float now) => o.Status switch
        {
            OrderStatus.Offered => $"expires {OrderRules.Clock(o.OfferExpiresAt - now)}",
            OrderStatus.Accepted => $"arrives in {OrderRules.Clock(o.WindowOpensAt - now)}",
            OrderStatus.Waiting => $"WAITING · {OrderRules.Clock(o.WindowClosesAt - now)}",
            _ => string.Empty,
        };

        // ------------------------------------------------------------------ Map

        private void BuildMap()
        {
            ClearChildren(_map);
            _spotDots.Clear();
            OrderManager manager = OrderManager.Instance;

            if (_mapFrame == null)
            {
            Block(new Rect(-14f, 34.7f, 1.2f, 2.6f), new Color(0.73f, 0.25f, 0.56f), "GLORP");
            Block(new Rect(12.9f, 34.7f, 1.2f, 2.6f), new Color(0.73f, 0.45f, 0.16f), "NOODLES");
            Block(new Rect(4.7f, 47.9f, 2.6f, 1.2f), new Color(0.15f, 0.6f, 0.7f), "LEGAL");
            Block(new Rect(-0.8f, 34.2f, 1.6f, 1.6f), new Color(0.49f, 0.35f, 0.84f), null);
            foreach (Vector2 p in new[] { new Vector2(-10f, 29f), new Vector2(10f, 29f), new Vector2(-10.5f, 47f), new Vector2(10.5f, 47f) })
            {
                Block(new Rect(p.x - 0.5f, p.y - 0.5f, 1f, 1f), new Color(1f, 1f, 1f, 0.22f), null);
            }
            Outline(new Rect(-10f, 32f, 20f, 14f), new Color(1f, 0.42f, 0.37f, 0.55f), 2f);
            Label(new Vector2(0f, 46.9f), "<color=#FF8A80>DRONE LOOP</color>");
            Block(new Rect(-8.5f, 20.6f, 1f, 0.8f), UiFactory.Go, null);
            Label(new Vector2(-8f, 22.4f), "<color=#5CFF8A>YOUR VENT</color>");
            Block(new Rect(-1.1f, 49.7f, 2.2f, 0.7f), new Color(0.88f, 0.36f, 0.85f), null);
            Label(new Vector2(0f, 48.8f), "<color=#F48AE8>CUSTOMER DOOR</color>");
            }
            else
            {
                Outline(new Rect(-6,-6,12,12),TermMuted,2);
                Label(new Vector2(0,5.6f),"CUSTOMER ENTRANCE");
                Label(new Vector2(0,-3),"CONCOURSE LOUNGE");
                Label(new Vector2(3,3),"SERVICE GRILLE >");
            }

            foreach (MeetSpot spot in manager.Spots)
            {
                if (spot == null)
                {
                    continue;
                }
                if(spot.RoomMeeting)continue;
                Vector3 position = _mapFrame != null ? _mapFrame.InverseTransformPoint(spot.transform.position) : spot.transform.position;
                Vector2 p = MapPoint(new Vector2(position.x, position.z));
                Image dot = UiFactory.Image(_map, $"Spot {spot.SpotId}", MeetSpot.RiskColor(spot.Risk), false);
                if (UiTheme.Current != null && UiTheme.Current.Circle != null)
                {
                    dot.sprite = UiTheme.Current.Circle;
                }
                UiFactory.Place(dot.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), p, new Vector2(36f, 36f));
                TextMeshProUGUI letter = UiFactory.Heading(dot.transform, "Letter", spot.SpotId, 20f, TextAlignmentOptions.Center, new Color(0.05f, 0.05f, 0.05f));
                UiFactory.Stretch(letter.rectTransform);
                _spotDots[spot.SpotId] = dot;
            }
            ShowSpotInfo(null);
        }

        private void ShowSpotInfo(string spotId)
        {
            OrderManager manager = OrderManager.Instance;
            MeetSpot spot = spotId != null ? manager.FindSpot(spotId) : null;
            if (spot == null)
            {
                _spotTitle.text = _mapFrame != null ? "Find your customer" : "Meet spots";
                var lines = new System.Text.StringBuilder();
                foreach (MeetSpot s in manager.Spots)
                {
                    if (s != null)
                    {
                        if(s.RoomMeeting){if(lines.Length==0)lines.Append("Meet me in the lounge. Match the mugshot and name among passengers.\n");}
                        else lines.Append($"<color=#{ColorUtility.ToHtmlStringRGB(MeetSpot.RiskColor(s.Risk))}>●</color> <b>{s.SpotId}</b> {s.DisplayName} · {s.Risk} risk\n");
                    }
                }
                _spotHint.text = lines.ToString();
                return;
            }
            _spotTitle.text = spot.RoomMeeting ? spot.DisplayName : $"{spot.SpotId} · {spot.DisplayName}  <size=75%><color=#{ColorUtility.ToHtmlStringRGB(MeetSpot.RiskColor(spot.Risk))}>{spot.Risk} risk</color></size>";
            _spotHint.text = spot.Hint;
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

        private Vector2 MapPoint(Vector2 world)
        {
            Rect area = _mapFrame != null ? _mapArea : PlazaArea;
            return new Vector2((world.x-area.xMin)/area.width*MapWidth,(world.y-area.yMin)/area.height*MapHeight);
        }

        private void Block(Rect world, Color colour, string label)
        {
            Vector2 a = MapPoint(world.min), b = MapPoint(world.max);
            Image img = UiFactory.Image(_map, "Block", colour);
            UiFactory.Place(img.rectTransform, Vector2.zero, Vector2.zero, a, b - a);
            img.pixelsPerUnitMultiplier = 6f;
            if (label != null)
            {
                TextMeshProUGUI t = UiFactory.Label(img.transform, "Label", label, Color.white, TextAlignmentOptions.Center);
                t.fontSize = 11f;
                t.characterSpacing = 2f;
                UiFactory.Stretch(t.rectTransform, -24f);
            }
        }

        private void Outline(Rect world, Color colour, float width)
        {
            Vector2 a = MapPoint(world.min), b = MapPoint(world.max);
            void Edge(Vector2 pos, Vector2 size)
            {
                Image e = UiFactory.Image(_map, "Edge", colour, false);
                UiFactory.Place(e.rectTransform, Vector2.zero, Vector2.zero, pos, size);
            }
            Edge(a, new Vector2(b.x - a.x, width));
            Edge(new Vector2(a.x, b.y - width), new Vector2(b.x - a.x, width));
            Edge(a, new Vector2(width, b.y - a.y));
            Edge(new Vector2(b.x - width, a.y), new Vector2(width, b.y - a.y));
        }

        private void Label(Vector2 world, string text)
        {
            TextMeshProUGUI t = UiFactory.Label(_map, "MapLabel", text, null, TextAlignmentOptions.Center);
            t.fontSize = 12f;
            UiFactory.Place(t.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), MapPoint(world), new Vector2(200f, 20f));
        }

        // ------------------------------------------------------------------ Layout

        protected override GameObject BuildUi()
        {
            Canvas canvas = UiFactory.CreateCanvas(transform, "VentNet Canvas", 13);
            UiFactory.Dim(canvas.transform, 0.7f);

            Image window = UiFactory.Panel(canvas.transform, "Window", Term, 8f, 18f);
            UiFactory.Place(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560f, 900f));
            var screenEdge = window.gameObject.AddComponent<Outline>();
            screenEdge.effectColor = new Color(0.36f, 1f, 0.54f, 0.25f);
            screenEdge.effectDistance = new Vector2(1.5f, -1.5f);
            Transform w = window.transform;

            // Title bar
            Image bar = UiFactory.Image(w, "TitleBar", new Color(0.043f, 0.07f, 0.055f, 1f), false);
            UiFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(1552f, 72f));
            TextMeshProUGUI title = UiFactory.Mono(bar.transform, "Title", "VENTNET", 32f, TextAlignmentOptions.Left, UiFactory.Go);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(200f, 40f));
            TextMeshProUGUI sub = _clockLabel=UiFactory.Label(bar.transform, "Sub", "Station time", TermMuted);
            UiFactory.Place(sub.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, -2f), new Vector2(600f, 24f));
            TextMeshProUGUI trustLabel = UiFactory.Label(bar.transform, "TrustLabel", "Rep", new Color(0.79f, 0.85f, 0.81f));
            UiFactory.Place(trustLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-420f, 0f), new Vector2(90f, 24f));
            _trustFill = UiFactory.Bar(bar.transform, "Trust", 0.5f, UiFactory.Hazard);
            UiFactory.Place((RectTransform)_trustFill.transform.parent, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-220f, 0f), new Vector2(190f, 16f));
            _trustValue = UiFactory.Mono(bar.transform, "TrustValue", "50", 22f, TextAlignmentOptions.Left, UiFactory.Hazard);
            UiFactory.Place(_trustValue.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(60f, 30f));
            UiFactory.Button(bar.transform, "Close", "Esc", Close, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(80f, 44f));

            // Left: inbox + deals
            _inboxTitle = UiFactory.Label(w, "InboxTitle", "Messages", TermMuted);
            UiFactory.Place(_inboxTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -96f), new Vector2(500f, 24f));
            _tabMessages = UiFactory.Button(w, "TabMessages", "Messages", () => ShowSupplies(false),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(690f, -84f), new Vector2(170f, 38f));
            _tabSupplies = UiFactory.Button(w, "TabSupplies", "Supplies", () => ShowSupplies(true),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(870f, -84f), new Vector2(120f, 38f));
            var inboxView=UiFactory.Rect("Inbox Viewport",w);
            UiFactory.Place(inboxView,new Vector2(0,1),new Vector2(0,1),new Vector2(30,-128),new Vector2(960,456));
            inboxView.gameObject.AddComponent<RectMask2D>();
            var scroll=inboxView.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;
            // A transparent raycast surface lets the wheel work between cards as well.
            var hit=inboxView.gameObject.AddComponent<Image>();hit.color=Color.clear;
            _inbox = UiFactory.Rect("Inbox", inboxView);
            _inbox.anchorMin=new Vector2(0,1);_inbox.anchorMax=Vector2.one;_inbox.pivot=new Vector2(.5f,1);
            _inbox.sizeDelta=Vector2.zero;_inbox.anchoredPosition=Vector2.zero;
            scroll.viewport=inboxView;scroll.content=_inbox;
            _inbox.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            UiFactory.VerticalList(_inbox, 12f).childAlignment = TextAnchor.UpperLeft;

            TextMeshProUGUI dealsTitle = UiFactory.Label(w, "DealsTitle", "Deals", TermMuted);
            UiFactory.Place(dealsTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -600f), new Vector2(500f, 24f));
            _deals = UiFactory.Rect("Deals", w);
            UiFactory.Place(_deals, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -632f), new Vector2(960f, 240f));
            UiFactory.VerticalList(_deals, 8f).childAlignment = TextAnchor.UpperLeft;

            Image divider = UiFactory.Image(w, "Divider", new Color(0f, 0f, 0f, 1f), false);
            UiFactory.Place(divider.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1018f, -74f), new Vector2(2f, 824f));

            // Right: map + spot card
            TextMeshProUGUI mapTitle = UiFactory.Label(w, "MapTitle", _mapFrame != null ? "Concourse Lounge" : "Central Plaza", TermMuted);
            UiFactory.Place(mapTitle.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -96f), new Vector2(MapWidth, 24f));
            Image mapBg = UiFactory.Panel(w, "Map", new Color(0.05f, 0.086f, 0.067f, 1f), 0f, 14f);
            UiFactory.Place(mapBg.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -128f), new Vector2(MapWidth + 20f, MapHeight + 20f));
            _map = UiFactory.Rect("MapContent", mapBg.transform);
            _map.anchorMin = Vector2.zero;
            _map.anchorMax = Vector2.zero;
            _map.pivot = Vector2.zero;
            _map.anchoredPosition = new Vector2(10f, 10f);
            _map.sizeDelta = new Vector2(MapWidth, MapHeight);

            Image info = UiFactory.Panel(w, "SpotCard", TermCard, 0f, 14f);
            UiFactory.Place(info.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -566f), new Vector2(MapWidth + 20f, 170f));
            _spotTitle = UiFactory.Heading(info.transform, "Title", string.Empty, 21f, TextAlignmentOptions.TopLeft, TermText);
            UiFactory.Place(_spotTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(MapWidth - 16f, 30f));
            _spotHint = UiFactory.Text(info.transform, "Hint", string.Empty, 17f, TextAlignmentOptions.TopLeft, new Color(0.66f, 0.76f, 0.69f));
            UiFactory.Place(_spotHint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -50f), new Vector2(MapWidth - 16f, 110f));

            TextMeshProUGUI hint = UiFactory.Label(w, "Hint", _mapFrame != null ? "Accept · make the product · find the face and name in the lounge" : "Accept · make the product · meet them at the lit spot while they wait", TermMuted, TextAlignmentOptions.Center);
            UiFactory.Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 22f), new Vector2(MapWidth + 20f, 50f));
            hint.textWrappingMode = TextWrappingModes.Normal;
            return canvas.gameObject;
        }
    }
}
