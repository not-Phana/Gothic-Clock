using System;
using System.IO;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Windows.Markup;
using Microsoft.Win32;

namespace GothicClock
{
    // =========================================================================
    // 0. REUSABLE ROLLING TICKER BOX (SLIDE DOWN AND ENTER FROM TOP)
    // =========================================================================
    public class RollingBox
    {
        public Grid Container;
        public TextBlock TxtCurrent;
        public TextBlock TxtIncoming;
        public TranslateTransform TransCurrent;
        public TranslateTransform TransIncoming;
        private string lastText = "";
        public string MotionStyle = "Spring";

        public RollingBox(System.Windows.Media.FontFamily font, double fontSize, SolidColorBrush brush, DropShadowEffect effect)
        {
            Container = new Grid();
            Container.ClipToBounds = true;
            Container.VerticalAlignment = VerticalAlignment.Center;
            Container.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            Container.Height = fontSize * 1.35;

            TransCurrent = new TranslateTransform();
            TxtCurrent = new TextBlock();
            TxtCurrent.FontFamily = font;
            TxtCurrent.FontSize = fontSize;
            TxtCurrent.Foreground = brush;
            TxtCurrent.TextAlignment = TextAlignment.Center;
            TxtCurrent.VerticalAlignment = VerticalAlignment.Center;
            TxtCurrent.RenderTransform = TransCurrent;
            TxtCurrent.Effect = effect;

            TransIncoming = new TranslateTransform();
            TxtIncoming = new TextBlock();
            TxtIncoming.FontFamily = font;
            TxtIncoming.FontSize = fontSize;
            TxtIncoming.Foreground = brush;
            TxtIncoming.TextAlignment = TextAlignment.Center;
            TxtIncoming.VerticalAlignment = VerticalAlignment.Center;
            TxtIncoming.RenderTransform = TransIncoming;
            TxtIncoming.Opacity = 0;
            TxtIncoming.Effect = effect;

            Container.Children.Add(TxtCurrent);
            Container.Children.Add(TxtIncoming);
        }

        public void ApplyStyle(SolidColorBrush textBrush, System.Windows.Media.Color glowColor, double glowRadius, double shadowDepth, double opacity, string motionStyle)
        {
            MotionStyle = motionStyle;
            TxtCurrent.Foreground = textBrush;
            TxtIncoming.Foreground = textBrush;

            DropShadowEffect e1 = new DropShadowEffect();
            e1.Color = glowColor;
            e1.BlurRadius = glowRadius;
            e1.ShadowDepth = shadowDepth;
            e1.Opacity = opacity;
            e1.Direction = 315;
            TxtCurrent.Effect = e1;

            DropShadowEffect e2 = new DropShadowEffect();
            e2.Color = glowColor;
            e2.BlurRadius = glowRadius;
            e2.ShadowDepth = shadowDepth;
            e2.Opacity = opacity;
            e2.Direction = 315;
            TxtIncoming.Effect = e2;
        }

        public void SetFontSize(double size)
        {
            TxtCurrent.FontSize = size;
            TxtIncoming.FontSize = size;
            Container.Height = size * 1.35;
        }

        public void Update(string newText, bool animate, int durationMs)
        {
            if (newText == lastText) return;

            if (string.IsNullOrEmpty(lastText) || !animate)
            {
                TxtCurrent.Text = newText;
                TransCurrent.Y = 0;
                TxtCurrent.Opacity = 1;
                TxtIncoming.Opacity = 0;
                lastText = newText;
                return;
            }

            Animate(lastText, newText, durationMs);
            lastText = newText;
        }

        public void ForceRoll(int durationMs)
        {
            if (!string.IsNullOrEmpty(lastText))
            {
                Animate(lastText, lastText, durationMs);
            }
        }

        private void Animate(string oldVal, string newVal, int durationMs)
        {
            // Clear any active animations to avoid conflict
            TransCurrent.BeginAnimation(TranslateTransform.YProperty, null);
            TxtCurrent.BeginAnimation(UIElement.OpacityProperty, null);
            TransIncoming.BeginAnimation(TranslateTransform.YProperty, null);
            TxtIncoming.BeginAnimation(UIElement.OpacityProperty, null);

            TxtCurrent.Text = oldVal;
            TxtIncoming.Text = newVal;

            double travelDistance = TxtCurrent.FontSize * 0.95;

            TransCurrent.Y = 0;
            TransIncoming.Y = -travelDistance;

            TxtCurrent.Opacity = 1.0;
            TxtIncoming.Opacity = 0.0;

            Duration dur = new Duration(TimeSpan.FromMilliseconds(durationMs));
            IEasingFunction ease;
            if (MotionStyle == "Spring")
            {
                ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 };
            }
            else if (MotionStyle == "Bounce")
            {
                ease = new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 1, Bounciness = 2 };
            }
            else if (MotionStyle == "Linear")
            {
                ease = null;
            }
            else
            {
                ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            }

            DoubleAnimation animOutY = new DoubleAnimation(0, travelDistance, dur) { EasingFunction = ease };
            DoubleAnimation animOutOp = new DoubleAnimation(1.0, 0.0, dur) { EasingFunction = ease };

            DoubleAnimation animInY = new DoubleAnimation(-travelDistance, 0, dur) { EasingFunction = ease };
            DoubleAnimation animInOp = new DoubleAnimation(0.0, 1.0, dur) { EasingFunction = ease };

            animInY.Completed += delegate {
                try
                {
                    TransCurrent.BeginAnimation(TranslateTransform.YProperty, null);
                    TxtCurrent.BeginAnimation(UIElement.OpacityProperty, null);
                    TransIncoming.BeginAnimation(TranslateTransform.YProperty, null);
                    TxtIncoming.BeginAnimation(UIElement.OpacityProperty, null);

                    TransCurrent.Y = 0;
                    TxtCurrent.Opacity = 1.0;
                    TxtCurrent.Text = newVal;

                    TransIncoming.Y = -travelDistance;
                    TxtIncoming.Opacity = 0.0;
                }
                catch { }
            };

            TransCurrent.BeginAnimation(TranslateTransform.YProperty, animOutY);
            TxtCurrent.BeginAnimation(UIElement.OpacityProperty, animOutOp);

