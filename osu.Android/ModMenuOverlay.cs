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

        // Триггер-кнопка — статичная, полупрозрачная, в левом верхнем углу экрана
        // Три нажатия: 1=osu меню, 2=catch меню, 3=скрыть
        private View? triggerButton;
        private WindowManagerLayoutParams? triggerParams;

        // Само меню — перетаскиваемое
        private LinearLayout? rootLayout;
        private LinearLayout? menuLayout;
        private ScrollView? scrollView;
        private WindowManagerLayoutParams? menuLayoutParams;

        private Button? btnAutoPlay;
        private Button? btnNoMiss;
        private Button? btnRelax;
        private Button? btnInstantSpin;
        private Button? btnForceRanked;
        private Button? btnCatchAssist;
        private Button? btnBigHitbox;

        private int menuPage; // 0=скрыто, 1=osu, 2=catch

        private bool triggerAdded;
        private bool menuVisible;
        private bool disposed;

        private float dragStartX, dragStartY;
        private int dragStartWinX, dragStartWinY;

        public ModMenuOverlay(Activity activity)
        {
            this.activity = activity;
            ModMenu.OnStateChanged += onModStateChanged;
        }

        /// <summary>
        /// Вызвать один раз при старте — создаёт статичную триггер-кнопку в левом верхнем углу.
        /// Сама кнопка полупрозрачная и маленькая, нажатие переключает страницы меню.
        /// </summary>
        public void InitTrigger()
        {
            if (disposed || triggerAdded) return;

            if (!canDrawOverlays()) return;

            windowManager ??= activity.GetSystemService(Context.WindowService) as IWindowManager;
            if (windowManager == null) return;

            var btn = new Button(activity);
            btn.Text = "≡";
            btn.TextSize = 14f;
            btn.SetTextColor(Color.White);
            btn.SetPadding(0, 0, 0, 0);
            // Полупрозрачная кнопка — видна, но не мешает
            btn.SetBackgroundColor(Color.Argb(140, 30, 30, 60));
            btn.Click += (_, _) => OnMenuButtonPressed();

            triggerParams = new WindowManagerLayoutParams(
                dpToPx(44), dpToPx(44),
                overlayType(),
                WindowManagerFlags.NotFocusable | WindowManagerFlags.NotTouchModal,
                Format.Translucent);

            triggerParams.Gravity = GravityFlags.Top | GravityFlags.Left;
            triggerParams.X = dpToPx(4);
            triggerParams.Y = dpToPx(4);

            triggerButton = btn;
            windowManager.AddView(triggerButton, triggerParams);
            triggerAdded = true;
        }

        public void OnMenuButtonPressed()
        {
            menuPage = (menuPage + 1) % 3;
            if (menuPage == 0)
                hideMenu();
            else
                showMenu(menuPage);
        }

        public void Hide()
        {
            hideMenu();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            ModMenu.OnStateChanged -= onModStateChanged;
            hideMenu();

            if (triggerAdded && triggerButton != null)
            {
                try { windowManager?.RemoveView(triggerButton); } catch { }
                triggerButton = null;
                triggerAdded = false;
            }
        }

        private void showMenu(int page)
        {
            if (disposed) return;
            if (!canDrawOverlays()) return;

            windowManager ??= activity.GetSystemService(Context.WindowService) as IWindowManager;
            if (windowManager == null) return;

            hideMenu(); // убрать старое если было

            menuPage = page;
            buildMenuLayout(page);

            menuLayoutParams = new WindowManagerLayoutParams(
                dpToPx(160),
                ViewGroup.LayoutParams.WrapContent,
                overlayType(),
                WindowManagerFlags.NotFocusable,
                Format.Translucent);

            menuLayoutParams.Gravity = GravityFlags.Top | GravityFlags.Left;
            menuLayoutParams.X = dpToPx(4);
            menuLayoutParams.Y = dpToPx(56); // ниже триггер-кнопки

            windowManager.AddView(rootLayout, menuLayoutParams);
            menuVisible = true;
        }

        private void hideMenu()
        {
            if (!menuVisible || rootLayout == null) return;

            try { windowManager?.RemoveView(rootLayout); } catch { }
            rootLayout?.Dispose();
            rootLayout = null;
            menuLayout = null;
            scrollView = null;
            btnAutoPlay = btnNoMiss = btnRelax = null;
            btnInstantSpin = btnForceRanked = btnCatchAssist = btnBigHitbox = null;
            menuVisible = false;
        }

        private void buildMenuLayout(int page)
        {
            rootLayout = new LinearLayout(activity) { Orientation = Orientation.Vertical };
            rootLayout.SetBackgroundColor(Color.Argb(240, 12, 12, 20));

            // ── Заголовок + кнопка закрытия ────────────────────────────────
            var header = new LinearLayout(activity) { Orientation = Orientation.Horizontal };

            var titleView = new TextView(activity);
            titleView.Text = page == 1 ? "osu!" : "osu!catch";
            titleView.TextSize = 11f;
            titleView.SetTextColor(Color.Rgb(255, 185, 70));
            titleView.Gravity = GravityFlags.CenterVertical;
            titleView.SetPadding(dpToPx(6), 0, 0, 0);

            // Кнопка закрытия — ВСЕГДА ВИДНА, красная ✕
            var btnClose = new Button(activity);
            btnClose.Text = "✕";
            btnClose.TextSize = 11f;
            btnClose.SetTextColor(Color.White);
            btnClose.SetPadding(0, 0, 0, 0);
            btnClose.SetMinimumHeight(0);
            btnClose.SetMinHeight(0);
            btnClose.SetBackgroundColor(Color.Argb(255, 200, 30, 30));
            btnClose.Click += (_, _) =>
            {
                menuPage = 0;
                hideMenu();
            };

            var titleParams = new LinearLayout.LayoutParams(0, dpToPx(36), 1f);
            var closeParams = new LinearLayout.LayoutParams(dpToPx(36), dpToPx(36));
            header.AddView(titleView, titleParams);
            header.AddView(btnClose, closeParams);

            rootLayout.AddView(header, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, dpToPx(36)));

            // Разделитель
            var divider = new View(activity);
            divider.SetBackgroundColor(Color.Argb(120, 255, 185, 70));
            rootLayout.AddView(divider, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 1));

            // ── Стрелки переключения страниц ────────────────────────────────
            var navRow = new LinearLayout(activity) { Orientation = Orientation.Horizontal };

            var btnPrev = makeNavButton("◀ osu!");
            var btnNext = makeNavButton("osu!catch ▶");

            btnPrev.Click += (_, _) => { menuPage = 1; refreshPage(); };
            btnNext.Click += (_, _) => { menuPage = 2; refreshPage(); };

            var navP = new LinearLayout.LayoutParams(0, dpToPx(34), 1f);
            navP.SetMargins(dpToPx(2), dpToPx(2), dpToPx(2), dpToPx(2));
            navRow.AddView(btnPrev, navP);
            navRow.AddView(btnNext, navP);
            rootLayout.AddView(navRow, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, dpToPx(38)));

            // ── Контент кнопок ──────────────────────────────────────────────
            menuLayout = new LinearLayout(activity) { Orientation = Orientation.Vertical };
            menuLayout.SetPadding(dpToPx(4), dpToPx(4), dpToPx(4), dpToPx(4));

            buildPageContent(page);

            scrollView = new ScrollView(activity);
            scrollView.VerticalScrollBarEnabled = false;
            scrollView.AddView(menuLayout);
            rootLayout.AddView(scrollView);

            refreshButtonStates();

            // Drag — перетаскивание меню
            rootLayout.SetOnTouchListener(new DragTouchListener(this));
        }

        private Button makeNavButton(string text)
        {
            var b = new Button(activity);
            b.Text = text;
            b.TextSize = 8f;
            b.SetTextColor(Color.Rgb(200, 200, 220));
            b.SetPadding(0, 0, 0, 0);
            b.SetMinimumHeight(0);
            b.SetMinHeight(0);
            b.SetBackgroundColor(Color.Argb(200, 40, 40, 70));
            return b;
        }

        private void buildPageContent(int page)
        {
            if (menuLayout == null) return;
            menuLayout.RemoveAllViews();

            if (page == 1)
            {
                btnAutoPlay    = addModButton("🎮 AutoPlay",   ModMenu.ToggleAutoPlay);
                btnNoMiss      = addModButton("❤ NoMiss",      ModMenu.ToggleNoMiss);
                btnRelax       = addModButton("😌 Relax",      ModMenu.ToggleRelax);
                btnBigHitbox   = addModButton("⭕ BigHitbox",  ModMenu.ToggleBigHitbox);
                btnInstantSpin = addModButton("🌀 InstSpin",   ModMenu.ToggleInstantSpin);
                btnForceRanked = addModButton("🏆 Ranked",     ModMenu.ToggleForceRanked);
            }
            else
            {
                btnAutoPlay    = addModButton("🎮 AutoPlay",   ModMenu.ToggleAutoPlay);
                btnNoMiss      = addModButton("❤ NoMiss",      ModMenu.ToggleNoMiss);
                btnRelax       = addModButton("😌 Relax",      ModMenu.ToggleRelax);
                btnCatchAssist = addModButton("🍎 CatchAssist",ModMenu.ToggleCatchAssist);
                btnBigHitbox   = addModButton("⭕ BigHitbox",  ModMenu.ToggleBigHitbox);
                btnForceRanked = addModButton("🏆 Ranked",     ModMenu.ToggleForceRanked);
            }

            var sep = new View(activity);
            sep.SetBackgroundColor(Color.Argb(60, 255, 255, 255));
            menuLayout.AddView(sep, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 1) { TopMargin = dpToPx(2), BottomMargin = dpToPx(2) });

            var btnReset = new Button(activity);
            btnReset.Text = "🔄 Reset All";
            btnReset.TextSize = 9f;
            btnReset.SetTextColor(Color.White);
            btnReset.SetPadding(0, 0, 0, 0);
            btnReset.SetMinimumHeight(0);
            btnReset.SetMinHeight(0);
            btnReset.SetBackgroundColor(Color.Argb(230, 130, 30, 30));
            btnReset.Click += (_, _) => ModMenu.ResetAll();
            menuLayout.AddView(btnReset, makeButtonParams());
        }

        private void refreshPage()
        {
            if (!menuVisible) return;
            buildPageContent(menuPage);
            refreshButtonStates();
        }

        private Button addModButton(string label, Action action)
        {
            var btn = new Button(activity);
            btn.Text = "⬜ " + label;
            btn.TextSize = 9f; // крупнее текст
            btn.SetTextColor(Color.White);
            btn.SetPadding(dpToPx(6), 0, 0, 0);
            btn.SetMinimumHeight(0);
            btn.SetMinHeight(0);
            btn.SetBackgroundColor(Color.Argb(220, 35, 35, 45));
            btn.Gravity = GravityFlags.CenterVertical | GravityFlags.Left;
            btn.Click += (_, _) => action();
            menuLayout?.AddView(btn, makeButtonParams());
            return btn;
        }

        private LinearLayout.LayoutParams makeButtonParams()
        {
            // Крупные кнопки: 40dp высота
            return new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                dpToPx(40))
            { BottomMargin = dpToPx(3) };
        }

        private void onModStateChanged()
        {
            activity.RunOnUiThread(refreshButtonStates);
        }

        private void refreshButtonStates()
        {
            if (!menuVisible) return;

            if (menuPage == 1)
            {
                setBtn(btnAutoPlay,    "🎮 AutoPlay",    ModMenu.AutoPlayEnabled);
                setBtn(btnNoMiss,      "❤ NoMiss",       ModMenu.NoMissEnabled);
                setBtn(btnRelax,       "😌 Relax",       ModMenu.RelaxEnabled);
                setBtn(btnBigHitbox,   "⭕ BigHitbox",   ModMenu.BigHitboxEnabled);
                setBtn(btnInstantSpin, "🌀 InstSpin",    ModMenu.InstantSpinEnabled);
                setBtn(btnForceRanked, "🏆 Ranked",      ModMenu.ForceRankedEnabled);
            }
            else
            {
                setBtn(btnAutoPlay,    "🎮 AutoPlay",    ModMenu.AutoPlayEnabled);
                setBtn(btnNoMiss,      "❤ NoMiss",       ModMenu.NoMissEnabled);
                setBtn(btnRelax,       "😌 Relax",       ModMenu.RelaxEnabled);
                setBtn(btnCatchAssist, "🍎 CatchAssist", ModMenu.CatchAssistEnabled);
                setBtn(btnBigHitbox,   "⭕ BigHitbox",   ModMenu.BigHitboxEnabled);
                setBtn(btnForceRanked, "🏆 Ranked",      ModMenu.ForceRankedEnabled);
            }
        }

        private static void setBtn(Button? btn, string label, bool active)
        {
            if (btn == null) return;
            btn.Text = (active ? "✅ " : "⬜ ") + label;
            btn.SetBackgroundColor(active
                ? Color.Argb(235, 30, 150, 65)
                : Color.Argb(220, 35, 35, 45));
        }

        private bool canDrawOverlays()
        {
            if (global::Android.OS.Build.VERSION.SdkInt < global::Android.OS.BuildVersionCodes.M)
                return true;

            if (!Settings.CanDrawOverlays(activity))
            {
                var intent = new Intent(
                    Settings.ActionManageOverlayPermission,
                    global::Android.Net.Uri.Parse($"package:{activity.PackageName}"));
                intent.AddFlags(ActivityFlags.NewTask);
                activity.StartActivity(intent);
                return false;
            }

            return true;
        }

        private WindowManagerTypes overlayType()
            => global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O
                ? WindowManagerTypes.ApplicationOverlay
                : WindowManagerTypes.Phone;

        private int dpToPx(int dp)
        {
            float density = activity.Resources?.DisplayMetrics?.Density ?? 2f;
            return (int)(dp * density + 0.5f);
        }

        private class DragTouchListener : Java.Lang.Object, View.IOnTouchListener
        {
            private readonly ModMenuOverlay o;
            public DragTouchListener(ModMenuOverlay overlay) { o = overlay; }

            public bool OnTouch(View? v, MotionEvent? e)
            {
                if (e == null || o.menuLayoutParams == null || o.windowManager == null)
                    return false;

                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        o.dragStartX = e.RawX;
                        o.dragStartY = e.RawY;
                        o.dragStartWinX = o.menuLayoutParams.X;
                        o.dragStartWinY = o.menuLayoutParams.Y;
                        return true;

                    case MotionEventActions.Move:
                        o.menuLayoutParams.X = o.dragStartWinX + (int)(e.RawX - o.dragStartX);
                        o.menuLayoutParams.Y = o.dragStartWinY + (int)(e.RawY - o.dragStartY);
                        o.windowManager.UpdateViewLayout(o.rootLayout, o.menuLayoutParams);
                        return true;
                }
                return false;
            }
        }
    }
}
