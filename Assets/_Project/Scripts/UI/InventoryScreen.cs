using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;
using VentsAndVending.Player;

namespace VentsAndVending.UI
{
    /// <summary>
    /// Player inventory (Tab) and storage transfer view (GDD §17.3). Click a row to move the stack to the other side;
    /// with no container open, clicking drops the stack into the world.
    /// </summary>
    public sealed class InventoryScreen : MonoBehaviour
    {
        [SerializeField] private InteractionController _interactor;

        private GameObject _root;
        private GameObject _otherColumn;
        private RectTransform _playerList;
        private RectTransform _otherList;
        private TextMeshProUGUI _playerTitle;
        private TextMeshProUGUI _otherTitle;
        private TextMeshProUGUI _details;
        private TextMeshProUGUI _hint;

        private InventoryComponent _player;
        private InventoryComponent _other;
        private int _openedFrame = -1;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            if (_interactor == null)
            {
                _interactor = FindAnyObjectByType<InteractionController>();
            }

            Build();
            _root.SetActive(false);
        }

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

        private void Update()
        {
            if (_interactor == null)
            {
                return;
            }

            PlayerInputHandler input = _interactor.Input;
            if (!IsOpen)
            {
                if (input.InventoryPressed && !_interactor.State.IsInteracting)
                {
                    Open(null);
                }
                return;
            }

            if (Time.frameCount == _openedFrame)
            {
                return;
            }

            if (input.InventoryPressed || input.InteractPressed || input.CancelPressed)
            {
                Close();
            }
        }

        public void Open(InventoryComponent other)
        {
            if (IsOpen || _interactor.Inventory == null)
            {
                return;
            }

            _player = _interactor.Inventory;
            _other = other;
            _player.Model.Changed += Refresh;
            if (_other != null)
            {
                _other.Model.Changed += Refresh;
            }

            _root.SetActive(true);
            _otherColumn.SetActive(_other != null);
            _openedFrame = Time.frameCount;
            _interactor.State.IsInteracting = true;
            PlayerCamera.SetCursorLocked(false);
            _details.text = string.Empty;
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _player.Model.Changed -= Refresh;
            if (_other != null)
            {
                _other.Model.Changed -= Refresh;
            }

            _root.SetActive(false);
            _other = null;
            _interactor.State.IsInteracting = false;
            PlayerCamera.SetCursorLocked(true);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void Refresh()
        {
            Populate(_playerList, _player.Model, OnPlayerSlotClicked);
            _playerTitle.text = $"{_player.DisplayName}  <color=#9AA3AD><size=70%>{_player.Model.UsedSlots}/{_player.Model.Capacity}</size></color>";

            if (_other != null)
            {
                Populate(_otherList, _other.Model, OnOtherSlotClicked);
                _otherTitle.text = $"{_other.DisplayName}  <color=#9AA3AD><size=70%>{_other.Model.UsedSlots}/{_other.Model.Capacity}</size></color>";
                _hint.text = $"Click an item to move it   ·   [ Tab ] [ {_interactor.InteractKeyLabel} ] [ Esc ] Close";
            }
            else
            {
                _hint.text = "Click an item to drop it   ·   [ Tab ] [ Esc ] Close";
            }
        }

        private void Populate(RectTransform list, InventoryModel model, System.Action<int> onClick)
        {
            for (int i = list.childCount - 1; i >= 0; i--)
            {
                GameObject child = list.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

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
                    _interactor.Notify($"{_other.DisplayName} is full");
                }
                return;
            }

            Drop(index);
        }

        private void OnOtherSlotClicked(int index)
        {
            if (_other.Model.TransferSlotTo(index, _player.Model) == 0)
            {
                _interactor.Notify("Your pockets are full");
            }
        }

        private void Drop(int index)
        {
            ItemStack stack = _player.Model.TakeSlot(index);
            if (stack.IsEmpty)
            {
                return;
            }

            Transform view = _interactor.View;
            Vector3 position = view.position + view.forward * 0.7f;
            if (PickupItem.Spawn(stack.Item, stack.Count, position, Quaternion.identity) == null)
            {
                _player.Model.Add(stack.Item, stack.Count);
                _interactor.Notify($"{stack.Item.DisplayName} can't be dropped");
                return;
            }

            _interactor.Notify($"Dropped {stack.Item.DisplayName} x{stack.Count}");
        }

        private void ShowDetails(ItemStack stack)
        {
            if (stack.IsEmpty)
            {
                _details.text = string.Empty;
                return;
            }

            ItemDefinition item = stack.Item;
            _details.text = $"<b>{item.DisplayName}</b>  <color=#9AA3AD>{item.Category} · stack {item.MaxStack} · {item.BaseValue} CC</color>\n{item.Description}";
        }

        // ------------------------------------------------------------------ Layout

        private void Build()
        {
            Canvas canvas = UiFactory.CreateCanvas(transform, "Inventory Canvas", 10);
            _root = canvas.gameObject;

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
        }

        private static GameObject BuildColumn(Transform panel, string name, Vector2 topLeft, out TextMeshProUGUI title, out RectTransform list)
        {
            RectTransform column = UiFactory.Rect(name, panel);
            UiFactory.Place(column, new Vector2(0f, 1f), new Vector2(0f, 1f), topLeft, new Vector2(540f, 640f));

            title = UiFactory.Text(column, "Title", string.Empty, 28f);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(540f, 40f));
            title.fontStyle = FontStyles.Bold;

            list = UiFactory.Rect("List", column);
            UiFactory.Place(list, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -52f), new Vector2(540f, 580f));
            UiFactory.VerticalList(list, 6f).childAlignment = TextAnchor.UpperLeft;
            return column.gameObject;
        }
    }
}