            TransIncoming.BeginAnimation(TranslateTransform.YProperty, animInY);
            TxtIncoming.BeginAnimation(UIElement.OpacityProperty, animInOp);
        }
    }

    // =========================================================================
    // 1. DESKTOP CLOCK WIDGET WINDOW (FULL ROLLING TICKER ON ALL ELEMENTS)
    // =========================================================================
    public class WidgetWindow : Window
    {
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int WM_SHOWWINDOW = 0x0018;
        private const int MA_NOACTIVATE = 3;
        private const int SC_MINIMIZE = 0xF020;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_HIDEWINDOW = 0x0080;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private const int GWL_EXSTYLE = -20;
        private const int GWLP_HWNDPARENT = -8;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // Rolling elements for Time line
        private StackPanel timePanel;
        public RollingBox BoxHour;
        private TextBlock txtColon1;
        public RollingBox BoxMin;
        private TextBlock txtColon2;
        public RollingBox BoxSec;
        public RollingBox BoxAmPm;

        // Rolling elements for Day and Date
        public RollingBox BoxDay;
        public RollingBox BoxDate;

        private StackPanel stack;
        private Border mainBorder;
        private ScaleTransform scaleTransform;
        private DispatcherTimer timer;

        // Settings
        public bool ShowTime = true;
        public bool ShowDay = true;
        public bool ShowDate = true;

        public double TimeFontSize = 28;
        public double DayFontSize = 14;
        public double DateFontSize = 13;
        public double LineSpacing = 2;

        public bool Is12Hour = true;
        public bool ShowSeconds = true;
        public bool ShowTodayPrefix = true;
        public string DateFormat = "MM/dd/yyyy";

        public bool EnableAnimation = true;
        public int AnimationDurationMs = 320;

        public double MasterScale = 0.85;
        public bool IsMoveMode = false;
        public bool StayOnDesktop = true;

        // Gothic Visual FX & Aura
        public string GlowTheme = "Moonlight";
        public double GlowRadius = 16;
        public double GlowOpacity = 0.85;
        public string MotionStyle = "Spring";
        public string ColonStyle = "Breath";
        public bool ShowDivider = true;
        public bool ShowGlassBackplate = false;

        private Grid dividerGrid;
        private System.Windows.Shapes.Polygon diamondCenter;
        private System.Windows.Shapes.Rectangle lineLeft;
        private System.Windows.Shapes.Rectangle lineRight;

        public WidgetWindow()
        {
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = System.Windows.Media.Brushes.Transparent;
            this.ShowInTaskbar = false;
            this.Topmost = false;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.ResizeMode = ResizeMode.NoResize;
            this.Title = "Gothic Clock Widget";

            BuildUI();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += delegate { UpdateClock(); };
            timer.Start();

            UpdateClock();

            this.Loaded += delegate {
                AttachToDesktop();
            };
        }

        private void BuildDivider()
        {
            dividerGrid = new Grid();
            dividerGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            dividerGrid.Height = 14;
            dividerGrid.Width = 240;
            dividerGrid.Margin = new Thickness(0, LineSpacing, 0, LineSpacing);

            lineLeft = new System.Windows.Shapes.Rectangle();
            lineLeft.Height = 1.2;
            lineLeft.Width = 100;
            lineLeft.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            lineLeft.VerticalAlignment = VerticalAlignment.Center;

            lineRight = new System.Windows.Shapes.Rectangle();
            lineRight.Height = 1.2;
            lineRight.Width = 100;
            lineRight.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            lineRight.VerticalAlignment = VerticalAlignment.Center;

            diamondCenter = new System.Windows.Shapes.Polygon();
            PointCollection pts = new PointCollection();
            pts.Add(new System.Windows.Point(5, 0));
            pts.Add(new System.Windows.Point(10, 5));
            pts.Add(new System.Windows.Point(5, 10));
            pts.Add(new System.Windows.Point(0, 5));
            diamondCenter.Points = pts;
            diamondCenter.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            diamondCenter.VerticalAlignment = VerticalAlignment.Center;

            dividerGrid.Children.Add(lineLeft);
            dividerGrid.Children.Add(diamondCenter);
            dividerGrid.Children.Add(lineRight);
        }

        public void UpdateDividerColors(SolidColorBrush textBrush, System.Windows.Media.Color glowColor)
        {
            if (lineLeft == null || lineRight == null || diamondCenter == null) return;

            LinearGradientBrush brushLeft = new LinearGradientBrush();
            brushLeft.StartPoint = new System.Windows.Point(0, 0.5);
            brushLeft.EndPoint = new System.Windows.Point(1, 0.5);
            brushLeft.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0, glowColor.R, glowColor.G, glowColor.B), 0.0));
            brushLeft.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(210, glowColor.R, glowColor.G, glowColor.B), 1.0));
            lineLeft.Fill = brushLeft;

            LinearGradientBrush brushRight = new LinearGradientBrush();
            brushRight.StartPoint = new System.Windows.Point(0, 0.5);
            brushRight.EndPoint = new System.Windows.Point(1, 0.5);
            brushRight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(210, glowColor.R, glowColor.G, glowColor.B), 0.0));
            brushRight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0, glowColor.R, glowColor.G, glowColor.B), 1.0));
            lineRight.Fill = brushRight;

            diamondCenter.Fill = textBrush;
            DropShadowEffect eff = new DropShadowEffect();
            eff.Color = glowColor;
            eff.BlurRadius = 8;
            eff.ShadowDepth = 0;
            eff.Opacity = 0.9;
            diamondCenter.Effect = eff;
        }

        private void BuildUI()
        {
            System.Windows.Media.FontFamily gothicFont = new System.Windows.Media.FontFamily("Old English Text MT, Cloister Black, Georgia, Times New Roman, serif");
            SolidColorBrush textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(242, 245, 245));

            DropShadowEffect shadow = new DropShadowEffect();
            shadow.Color = Colors.Black;
            shadow.BlurRadius = 3;
            shadow.ShadowDepth = 1.2;
            shadow.Direction = 315;
            shadow.Opacity = 0.85;

            stack = new StackPanel();
            stack.Orientation = System.Windows.Controls.Orientation.Vertical;
            stack.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            stack.Margin = new Thickness(4);

            // =================================================================
            // TIME LINE WITH INDEPENDENT ROLLING BOXES (Hour : Min : Sec AM/PM)
            // =================================================================
            timePanel = new StackPanel();
            timePanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
            timePanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

            BoxHour = new RollingBox(gothicFont, TimeFontSize, textBrush, shadow);

            txtColon1 = new TextBlock();
            txtColon1.FontFamily = gothicFont;
            txtColon1.FontSize = TimeFontSize;
            txtColon1.Foreground = textBrush;
            txtColon1.Text = ":";
            txtColon1.VerticalAlignment = VerticalAlignment.Center;
            txtColon1.Effect = shadow;

            BoxMin = new RollingBox(gothicFont, TimeFontSize, textBrush, shadow);

            txtColon2 = new TextBlock();
            txtColon2.FontFamily = gothicFont;
            txtColon2.FontSize = TimeFontSize;
            txtColon2.Foreground = textBrush;
            txtColon2.Text = ":";
            txtColon2.VerticalAlignment = VerticalAlignment.Center;
            txtColon2.Effect = shadow;

            BoxSec = new RollingBox(gothicFont, TimeFontSize, textBrush, shadow);
            BoxAmPm = new RollingBox(gothicFont, TimeFontSize, textBrush, shadow);
            BoxAmPm.Container.Margin = new Thickness(6, 0, 0, 0);

            timePanel.Children.Add(BoxHour.Container);
            timePanel.Children.Add(txtColon1);
            timePanel.Children.Add(BoxMin.Container);
            timePanel.Children.Add(txtColon2);
            timePanel.Children.Add(BoxSec.Container);
            timePanel.Children.Add(BoxAmPm.Container);

            // Ornate Divider
            BuildDivider();

            // =================================================================
            // DAY & DATE WITH ROLLING TICKER SUPPORT
            // =================================================================
            BoxDay = new RollingBox(gothicFont, DayFontSize, textBrush, shadow);
            BoxDay.Container.Margin = new Thickness(0, LineSpacing, 0, LineSpacing);

            BoxDate = new RollingBox(gothicFont, DateFontSize, textBrush, shadow);

            stack.Children.Add(timePanel);
            stack.Children.Add(dividerGrid);
            stack.Children.Add(BoxDay.Container);
            stack.Children.Add(BoxDate.Container);

            mainBorder = new Border();
            mainBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(1, 255, 255, 255));
            mainBorder.Padding = new Thickness(8, 4, 8, 4);
            mainBorder.CornerRadius = new CornerRadius(4);
            mainBorder.BorderThickness = new Thickness(1);
            mainBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;

            scaleTransform = new ScaleTransform(MasterScale, MasterScale);
            mainBorder.LayoutTransform = scaleTransform;
            mainBorder.Child = stack;

            this.Content = mainBorder;

            mainBorder.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) {
                if (IsMoveMode && e.ButtonState == MouseButtonState.Pressed)
                {
                    try { this.DragMove(); } catch { }
                }
            };
        }

        public void SetGlowTheme(string theme)
        {
            GlowTheme = theme;
            SolidColorBrush textBrush;
            System.Windows.Media.Color glowColor;

            if (theme == "Crimson")
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 240, 243));
                glowColor = System.Windows.Media.Color.FromRgb(255, 26, 64);
            }
            else if (theme == "Violet")
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 245, 255));
                glowColor = System.Windows.Media.Color.FromRgb(168, 85, 247);
            }
            else if (theme == "Cyan")
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 253, 255));
                glowColor = System.Windows.Media.Color.FromRgb(0, 229, 255);
            }
            else if (theme == "Gold")
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 253, 240));
                glowColor = System.Windows.Media.Color.FromRgb(255, 184, 0);
            }
            else if (theme == "Stealth")
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(242, 245, 245));
                glowColor = Colors.Black;
            }
            else // "Moonlight"
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                glowColor = System.Windows.Media.Color.FromRgb(150, 180, 215);
            }

            double depth = (theme == "Stealth") ? 1.5 : 0;
            double op = (theme == "Stealth") ? 0.9 : GlowOpacity;
            double rad = (theme == "Stealth" && GlowRadius > 6) ? 3 : GlowRadius;

            BoxHour.ApplyStyle(textBrush, glowColor, rad, depth, op, MotionStyle);
            BoxMin.ApplyStyle(textBrush, glowColor, rad, depth, op, MotionStyle);
            BoxSec.ApplyStyle(textBrush, glowColor, rad, depth, op, MotionStyle);
            BoxAmPm.ApplyStyle(textBrush, glowColor, rad, depth, op, MotionStyle);
            BoxDay.ApplyStyle(textBrush, glowColor, rad, depth, op, MotionStyle);
            BoxDate.ApplyStyle(textBrush, glowColor, rad, depth, op, MotionStyle);

            txtColon1.Foreground = textBrush;
            DropShadowEffect cEff1 = new DropShadowEffect();
            cEff1.Color = glowColor;
            cEff1.BlurRadius = rad;
            cEff1.ShadowDepth = depth;
            cEff1.Opacity = op;
            txtColon1.Effect = cEff1;

            txtColon2.Foreground = textBrush;
            DropShadowEffect cEff2 = new DropShadowEffect();
            cEff2.Color = glowColor;
            cEff2.BlurRadius = rad;
            cEff2.ShadowDepth = depth;
            cEff2.Opacity = op;
            txtColon2.Effect = cEff2;

            UpdateDividerColors(textBrush, glowColor);
        }

        public void UpdateColonAnimation()
        {
            if (ColonStyle == "Breath")
            {
                DoubleAnimation anim = new DoubleAnimation(1.0, 0.25, new Duration(TimeSpan.FromMilliseconds(550)));
                anim.AutoReverse = true;
                anim.RepeatBehavior = RepeatBehavior.Forever;
                anim.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };

                txtColon1.BeginAnimation(UIElement.OpacityProperty, anim);
                txtColon2.BeginAnimation(UIElement.OpacityProperty, anim);
            }
            else
            {
                txtColon1.BeginAnimation(UIElement.OpacityProperty, null);
                txtColon2.BeginAnimation(UIElement.OpacityProperty, null);
                txtColon1.Opacity = 1.0;
                txtColon2.Opacity = 1.0;
            }
        }

        public void SetGlassPlate(bool enable)
        {
            ShowGlassBackplate = enable;
            if (enable)
            {
                mainBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(145, 14, 16, 22));
                mainBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(55, 255, 255, 255));
                mainBorder.CornerRadius = new CornerRadius(12);
                mainBorder.Padding = new Thickness(22, 12, 22, 14);
            }
            else
            {
                mainBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(1, 255, 255, 255));
                mainBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
                mainBorder.CornerRadius = new CornerRadius(4);
                mainBorder.Padding = new Thickness(8, 4, 8, 4);
            }
        }

        public void RefreshAppearance()
        {
            timePanel.Visibility = ShowTime ? Visibility.Visible : Visibility.Collapsed;
            dividerGrid.Visibility = ShowDivider ? Visibility.Visible : Visibility.Collapsed;
            BoxDay.Container.Visibility = ShowDay ? Visibility.Visible : Visibility.Collapsed;
            BoxDate.Container.Visibility = ShowDate ? Visibility.Visible : Visibility.Collapsed;

            BoxHour.SetFontSize(TimeFontSize);
            txtColon1.FontSize = TimeFontSize;
            BoxMin.SetFontSize(TimeFontSize);
            txtColon2.FontSize = TimeFontSize;
            BoxSec.SetFontSize(TimeFontSize);
            BoxAmPm.SetFontSize(TimeFontSize);

            txtColon2.Visibility = ShowSeconds ? Visibility.Visible : Visibility.Collapsed;
            BoxSec.Container.Visibility = ShowSeconds ? Visibility.Visible : Visibility.Collapsed;
            BoxAmPm.Container.Visibility = Is12Hour ? Visibility.Visible : Visibility.Collapsed;

            BoxDay.SetFontSize(DayFontSize);
            BoxDate.SetFontSize(DateFontSize);

            dividerGrid.Margin = new Thickness(0, LineSpacing, 0, LineSpacing);
            BoxDay.Container.Margin = new Thickness(0, LineSpacing, 0, LineSpacing);

            scaleTransform.ScaleX = MasterScale;
            scaleTransform.ScaleY = MasterScale;

            SetGlassPlate(ShowGlassBackplate);
            SetGlowTheme(GlowTheme);
            UpdateColonAnimation();

            UpdateClock();
        }

        public void ForceRollAll()
        {
            BoxHour.ForceRoll(AnimationDurationMs);
            BoxMin.ForceRoll(AnimationDurationMs);
            BoxSec.ForceRoll(AnimationDurationMs);
            BoxAmPm.ForceRoll(AnimationDurationMs);
            BoxDay.ForceRoll(AnimationDurationMs);
            BoxDate.ForceRoll(AnimationDurationMs);
        }

        public void SetMoveMode(bool enable)
        {
            IsMoveMode = enable;
            if (enable)
            {
                mainBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(220, 59, 130, 246));
                mainBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 0, 0, 0));
                this.Cursor = System.Windows.Input.Cursors.SizeAll;
                this.Topmost = true;
            }
            else
            {
                SetGlassPlate(ShowGlassBackplate);
                this.Cursor = System.Windows.Input.Cursors.Arrow;
                this.Topmost = false;
                AttachToDesktop();
            }
        }

        public void AttachToDesktop()
        {
            if (StayOnDesktop && !IsMoveMode)
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    IntPtr progman = FindWindow("Progman", null);
                    if (progman != IntPtr.Zero)
                    {
                        try { SetWindowLongPtr(hwnd, GWLP_HWNDPARENT, progman); } catch { }
                    }
                    SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            }
        }

        private void UpdateClock()
        {
            DateTime now = DateTime.Now;

            // Colon blink mode
            if (ColonStyle == "Blink")
            {
                double op = (now.Second % 2 == 0) ? 1.0 : 0.0;
                txtColon1.Opacity = op;
                txtColon2.Opacity = op;
            }

            // 1. Time Update
            if (ShowTime)
            {
                string hourStr = Is12Hour ? now.ToString("hh") : now.ToString("HH");
                string minStr = now.ToString("mm");
                string secStr = now.ToString("ss");
                string amPmStr = Is12Hour ? now.ToString("tt") : "";

                BoxHour.Update(hourStr, EnableAnimation, AnimationDurationMs);
                BoxMin.Update(minStr, EnableAnimation, AnimationDurationMs);

                if (ShowSeconds)
                {
                    BoxSec.Update(secStr, EnableAnimation, AnimationDurationMs);
                }

                if (Is12Hour)
                {
                    BoxAmPm.Update(amPmStr, EnableAnimation, AnimationDurationMs);
                }
            }

            // 2. Day Update
            if (ShowDay)
            {
                string dayName = now.ToString("dddd");
                string dayText = ShowTodayPrefix ? ("Today is " + dayName) : dayName;
                BoxDay.Update(dayText, EnableAnimation, AnimationDurationMs);
            }

            // 3. Date Update
            if (ShowDate)
            {
                string dateText = now.ToString(DateFormat);
                BoxDate.Update(dateText, EnableAnimation, AnimationDurationMs);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            if (source != null)
            {
                source.AddHook(HwndHook);
            }

            AttachToDesktop();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE && !IsMoveMode)
            {
                handled = true;
                return new IntPtr(MA_NOACTIVATE);
            }

            if (msg == WM_SHOWWINDOW && (int)wParam == 0 && StayOnDesktop)
            {
                handled = true;
                return IntPtr.Zero;
            }

            if (msg == WM_WINDOWPOSCHANGING)
            {
                WINDOWPOS pos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                if (pos.x == -32000 || pos.y == -32000 || (pos.flags & SWP_HIDEWINDOW) != 0)
                {
                    pos.flags |= (SWP_NOMOVE | SWP_NOSIZE);
                    pos.flags &= ~SWP_HIDEWINDOW;
                    pos.flags |= SWP_SHOWWINDOW;
                }

                if (StayOnDesktop && !IsMoveMode)
                {
                    pos.hwndInsertAfter = HWND_BOTTOM;
                    pos.flags &= ~0x0004u;
                }

                Marshal.StructureToPtr(pos, lParam, true);
                handled = true;
            }
            else if (msg == WM_SYSCOMMAND)
            {
                int sc = (wParam.ToInt32() & 0xFFF0);
                if (sc == SC_MINIMIZE && StayOnDesktop)
                {
                    handled = true;
                    return IntPtr.Zero;
                }
            }

            return IntPtr.Zero;
        }
    }

    // =========================================================================
    // 2. MODERN CONTROL PANEL WITH FULL ROLLING ANIMATION CONTROLS
    // =========================================================================
    public class ControlPanelWindow : Window
    {
        private WidgetWindow widget;
        private NotifyIcon trayIcon;
        private string configPath;

        private System.Windows.Controls.Button btnToggleAll;
        private System.Windows.Controls.Button btnMoveMode;
        private System.Windows.Controls.Button btnPreviewAnim;

        // Master scale & spacing
        private System.Windows.Controls.Slider sliderMasterScale;
        private TextBlock txtMasterScaleVal;
        private System.Windows.Controls.Slider sliderSpacing;
        private TextBlock txtSpacingVal;

        // Master Animation Settings
        private System.Windows.Controls.CheckBox chkEnableAnim;
        private System.Windows.Controls.Slider sliderAnimSpeed;
        private TextBlock txtAnimSpeedVal;

        // Card: Gothic Aura & Visual Effects
        private System.Windows.Controls.Slider sliderGlowRadius;
        private TextBlock txtGlowRadiusVal;
        private System.Windows.Controls.ComboBox cmbMotionStyle;
        private System.Windows.Controls.ComboBox cmbColonStyle;
        private System.Windows.Controls.CheckBox chkShowDivider;
        private System.Windows.Controls.CheckBox chkGlassPlate;

        // Element 1: Time
        private System.Windows.Controls.CheckBox chkShowTime;
        private System.Windows.Controls.Slider sliderTimeSize;
        private TextBlock txtTimeSizeVal;
        private System.Windows.Controls.CheckBox chk12Hour;
        private System.Windows.Controls.CheckBox chkShowSeconds;

        // Element 2: Day
        private System.Windows.Controls.CheckBox chkShowDay;
        private System.Windows.Controls.Slider sliderDaySize;
        private TextBlock txtDaySizeVal;
        private System.Windows.Controls.CheckBox chkTodayPrefix;

        // Element 3: Date
        private System.Windows.Controls.CheckBox chkShowDate;
        private System.Windows.Controls.Slider sliderDateSize;
        private TextBlock txtDateSizeVal;
        private System.Windows.Controls.ComboBox cmbDateFormat;

        // System
        private System.Windows.Controls.CheckBox chkAutoStart;
        private System.Windows.Controls.CheckBox chkStayDesktop;

        private bool isMasterActive = true;

        public ControlPanelWindow(WidgetWindow widgetWin)
        {
            this.widget = widgetWin;

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            configPath = Path.Combine(appDir, "settings.ini");

            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = System.Windows.Media.Brushes.Transparent;
            this.Title = "Gothic Clock - Control Panel";
            this.Width = 470;
            this.Height = 740;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.ResizeMode = ResizeMode.CanMinimize;

            LoadCustomStyles();
            BuildUI();
            LoadConfig();
            SetupTray();

            this.Closing += delegate(object s, System.ComponentModel.CancelEventArgs e) {
                e.Cancel = true;
                this.Hide();
            };
        }

        private void LoadCustomStyles()
        {
            string xaml = @"<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">

                <Style TargetType=""ScrollBar"">
                    <Setter Property=""Background"" Value=""Transparent"" />
                    <Setter Property=""Width"" Value=""6"" />
                    <Setter Property=""Template"">
                        <Setter.Value>
                            <ControlTemplate TargetType=""ScrollBar"">
                                <Grid Background=""Transparent"">
                                    <Track x:Name=""PART_Track"" IsDirectionReversed=""True"">
                                        <Track.Thumb>
                                            <Thumb>
                                                <Thumb.Template>
                                                    <ControlTemplate TargetType=""Thumb"">
                                                        <Border Background=""#3E4452"" CornerRadius=""3"" />
                                                    </ControlTemplate>
                                                </Thumb.Template>
                                            </Thumb>
                                        </Track.Thumb>
                                    </Track>
                                </Grid>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>

                <Style TargetType=""Slider"">
                    <Setter Property=""Height"" Value=""24"" />
                    <Setter Property=""Template"">
                        <Setter.Value>
                            <ControlTemplate TargetType=""Slider"">
                                <Grid VerticalAlignment=""Center"">
                                    <Border Height=""5"" Background=""#262936"" CornerRadius=""2.5"" />
                                    <Track x:Name=""PART_Track"">
                                        <Track.Thumb>
                                            <Thumb Cursor=""Hand"">
                                                <Thumb.Template>
                                                    <ControlTemplate TargetType=""Thumb"">
                                                        <Grid Width=""16"" Height=""16"">
                                                            <Ellipse Fill=""#FFFFFF"" Stroke=""#3B82F6"" StrokeThickness=""2.5"" />
                                                        </Grid>
                                                    </ControlTemplate>
                                                </Thumb.Template>
                                            </Thumb>
                                        </Track.Thumb>
                                    </Track>
                                </Grid>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
            </ResourceDictionary>";

            try
            {
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
                {
                    ResourceDictionary rd = (ResourceDictionary)XamlReader.Load(ms);
                    this.Resources.MergedDictionaries.Add(rd);
                }
            }
            catch { }
        }

        private void BuildUI()
        {
            Border windowBorder = new Border();
            windowBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 21, 26));
            windowBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 48, 60));
            windowBorder.BorderThickness = new Thickness(1);
            windowBorder.CornerRadius = new CornerRadius(10);
            windowBorder.ClipToBounds = true;

            DropShadowEffect winShadow = new DropShadowEffect();
            winShadow.Color = Colors.Black;
            winShadow.BlurRadius = 15;
            winShadow.ShadowDepth = 3;
            winShadow.Opacity = 0.65;
            windowBorder.Effect = winShadow;

            DockPanel mainDock = new DockPanel();

            // 1. Dark Modern Title Bar
            Grid titleBar = new Grid();
            titleBar.Height = 36;
            titleBar.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 16, 20));
            DockPanel.SetDock(titleBar, Dock.Top);

            titleBar.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    try { this.DragMove(); } catch { }
                }
            };

            StackPanel spTitleLeft = new StackPanel();
            spTitleLeft.Orientation = System.Windows.Controls.Orientation.Horizontal;
            spTitleLeft.VerticalAlignment = VerticalAlignment.Center;
            spTitleLeft.Margin = new Thickness(12, 0, 0, 0);

            TextBlock tbTitle = new TextBlock();
            tbTitle.Text = "Gothic Clock - Control Panel";
            tbTitle.FontSize = 12;
            tbTitle.FontWeight = FontWeights.SemiBold;
            tbTitle.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 208, 220));
            spTitleLeft.Children.Add(tbTitle);
            titleBar.Children.Add(spTitleLeft);

            StackPanel spTitleRight = new StackPanel();
            spTitleRight.Orientation = System.Windows.Controls.Orientation.Horizontal;
            spTitleRight.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;

            System.Windows.Controls.Button btnMin = CreateTitleButton("—", false);
            btnMin.Click += delegate { this.WindowState = WindowState.Minimized; };

            System.Windows.Controls.Button btnClose = CreateTitleButton("✕", true);
            btnClose.Click += delegate { this.Hide(); };

            spTitleRight.Children.Add(btnMin);
            spTitleRight.Children.Add(btnClose);
            titleBar.Children.Add(spTitleRight);

            mainDock.Children.Add(titleBar);

            // 2. Scrollable Body
            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.Background = System.Windows.Media.Brushes.Transparent;
            scroll.BorderThickness = new Thickness(0);

            StackPanel content = new StackPanel();
            content.Margin = new Thickness(16, 12, 16, 16);

            // Header Banner
            TextBlock txtTitle = new TextBlock();
            txtTitle.Text = "GOTHIC CLOCK";
            txtTitle.FontFamily = new System.Windows.Media.FontFamily("Old English Text MT, Georgia, serif");
            txtTitle.FontSize = 25;
            txtTitle.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 240, 248));
            txtTitle.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

            TextBlock txtSub = new TextBlock();
            txtSub.Text = "Universal Rolling Animation (All Elements) & Control Center";
            txtSub.FontSize = 11;
            txtSub.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(130, 140, 155));
            txtSub.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            txtSub.Margin = new Thickness(0, 2, 0, 14);

            content.Children.Add(txtTitle);
            content.Children.Add(txtSub);

            // =================================================================
            // CARD 1: MASTER CONTROLS & UNIVERSAL ROLLING ANIMATION
            // =================================================================
            Border cardMaster = CreateCard();
            StackPanel spMaster = new StackPanel();

            TextBlock lblMaster = CreateCardHeader("Master Controls & Universal Animation");

            btnToggleAll = CreateStyledButton("All Elements: ACTIVE (ON)", System.Windows.Media.Color.FromRgb(30, 120, 60));
            btnToggleAll.Margin = new Thickness(0, 0, 0, 8);
            btnToggleAll.Click += delegate {
                isMasterActive = !isMasterActive;
                if (isMasterActive)
                {
                    widget.Show();
                    widget.AttachToDesktop();
                    btnToggleAll.Content = "All Elements: ACTIVE (ON)";
                    btnToggleAll.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 120, 60));
                }
                else
                {
                    widget.Hide();
                    btnToggleAll.Content = "All Elements: STOPPED (OFF)";
                    btnToggleAll.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 75, 85));
                }
                SaveConfig();
            };

            // Universal Animation Toggles
            chkEnableAnim = CreateCheckBox("Slide-Down Animation on ALL Elements (Time, Day & Date)", true);
            chkEnableAnim.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            chkEnableAnim.FontWeight = FontWeights.SemiBold;
            chkEnableAnim.Click += delegate {
                widget.EnableAnimation = chkEnableAnim.IsChecked == true;
                SaveConfig();
            };

            btnPreviewAnim = CreateStyledButton("Test / Trigger Rolling Animation on All Elements", System.Windows.Media.Color.FromRgb(35, 75, 135));
            btnPreviewAnim.Margin = new Thickness(0, 4, 0, 10);
            btnPreviewAnim.Click += delegate {
                widget.ForceRollAll();
            };

            // Animation Speed
            TextBlock lblAnimSpeed = CreateFieldLabel("Animation Roll Speed (ms):");
            DockPanel dpAnimSpeed = new DockPanel();
            dpAnimSpeed.Margin = new Thickness(0, 0, 0, 8);

            txtAnimSpeedVal = new TextBlock();
            txtAnimSpeedVal.Text = "320ms";
            txtAnimSpeedVal.Width = 46;
            txtAnimSpeedVal.TextAlignment = TextAlignment.Right;
            txtAnimSpeedVal.VerticalAlignment = VerticalAlignment.Center;
            txtAnimSpeedVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 190, 205));
            DockPanel.SetDock(txtAnimSpeedVal, Dock.Right);

            sliderAnimSpeed = new System.Windows.Controls.Slider();
            sliderAnimSpeed.Minimum = 150;
            sliderAnimSpeed.Maximum = 600;
            sliderAnimSpeed.Value = 320;
            sliderAnimSpeed.TickFrequency = 10;
            sliderAnimSpeed.IsSnapToTickEnabled = true;
            sliderAnimSpeed.Margin = new Thickness(0, 0, 10, 0);
            sliderAnimSpeed.ValueChanged += delegate {
                widget.AnimationDurationMs = (int)Math.Round(sliderAnimSpeed.Value);
                txtAnimSpeedVal.Text = widget.AnimationDurationMs + "ms";
                SaveConfig();
            };

            dpAnimSpeed.Children.Add(txtAnimSpeedVal);
            dpAnimSpeed.Children.Add(sliderAnimSpeed);

            // Master Scale Slider
            TextBlock lblScale = CreateFieldLabel("Overall Master Scale (Resize Everything):");
            DockPanel dpMasterScale = new DockPanel();
            dpMasterScale.Margin = new Thickness(0, 0, 0, 8);

            txtMasterScaleVal = new TextBlock();
            txtMasterScaleVal.Text = "85%";
            txtMasterScaleVal.Width = 46;
            txtMasterScaleVal.TextAlignment = TextAlignment.Right;
            txtMasterScaleVal.VerticalAlignment = VerticalAlignment.Center;
            txtMasterScaleVal.FontWeight = FontWeights.Bold;
            txtMasterScaleVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            DockPanel.SetDock(txtMasterScaleVal, Dock.Right);

            sliderMasterScale = new System.Windows.Controls.Slider();
            sliderMasterScale.Minimum = 0.20;
            sliderMasterScale.Maximum = 3.00;
            sliderMasterScale.Value = 0.85;
            sliderMasterScale.TickFrequency = 0.05;
            sliderMasterScale.IsSnapToTickEnabled = true;
            sliderMasterScale.Margin = new Thickness(0, 0, 10, 0);
            sliderMasterScale.ValueChanged += delegate {
                widget.MasterScale = Math.Round(sliderMasterScale.Value, 2);
                txtMasterScaleVal.Text = Math.Round(widget.MasterScale * 100) + "%";
                widget.RefreshAppearance();
                SaveConfig();
            };

            dpMasterScale.Children.Add(txtMasterScaleVal);
            dpMasterScale.Children.Add(sliderMasterScale);

            // Quick Scale Presets
            UniformGrid gridPresets = new UniformGrid();
            gridPresets.Columns = 5;
            gridPresets.Margin = new Thickness(0, 0, 0, 10);

            gridPresets.Children.Add(CreateSmallPresetButton("Tiny (40%)", 0.40));
            gridPresets.Children.Add(CreateSmallPresetButton("Small (65%)", 0.65));
            gridPresets.Children.Add(CreateSmallPresetButton("Norm (85%)", 0.85));
            gridPresets.Children.Add(CreateSmallPresetButton("Large (120%)", 1.20));
            gridPresets.Children.Add(CreateSmallPresetButton("Huge (180%)", 1.80));

            // Line Spacing Slider
            TextBlock lblSpacing = CreateFieldLabel("Vertical Gap / Spacing Between Lines:");
            DockPanel dpSpacing = new DockPanel();

            txtSpacingVal = new TextBlock();
            txtSpacingVal.Text = "2px";
            txtSpacingVal.Width = 46;
            txtSpacingVal.TextAlignment = TextAlignment.Right;
            txtSpacingVal.VerticalAlignment = VerticalAlignment.Center;
            txtSpacingVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 190, 205));
            DockPanel.SetDock(txtSpacingVal, Dock.Right);

            sliderSpacing = new System.Windows.Controls.Slider();
            sliderSpacing.Minimum = 0;
            sliderSpacing.Maximum = 25;
            sliderSpacing.Value = 2;
            sliderSpacing.TickFrequency = 1;
            sliderSpacing.IsSnapToTickEnabled = true;
            sliderSpacing.Margin = new Thickness(0, 0, 10, 0);
            sliderSpacing.ValueChanged += delegate {
                widget.LineSpacing = Math.Round(sliderSpacing.Value);
                txtSpacingVal.Text = widget.LineSpacing + "px";
                widget.RefreshAppearance();
                SaveConfig();
            };

            dpSpacing.Children.Add(txtSpacingVal);
            dpSpacing.Children.Add(sliderSpacing);

            spMaster.Children.Add(lblMaster);
            spMaster.Children.Add(btnToggleAll);
            spMaster.Children.Add(chkEnableAnim);
            spMaster.Children.Add(btnPreviewAnim);
            spMaster.Children.Add(lblAnimSpeed);
            spMaster.Children.Add(dpAnimSpeed);
            spMaster.Children.Add(lblScale);
            spMaster.Children.Add(dpMasterScale);
            spMaster.Children.Add(gridPresets);
            spMaster.Children.Add(lblSpacing);
            spMaster.Children.Add(dpSpacing);
            cardMaster.Child = spMaster;
            content.Children.Add(cardMaster);

            // =================================================================
            // CARD 2: GOTHIC AURA & VISUAL EFFECTS (BLOOM, MOTION PHYSICS, PULSE)
            // =================================================================
            Border cardFX = CreateCard();
            StackPanel spFX = new StackPanel();

            TextBlock lblFXTitle = CreateCardHeader("Gothic Aura & Visual Effects");
            spFX.Children.Add(lblFXTitle);

            TextBlock lblPresets = CreateFieldLabel("Aura Glow & Color Theme Presets:");
            spFX.Children.Add(lblPresets);

            UniformGrid gridGlowThemes = new UniformGrid();
            gridGlowThemes.Columns = 3;
            gridGlowThemes.Margin = new Thickness(0, 0, 0, 10);

            gridGlowThemes.Children.Add(CreateThemeButton("Moonlight", System.Windows.Media.Color.FromRgb(150, 180, 215), "Moonlight"));
            gridGlowThemes.Children.Add(CreateThemeButton("Blood Crimson", System.Windows.Media.Color.FromRgb(255, 26, 64), "Crimson"));
            gridGlowThemes.Children.Add(CreateThemeButton("Phantom Violet", System.Windows.Media.Color.FromRgb(168, 85, 247), "Violet"));
            gridGlowThemes.Children.Add(CreateThemeButton("Eldritch Cyan", System.Windows.Media.Color.FromRgb(0, 229, 255), "Cyan"));
            gridGlowThemes.Children.Add(CreateThemeButton("Cathedral Gold", System.Windows.Media.Color.FromRgb(255, 184, 0), "Gold"));
            gridGlowThemes.Children.Add(CreateThemeButton("Stealth Black", System.Windows.Media.Color.FromRgb(40, 44, 52), "Stealth"));
            spFX.Children.Add(gridGlowThemes);

            // Glow Radius Slider
            TextBlock lblGlowRad = CreateFieldLabel("Aura Bloom / Glow Radius:");
            DockPanel dpGlowRad = new DockPanel();
            dpGlowRad.Margin = new Thickness(0, 0, 0, 8);

            txtGlowRadiusVal = new TextBlock();
            txtGlowRadiusVal.Text = "16px";
            txtGlowRadiusVal.Width = 46;
            txtGlowRadiusVal.TextAlignment = TextAlignment.Right;
            txtGlowRadiusVal.VerticalAlignment = VerticalAlignment.Center;
            txtGlowRadiusVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 190, 205));
            DockPanel.SetDock(txtGlowRadiusVal, Dock.Right);

            sliderGlowRadius = new System.Windows.Controls.Slider();
            sliderGlowRadius.Minimum = 0;
            sliderGlowRadius.Maximum = 40;
            sliderGlowRadius.Value = 16;
            sliderGlowRadius.TickFrequency = 1;
            sliderGlowRadius.IsSnapToTickEnabled = true;
            sliderGlowRadius.Margin = new Thickness(0, 0, 10, 0);
            sliderGlowRadius.ValueChanged += delegate {
                widget.GlowRadius = Math.Round(sliderGlowRadius.Value);
                txtGlowRadiusVal.Text = widget.GlowRadius + "px";
                widget.SetGlowTheme(widget.GlowTheme);
                SaveConfig();
            };

            dpGlowRad.Children.Add(txtGlowRadiusVal);
            dpGlowRad.Children.Add(sliderGlowRadius);
            spFX.Children.Add(lblGlowRad);
            spFX.Children.Add(dpGlowRad);

            // Motion Easing Style
            TextBlock lblMotion = CreateFieldLabel("Animation Motion Physics:");
            cmbMotionStyle = new System.Windows.Controls.ComboBox();
            cmbMotionStyle.Items.Add("Tactile Spring Snap (Overshoot Bounce)");
            cmbMotionStyle.Items.Add("Smooth Fluid Glide (Cubic Ease)");
            cmbMotionStyle.Items.Add("Micro Bounce");
            cmbMotionStyle.Items.Add("Classic Linear");
            cmbMotionStyle.SelectedIndex = 0;
            cmbMotionStyle.Margin = new Thickness(0, 0, 0, 8);
            cmbMotionStyle.SelectionChanged += delegate {
                if (cmbMotionStyle.SelectedIndex == 0) widget.MotionStyle = "Spring";
                else if (cmbMotionStyle.SelectedIndex == 1) widget.MotionStyle = "Smooth";
                else if (cmbMotionStyle.SelectedIndex == 2) widget.MotionStyle = "Bounce";
                else if (cmbMotionStyle.SelectedIndex == 3) widget.MotionStyle = "Linear";
                widget.SetGlowTheme(widget.GlowTheme);
                SaveConfig();
            };
            spFX.Children.Add(lblMotion);
            spFX.Children.Add(cmbMotionStyle);

            // Colon Pulse Style
            TextBlock lblColon = CreateFieldLabel("Colon (:) Pulse Effect:");
            cmbColonStyle = new System.Windows.Controls.ComboBox();
            cmbColonStyle.Items.Add("Smooth Breathing Pulse (Hypnotic Sine-Wave)");
            cmbColonStyle.Items.Add("1-Second Rhythm Blink");
            cmbColonStyle.Items.Add("Solid (Static)");
            cmbColonStyle.SelectedIndex = 0;
            cmbColonStyle.Margin = new Thickness(0, 0, 0, 8);
            cmbColonStyle.SelectionChanged += delegate {
                if (cmbColonStyle.SelectedIndex == 0) widget.ColonStyle = "Breath";
                else if (cmbColonStyle.SelectedIndex == 1) widget.ColonStyle = "Blink";
                else if (cmbColonStyle.SelectedIndex == 2) widget.ColonStyle = "Solid";
                widget.UpdateColonAnimation();
                SaveConfig();
            };
            spFX.Children.Add(lblColon);
            spFX.Children.Add(cmbColonStyle);

            // Gothic Divider Checkbox
            chkShowDivider = CreateCheckBox("Ornate Gothic Diamond Filigree Divider", true);
            chkShowDivider.Margin = new Thickness(0, 4, 0, 4);
            chkShowDivider.Click += delegate {
                widget.ShowDivider = chkShowDivider.IsChecked == true;
                widget.RefreshAppearance();
                SaveConfig();
            };
            spFX.Children.Add(chkShowDivider);

            // Frosted Glass Plate Checkbox
            chkGlassPlate = CreateCheckBox("Frosted Smoked Glass Acrylic Plate (Contrast Backdrop)", false);
            chkGlassPlate.Margin = new Thickness(0, 4, 0, 4);
            chkGlassPlate.Click += delegate {
                widget.SetGlassPlate(chkGlassPlate.IsChecked == true);
                SaveConfig();
            };
            spFX.Children.Add(chkGlassPlate);

            cardFX.Child = spFX;
            content.Children.Add(cardFX);

            // =================================================================
            // CARD 3: ELEMENT 1 - TIME (HOUR, MINUTE, SECOND)
            // =================================================================
            Border cardTime = CreateCard();
            StackPanel spTime = new StackPanel();

            TextBlock lblTimeTitle = CreateCardHeader("Element 1: Time (Hours, Minutes, Seconds)");

            chkShowTime = CreateCheckBox("Show Time Element", true);
            chkShowTime.Click += delegate {
                widget.ShowTime = chkShowTime.IsChecked == true;
                widget.RefreshAppearance();
                SaveConfig();
            };

            TextBlock lblTimeSize = CreateFieldLabel("Time Font Size:");
            DockPanel dpTimeSize = new DockPanel();
            dpTimeSize.Margin = new Thickness(0, 0, 0, 6);

            txtTimeSizeVal = new TextBlock();
            txtTimeSizeVal.Text = "28px";
            txtTimeSizeVal.Width = 46;
            txtTimeSizeVal.TextAlignment = TextAlignment.Right;
            txtTimeSizeVal.VerticalAlignment = VerticalAlignment.Center;
            txtTimeSizeVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            DockPanel.SetDock(txtTimeSizeVal, Dock.Right);

            sliderTimeSize = new System.Windows.Controls.Slider();
            sliderTimeSize.Minimum = 12;
            sliderTimeSize.Maximum = 100;
            sliderTimeSize.Value = 28;
            sliderTimeSize.TickFrequency = 1;
            sliderTimeSize.IsSnapToTickEnabled = true;
            sliderTimeSize.Margin = new Thickness(0, 0, 10, 0);
            sliderTimeSize.ValueChanged += delegate {
                widget.TimeFontSize = Math.Round(sliderTimeSize.Value);
                txtTimeSizeVal.Text = widget.TimeFontSize + "px";
                widget.RefreshAppearance();
                SaveConfig();
            };

            dpTimeSize.Children.Add(txtTimeSizeVal);
            dpTimeSize.Children.Add(sliderTimeSize);

            StackPanel spTimeOpts = new StackPanel();
            spTimeOpts.Orientation = System.Windows.Controls.Orientation.Horizontal;

            chk12Hour = CreateCheckBox("12-Hour (AM/PM)", true);
            chk12Hour.Margin = new Thickness(0, 0, 16, 0);
            chk12Hour.Click += delegate {
                widget.Is12Hour = chk12Hour.IsChecked == true;
                widget.RefreshAppearance();
                SaveConfig();
            };

            chkShowSeconds = CreateCheckBox("Show Seconds", true);
            chkShowSeconds.Click += delegate {
                widget.ShowSeconds = chkShowSeconds.IsChecked == true;
                widget.RefreshAppearance();
                SaveConfig();
            };

            spTimeOpts.Children.Add(chk12Hour);
            spTimeOpts.Children.Add(chkShowSeconds);

            spTime.Children.Add(lblTimeTitle);
            spTime.Children.Add(chkShowTime);
            spTime.Children.Add(lblTimeSize);
            spTime.Children.Add(dpTimeSize);
            spTime.Children.Add(spTimeOpts);
            cardTime.Child = spTime;
            content.Children.Add(cardTime);

            // =================================================================
            // CARD 3: ELEMENT 2 - DAY (Today is Wednesday)
            // =================================================================
            Border cardDay = CreateCard();
            StackPanel spDay = new StackPanel();

            TextBlock lblDayTitle = CreateCardHeader("Element 2: Day of Week");

            chkShowDay = CreateCheckBox("Show Day of Week", true);
            chkShowDay.Click += delegate {
                widget.ShowDay = chkShowDay.IsChecked == true;
                widget.RefreshAppearance();
                SaveConfig();
            };

            TextBlock lblDaySize = CreateFieldLabel("Day Font Size:");
            DockPanel dpDaySize = new DockPanel();
            dpDaySize.Margin = new Thickness(0, 0, 0, 6);

            txtDaySizeVal = new TextBlock();
            txtDaySizeVal.Text = "14px";
            txtDaySizeVal.Width = 46;
            txtDaySizeVal.TextAlignment = TextAlignment.Right;
            txtDaySizeVal.VerticalAlignment = VerticalAlignment.Center;
            txtDaySizeVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            DockPanel.SetDock(txtDaySizeVal, Dock.Right);

            sliderDaySize = new System.Windows.Controls.Slider();
            sliderDaySize.Minimum = 10;
            sliderDaySize.Maximum = 80;
            sliderDaySize.Value = 14;
            sliderDaySize.TickFrequency = 1;
            sliderDaySize.IsSnapToTickEnabled = true;
            sliderDaySize.Margin = new Thickness(0, 0, 10, 0);
            sliderDaySize.ValueChanged += delegate {
                widget.DayFontSize = Math.Round(sliderDaySize.Value);
                txtDaySizeVal.Text = widget.DayFontSize + "px";
                widget.RefreshAppearance();
                SaveConfig();
            };

            dpDaySize.Children.Add(txtDaySizeVal);
            dpDaySize.Children.Add(sliderDaySize);

            chkTodayPrefix = CreateCheckBox("Include 'Today is' prefix", true);
            chkTodayPrefix.Click += delegate {
                widget.ShowTodayPrefix = chkTodayPrefix.IsChecked == true;
                widget.RefreshAppearance();
                SaveConfig();
            };

            spDay.Children.Add(lblDayTitle);
            spDay.Children.Add(chkShowDay);
            spDay.Children.Add(lblDaySize);
            spDay.Children.Add(dpDaySize);
            spDay.Children.Add(chkTodayPrefix);
            cardDay.Child = spDay;
            content.Children.Add(cardDay);

            // =================================================================
            // CARD 4: ELEMENT 3 - DATE (11/11/2026)
            // =================================================================
            Border cardDate = CreateCard();
            StackPanel spDate = new StackPanel();

            TextBlock lblDateTitle = CreateCardHeader("Element 3: Date");

            chkShowDate = CreateCheckBox("Show Date Element", true);
            chkShowDate.Click += delegate {
                widget.ShowDate = chkShowDate.IsChecked == true;
                widget.RefreshAppearance();
                SaveConfig();
            };

            TextBlock lblDateSize = CreateFieldLabel("Date Font Size:");
            DockPanel dpDateSize = new DockPanel();
            dpDateSize.Margin = new Thickness(0, 0, 0, 8);

            txtDateSizeVal = new TextBlock();
            txtDateSizeVal.Text = "13px";
            txtDateSizeVal.Width = 46;
            txtDateSizeVal.TextAlignment = TextAlignment.Right;
            txtDateSizeVal.VerticalAlignment = VerticalAlignment.Center;
            txtDateSizeVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            DockPanel.SetDock(txtDateSizeVal, Dock.Right);

            sliderDateSize = new System.Windows.Controls.Slider();
            sliderDateSize.Minimum = 10;
            sliderDateSize.Maximum = 80;
            sliderDateSize.Value = 13;
            sliderDateSize.TickFrequency = 1;
            sliderDateSize.IsSnapToTickEnabled = true;
            sliderDateSize.Margin = new Thickness(0, 0, 10, 0);
            sliderDateSize.ValueChanged += delegate {
                widget.DateFontSize = Math.Round(sliderDateSize.Value);
                txtDateSizeVal.Text = widget.DateFontSize + "px";
                widget.RefreshAppearance();
                SaveConfig();
            };

            dpDateSize.Children.Add(txtDateSizeVal);
            dpDateSize.Children.Add(sliderDateSize);

            TextBlock lblFormat = CreateFieldLabel("Date Format:");
            cmbDateFormat = new System.Windows.Controls.ComboBox();
            cmbDateFormat.Items.Add("MM/dd/yyyy  (11/11/2026)");
            cmbDateFormat.Items.Add("dd/MM/yyyy  (11/11/2026)");
            cmbDateFormat.Items.Add("yyyy-MM-dd  (2026-11-11)");
            cmbDateFormat.Items.Add("MMMM d, yyyy (November 11, 2026)");
            cmbDateFormat.SelectedIndex = 0;
            cmbDateFormat.Margin = new Thickness(0, 0, 0, 2);
            cmbDateFormat.SelectionChanged += delegate {
                if (cmbDateFormat.SelectedIndex == 0) widget.DateFormat = "MM/dd/yyyy";
                else if (cmbDateFormat.SelectedIndex == 1) widget.DateFormat = "dd/MM/yyyy";
                else if (cmbDateFormat.SelectedIndex == 2) widget.DateFormat = "yyyy-MM-dd";
                else if (cmbDateFormat.SelectedIndex == 3) widget.DateFormat = "MMMM d, yyyy";
                widget.RefreshAppearance();
                SaveConfig();
            };

            spDate.Children.Add(lblDateTitle);
            spDate.Children.Add(chkShowDate);
            spDate.Children.Add(lblDateSize);
            spDate.Children.Add(dpDateSize);
            spDate.Children.Add(lblFormat);
            spDate.Children.Add(cmbDateFormat);
            cardDate.Child = spDate;
            content.Children.Add(cardDate);

            // =================================================================
            // CARD 5: DESKTOP POSITIONING & MOVE MODE
            // =================================================================
            Border cardPos = CreateCard();
            StackPanel spPos = new StackPanel();

            TextBlock lblPos = CreateCardHeader("Desktop Position");

            UniformGrid gridPos = new UniformGrid();
            gridPos.Columns = 3;
            gridPos.Margin = new Thickness(0, 0, 0, 8);

            System.Windows.Controls.Button btnTR = CreateSmallButton("Top-Right");
            btnTR.Click += delegate { SnapPosition(SystemParameters.WorkArea.Right - (220 * widget.MasterScale) - 20, 30); };

            System.Windows.Controls.Button btnTL = CreateSmallButton("Top-Left");
            btnTL.Click += delegate { SnapPosition(30, 30); };

            System.Windows.Controls.Button btnBR = CreateSmallButton("Bottom-Right");
            btnBR.Click += delegate { SnapPosition(SystemParameters.WorkArea.Right - (220 * widget.MasterScale) - 20, SystemParameters.WorkArea.Bottom - (90 * widget.MasterScale) - 30); };

            System.Windows.Controls.Button btnBL = CreateSmallButton("Bottom-Left");
            btnBL.Click += delegate { SnapPosition(30, SystemParameters.WorkArea.Bottom - (90 * widget.MasterScale) - 30); };

            System.Windows.Controls.Button btnCenter = CreateSmallButton("Center");
            btnCenter.Click += delegate { SnapPosition((SystemParameters.WorkArea.Width - (220 * widget.MasterScale)) / 2, (SystemParameters.WorkArea.Height - (90 * widget.MasterScale)) / 2); };

            System.Windows.Controls.Button btnReset = CreateSmallButton("Reset");
            btnReset.Click += delegate { SnapPosition(SystemParameters.WorkArea.Right - 230, 40); };

            gridPos.Children.Add(btnTR);
            gridPos.Children.Add(btnTL);
            gridPos.Children.Add(btnCenter);
            gridPos.Children.Add(btnBR);
            gridPos.Children.Add(btnBL);
            gridPos.Children.Add(btnReset);

            btnMoveMode = CreateStyledButton("Enable Move Mode (Drag on Screen)", System.Windows.Media.Color.FromRgb(40, 80, 140));
            btnMoveMode.Margin = new Thickness(0, 6, 0, 0);
            btnMoveMode.Click += delegate {
                bool newMode = !widget.IsMoveMode;
                widget.SetMoveMode(newMode);
                if (newMode)
                {
                    btnMoveMode.Content = "Lock Position to Desktop (Done Dragging)";
                    btnMoveMode.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 90, 20));
                }
                else
                {
                    btnMoveMode.Content = "Enable Move Mode (Drag on Screen)";
                    btnMoveMode.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 80, 140));
                    SaveConfig();
                }
            };

            spPos.Children.Add(lblPos);
            spPos.Children.Add(gridPos);
            spPos.Children.Add(btnMoveMode);
            cardPos.Child = spPos;
            content.Children.Add(cardPos);

            // =================================================================
            // CARD 6: SYSTEM INTEGRATION & WIN+D IMMUNITY
            // =================================================================
            Border cardSys = CreateCard();
            StackPanel spSys = new StackPanel();

            TextBlock lblSys = CreateCardHeader("System Integration & Win+D Protection");

            chkAutoStart = CreateCheckBox("Start silently with Windows on boot", IsStartupEnabled());
            chkAutoStart.Click += delegate {
                SetStartupEnabled(chkAutoStart.IsChecked == true);
                SaveConfig();
            };

            chkStayDesktop = CreateCheckBox("Win+D Immunity & Main Desktop Layer (Never hides on Win+D)", true);
            chkStayDesktop.Click += delegate {
                widget.StayOnDesktop = (chkStayDesktop.IsChecked == true);
                widget.AttachToDesktop();
                SaveConfig();
            };

            spSys.Children.Add(lblSys);
            spSys.Children.Add(chkAutoStart);
            spSys.Children.Add(chkStayDesktop);
            cardSys.Child = spSys;
            content.Children.Add(cardSys);

            // =================================================================
            // CARD 7: MAINTENANCE & UNINSTALL
            // =================================================================
            Border cardUninst = CreateCard();
            StackPanel spUninst = new StackPanel();

            TextBlock lblUninst = CreateCardHeader("Maintenance & Uninstall");

            System.Windows.Controls.Button btnUninstall = CreateStyledButton("Uninstall Gothic Clock", System.Windows.Media.Color.FromRgb(140, 35, 35));
            btnUninstall.Click += delegate {
                PerformUninstall();
            };

            spUninst.Children.Add(lblUninst);
            spUninst.Children.Add(btnUninstall);
            cardUninst.Child = spUninst;
            content.Children.Add(cardUninst);

            scroll.Content = content;
            mainDock.Children.Add(scroll);

            windowBorder.Child = mainDock;
            this.Content = windowBorder;
        }

        private System.Windows.Controls.Button CreateSmallPresetButton(string text, double scale)
        {
            System.Windows.Controls.Button btn = CreateSmallButton(text);
            btn.Click += delegate {
                sliderMasterScale.Value = scale;
            };
            return btn;
        }

        private System.Windows.Controls.Button CreateTitleButton(string text, bool isClose)
        {
            System.Windows.Controls.Button btn = new System.Windows.Controls.Button();
            btn.Content = text;
            btn.Width = 44;
            btn.Height = 36;
            btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 185, 195));
            btn.Background = System.Windows.Media.Brushes.Transparent;
            btn.BorderThickness = new Thickness(0);
            btn.FontSize = 13;
            btn.Cursor = System.Windows.Input.Cursors.Hand;

            SolidColorBrush defaultBg = System.Windows.Media.Brushes.Transparent;
            SolidColorBrush hoverBg = isClose ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 43, 52));
            SolidColorBrush hoverFg = System.Windows.Media.Brushes.White;
            SolidColorBrush defaultFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 185, 195));

            btn.MouseEnter += delegate {
                btn.Background = hoverBg;
                btn.Foreground = hoverFg;
            };
            btn.MouseLeave += delegate {
                btn.Background = defaultBg;
                btn.Foreground = defaultFg;
            };

            return btn;
        }

        private void SnapPosition(double left, double top)
        {
            widget.Left = Math.Max(0, left);
            widget.Top = Math.Max(0, top);
            widget.AttachToDesktop();
            SaveConfig();
        }

        private Border CreateCard()
        {
            Border b = new Border();
            b.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(28, 29, 35));
            b.CornerRadius = new CornerRadius(7);
            b.Padding = new Thickness(14);
            b.Margin = new Thickness(0, 0, 0, 10);
            b.BorderThickness = new Thickness(1);
            b.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 45, 54));
            return b;
        }

        private TextBlock CreateCardHeader(string text)
        {
            TextBlock tb = new TextBlock();
            tb.Text = text;
            tb.FontWeight = FontWeights.SemiBold;
            tb.FontSize = 13;
            tb.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 218, 230));
            tb.Margin = new Thickness(0, 0, 0, 8);
            return tb;
        }

        private TextBlock CreateFieldLabel(string text)
        {
            TextBlock tb = new TextBlock();
            tb.Text = text;
            tb.FontSize = 11;
            tb.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 170, 185));
            tb.Margin = new Thickness(0, 4, 0, 3);
            return tb;
        }

        private System.Windows.Controls.CheckBox CreateCheckBox(string text, bool isChecked)
        {
            System.Windows.Controls.CheckBox cb = new System.Windows.Controls.CheckBox();
            cb.Content = text;
            cb.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(215, 220, 230));
            cb.IsChecked = isChecked;
            cb.Margin = new Thickness(0, 2, 0, 6);
            return cb;
        }

        private System.Windows.Controls.Button CreateStyledButton(string text, System.Windows.Media.Color bg)
        {
            System.Windows.Controls.Button btn = new System.Windows.Controls.Button();
            btn.Content = text;
            btn.Height = 36;
            btn.Foreground = System.Windows.Media.Brushes.White;
            btn.Background = new SolidColorBrush(bg);
            btn.BorderThickness = new Thickness(0);
            btn.FontWeight = FontWeights.Medium;
            btn.Cursor = System.Windows.Input.Cursors.Hand;
            return btn;
        }

        private System.Windows.Controls.Button CreateSmallButton(string text)
        {
            System.Windows.Controls.Button btn = new System.Windows.Controls.Button();
            btn.Content = text;
            btn.Height = 28;
            btn.Margin = new Thickness(2);
            btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 225, 235));
            btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 48, 58));
            btn.BorderThickness = new Thickness(0);
            btn.Cursor = System.Windows.Input.Cursors.Hand;
            return btn;
        }

        private System.Windows.Controls.Button CreateThemeButton(string label, System.Windows.Media.Color accentColor, string themeKey)
        {
            System.Windows.Controls.Button btn = new System.Windows.Controls.Button();
            btn.Height = 32;
            btn.Margin = new Thickness(2, 3, 2, 3);
            btn.Cursor = System.Windows.Input.Cursors.Hand;
            btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(28, 30, 38));
            btn.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(140, accentColor.R, accentColor.G, accentColor.B));
            btn.BorderThickness = new Thickness(1);

            StackPanel sp = new StackPanel();
            sp.Orientation = System.Windows.Controls.Orientation.Horizontal;
            sp.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

            Border dot = new Border();
            dot.Width = 8;
            dot.Height = 8;
            dot.CornerRadius = new CornerRadius(4);
            dot.Background = new SolidColorBrush(accentColor);
            dot.Margin = new Thickness(0, 0, 6, 0);

            TextBlock tb = new TextBlock();
            tb.Text = label;
            tb.FontSize = 10.5;
            tb.FontWeight = FontWeights.Medium;
            tb.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 226, 235));

            sp.Children.Add(dot);
            sp.Children.Add(tb);
            btn.Content = sp;

            btn.Click += delegate {
                widget.SetGlowTheme(themeKey);
                if (themeKey == "Stealth")
                {
                    sliderGlowRadius.Value = 3;
                }
                else if (sliderGlowRadius.Value < 8)
                {
                    sliderGlowRadius.Value = 16;
                }
                SaveConfig();
            };

            return btn;
        }

        private void SetupTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Gothic Clock Control Panel";

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
            {
                try { trayIcon.Icon = new Icon(iconPath); } catch { trayIcon.Icon = SystemIcons.Application; }
            }
            else
            {
                trayIcon.Icon = SystemIcons.Application;
            }

            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };

            System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu();
            menu.MenuItems.Add(new System.Windows.Forms.MenuItem("Open Control Panel", delegate {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            }));
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(new System.Windows.Forms.MenuItem("Exit Completely", delegate {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                System.Windows.Application.Current.Shutdown();
            }));

            trayIcon.ContextMenu = menu;
        }

        private bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (rk != null) return rk.GetValue("GothicClockWidget") != null;
                }
            }
            catch { }
            return false;
        }

        private void SetStartupEnabled(bool enable)
        {
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (rk != null)
                    {
                        if (enable)
                        {
                            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                            rk.SetValue("GothicClockWidget", "\"" + exePath + "\" --silent");
                        }
                        else
                        {
                            rk.DeleteValue("GothicClockWidget", false);
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadConfig()
        {
            double defaultLeft = SystemParameters.WorkArea.Right - 230;
            double defaultTop = 40;

            if (File.Exists(configPath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(configPath);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string k = parts[0].Trim();
                            string v = parts[1].Trim();
                            if (k == "Left") defaultLeft = double.Parse(v);
                            else if (k == "Top") defaultTop = double.Parse(v);
                            else if (k == "MasterScale") widget.MasterScale = double.Parse(v);
                            else if (k == "LineSpacing") widget.LineSpacing = double.Parse(v);
                            else if (k == "ShowTime") widget.ShowTime = bool.Parse(v);
                            else if (k == "TimeFontSize") widget.TimeFontSize = double.Parse(v);
                            else if (k == "Is12Hour") widget.Is12Hour = bool.Parse(v);
                            else if (k == "ShowSeconds") widget.ShowSeconds = bool.Parse(v);
                            else if (k == "EnableAnim") widget.EnableAnimation = bool.Parse(v);
                            else if (k == "AnimDuration") widget.AnimationDurationMs = int.Parse(v);
                            else if (k == "ShowDay") widget.ShowDay = bool.Parse(v);
                            else if (k == "DayFontSize") widget.DayFontSize = double.Parse(v);
                            else if (k == "ShowTodayPrefix") widget.ShowTodayPrefix = bool.Parse(v);
                            else if (k == "ShowDate") widget.ShowDate = bool.Parse(v);
                            else if (k == "DateFontSize") widget.DateFontSize = double.Parse(v);
                            else if (k == "DateFormat") widget.DateFormat = v;
                            else if (k == "Active") isMasterActive = bool.Parse(v);
                            else if (k == "StayDesktop") widget.StayOnDesktop = bool.Parse(v);
                            else if (k == "GlowTheme") widget.GlowTheme = v;
                            else if (k == "GlowRadius") widget.GlowRadius = double.Parse(v);
                            else if (k == "MotionStyle") widget.MotionStyle = v;
                            else if (k == "ColonStyle") widget.ColonStyle = v;
                            else if (k == "ShowDivider") widget.ShowDivider = bool.Parse(v);
                            else if (k == "ShowGlass") widget.ShowGlassBackplate = bool.Parse(v);
                        }
                    }
                }
                catch { }
            }

            sliderMasterScale.Value = widget.MasterScale;
            txtMasterScaleVal.Text = Math.Round(widget.MasterScale * 100) + "%";

            sliderSpacing.Value = widget.LineSpacing;
            txtSpacingVal.Text = widget.LineSpacing + "px";

            chkEnableAnim.IsChecked = widget.EnableAnimation;
            sliderAnimSpeed.Value = widget.AnimationDurationMs;
            txtAnimSpeedVal.Text = widget.AnimationDurationMs + "ms";

            chkShowTime.IsChecked = widget.ShowTime;
            sliderTimeSize.Value = widget.TimeFontSize;
            txtTimeSizeVal.Text = widget.TimeFontSize + "px";
            chk12Hour.IsChecked = widget.Is12Hour;
            chkShowSeconds.IsChecked = widget.ShowSeconds;

            chkShowDay.IsChecked = widget.ShowDay;
            sliderDaySize.Value = widget.DayFontSize;
            txtDaySizeVal.Text = widget.DayFontSize + "px";
            chkTodayPrefix.IsChecked = widget.ShowTodayPrefix;

            chkShowDate.IsChecked = widget.ShowDate;
            sliderDateSize.Value = widget.DateFontSize;
            txtDateSizeVal.Text = widget.DateFontSize + "px";

            if (widget.DateFormat == "MM/dd/yyyy") cmbDateFormat.SelectedIndex = 0;
            else if (widget.DateFormat == "dd/MM/yyyy") cmbDateFormat.SelectedIndex = 1;
            else if (widget.DateFormat == "yyyy-MM-dd") cmbDateFormat.SelectedIndex = 2;
            else if (widget.DateFormat == "MMMM d, yyyy") cmbDateFormat.SelectedIndex = 3;

            sliderGlowRadius.Value = widget.GlowRadius;
            txtGlowRadiusVal.Text = widget.GlowRadius + "px";

            if (widget.MotionStyle == "Spring") cmbMotionStyle.SelectedIndex = 0;
            else if (widget.MotionStyle == "Smooth") cmbMotionStyle.SelectedIndex = 1;
            else if (widget.MotionStyle == "Bounce") cmbMotionStyle.SelectedIndex = 2;
            else if (widget.MotionStyle == "Linear") cmbMotionStyle.SelectedIndex = 3;

            if (widget.ColonStyle == "Breath") cmbColonStyle.SelectedIndex = 0;
            else if (widget.ColonStyle == "Blink") cmbColonStyle.SelectedIndex = 1;
            else if (widget.ColonStyle == "Solid") cmbColonStyle.SelectedIndex = 2;

            chkShowDivider.IsChecked = widget.ShowDivider;
            chkGlassPlate.IsChecked = widget.ShowGlassBackplate;

            chkStayDesktop.IsChecked = widget.StayOnDesktop;

            widget.RefreshAppearance();
            SnapPosition(defaultLeft, defaultTop);

            if (!isMasterActive)
            {
                widget.Hide();
                btnToggleAll.Content = "All Elements: STOPPED (OFF)";
                btnToggleAll.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 75, 85));
            }
            else
            {
                widget.Show();
                widget.AttachToDesktop();
            }
        }

        private void SaveConfig()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(configPath, false))
                {
                    sw.WriteLine("Left=" + Math.Round(widget.Left, 1));
                    sw.WriteLine("Top=" + Math.Round(widget.Top, 1));
                    sw.WriteLine("MasterScale=" + widget.MasterScale);
                    sw.WriteLine("LineSpacing=" + widget.LineSpacing);
                    sw.WriteLine("ShowTime=" + widget.ShowTime);
                    sw.WriteLine("TimeFontSize=" + widget.TimeFontSize);
                    sw.WriteLine("Is12Hour=" + widget.Is12Hour);
                    sw.WriteLine("ShowSeconds=" + widget.ShowSeconds);
                    sw.WriteLine("EnableAnim=" + widget.EnableAnimation);
                    sw.WriteLine("AnimDuration=" + widget.AnimationDurationMs);
                    sw.WriteLine("ShowDay=" + widget.ShowDay);
                    sw.WriteLine("DayFontSize=" + widget.DayFontSize);
                    sw.WriteLine("ShowTodayPrefix=" + widget.ShowTodayPrefix);
                    sw.WriteLine("ShowDate=" + widget.ShowDate);
                    sw.WriteLine("DateFontSize=" + widget.DateFontSize);
                    sw.WriteLine("DateFormat=" + widget.DateFormat);
                    sw.WriteLine("Active=" + isMasterActive);
                    sw.WriteLine("StayDesktop=" + widget.StayOnDesktop);
                    sw.WriteLine("GlowTheme=" + widget.GlowTheme);
                    sw.WriteLine("GlowRadius=" + widget.GlowRadius);
                    sw.WriteLine("MotionStyle=" + widget.MotionStyle);
                    sw.WriteLine("ColonStyle=" + widget.ColonStyle);
                    sw.WriteLine("ShowDivider=" + widget.ShowDivider);
                    sw.WriteLine("ShowGlass=" + widget.ShowGlassBackplate);
                }
            }
            catch { }
        }

        private void PerformUninstall()
        {
            MessageBoxResult res = System.Windows.MessageBox.Show(
                "Are you sure you want to uninstall Gothic Clock?\n\nThis will:\n- Remove startup entries\n- Delete Desktop and Start Menu shortcuts\n- Close all Gothic Clock widgets",
                "Uninstall Gothic Clock",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    SetStartupEnabled(false);

                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string desktopLnk = Path.Combine(desktop, "Gothic Clock.lnk");
                    if (File.Exists(desktopLnk)) File.Delete(desktopLnk);

                    string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                    string startLnk = Path.Combine(programs, "Gothic Clock.lnk");
                    if (File.Exists(startLnk)) File.Delete(startLnk);

                    if (trayIcon != null)
                    {
                        trayIcon.Visible = false;
                        trayIcon.Dispose();
                    }

                    string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                    if (appDir.IndexOf("Programs", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                                FileName = "cmd.exe",
                                Arguments = "/c timeout /t 1 /nobreak >nul & rmdir /s /q \"" + appDir + "\"",
                                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                                CreateNoWindow = true
                            });
                        }
                        catch { }
                    }

                    System.Windows.MessageBox.Show(
                        "Gothic Clock has been uninstalled successfully.\n\nAll shortcuts, files, and startup entries have been removed.",
                        "Uninstalled",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    System.Windows.Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Uninstall error: " + ex.Message, "Gothic Clock", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    // =========================================================================
    // 3. MAIN APPLICATION ENTRY POINT
    // =========================================================================
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) {
                try {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), e.ExceptionObject != null ? e.ExceptionObject.ToString() : "Unknown crash");
                } catch { }
            };

            try
            {
                bool isSilent = false;
                if (args != null && args.Length > 0)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i] == "--silent") isSilent = true;
                    }
                }

                System.Diagnostics.Process current = System.Diagnostics.Process.GetCurrentProcess();
                System.Diagnostics.Process[] running = System.Diagnostics.Process.GetProcessesByName(current.ProcessName);
                if (running.Length > 1)
                {
                    try
                    {
                        EventWaitHandle ev = EventWaitHandle.OpenExisting("GothicClockShowControlPanelEvent");
                        ev.Set();
                    }
                    catch { }
                    return;
                }

                EventWaitHandle showEventHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "GothicClockShowControlPanelEvent");

                System.Windows.Application app = new System.Windows.Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                app.DispatcherUnhandledException += delegate(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
                    try {
                        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), e.Exception != null ? e.Exception.ToString() : "Dispatcher crash");
                    } catch { }
                };

                WidgetWindow widget = new WidgetWindow();
                ControlPanelWindow controlPanel = new ControlPanelWindow(widget);

                Thread listenThread = new Thread(new ThreadStart(delegate {
                    while (true)
                    {
                        showEventHandle.WaitOne();
                        app.Dispatcher.BeginInvoke(new Action(delegate {
                            controlPanel.Show();
                            controlPanel.WindowState = WindowState.Normal;
                            controlPanel.Activate();
                        }));
                    }
                }));
                listenThread.IsBackground = true;
                listenThread.Start();

                if (!isSilent)
                {
                    controlPanel.Show();
                }

                app.Run();
            }
            catch (Exception ex)
            {
                try {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), ex.ToString());
                } catch { }
            }
        }
    }
}
