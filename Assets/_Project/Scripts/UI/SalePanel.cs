using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Customers;
using VentsAndVending.Economy;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;

namespace VentsAndVending.UI
{
    /// <summary>
    /// Sales interaction (GDD §10.2): see the request, pick which quality to sell, set your price, make an offer.
    /// Fast and compact — routine sales shouldn't need long dialogue.
    /// </summary>
    public sealed class SalePanel : ModalScreen
    {
        private Customer _customer;
        private SaleNegotiation _negotiation;
        private PlayerWallet _wallet;
        private bool _resolved;
        private ItemQuality? _selectedQuality;
        private int _unitPrice;

        private TextMeshProUGUI _title;
        private TextMeshProUGUI _request;
        private TextMeshProUGUI _reply;
        private TextMeshProUGUI _stockTitle;
        private RectTransform _stockList;
        private TextMeshProUGUI _price;
        private TextMeshProUGUI _total;
        private Image _riskFill;
        private TextMeshProUGUI _riskLabel;
        private Button _offerButton;
        private TextMeshProUGUI _offerLabel;
        private TextMeshProUGUI _declineLabel;
        private TextMeshProUGUI _hint;
        private readonly List<GameObject> _priceControls = new List<GameObject>();
        private Orders.CustomerOrder _order;

        private void OnEnable() => Customer.TradeRequested += OnTradeRequested;

        private void OnDisable() => Customer.TradeRequested -= OnTradeRequested;

        private void OnTradeRequested(Customer customer, InteractionController interactor)
        {
            if (interactor != Interactor || !BeginOpen())
            {
                customer.EndTrade(TradeOutcome.Cancelled, null);
                return;
            }

            _customer = customer;
            _negotiation = customer.CreateNegotiation();
            _wallet = Interactor.GetComponent<PlayerWallet>();
            SetDealing(true);
            _resolved = false;
            _selectedQuality = BestSellableQuality();
            _unitPrice = _selectedQuality.HasValue ? customer.FairUnitPrice(_selectedQuality.Value) : customer.FairUnitPrice(ItemQuality.Standard);

            _order = customer.Order;
            bool deal = _order != null;
            foreach (GameObject control in _priceControls)
            {
                control.SetActive(!deal);
            }
            _offerLabel.text = deal ? "Hand over" : "Make offer";
            _declineLabel.text = deal ? "Not yet" : "Walk away";
            _hint.text = deal
                ? "VentNet deal: the price is agreed · better quality than asked pays more   ·   [ Esc ] Step back (they keep waiting until the window closes)"
                : "Pick a quality · set your price · higher markups risk a walk-out   ·   [ Esc ] Step back (they'll keep waiting)";
            if (deal && _selectedQuality.HasValue)
            {
                _unitPrice = Orders.OrderManager.Instance != null ? Orders.OrderManager.Instance.UnitPayout(_order, _selectedQuality.Value) : _unitPrice;
            }
            _title.text = $"{customer.DisplayName} <color=#9AA3AD><size=70%>the {customer.Definition.SpeciesName}</size></color>";
            _reply.text = $"<i>“{customer.Line(customer.Definition.Lines.Greeting)}”</i>";
            Refresh();
        }

        /// <summary>Dealing in view of security is the most suspicious thing you can do (GDD §11.2).</summary>
        private void SetDealing(bool dealing)
        {
            var exposure = Interactor.GetComponent<Security.PlayerExposure>();
            if (exposure != null)
            {
                exposure.IsDealing = dealing;
            }
        }

        protected override void OnClosing()
        {
            SetDealing(false);
            if (!_resolved && _customer != null)
            {
                _customer.EndTrade(TradeOutcome.Cancelled, null);
            }
            _customer = null;
            _negotiation = null;
        }

        private ItemQuality? BestSellableQuality()
        {
            ItemQuality? best = null;
            InventoryModel pockets = Interactor.Inventory.Model;
            for (var q = ItemQuality.Exceptional; q >= ItemQuality.Poor; q--)
            {
                if (pockets.Count(_customer.WantedProduct, q) >= _customer.WantedQuantity)
                {
                    if (_negotiation.AcceptsQuality(q))
                    {
                        return q;
                    }
                    best ??= q;
                }
            }
            return best;
        }

