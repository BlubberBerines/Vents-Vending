using UnityEngine;
using VentsAndVending.Inventory;

namespace VentsAndVending.UI
{
    internal static class QualityStyle
    {
        public static string Hex(ItemQuality quality) => quality switch
        {
            ItemQuality.Poor => "#C0705A",
            ItemQuality.Premium => "#19E5FF",
            ItemQuality.Exceptional => "#FFC83D",
            _ => "#C8CDD2"
        };

        public static Color Color(ItemQuality quality) =>
            ColorUtility.TryParseHtmlString(Hex(quality), out Color c) ? c : UnityEngine.Color.white;

        /// <summary>Display name with a coloured quality tag for products / non-standard items.</summary>
        public static string Name(ItemStack stack)
        {
            bool showQuality = stack.Item.Category == ItemCategory.Product || stack.Quality != ItemQuality.Standard;
            return showQuality
                ? $"{stack.Item.DisplayName} <color={Hex(stack.Quality)}><size=80%>{stack.Quality}</size></color>"
                : stack.Item.DisplayName;
        }
    }
}
