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
        // Флаг чтобы отличать drag от tap на rootLayout
        private bool isDragging;
        private const int DRAG_THRESHOLD_PX = 10;

        public ModMenuOverlay(Activity activity)
        {
            this.activity = activity;
            ModMenu.OnStateChanged += onModStateChanged;
        }

        /// <summary>
        /// Вызвать один раз при старте — создаёт статичную триггер-кнопку в левом верхнем углу.
        /// </summary>
        public void InitTrigger()
        {
            if (disposed || triggerAdded) return;
            if (!canDrawOverlays()) return;

            windowManager ??= activity.GetSystemService(Context.WindowService) as IWindowManager;
            if (windowManager == null) return;

            var btn = new Button(activity);
            btn.Text = "≡";
            btn.TextSize = 15f;
            btn.SetTextColor(Color.White);
            btn.SetPadding(0, 0, 0, 0);
            btn.SetBackgroundColor(Color.Argb(160, 20, 20, 50));
            btn.Click += (_, _) => OnMenuButtonPressed();

            triggerParams = new WindowManagerLayoutParams(
                dpToPx(46), dpToPx(46),
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
            menuPage = 0;
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

            hideMenu();

            menuPage = page;
            buildMenuLayout(page);

            menuLayoutParams = new WindowManagerLayoutParams(
                dpToPx(170),
                ViewGroup.LayoutParams.WrapContent,
                overlayType(),
                WindowManagerFlags.NotFocusable,
                Format.Translucent);

            menuLayoutParams.Gravity = GravityFlags.Top | GravityFlags.Left;
            menuLayoutParams.X = dpToPx(4);
            menuLayoutParams.Y = dpToPx(58);

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
            rootLayout.SetBackgroundColor(Color.Argb(245, 10, 10, 18));

            // ── Заголовок + кнопка закрытия ──────────────────────────────
            var header = new LinearLayout(activity) { Orientation = Orientation.Horizontal };

            var titleView = new TextView(activity);
            titleView.Text = page == 1 ? "🎯 osu!" : "🍎 osu!catch";
            titleView.TextSize = 10f;
            titleView.SetTextColor(Color.Rgb(255, 190, 60));
            titleView.Gravity = GravityFlags.CenterVertical;
            titleView.SetPadding(dpToPx(6), 0, 0, 0);

            // Кнопка закрытия — красная, всегда видна
            var btnClose = new Button(activity);
            btnClose.Text = "✕";
            btnClose.TextSize = 12f;
            btnClose.SetTextColor(Color.White);
            btnClose.SetPadding(0, 0, 0, 0);
            btnClose.SetMinimumHeight(0);
            btnClose.SetMinHeight(0);
            btnClose.SetBackgroundColor(Color.Argb(255, 210, 30, 30));
            btnClose.Click += (_, _) => { menuPage = 0; hideMenu(); };

            var titleParams = new LinearLayout.LayoutParams(0, dpToPx(38), 1f);
            var closeParams = new LinearLayout.LayoutParams(dpToPx(38), dpToPx(38));
            header.AddView(titleView, titleParams);
            header.AddView(btnClose, closeParams);

            rootLayout.AddView(header, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, dpToPx(38)));

            // Разделитель
            addDivider(rootLayout, 80);

            // ── Стрелки переключения страниц ─────────────────────────────
            var navRow = new LinearLayout(activity) { Orientation = Orientation.Horizontal };

            var btnPrev = makeNavButton(page == 1 ? "✦ osu!" : "◀ osu!");
            var btnNext = makeNavButton(page == 2 ? "✦ catch" : "catch ▶");

            // Подсветить активную страницу
            btnPrev.SetBackgroundColor(page == 1
                ? Color.Argb(220, 60, 60, 120)
                : Color.Argb(180, 30, 30, 60));
            btnNext.SetBackgroundColor(page == 2
                ? Color.Argb(220, 60, 60, 120)
                : Color.Argb(180, 30, 30, 60));

            btnPrev.Click += (_, _) => { if (menuPage != 1) { menuPage = 1; rebuildMenu(); } };
            btnNext.Click += (_, _) => { if (menuPage != 2) { menuPage = 2; rebuildMenu(); } };

            var navP = new LinearLayout.LayoutParams(0, dpToPx(32), 1f);
            navP.SetMargins(dpToPx(2), dpToPx(2), dpToPx(1), dpToPx(2));
            var navP2 = new LinearLayout.LayoutParams(0, dpToPx(32), 1f);
            navP2.SetMargins(dpToPx(1), dpToPx(2), dpToPx(2), dpToPx(2));
            navRow.AddView(btnPrev, navP);
            navRow.AddView(btnNext, navP2);
            rootLayout.AddView(navRow, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, dpToPx(36)));

            addDivider(rootLayout, 40);

            // ── Контент кнопок ───────────────────────────────────────────
            menuLayout = new LinearLayout(activity) { Orientation = Orientation.Vertical };
            menuLayout.SetPadding(dpToPx(3), dpToPx(3), dpToPx(3), dpToPx(3));

            buildPageContent(page);

            scrollView = new ScrollView(activity);
            scrollView.VerticalScrollBarEnabled = false;
            scrollView.AddView(menuLayout);
            rootLayout.AddView(scrollView);

            refreshButtonStates();

            // Drag — перетаскивание всего меню
            rootLayout.SetOnTouchListener(new DragTouchListener(this));
        }

        private void rebuildMenu()
        {
            if (!menuVisible || menuLayoutParams == null) return;
            int savedX = menuLayoutParams.X;
            int savedY = menuLayoutParams.Y;

            try { windowManager?.RemoveView(rootLayout); } catch { }
            rootLayout?.Dispose();
            rootLayout = null;
            menuLayout = null;
            scrollView = null;
            btnAutoPlay = btnNoMiss = btnRelax = null;
            btnInstantSpin = btnForceRanked = btnCatchAssist = btnBigHitbox = null;
            menuVisible = false;

            buildMenuLayout(menuPage);

            menuLayoutParams.X = savedX;
            menuLayoutParams.Y = savedY;

            windowManager?.AddView(rootLayout, menuLayoutParams);
            menuVisible = true;
        }

        private void addDivider(LinearLayout parent, int alpha)
        {
            var divider = new View(activity);
            divider.SetBackgroundColor(Color.Argb(alpha, 255, 190, 60));
            parent.AddView(divider, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 1));
        }

        private Button makeNavButton(string text)
        {
            var b = new Button(activity);
            b.Text = text;
            b.TextSize = 8f;
            b.SetTextColor(Color.Rgb(210, 210, 230));
            b.SetPadding(0, 0, 0, 0);
            b.SetMinimumHeight(0);
            b.SetMinHeight(0);
            b.SetBackgroundColor(Color.Argb(180, 30, 30, 60));
            return b;
        }

        private void buildPageContent(int page)
        {
            if (menuLayout == null) return;
            menuLayout.RemoveAllViews();

            if (page == 1)
            {
                // osu! стандарт
                btnAutoPlay    = addModButton("🎮 AutoPlay",   ModMenu.ToggleAutoPlay);
                // NoMiss: когда включён — NF/Easy не нужны (скрыты из меню)
                btnNoMiss      = addModButton("💚 NoMiss",     ModMenu.ToggleNoMiss);
                btnRelax       = addModButton("😌 Relax",      ModMenu.ToggleRelax);
                btnBigHitbox   = addModButton("⭕ BigHitbox",  ModMenu.ToggleBigHitbox);
                btnInstantSpin = addModButton("🌀 InstSpin",   ModMenu.ToggleInstantSpin);
                btnForceRanked = addModButton("🏆 Ranked",     ModMenu.ToggleForceRanked);
            }
            else
            {
                // osu!catch
                btnAutoPlay    = addModButton("🎮 AutoPlay",   ModMenu.ToggleAutoPlay);
                btnNoMiss      = addModButton("💚 NoMiss",     ModMenu.ToggleNoMiss);
                btnRelax       = addModButton("😌 Relax",      ModMenu.ToggleRelax);
                btnCatchAssist = addModButton("🍎 CatchAssist",ModMenu.ToggleCatchAssist);
                btnBigHitbox   = addModButton("⭕ BigHitbox",  ModMenu.ToggleBigHitbox);
                btnForceRanked = addModButton("🏆 Ranked",     ModMenu.ToggleForceRanked);
            }

            addDivider(menuLayout, 40);

            var btnReset = new Button(activity);
            btnReset.Text = "🔄 Reset All";
            btnReset.TextSize = 9f;
            btnReset.SetTextColor(Color.White);
            btnReset.SetPadding(dpToPx(4), 0, 0, 0);
            btnReset.SetMinimumHeight(0);
            btnReset.SetMinHeight(0);
            btnReset.SetBackgroundColor(Color.Argb(230, 140, 30, 30));
            btnReset.Click += (_, _) => ModMenu.ResetAll();
            menuLayout.AddView(btnReset, makeButtonParams());
        }

        private Button addModButton(string label, Action action)
        {
            var btn = new Button(activity);
            btn.Text = "⬜ " + label;
            btn.TextSize = 9f;
            btn.SetTextColor(Color.White);
            btn.SetPadding(dpToPx(4), 0, 0, 0);
            btn.SetMinimumHeight(0);
            btn.SetMinHeight(0);
            btn.SetBackgroundColor(Color.Argb(220, 28, 28, 42));
            btn.Gravity = GravityFlags.CenterVertical | GravityFlags.Left;
            btn.Click += (_, _) => action();
            menuLayout?.AddView(btn, makeButtonParams());
            return btn;
        }

        private LinearLayout.LayoutParams makeButtonParams()
        {
            return new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                dpToPx(38))
            { BottomMargin = dpToPx(2) };
        }

        private void onModStateChanged()
        {
            activity.RunOnUiThread(refreshButtonStates);
        }

        private void refreshButtonStates()
        {
            if (!menuVisible) return;

            bool nm = ModMenu.NoMissEnabled;

            if (menuPage == 1)
            {
                setBtn(btnAutoPlay,    "🎮 AutoPlay",    ModMenu.AutoPlayEnabled);
                setBtn(btnNoMiss,      "💚 NoMiss",      nm);
                setBtn(btnRelax,       "😌 Relax",       ModMenu.RelaxEnabled);
                setBtn(btnBigHitbox,   "⭕ BigHitbox",   ModMenu.BigHitboxEnabled);
                setBtn(btnInstantSpin, "🌀 InstSpin",    ModMenu.InstantSpinEnabled);
                setBtn(btnForceRanked, "🏆 Ranked",      ModMenu.ForceRankedEnabled);

                // Когда NoMiss включён — AutoPlay меняет надпись для ясности
                if (btnAutoPlay != null && ModMenu.AutoPlayEnabled)
                    btnAutoPlay.Text = "✅ 🎮 AutoPlay+NM";
            }
            else
            {
                setBtn(btnAutoPlay,    "🎮 AutoPlay",    ModMenu.AutoPlayEnabled);
                setBtn(btnNoMiss,      "💚 NoMiss",      nm);
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
                ? Color.Argb(240, 25, 160, 70)
                : Color.Argb(220, 28, 28, 42));
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
                        o.isDragging = false;
                        return true;

                    case MotionEventActions.Move:
                        float dx = e.RawX - o.dragStartX;
                        float dy = e.RawY - o.dragStartY;
                        if (!o.isDragging && Math.Abs(dx) < ModMenuOverlay.DRAG_THRESHOLD_PX && Math.Abs(dy) < ModMenuOverlay.DRAG_THRESHOLD_PX)
                            return true;
                        o.isDragging = true;
                        o.menuLayoutParams.X = o.dragStartWinX + (int)dx;
                        o.menuLayoutParams.Y = o.dragStartWinY + (int)dy;
                        try { o.windowManager.UpdateViewLayout(o.rootLayout, o.menuLayoutParams); }
                        catch { }
                        return true;
                }
                return false;
            }
        }
    }
}
