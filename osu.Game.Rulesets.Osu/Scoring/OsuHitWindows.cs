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

        // Расширенные окна для BigHitbox
        private const double BIG_HITBOX_GREAT = 380;
        private const double BIG_HIT_BOX_OK    = 200;
        private const double BIG_HIT_BOX_MEH   = 320;
        private const double BIG_HIT_BOX_MISS = 50;

        // Стандартные фиксированные окна
        private const double NORMAL_GREAT = 50;
        private const double NORMAL_OK    = 60;
        private const double NORMAL_MEH   = 100;
        private const double NORMAL_MISS  = 300;

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
            // BigHitbox: расширенные окна для легкого попадания
            if (OsuModMenuBridge.BigHitboxEnabled)
            {
                great = 140;
                ok    = 220;
                meh   = 320;
                miss  = 50;
            }
            else
            {
                // Стандартные окна: фиксированные значения
                great = 130;
                ok    = 200;
                meh   = 320;
                miss  = 40;
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


// TIMING_ASSIST_PATCH
// Enlarged hit timing windows when BigHitbox is enabled.
