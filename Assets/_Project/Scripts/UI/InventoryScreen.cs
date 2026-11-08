using TMPro;
using UnityEngine;
using VentsAndVending.Crafting;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;

namespace VentsAndVending.UI
{
    /// <summary>
    /// Player inventory (Tab) and storage transfer view (GDD §17.3). Click a row to move the stack to the other side;
    /// with no container open, clicking drops the stack into the world.
    /// </summary>
    public sealed class InventoryScreen : ModalScreen
    {
        private GameObject _otherColumn;
        private RectTransform _playerList;
        private RectTransform _otherList;
        private TextMeshProUGUI _playerTitle;
        private TextMeshProUGUI _otherTitle;
        private TextMeshProUGUI _details;
        private TextMeshProUGUI _hint;

        private InventoryComponent _player;
        private InventoryComponent _other;

        private void OnEnable()
        {
            if (_interactor != null)
            {
                _interactor.InventoryViewRequested += Open;
            }
        }

        private void OnDisable()
        {
            if (_interactor != null)
            {
                _interactor.InventoryViewRequested -= Open;
            }
        }

        protected override void UpdateWhileClosed()
        {
            if (Interactor.Input.InventoryPressed)
            {
                Open(null);
            }
        }

        public void Open(InventoryComponent other)
        {
            if (Interactor.Inventory == null || !BeginOpen())
            {
                return;
            }

            _player = Interactor.Inventory;
            _other = other;
            _player.Model.Changed += Refresh;
            if (_other != null)
            {
                _other.Model.Changed += Refresh;
            }

            _otherColumn.SetActive(_other != null);
            _details.text = string.Empty;
            Refresh();
        }

        protected override void OnClosing()
        {
            _player.Model.Changed -= Refresh;
            if (_other != null)
            {
                _other.Model.Changed -= Refresh;
            }
            _other = null;
        }

        private void Refresh()
        {
            Populate(_playerList, _player.Model, OnPlayerSlotClicked);
            _playerTitle.text = Title(_player);

            if (_other != null)
            {
                Populate(_otherList, _other.Model, OnOtherSlotClicked);
                _otherTitle.text = Title(_other);
                _hint.text = $"Click an item to move it   ·   [ Tab ] [ {Interactor.InteractKeyLabel} ] [ Esc ] Close";
            }
            else
            {
                _hint.text = "Click an item to drop it   ·   [ Tab ] [ Esc ] Close";
            }
        }

        private static string Title(InventoryComponent inv) =>
            $"{inv.DisplayName}  <color=#9AA3AD><size=70%>{inv.Model.UsedSlots}/{inv.Model.Capacity}</size></color>";

        private void Populate(RectTransform list, InventoryModel model, System.Action<int> onClick)
        {
            ClearChildren(list);

            bool any = false;
            for (int i = 0; i < model.Capacity; i++)
            {
                ItemStack stack = model.Slots[i];
                if (stack.IsEmpty)
                {
                    continue;
                }

                int index = i;
                InventorySlotView.Create(list, stack, () => onClick(index), ShowDetails);
                any = true;
            }

            if (!any)
            {
                TextMeshProUGUI empty = UiFactory.Text(list, "Empty", "Empty", 20f, TextAlignmentOptions.Center, UiFactory.MutedText);
                UiFactory.Layout(empty.gameObject, preferredHeight: 44f);
            }
        }

        private void OnPlayerSlotClicked(int index)
        {
            if (_other != null)
            {
                if (_player.Model.TransferSlotTo(index, _other.Model) == 0)
                {
                    Interactor.Notify($"{_other.DisplayName} is full");
                }
                return;
            }

            Drop(index);
        }

        private void OnOtherSlotClicked(int index)
        {
            if (_other.Model.TransferSlotTo(index, _player.Model) == 0)
            {
                Interactor.Notify("Your pockets are full");
            }
        }

        private void Drop(int index)
        {
            ItemStack stack = _player.Model.TakeSlot(index);
            if (stack.IsEmpty)
            {
                return;
            }

            Transform view = Interactor.View;
            Vector3 position = view.position + view.forward * 0.7f;
            if (PickupItem.Spawn(stack.Item, stack.Count, stack.Quality, position, Quaternion.identity) == null)
            {
                _player.Model.Add(stack.Item, stack.Count, stack.Quality);
                Interactor.Notify($"{stack.Item.DisplayName} can't be dropped");
                return;
            }

            Interactor.Notify($"Dropped {stack}");
        }

        private void ShowDetails(ItemStack stack)
        {
            if (stack.IsEmpty)
            {
                _details.text = string.Empty;
                return;
            }

            ItemDefinition item = stack.Item;
            string value = item is ProductDefinition product
                ? $"sells ~{product.SalePriceFor(stack.Quality)} CC"
                : $"{item.BaseValue} CC";
            _details.text = $"<b>{QualityStyle.Name(stack)}</b>  <color=#9AA3AD>{item.Category} · stack {item.MaxStack} · {value}</color>\n{item.Description}";
        }

        // ------------------------------------------------------------------ Layout

        protected override GameObject BuildUi()
        {
            Canvas canvas = UiFactory.CreateCanvas(transform, "Inventory Canvas", 10);

            var dim = UiFactory.Image(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.45f));
            UiFactory.Stretch(dim.rectTransform);

            var panel = UiFactory.Image(canvas.transform, "Panel", UiFactory.PanelColor);
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 820f));

            var accent = UiFactory.Image(panel.transform, "AccentBar", UiFactory.Accent);
            UiFactory.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1180f, 4f));

            BuildColumn(panel.transform, "PlayerColumn", new Vector2(30f, -30f), out _playerTitle, out _playerList);
            _otherColumn = BuildColumn(panel.transform, "OtherColumn", new Vector2(610f, -30f), out _otherTitle, out _otherList);

            _details = UiFactory.Text(panel.transform, "Details", string.Empty, 20f, TextAlignmentOptions.TopLeft);
            UiFactory.Place(_details.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 60f), new Vector2(1120f, 90f));

            _hint = UiFactory.Text(panel.transform, "Hint", string.Empty, 18f, TextAlignmentOptions.Center, UiFactory.MutedText);
            UiFactory.Place(_hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1120f, 30f));

            return canvas.gameObject;
        }

        internal static GameObject BuildColumn(Transform panel, string name, Vector2 topLeft, out TextMeshProUGUI title, out RectTransform list,
            float height = 640f)
        {
            RectTransform column = UiFactory.Rect(name, panel);
            UiFactory.Place(column, new Vector2(0f, 1f), new Vector2(0f, 1f), topLeft, new Vector2(540f, height));

            title = UiFactory.Text(column, "Title", string.Empty, 28f);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(540f, 40f));
            title.fontStyle = FontStyles.Bold;

            list = UiFactory.Rect("List", column);
            UiFactory.Place(list, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -52f), new Vector2(540f, height - 60f));
            UiFactory.VerticalList(list, 6f).childAlignment = TextAnchor.UpperLeft;
            return column.gameObject;
        }
    }
}
