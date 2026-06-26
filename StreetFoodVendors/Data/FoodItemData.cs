using System;

namespace StreetFoodVendors.Data
{
    /// <summary>
    /// Defines a purchasable food or drink item.
    /// </summary>
    internal class FoodItemData
    {
        public string Id { get; }
        public string Name { get; }
        public int Price { get; }
        public FoodItemCategory Category { get; }
        public string Description { get; }

        public FoodItemData(string id, string name, int price, FoodItemCategory category, string description)
        {
            Id = id;
            Name = name;
            Price = price;
            Category = category;
            Description = description;
        }
    }

    /// <summary>
    /// Categories for food items.
    /// </summary>
    internal enum FoodItemCategory
    {
        Snack,
        Drink,
        Utility,
        Medical,
        Other
    }
}
