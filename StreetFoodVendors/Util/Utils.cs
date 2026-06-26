using GTA;
using GTA.Math;
using GTA.Native;
using System;

namespace StreetFoodVendors.Util
{
    public static class Utils
    {
        // ---------------------------------------------------------
        // HELP TEXT
        // ---------------------------------------------------------
        public static void DisplayHelpTextThisFrame(string text)
        {
            Function.Call((Hash)0x8509B634FBE7DA11, "STRING");
            Function.Call((Hash)0x6C188BE134E074AA, text);
            Function.Call((Hash)0x238FFE5C7B0498A6, 0, false, true, -1);
        }

        // ---------------------------------------------------------
        // SPEECH (Ambient Speech)
        // ---------------------------------------------------------
        public static void PlayAmbientSpeech(this Ped ped, string speechFile, bool immediately = false, string[] queue = null)
        {
            if (ped == null || !ped.Exists())
                return;

            string text = speechFile;

            try
            {
                // Stop current speech if requested
                if (immediately)
                {
                    Function.Call(Hash.STOP_PED_SPEAKING, ped.Handle, true);
                }

                // If queue provided, try fallback speech lines
                if (queue != null && queue.Length > 0)
                {
                    // If main line fails, try queued lines
                    if (!Function.Call<bool>(Hash.IS_AMBIENT_SPEECH_PLAYING, ped.Handle))
                    {
                        for (int i = 0; i < queue.Length; i++)
                        {
                            if (Function.Call<bool>(Hash.IS_AMBIENT_SPEECH_PLAYING, ped.Handle))
                            {
                                text = queue[i];
                                break;
                            }
                        }
                    }
                }

                // Enable director mode audio flag (required for some speech types)
                Function.Call(Hash.SET_AUDIO_FLAG, "IsDirectorModeActive", true);

                // Play the speech
                Function.Call(
                    Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE,
                    ped.Handle,
                    text,
                    "SPEECH_PARAMS_FORCE"
                );

                // Disable flag
                Function.Call(Hash.SET_AUDIO_FLAG, "IsDirectorModeActive", false);
            }
            catch (Exception ex)
            {
                
            }
        }

        // ---------------------------------------------------------
        // PLAY ANIMATION (ONE-SHOT)
        // ---------------------------------------------------------
        public static void TaskPlayAnim(this Ped ped, string animDict, string animFile, int animFlag, int duration)
        {
            if (ped == null || !ped.Exists())
                return;

            // Request dictionary
            Function.Call(Hash.REQUEST_ANIM_DICT, animDict);

            // Wait up to 1 second for it to load
            DateTime timeout = DateTime.Now + TimeSpan.FromSeconds(1);
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict))
            {
                Script.Yield();
                if (DateTime.Now >= timeout)
                    return;
            }

            // Play animation
            Function.Call(
                Hash.TASK_PLAY_ANIM,
                ped.Handle,
                animDict,
                animFile,
                4.0f,     // speed
                -4.0f,    // speedMult
                duration,
                animFlag,
                0.0f,     // playbackRate
                false,
                false,
                false
            );
        }

        // ---------------------------------------------------------
        // PLAY ANIMATION (LOOP)
        // ---------------------------------------------------------
        public static void TaskPlayAnimLoop(this Ped ped, string animDict, string animName, int duration)
        {
            Function.Call(Hash.REQUEST_ANIM_DICT, animDict);

            DateTime timeout = DateTime.Now.AddMilliseconds(1000);
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict))
            {
                Script.Yield();
                if (DateTime.Now >= timeout)
                    return;
            }

            Function.Call(Hash.TASK_PLAY_ANIM,
                ped,
                animDict,
                animName,
                8.0f,
                -4.0f,
                duration,
                (int)AnimationFlags.Loop,
                0.0f,
                false,
                false,
                false
            );
        }

        // ---------------------------------------------------------
        // PLAY ANIMATION (UPPER BODY ONLY)
        // ---------------------------------------------------------
        public static void TaskPlayAnimUpperBody(this Ped ped, string animDict, string animName, int duration, bool loop)
        {
            Function.Call(Hash.REQUEST_ANIM_DICT, animDict);

            DateTime timeout = DateTime.Now.AddMilliseconds(1000);
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict))
            {
                Script.Yield();
                if (DateTime.Now >= timeout)
                    return;
            }

            AnimationFlags flags = loop
                ? AnimationFlags.UpperBodyOnly | AnimationFlags.Loop
                : AnimationFlags.UpperBodyOnly;

            Function.Call(Hash.TASK_PLAY_ANIM,
                ped,
                animDict,
                animName,
                8.0f,
                -4.0f,
                duration,
                (int)flags,
                0.0f,
                false,
                false,
                false
            );
        }

        // ------------------------------------------------------------
        // GENERIC HELPERS USED BY ShopConsumeSystem
        // ------------------------------------------------------------
        public static bool IsPlayerBusy(Ped player)
        {
            return player == null || !player.Exists() || player.IsInVehicle() || player.IsRagdoll || player.IsDead;
        }

        public static void RequestAnimDict(string dict)
        {
            try
            {
                Function.Call(Hash.REQUEST_ANIM_DICT, dict);
                int timeout = Game.GameTime + 2000;
                while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, dict) && Game.GameTime < timeout)
                    Script.Yield();
            }
            catch (Exception ex)
            {
                
            }
        }

        public static bool IsPlayingAnim(this Ped ped, string animDict, string animFile)
        {
            if (ped == null || !ped.Exists())
                return false;

            return Function.Call<bool>(
                Hash.IS_ENTITY_PLAYING_ANIM,
                ped.Handle,
                animDict,
                animFile,
                3
            );
        }

        public static void SetAnimTime(this Ped ped, string animDict, string animFile, float time)
        {
            if (ped == null || !ped.Exists())
                return;

            Script.Wait(0);

            Function.Call(
                Hash.SET_ENTITY_ANIM_CURRENT_TIME,
                ped.Handle,
                animDict,
                animFile,
                time
            );
        }

        public static float GetAnimTime(this Ped ped, string animDict, string animFile)
        {
            if (ped == null || !ped.Exists())
                return 0f;

            return Function.Call<float>(
                Hash.GET_ENTITY_ANIM_CURRENT_TIME,
                ped.Handle,
                animDict,
                animFile
            );
        }

        public static void PlaceOnGround(this Entity entity, bool isWorld = false)
        {
            if (!isWorld)
            {
                RaycastResult hit = World.Raycast(
                    entity.Position,
                    entity.UpVector * -10f,
                    1000f,
                    IntersectFlags.Everything,
                    entity
                );

                entity.Position = new Vector3(
                    hit.HitPosition.X,
                    hit.HitPosition.Y,
                    hit.HitPosition.Z
                );
            }
            else
            {
                entity.Position = new Vector3(
                    entity.Position.X,
                    entity.Position.Y,
                    World.GetGroundHeight(entity.Position)
                );
            }
        }

        public static void DeleteProp(Prop prop)
        {
            if (prop != null && prop.Exists())
                prop.Delete();
        }
    }
}
