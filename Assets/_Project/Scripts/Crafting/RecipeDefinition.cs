using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VentsAndVending.Inventory;

namespace VentsAndVending.Crafting
{
    public enum CraftingEquipment
    {
        MixingStation,
        MolecularRefinery,
        StabilisationChamber,
        PackagingMachine
    }

    /// <summary>Result of checking a vessel's contents against a recipe.</summary>
    public readonly struct RecipeCheck
    {
        public readonly bool Matches;
        public readonly bool CorrectOrder;
        /// <summary>Human-readable reason it doesn't match yet (null when it matches).</summary>
        public readonly string Problem;

        public RecipeCheck(bool matches, bool correctOrder, string problem)
        {
            Matches = matches;
            CorrectOrder = correctOrder;
            Problem = problem;
        }
    }

    /// <summary>Data-driven recipe (GDD §7.4). Ingredient list order is the "correct production sequence".</summary>
    [CreateAssetMenu(menuName = "Vents & Vending/Recipe Definition", fileName = "Recipe_New")]
    public sealed class RecipeDefinition : ScriptableObject
    {
        [Serializable]
        public struct Ingredient
        {
            public ItemDefinition Item;
            [Min(1)] public int Quantity;
        }

        [SerializeField] private string _id;
        [SerializeField] private ProductDefinition _output;
        [SerializeField, Min(1)] private int _outputCount = 1;
        [Tooltip("In the correct order. Adding them in this order avoids a quality penalty.")]
        [SerializeField] private List<Ingredient> _ingredients = new List<Ingredient>();
        [SerializeField] private CraftingEquipment _equipment = CraftingEquipment.MixingStation;
        [Tooltip("Seconds for the mix to reach 100% (the end of the ideal window).")]
        [SerializeField, Min(0.5f)] private float _mixDuration = 6f;
        [SerializeField, Min(0f)] private float _settleDuration = 2.5f;

        public string Id => _id;
        public ProductDefinition Output => _output;
        public int OutputCount => _outputCount;
        public IReadOnlyList<Ingredient> Ingredients => _ingredients;
        public CraftingEquipment Equipment => _equipment;
        public float MixDuration => _mixDuration;
        public float SettleDuration => _settleDuration;
        public string DisplayName => _output != null ? _output.DisplayName : name;
        public int TotalIngredientCount => _ingredients.Sum(i => i.Quantity);

        /// <summary>"Glarb Extract → Lumina Gel → Stabiliser Gel" (quantities shown when above 1).</summary>
        public string SequenceText => string.Join(" → ",
            _ingredients.Select(i => i.Quantity > 1 ? $"{i.Item.DisplayName} x{i.Quantity}" : i.Item.DisplayName));

        public RecipeCheck Check(IReadOnlyList<ItemDefinition> loaded)
        {
            var have = new Dictionary<ItemDefinition, int>();
            foreach (ItemDefinition item in loaded)
            {
                have[item] = have.TryGetValue(item, out int n) ? n + 1 : 1;
            }

            foreach (var pair in have)
            {
                Ingredient? needed = FindIngredient(pair.Key);
                if (needed == null)
                {
                    return new RecipeCheck(false, false, $"{pair.Key.DisplayName} isn't part of {DisplayName}");
                }

                if (pair.Value > needed.Value.Quantity)
                {
                    return new RecipeCheck(false, false, $"Too much {pair.Key.DisplayName}");
                }
            }

            foreach (Ingredient ingredient in _ingredients)
            {
                int count = have.TryGetValue(ingredient.Item, out int n) ? n : 0;
                if (count < ingredient.Quantity)
                {
                    int missing = ingredient.Quantity - count;
                    return new RecipeCheck(false, false, missing > 1 ? $"Missing {ingredient.Item.DisplayName} x{missing}" : $"Missing {ingredient.Item.DisplayName}");
                }
            }

            return new RecipeCheck(true, IsCorrectOrder(loaded), null);
        }

        /// <summary>True when ingredients first appear in the recipe's listed order.</summary>
        public bool IsCorrectOrder(IReadOnlyList<ItemDefinition> loaded)
        {
            var firstSeen = new List<ItemDefinition>();
            foreach (ItemDefinition item in loaded)
            {
                if (!firstSeen.Contains(item))
                {
                    firstSeen.Add(item);
                }
            }

            int index = 0;
            foreach (Ingredient ingredient in _ingredients)
            {
                if (index >= firstSeen.Count || firstSeen[index] != ingredient.Item)
                {
                    return false;
                }
                index++;
            }

            return index == firstSeen.Count;
        }

        /// <summary>How many of the loaded items this recipe uses — for picking the closest recipe to describe.</summary>
        public int Overlap(IReadOnlyList<ItemDefinition> loaded) => loaded.Count(item => FindIngredient(item) != null);

        private Ingredient? FindIngredient(ItemDefinition item)
        {
            foreach (Ingredient ingredient in _ingredients)
            {
                if (ingredient.Item == item)
                {
                    return ingredient;
                }
            }
            return null;
        }

        internal void Configure(string id, ProductDefinition output, int outputCount, CraftingEquipment equipment,
            float mixDuration, float settleDuration, params (ItemDefinition item, int quantity)[] ingredients)
        {
            _id = id;
            _output = output;
            _outputCount = Mathf.Max(1, outputCount);
            _equipment = equipment;
            _mixDuration = Mathf.Max(0.5f, mixDuration);
            _settleDuration = Mathf.Max(0f, settleDuration);
            _ingredients = ingredients.Select(i => new Ingredient { Item = i.item, Quantity = Mathf.Max(1, i.quantity) }).ToList();
        }
    }
}
