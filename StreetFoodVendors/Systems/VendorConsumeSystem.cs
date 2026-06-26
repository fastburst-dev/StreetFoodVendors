using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using StreetFoodVendors.Data;
using StreetFoodVendors.Util;

namespace StreetFoodVendors.Systems
{
    internal class VendorConsumeSystem
    {
        private readonly Queue<string> _queue = new Queue<string>();
        private readonly Dictionary<string, int> _cooldowns = new Dictionary<string, int>();
        private readonly VendorSystem _vendorSystem;
        private Ped _activeVendor;
        private const int CONSUME_COOLDOWN_MS = 3000;

        public VendorConsumeSystem()
        {
            GTA.UI.Screen.ShowSubtitle("Street Food Vendors now available");
        }

        public void SetActiveVendor(Ped vendor)
        {
            _activeVendor = vendor;
        }


        // ------------------------------------------------------------
        // PUBLIC API — Called by VendorMenuUI
        // ------------------------------------------------------------
        public void QueueItem(string itemId)
        {
            _queue.Enqueue(itemId);
        }

        // ------------------------------------------------------------
        // TICK
        // ------------------------------------------------------------
        public void Tick()
        {
            try
            {
                Ped player = Game.Player.Character;
                if (!player.Exists())
                    return;

                if (_queue.Count == 0)
                    return;

                string itemId = _queue.Peek();

                if (!IsReady(itemId))
                    return;

                Consume(itemId);

                _cooldowns[itemId] = Game.GameTime + CONSUME_COOLDOWN_MS;
                _queue.Dequeue();
            }
            catch (Exception ex)
            {
                GTA.UI.Notification.Show($"VendorConsumeSystem.Tick ERROR: {ex}");
            }
        }

        private bool IsReady(string itemId)
        {
            if (!_cooldowns.ContainsKey(itemId))
                return true;

            return Game.GameTime > _cooldowns[itemId];
        }

        // ------------------------------------------------------------
        // PROP MODEL RESOLUTION
        // ------------------------------------------------------------
        private string GetPropModelForItem(string itemId)
        {
            switch (itemId)
            {
                case "sprunk": return "prop_ld_can_01";
                case "e_colas": return "prop_ecola_can";
                case "coffee": return "prop_food_coffee";
                case "juice01": return "prop_food_juice01";

                case "beer1": return "prop_cs_beer_bot_01";
                case "beer2": return "prop_cs_beer_bot_02";
                case "beer40": return "prop_cs_beer_bot_40oz_02";
                case "whiskey": return "prop_cs_whiskey_bottle";

                case "egochaser": return "prop_choc_ego";
                case "ps_and_qs": return "prop_candy_pqs";
                case "meteorite": return "prop_choc_meto";

                case "sandwich": return "prop_sandwich_01";
                case "taco": return "prop_taco_01";
                case "hotdog": return "prop_cs_hotdog_01";
                case "burger": return "prop_cs_burger_01";

                case "donut": return "prop_donut_01";

                default:
                    return "prop_ecola_can";
            }
        }

        // ------------------------------------------------------------
        // ANIMATION RESOLUTION
        // ------------------------------------------------------------
        private (string dict, string anim) GetAnimationForItem(string itemId)
        {
            switch (itemId)
            {
                // Drinks
                case "sprunk":
                case "e_colas":
                case "coffee":
                case "juice01":
                case "beer1":
                case "beer2":
                case "beer40":
                case "whiskey":
                    return ("mini@sprunk", "PLYR_BUY_DRINK_PT2");

                // Food (snacks)
                default:
                    return ("mp_player_inteat@burger", "mp_player_int_eat_burger_left");
            }
        }

        // ------------------------------------------------------------
        // MAIN CONSUME LOGIC
        // ------------------------------------------------------------
        private void Consume(string itemId)
        {
            Ped player = Game.Player.Character;

            if (!player.Exists())
                return;

            if (Utils.IsPlayerBusy(player))
                return;

            Game.Player.CanControlCharacter = true;
            player.Task.ClearAllImmediately();
            player.PlayAmbientSpeech("GENERIC_BUY", false, null);

            // --------------------------------------------------------
            // Resolve animation + prop
            // --------------------------------------------------------
            var (animDict, animName) = GetAnimationForItem(itemId);
            Utils.RequestAnimDict(animDict);

            string modelName = GetPropModelForItem(itemId);
            Model model = new Model(modelName);

            if (!model.IsLoaded)
                model.Request(500);

            if (!model.IsLoaded)
                return;

            Vector3 spawnPos = player.Position + player.ForwardVector * 0.2f + new Vector3(0, 0, 0.1f);
            Prop prop = World.CreateProp(model, spawnPos, true, true);

            if (!prop.Exists())
                return;

            // ------------------------------------------------------------
            // CLERK HANDOFF ANIMATION
            // ------------------------------------------------------------
            if (_activeVendor != null && _activeVendor.Exists())
            {
                _activeVendor.TaskPlayAnim("mp_am_hold_up", "purchase_beer_shopkeeper", 8, -1);
                Script.Wait(500);

                prop.AttachTo(_activeVendor.Bones[Bone.SkelLeftHand], new Vector3(0.09f, 0.01f, 0.07f), new Vector3(-170f, 0f, 0f));

                Script.Wait(750);
                prop.Detach();
                Script.Wait(1000);
            }

            bool isDrink =
                itemId == "sprunk" ||
                itemId == "e_colas" ||
                itemId == "coffee" ||
                itemId == "juice01" ||
                itemId == "beer1" ||
                itemId == "beer2" ||
                itemId == "beer40" ||
                itemId == "whiskey";

            Bone handBone = isDrink ? Bone.PHRightHand : Bone.PHLeftHand;

            Vector3 posOffset = isDrink
                ? new Vector3(0, 0, 0)
                : new Vector3(0.025f, 0.015f, -0.025f);

            Vector3 rotOffset = isDrink
                ? new Vector3(0, 0, 0)
                : new Vector3(0, 0, 0);

            // Special offsets
            if (itemId == "taco" || itemId == "hotdog")
            {
                posOffset = new Vector3(0.055f, 0.015f, -0.025f);
                rotOffset = new Vector3(0, 0, 90f);
            }

            Script.Wait(1000);

            // Attach to player
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY,
                prop.Handle,
                player.Handle,
                player.Bones[handBone].Index,
                posOffset.X, posOffset.Y, posOffset.Z,
                rotOffset.X, rotOffset.Y, rotOffset.Z,
                true, true, false, false, 2, true
            );

