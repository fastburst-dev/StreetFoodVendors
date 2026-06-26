using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using StreetFoodVendors.UI;
using StreetFoodVendors.Util;

namespace StreetFoodVendors.Systems
{
    internal class VendorSystem
    {
        private readonly VendorConsumeSystem _consumeSystem;

        // Vendor model
        private readonly Model VendorModel = new Model("S_M_M_StrVend_01");

        // Stand models
        private readonly Model[] HotdogStandModels =
        {
            new Model("prop_hotdogstand_01"),
            new Model("prop_hotdogstand_02")
        };

        private readonly Model[] BurgerStandModels =
        {
            new Model("prop_burgerstand_01"),
            new Model("prop_burgerstand_02")
        };

        // Vendor tracking: standHandle → (vendorPed, vendorType)
        public readonly Dictionary<int, (Ped ped, string type)> _vendors =
            new Dictionary<int, (Ped, string)>();

        // Active menu
        private VendorMenuUI _activeMenu;

        public VendorSystem(VendorConsumeSystem consumeSystem)
        {
            _consumeSystem = consumeSystem;
        }

        // ------------------------------------------------------------
        // MAIN TICK
        // ------------------------------------------------------------
        public void Tick()
        {
            try
            {
                SpawnVendorsIfMissing();
                HandleVendorInteraction();
            }
            catch (Exception ex)
            {
                GTA.UI.Notification.Show($"VendorSystem.Tick ERROR: {ex}");
            }
        }

        // ------------------------------------------------------------
        // SPAWN VENDORS AT STANDS (RELOAD-SAFE)
        // ------------------------------------------------------------
        private void SpawnVendorsIfMissing()
        {
            Prop[] props = World.GetAllProps();

            foreach (Prop stand in props)
            {
                if (!stand.Exists())
                    continue;

                string vendorType = GetVendorTypeFromStand(stand);
                if (vendorType == null)
                    continue;

                int key = stand.Handle;

                // If we already have a vendor tracked AND it exists, skip
                if (_vendors.TryGetValue(key, out var existing) &&
                    existing.ped.Exists() && existing.ped.IsAlive)
                {
                    continue;
                }

                // If old vendor exists but dead or invalid, delete it
                if (_vendors.TryGetValue(key, out var oldVendor) &&
                    oldVendor.ped.Exists())
                {
                    oldVendor.ped.Delete();
                }

                // 🔍 NEW: Check for an existing vendor ped near the stand
                Ped[] nearbyPeds = World.GetNearbyPeds(stand.Position, 2.0f);
                foreach (Ped p in nearbyPeds)
                {
                    if (p.Model == VendorModel)
                    {
                        // Adopt this vendor instead of spawning a new one
                        p.IsPersistent = true;
                        p.BlockPermanentEvents = true;

                        _vendors[key] = (p, vendorType);
                        goto NextStand;
                    }
                }

                // No vendor found → spawn a new one
                Vector3 forward = stand.ForwardVector;
                if (forward.Length() < 0.1f)
                    forward = stand.Quaternion * Vector3.RelativeFront;

                Vector3 spawnPos = stand.Position + forward * 1.0f;
                float heading = stand.Heading + 180f;

                Ped vendor = World.CreatePed(VendorModel, spawnPos);
                if (vendor.Exists())
                {
                    vendor.Heading = heading;
                    vendor.Task.StandStill(-1);
                    vendor.IsPersistent = true;
                    vendor.BlockPermanentEvents = true;

                    _vendors[key] = (vendor, vendorType);
                }

            NextStand:;
            }
        }

        // ------------------------------------------------------------
        // DETERMINE VENDOR TYPE FROM STAND MODEL
        // ------------------------------------------------------------
        private string GetVendorTypeFromStand(Prop stand)
        {
            foreach (var m in HotdogStandModels)
                if (stand.Model == m) return "Hotdog Stand";

            foreach (var m in BurgerStandModels)
                if (stand.Model == m) return "Burger Stand";

            return null;
        }

        // ------------------------------------------------------------
        // INTERACTION HANDLING
        // ------------------------------------------------------------
        private void HandleVendorInteraction()
        {
            Ped player = Game.Player.Character;
            if (!player.Exists() || !player.IsOnFoot)
                return;

            foreach (var kvp in _vendors)
            {
                Ped vendor = kvp.Value.ped;
                string vendorType = kvp.Value.type;

                if (!vendor.Exists() || !vendor.IsAlive)
                    continue;

                float dist = player.Position.DistanceTo(vendor.Position);
                if (dist > 2.0f)
                    continue;

                // Show prompt
                Utils.DisplayHelpTextThisFrame($"Press ~INPUT_CONTEXT~ to buy from {vendorType}");

                // Open menu
                if (Game.IsControlJustPressed(Control.Context))
                {
                    vendor.PlayAmbientSpeech("GENERIC_HI", false, null);
                    player.TaskPlayAnim("misscarsteal4@vendor", "base_customer1", 0, -1);
                    // Pass vendor to consume system
                    _consumeSystem.SetActiveVendor(vendor);

                    _activeMenu = new VendorMenuUI(vendorType, _consumeSystem);
                    _activeMenu.Show();
                }

                return;
            }
        }
    }
}
