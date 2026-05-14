// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// ПУТЬ: osu.Game.Rulesets.Osu/Scoring/OsuHealthProcessor.cs
//
// БАГ FIX: этого файла не существовало — OsuRuleset.CreateHealthProcessor()
// возвращал new OsuHealthProcessor() который не компилировался.
// Также добавлена поддержка NoMiss через OsuModMenuBridge.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Scoring
{
    public partial class OsuHealthProcessor : LegacyDrainingHealthProcessor
    {
        public OsuHealthProcessor(double drainStartTime)
            : base(drainStartTime)
        {
        }

        protected override IEnumerable<HitObject> EnumerateTopLevelHitObjects()
            => Beatmap.HitObjects;

        protected override IEnumerable<HitObject> EnumerateNestedHitObjects(HitObject hitObject)
        {
            switch (hitObject)
            {
                case Slider slider:
                    foreach (var nested in slider.NestedHitObjects)
                        yield return nested;
                    break;

                case Spinner spinner:
                    foreach (var nested in spinner.NestedHitObjects.Where(t => t is not SpinnerBonusTick))
                        yield return nested;
                    break;
            }
        }

        protected override bool CheckDefaultFailCondition(JudgementResult result)
        {
            // NoMiss отключён — позволяем нормально проигрывать
            // BigHitbox только расширяет окна, не отключает фейлы
            return base.CheckDefaultFailCondition(result);
        }

        protected override double GetHealthIncreaseFor(HitObject hitObject, HitResult result)
        {
            double increase = 0;

            switch (result)
            {
                case HitResult.SmallTickMiss:
                    // BigHitbox: меньше штраф за мелкие миссы
                    if (OsuModMenuBridge.BigHitboxEnabled)
                        return IBeatmapDifficultyInfo.DifficultyRange(Beatmap.Difficulty.DrainRate, -0.01, -0.04, -0.07);
                    return IBeatmapDifficultyInfo.DifficultyRange(Beatmap.Difficulty.DrainRate, -0.02, -0.075, -0.14);

                case HitResult.LargeTickMiss:
                    if (OsuModMenuBridge.BigHitboxEnabled)
                        return IBeatmapDifficultyInfo.DifficultyRange(Beatmap.Difficulty.DrainRate, -0.01, -0.04, -0.07);
                    return IBeatmapDifficultyInfo.DifficultyRange(Beatmap.Difficulty.DrainRate, -0.02, -0.075, -0.14);

                case HitResult.Miss:
                    if (OsuModMenuBridge.BigHitboxEnabled)
                        return IBeatmapDifficultyInfo.DifficultyRange(Beatmap.Difficulty.DrainRate, -0.015, -0.06, -0.1);
                    return IBeatmapDifficultyInfo.DifficultyRange(Beatmap.Difficulty.DrainRate, -0.03, -0.125, -0.2);

                case HitResult.SmallTickHit:
                    increase = 0.02;
                    break;

                case HitResult.SliderTailHit:
                case HitResult.LargeTickHit:
                    increase = hitObject is SliderTick ? 0.015 : 0.02;
                    break;

                case HitResult.Meh:
                    increase = 0.002;
                    break;

                case HitResult.Ok:
                    increase = 0.011;
                    break;

                case HitResult.Great:
                    increase = 0.03;
                    break;

                case HitResult.SmallBonus:
                    increase = 0.0085;
                    break;

                case HitResult.LargeBonus:
                    increase = 0.01;
                    break;
            }

            return HpMultiplierNormal * increase;
        }
    }
}
