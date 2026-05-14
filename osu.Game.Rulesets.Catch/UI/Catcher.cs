// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Pooling;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Judgements;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Objects.Drawables;
using osu.Game.Rulesets.Judgements;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Catch.UI
{
    [Cached]
    public partial class Catcher : SkinReloadableDrawable
    {
        public static readonly Color4 DEFAULT_HYPER_DASH_COLOUR = Color4.Red;

        public const float BASE_SIZE = 106.75f;
        public const double BASE_WALK_SPEED = 0.5;
        public const double BASE_DASH_SPEED = 1.0;
        public const float ALLOWED_CATCH_RANGE = 0.8f;

        public bool HyperDashing => hyperDashModifier != 1;

        public bool Dashing
        {
            get => dashing;
            set
            {
                if (value == dashing) return;
                dashing = value;
            }
        }

        public double Speed => (Dashing ? BASE_DASH_SPEED : BASE_WALK_SPEED) * hyperDashModifier;

        public float CatchWidth => BASE_SIZE * AbsoluteScale.X * ALLOWED_CATCH_RANGE;

        public Vector2 BodyScale => Scale * catcherScale;

        public CatcherAnimationState CurrentState
        {
            get => skinnableCatcher.AnimationState.Value;
            private set => skinnableCatcher.AnimationState.Value = value;
        }

        public bool CatchFruitOnPlate { get; set; } = true;

        public Direction VisualDirection
        {
            get => Scale.X > 0 ? Direction.Right : Direction.Left;
            set => Scale = new Vector2(value == Direction.Right ? catcherScale : -catcherScale, catcherScale);
        }

        public static float CalculateCatchWidth(IBeatmapDifficultyInfo difficulty)
            => BASE_SIZE * (float)IBeatmapDifficultyInfo.DifficultyRange(difficulty.CircleSize, 0.8, 0.5, 0.3) * ALLOWED_CATCH_RANGE;

        public static float CalculateCatchWidth(Vector2 scale)
            => BASE_SIZE * Math.Abs(scale.X) * ALLOWED_CATCH_RANGE;

        public bool CanCatch(CatchHitObject hitObject)
        {
            if (hitObject is not PalpableCatchHitObject fruit)
                return false;

            float halfCatchWidth = CatchWidth / 2;
            return fruit.EffectiveX >= X - halfCatchWidth &&
                   fruit.EffectiveX <= X + halfCatchWidth;
        }

        private readonly Container<CaughtObject> caughtObjectContainer;
        private readonly DroppedObjectContainer droppedObjectTarget;
        private readonly SkinnableCatcher skinnableCatcher;

        private bool dashing;
        private float catcherScale = 1;
        private double hyperDashModifier = 1;

        private DrawablePool<CaughtFruit> caughtFruitPool = null!;
        private DrawablePool<CaughtBanana> caughtBananaPool = null!;
        private DrawablePool<CaughtDroplet> caughtDropletPool = null!;

        public Catcher(DroppedObjectContainer droppedObjectTarget, IBeatmapDifficultyInfo? difficulty = null)
        {
            this.droppedObjectTarget = droppedObjectTarget;

            Origin = Anchor.TopCentre;
            Size = new Vector2(BASE_SIZE);

            if (difficulty != null)
            {
                catcherScale = (float)IBeatmapDifficultyInfo.DifficultyRange(difficulty.CircleSize, 0.8, 0.5, 0.3);
                Scale = new Vector2(catcherScale);
            }

            InternalChildren = new Drawable[]
            {
                caughtObjectContainer = new Container<CaughtObject>
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.BottomCentre,
                },
                skinnableCatcher = new SkinnableCatcher(),
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(caughtFruitPool = new DrawablePool<CaughtFruit>(50));
            AddInternal(caughtBananaPool = new DrawablePool<CaughtBanana>(50));
            AddInternal(caughtDropletPool = new DrawablePool<CaughtDroplet>(250));
        }

        public void ApplyDifficulty(IBeatmapDifficultyInfo difficulty)
        {
            catcherScale = (float)IBeatmapDifficultyInfo.DifficultyRange(difficulty.CircleSize, 0.8, 0.5, 0.3);
            Scale = new Vector2(Scale.X > 0 ? catcherScale : -catcherScale, catcherScale);
            Size = new Vector2(BASE_SIZE);
        }

        public Drawable CreateProxiedContent() => caughtObjectContainer.CreateProxy();

        public void OnNewResult(DrawableCatchHitObject hitObject, JudgementResult result)
        {
            var catchResult = (CatchJudgementResult)result;
            catchResult.CatcherAnimationState = CurrentState;
            catchResult.CatcherHyperDash = HyperDashing;

            if (result.IsHit && hitObject.HitObject is PalpableCatchHitObject palpable)
            {
                catchObjectOnPlate(hitObject, palpable);

                if (palpable.HyperDash)
                    hyperDashModifier = 2;
                else
                    hyperDashModifier = 1;

                updateState(CatcherAnimationState.Idle);
            }
            else
            {
                updateState(CatcherAnimationState.Fail);
                hyperDashModifier = 1;
            }
        }

        public void OnRevertResult(JudgementResult result)
        {
            var catchResult = (CatchJudgementResult)result;
            CurrentState = catchResult.CatcherAnimationState;
            hyperDashModifier = catchResult.CatcherHyperDash ? 2 : 1;
            clearPlate();
        }

        private void catchObjectOnPlate(DrawableCatchHitObject drawable, PalpableCatchHitObject hitObject)
        {
            if (!CatchFruitOnPlate) return;

            CaughtObject? obj = getCaughtObject(hitObject);
            if (obj == null) return;

            obj.RestoreState(((IHasCatchObjectState)drawable).SaveState());
            obj.Anchor = Anchor.TopCentre;
            obj.Position = new Vector2(hitObject.EffectiveX - X, -caughtObjectContainer.Height);

            caughtObjectContainer.Add(obj);
        }

        private CaughtObject? getCaughtObject(PalpableCatchHitObject source)
        {
            switch (source)
            {
                case Fruit:
                    return caughtFruitPool.Get();
                case Banana:
                    return caughtBananaPool.Get();
                case Droplet:
                    return caughtDropletPool.Get();
                default:
                    return null;
            }
        }

        private void clearPlate()
        {
            var objs = caughtObjectContainer.ToArray();
            caughtObjectContainer.Clear(false);
            foreach (var obj in objs)
                returnToPool(obj);
        }

        private void returnToPool(CaughtObject obj)
        {
            switch (obj)
            {
                case CaughtFruit fruit:
                    caughtFruitPool.Return(fruit);
                    break;
                case CaughtBanana banana:
                    caughtBananaPool.Return(banana);
                    break;
                case CaughtDroplet droplet:
                    caughtDropletPool.Return(droplet);
                    break;
            }
        }

        private void updateState(CatcherAnimationState state)
        {
            CurrentState = state;
        }

        protected override void SkinChanged(ISkinSource skin)
        {
            base.SkinChanged(skin);
        }
    }
}
