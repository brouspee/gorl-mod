// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Game.Rulesets.Catch.Objects
{
    /// <summary>
    /// Represents a <see cref="CatchHitObject"/> that can be physically caught by the catcher.
    /// This excludes container objects like <see cref="BananaShower"/>.
    /// </summary>
    public abstract class PalpableCatchHitObject : CatchHitObject
    {
        /// <summary>
        /// Whether this object will trigger a hyper dash when missed.
        /// </summary>
        public bool HyperDash => HyperDashTarget != null;

        // Bindable exposed to drawables so they can react to hyperdash state
        public readonly Bindable<bool> HyperDashBindable = new Bindable<bool>();

        private PalpableCatchHitObject? hyperDashTarget;

        /// <summary>
        /// The target object that requires a hyper dash to reach.
        /// Setting this also updates <see cref="HyperDashBindable"/>.
        /// </summary>
        public PalpableCatchHitObject? HyperDashTarget
        {
            get => hyperDashTarget;
            set
            {
                hyperDashTarget = value;
                HyperDashBindable.Value = value != null;
            }
        }

        private float distanceToHyperDash;

        /// <summary>
        /// The distance from this object where a hyper dash is required.
        /// </summary>
        public float DistanceToHyperDash
        {
            get => distanceToHyperDash;
            set => distanceToHyperDash = value;
        }
    }
}
