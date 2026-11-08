using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Interaction;
using VentsAndVending.Player.Appearance;

namespace VentsAndVending.UI
{
    /// <summary>
    /// Wardrobe (player personalisation): pick a slot, wear or remove owned items, choose a skin colour, with a live
    /// preview of yourself. Locked items show their price — buying arrives with clothing shops.
    /// </summary>
    public sealed class WardrobePanel : ModalScreen
    {
        private PlayerBody _body;
        private OutfitSlot _slot = OutfitSlot.Torso;
        private RectTransform _tabs;
        private RectTransform _items;
        private RectTransform _skins;
        private TextMeshProUGUI _slotTitle;
        private RawImage _preview;
        private Camera _previewCam;
        private Light _previewLight;
        private RenderTexture _rt;

        private void OnEnable() => WardrobeMirror.PanelRequested += OnRequested;

        private void OnDisable() => WardrobeMirror.PanelRequested -= OnRequested;

        private void OnRequested(WardrobeMirror mirror, InteractionController interactor)
        {
            if (interactor != Interactor || !BeginOpen())
            {
                return;
            }
            _body = interactor.GetComponentInChildren<PlayerBody>();
            if (_body == null)
            {
                Close();
                return;
            }
            _body.SetPreview(true);
            _body.State.Changed += Refresh;
            StartPreview(interactor.transform);
            Refresh();
        }

        protected override void OnClosing()
        {
            if (_body != null)
            {
                _body.State.Changed -= Refresh;
                _body.SetPreview(false);
            }
            if (_previewCam != null)
            {
                Destroy(_previewCam.gameObject);
            }
            if (_previewLight != null)
            {
                Destroy(_previewLight.gameObject);
            }
            if (_rt != null)
            {
                _rt.Release();
                _rt = null;
            }
        }

        private void StartPreview(Transform player)
        {
            _rt = new RenderTexture(640, 900, 24);
            var go = new GameObject("Wardrobe Preview Camera");
            _previewCam = go.AddComponent<Camera>();
            _previewCam.targetTexture = _rt;
            _previewCam.fieldOfView = 34f;
            _previewCam.nearClipPlane = 0.05f;
            _previewCam.clearFlags = CameraClearFlags.SolidColor;
            _previewCam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
            Vector3 front = player.forward;
            Vector3 focus = _body != null ? _body.transform.position : player.position; // the rig sits a little behind the capsule
            go.transform.position = focus + front * 3.3f + Vector3.up * 1.05f;
            go.transform.LookAt(focus + Vector3.up * 0.95f);
            _previewLight = new GameObject("Wardrobe Preview Light").AddComponent<Light>();
            _previewLight.type = LightType.Point;
            _previewLight.range = 4f;
            _previewLight.intensity = 2.2f;
            _previewLight.color = new Color(1f, 0.92f, 0.82f);
            _previewLight.transform.position = player.position + front * 1.4f + Vector3.up * 1.6f + player.right * 0.6f;
            _preview.texture = _rt;
        }

        // ------------------------------------------------------------------ Content

