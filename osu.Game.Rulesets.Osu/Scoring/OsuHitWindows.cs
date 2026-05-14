// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Scoring
{
    public class OsuHitWindows : HitWindows
    {
        public static readonly DifficultyRange GREAT_WINDOW_RANGE = new DifficultyRange(80, 50, 20);
        public static readonly DifficultyRange OK_WINDOW_RANGE    = new DifficultyRange(140, 100, 60);
        public static readonly DifficultyRange MEH_WINDOW_RANGE   = new DifficultyRange(200, 150, 100);

        /// <summary>
        /// osu! ruleset has a fixed miss window regardless of difficulty settings.
        /// </summary>
        public const double MISS_WINDOW = 10;

        // Фиксированные окна когда NoMiss включён — шире, легче попасть
        private const double NOMISS_GREAT = 140;
        private const double NOMISS_OK    = 220;
        private const double NOMISS_MEH   = 320;
        private const double NOMISS_MISS  = 500;

        // Стандартные фиксированные окна (без NoMiss)
        private const double NORMAL_GREAT = 80;
        private const double NORMAL_OK    = 140;
        private const double NORMAL_MEH   = 200;
        private const double NORMAL_MISS  = 400;

        private double great;
        private double ok;
        private double meh;
        private double miss;

        public override bool IsHitResultAllowed(HitResult result)
        {
            switch (result)
            {
                case HitResult.Great:
                case HitResult.Ok:
                case HitResult.Meh:
                case HitResult.Miss:
                    return true;
            }

            return false;
        }

        public override void SetDifficulty(double difficulty)
        {
            if (OsuModMenuBridge.NoMissEnabled)
            {
                // Когда NoMiss включён: фиксированные широкие окна из таблицы
                great = NOMISS_GREAT;
                ok    = NOMISS_OK;
                meh   = NOMISS_MEH;
                miss  = NOMISS_MISS;
            }
            else
            {
                // Стандартные окна: фиксированные значения из таблицы (без NoMiss)
                great = NORMAL_GREAT;
                ok    = NORMAL_OK;
                meh   = NORMAL_MEH;
                miss  = NORMAL_MISS;
            }
        }

        public override double WindowFor(HitResult result)
        {
            switch (result)
            {
                case HitResult.Great:
                    return great;

                case HitResult.Ok:
                    return ok;

                case HitResult.Meh:
                    return meh;

                case HitResult.Miss:
                    return miss;

                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }
        }
    }
}
