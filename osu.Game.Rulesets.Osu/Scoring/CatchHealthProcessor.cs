// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// ПУТЬ: osu.Game.Rulesets.Catch/Scoring/CatchHealthProcessor.cs

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Catch.Scoring
{
    public partial class CatchHealthProcessor : LegacyDrainingHealthProcessor
    {
        public CatchHealthProcessor(double drainStartTime)
            : base(drainStartTime)
        {
        }

        protected override IEnumerable<HitObject> EnumerateTopLevelHitObjects() =>
            EnumerateHitObjects(Beatmap).Where(h => h is Fruit || h is Droplet || h is Banana);

        protected override IEnumerable<HitObject> EnumerateNestedHitObjects(HitObject hitObject) =>
            Enumerable.Empty<HitObject>();

        protected override bool CheckDefaultFailCondition(JudgementResult result)
        {
            // NoMiss: никогда не проваливаемся
            if (ModMenuBridge.NoMissEnabled)
                return false;

            // tiny droplet не вызывает провал
            if (result.Type == HitResult.SmallTickMiss)
                return false;

            // banana shower не вызывает провал
            if (result.HitObject is BananaShower)
                return false;

            return base.CheckDefaultFailCondition(result);
        }

        protected override double GetHealthIncreaseFor(HitObject hitObject, HitResult result)
        {
            switch (result)
            {
                case HitResult.SmallTickMiss:
                    return 0;

                case HitResult.LargeTickMiss:
                case HitResult.Miss:
                    // NoMiss: нулевая потеря здоровья за промах
                    if (ModMenuBridge.NoMissEnabled)
                        return 0;
                    return IBeatmapDifficultyInfo.DifficultyRange(
                        Beatmap.Difficulty.DrainRate, -0.03, -0.125, -0.2);

                case HitResult.SmallTickHit:
                    return HpMultiplierNormal * 0.0015;

                case HitResult.LargeTickHit:
                    return HpMultiplierNormal * 0.015;

                case HitResult.Great:
                    return HpMultiplierNormal * 0.03;

                case HitResult.LargeBonus:
                    return HpMultiplierNormal * 0.0025;

                default:
                    return 0;
            }
        }
    }
}
