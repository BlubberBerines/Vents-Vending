using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VentsAndVending.Inventory;

namespace VentsAndVending.UI
{
    /// <summary>One clickable row in an inventory list.</summary>
    internal sealed class InventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private Image _background;
        private Action _onClick;
        private Action<ItemStack> _onHover;
        private ItemStack _stack;

        public static InventorySlotView Create(Transform parent, ItemStack stack, Action onClick, Action<ItemStack> onHover)
        {
            Image bg = UiFactory.Image(parent, $"Slot {stack.Item.DisplayName}", UiFactory.RowColor);
            UiFactory.Layout(bg.gameObject, preferredHeight: 44f);

            var row = bg.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(10, 14, 6, 6);
            row.spacing = 12f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            Image swatch = UiFactory.Image(bg.transform, "Swatch", stack.Item.UiColor);
            UiFactory.Layout(swatch.gameObject, preferredWidth: 28f);
            swatch.raycastTarget = false;
            if (stack.Item.Icon != null)
            {
                swatch.sprite = stack.Item.Icon;
                swatch.color = Color.white;
                swatch.preserveAspect = true;
            }

            TextMeshProUGUI name = UiFactory.Text(bg.transform, "Name", QualityStyle.Name(stack), 22f);
            UiFactory.Layout(name.gameObject, flexibleWidth: 1f);

            TextMeshProUGUI count = UiFactory.Text(bg.transform, "Count", $"x{stack.Count}", 22f, TextAlignmentOptions.Right, UiFactory.Accent);
            UiFactory.Layout(count.gameObject, preferredWidth: 70f);

            var view = bg.gameObject.AddComponent<InventorySlotView>();
            view._background = bg;
            view._onClick = onClick;
            view._onHover = onHover;
            view._stack = stack;
            return view;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _background.color = UiFactory.RowHoverColor;
            _onHover?.Invoke(_stack);
        }

        public void OnPointerExit(PointerEventData eventData) => _background.color = UiFactory.RowColor;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _onClick?.Invoke();
            }
        }
    }
}
