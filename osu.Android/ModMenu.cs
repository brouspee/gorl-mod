// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// ПУТЬ: osu.Android/ModMenu.cs

using System;

namespace osu.Android
{
    public static class ModMenu
    {
        private static readonly object sync = new object();

        private static bool autoPlay;
        private static bool noMiss;
        private static bool relax;
        private static bool instantSpin;
        private static bool forceRanked;
        private static bool catchAssist;
        private static bool bigHitbox;

        public static event Action? OnStateChanged;

        public static bool AutoPlayEnabled   { get { lock (sync) return autoPlay;    } }
        public static bool NoMissEnabled     { get { lock (sync) return noMiss;      } }
        public static bool RelaxEnabled      { get { lock (sync) return relax;       } }
        public static bool InstantSpinEnabled{ get { lock (sync) return instantSpin; } }
        public static bool ForceRankedEnabled{ get { lock (sync) return forceRanked; } }
        public static bool CatchAssistEnabled{ get { lock (sync) return catchAssist; } }
        public static bool BigHitboxEnabled  { get { lock (sync) return bigHitbox;   } }

        /// <summary>
        /// Когда включён NoMiss — NF и Easy скрываются из меню (не нужны).
        /// Когда включён AutoPlay — Relax несовместим.
        /// </summary>
        public static void ToggleAutoPlay()
        {
            lock (sync)
            {
                autoPlay = !autoPlay;
                // AutoPlay подразумевает NoMiss; relax несовместим с autoplay
                if (autoPlay)
                {
                    noMiss = true;
                    relax = false;
                }
            }
            fire();
        }

        public static void ToggleNoMiss()
        {
            lock (sync)
            {
                // NoMiss нельзя выключить когда AutoPlay активен
                if (autoPlay) return;
                noMiss = !noMiss;
                // Если выключаем NoMiss — снять и AutoPlay на случай рассинхрона
                if (!noMiss) autoPlay = false;
            }
            fire();
        }

        public static void ToggleRelax()
        {
            lock (sync)
            {
                relax = !relax;
                // Relax несовместим с AutoPlay
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
                autoPlay = noMiss = relax = instantSpin =
                forceRanked = catchAssist = bigHitbox = false;
            }
            fire();
        }

        public static string GetDebugInfo()
        {
            lock (sync)
            {
                return $"AutoPlay={autoPlay} NoMiss={noMiss} Relax={relax} " +
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
