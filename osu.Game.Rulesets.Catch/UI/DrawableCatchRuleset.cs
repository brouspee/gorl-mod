// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ПУТЬ: osu.Game.Rulesets.Catch/UI/DrawableCatchRuleset.cs

using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Input;
using osu.Framework.Input.StateChanges;
using osu.Game.Beatmaps;
using osu.Game.Input.Handlers;
using osu.Game.Replays;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Catch.UI
{
    public partial class DrawableCatchRuleset : DrawableScrollingRuleset<CatchHitObject>
    {
        public new IBindable<bool> HasReplayLoaded => base.HasReplayLoaded;

        public DrawableCatchRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod>? mods = null)
            : base(ruleset, beatmap, mods)
        {
            Direction.Value = ScrollingDirection.Down;
            TimeRange.Value = GetTimeRange(beatmap.Difficulty.ApproachRate);
        }

        public static double GetTimeRange(float approachRate)
            => IBeatmapDifficultyInfo.DifficultyRange(approachRate, 1800, 1200, 450);

        protected override Playfield CreatePlayfield() => new CatchPlayfield(Beatmap.Difficulty);

        // public, не protected — именно так объявлено в DrawableRuleset<T>
        public override DrawableHitObject<CatchHitObject>? CreateDrawableRepresentation(CatchHitObject h) => null;

        protected override ReplayInputHandler? CreateReplayInputHandler(Replay replay)
        {
            if (replay != null)
                return new CatchFramedReplayInputHandler(replay);

            // ModMenu AutoPlay: генерируем реплей без включения мода в SelectedMods
            if (ModMenuBridge.AutoPlayEnabled)
            {
                var autoReplay = new CatchAutoGenerator(Beatmap).Generate();
                return new CatchFramedReplayInputHandler(autoReplay);
            }

            return null;
        }

        protected override ReplayRecorder? CreateReplayRecorder(Score score)
            => new CatchReplayRecorder(score, (CatchPlayfield)Playfield);

        protected override PassThroughInputManager CreateInputManager()
            => new CatchInputManager(Ruleset!.RulesetInfo);
    }
}