        private void Refresh()
        {
            if (_customer == null)
            {
                return;
            }

            var product = _customer.WantedProduct;
            int qty = _customer.WantedQuantity;
            int fair = _customer.FairUnitPrice(_selectedQuality ?? ItemQuality.Standard);
            if (_order != null)
            {
                _request.text = $"Deal {_order.Id}: <b>{product.DisplayName} x{qty}</b>, {_order.MinimumQuality} or better   <color=#9AA3AD>· agreed {UnitPayoutFor(_order.MinimumQuality)} CC each at {_order.MinimumQuality}</color>";
            }
            else
            {
                _request.text = $"Wants <b>{product.DisplayName} x{qty}</b>   <color=#9AA3AD>· fair price ~{fair} CC each · offers left: {_negotiation.AttemptsLeft}</color>";
            }

            // Stock rows per quality
            ClearChildren(_stockList);
            InventoryModel pockets = Interactor.Inventory.Model;
            bool any = false;
            for (var q = ItemQuality.Exceptional; q >= ItemQuality.Poor; q--)
            {
                int have = pockets.Count(product, q);
                if (have == 0)
                {
                    continue;
                }

                any = true;
                ItemQuality quality = q;
                var stack = new ItemStack(product, have, q);
                var row = InventorySlotView.Create(_stockList, stack, () => Select(quality), null);
                if (_selectedQuality == q)
                {
                    row.GetComponent<Image>().color = new Color(0.1f, 0.9f, 1f, 0.3f);
                }
            }

            if (!any)
            {
                TextMeshProUGUI none = UiFactory.Text(_stockList, "None", $"You don't have any {product.DisplayName}. Go make some!", 20f, TextAlignmentOptions.Left, UiFactory.MutedText);
                UiFactory.Layout(none.gameObject, preferredHeight: 40f);
            }
            _stockTitle.text = "Your stock";

            bool enough = _selectedQuality.HasValue && pockets.Count(product, _selectedQuality.Value) >= qty;
            _price.text = $"{_unitPrice} CC";
            _total.text = enough
                ? $"Total: <b>{_unitPrice * qty} CC</b>  <color=#9AA3AD>for {qty} × {_selectedQuality}</color>"
                : $"<color=#FF8060>You need {qty} of one quality to sell</color>";
            if (_order != null && enough && !_negotiation.AcceptsQuality(_selectedQuality.Value))
            {
                _total.text = $"<color=#FF8060>They asked for {_order.MinimumQuality} or better</color>";
                enough = false;
            }
            _offerButton.interactable = enough && !_negotiation.IsClosed;

            if (_order == null && _riskFill != null)
            {
                // How far over fair you are, against this customer's (hidden) tolerance: a readable hint, not the exact limit
                int fairNow = _customer.FairUnitPrice(_selectedQuality ?? ItemQuality.Standard);
                float over = fairNow > 0 ? (_unitPrice / (float)fairNow - 1f) : 0f;
                float risk = Mathf.Clamp01(over / Mathf.Max(0.05f, _customer.PriceTolerance * 1.15f));
                UiFactory.SetBar(_riskFill, Mathf.Max(0.04f, risk));
                _riskFill.color = risk < 0.5f ? UiFactory.Go : risk < 0.85f ? UiFactory.Hazard : UiFactory.Alert;
                _riskLabel.text = $"Walk-out risk  <b><color=#{ColorUtility.ToHtmlStringRGB(_riskFill.color)}>{(risk < 0.5f ? "safe" : risk < 0.85f ? "pushing it" : "greedy")}</color></b>";
            }
        }

        private void Select(ItemQuality quality)
        {
            _selectedQuality = quality;
            _unitPrice = _order != null ? UnitPayoutFor(quality) : _customer.FairUnitPrice(quality);
            Refresh();
        }

        private void AdjustPrice(int delta)
        {
            _unitPrice = Mathf.Max(1, _unitPrice + delta);
            Refresh();
        }

        private void SetMarkup(float markup)
        {
            _unitPrice = Mathf.Max(1, Mathf.RoundToInt(_customer.FairUnitPrice(_selectedQuality ?? ItemQuality.Standard) * (1f + markup)));
            Refresh();
        }