        private void Refresh()
        {
            if (_body == null)
            {
                return;
            }
            ClearChildren(_tabs);
            foreach (OutfitSlot s in Enum.GetValues(typeof(OutfitSlot)))
            {
                OutfitSlot slot = s;
                ClothingDefinition worn = _body.Find(_body.State.Equipped(slot));
                Button b = UiFactory.Button(_tabs, $"Tab {slot}", slot.ToString(), () => { _slot = slot; Refresh(); },
                    Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, slot == _slot ? ButtonStyle.Primary : ButtonStyle.Ghost);
                UiFactory.Layout(b.gameObject, preferredHeight: 62f);
                var label = b.GetComponentInChildren<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.rectTransform.offsetMin = new Vector2(18f, 4f);
                label.text = $"{slot}<size=65%>\n<color=#{(slot == _slot ? "4A3D10" : "9AA3AD")}>{(worn != null ? worn.DisplayName : "nothing")}</color></size>";
                label.textWrappingMode = TextWrappingModes.Normal;
            }

            _slotTitle.text = _slot.ToString();
            ClearChildren(_items);
            string equipped = _body.State.Equipped(_slot);

            // "Nothing" card
            ItemCard(null, equipped == null);
            foreach (ClothingDefinition c in _body.Catalogue)
            {
                if (c != null && c.Slot == _slot)
                {
                    ItemCard(c, equipped == c.Id);
                }
            }

            ClearChildren(_skins);
            for (int i = 0; i < PlayerBody.SkinPalette.Length; i++)
            {
                int index = i;
                Button sw = UiFactory.Button(_skins, $"Skin {i}", string.Empty, () => _body.State.SetSkin(index),
                    Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                sw.GetComponent<Image>().color = PlayerBody.SkinPalette[i];
                UiFactory.Layout(sw.gameObject, preferredHeight: 52f, preferredWidth: 52f);
                if (_body.State.SkinIndex == i)
                {
                    var ring = sw.gameObject.AddComponent<Outline>();
                    ring.effectColor = UiFactory.Hazard;
                    ring.effectDistance = new Vector2(3f, -3f);
                }
            }
        }

        private void ItemCard(ClothingDefinition item, bool wearing)
        {
            bool owned = item == null || _body.State.IsOwned(item.Id);
            Image card = UiFactory.Panel(_items, item != null ? item.Id : "None", wearing ? new Color(0.13f, 0.11f, 0.04f) : UiFactory.Row, 4f, 14f);
            if (wearing)
            {
                var ring = card.gameObject.AddComponent<Outline>();
                ring.effectColor = UiFactory.Hazard;
                ring.effectDistance = new Vector2(2f, -2f);
            }
            Transform c = card.transform;

            // Colour swatches
            Image a = UiFactory.Panel(c, "Primary", item != null ? item.Primary : UiFactory.Track, 0f, 10f);
            UiFactory.Place(a.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(44f, 44f));
            if (item != null)
            {
                Image b = UiFactory.Panel(c, "Secondary", item.Secondary, 0f, 6f);
                UiFactory.Place(b.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -42f), new Vector2(22f, 22f));
            }

            TextMeshProUGUI name = UiFactory.Heading(c, "Name", item != null ? item.DisplayName : "Nothing", 21f);
            UiFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, -14f), new Vector2(250f, 28f));
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            TextMeshProUGUI desc = UiFactory.Text(c, "Desc", item != null ? item.Description : "Go without.", 15f, TextAlignmentOptions.TopLeft, UiFactory.MutedText);
            UiFactory.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, -42f), new Vector2(250f, 42f));
            desc.overflowMode = TextOverflowModes.Ellipsis;

            if (!owned)
            {
                card.color = new Color(0.06f, 0.07f, 0.085f);
                UiFactory.Chip(c, "Price", $"{item.Price} CC · shops soon", UiFactory.Track, UiFactory.MutedText, 14f)
                    .rectTransform.anchoredPosition = Vector2.zero;
                var chipRt = c.Find("Price") as RectTransform;
                UiFactory.Place(chipRt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 12f), new Vector2(170f, 30f));
                return;
            }

            if (wearing)
            {
                Image tag = UiFactory.Chip(c, "Wearing", "WEARING", UiFactory.Hazard, new Color(0.08f, 0.075f, 0.05f), 14f);
                UiFactory.Place(tag.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 12f), new Vector2(110f, 30f));
            }

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            OutfitSlot slot = _slot;
            button.onClick.AddListener(() =>
            {
                if (item == null)
                {
                    _body.State.Unequip(slot);
                }
                else
                {
                    _body.State.Equip(slot, item.Id);
                }
            });
        }

        // ------------------------------------------------------------------ Layout

        protected override GameObject BuildUi()
        {
            Canvas canvas = UiFactory.CreateCanvas(transform, "Wardrobe Canvas", 13);
            UiFactory.Dim(canvas.transform, 0.6f);
            Image panel = UiFactory.Panel(canvas.transform, "Panel", null, 8f, 18f);
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560f, 880f));
            Transform p = panel.transform;

            TextMeshProUGUI title = UiFactory.Display(p, "Title", "WARDROBE", 44f);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -26f), new Vector2(600f, 56f));
            TextMeshProUGUI sub = UiFactory.Label(p, "Sub", "cracked mirror · den edition");
            UiFactory.Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(38f, -88f), new Vector2(500f, 24f));

            _tabs = UiFactory.Rect("Tabs", p);
            UiFactory.Place(_tabs, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -130f), new Vector2(240f, 620f));
            UiFactory.VerticalList(_tabs, 10f).childAlignment = TextAnchor.UpperLeft;

            _slotTitle = UiFactory.Heading(p, "SlotTitle", string.Empty, 26f);
            UiFactory.Place(_slotTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, -126f), new Vector2(500f, 36f));
            _items = UiFactory.Rect("Items", p);
            UiFactory.Place(_items, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, -172f), new Vector2(700f, 500f));
            var grid = _items.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(344f, 132f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            TextMeshProUGUI skinLabel = UiFactory.Label(p, "SkinLabel", "Skin");
            UiFactory.Place(skinLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(300f, 150f), new Vector2(200f, 24f));
            _skins = UiFactory.Rect("Skins", p);
            UiFactory.Place(_skins, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(300f, 84f), new Vector2(700f, 56f));
            UiFactory.HRow(_skins, 12f);

            Image frame = UiFactory.Panel(p, "PreviewFrame", new Color(0.04f, 0.05f, 0.06f), 0f, 16f);
            UiFactory.Place(frame.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -40f), new Vector2(500f, 720f));
            var rawGo = new GameObject("Preview", typeof(RectTransform), typeof(RawImage));
            rawGo.transform.SetParent(frame.transform, false);
            _preview = rawGo.GetComponent<RawImage>();
            UiFactory.Stretch(_preview.rectTransform, 8f);

            RectTransform hints = UiFactory.Rect("Hints", p);
            UiFactory.Place(hints, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 26f), new Vector2(900f, 34f));
            UiFactory.HRow(hints, 26f);
            UiFactory.KeyHint(hints, "Esc", "Done");
            TextMeshProUGUI note = UiFactory.Text(hints, "Note", "Locked items arrive with clothing shops", 17f, TextAlignmentOptions.Left, UiFactory.MutedText);
            note.textWrappingMode = TextWrappingModes.NoWrap;
            return canvas.gameObject;
        }
    }
}
