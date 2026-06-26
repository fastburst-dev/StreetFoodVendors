using System.Collections.Generic;

namespace StreetFoodVendors.Data
{
    /// <summary>
    /// Static database of all items sold by vendors and shops.
    /// </summary>
    internal static class FoodItemDatabase
    {
        public static readonly Dictionary<string, FoodItemData> Items = new Dictionary<string, FoodItemData>
        {
            // Snacks
            { "ps_and_qs", new FoodItemData("ps_and_qs", "P's & Q's", 1, FoodItemCategory.Snack, "Small candy snack. Restores a little health.") },
            { "egochaser", new FoodItemData("egochaser", "EgoChaser", 2, FoodItemCategory.Snack, "Energy bar. Restores some health.") },
            { "meteorite", new FoodItemData("meteorite", "Meteorite", 4, FoodItemCategory.Snack, "Chocolate bar. Restores moderate health.") },
            { "donut", new FoodItemData("donut", "Donut", 2, FoodItemCategory.Snack, "Donut. Restores moderate health.") },
            { "sandwich", new FoodItemData("sandwich", "Sandwich", 5, FoodItemCategory.Snack, "Half Sandwich. Restores moderate health.") },
            { "taco", new FoodItemData("taco", "Taco", 5, FoodItemCategory.Snack, "Taco. Restores moderate health.") },
            { "hotdog", new FoodItemData("hotdog", "Hotdog", 5, FoodItemCategory.Snack, "Hotdog. Restores moderate health.") },
            { "burger", new FoodItemData("burger", "Hamburger", 7, FoodItemCategory.Snack, "Hamburger. Restores moderate health.") },

            // Drinks
            { "coffee", new FoodItemData("coffee", "Coffee", 3, FoodItemCategory.Drink, "Coffee cup. Restores 15% health.") },
            { "juice01", new FoodItemData("juice01", "Juice", 2, FoodItemCategory.Drink, "Juice drink. Restores 15% health.") },
            { "sprunk", new FoodItemData("sprunk", "Sprunk", 1, FoodItemCategory.Drink, "Sprunk soda. Restores 15% health.") },
            { "e_colas", new FoodItemData("e_colas", "eCola", 1, FoodItemCategory.Drink, "Classic cola drink. Restores 15% health.") },
            { "beer1", new FoodItemData("beer1", "Bottle of Beer (PiBwasser)", 5, FoodItemCategory.Drink, "Bottle of beer. Restores 15% health.") },
            { "beer2", new FoodItemData("beer2", "Bottle of Beer (Logger)", 5, FoodItemCategory.Drink, "Bottle of beer. Restores 15% health.") },
            { "beer40", new FoodItemData("beer40", "40oz Bottle of Beer", 9, FoodItemCategory.Drink, "40oz bottle of beer. Restores 15% health.") },
            { "whiskey", new FoodItemData("whiskey", "Bottle of Whiskey", 20, FoodItemCategory.Drink, "Bottle of whiskey. Restores 15% health.") }
        };
    }
}