        private void MakeOffer()
        {
            if (_customer == null || !_selectedQuality.HasValue)
            {
                return;
            }

            ItemQuality quality = _selectedQuality.Value;
            var lines = _customer.Definition.Lines;
            if (_order != null)
            {
                HandOver(quality, lines);
                return;
            }
            OfferResult result = _negotiation.Offer(_unitPrice, quality, _customer.FairUnitPrice(quality));

            switch (result.Outcome)
            {
                case OfferOutcome.Accepted:
                    Interactor.Inventory.Model.Remove(_customer.WantedProduct, _customer.WantedQuantity, quality);
                    if (_wallet != null)
                    {
                        _wallet.Wallet.Add(result.Total);
                    }
                    _resolved = true;
                    _customer.EndTrade(TradeOutcome.Sold, _customer.Line(lines.Accept, _unitPrice));
                    Interactor.Notify($"Sold {_customer.WantedQuantity} × {_customer.WantedProduct.DisplayName} for <color=#19E5FF>+{result.Total} CC</color>");
                    Close();
                    break;

                case OfferOutcome.TooExpensive:
                    _customer.ShowAnnoyed();
                    _reply.text = $"<i>“{_customer.Line(lines.TooExpensive, _unitPrice, result.CounterOffer)}”</i>";
                    _unitPrice = result.CounterOffer;
                    Refresh();
                    break;

                case OfferOutcome.QualityRejected:
                    _customer.ShowAnnoyed();
                    _reply.text = $"<i>“{_customer.Line(lines.QualityRejected, _unitPrice)}”</i>";
                    Refresh();
                    break;

                case OfferOutcome.WalkedAway:
                    _resolved = true;
                    _customer.EndTrade(TradeOutcome.WalkedAway, _customer.Line(lines.WalkAway, _unitPrice));
                    Interactor.Notify($"{_customer.DisplayName} walked away");
                    Close();
                    break;
            }
        }

        private int UnitPayoutFor(ItemQuality quality) =>
            Orders.OrderManager.Instance != null ? Orders.OrderManager.Instance.UnitPayout(_order, quality) : _customer.FairUnitPrice(quality);

        /// <summary>VentNet deal: no haggling, pay what was agreed for the quality delivered.</summary>
        private void HandOver(ItemQuality quality, CustomerDefinition.DialogueLines lines)
        {
            if (!_negotiation.AcceptsQuality(quality) || Interactor.Inventory.Model.Count(_customer.WantedProduct, quality) < _customer.WantedQuantity)
            {
                _customer.ShowAnnoyed();
                _reply.text = $"<i>“{_customer.Line(lines.QualityRejected)}”</i>";
                return;
            }

            int unit = UnitPayoutFor(quality);
            int total = unit * _customer.WantedQuantity;
            Interactor.Inventory.Model.Remove(_customer.WantedProduct, _customer.WantedQuantity, quality);
            if (_wallet != null)
            {
                _wallet.Wallet.Add(total);
            }
            _resolved = true;
            Orders.OrderManager.Instance?.CompleteHandoff(_order, total);
            _customer.EndTrade(TradeOutcome.Sold, _customer.Line(lines.Accept, unit));
            Interactor.Notify($"Deal done: {_customer.WantedQuantity} × {_customer.WantedProduct.DisplayName} for <color=#19E5FF>+{total} CC</color>  <color=#9AA3AD>(trust +{Orders.OrderRules.TrustForCompleting})</color>");
            Close();
        }

        private void Decline()
        {
            if (_order != null)
            {
                Close(); // step back; they keep waiting until the window closes
                return;
            }
            if (_customer != null)
            {
                _resolved = true;
                _customer.EndTrade(TradeOutcome.WalkedAway, _customer.Line(_customer.Definition.Lines.WalkAway));
            }
            Close();
        }

        // ------------------------------------------------------------------ Layout

        protected override GameObject BuildUi()
        {
            Canvas canvas = UiFactory.CreateCanvas(transform, "Sale Canvas", 12);

            UiFactory.Dim(canvas.transform, 0.35f);

            var panel = UiFactory.Panel(canvas.transform, "Panel");
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1060f, 700f));

