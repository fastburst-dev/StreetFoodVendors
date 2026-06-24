using GTA;
using GTA.Math;
using GTA.Native;
using StreetFoodVendors.Util;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace StreetFoodVendors
{
    public class Main : Script
    {
        // -----------------------------
        // STAND KEY (STABLE IDENTIFIER)
        // -----------------------------
        private struct StandKey
        {
            public int Model;
            public Vector3 Position;

            public StandKey(int model, Vector3 pos)
            {
                Model = model;
                Position = new Vector3(
                    (float)Math.Round(pos.X, 1),
                    (float)Math.Round(pos.Y, 1),
                    (float)Math.Round(pos.Z, 1)
                );
            }

            public override int GetHashCode()
            {
                return Model.GetHashCode() ^ Position.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (!(obj is StandKey)) return false;
                StandKey other = (StandKey)obj;
                return Model == other.Model && Position == other.Position;
            }
        }

        // -----------------------------
        // MESSAGES
        // -----------------------------
        private string MsgHotdog = "Press ~INPUT_CONTEXT~ to buy a hot dog for $5.";
        private string MsgHamburger = "Press ~INPUT_CONTEXT~ to buy a hamburger for $7.";
        private string MsgWanted = "Unavailable. Lose your wanted level.";
        private string MsgMoney = "Unavailable. Not enough money.";

        // -----------------------------
        // MODELS
        // -----------------------------
        private readonly Model VendorModel = new Model("S_M_M_StrVend_01");

        private readonly Model[] HotdogStandModels =
        {
            new Model("prop_hotdogstand_01"),
            new Model("prop_hotdogstand_02")
        };

        private readonly Model[] HamburgerStandModels =
        {
            new Model("prop_burgerstand_01"),
            new Model("prop_burgerstand_02")
        };

        private readonly Dictionary<StandKey, Ped> StandVendors = new Dictionary<StandKey, Ped>();
        private bool initializedCleanup = false;
        private Prop FoodProp;
        private static string ScriptVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public void ShowLoadedNotification()
        {
            GTA.UI.Notification.PostTicker($"~b~Street Food Vendors Enhanced v{ScriptVersion}~w~ is loaded.", true);
        }

        public Main()
        {
            Tick += OnTick;
            Interval = 1;

            // Build version notification
            ShowLoadedNotification();

            this.Aborted += OnScriptAbort;
        }

        // Cleanup on script reload
        private void OnScriptAbort(object sender, EventArgs e)
        {
            foreach (var kvp in StandVendors)
            {
                Ped vendor = kvp.Value;
                if (vendor != null && vendor.Exists())
                    vendor.Delete();
            }

            StandVendors.Clear();
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!initializedCleanup)
            {
                CleanupLeftoverVendorsFromGameLoad();
                initializedCleanup = true;
            }

            SpawnVendorsIfMissing();
            HandlePurchases();
        }

        // ---------------------------------------------------------
        // REMOVE OLD VENDORS FROM PREVIOUS GAME SESSION
        // ---------------------------------------------------------
        private void CleanupLeftoverVendorsFromGameLoad()
        {
            Ped[] allPeds = World.GetAllPeds();

            foreach (Ped ped in allPeds)
            {
                if (ped.Exists() && ped.Model == VendorModel)
                {
                    ped.Delete();
                }
            }

            StandVendors.Clear();
        }

        // ---------------------------------------------------------
        // 1. AUTO-SPAWN VENDORS FOR HOTDOG + HAMBURGER STANDS
        // ---------------------------------------------------------
        private void SpawnVendorsIfMissing()
        {
            Prop[] props = World.GetAllProps();

            foreach (Prop stand in props)
            {
                if (!stand.Exists())
                    continue;

                bool isHotdogStand = false;
                bool isHamburgerStand = false;

                foreach (var m in HotdogStandModels)
                    if (stand.Model == m) isHotdogStand = true;

                foreach (var m in HamburgerStandModels)
                    if (stand.Model == m) isHamburgerStand = true;

                if (!isHotdogStand && !isHamburgerStand)
                    continue;

                StandKey key = new StandKey(stand.Model.Hash, stand.Position);

                Ped vendor = null;
                StandVendors.TryGetValue(key, out vendor);

                bool vendorValid =
                    vendor != null &&
                    vendor.Exists() &&
                    vendor.IsAlive &&
                    vendor.Position.DistanceTo(stand.Position) < 3f;

                if (!vendorValid)
                {
                    if (vendor != null && vendor.Exists())
                        vendor.Delete();

                    // Correct spawn position: behind the stand
                    Vector3 forward = stand.ForwardVector;

                    if (forward == null || forward.Length() < 0.1f)
                        forward = stand.Quaternion * Vector3.RelativeFront; // fallback

                    Vector3 spawnPos = stand.Position + forward * 1.0f;

                    float heading = stand.Heading + 180f;

                    Ped newVendor = World.CreatePed(VendorModel, spawnPos);

                    if (newVendor.Exists())
                    {
                        newVendor.Heading = heading;
                        newVendor.Task.StandStill(-1);
                        newVendor.IsPersistent = true;
                        newVendor.BlockPermanentEvents = true;

                        StandVendors[key] = newVendor;
                    }
                }
            }
        }

        // ---------------------------------------------------------
        // 2. PURCHASE HOTDOG OR HAMBURGER
        // ---------------------------------------------------------
        private void HandlePurchases()
        {
            Ped player = Game.Player.Character;

            if (!player.Exists() || !player.IsOnFoot)
                return;

            Ped[] nearby = World.GetNearbyPeds(player.Position, 2f);

            foreach (Ped ped in nearby)
            {
                if (!ped.Exists() || !ped.IsAlive)
                    continue;

                if (ped.Model != VendorModel)
                    continue;

                bool nearHotdogStand = false;
                bool nearHamburgerStand = false;

                Prop[] props = World.GetNearbyProps(ped.Position, 2f);

                foreach (Prop p in props)
                {
                    foreach (var m in HotdogStandModels)
                        if (p.Model == m) nearHotdogStand = true;

                    foreach (var m in HamburgerStandModels)
                        if (p.Model == m) nearHamburgerStand = true;
                }

                if (!nearHotdogStand && !nearHamburgerStand)
                    continue;

                if (nearHotdogStand)
                    Utils.DisplayHelpTextThisFrame(MsgHotdog);
                else if (nearHamburgerStand)
                    Utils.DisplayHelpTextThisFrame(MsgHamburger);

                if (!Game.IsControlJustPressed(Control.Context))
                {
                    ped.PlaySpeech("GENERIC_HI");
                    continue;
                }

                if (Game.Player.WantedLevel > 0)
                {
                    Utils.DisplayHelpTextThisFrame(MsgWanted);
                    Script.Wait(2000);
                    return;
                }

                int price = nearHotdogStand ? 5 : 7;

                if (Game.Player.Money < price)
                {
                    Utils.DisplayHelpTextThisFrame(MsgMoney);
                    Script.Wait(2000);
                    return;
                }

                Function.Call(Hash.REQUEST_ANIM_DICT, "gestures@m@standing@casual");
                Function.Call(Hash.REQUEST_ANIM_DICT, "mp_player_inteat@burger");
                Function.Call(Hash.REQUEST_ANIM_DICT, "misscarsteal4@vendor");

                while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "gestures@m@standing@casual"))
                    Script.Wait(0);

                while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "mp_player_inteat@burger"))
                    Script.Wait(0);

                while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "misscarsteal4@vendor"))
                    Script.Wait(0);

                player.TaskPlayAnim("gestures@m@standing@casual", "gesture_you_soft", -1);
                Script.Wait(1000);
                Function.Call(Hash.STOP_ANIM_TASK,
                    player,
                    "gestures@m@standing@casual",
                    "gesture_you_soft",
                    1.0f);

                Game.Player.Money -= price;
                ped.TaskPlayAnimLoop("misscarsteal4@vendor", "base", 0);
                ped.PlaySpeech("GENERIC_BYE");

                Script.Wait(1500);

                string foodModel = nearHotdogStand ? "prop_cs_hotdog_01" : "prop_cs_burger_01";
                FoodProp = World.CreateProp(foodModel, player.Position, true, true);
                if (FoodProp.Exists())
                {
                    Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY,
                        FoodProp.Handle,
                        player.Handle,
                        player.Bones[Bone.PHLeftHand].Index,
                        0.0f, 0.0f, 0.0f,
                        0.0f, 0.0f, 0.0f,
                        true, true, false, false, 2, true
                    );
                }
                player.TaskPlayAnimUpperBody("mp_player_inteat@burger", "mp_player_int_eat_burger", 4500, false);
                player.Health += 5;
                Script.Wait(2000);
                Function.Call(Hash.STOP_ANIM_TASK,
                    player,
                    "mp_player_inteat@burger",
                    "mp_player_int_eat_burger",
                    1.0f);

                player.PlaySpeech("GENERIC_EAT");
                
                if (FoodProp.Exists())
                    FoodProp.Delete();
                GTA.UI.Notification.PostTicker("~y~ Health increased by 5%", true);
                player.Health += 5;

                Script.Wait(500);
            }
        }
    }
}