            // --------------------------------------------------------
            // Play animation
            // --------------------------------------------------------
            if (isDrink)
            {
                // Load anim dictionary properly
                Function.Call(Hash.REQUEST_ANIM_DICT, animDict);
                while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict))
                    Script.Yield();

                // Lock player so nothing interrupts
                player.Task.ClearAllImmediately();
                player.AlwaysKeepTask = true;
                player.BlockPermanentEvents = true;

                // Play the looping drink animation
                player.TaskPlayAnim(animDict, animName, 1, 6000);

                // Hold the drink animation for a fixed time (Rockstar uses ~2.8s)
                int drinkEnd = Game.GameTime + 8000;
                while (Game.GameTime < drinkEnd)
                {
                    if (!player.IsPlayingAnim(animDict, animName))
                        break;

                    Script.Yield();
                }

                // Manually stop the drink animation
                Function.Call(Hash.STOP_ANIM_TASK, player.Handle, animDict, animName, 1.0f);

                // Play vending sound
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "VENDING_MACHINE", "VENDING_MACHINE", false);
                Script.Wait(6000);

                // Load throw anim
                Function.Call(Hash.REQUEST_ANIM_DICT, "mini@sprunk");
                while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "mini@sprunk"))
                    Script.Yield();

                // Play throw animation
                player.TaskPlayAnim("mini@sprunk", "plyr_buy_drink_pt3", 0, -1);

                // Wait for throw animation to finish
                while (player.IsPlayingAnim("mini@sprunk", "plyr_buy_drink_pt3"))
                    Script.Yield();

                // Restore player state
                player.AlwaysKeepTask = false;
                player.BlockPermanentEvents = false;

                if (Function.Call<int>(Hash.GET_PLAYER_WANTED_LEVEL, Game.Player) == 0)
                    player.PlayAmbientSpeech("GENERIC_DRINK", false);

                Script.Wait(750);

                if (prop.Exists())
                {
                    prop.Detach();
                    prop.ApplyForce(player.RightVector * -5f + player.UpVector * 5f);
                    prop.MarkAsNoLongerNeeded();
                    Script.Wait(3000);
                    Utils.DeleteProp(prop);
                }
            }
            else
            {
                const string finishDict = "mp_player_inteat@burger";
                const string finishAnim = "mp_player_int_eat_burger_fp";

                Utils.RequestAnimDict(finishDict);
                player.TaskPlayAnim(finishDict, finishAnim, 1, 4000);
                Script.Wait(900);

                if (Function.Call<int>(Hash.GET_PLAYER_WANTED_LEVEL, Game.Player) == 0)
                    player.PlayAmbientSpeech("GENERIC_EAT", false);

                Script.Wait(3000);

                if (prop.Exists())
                {
                    prop.MarkAsNoLongerNeeded();
                    Utils.DeleteProp(prop);
                }
            }

            ApplyEffects(itemId);

            _activeVendor.PlayAmbientSpeech("GENERIC_BYE", false);
        }

        // ------------------------------------------------------------
        // EFFECTS
        // ------------------------------------------------------------
        private void ApplyEffects(string itemId)
        {
            Ped player = Game.Player.Character;

            switch (itemId)
            {
                // Food
                case "ps_and_qs":
                case "egochaser":
                case "meteorite":
                case "sandwich":
                case "taco":
                case "hotdog":
                case "burger":
                case "donut":
                    player.Health = Math.Min(player.MaxHealth, player.Health + 5);
                    player.PlayAmbientSpeech("GENERIC_EAT", false, null);
                    GTA.UI.Notification.PostTicker("~y~Health restored by 5%", true, true);
                    break;

                // Drinks
                case "sprunk":
                case "e_colas":
                case "coffee":
                case "juice01":
                case "beer1":
                case "beer2":
                case "beer40":
                case "whiskey":
                    player.Health = Math.Min(player.MaxHealth, player.Health + 15);
                    player.PlayAmbientSpeech("GENERIC_DRINK", false, null);
                    GTA.UI.Notification.PostTicker("~o~Health restored by 15%", true, true);
                    break;
            }
        }
    }
}
