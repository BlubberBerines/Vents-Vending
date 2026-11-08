using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Crafting;
using VentsAndVending.Inventory;

namespace VentsAndVending.UI
{
    /// <summary>
    /// Crafting interface for the mixing station (GDD §17.4): pockets' ingredients on the left, the vessel on the
    /// right, recipe status, Start / Empty buttons and the known recipe cards. Mixing itself happens in the world.
    /// </summary>
    public sealed class MixingPanel : ModalScreen
    {
        private MixingStation _station;

        private TextMeshProUGUI _title;
        private TextMeshProUGUI _pocketsTitle;
        private TextMeshProUGUI _vesselTitle;
        private RectTransform _pocketsList;
        private RectTransform _vesselList;
        private TextMeshProUGUI _status;
        private TextMeshProUGUI _recipes;
        private Button _startButton;
        private Button _emptyButton;

        private void OnEnable() => MixingStation.PanelRequested += OnPanelRequested;

        private void OnDisable() => MixingStation.PanelRequested -= OnPanelRequested;

        private void OnPanelRequested(MixingStation station, Interaction.InteractionController interactor)
        {
            if (interactor != Interactor || !BeginOpen())
            {
                return;
            }

            _station = station;
            Interactor.Inventory.Model.Changed += Refresh;
            Refresh();
        }

        protected override void OnClosing()
        {
            Interactor.Inventory.Model.Changed -= Refresh;
            _station = null;
        }

        private void Refresh()
        {
            if (_station == null)
            {
                return;
            }

            MixingProcess process = _station.Process;
            _title.text = _station.DisplayName;

            // Pockets: ingredients only
            ClearChildren(_pocketsList);
            InventoryModel pockets = Interactor.Inventory.Model;
            bool anyIngredient = false;
            foreach (ItemStack stack in pockets.Slots)
            {
                if (stack.IsEmpty || stack.Item.Category != ItemCategory.Ingredient)
                {
                    continue;
                }

                ItemDefinition item = stack.Item;
                InventorySlotView.Create(_pocketsList, stack, () => Load(item), null);
                anyIngredient = true;
            }
            if (!anyIngredient)
            {
                AddMuted(_pocketsList, "No ingredients in your pockets");
            }
            _pocketsTitle.text = "Your ingredients";

            // Vessel: loading order matters
            ClearChildren(_vesselList);
            for (int i = 0; i < process.Loaded.Count; i++)
            {
                ItemDefinition item = process.Loaded[i];
                TextMeshProUGUI row = UiFactory.Text(_vesselList, $"Loaded {i}", $"<color=#9AA3AD>{i + 1}.</color>  {item.DisplayName}", 22f);
                UiFactory.Layout(row.gameObject, preferredHeight: 34f);
            }
            if (process.Loaded.Count == 0)
            {
                AddMuted(_vesselList, "Empty — click ingredients to add them, in order");
            }
            _vesselTitle.text = $"Vessel  <color=#9AA3AD><size=70%>{process.Loaded.Count}/{process.Capacity}</size></color>";

            // Recipe status
            RecipeDefinition match = _station.MatchedRecipe;
            if (match != null)
            {
                bool ordered = match.IsCorrectOrder(process.Loaded);
                _status.text = $"<color=#5CFF7A>Ready to mix: {match.Output.DisplayName} x{match.OutputCount}</color>" +
                               (ordered ? string.Empty : "\n<color=#FFB040>Ingredients added out of order — quality will suffer</color>");
            }
            else if (process.Loaded.Count == 0)
            {
                _status.text = "<color=#9AA3AD>Add ingredients to begin</color>";
            }
            else
            {
                RecipeDefinition closest = _station.ClosestRecipe;
                _status.text = closest != null
                    ? $"<color=#FFB040>{closest.Check(process.Loaded).Problem}</color>"
                    : "<color=#FF6060>No known recipe uses these ingredients</color>";
            }

            _startButton.interactable = match != null;
            _emptyButton.interactable = process.Loaded.Count > 0;

            _recipes.text = "<b>Known recipes</b>\n" + string.Join("\n", _station.Recipes.Where(r => r != null)
                .Select(r => $"<color=#19E5FF>{r.Output.DisplayName}</color>  <color=#9AA3AD>{r.SequenceText}</color>"));
        }

        private void Load(ItemDefinition item)
        {
            if (_station.TryLoad(Interactor, item))
            {
                Refresh(); // inventory Changed also refreshes; vessel changes don't raise it
            }
        }

        private void StartMixing()
        {
            MixingStation station = _station;
            if (station == null || !station.TryStartMixing(Interactor))
            {
                return;
            }

            Close();
            Interactor.Notify($"Mixing… stop it in the <color=#5CFF7A>green</color> with [ {Interactor.InteractKeyLabel} ]");
        }

        private void EmptyVessel()
        {
            _station.EmptyVessel(Interactor);
            Refresh();
        }

        private static void AddMuted(Transform list, string text)
        {
            TextMeshProUGUI t = UiFactory.Text(list, "Muted", text, 20f, TextAlignmentOptions.Left, UiFactory.MutedText);
            UiFactory.Layout(t.gameObject, preferredHeight: 34f);
        }

        // ------------------------------------------------------------------ Layout

        protected override GameObject BuildUi()
        {
            Canvas canvas = UiFactory.CreateCanvas(transform, "Mixing Canvas", 11);

            var dim = UiFactory.Image(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.45f));
            UiFactory.Stretch(dim.rectTransform);

            var panel = UiFactory.Image(canvas.transform, "Panel", UiFactory.PanelColor);
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 860f));
            var accent = UiFactory.Image(panel.transform, "AccentBar", UiFactory.Accent);
            UiFactory.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1180f, 4f));

            _title = UiFactory.Text(panel.transform, "Title", string.Empty, 34f);
            UiFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -22f), new Vector2(1100f, 46f));
            _title.fontStyle = FontStyles.Bold;

            InventoryScreen.BuildColumn(panel.transform, "Pockets", new Vector2(30f, -86f), out _pocketsTitle, out _pocketsList, 420f);
            InventoryScreen.BuildColumn(panel.transform, "Vessel", new Vector2(610f, -86f), out _vesselTitle, out _vesselList, 420f);

            _status = UiFactory.Text(panel.transform, "Status", string.Empty, 24f, TextAlignmentOptions.TopLeft);
            UiFactory.Place(_status.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -520f), new Vector2(760f, 70f));

            _startButton = UiFactory.Button(panel.transform, "StartButton", "Start mixing", StartMixing,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -520f), new Vector2(330f, 54f));
            _emptyButton = UiFactory.Button(panel.transform, "EmptyButton", "Empty vessel", EmptyVessel,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -584f), new Vector2(330f, 44f));

            _recipes = UiFactory.Text(panel.transform, "Recipes", string.Empty, 20f, TextAlignmentOptions.TopLeft);
            UiFactory.Place(_recipes.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -650f), new Vector2(1120f, 120f));

            TextMeshProUGUI hint = UiFactory.Text(panel.transform, "Hint",
                "Click an ingredient to add one   ·   Order matters   ·   Stop the mixer in the green for Premium   ·   [ E ] [ Esc ] Close",
                18f, TextAlignmentOptions.Center, UiFactory.MutedText);
            UiFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1120f, 30f));

            return canvas.gameObject;
        }
    }
}
