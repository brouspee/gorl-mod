// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Objects.Drawables;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using osuTK;


namespace osu.Game.Rulesets.Catch.UI
{
    /// <summary>
    /// The horizontal band at the bottom of the playfield the catcher is moving on.
    /// It holds a <see cref="Catcher"/> as a child and translates input to the catcher movement.
    /// It also holds a combo display that is above the catcher, and judgment results are translated to the catcher and the combo display.
    /// </summary>
    public partial class CatcherArea : Container, IKeyBindingHandler<CatchAction>
    {
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;
        public Catcher Catcher
        {
            get => catcher;
            set => catcherContainer.Child = catcher = value;
        }

        private readonly Container<Catcher> catcherContainer;

        private readonly CatchComboDisplay comboDisplay;

        public readonly CatcherTrailDisplay CatcherTrails;

        private Catcher catcher = null!;

        /// <summary>
        /// <c>-1</c> when only left button is pressed.
        /// <c>1</c> when only right button is pressed.
        /// <c>0</c> when none or both left and right buttons are pressed.
        /// </summary>
        private int currentDirection;

        // TODO: support replay rewind
        private bool lastHyperDashState;

        /// <remarks>
        /// <see cref="Catcher"/> must be set before loading.
        /// </remarks>
        public CatcherArea()
        {
            Size = new Vector2(CatchPlayfield.WIDTH, Catcher.BASE_SIZE);
            Children = new Drawable[]
            {
                catcherContainer = new Container<Catcher> { RelativeSizeAxes = Axes.Both },
                CatcherTrails = new CatcherTrailDisplay(),
                comboDisplay = new CatchComboDisplay
                {
                    RelativeSizeAxes = Axes.None,
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.Centre,
                    Margin = new MarginPadding { Bottom = 350f },
                    X = CatchPlayfield.CENTER_X
                }
            };
        }

        public void OnNewResult(DrawableCatchHitObject hitObject, JudgementResult result)
        {
            Catcher.OnNewResult(hitObject, result);
            comboDisplay.OnNewResult(hitObject, result);
        }

        public void OnRevertResult(JudgementResult result)
        {
            comboDisplay.OnRevertResult(result);
            Catcher.OnRevertResult(result);
        }

        protected override void Update()
        {
            base.Update();

            var replayState = (GetContainingInputManager()!.CurrentState as RulesetInputManagerInputState<CatchAction>)?.LastReplayState as CatchFramedReplayInputHandler.CatchReplayState;

            // ModMenu AutoPlay: ловец сам двигается к ближайшему объекту
            if (ModMenuBridge.AutoPlayEnabled && replayState == null)
            {
                float? targetX = findNearestFruitX();
                if (targetX.HasValue)
                {
                    float maxDelta = (float)(Catcher.BASE_DASH_SPEED * 2.0 * Clock.ElapsedFrameTime);
                    float newX = Catcher.X + Math.Clamp(targetX.Value - Catcher.X, -maxDelta, maxDelta);
                    SetCatcherPosition(newX);
                    return;
                }
            }

            SetCatcherPosition(
                replayState?.CatcherX ??
                (float)(Catcher.X + Catcher.Speed * currentDirection * Clock.ElapsedFrameTime));
        }

        private float? findNearestFruitX()
        {
            var playfield = (Parent as CatchPlayfield);
            if (playfield == null) return null;

            float? nearest = null;
            double nearestTime = double.MaxValue;

            foreach (var obj in playfield.HitObjectContainer.AliveObjects)
            {
                if (obj.HitObject is PalpableCatchHitObject palpable)
                {
                    double t = Math.Abs(obj.HitObject.StartTime - playfield.Clock.CurrentTime);
                    if (t < nearestTime)
                    {
                        nearestTime = t;
                        nearest = palpable.EffectiveX;
                    }
                }
            }

            return nearest;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            comboDisplay.X = Catcher.X;

            if ((Clock as IGameplayClock)?.IsRewinding == true)
            {
                // This is probably a wrong value, but currently the true value is not recorded.
                // Setting `true` will prevent generation of false-positive after-images (with more false-negatives).
                lastHyperDashState = true;
                return;
            }

            if (!lastHyperDashState && Catcher.HyperDashing)
                displayCatcherTrail(CatcherTrailAnimation.HyperDashAfterImage);

            if (Catcher.Dashing || Catcher.HyperDashing)
            {
                const double trail_generation_interval = 16;

                if (Time.Current - CatcherTrails.LastDashTrailTime >= trail_generation_interval)
                    displayCatcherTrail(Catcher.HyperDashing ? CatcherTrailAnimation.HyperDashing : CatcherTrailAnimation.Dashing);
            }

            lastHyperDashState = Catcher.HyperDashing;
        }

        public void SetCatcherPosition(float x)
        {
            float lastPosition = Catcher.X;
            float newPosition = Math.Clamp(x, 0, CatchPlayfield.WIDTH);

            Catcher.X = newPosition;

            if (lastPosition < newPosition)
                Catcher.VisualDirection = Direction.Right;
            else if (lastPosition > newPosition)
                Catcher.VisualDirection = Direction.Left;
        }

        public bool OnPressed(KeyBindingPressEvent<CatchAction> e)
        {
            // ModMenu Relax: клавиши лево/право блокируются
            if (ModMenuBridge.RelaxEnabled)
            {
                if (e.Action == CatchAction.Dash) { Catcher.Dashing = true; return true; }
                return true;
            }
            switch (e.Action)
            {
                case CatchAction.MoveLeft:
                    currentDirection--;
                    return true;

                case CatchAction.MoveRight:
                    currentDirection++;
                    return true;

                case CatchAction.Dash:
                    Catcher.Dashing = true;
                    return true;
            }

            return false;
        }

        protected override void OnTouchMove(TouchMoveEvent e)
        {
            if (ModMenuBridge.RelaxEnabled)
            {
                float relativeX = ToLocalSpace(e.Touch.Position).X;
                float catchX = relativeX / DrawWidth * CatchPlayfield.WIDTH;
                SetCatcherPosition(catchX);
                return;
            }
            base.OnTouchMove(e);
        }

        protected override bool OnTouchDown(TouchDownEvent e)
        {
            if (ModMenuBridge.RelaxEnabled)
            {
                float relativeX = ToLocalSpace(e.Touch.Position).X;
                SetCatcherPosition(relativeX / DrawWidth * CatchPlayfield.WIDTH);
                return true;
            }
            return base.OnTouchDown(e);
        }

        public void OnReleased(KeyBindingReleaseEvent<CatchAction> e)
        {
            switch (e.Action)
            {
                case CatchAction.MoveLeft:
                    currentDirection++;
                    break;

                case CatchAction.MoveRight:
                    currentDirection--;
                    break;

                case CatchAction.Dash:
                    Catcher.Dashing = false;
                    break;
            }
        }

        private void displayCatcherTrail(CatcherTrailAnimation animation) => CatcherTrails.Add(new CatcherTrailEntry(Time.Current, Catcher.CurrentState, Catcher.X, Catcher.BodyScale, animation));
    }
}
