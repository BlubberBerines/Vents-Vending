using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Economy;
using VentsAndVending.Interaction;
using VentsAndVending.Progression;

namespace VentsAndVending.UI
{
    /// <summary>
    /// Upgrade shop (GDD §5.2, §12.3) as a card grid: category tabs, one card per upgrade with its tier, what it does,
    /// and a clear state — installed, locked behind a prerequisite, too expensive, or ready to buy.
    /// </summary>
    public sealed class UpgradePanel : ModalScreen
    {
        private PlayerWallet _wallet;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _credits;
        private TextMeshProUGUI _progress;
        private RectTransform _grid;
        private RectTransform _tabs;
        private UpgradeCategory? _filter;

        private void OnEnable() => UpgradeTerminal.PanelRequested += OnPanelRequested;

        private void OnDisable() => UpgradeTerminal.PanelRequested -= OnPanelRequested;

        private void OnPanelRequested(UpgradeTerminal terminal, InteractionController interactor)
        {
            if (interactor != Interactor || ProgressionManager.Instance == null || !BeginOpen())
            {
                return;
            }

            _wallet = Interactor.GetComponent<PlayerWallet>();
            _title.text = terminal.DisplayName.ToUpperInvariant();
            if (_wallet != null)
            {
                _wallet.Wallet.Changed += OnCreditsChanged;
            }
            Refresh();
        }

        protected override void OnClosing()
        {
            if (_wallet != null)
            {
                _wallet.Wallet.Changed -= OnCreditsChanged;
            }
        }

        private void OnCreditsChanged(int balance, int delta) => Refresh();

        public static Color CategoryColour(UpgradeCategory c) => c switch
        {
            UpgradeCategory.Hideout => UiFactory.Hazard,
            UpgradeCategory.Storage => UiFactory.Credit,
            UpgradeCategory.Production => UiFactory.Go,
            UpgradeCategory.Security => UiFactory.Alert,
            _ => new Color(0.75f, 0.6f, 1f),
        };

        private void Refresh()
        {
            ProgressionManager progression = ProgressionManager.Instance;
            Wallet wallet = _wallet != null ? _wallet.Wallet : null;
            _credits.text = $"{wallet?.Balance ?? 0} <size=60%><color=#3FE0FF>CC</color></size>";

            List<UpgradeDefinition> all = progression.Catalogue.Where(u => u != null).OrderBy(u => u.Tier).ThenBy(u => u.Cost).ToList();
            int owned = all.Count(u => progression.Check(u, wallet) == PurchaseCheck.AlreadyOwned);
            _progress.text = $"{owned} / {all.Count} installed";

            BuildTabs(all);
            ClearChildren(_grid);
            foreach (UpgradeDefinition upgrade in all)
            {
                if (_filter.HasValue && upgrade.Category != _filter.Value)
                {
                    continue;
                }
                BuildCard(upgrade, progression.Check(upgrade, wallet));
            }
        }

        private void BuildTabs(List<UpgradeDefinition> all)
        {
            ClearChildren(_tabs);
            Tab("All", null);
            foreach (UpgradeCategory c in all.Select(u => u.Category).Distinct().OrderBy(c => c))
            {
                Tab(c.ToString(), c);
            }
        }

        private void Tab(string label, UpgradeCategory? category)
        {
            bool on = _filter == category;
            Button b = UiFactory.Button(_tabs, $"Tab {label}", label, () => { _filter = category; Refresh(); },
                Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(150f, 44f), on ? ButtonStyle.Primary : ButtonStyle.Ghost);
            UiFactory.Layout(b.gameObject, preferredHeight: 44f, preferredWidth: 150f);
        }

        private void BuildCard(UpgradeDefinition upgrade, PurchaseCheck check)
        {
            bool installed = check == PurchaseCheck.AlreadyOwned;
            Image card = UiFactory.Panel(_grid, upgrade.Id, installed ? new Color(0.06f, 0.11f, 0.08f, 1f) : UiFactory.Row, 5f, 16f);
            Transform c = card.transform;
            Color cat = CategoryColour(upgrade.Category);

            // Category strip + tier pips
            Image strip = UiFactory.Image(c, "Strip", cat, true);
            UiFactory.Place(strip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(6f, 26f));
            TextMeshProUGUI category = UiFactory.Label(c, "Category", upgrade.Category.ToString(), cat);
            UiFactory.Place(category.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -19f), new Vector2(240f, 24f));
            RectTransform pips = UiFactory.Rect("Tier", c);
            UiFactory.Place(pips, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(120f, 18f));
            UiFactory.HRow(pips, 5f, TextAnchor.MiddleRight);
            for (int i = 0; i < 3; i++)
            {
                Image pip = UiFactory.Panel(pips, "Pip", i < upgrade.Tier ? cat : UiFactory.Track, 0f, 4f);
                UiFactory.Layout(pip.gameObject, preferredHeight: 14f, preferredWidth: 22f);
            }