            _title = UiFactory.Heading(panel.transform, "Title", string.Empty, 34f);
            UiFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(1000f, 46f));
            _title.fontStyle = FontStyles.Bold;

            _reply = UiFactory.Text(panel.transform, "Reply", string.Empty, 24f, TextAlignmentOptions.TopLeft, new Color(1f, 0.85f, 0.95f));
            UiFactory.Place(_reply.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -72f), new Vector2(1000f, 64f));

            _request = UiFactory.Text(panel.transform, "Request", string.Empty, 22f);
            UiFactory.Place(_request.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -144f), new Vector2(1000f, 34f));

            InventoryScreen.BuildColumn(panel.transform, "Stock", new Vector2(30f, -192f), out _stockTitle, out _stockList, 300f);

            // Price controls (right side)
            var priceLabel = UiFactory.Text(panel.transform, "PriceLabel", "Your price (each)", 22f, TextAlignmentOptions.Center, UiFactory.MutedText);
            UiFactory.Place(priceLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -192f), new Vector2(420f, 32f));

            _price = UiFactory.Mono(panel.transform, "Price", string.Empty, 56f, TextAlignmentOptions.Center, UiFactory.Credit);
            UiFactory.Place(_price.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -230f), new Vector2(420f, 64f));
            _price.fontStyle = FontStyles.Bold;

            _priceControls.Clear();
            _priceControls.Add(priceLabel.gameObject);
            _priceControls.Add(_price.gameObject);
            float bx = -30f;
            _priceControls.Add(UiFactory.Button(panel.transform, "Plus5", "+5", () => AdjustPrice(5), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx, -306f), new Vector2(96f, 44f)).gameObject);
            _priceControls.Add(UiFactory.Button(panel.transform, "Plus1", "+1", () => AdjustPrice(1), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx - 108f, -306f), new Vector2(96f, 44f)).gameObject);
            _priceControls.Add(UiFactory.Button(panel.transform, "Minus1", "-1", () => AdjustPrice(-1), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx - 216f, -306f), new Vector2(96f, 44f)).gameObject);
            _priceControls.Add(UiFactory.Button(panel.transform, "Minus5", "-5", () => AdjustPrice(-5), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx - 324f, -306f), new Vector2(96f, 44f)).gameObject);

            _priceControls.Add(UiFactory.Button(panel.transform, "Fair", "Fair", () => SetMarkup(0f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx - 288f, -362f), new Vector2(132f, 40f)).gameObject);
            _priceControls.Add(UiFactory.Button(panel.transform, "Markup20", "+20%", () => SetMarkup(0.2f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx - 144f, -362f), new Vector2(132f, 40f)).gameObject);
            _priceControls.Add(UiFactory.Button(panel.transform, "Markup40", "+40%", () => SetMarkup(0.4f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx, -362f), new Vector2(132f, 40f)).gameObject);

            _riskLabel = UiFactory.Text(panel.transform, "RiskLabel", string.Empty, 18f, TextAlignmentOptions.Left, new Color(0.79f, 0.82f, 0.85f));
            UiFactory.Place(_riskLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -462f), new Vector2(420f, 26f));
            _riskFill = UiFactory.Bar(panel.transform, "Risk", 0f, UiFactory.Hazard);
            UiFactory.Place((RectTransform)_riskFill.transform.parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -494f), new Vector2(420f, 14f));
            _priceControls.Add(_riskLabel.gameObject);
            _priceControls.Add(_riskFill.transform.parent.gameObject);

            _total = UiFactory.Text(panel.transform, "Total", string.Empty, 22f, TextAlignmentOptions.Center);
            UiFactory.Place(_total.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -410f), new Vector2(420f, 34f));

            _offerButton = UiFactory.Button(panel.transform, "OfferButton", "Make offer", MakeOffer,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 70f), new Vector2(420f, 60f), ButtonStyle.Primary);
            _offerLabel = _offerButton.GetComponentInChildren<TextMeshProUGUI>();
            _declineLabel = UiFactory.Button(panel.transform, "DeclineButton", "Walk away", Decline,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 70f), new Vector2(240f, 48f)).GetComponentInChildren<TextMeshProUGUI>();

            TextMeshProUGUI hint = _hint = UiFactory.Text(panel.transform, "Hint",
                "Pick a quality · set your price · higher markups risk a walk-out   ·   [ Esc ] Step back (they'll keep waiting)",
                18f, TextAlignmentOptions.Center, UiFactory.MutedText);
            UiFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1000f, 30f));

            return canvas.gameObject;
        }
    }
}
