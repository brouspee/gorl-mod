// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ПУТЬ: osu.Game.Rulesets.Catch/ModMenuBridge.cs
//
// Статический мост между osu.Android.ModMenu и osu.Game.Rulesets.Catch.
// Catch проект не может напрямую ссылаться на osu.Android (разные сборки),
// поэтому Android проект при старте вызывает ModMenuBridge.Init() один раз
// и передаёт делегаты-геттеры.

using System;

namespace osu.Game.Rulesets.Catch
{
    public static class ModMenuBridge
    {
        private static Func<bool> getAutoPlay  = () => false;
        private static Func<bool> getNoMiss    = () => false;
        private static Func<bool> getRelax     = () => false;

        public static bool AutoPlayEnabled  => getAutoPlay();
        public static bool NoMissEnabled    => getNoMiss();
        public static bool RelaxEnabled     => getRelax();

        /// <summary>
        /// Вызывается один раз из OsuGameAndroid (или OsuGameActivity) при старте.
        /// Передаёт геттеры флагов из ModMenu.
        /// </summary>
        public static void Init(
            Func<bool> autoPlay,
            Func<bool> noMiss,
            Func<bool> relax)
        {
            getAutoPlay = autoPlay;
            getNoMiss   = noMiss;
            getRelax    = relax;
        }
    }
}