            TextMeshProUGUI name = UiFactory.Heading(c, "Name", upgrade.DisplayName, 26f);
            UiFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -52f), new Vector2(440f, 34f));
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI desc = UiFactory.Text(c, "Description", upgrade.Description, 17f, TextAlignmentOptions.TopLeft, new Color(0.79f, 0.82f, 0.85f));
            UiFactory.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -92f), new Vector2(440f, 52f));
            desc.overflowMode = TextOverflowModes.Ellipsis;

            // Footer: state
            switch (check)
            {
                case PurchaseCheck.AlreadyOwned:
                    Image chip = UiFactory.Chip(c, "Installed", "INSTALLED", UiFactory.Go, new Color(0.03f, 0.075f, 0.047f), 15f);
                    UiFactory.Place(chip.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(120f, 32f));
                    break;
                case PurchaseCheck.MissingPrerequisite:
                    TextMeshProUGUI needs = UiFactory.Text(c, "Needs", $"Needs <b>{upgrade.Prerequisite.DisplayName}</b> first", 17f, TextAlignmentOptions.Left, new Color(1f, 0.69f, 0.31f));
                    UiFactory.Place(needs.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 22f), new Vector2(300f, 40f));
                    TextMeshProUGUI locked = UiFactory.Mono(c, "Cost", $"{upgrade.Cost} <size=65%>CC</size>", 26f, TextAlignmentOptions.Right, UiFactory.MutedText);
                    UiFactory.Place(locked.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 22f), new Vector2(160f, 40f));
                    break;
                default:
                    Price(c, upgrade.Cost, check == PurchaseCheck.CannotAfford ? UiFactory.Alert : UiFactory.Credit);
                    Button buy = UiFactory.Button(c, "Buy", check == PurchaseCheck.CannotAfford ? "Can't afford" : "Buy", () => Buy(upgrade),
                        new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 18f), new Vector2(160f, 48f),
                        check == PurchaseCheck.Available ? ButtonStyle.Primary : ButtonStyle.Ghost);
                    buy.interactable = check == PurchaseCheck.Available;
                    break;
            }
        }

        private static void Price(Transform card, int cost, Color colour)
        {
            TextMeshProUGUI price = UiFactory.Mono(card, "Cost", $"{cost} <size=65%>CC</size>", 30f, TextAlignmentOptions.Left, colour);
            UiFactory.Place(price.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 20f), new Vector2(200f, 48f));
        }

        private void Buy(UpgradeDefinition upgrade)
        {
            if (ProgressionManager.Instance.TryPurchase(upgrade, _wallet != null ? _wallet.Wallet : null))
            {
                Interactor.Notify($"Upgrade installed: <color=#5CFF8A>{upgrade.DisplayName}</color>");
            }
            Refresh();
        }

        protected override GameObject BuildUi()
        {
            Canvas canvas = UiFactory.CreateCanvas(transform, "Upgrade Canvas", 13);
            UiFactory.Dim(canvas.transform, 0.6f);

            Image panel = UiFactory.Panel(canvas.transform, "Panel", null, 8f, 18f);
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560f, 880f));
            Transform p = panel.transform;

            _title = UiFactory.Display(p, "Title", "UPGRADES", 44f);
            UiFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -26f), new Vector2(700f, 56f));
            _progress = UiFactory.Label(p, "Progress", string.Empty);
            UiFactory.Place(_progress.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(38f, -88f), new Vector2(400f, 24f));

            Image creditsBox = UiFactory.Panel(p, "Credits", UiFactory.Row, 4f, 12f);
            UiFactory.Place(creditsBox.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -30f), new Vector2(220f, 60f));
            _credits = UiFactory.Mono(creditsBox.transform, "Amount", string.Empty, 32f, TextAlignmentOptions.Right);
            UiFactory.Stretch(_credits.rectTransform, 10f);
            _credits.rectTransform.offsetMax = new Vector2(-18f, -6f);

            _tabs = UiFactory.Rect("Tabs", p);
            UiFactory.Place(_tabs, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -126f), new Vector2(1000f, 48f));
            UiFactory.HRow(_tabs, 10f);

            _grid = UiFactory.Rect("Grid", p);
            UiFactory.Place(_grid, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(1488f, 620f));
            var grid = _grid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(480f, 210f);
            grid.spacing = new Vector2(24f, 22f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            RectTransform hints = UiFactory.Rect("Hints", p);
            UiFactory.Place(hints, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(800f, 34f));
            UiFactory.HRow(hints, 28f, TextAnchor.MiddleCenter);
            UiFactory.KeyHint(hints, "Esc", "Close");
            TextMeshProUGUI earn = UiFactory.Text(hints, "Earn", "Earn Cosmic Credits from deals and sales", 18f, TextAlignmentOptions.Left, UiFactory.MutedText);
            earn.textWrappingMode = TextWrappingModes.NoWrap;
            return canvas.gameObject;
        }
    }
}
