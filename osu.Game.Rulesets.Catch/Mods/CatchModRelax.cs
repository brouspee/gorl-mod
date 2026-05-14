// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ПУТЬ: osu.Game.Rulesets.Catch/Mods/CatchModRelax.cs

using System;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using osuTK;

namespace osu.Game.Rulesets.Catch.Mods
{
    public partial class CatchModRelax : ModRelax,
        IApplicableToDrawableRuleset<CatchHitObject>,
        IApplicableToPlayer
    {
        public override LocalisableString Description => @"Use the mouse to control the catcher.";

        public override Type[] IncompatibleMods =>
            base.IncompatibleMods.Concat(new[] { typeof(CatchModMovingFast) }).ToArray();

        private DrawableCatchRuleset drawableRuleset = null!;

        public void ApplyToDrawableRuleset(DrawableRuleset<CatchHitObject> drawableRuleset)
        {
            this.drawableRuleset = (DrawableCatchRuleset)drawableRuleset;
        }

        public void ApplyToPlayer(Player player)
        {
            if (drawableRuleset.HasReplayLoaded.Value)
                return;

            var catchPlayfield = (CatchPlayfield)drawableRuleset.Playfield;

            // Всегда добавляем хелпер — он сам проверяет ModMenuBridge.RelaxEnabled
            // при каждом событии, поэтому работает при включении/выключении во время игры
            catchPlayfield.CatcherArea.Add(new MouseInputHelper(catchPlayfield.CatcherArea));
        }

        private partial class MouseInputHelper : Drawable,
            IKeyBindingHandler<CatchAction>,
            IRequireHighFrequencyMousePosition
        {
            private readonly CatcherArea catcherArea;

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

            public MouseInputHelper(CatcherArea catcherArea)
            {
                this.catcherArea = catcherArea;
                RelativeSizeAxes = Axes.Both;
            }

            // Блокируем клавиатуру только когда Relax активен через ModMenuBridge
            public bool OnPressed(KeyBindingPressEvent<CatchAction> e)
                => ModMenuBridge.RelaxEnabled;

            public void OnReleased(KeyBindingReleaseEvent<CatchAction> e) { }

            protected override bool OnMouseMove(MouseMoveEvent e)
            {
                // Проверяем при каждом движении — работает при динамическом вкл/выкл
                if (!ModMenuBridge.RelaxEnabled)
                    return base.OnMouseMove(e);

                catcherArea.SetCatcherPosition(
                    e.MousePosition.X / DrawSize.X * CatchPlayfield.WIDTH);
                return base.OnMouseMove(e);
            }
        }
    }
}
