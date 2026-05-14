// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ПУТЬ: osu.Android/OsuGameAndroid.cs

using System;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Microsoft.Maui.Devices;
using osu.Framework.Allocation;
using osu.Framework.Development;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Platform;
using osu.Game;
using osu.Game.Rulesets.Catch;
using osu.Game.Screens;
using osu.Game.Updater;
using osu.Game.Utils;
using osuTK;

namespace osu.Android
{
    public partial class OsuGameAndroid : OsuGame
    {
        [Cached]
        private readonly OsuGameActivity gameActivity;

        private readonly PackageInfo packageInfo;

        private bool isModIntegrationSetup;

        public override Vector2 ScalingContainerTargetDrawSize
            => new Vector2(1024, 1024 * DrawHeight / DrawWidth);

        public OsuGameAndroid(OsuGameActivity activity)
            : base(null)
        {
            gameActivity = activity;

            packageInfo =
                Application.Context.ApplicationContext!
                    .PackageManager!
                    .GetPackageInfo(
                        Application.Context.ApplicationContext.PackageName!,
                        0)
                    .AsNonNull();
        }

        public override string Version
        {
            get
            {
                if (!IsDeployedBuild)
                    return @"local " + (DebugUtils.IsDebugBuild ? @"debug" : @"release");

                return packageInfo.VersionName.AsNonNull();
            }
        }

        public override Version AssemblyVersion
            => new Version(packageInfo.VersionName.AsNonNull().Split('-').First());

        protected override void LoadComplete()
        {
            base.LoadComplete();

            UserPlayingState.BindValueChanged(_ => updateOrientation());

            setupModIntegration();
        }

        private void setupModIntegration()
        {
            if (isModIntegrationSetup)
                return;

            try
            {
                // Инициализируем мост между Android-проектом и Catch-проектом.
                // Catch не видит osu.Android напрямую, поэтому передаём геттеры через делегаты.
                ModMenuBridge.Init(
                    autoPlay: () => ModMenu.AutoPlayEnabled,
                    noMiss:   () => ModMenu.NoMissEnabled,
                    relax:    () => ModMenu.RelaxEnabled
                );

                ModMenu.OnStateChanged += onModMenuStateChanged;
                isModIntegrationSetup = true;

                global::Android.Util.Log.Info(
                    "OsuGameAndroid",
                    "ModMenu integration setup completed");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error(
                    "OsuGameAndroid",
                    $"Failed to setup ModMenu integration: {ex.Message}");
            }
        }

        private void onModMenuStateChanged()
        {
            global::Android.Util.Log.Debug(
                "OsuGameAndroid",
                $"ModMenu state changed: {ModMenu.GetDebugInfo()}");
        }

        protected override void ScreenChanged(IOsuScreen? current, IOsuScreen? newScreen)
        {
            base.ScreenChanged(current, newScreen);

            if (newScreen == null)
                return;

            updateOrientation();
        }

        private void updateOrientation()
        {
            var orientation = MobileUtils.GetOrientation(
                this,
                (IOsuScreen)ScreenStack.CurrentScreen,
                gameActivity.IsTablet);

            switch (orientation)
            {
                case MobileUtils.Orientation.Locked:
                    gameActivity.RequestedOrientation = ScreenOrientation.Locked;
                    break;

                case MobileUtils.Orientation.Portrait:
                    gameActivity.RequestedOrientation = ScreenOrientation.Portrait;
                    break;

                case MobileUtils.Orientation.Default:
                    gameActivity.RequestedOrientation = gameActivity.DefaultOrientation;
                    break;
            }
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);
            host.Window.CursorState |= CursorState.Hidden;
        }

        protected override UpdateManager CreateUpdateManager()
            => new MobileUpdateNotifier();

        protected override BatteryInfo CreateBatteryInfo()
            => new AndroidBatteryInfo();

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing && isModIntegrationSetup)
            {
                try
                {
                    ModMenu.OnStateChanged -= onModMenuStateChanged;

                    global::Android.Util.Log.Info(
                        "OsuGameAndroid",
                        "ModMenu integration disposed");
                }
                catch (Exception ex)
                {
                    global::Android.Util.Log.Error(
                        "OsuGameAndroid",
                        $"Error disposing ModMenu integration: {ex.Message}");
                }
            }

            base.Dispose(isDisposing);
        }

        private class AndroidBatteryInfo : BatteryInfo
        {
            public override double? ChargeLevel => Battery.ChargeLevel;

            public override bool OnBattery
                => Battery.PowerSource == BatteryPowerSource.Battery;
        }
    }
}
