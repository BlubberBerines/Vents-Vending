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
        private Button _offerButton;

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
            _request.text = $"Wants <b>{product.DisplayName} x{qty}</b>   <color=#9AA3AD>· fair price ~{fair} CC each · offers left: {_negotiation.AttemptsLeft}</color>";

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
            _offerButton.interactable = enough && !_negotiation.IsClosed;
        }

        private void Select(ItemQuality quality)
        {
            _selectedQuality = quality;
            _unitPrice = _customer.FairUnitPrice(quality);
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

        private void Decline()
        {
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

            var dim = UiFactory.Image(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.35f));
            UiFactory.Stretch(dim.rectTransform);

            var panel = UiFactory.Image(canvas.transform, "Panel", UiFactory.PanelColor);
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1060f, 700f));
            var accent = UiFactory.Image(panel.transform, "AccentBar", new Color(1f, 0.3f, 0.85f));
            UiFactory.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1060f, 4f));

            _title = UiFactory.Text(panel.transform, "Title", string.Empty, 34f);
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

            _price = UiFactory.Text(panel.transform, "Price", string.Empty, 48f, TextAlignmentOptions.Center, UiFactory.Accent);
            UiFactory.Place(_price.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -230f), new Vector2(420f, 64f));
            _price.fontStyle = FontStyles.Bold;

            float bx = -30f;
            UiFactory.Button(panel.transform, "Plus5", "+5", () => AdjustPrice(5), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx, -306f), new Vector2(96f, 44f));
            UiFactory.Button(panel.transform, "Plus1", "+1", () => AdjustPrice(1), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx - 108f, -306f), new Vector2(96f, 44f));
            UiFactory.Button(panel.transform, "Minus1", "-1", () => AdjustPrice(-1), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx - 216f, -306f), new Vector2(96f, 44f));
            UiFactory.Button(panel.transform, "Minus5", "-5", () => AdjustPrice(-5), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx - 324f, -306f), new Vector2(96f, 44f));

            UiFactory.Button(panel.transform, "Fair", "Fair", () => SetMarkup(0f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx - 288f, -362f), new Vector2(132f, 40f));
            UiFactory.Button(panel.transform, "Markup20", "+20%", () => SetMarkup(0.2f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx - 144f, -362f), new Vector2(132f, 40f));
            UiFactory.Button(panel.transform, "Markup40", "+40%", () => SetMarkup(0.4f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(bx, -362f), new Vector2(132f, 40f));

            _total = UiFactory.Text(panel.transform, "Total", string.Empty, 22f, TextAlignmentOptions.Center);
            UiFactory.Place(_total.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -420f), new Vector2(420f, 34f));

            _offerButton = UiFactory.Button(panel.transform, "OfferButton", "Make offer", MakeOffer,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 70f), new Vector2(420f, 60f));
            UiFactory.Button(panel.transform, "DeclineButton", "Walk away", Decline,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 70f), new Vector2(240f, 48f));

            TextMeshProUGUI hint = UiFactory.Text(panel.transform, "Hint",
                "Pick a quality · set your price · higher markups risk a walk-out   ·   [ Esc ] Step back (they'll keep waiting)",
                18f, TextAlignmentOptions.Center, UiFactory.MutedText);
            UiFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1000f, 30f));

            return canvas.gameObject;
        }
    }
}
