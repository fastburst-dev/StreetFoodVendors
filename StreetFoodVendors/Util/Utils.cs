using System;
using GTA;
using GTA.Native;

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
        public static void PlaySpeech(this Ped ped, string speechFile)
        {
            // PLAY_PED_AMBIENT_SPEECH_NATIVE
            Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE,
                ped,
                speechFile,
                "SPEECH_PARAMS_FORCE"
            );
        }

        // ---------------------------------------------------------
        // PLAY ANIMATION (ONE-SHOT)
        // ---------------------------------------------------------
        public static void TaskPlayAnim(this Ped ped, string animDict, string animName, int duration)
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
                (int)AnimationFlags.None,
                0.0f,
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
    }
}
