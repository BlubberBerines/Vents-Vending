using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Economy;
using VentsAndVending.Interaction;
using VentsAndVending.Progression;

namespace VentsAndVending.UI
{
    /// <summary>Upgrade shop opened from an <see cref="UpgradeTerminal"/> (GDD §5.2, §12.3).</summary>
    public sealed class UpgradePanel : ModalScreen
    {
        private PlayerWallet _wallet;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _credits;
        private RectTransform _list;

        private void OnEnable() => UpgradeTerminal.PanelRequested += OnPanelRequested;

        private void OnDisable() => UpgradeTerminal.PanelRequested -= OnPanelRequested;

        private void OnPanelRequested(UpgradeTerminal terminal, InteractionController interactor)
        {
            if (interactor != Interactor || ProgressionManager.Instance == null || !BeginOpen())
            {
                return;
            }

            _wallet = Interactor.GetComponent<PlayerWallet>();
            _title.text = terminal.DisplayName;
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

        private void Refresh()
        {
            ProgressionManager progression = ProgressionManager.Instance;
            Wallet wallet = _wallet != null ? _wallet.Wallet : null;
            _credits.text = $"{wallet?.Balance ?? 0} <color=#19E5FF><size=70%>CC</size></color>";

            ClearChildren(_list);
            foreach (UpgradeDefinition upgrade in progression.Catalogue.Where(u => u != null).OrderBy(u => u.Tier).ThenBy(u => u.Cost))
            {
                PurchaseCheck check = progression.Check(upgrade, wallet);
                BuildRow(upgrade, check);
            }
        }

        private void BuildRow(UpgradeDefinition upgrade, PurchaseCheck check)
        {
            Image row = UiFactory.Image(_list, upgrade.Id, check == PurchaseCheck.AlreadyOwned ? new Color(0.2f, 1f, 0.4f, 0.07f) : UiFactory.RowColor);
            UiFactory.Layout(row.gameObject, preferredHeight: 86f);

            TextMeshProUGUI name = UiFactory.Text(row.transform, "Name",
                $"<b>{upgrade.DisplayName}</b>  <color=#9AA3AD><size=75%>{upgrade.Category} · Tier {upgrade.Tier}</size></color>", 24f);
            UiFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -8f), new Vector2(760f, 32f));

            string description = upgrade.Description;
            if (check == PurchaseCheck.MissingPrerequisite)
            {
                description = $"<color=#FFB040>Requires {upgrade.Prerequisite.DisplayName}.</color> {description}";
            }
            TextMeshProUGUI desc = UiFactory.Text(row.transform, "Description", description, 18f, TextAlignmentOptions.TopLeft, UiFactory.MutedText);
            UiFactory.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -40f), new Vector2(760f, 44f));

            string costColor = check == PurchaseCheck.CannotAfford ? "#FF7060" : "#19E5FF";
            TextMeshProUGUI cost = UiFactory.Text(row.transform, "Cost",
                check == PurchaseCheck.AlreadyOwned ? "<color=#5CFF7A>OWNED</color>" : $"<color={costColor}>{upgrade.Cost} CC</color>",
                24f, TextAlignmentOptions.Right);
            UiFactory.Place(cost.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-196f, 0f), new Vector2(140f, 40f));
            cost.fontStyle = FontStyles.Bold;

            if (check != PurchaseCheck.AlreadyOwned)
            {
                Button buy = UiFactory.Button(row.transform, "BuyButton", "Buy", () => Buy(upgrade),
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(160f, 50f));
                buy.interactable = check == PurchaseCheck.Available;
            }
        }

        private void Buy(UpgradeDefinition upgrade)
        {
            if (ProgressionManager.Instance.TryPurchase(upgrade, _wallet != null ? _wallet.Wallet : null))
            {
                Interactor.Notify($"Upgrade installed: <color=#5CFF7A>{upgrade.DisplayName}</color>");
            }
            Refresh();
        }

        protected override GameObject BuildUi()
        {
            Canvas canvas = UiFactory.CreateCanvas(transform, "Upgrade Canvas", 13);

            var dim = UiFactory.Image(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.45f));
            UiFactory.Stretch(dim.rectTransform);

            var panel = UiFactory.Image(canvas.transform, "Panel", UiFactory.PanelColor);
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 860f));
            var accent = UiFactory.Image(panel.transform, "AccentBar", new Color(0.35f, 1f, 0.45f));
            UiFactory.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1180f, 4f));

            _title = UiFactory.Text(panel.transform, "Title", string.Empty, 34f);
            UiFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -22f), new Vector2(700f, 46f));
            _title.fontStyle = FontStyles.Bold;

            _credits = UiFactory.Text(panel.transform, "Credits", string.Empty, 32f, TextAlignmentOptions.Right);
            UiFactory.Place(_credits.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -22f), new Vector2(300f, 46f));
            _credits.fontStyle = FontStyles.Bold;

            _list = UiFactory.Rect("List", panel.transform);
            UiFactory.Place(_list, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1120f, 700f));
            UiFactory.VerticalList(_list, 8f).childAlignment = TextAnchor.UpperCenter;

            TextMeshProUGUI hint = UiFactory.Text(panel.transform, "Hint", "Earn Cosmic Credits by selling products   ·   [ E ] [ Esc ] Close",
                18f, TextAlignmentOptions.Center, UiFactory.MutedText);
            UiFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1120f, 30f));

            return canvas.gameObject;
        }
    }
}
