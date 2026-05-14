using Android.Preferences;
// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// ПУТЬ: osu.Android/ModMenu.cs

using System;

namespace osu.Android
{
    public static class ModMenu
    {
        private static readonly object sync = new object();

        private static bool autoPlay;
        private static bool relax;
        private static bool instantSpin;
        private static bool forceRanked;
        private static bool catchAssist;
        private static bool bigHitbox;

        public static event Action? OnStateChanged;

        public static bool AutoPlayEnabled   { get { lock (sync) return autoPlay;    } }
        public static bool NoMissEnabled       { get { lock (sync) return false;      } }
        public static bool RelaxEnabled      { get { lock (sync) return relax;       } }
        public static bool InstantSpinEnabled{ get { lock (sync) return instantSpin; } }
        public static bool ForceRankedEnabled{ get { lock (sync) return forceRanked; } }
        public static bool CatchAssistEnabled{ get { lock (sync) return catchAssist; } }
        public static bool BigHitboxEnabled  { get { lock (sync) return bigHitbox;   } }

        public static void ToggleAutoPlay()
        {
            lock (sync)
            {
                autoPlay = !autoPlay;
                if (autoPlay) relax = false;
            }
            fire();
        }

        public static void ToggleNoMiss()
        {
            // NoMiss убран - ничего не делает
        }

        public static void ToggleRelax()
        {
            lock (sync)
            {
                relax = !relax;
                if (relax) autoPlay = false;
            }
            fire();
        }

        public static void ToggleInstantSpin()
        {
            lock (sync) instantSpin = !instantSpin;
            fire();
        }

        public static void ToggleForceRanked()
        {
            lock (sync) forceRanked = !forceRanked;
            fire();
        }

        public static void ToggleCatchAssist()
        {
            lock (sync) catchAssist = !catchAssist;
            fire();
        }

        public static void ToggleBigHitbox()
        {
            lock (sync) bigHitbox = !bigHitbox;
            fire();
        }

        public static void ResetAll()
        {
            lock (sync)
            {
                autoPlay = relax = instantSpin =
                forceRanked = catchAssist = bigHitbox = false;
            }
            fire();
        }

        public static string GetDebugInfo()
        {
            lock (sync)
            {
                return $"AutoPlay={autoPlay} Relax={relax} " +
                       $"InstantSpin={instantSpin} ForceRanked={forceRanked} " +
                       $"CatchAssist={catchAssist} BigHitbox={bigHitbox}";
            }
        }

        private static void fire()
        {
            try { OnStateChanged?.Invoke(); }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenu", $"OnStateChanged error: {ex}");
            }
        }
    }
}


// ---- PATCHED ----
// NoMiss полностью убран.
// -----------------