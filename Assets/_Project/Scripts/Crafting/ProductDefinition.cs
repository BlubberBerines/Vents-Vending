using UnityEngine;
using VentsAndVending.Inventory;

namespace VentsAndVending.Crafting
{
    public enum ProductDifficulty
    {
        Beginner,
        BeginnerIntermediate,
        Intermediate,
        Advanced,
        Expert
    }

    public enum DemandLevel
    {
        Low,
        LowMedium,
        Medium,
        MediumHigh,
        High,
        Variable,
        Premium
    }

    /// <summary>A sellable product (GDD §7.3). Economy values are the §12.4 starting targets.</summary>
    [CreateAssetMenu(menuName = "Vents & Vending/Product Definition", fileName = "Product_New")]
    public sealed class ProductDefinition : ItemDefinition
    {
        [Header("Product")]
        [SerializeField] private string _productCategory;
        [SerializeField] private ProductDifficulty _difficulty;
        [SerializeField] private DemandLevel _demand;
        [Tooltip("Production cost target in Cosmic Credits (GDD §12.4).")]
        [SerializeField, Min(0)] private int _productionCost;
        [Tooltip("Standard-quality sale price in Cosmic Credits (GDD §12.4).")]
        [SerializeField, Min(0)] private int _standardSalePrice;

        public string ProductCategory => _productCategory;
        public ProductDifficulty Difficulty => _difficulty;
        public DemandLevel Demand => _demand;
        public int ProductionCost => _productionCost;
        public int StandardSalePrice => _standardSalePrice;

        /// <summary>Sale-price multiplier per quality. Starting values — balance in playtests.</summary>
        public static float QualityMultiplier(ItemQuality quality) => quality switch
        {
            ItemQuality.Poor => 0.6f,
            ItemQuality.Premium => 1.4f,
            ItemQuality.Exceptional => 2.0f,
            _ => 1f
        };

        public int SalePriceFor(ItemQuality quality) => Mathf.RoundToInt(_standardSalePrice * QualityMultiplier(quality));

        internal void ConfigureProduct(string productCategory, ProductDifficulty difficulty, DemandLevel demand,
            int productionCost, int standardSalePrice)
        {
            _productCategory = productCategory;
            _difficulty = difficulty;
            _demand = demand;
            _productionCost = Mathf.Max(0, productionCost);
            _standardSalePrice = Mathf.Max(0, standardSalePrice);
        }
    }
}
