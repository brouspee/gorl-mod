// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// ПУТЬ: osu.Android/ModMenuOverlay.cs

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

        // Корневой layout — к нему прикреплён TouchListener для перетаскивания
        private LinearLayout? rootLayout;
        // Кнопка закрытия — статичная, не перетаскивается вместе с меню
        // (реализовано через отдельный оверлей поверх)
        private LinearLayout? menuLayout;
        private ScrollView? scrollView;

        private Button? btnAutoPlay;
        private Button? btnNoMiss;
        private Button? btnRelax;
        private Button? btnInstantSpin;
        private Button? btnForceRanked;
        private Button? btnCatchAssist;
        private Button? btnBigHitbox;

        // Состояние меню: 0=скрыто, 1=osu меню, 2=catch меню
        private int menuPage;

        private bool isVisible;
        private bool disposed;

        // Параметры окна для перетаскивания
        private WindowManagerLayoutParams? layoutParams;
        private float dragStartX, dragStartY;
        private int dragStartWinX, dragStartWinY;

        public bool IsVisible => isVisible;

        public ModMenuOverlay(Activity activity)
        {
            this.activity = activity;
            ModMenu.OnStateChanged += onModStateChanged;
        }

        // Три нажатия: 1=osu меню, 2=catch меню, 3=скрыть
        public void OnMenuButtonPressed()
        {
            menuPage = (menuPage + 1) % 3;
            if (menuPage == 0)
                Hide();
            else
                Show(menuPage);
        }

        public void Toggle() => OnMenuButtonPressed();

        public void Show(int page = 1)
        {
            if (disposed) return;

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

            if (isVisible) Hide();

            windowManager = activity.GetSystemService(Context.WindowService) as IWindowManager;
            if (windowManager == null) return;

            menuPage = page;
            buildLayout(page);

            layoutParams = new WindowManagerLayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent,
                global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O
                    ? WindowManagerTypes.ApplicationOverlay
                    : WindowManagerTypes.Phone,
                WindowManagerFlags.NotFocusable,
                Format.Translucent);

            layoutParams.Gravity = GravityFlags.Top | GravityFlags.Left;
            layoutParams.X = dpToPx(4);
            layoutParams.Y = dpToPx(60); // Отступ сверху чтобы кнопка закрытия была видна

            windowManager.AddView(rootLayout, layoutParams);
            isVisible = true;
        }

        public void Hide()
        {
            if (!isVisible || disposed) return;

            if (rootLayout != null)
            {
                windowManager?.RemoveView(rootLayout);
                rootLayout.Dispose();
                rootLayout = null;
                menuLayout = null;
                scrollView = null;
                btnAutoPlay = btnNoMiss = btnRelax = null;
                btnInstantSpin = btnForceRanked = btnCatchAssist = btnBigHitbox = null;
            }

            isVisible = false;
            menuPage = 0;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            ModMenu.OnStateChanged -= onModStateChanged;
            Hide();
        }

        private void buildLayout(int page)
        {
            // Корневой контейнер — перетаскиваемый
            rootLayout = new LinearLayout(activity)
            {
                Orientation = Orientation.Vertical
            };

            // Кнопка закрытия — ВСЕГДА ВИДНА, статичная вверху
            var btnClose = new Button(activity);
            btnClose.Text = "✕";
            btnClose.TextSize = 10f;
            btnClose.SetTextColor(Color.White);
            btnClose.SetPadding(0, 0, 0, 0);
            btnClose.SetMinimumHeight(0);
            btnClose.SetMinHeight(0);
            btnClose.SetBackgroundColor(Color.Argb(255, 180, 30, 30));
            btnClose.Click += (_, _) => Hide();

            var closeParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, dpToPx(28));
            closeParams.BottomMargin = dpToPx(2);
            rootLayout.AddView(btnClose, closeParams);

            // Кнопки переключения страниц
            var navLayout = new LinearLayout(activity)
            {
                Orientation = Orientation.Horizontal
            };

            var btnPrev = new Button(activity);
            btnPrev.Text = "◀";
            btnPrev.TextSize = 8f;
            btnPrev.SetTextColor(Color.White);
            btnPrev.SetPadding(0, 0, 0, 0);
            btnPrev.SetMinimumHeight(0);
            btnPrev.SetMinHeight(0);
            btnPrev.SetBackgroundColor(Color.Argb(200, 50, 50, 80));
            btnPrev.Click += (_, _) =>
            {
                menuPage = menuPage == 1 ? 2 : 1;
                refreshPage();
            };

            var btnNext = new Button(activity);
            btnNext.Text = "▶";
            btnNext.TextSize = 8f;
            btnNext.SetTextColor(Color.White);
            btnNext.SetPadding(0, 0, 0, 0);
            btnNext.SetMinimumHeight(0);
            btnNext.SetMinHeight(0);
            btnNext.SetBackgroundColor(Color.Argb(200, 50, 50, 80));
            btnNext.Click += (_, _) =>
            {
                menuPage = menuPage == 1 ? 2 : 1;
                refreshPage();
            };

            var navParams = new LinearLayout.LayoutParams(0, dpToPx(24), 1f);
            navParams.BottomMargin = dpToPx(2);
            navLayout.AddView(btnPrev, navParams);
            navLayout.AddView(btnNext, navParams);
            rootLayout.AddView(navLayout, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, dpToPx(24)) { BottomMargin = dpToPx(2) });

            // Меню контент
            menuLayout = new LinearLayout(activity)
            {
                Orientation = Orientation.Vertical
            };
            menuLayout.SetBackgroundColor(Color.Argb(220, 15, 15, 15));
            menuLayout.SetPadding(dpToPx(4), dpToPx(4), dpToPx(4), dpToPx(4));

            scrollView = new ScrollView(activity);
            scrollView.VerticalScrollBarEnabled = false;
            scrollView.AddView(menuLayout);
            rootLayout.AddView(scrollView);

            buildPageContent(page);
            refreshButtonStates();

            // Drag: перетаскивание всего меню
            rootLayout.SetOnTouchListener(new DragTouchListener(this));
        }

        private void buildPageContent(int page)
        {
            if (menuLayout == null) return;
            menuLayout.RemoveAllViews();

            var title = new TextView(activity);
            title.Text = page == 1 ? "osu!" : "osu!catch";
            title.TextSize = 9f;
            title.Gravity = GravityFlags.Center;
            title.SetTextColor(Color.Rgb(255, 180, 80));
            title.SetPadding(0, 0, 0, dpToPx(3));
            menuLayout.AddView(title);

            if (page == 1)
            {
                // osu! страница
                btnAutoPlay   = addModButton("🎮 AutoPlay",  ModMenu.ToggleAutoPlay);
                btnNoMiss     = addModButton("❤ NoMiss",     ModMenu.ToggleNoMiss);
                btnRelax      = addModButton("😌 Relax",     ModMenu.ToggleRelax);
                btnBigHitbox  = addModButton("⭕ BigHB",     ModMenu.ToggleBigHitbox);
                btnInstantSpin= addModButton("🌀 InstSpin",  ModMenu.ToggleInstantSpin);
                btnForceRanked= addModButton("🏆 Ranked",    ModMenu.ToggleForceRanked);
            }
            else
            {
                // osu!catch страница
                btnAutoPlay   = addModButton("🎮 AutoPlay",  ModMenu.ToggleAutoPlay);
                btnNoMiss     = addModButton("❤ NoMiss",     ModMenu.ToggleNoMiss);
                btnRelax      = addModButton("😌 Relax",     ModMenu.ToggleRelax);
                btnCatchAssist= addModButton("🍎 CatchAst",  ModMenu.ToggleCatchAssist);
                btnBigHitbox  = addModButton("⭕ BigHB",     ModMenu.ToggleBigHitbox);
                btnForceRanked= addModButton("🏆 Ranked",    ModMenu.ToggleForceRanked);
            }

            // Reset
            var btnReset = new Button(activity);
            btnReset.Text = "🔄 Reset";
            btnReset.TextSize = 8f;
            btnReset.SetTextColor(Color.White);
            btnReset.SetPadding(0, 0, 0, 0);
            btnReset.SetMinimumHeight(0);
            btnReset.SetMinHeight(0);
            btnReset.SetBackgroundColor(Color.Argb(220, 120, 35, 35));
            btnReset.Click += (_, _) => ModMenu.ResetAll();
            menuLayout.AddView(btnReset, makeButtonParams());
        }

        private void refreshPage()
        {
            buildPageContent(menuPage);
            refreshButtonStates();
        }

        private Button addModButton(string label, Action action)
        {
            var btn = new Button(activity);
            btn.Text = "⬜ " + label;
            btn.TextSize = 8f;
            btn.SetTextColor(Color.White);
            btn.SetPadding(dpToPx(4), 0, 0, 0);
            btn.SetMinimumHeight(0);
            btn.SetMinHeight(0);
            btn.SetBackgroundColor(Color.Argb(220, 35, 35, 35));
            btn.Gravity = GravityFlags.CenterVertical | GravityFlags.Left;
            btn.Click += (_, _) => action();
            menuLayout?.AddView(btn, makeButtonParams());
            return btn;
        }

        private LinearLayout.LayoutParams makeButtonParams()
        {
            return new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                dpToPx(26))
            { BottomMargin = dpToPx(2) };
        }

        private void onModStateChanged()
        {
            activity.RunOnUiThread(refreshButtonStates);
        }

        private void refreshButtonStates()
        {
            if (menuPage == 1)
            {
                setBtn(btnAutoPlay,    "🎮 AutoPlay",  ModMenu.AutoPlayEnabled);
                setBtn(btnNoMiss,      "❤ NoMiss",     ModMenu.NoMissEnabled);
                setBtn(btnRelax,       "😌 Relax",     ModMenu.RelaxEnabled);
                setBtn(btnBigHitbox,   "⭕ BigHB",     ModMenu.BigHitboxEnabled);
                setBtn(btnInstantSpin, "🌀 InstSpin",  ModMenu.InstantSpinEnabled);
                setBtn(btnForceRanked, "🏆 Ranked",    ModMenu.ForceRankedEnabled);
            }
            else
            {
                setBtn(btnAutoPlay,    "🎮 AutoPlay",  ModMenu.AutoPlayEnabled);
                setBtn(btnNoMiss,      "❤ NoMiss",     ModMenu.NoMissEnabled);
                setBtn(btnRelax,       "😌 Relax",     ModMenu.RelaxEnabled);
                setBtn(btnCatchAssist, "🍎 CatchAst",  ModMenu.CatchAssistEnabled);
                setBtn(btnBigHitbox,   "⭕ BigHB",     ModMenu.BigHitboxEnabled);
                setBtn(btnForceRanked, "🏆 Ranked",    ModMenu.ForceRankedEnabled);
            }
        }

        private static void setBtn(Button? btn, string label, bool active)
        {
            if (btn == null) return;
            btn.Text = (active ? "✅ " : "⬜ ") + label;
            btn.SetBackgroundColor(
                active
                    ? Color.Argb(230, 35, 140, 60)
                    : Color.Argb(220, 35, 35, 35));
        }

        private int dpToPx(int dp)
        {
            float density = activity.Resources?.DisplayMetrics?.Density ?? 2f;
            return (int)(dp * density + 0.5f);
        }

        // Drag-to-move реализация через TouchListener
        private class DragTouchListener : Java.Lang.Object, View.IOnTouchListener
        {
            private readonly ModMenuOverlay overlay;
            public DragTouchListener(ModMenuOverlay overlay) { this.overlay = overlay; }

            public bool OnTouch(View? v, MotionEvent? e)
            {
                if (e == null || overlay.layoutParams == null || overlay.windowManager == null)
                    return false;

                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        overlay.dragStartX = e.RawX;
                        overlay.dragStartY = e.RawY;
                        overlay.dragStartWinX = overlay.layoutParams.X;
                        overlay.dragStartWinY = overlay.layoutParams.Y;
                        return true;

                    case MotionEventActions.Move:
                        overlay.layoutParams.X = overlay.dragStartWinX + (int)(e.RawX - overlay.dragStartX);
                        overlay.layoutParams.Y = overlay.dragStartWinY + (int)(e.RawY - overlay.dragStartY);
                        overlay.windowManager.UpdateViewLayout(overlay.rootLayout, overlay.layoutParams);
                        return true;
                }
                return false;
            }
        }
    }
}
