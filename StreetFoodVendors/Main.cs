using GTA;
using GTA.Native;
using StreetFoodVendors.Systems;
using StreetFoodVendors.UI;
using StreetFoodVendors.Util;
using LemonUI.Elements;
using LemonUI.Menus;
using LemonUI;
using System;
using System.Reflection;

namespace StreetFoodVendors
{
    public class Main : Script
    {
        // Systems
        private VendorSystem _vendorSystem;
        private VendorConsumeSystem _consumeSystem;

        // LemonUI Menu Pool (global)
        public static LemonUI.ObjectPool MenuPool = new ObjectPool();

        private static string ScriptVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public Main()
        {
            Interval = 1;
            Tick += OnTick;
            Aborted += OnScriptAbort;

            // Initialize systems
            _consumeSystem = new VendorConsumeSystem();
            _vendorSystem = new VendorSystem(_consumeSystem);

            ShowLoadedNotification();
        }

        private void ShowLoadedNotification()
        {
            GTA.UI.Notification.PostTicker($"~b~Street Food Vendors Enhanced v{ScriptVersion}~w~ loaded.", true);
        }

        // ---------------------------------------------------------
        // CLEANUP ON SCRIPT ABORT
        // ---------------------------------------------------------
        private void OnScriptAbort(object sender, EventArgs e)
        {
            // Cleanup LemonUI menus
            MenuPool.HideAll();
        }

        // ---------------------------------------------------------
        // MAIN TICK
        // ---------------------------------------------------------
        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                // Process LemonUI
                MenuPool.Process();

                // Vendor logic
                _vendorSystem.Tick();

                // Consumption logic
                _consumeSystem.Tick();
            }
            catch (Exception ex)
            {
                GTA.UI.Notification.Show($"StreetFoodVendors ERROR: {ex}");
            }
        }
    }
}
