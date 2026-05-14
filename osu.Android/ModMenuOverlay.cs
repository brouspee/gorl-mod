// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.

using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Provider;
using Android.Views;
using Android.Widget;

namespace osu.Android
{
    public class ModMenuOverlay : IDisposable
    {
        private readonly Activity activity;

        private IWindowManager? windowManager;

        private LinearLayout? overlayLayout;
        private ScrollView? scrollView;

        private Button? btnAutoPlay;
        private Button? btnNoMiss;
        private Button? btnRelax;
        private Button? btnInstantSpin;
        private Button? btnForceRanked;
        private Button? btnCatchAssist;
        private Button? btnEasyMode;

        private bool isVisible;
        private bool disposed;

        public bool IsVisible => isVisible;

        public ModMenuOverlay(Activity activity)
        {
            this.activity = activity;

            ModMenu.OnStateChanged += onModStateChanged;
        }

        public void Toggle()
        {
            if (isVisible)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            if (isVisible || disposed)
                return;

            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.M)
            {
                if (!Settings.CanDrawOverlays(activity))
                {
                    var intent = new Intent(
                        Settings.ActionManageOverlayPermission,
                        global::Android.Net.Uri.Parse($"package:{activity.PackageName}"));

                    intent.AddFlags(ActivityFlags.NewTask);

                    activity.StartActivity(intent);

                    return;
                }
            }

            windowManager =
                activity.GetSystemService(Context.WindowService)
                as IWindowManager;

            if (windowManager == null)
                return;

            buildLayout();

            var lp = new WindowManagerLayoutParams(
                dpToPx(110),
                dpToPx(180),
                global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O
                    ? WindowManagerTypes.ApplicationOverlay
                    : WindowManagerTypes.Phone,
                WindowManagerFlags.NotFocusable,
                Format.Translucent);

            lp.Gravity = GravityFlags.Top | GravityFlags.Left;

            lp.X = dpToPx(4);
            lp.Y = dpToPx(4);

            windowManager.AddView(scrollView, lp);

            isVisible = true;
        }

        public void Hide()
        {
            if (!isVisible || disposed)
                return;

            if (scrollView != null)
            {
                windowManager?.RemoveView(scrollView);

                scrollView.Dispose();

                scrollView = null;
                overlayLayout = null;

                btnAutoPlay = null;
                btnNoMiss = null;
                btnRelax = null;
                btnInstantSpin = null;
                btnForceRanked = null;
                btnCatchAssist = null;
                btnEasyMode = null;
            }

            isVisible = false;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            ModMenu.OnStateChanged -= onModStateChanged;

            Hide();
        }

        private void buildLayout()
        {
            overlayLayout = new LinearLayout(activity)
            {
                Orientation = Orientation.Vertical
            };

            overlayLayout.SetBackgroundColor(
                Color.Argb(220, 15, 15, 15));

            overlayLayout.SetPadding(
                dpToPx(4),
                dpToPx(4),
                dpToPx(4),
                dpToPx(4));

            var title = new TextView(activity);

            title.Text = "MENU";
            title.TextSize = 8f;

            title.Gravity = GravityFlags.Center;

            title.SetTextColor(Color.White);

            title.SetPadding(0, 0, 0, dpToPx(2));

            overlayLayout.AddView(title);

            btnAutoPlay =
                addModButton("Auto", ModMenu.ToggleAutoPlay);

            btnNoMiss =
                addModButton("NoMiss", ModMenu.ToggleNoMiss);

            btnRelax =
                addModButton("Relax", ModMenu.ToggleRelax);

            btnInstantSpin =
                addModButton("Spin", ModMenu.ToggleInstantSpin);

            btnForceRanked =
                addModButton("Rank", ModMenu.ToggleForceRanked);

            btnCatchAssist =
                addModButton("Catch", ModMenu.ToggleCatchAssist);

            btnEasyMode =
                addModButton("Easy", ModMenu.ToggleEasyMode);

            var btnReset = new Button(activity);

            btnReset.Text = "Reset";
            btnReset.TextSize = 8f;

            btnReset.SetTextColor(Color.White);

            btnReset.SetPadding(0, 0, 0, 0);

            btnReset.SetMinimumHeight(0);
            btnReset.SetMinHeight(0);

            btnReset.SetBackgroundColor(
                Color.Argb(220, 120, 35, 35));

            btnReset.Click += (_, _) => ModMenu.ResetAll();

            overlayLayout.AddView(btnReset, makeButtonParams());

            var btnHide = new Button(activity);

            btnHide.Text = "Hide";
            btnHide.TextSize = 8f;

            btnHide.SetTextColor(Color.White);

            btnHide.SetPadding(0, 0, 0, 0);

            btnHide.SetMinimumHeight(0);
            btnHide.SetMinHeight(0);

            btnHide.SetBackgroundColor(
                Color.Argb(220, 40, 40, 40));

            btnHide.Click += (_, _) => Hide();

            overlayLayout.AddView(btnHide, makeButtonParams());

            scrollView = new ScrollView(activity);

            scrollView.VerticalScrollBarEnabled = false;

            scrollView.AddView(overlayLayout);

            refreshButtonStates();
        }

        private Button addModButton(string label, Action action)
        {
            var btn = new Button(activity);

            btn.Text = "⬜ " + label;
            btn.TextSize = 8f;

            btn.SetTextColor(Color.White);

            btn.SetPadding(0, 0, 0, 0);

            btn.SetMinimumHeight(0);
            btn.SetMinHeight(0);

            btn.SetBackgroundColor(
                Color.Argb(220, 35, 35, 35));

            btn.Click += (_, _) => action();

            overlayLayout?.AddView(btn, makeButtonParams());

            return btn;
        }

        private LinearLayout.LayoutParams makeButtonParams()
        {
            return new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                dpToPx(22))
            {
                BottomMargin = dpToPx(2)
            };
        }

        private void onModStateChanged()
        {
            activity.RunOnUiThread(refreshButtonStates);
        }

        private void refreshButtonStates()
        {
            setBtn(btnAutoPlay, "Auto", ModMenu.AutoPlayEnabled);

            setBtn(btnNoMiss, "NoMiss", ModMenu.NoMissEnabled);

            setBtn(btnRelax, "Relax", ModMenu.RelaxEnabled);

            setBtn(btnInstantSpin, "Spin", ModMenu.InstantSpinEnabled);

            setBtn(btnForceRanked, "Rank", ModMenu.ForceRankedEnabled);

            setBtn(btnCatchAssist, "Catch", ModMenu.CatchAssistEnabled);

            setBtn(btnEasyMode, "Easy", ModMenu.EasyModeEnabled);
        }

        private static void setBtn(
            Button? btn,
            string label,
            bool active)
        {
            if (btn == null)
                return;

            btn.Text =
                (active ? "✅ " : "⬜ ") + label;

            btn.SetBackgroundColor(
                active
                    ? Color.Argb(230, 35, 140, 60)
                    : Color.Argb(220, 35, 35, 35));
        }

        private int dpToPx(int dp)
        {
            float density =
                activity.Resources?.DisplayMetrics?.Density ?? 2f;

            return (int)(dp * density + 0.5f);
        }
    }
}
