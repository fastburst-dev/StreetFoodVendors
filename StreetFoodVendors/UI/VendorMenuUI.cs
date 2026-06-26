using GTA;
using LemonUI.Elements;
using LemonUI.Menus;
using LemonUI;
using StreetFoodVendors.Data;
using StreetFoodVendors.Systems;
using System;
using System.Drawing;
using System.Reflection;
using System.Security.Policy;

namespace StreetFoodVendors.UI
{
    internal class VendorMenuUI
    {
        private readonly NativeMenu _menu;
        private readonly VendorConsumeSystem _consumeSystem;

        public NativeMenu Menu => _menu;

        public VendorMenuUI(string vendorType, VendorConsumeSystem consumeSystem)
        {
            _consumeSystem = consumeSystem;

            // Title removed for clean look, subtitle is vendor type
            _menu = new NativeMenu("", vendorType);

            // Add to LemonUI pool
            Main.MenuPool.Add(_menu);

            // Apply banner
            _menu.Banner = new ScaledTexture(
                new PointF(0f, 0f),
                new SizeF(512f, 128f),
                GetBannerDict(vendorType),
                GetBannerName(vendorType)
            );

            BuildMenu(vendorType);
        }

        // ------------------------------------------------------------
        // BANNERS
        // ------------------------------------------------------------
        private string GetBannerDict(string vendorType)
        {
            // Using Rockstar's convenience store banner as a placeholder
            return "shopui_title_conveniencestore";
        }

        private string GetBannerName(string vendorType)
        {
            return "shopui_title_conveniencestore";
        }

        // ------------------------------------------------------------
        // BUILD MENU BASED ON VENDOR TYPE
        // ------------------------------------------------------------
        private void BuildMenu(string vendorType)
        {
            try
            {
                foreach (var item in FoodItemDatabase.Items.Values)
                {
                    // Filter items based on vendor type
                    if (!IsItemAllowedForVendor(vendorType, item.Id))
                        continue;

                    var menuItem = new NativeItem(item.Name, item.Description)
                    {
                        AltTitle = $"${item.Price}"
                    };

                    menuItem.Activated += (sender, args) =>
                    {
                        if (Game.Player.Money < item.Price)
                        {
                            GTA.UI.Screen.ShowSubtitle("~r~Not enough money.");
                            return;
                        }

                        Game.Player.Money -= item.Price;

                        _consumeSystem.QueueItem(item.Id);

                        GTA.UI.Screen.ShowSubtitle(
                            $"Purchased ~g~{item.Name}~w~ for ~g~${item.Price}"
                        );

                        _menu.Visible = false;
                    };

                    _menu.Add(menuItem);
                }
            }
            catch (Exception ex)
            {
                GTA.UI.Notification.Show($"VendorMenuUI.BuildMenu ERROR: {ex}");
            }
        }

        // ------------------------------------------------------------
        // FILTER ITEMS PER VENDOR TYPE
        // ------------------------------------------------------------
        private bool IsItemAllowedForVendor(string vendorType, string itemId)
        {
            vendorType = vendorType.ToLower();

            // Hotdog stand: allow everything except burgers
            if (vendorType.Contains("hotdog"))
                return itemId != "burger";

            // Burger stand: allow everything except hotdogs
            if (vendorType.Contains("burger"))
                return itemId != "hotdog";

            return true;
        }

        // ------------------------------------------------------------
        // SHOW / HIDE
        // ------------------------------------------------------------
        public void Show()
        {
            _menu.Visible = true;
        }

        public void Hide()
        {
            _menu.Visible = false;
        }
    }
}
