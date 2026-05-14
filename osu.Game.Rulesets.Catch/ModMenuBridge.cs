// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// ПУТЬ: osu.Game.Rulesets.Catch/ModMenuBridge.cs

using System;

namespace osu.Game.Rulesets.Catch
{
    public static class ModMenuBridge
    {
        private static Func<bool> getAutoPlay  = () => false;
        private static Func<bool> getNoMiss    = () => false;
        private static Func<bool> getRelax     = () => false;
        // БАГ FIX: BigHitbox не было в полях и Init-подписи → ошибка компиляции
        private static Func<bool> getBigHitbox = () => false;

        public static bool AutoPlayEnabled  => getAutoPlay();
        public static bool NoMissEnabled    => getNoMiss();
        public static bool RelaxEnabled     => getRelax();
        public static bool BigHitboxEnabled => getBigHitbox();

        public static void Init(
            Func<bool> autoPlay,
            Func<bool> noMiss,
            Func<bool> relax,
            Func<bool> bigHitbox)   // БАГ FIX: добавлен параметр
        {
            getAutoPlay  = autoPlay;
            getNoMiss    = noMiss;
            getRelax     = relax;
            getBigHitbox = bigHitbox;
        }
    }
}
