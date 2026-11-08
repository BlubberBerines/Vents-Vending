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
            UiFactory.Layout(bg.gameObject, preferredHeight: 48f);

            var row = bg.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(10, 14, 6, 6);
            row.spacing = 12f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            Image swatch = UiFactory.Panel(bg.transform, "Swatch", stack.Item.UiColor, 0f, 8f);
            UiFactory.Layout(swatch.gameObject, preferredWidth: 30f);
            swatch.raycastTarget = false;
            if (stack.Item.Icon != null)
            {
                swatch.sprite = stack.Item.Icon;
                swatch.type = Image.Type.Simple;
                swatch.color = Color.white;
                swatch.preserveAspect = true;
            }

            TextMeshProUGUI name = UiFactory.Heading(bg.transform, "Name", stack.Item.DisplayName, 21f);
            UiFactory.Layout(name.gameObject, flexibleWidth: 1f);
            name.raycastTarget = false;

            var (qbg, qfg) = UiFactory.QualityColours(stack.Quality);
            Image chip = UiFactory.Chip(bg.transform, "Quality", stack.Quality.ToString(), qbg, qfg, 15f);
            chip.raycastTarget = false;

            TextMeshProUGUI count = UiFactory.Mono(bg.transform, "Count", $"×{stack.Count}", 20f, TextAlignmentOptions.Right, UiFactory.Paper);
            UiFactory.Layout(count.gameObject, preferredWidth: 56f);

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
