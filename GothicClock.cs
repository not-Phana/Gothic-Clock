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
using System.Collections.Generic;
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

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private const int SW_SHOWNOACTIVATE = 4;

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

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

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
        public bool IsAlwaysOnTop = false;

        // Bleach Gothic Visual FX & Spiritual Reiatsu Aura
        public string GlowTheme = "Getsuga";
        public double GlowRadius = 18;
        public double GlowOpacity = 0.90;
        public string MotionStyle = "Spring";
        public string ColonStyle = "Breath";
        public bool ShowDivider = true;
        public bool ShowGlassBackplate = false;

        private Grid dividerGrid;
        private System.Windows.Shapes.Polygon diamondCenter;
        private System.Windows.Shapes.Rectangle lineLeft;
        private System.Windows.Shapes.Rectangle lineRight;

        private ScaleTransform widgetAnimScale;
        private TranslateTransform widgetAnimTranslate;

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

            this.StateChanged += delegate {
                if (this.WindowState != WindowState.Normal)
                {
                    this.WindowState = WindowState.Normal;
                }
            };

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

            widgetAnimScale = new ScaleTransform(1.0, 1.0);
            widgetAnimTranslate = new TranslateTransform(0, 0);
            TransformGroup animTg = new TransformGroup();
            animTg.Children.Add(widgetAnimScale);
            animTg.Children.Add(widgetAnimTranslate);
            mainBorder.RenderTransform = animTg;
            mainBorder.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

            this.Content = mainBorder;

            mainBorder.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) {
                if (IsMoveMode && e.ButtonState == MouseButtonState.Pressed)
                {
                    try { this.DragMove(); } catch { }
                }
            };
        }

        public void AnimateHide(Action onCompleted = null)
        {
            if (!this.IsVisible)
            {
                if (onCompleted != null) onCompleted();
                return;
            }

            if (!EnableAnimation)
            {
                this.Hide();
                if (onCompleted != null) onCompleted();
                return;
            }

            Storyboard sb = new Storyboard();

            DoubleAnimation animY = new DoubleAnimation(0, 16, new Duration(TimeSpan.FromMilliseconds(220)));
            animY.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            Storyboard.SetTarget(animY, widgetAnimTranslate);
            Storyboard.SetTargetProperty(animY, new PropertyPath(TranslateTransform.YProperty));
            sb.Children.Add(animY);

            DoubleAnimation animScale = new DoubleAnimation(1.0, 0.90, new Duration(TimeSpan.FromMilliseconds(220)));
            animScale.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            Storyboard.SetTarget(animScale, widgetAnimScale);
            Storyboard.SetTargetProperty(animScale, new PropertyPath(ScaleTransform.ScaleYProperty));
            sb.Children.Add(animScale);

            DoubleAnimation animOp = new DoubleAnimation(1.0, 0.0, new Duration(TimeSpan.FromMilliseconds(220)));
            animOp.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            Storyboard.SetTarget(animOp, mainBorder);
            Storyboard.SetTargetProperty(animOp, new PropertyPath(UIElement.OpacityProperty));
            sb.Children.Add(animOp);

            sb.Completed += delegate {
                this.Hide();
                widgetAnimTranslate.Y = 0;
                widgetAnimScale.ScaleX = 1.0;
                widgetAnimScale.ScaleY = 1.0;
                mainBorder.Opacity = 1.0;
                if (onCompleted != null) onCompleted();
            };

            sb.Begin();
        }

        public void AnimateShow()
        {
            this.Show();
            this.AttachToDesktop();

            if (!EnableAnimation)
            {
                mainBorder.Opacity = 1.0;
                return;
            }

            widgetAnimTranslate.Y = -12;
            widgetAnimScale.ScaleX = 0.94;
            widgetAnimScale.ScaleY = 0.94;
            mainBorder.Opacity = 0.0;

            Storyboard sb = new Storyboard();

            DoubleAnimation animY = new DoubleAnimation(-12, 0, new Duration(TimeSpan.FromMilliseconds(260)));
            animY.EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut };
            Storyboard.SetTarget(animY, widgetAnimTranslate);
            Storyboard.SetTargetProperty(animY, new PropertyPath(TranslateTransform.YProperty));
            sb.Children.Add(animY);

            DoubleAnimation animScaleX = new DoubleAnimation(0.94, 1.0, new Duration(TimeSpan.FromMilliseconds(260)));
            animScaleX.EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut };
            Storyboard.SetTarget(animScaleX, widgetAnimScale);
            Storyboard.SetTargetProperty(animScaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
            sb.Children.Add(animScaleX);

            DoubleAnimation animScaleY = new DoubleAnimation(0.94, 1.0, new Duration(TimeSpan.FromMilliseconds(260)));
            animScaleY.EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut };
            Storyboard.SetTarget(animScaleY, widgetAnimScale);
            Storyboard.SetTargetProperty(animScaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
            sb.Children.Add(animScaleY);

            DoubleAnimation animOp = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(220)));
            Storyboard.SetTarget(animOp, mainBorder);
            Storyboard.SetTargetProperty(animOp, new PropertyPath(UIElement.OpacityProperty));
            sb.Children.Add(animOp);

            sb.Completed += delegate {
                widgetAnimTranslate.Y = 0;
                widgetAnimScale.ScaleX = 1.0;
                widgetAnimScale.ScaleY = 1.0;
                mainBorder.Opacity = 1.0;
            };

            sb.Begin();
        }

        public void SetGlowTheme(string theme)
        {
            GlowTheme = theme;
            SolidColorBrush textBrush;
            System.Windows.Media.Color glowColor;

            if (theme == "Getsuga" || theme == "GetsugaTensho" || theme == "Crimson")
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // Pure Soul Bone White
                glowColor = System.Windows.Media.Color.FromRgb(225, 29, 72); // Crimson Blood Reiatsu
                if (GlowRadius < 8) GlowRadius = 18;
            }
            else if (theme == "Bankai" || theme == "TensaZangetsu")
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // Stark White
                glowColor = System.Windows.Media.Color.FromRgb(15, 15, 22); // Pitch Void Reiatsu Shadow
                if (GlowRadius < 8) GlowRadius = 14;
            }
            else if (theme == "Hollow" || theme == "HollowMask")
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)); // Hollow Bone White
                glowColor = System.Windows.Media.Color.FromRgb(153, 27, 27); // Dark Crimson Reiatsu
                if (GlowRadius < 8) GlowRadius = 18;
            }
            else if (theme == "Ichigo" || theme == "ReiatsuFlame")
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 250, 220)); // Radiant Light
                glowColor = System.Windows.Media.Color.FromRgb(234, 88, 12); // Flame Reiatsu Orange
                if (GlowRadius < 8) GlowRadius = 20;
            }
            else if (theme == "Quincy" || theme == "Sternritter")
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 249, 255)); // Spirit Silver
                glowColor = System.Windows.Media.Color.FromRgb(2, 132, 199); // Sacred Quincy Blue
                if (GlowRadius < 8) GlowRadius = 18;
            }
            else if (theme == "Cero" || theme == "CeroOscuras")
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(236, 253, 245)); // Spectral White
                glowColor = System.Windows.Media.Color.FromRgb(16, 185, 129); // Emerald Cero
                if (GlowRadius < 8) GlowRadius = 20;
            }
            else if (theme == "Senbonzakura")
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 241, 242));
                glowColor = System.Windows.Media.Color.FromRgb(244, 63, 94); // Cherry Blossom
                if (GlowRadius < 8) GlowRadius = 18;
            }
            else // "Moonlight"
            {
                textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                glowColor = System.Windows.Media.Color.FromRgb(148, 163, 184); // Moonlight Silver
            }

            double depth = (theme == "Bankai") ? 2.0 : 0;
            double op = (theme == "Bankai") ? 0.95 : GlowOpacity;
            double rad = (theme == "Bankai" && GlowRadius > 14) ? 14 : GlowRadius;

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
                AttachToDesktop();
            }
        }

        public void AttachToDesktop()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            if (IsMoveMode)
            {
                this.Topmost = true;
                return;
            }

            if (IsAlwaysOnTop)
            {
                this.Topmost = true;
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
            else
            {
                this.Topmost = false;
                // Place window in normal desktop Z-order (above Progman, WorkerW & live wallpaper, but below active apps)
                SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
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

            // Immune to Win+D minimization/hide: auto-restore if ever minimized or hidden
            if (StayOnDesktop || IsAlwaysOnTop)
            {
                try {
                    IntPtr h = new WindowInteropHelper(this).Handle;
                    if (h != IntPtr.Zero && (IsIconic(h) || !IsWindowVisible(h)))
                    {
                        ShowWindow(h, SW_SHOWNOACTIVATE);
                    }
                } catch { }
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

            // Block Windows from hiding the widget (e.g. during Win+D / Show Desktop)
            if (msg == WM_SHOWWINDOW && (int)wParam == 0)
            {
                handled = true;
                return IntPtr.Zero;
            }

            if (msg == WM_WINDOWPOSCHANGING)
            {
                WINDOWPOS pos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                // Block minimization offscreen (-32000) or SWP_HIDEWINDOW (0x0080)
                if (pos.x <= -10000 || pos.y <= -10000 || (pos.flags & SWP_HIDEWINDOW) != 0)
                {
                    pos.x = (int)this.Left;
                    pos.y = (int)this.Top;
                    pos.flags |= (SWP_NOMOVE | SWP_NOSIZE);
                    pos.flags &= ~SWP_HIDEWINDOW;
                    pos.flags |= SWP_SHOWWINDOW;
                }

                // Never force HWND_BOTTOM! Preserve Z-order so it never gets buried behind the desktop/wallpaper
                pos.flags |= 0x0004u; // SWP_NOZORDER

                Marshal.StructureToPtr(pos, lParam, true);
                handled = true;
                return IntPtr.Zero;
            }
            else if (msg == WM_SYSCOMMAND)
            {
                int sc = (wParam.ToInt32() & 0xFFF0);
                if (sc == SC_MINIMIZE)
                {
                    handled = true;
                    return IntPtr.Zero;
                }
            }

            return IntPtr.Zero;
        }
    }

    // =========================================================================
    // 2. CYBERPUNK MAINFRAME CONTROL PANEL (BLEACH & HIGH-TECH HYBRID)
    // =========================================================================
    public class CyberToggle
    {
        public Border Container;
        public TextBlock DiamondIcon;
        public TextBlock TitleText;
        public TextBlock SubText;
        private bool isChecked;
        public Action<bool> OnChanged;

        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                UpdateVisual();
            }
        }

        public CyberToggle(string title, string sub, bool initial, Action<bool> onChanged)
        {
            this.isChecked = initial;
            this.OnChanged = onChanged;

            Container = new Border();
            Container.Background = System.Windows.Media.Brushes.Transparent;
            Container.Padding = new Thickness(4, 5, 4, 5);
            Container.Cursor = System.Windows.Input.Cursors.Hand;
            Container.Margin = new Thickness(0, 1, 0, 1);
            Container.CornerRadius = new CornerRadius(2);

            DockPanel dp = new DockPanel();

            DiamondIcon = new TextBlock();
            DiamondIcon.Width = 18;
            DiamondIcon.FontSize = 12;
            DiamondIcon.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Symbol, Consolas, sans-serif");
            DiamondIcon.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(DiamondIcon, Dock.Left);

            StackPanel spText = new StackPanel();
            spText.Orientation = System.Windows.Controls.Orientation.Horizontal;
            spText.VerticalAlignment = VerticalAlignment.Center;

            TitleText = new TextBlock();
            TitleText.FontSize = 11;
            TitleText.FontFamily = new System.Windows.Media.FontFamily("Consolas, Segoe UI, sans-serif");
            TitleText.FontWeight = FontWeights.Bold;

            SubText = new TextBlock();
            SubText.FontSize = 10;
            SubText.FontFamily = new System.Windows.Media.FontFamily("Consolas, Segoe UI, sans-serif");
            SubText.Margin = new Thickness(8, 0, 0, 0);
            SubText.VerticalAlignment = VerticalAlignment.Center;

            spText.Children.Add(TitleText);
            if (!string.IsNullOrEmpty(sub))
            {
                spText.Children.Add(SubText);
            }

            dp.Children.Add(DiamondIcon);
            dp.Children.Add(spText);
            Container.Child = dp;

            Container.MouseEnter += delegate {
                Container.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, 255, 42, 85));
            };
            Container.MouseLeave += delegate {
                Container.Background = System.Windows.Media.Brushes.Transparent;
            };
            Container.MouseLeftButtonDown += delegate {
                IsChecked = !IsChecked;
                if (OnChanged != null) OnChanged(IsChecked);
            };

            TitleText.Text = title;
            SubText.Text = sub;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (isChecked)
            {
                DiamondIcon.Text = "\u25C6"; // Solid diamond ◆
                DiamondIcon.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
                TitleText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
                SubText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
            }
            else
            {
                DiamondIcon.Text = "\u25C7"; // Hollow diamond ◇
                DiamondIcon.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(142, 117, 130));
                TitleText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 232, 236));
                SubText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(110, 90, 101));
            }
        }
    }

    public class ThemeOptionItem
    {
        public string Key;
        public string Name;
        public string Description;
        public System.Windows.Media.Color AccentColor;
        public Border Container;
        public TextBlock DiamondIcon;
        public TextBlock NameText;
        public TextBlock DescText;

        public ThemeOptionItem(string key, string name, string desc, System.Windows.Media.Color accent, Action<string> onSelect)
        {
            this.Key = key;
            this.Name = name;
            this.Description = desc;
            this.AccentColor = accent;

            Container = new Border();
            Container.Background = System.Windows.Media.Brushes.Transparent;
            Container.Padding = new Thickness(4, 5, 4, 5);
            Container.Cursor = System.Windows.Input.Cursors.Hand;
            Container.Margin = new Thickness(0, 1, 0, 1);
            Container.CornerRadius = new CornerRadius(2);

            DockPanel dp = new DockPanel();

            DiamondIcon = new TextBlock();
            DiamondIcon.Width = 18;
            DiamondIcon.FontSize = 12;
            DiamondIcon.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Symbol, Consolas, sans-serif");
            DiamondIcon.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(DiamondIcon, Dock.Left);

            StackPanel spText = new StackPanel();
            spText.Orientation = System.Windows.Controls.Orientation.Horizontal;
            spText.VerticalAlignment = VerticalAlignment.Center;

            Border dot = new Border();
            dot.Width = 8;
            dot.Height = 8;
            dot.CornerRadius = new CornerRadius(4);
            dot.Background = new SolidColorBrush(accent);
            dot.Margin = new Thickness(0, 0, 8, 0);
            dot.VerticalAlignment = VerticalAlignment.Center;

            NameText = new TextBlock();
            NameText.Text = name.ToUpper();
            NameText.FontSize = 11;
            NameText.FontFamily = new System.Windows.Media.FontFamily("Consolas, Segoe UI, sans-serif");
            NameText.FontWeight = FontWeights.Bold;

            DescText = new TextBlock();
            DescText.Text = desc;
            DescText.FontSize = 10;
            DescText.FontFamily = new System.Windows.Media.FontFamily("Consolas, Segoe UI, sans-serif");
            DescText.Margin = new Thickness(8, 0, 0, 0);
            DescText.VerticalAlignment = VerticalAlignment.Center;

            spText.Children.Add(dot);
            spText.Children.Add(NameText);
            spText.Children.Add(DescText);

            dp.Children.Add(DiamondIcon);
            dp.Children.Add(spText);
            Container.Child = dp;

            Container.MouseEnter += delegate {
                Container.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, 255, 42, 85));
            };
            Container.MouseLeave += delegate {
                Container.Background = System.Windows.Media.Brushes.Transparent;
            };
            Container.MouseLeftButtonDown += delegate {
                if (onSelect != null) onSelect(Key);
            };

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selected)
            {
                DiamondIcon.Text = "\u25C6"; // Solid diamond ◆
                DiamondIcon.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
                NameText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
                DescText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
            }
            else
            {
                DiamondIcon.Text = "\u25C7"; // Hollow diamond ◇
                DiamondIcon.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(142, 117, 130));
                NameText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 232, 236));
                DescText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(110, 90, 101));
            }
        }
    }

    public class ControlPanelWindow : Window
    {
        private WidgetWindow widget;
        private NotifyIcon trayIcon;
        private string configPath;

        private System.Windows.Controls.Button btnHeaderStatus;
        private System.Windows.Controls.Button btnHeaderThemes;
        private System.Windows.Controls.Button btnMoveMode;
        private System.Windows.Controls.Button btnPreviewAnim;
        private ScrollViewer scroll;

        // Master scale & spacing
        private System.Windows.Controls.Slider sliderMasterScale;
        private TextBlock txtMasterScaleVal;
        private System.Windows.Controls.Slider sliderSpacing;
        private TextBlock txtSpacingVal;

        // Animation Settings
        private CyberToggle toggleAnim;
        private System.Windows.Controls.Slider sliderAnimSpeed;
        private TextBlock txtAnimSpeedVal;

        // Aura & Visual Effects
        private System.Windows.Controls.Slider sliderGlowRadius;
        private TextBlock txtGlowRadiusVal;
        private System.Windows.Controls.ComboBox cmbMotionStyle;
        private System.Windows.Controls.ComboBox cmbColonStyle;
        private CyberToggle toggleDivider;
        private CyberToggle toggleGlass;

        // Element 1: Time
        private CyberToggle toggleShowTime;
        private System.Windows.Controls.Slider sliderTimeSize;
        private TextBlock txtTimeSizeVal;
        private CyberToggle toggle12Hour;
        private CyberToggle toggleSeconds;

        // Element 2: Day
        private CyberToggle toggleShowDay;
        private System.Windows.Controls.Slider sliderDaySize;
        private TextBlock txtDaySizeVal;
        private CyberToggle toggleTodayPrefix;

        // Element 3: Date
        private CyberToggle toggleShowDate;
        private System.Windows.Controls.Slider sliderDateSize;
        private TextBlock txtDateSizeVal;
        private System.Windows.Controls.ComboBox cmbDateFormat;

        // System
        private CyberToggle toggleAutoStart;
        private CyberToggle toggleStayDesktop;
        private CyberToggle toggleAlwaysOnTop;
        private CyberToggle toggleCloseAnimation;

        // Theme diamond items
        private List<ThemeOptionItem> themeItems = new List<ThemeOptionItem>();

        private bool isMasterActive = true;
        public bool EnableCloseAnimation = true;
        private bool isClosingAnimating = false;

        private Grid rootGrid;
        private ScaleTransform rootScale;
        private System.Windows.Shapes.Rectangle crtBeam;
        private System.Windows.Shapes.Ellipse crtSpark;
        private DropShadowEffect winShadow;

        public ControlPanelWindow(WidgetWindow widgetWin)
        {
            this.widget = widgetWin;

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            configPath = Path.Combine(appDir, "settings.ini");

            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = System.Windows.Media.Brushes.Transparent;
            this.Title = "Gothic Clock - Mainframe";
            this.Width = 460;
            this.Height = 800;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.ResizeMode = ResizeMode.CanMinimize;

            LoadCustomStyles();
            BuildUI();
            LoadConfig();
            SetupTray();

            this.Closing += delegate(object s, System.ComponentModel.CancelEventArgs e) {
                e.Cancel = true;
                this.AnimateClose();
            };
        }

        private void LoadCustomStyles()
        {
            string xaml = @"<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">

                <Style TargetType=""ScrollBar"">
                    <Setter Property=""Background"" Value=""Transparent"" />
                    <Setter Property=""Width"" Value=""5"" />
                    <Setter Property=""Template"">
                        <Setter.Value>
                            <ControlTemplate TargetType=""ScrollBar"">
                                <Grid Background=""#09050A"">
                                    <Track x:Name=""PART_Track"" IsDirectionReversed=""True"">
                                        <Track.Thumb>
                                            <Thumb>
                                                <Thumb.Template>
                                                    <ControlTemplate TargetType=""Thumb"">
                                                        <Border Background=""#FF2A55"" CornerRadius=""1"" Opacity=""0.85"" />
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
                                    <Border Height=""4"" Background=""#140816"" BorderBrush=""#3B1728"" BorderThickness=""1"" CornerRadius=""1"" />
                                    <Track x:Name=""PART_Track"">
                                        <Track.Thumb>
                                            <Thumb Cursor=""Hand"">
                                                <Thumb.Template>
                                                    <ControlTemplate TargetType=""Thumb"">
                                                        <Grid Width=""14"" Height=""14"">
                                                            <Border Background=""#00F0FF"" BorderBrush=""#FF2A55"" BorderThickness=""1.5"" CornerRadius=""2"">
                                                                <Border.Effect>
                                                                    <DropShadowEffect Color=""#00F0FF"" BlurRadius=""8"" ShadowDepth=""0"" Opacity=""0.9"" />
                                                                </Border.Effect>
                                                            </Border>
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

                <Style TargetType=""ComboBoxItem"">
                    <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
                    <Setter Property=""Foreground"" Value=""#E2E8F0"" />
                    <Setter Property=""Background"" Value=""#0B050C"" />
                    <Setter Property=""FontFamily"" Value=""Consolas, monospace"" />
                    <Setter Property=""FontSize"" Value=""11"" />
                    <Setter Property=""Padding"" Value=""10,6,10,6"" />
                    <Setter Property=""Template"">
                        <Setter.Value>
                            <ControlTemplate TargetType=""ComboBoxItem"">
                                <Border Name=""ItemBorder""
                                    Padding=""{TemplateBinding Padding}""
                                    Background=""{TemplateBinding Background}""
                                    BorderBrush=""Transparent""
                                    BorderThickness=""0,0,0,1"">
                                    <ContentPresenter />
                                </Border>
                                <ControlTemplate.Triggers>
                                    <Trigger Property=""IsHighlighted"" Value=""True"">
                                        <Setter TargetName=""ItemBorder"" Property=""Background"" Value=""#1E0C22"" />
                                        <Setter TargetName=""ItemBorder"" Property=""BorderBrush"" Value=""#FF2A55"" />
                                        <Setter Property=""Foreground"" Value=""#00F0FF"" />
                                    </Trigger>
                                    <Trigger Property=""IsSelected"" Value=""True"">
                                        <Setter TargetName=""ItemBorder"" Property=""Background"" Value=""#2B0F2A"" />
                                        <Setter TargetName=""ItemBorder"" Property=""BorderBrush"" Value=""#FF2A55"" />
                                        <Setter Property=""Foreground"" Value=""#FFFFFF"" />
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>

                <Style TargetType=""ComboBox"">
                    <Setter Property=""Foreground"" Value=""#00F0FF"" />
                    <Setter Property=""FontFamily"" Value=""Consolas, monospace"" />
                    <Setter Property=""FontSize"" Value=""11"" />
                    <Setter Property=""FontWeight"" Value=""SemiBold"" />
                    <Setter Property=""Height"" Value=""28"" />
                    <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
                    <Setter Property=""Template"">
                        <Setter.Value>
                            <ControlTemplate TargetType=""ComboBox"">
                                <Grid>
                                    <ToggleButton Name=""ToggleButton""
                                        Background=""#120814""
                                        BorderBrush=""#FF2A55""
                                        BorderThickness=""1""
                                        Focusable=""False""
                                        IsChecked=""{Binding Path=IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}""
                                        ClickMode=""Press"">
                                        <ToggleButton.Template>
                                            <ControlTemplate TargetType=""ToggleButton"">
                                                <Border Name=""Border""
                                                    Background=""{TemplateBinding Background}""
                                                    BorderBrush=""{TemplateBinding BorderBrush}""
                                                    BorderThickness=""{TemplateBinding BorderThickness}""
                                                    CornerRadius=""2""
                                                    Padding=""10,4,8,4"">
                                                    <Grid>
                                                        <Grid.ColumnDefinitions>
                                                            <ColumnDefinition Width=""*"" />
                                                            <ColumnDefinition Width=""20"" />
                                                        </Grid.ColumnDefinitions>
                                                        <Path Grid.Column=""1""
                                                            HorizontalAlignment=""Center""
                                                            VerticalAlignment=""Center""
                                                            Data=""M 0 0 L 4 4 L 8 0 Z""
                                                            Fill=""#00F0FF"" />
                                                    </Grid>
                                                </Border>
                                                <ControlTemplate.Triggers>
                                                    <Trigger Property=""IsMouseOver"" Value=""True"">
                                                        <Setter TargetName=""Border"" Property=""BorderBrush"" Value=""#00F0FF"" />
                                                        <Setter TargetName=""Border"" Property=""Background"" Value=""#1E0C22"" />
                                                    </Trigger>
                                                    <Trigger Property=""IsChecked"" Value=""True"">
                                                        <Setter TargetName=""Border"" Property=""BorderBrush"" Value=""#00F0FF"" />
                                                    </Trigger>
                                                </ControlTemplate.Triggers>
                                            </ControlTemplate>
                                        </ToggleButton.Template>
                                    </ToggleButton>
                                    <ContentPresenter Name=""ContentSite""
                                        IsHitTestVisible=""False""
                                        Content=""{TemplateBinding SelectionBoxItem}""
                                        ContentTemplate=""{TemplateBinding SelectionBoxItemTemplate}""
                                        ContentTemplateSelector=""{TemplateBinding ItemTemplateSelector}""
                                        Margin=""10,0,24,0""
                                        VerticalAlignment=""Center""
                                        HorizontalAlignment=""Left"" />
                                    <Popup Name=""PART_Popup""
                                        Placement=""Bottom""
                                        IsOpen=""{TemplateBinding IsDropDownOpen}""
                                        AllowsTransparency=""True""
                                        Focusable=""False""
                                        PopupAnimation=""Slide"">
                                        <Grid Name=""DropDown""
                                            SnapsToDevicePixels=""True""
                                            MinWidth=""{TemplateBinding ActualWidth}""
                                            MaxHeight=""{TemplateBinding MaxDropDownHeight}"">
                                            <Border Name=""DropDownBorder""
                                                Background=""#0B050C""
                                                BorderBrush=""#FF2A55""
                                                BorderThickness=""1""
                                                CornerRadius=""2""
                                                Margin=""0,2,0,4"">
                                                <Border.Effect>
                                                    <DropShadowEffect Color=""#FF2A55"" BlurRadius=""12"" ShadowDepth=""0"" Opacity=""0.5"" />
                                                </Border.Effect>
                                                <ScrollViewer Margin=""2"" SnapsToDevicePixels=""True"">
                                                    <StackPanel IsItemsHost=""True"" KeyboardNavigation.DirectionalNavigation=""Contained"" />
                                                </ScrollViewer>
                                            </Border>
                                        </Grid>
                                    </Popup>
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
            catch (Exception ex)
            {
                try {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "xaml_error.log"), ex.ToString());
                } catch { }
            }
        }

        private void BuildUI()
        {
            rootGrid = new Grid();
            rootGrid.Width = 440;
            rootGrid.Height = 780;
            rootGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            rootGrid.VerticalAlignment = VerticalAlignment.Center;

            rootScale = new ScaleTransform(1.0, 1.0);
            rootGrid.RenderTransform = rootScale;
            rootGrid.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

            // Cyberpunk Chamfered Outer Frame (22px 45-deg cuts top-left and bottom-right)
            System.Windows.Shapes.Path bgFrame = new System.Windows.Shapes.Path();
            bgFrame.Data = Geometry.Parse("M 22 0 L 434 0 L 440 6 L 440 758 L 418 780 L 6 780 L 0 774 L 0 22 Z");
            bgFrame.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(11, 7, 12));
            bgFrame.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 42, 85));
            bgFrame.StrokeThickness = 1.5;

            winShadow = new DropShadowEffect();
            winShadow.Color = System.Windows.Media.Color.FromRgb(255, 42, 85);
            winShadow.BlurRadius = 24;
            winShadow.ShadowDepth = 0;
            winShadow.Opacity = 0.35;
            bgFrame.Effect = winShadow;
            rootGrid.Children.Add(bgFrame);

            // High-Tech Corner Armor Notches
            System.Windows.Shapes.Path tlNotch = new System.Windows.Shapes.Path();
            tlNotch.Data = Geometry.Parse("M 26 0 L 4 0 L 0 4 L 0 26");
            tlNotch.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 85, 119));
            tlNotch.StrokeThickness = 2.5;
            rootGrid.Children.Add(tlNotch);

            System.Windows.Shapes.Path brNotch = new System.Windows.Shapes.Path();
            brNotch.Data = Geometry.Parse("M 414 780 L 436 780 L 440 776 L 440 754");
            brNotch.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 85, 119));
            brNotch.StrokeThickness = 2.5;
            rootGrid.Children.Add(brNotch);

            DockPanel mainDock = new DockPanel();
            mainDock.Margin = new Thickness(14, 10, 14, 12);
            rootGrid.Children.Add(mainDock);

            // =================================================================
            // 1. TOP HEADER (MAINFRAME // TABS // MIN & CLOSE)
            // =================================================================
            Grid titleBar = new Grid();
            titleBar.Height = 40;
            titleBar.Background = System.Windows.Media.Brushes.Transparent;
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
            spTitleLeft.Margin = new Thickness(4, 0, 0, 0);

            string titleIconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(titleIconPath))
            {
                try
                {
                    System.Windows.Controls.Image titleImg = new System.Windows.Controls.Image();
                    titleImg.Width = 18;
                    titleImg.Height = 18;
                    titleImg.Margin = new Thickness(0, 0, 8, 0);
                    titleImg.Source = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(titleIconPath));
                    spTitleLeft.Children.Add(titleImg);
                }
                catch { }
            }

            TextBlock tbTitle = new TextBlock();
            tbTitle.Text = "MAINFRAME";
            tbTitle.FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Arial Black, Impact, sans-serif");
            tbTitle.FontSize = 18;
            tbTitle.FontWeight = FontWeights.Bold;
            tbTitle.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 51, 85));
            tbTitle.VerticalAlignment = VerticalAlignment.Center;
            tbTitle.Margin = new Thickness(0, 0, 12, 0);
            spTitleLeft.Children.Add(tbTitle);

            // Header Pill 1: Settings / Status (Active)
            btnHeaderStatus = new System.Windows.Controls.Button();
            btnHeaderStatus.Content = "Widget: ON";
            btnHeaderStatus.Height = 24;
            btnHeaderStatus.Padding = new Thickness(10, 0, 10, 0);
            btnHeaderStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
            btnHeaderStatus.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(12, 32, 40));
            btnHeaderStatus.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 229, 255));
            btnHeaderStatus.BorderThickness = new Thickness(1);
            btnHeaderStatus.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
            btnHeaderStatus.FontSize = 10.5;
            btnHeaderStatus.FontWeight = FontWeights.Bold;
            btnHeaderStatus.Cursor = System.Windows.Input.Cursors.Hand;
            btnHeaderStatus.Margin = new Thickness(0, 0, 6, 0);
            btnHeaderStatus.Click += delegate {
                isMasterActive = !isMasterActive;
                ApplyMasterActive();
                SaveConfig();
            };
            spTitleLeft.Children.Add(btnHeaderStatus);

            // Header Pill 2: Aura Themes
            btnHeaderThemes = new System.Windows.Controls.Button();
            btnHeaderThemes.Content = "Aura Themes";
            btnHeaderThemes.Height = 24;
            btnHeaderThemes.Padding = new Thickness(10, 0, 10, 0);
            btnHeaderThemes.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 77, 109));
            btnHeaderThemes.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 10, 18));
            btnHeaderThemes.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 24, 38));
            btnHeaderThemes.BorderThickness = new Thickness(1);
            btnHeaderThemes.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
            btnHeaderThemes.FontSize = 10.5;
            btnHeaderThemes.FontWeight = FontWeights.Bold;
            btnHeaderThemes.Cursor = System.Windows.Input.Cursors.Hand;
            btnHeaderThemes.Margin = new Thickness(0, 0, 6, 0);
            btnHeaderThemes.Click += delegate {
                if (scroll != null) scroll.ScrollToVerticalOffset(280);
            };
            spTitleLeft.Children.Add(btnHeaderThemes);

            titleBar.Children.Add(spTitleLeft);

            // Window Min & Close buttons
            StackPanel spTitleRight = new StackPanel();
            spTitleRight.Orientation = System.Windows.Controls.Orientation.Horizontal;
            spTitleRight.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;

            System.Windows.Controls.Button btnMin = CreateTitleButton("min", false);
            btnMin.Click += delegate { this.WindowState = WindowState.Minimized; };

            System.Windows.Controls.Button btnClose = CreateTitleButton("close", true);
            btnClose.Click += delegate { this.AnimateClose(); };

            spTitleRight.Children.Add(btnMin);
            spTitleRight.Children.Add(btnClose);
            titleBar.Children.Add(spTitleRight);

            mainDock.Children.Add(titleBar);

            // =================================================================
            // 2. SCROLLABLE MAINFRAME BODY
            // =================================================================
            scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.Background = System.Windows.Media.Brushes.Transparent;
            scroll.BorderThickness = new Thickness(0);

            StackPanel content = new StackPanel();
            content.Margin = new Thickness(4, 4, 8, 12);

            // -----------------------------------------------------------------
            // SECTION 1: CHRONO CORE (BRAIN)
            // -----------------------------------------------------------------
            content.Children.Add(CreateCyberSectionHeader("CHRONO CORE", "Universal Roll Engine"));

            TextBlock tbCoreSub = new TextBlock();
            tbCoreSub.Text = "Master scale and precision synchronization for all clock elements";
            tbCoreSub.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
            tbCoreSub.FontSize = 10.5;
            tbCoreSub.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(142, 117, 130));
            tbCoreSub.Margin = new Thickness(4, 0, 0, 6);
            content.Children.Add(tbCoreSub);

            // Master Scale Slider
            content.Children.Add(CreateCyberFieldLabel("Master Chrono Scale (Widget Zoom):"));
            DockPanel dpMasterScale = new DockPanel();
            dpMasterScale.Margin = new Thickness(4, 0, 0, 6);

            txtMasterScaleVal = new TextBlock();
            txtMasterScaleVal.Text = "85%";
            txtMasterScaleVal.Width = 46;
            txtMasterScaleVal.TextAlignment = TextAlignment.Right;
            txtMasterScaleVal.VerticalAlignment = VerticalAlignment.Center;
            txtMasterScaleVal.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
            txtMasterScaleVal.FontWeight = FontWeights.Bold;
            txtMasterScaleVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
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
            content.Children.Add(dpMasterScale);

            // Quick Scale Presets
            UniformGrid gridPresets = new UniformGrid();
            gridPresets.Columns = 5;
            gridPresets.Margin = new Thickness(2, 0, 2, 8);
            gridPresets.Children.Add(CreateSmallPresetButton("40%", 0.40));
            gridPresets.Children.Add(CreateSmallPresetButton("65%", 0.65));
            gridPresets.Children.Add(CreateSmallPresetButton("85%", 0.85));
            gridPresets.Children.Add(CreateSmallPresetButton("120%", 1.20));
            gridPresets.Children.Add(CreateSmallPresetButton("180%", 1.80));
            content.Children.Add(gridPresets);

            // Spacing Slider
            content.Children.Add(CreateCyberFieldLabel("Vertical Element Line Spacing:"));
            DockPanel dpSpacing = new DockPanel();
            dpSpacing.Margin = new Thickness(4, 0, 0, 6);

            txtSpacingVal = new TextBlock();
            txtSpacingVal.Text = "2px";
            txtSpacingVal.Width = 46;
            txtSpacingVal.TextAlignment = TextAlignment.Right;
            txtSpacingVal.VerticalAlignment = VerticalAlignment.Center;
            txtSpacingVal.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
            txtSpacingVal.FontWeight = FontWeights.Bold;
            txtSpacingVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
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
            content.Children.Add(dpSpacing);

            // Universal Animation Diamond Toggle
            toggleAnim = new CyberToggle("UNIVERSAL ROLLING ANIMATION", "Smooth vertical digit physics across all rows", true, delegate(bool val) {
                widget.EnableAnimation = val;
                SaveConfig();
            });
            content.Children.Add(toggleAnim.Container);

            // Animation Speed
            content.Children.Add(CreateCyberFieldLabel("Animation Roll Duration:"));
            DockPanel dpAnimSpeed = new DockPanel();
            dpAnimSpeed.Margin = new Thickness(4, 0, 0, 6);

            txtAnimSpeedVal = new TextBlock();
            txtAnimSpeedVal.Text = "320ms";
            txtAnimSpeedVal.Width = 46;
            txtAnimSpeedVal.TextAlignment = TextAlignment.Right;
            txtAnimSpeedVal.VerticalAlignment = VerticalAlignment.Center;
            txtAnimSpeedVal.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
            txtAnimSpeedVal.FontWeight = FontWeights.Bold;
            txtAnimSpeedVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
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
            content.Children.Add(dpAnimSpeed);

            btnPreviewAnim = CreateCyberButton("[ TEST ROLLING DIGIT ANIMATION ]", 30);
            btnPreviewAnim.Margin = new Thickness(2, 2, 2, 6);
            btnPreviewAnim.Click += delegate {
                widget.ForceRollAll();
            };
            content.Children.Add(btnPreviewAnim);

            content.Children.Add(CreateAmberCallout("Universal digit roll engine synchronizes hours, minutes, and rolling seconds simultaneously."));

            // -----------------------------------------------------------------
            // SECTION 2: REIATSU & NEON AURA (VOICE)
            // -----------------------------------------------------------------
            content.Children.Add(CreateCyberSectionHeader("REIATSU & NEON AURA", "Spiritual Glow Physics"));

            TextBlock tbAuraSub = new TextBlock();
            tbAuraSub.Text = "Select spiritual pressure aura or custom atmospheric bloom";
            tbAuraSub.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
            tbAuraSub.FontSize = 10.5;
            tbAuraSub.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(142, 117, 130));
            tbAuraSub.Margin = new Thickness(4, 0, 0, 6);
            content.Children.Add(tbAuraSub);

            // Diamond Theme Selectors
            themeItems.Clear();
            themeItems.Add(new ThemeOptionItem("Getsuga", "Getsuga", "月牙天衝 · Pure Bone White with Crimson Blood Reiatsu", System.Windows.Media.Color.FromRgb(225, 29, 72), SelectTheme));
            themeItems.Add(new ThemeOptionItem("Bankai", "Bankai", "天鎖斬月 · Stark Pure White with Void Shadow", System.Windows.Media.Color.FromRgb(250, 250, 255), SelectTheme));
            themeItems.Add(new ThemeOptionItem("Hollow", "Hollow", "虚化 · Hollow Bone White with Dark Blood Crimson", System.Windows.Media.Color.FromRgb(153, 27, 27), SelectTheme));
            themeItems.Add(new ThemeOptionItem("Ichigo", "Ichigo", "黒崎一護 · Radiant Light with Flame Reiatsu Orange", System.Windows.Media.Color.FromRgb(234, 88, 12), SelectTheme));
            themeItems.Add(new ThemeOptionItem("Quincy", "Quincy", "滅却師 · Spirit Silver with Sacred Quincy Blue", System.Windows.Media.Color.FromRgb(2, 132, 199), SelectTheme));
            themeItems.Add(new ThemeOptionItem("Cero", "Cero", "黒虚閃 · Spectral White with Ulquiorra Emerald", System.Windows.Media.Color.FromRgb(16, 185, 129), SelectTheme));
            themeItems.Add(new ThemeOptionItem("Senbonzakura", "Senbonzakura", "千本桜 · Cherry Blossom Reiatsu Pink", System.Windows.Media.Color.FromRgb(244, 63, 94), SelectTheme));
            themeItems.Add(new ThemeOptionItem("Moonlight", "Moonlight", "月光 · Classic Gothic Moonlight Silver", System.Windows.Media.Color.FromRgb(148, 163, 184), SelectTheme));

            foreach (var item in themeItems)
            {
                content.Children.Add(item.Container);
            }

            // Spiritual Pressure Bloom Radius Slider
            content.Children.Add(CreateCyberFieldLabel("Spiritual Pressure (Bloom Radius):"));
            DockPanel dpGlowRad = new DockPanel();
            dpGlowRad.Margin = new Thickness(4, 0, 0, 6);

            txtGlowRadiusVal = new TextBlock();
            txtGlowRadiusVal.Text = "18px";
            txtGlowRadiusVal.Width = 46;
            txtGlowRadiusVal.TextAlignment = TextAlignment.Right;
            txtGlowRadiusVal.VerticalAlignment = VerticalAlignment.Center;
            txtGlowRadiusVal.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
            txtGlowRadiusVal.FontWeight = FontWeights.Bold;
            txtGlowRadiusVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
            DockPanel.SetDock(txtGlowRadiusVal, Dock.Right);

            sliderGlowRadius = new System.Windows.Controls.Slider();
            sliderGlowRadius.Minimum = 0;
            sliderGlowRadius.Maximum = 40;
            sliderGlowRadius.Value = 18;
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
            content.Children.Add(dpGlowRad);

            // Motion Easing Style
            content.Children.Add(CreateCyberFieldLabel("Animation Motion Physics:"));
            cmbMotionStyle = new System.Windows.Controls.ComboBox();
            cmbMotionStyle.Items.Add("Tactile Spring Snap (Overshoot Bounce)");
            cmbMotionStyle.Items.Add("Smooth Fluid Glide (Cubic Ease)");
            cmbMotionStyle.Items.Add("Micro Bounce");
            cmbMotionStyle.Items.Add("Classic Linear");
            cmbMotionStyle.SelectedIndex = 0;
            cmbMotionStyle.Margin = new Thickness(2, 0, 2, 6);
            cmbMotionStyle.SelectionChanged += delegate {
                if (cmbMotionStyle.SelectedIndex == 0) widget.MotionStyle = "Spring";
                else if (cmbMotionStyle.SelectedIndex == 1) widget.MotionStyle = "Smooth";
                else if (cmbMotionStyle.SelectedIndex == 2) widget.MotionStyle = "Bounce";
                else if (cmbMotionStyle.SelectedIndex == 3) widget.MotionStyle = "Linear";
                widget.SetGlowTheme(widget.GlowTheme);
                SaveConfig();
            };
            content.Children.Add(cmbMotionStyle);

            // Colon Pulse Style
            content.Children.Add(CreateCyberFieldLabel("Colon (:) Pulse Effect:"));
            cmbColonStyle = new System.Windows.Controls.ComboBox();
            cmbColonStyle.Items.Add("Smooth Breathing Pulse (Hypnotic Sine-Wave)");
            cmbColonStyle.Items.Add("1-Second Rhythm Blink");
            cmbColonStyle.Items.Add("Solid (Static)");
            cmbColonStyle.SelectedIndex = 0;
            cmbColonStyle.Margin = new Thickness(2, 0, 2, 6);
            cmbColonStyle.SelectionChanged += delegate {
                if (cmbColonStyle.SelectedIndex == 0) widget.ColonStyle = "Breath";
                else if (cmbColonStyle.SelectedIndex == 1) widget.ColonStyle = "Blink";
                else if (cmbColonStyle.SelectedIndex == 2) widget.ColonStyle = "Solid";
                widget.UpdateColonAnimation();
                SaveConfig();
            };
            content.Children.Add(cmbColonStyle);

            // Toggles for divider and glass plate
            toggleDivider = new CyberToggle("ORNATE GOTHIC DIAMOND DIVIDER", "Filigree accent divider between time and date", true, delegate(bool val) {
                widget.ShowDivider = val;
                widget.RefreshAppearance();
                SaveConfig();
            });
            content.Children.Add(toggleDivider.Container);

            toggleGlass = new CyberToggle("SMOKED CYBER-GLASS BACKPLATE", "Translucent dark acrylic backplate", false, delegate(bool val) {
                widget.SetGlassPlate(val);
                SaveConfig();
            });
            content.Children.Add(toggleGlass.Container);

            // -----------------------------------------------------------------
            // SECTION 3: TIME CIRCUITS
            // -----------------------------------------------------------------
            content.Children.Add(CreateCyberSectionHeader("TIME CIRCUITS", "12H / 24H Cycle"));

            toggleShowTime = new CyberToggle("SHOW TIME ELEMENT", "Primary chrono display (hours, minutes, seconds)", true, delegate(bool val) {
                widget.ShowTime = val;
                widget.RefreshAppearance();
                SaveConfig();
            });
            content.Children.Add(toggleShowTime.Container);

            content.Children.Add(CreateCyberFieldLabel("Time Font Size (Chrono Scale):"));
            DockPanel dpTimeSize = new DockPanel();
            dpTimeSize.Margin = new Thickness(4, 0, 0, 6);

            txtTimeSizeVal = new TextBlock();
            txtTimeSizeVal.Text = "28px";
            txtTimeSizeVal.Width = 46;
            txtTimeSizeVal.TextAlignment = TextAlignment.Right;
            txtTimeSizeVal.VerticalAlignment = VerticalAlignment.Center;
            txtTimeSizeVal.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
            txtTimeSizeVal.FontWeight = FontWeights.Bold;
            txtTimeSizeVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
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
            content.Children.Add(dpTimeSize);

            toggle12Hour = new CyberToggle("12-HOUR CYCLE (AM/PM)", "Standard 12h format or 24h military chrono", true, delegate(bool val) {
                widget.Is12Hour = val;
                widget.RefreshAppearance();
                SaveConfig();
            });
            content.Children.Add(toggle12Hour.Container);

            toggleSeconds = new CyberToggle("SHOW ROLLING SECONDS", "Live ticking vertical seconds flip", true, delegate(bool val) {
                widget.ShowSeconds = val;
                widget.RefreshAppearance();
                SaveConfig();
            });
            content.Children.Add(toggleSeconds.Container);

            // -----------------------------------------------------------------
            // SECTION 4: DAY & DATE TELEMETRY
            // -----------------------------------------------------------------
            content.Children.Add(CreateCyberSectionHeader("DAY & DATE TELEMETRY", "Inscription Core"));

            toggleShowDay = new CyberToggle("SHOW DAY OF WEEK", "e.g. Wednesday", true, delegate(bool val) {
                widget.ShowDay = val;
                widget.RefreshAppearance();
                SaveConfig();
            });
            content.Children.Add(toggleShowDay.Container);

            content.Children.Add(CreateCyberFieldLabel("Day Font Size:"));
            DockPanel dpDaySize = new DockPanel();
            dpDaySize.Margin = new Thickness(4, 0, 0, 6);

            txtDaySizeVal = new TextBlock();
            txtDaySizeVal.Text = "14px";
            txtDaySizeVal.Width = 46;
            txtDaySizeVal.TextAlignment = TextAlignment.Right;
            txtDaySizeVal.VerticalAlignment = VerticalAlignment.Center;
            txtDaySizeVal.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
            txtDaySizeVal.FontWeight = FontWeights.Bold;
            txtDaySizeVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
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
            content.Children.Add(dpDaySize);

            toggleTodayPrefix = new CyberToggle("INCLUDE 'TODAY IS' PREFIX", "e.g. Today is Wednesday vs Wednesday", true, delegate(bool val) {
                widget.ShowTodayPrefix = val;
                widget.RefreshAppearance();
                SaveConfig();
            });
            content.Children.Add(toggleTodayPrefix.Container);

            toggleShowDate = new CyberToggle("SHOW DATE ELEMENT", "Calendar date inscription", true, delegate(bool val) {
                widget.ShowDate = val;
                widget.RefreshAppearance();
                SaveConfig();
            });
            content.Children.Add(toggleShowDate.Container);

            content.Children.Add(CreateCyberFieldLabel("Date Font Size:"));
            DockPanel dpDateSize = new DockPanel();
            dpDateSize.Margin = new Thickness(4, 0, 0, 6);

            txtDateSizeVal = new TextBlock();
            txtDateSizeVal.Text = "13px";
            txtDateSizeVal.Width = 46;
            txtDateSizeVal.TextAlignment = TextAlignment.Right;
            txtDateSizeVal.VerticalAlignment = VerticalAlignment.Center;
            txtDateSizeVal.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
            txtDateSizeVal.FontWeight = FontWeights.Bold;
            txtDateSizeVal.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
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
            content.Children.Add(dpDateSize);

            content.Children.Add(CreateCyberFieldLabel("Date Format Archetype:"));
            cmbDateFormat = new System.Windows.Controls.ComboBox();
            cmbDateFormat.Items.Add("MM/dd/yyyy  (11/11/2026)");
            cmbDateFormat.Items.Add("dd/MM/yyyy  (11/11/2026)");
            cmbDateFormat.Items.Add("yyyy-MM-dd  (2026-11-11)");
            cmbDateFormat.Items.Add("MMMM d, yyyy (November 11, 2026)");
            cmbDateFormat.SelectedIndex = 0;
            cmbDateFormat.Margin = new Thickness(2, 0, 2, 6);
            cmbDateFormat.SelectionChanged += delegate {
                if (cmbDateFormat.SelectedIndex == 0) widget.DateFormat = "MM/dd/yyyy";
                else if (cmbDateFormat.SelectedIndex == 1) widget.DateFormat = "dd/MM/yyyy";
                else if (cmbDateFormat.SelectedIndex == 2) widget.DateFormat = "yyyy-MM-dd";
                else if (cmbDateFormat.SelectedIndex == 3) widget.DateFormat = "MMMM d, yyyy";
                widget.RefreshAppearance();
                SaveConfig();
            };
            content.Children.Add(cmbDateFormat);

            // -----------------------------------------------------------------
            // SECTION 5: COORDINATES & POSITION (CAPRI)
            // -----------------------------------------------------------------
            content.Children.Add(CreateCyberSectionHeader("COORDINATES & POSITION", "Drag & Anchor"));

            UniformGrid gridPos1 = new UniformGrid();
            gridPos1.Columns = 3;
            gridPos1.Margin = new Thickness(2, 2, 2, 2);

            System.Windows.Controls.Button btnTR = CreateCyberButton("[ TOP-RIGHT ]", 28);
            btnTR.Click += delegate { SnapPosition(SystemParameters.WorkArea.Right - (220 * widget.MasterScale) - 20, 30); };

            System.Windows.Controls.Button btnTL = CreateCyberButton("[ TOP-LEFT ]", 28);
            btnTL.Click += delegate { SnapPosition(30, 30); };

            System.Windows.Controls.Button btnCenter = CreateCyberButton("[ CENTER ]", 28);
            btnCenter.Click += delegate { SnapPosition((SystemParameters.WorkArea.Width - (220 * widget.MasterScale)) / 2, (SystemParameters.WorkArea.Height - (90 * widget.MasterScale)) / 2); };

            gridPos1.Children.Add(btnTR);
            gridPos1.Children.Add(btnTL);
            gridPos1.Children.Add(btnCenter);
            content.Children.Add(gridPos1);

            UniformGrid gridPos2 = new UniformGrid();
            gridPos2.Columns = 3;
            gridPos2.Margin = new Thickness(2, 0, 2, 4);

            System.Windows.Controls.Button btnBR = CreateCyberButton("[ BTM-RIGHT ]", 28);
            btnBR.Click += delegate { SnapPosition(SystemParameters.WorkArea.Right - (220 * widget.MasterScale) - 20, SystemParameters.WorkArea.Bottom - (90 * widget.MasterScale) - 30); };

            System.Windows.Controls.Button btnBL = CreateCyberButton("[ BTM-LEFT ]", 28);
            btnBL.Click += delegate { SnapPosition(30, SystemParameters.WorkArea.Bottom - (90 * widget.MasterScale) - 30); };

            System.Windows.Controls.Button btnReset = CreateCyberButton("[ RESET ]", 28);
            btnReset.Click += delegate { SnapPosition(SystemParameters.WorkArea.Right - 230, 40); };

            gridPos2.Children.Add(btnBR);
            gridPos2.Children.Add(btnBL);
            gridPos2.Children.Add(btnReset);
            content.Children.Add(gridPos2);

            btnMoveMode = CreateCyberButton("[ ENTER MOVE MODE // DRAG ON SCREEN ]", 32);
            btnMoveMode.Margin = new Thickness(2, 2, 2, 6);
            btnMoveMode.Click += delegate {
                bool newMode = !widget.IsMoveMode;
                widget.SetMoveMode(newMode);
                if (newMode)
                {
                    btnMoveMode.Content = "[ LOCK COORDINATES // DONE DRAGGING ]";
                    btnMoveMode.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11));
                    btnMoveMode.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11));
                    btnMoveMode.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 18, 10));
                }
                else
                {
                    btnMoveMode.Content = "[ ENTER MOVE MODE // DRAG ON SCREEN ]";
                    btnMoveMode.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 42, 85));
                    btnMoveMode.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 77, 109));
                    btnMoveMode.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 10, 18));
                    SaveConfig();
                }
            };
            content.Children.Add(btnMoveMode);

            // -----------------------------------------------------------------
            // SECTION 6: SYSTEM DAEMON & MAINTENANCE
            // -----------------------------------------------------------------
            content.Children.Add(CreateCyberSectionHeader("SYSTEM DAEMON", "WorkerW Layer · Win+D Immune"));

            toggleAutoStart = new CyberToggle("START DAEMON SILENTLY ON WINDOWS BOOT", "Auto-launches into background on user login", IsStartupEnabled(), delegate(bool val) {
                SetStartupEnabled(val);
                SaveConfig();
            });
            content.Children.Add(toggleAutoStart.Container);

            toggleStayDesktop = new CyberToggle("STAY AT MAIN DESKTOP LAYER", "Pins above wallpaper & icons, survives Win+D show-desktop", widget.StayOnDesktop, delegate(bool val) {
                widget.StayOnDesktop = val;
                if (val && widget.IsAlwaysOnTop)
                {
                    widget.IsAlwaysOnTop = false;
                    if (toggleAlwaysOnTop != null) toggleAlwaysOnTop.IsChecked = false;
                }
                widget.AttachToDesktop();
                SaveConfig();
            });
            content.Children.Add(toggleStayDesktop.Container);

            toggleAlwaysOnTop = new CyberToggle("ALWAYS ON TOP (CYBER HUD OVERLAY)", "Floats above all full-screen windows and apps", widget.IsAlwaysOnTop, delegate(bool val) {
                widget.IsAlwaysOnTop = val;
                if (val && widget.StayOnDesktop)
                {
                    widget.StayOnDesktop = false;
                    if (toggleStayDesktop != null) toggleStayDesktop.IsChecked = false;
                }
                widget.AttachToDesktop();
                SaveConfig();
            });
            content.Children.Add(toggleAlwaysOnTop.Container);

            toggleCloseAnimation = new CyberToggle("CYBERPUNK DE-REZ SHUTDOWN FX", "Holographic CRT electron beam collapse on window close/exit", true, delegate(bool val) {
                EnableCloseAnimation = val;
                SaveConfig();
            });
            content.Children.Add(toggleCloseAnimation.Container);

            System.Windows.Controls.Button btnTestDeRez = CreateCyberButton("[ PREVIEW CLOSE ANIMATION ]", 26);
            btnTestDeRez.Margin = new Thickness(2, 2, 2, 8);
            btnTestDeRez.Click += delegate {
                this.AnimateClose(delegate {
                    DispatcherTimer reopenTimer = new DispatcherTimer();
                    reopenTimer.Interval = TimeSpan.FromMilliseconds(550);
                    reopenTimer.Tick += delegate {
                        reopenTimer.Stop();
                        this.AnimateOpen();
                    };
                    reopenTimer.Start();
                });
            };
            content.Children.Add(btnTestDeRez);

            // Bottom action buttons (like Check for updates, Help, Quit Capri)
            UniformGrid gridBottom = new UniformGrid();
            gridBottom.Columns = 3;
            gridBottom.Margin = new Thickness(2, 8, 2, 4);

            System.Windows.Controls.Button btnCloseApp = CreateCyberButton("[ CLOSE ]", 30);
            btnCloseApp.Click += delegate {
                this.AnimateClose();
            };

            System.Windows.Controls.Button btnQuitApp = CreateCyberButton("[ QUIT APP ]", 30);
            btnQuitApp.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 42, 85));
            btnQuitApp.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 80, 100));
            btnQuitApp.Click += delegate {
                ShutdownApplication();
            };

            System.Windows.Controls.Button btnUninstall = CreateCyberButton("[ UNINSTALL ]", 30);
            btnUninstall.Click += delegate {
                PerformUninstall();
            };

            gridBottom.Children.Add(btnCloseApp);
            gridBottom.Children.Add(btnQuitApp);
            gridBottom.Children.Add(btnUninstall);
            content.Children.Add(gridBottom);

            // Footer note
            TextBlock tbFooter = new TextBlock();
            tbFooter.Text = "Gothic Chronometer · Bleach & Cyberpunk Edition · Daemon Active.";
            tbFooter.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
            tbFooter.FontSize = 9.5;
            tbFooter.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(110, 90, 101));
            tbFooter.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            tbFooter.Margin = new Thickness(0, 4, 0, 8);
            content.Children.Add(tbFooter);

            scroll.Content = content;
            mainDock.Children.Add(scroll);

            // Holographic Central CRT Electron Beam & Spark
            crtBeam = new System.Windows.Shapes.Rectangle();
            crtBeam.Height = 3.0;
            crtBeam.Width = 440;
            crtBeam.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            crtBeam.VerticalAlignment = VerticalAlignment.Center;
            crtBeam.Visibility = Visibility.Collapsed;
            crtBeam.IsHitTestVisible = false;

            LinearGradientBrush beamBrush = new LinearGradientBrush();
            beamBrush.StartPoint = new System.Windows.Point(0, 0.5);
            beamBrush.EndPoint = new System.Windows.Point(1, 0.5);
            beamBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0, 0, 240, 255), 0.0));
            beamBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(255, 0, 240, 255), 0.2));
            beamBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(255, 255, 255, 255), 0.5));
            beamBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(255, 255, 42, 85), 0.8));
            beamBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0, 255, 42, 85), 1.0));
            crtBeam.Fill = beamBrush;

            DropShadowEffect beamGlow = new DropShadowEffect();
            beamGlow.Color = System.Windows.Media.Color.FromRgb(0, 240, 255);
            beamGlow.BlurRadius = 20;
            beamGlow.ShadowDepth = 0;
            beamGlow.Opacity = 1.0;
            crtBeam.Effect = beamGlow;
            rootGrid.Children.Add(crtBeam);

            crtSpark = new System.Windows.Shapes.Ellipse();
            crtSpark.Width = 10;
            crtSpark.Height = 10;
            crtSpark.Fill = new SolidColorBrush(Colors.White);
            crtSpark.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            crtSpark.VerticalAlignment = VerticalAlignment.Center;
            crtSpark.Visibility = Visibility.Collapsed;
            crtSpark.IsHitTestVisible = false;

            DropShadowEffect sparkGlow = new DropShadowEffect();
            sparkGlow.Color = System.Windows.Media.Color.FromRgb(0, 240, 255);
            sparkGlow.BlurRadius = 22;
            sparkGlow.ShadowDepth = 0;
            sparkGlow.Opacity = 1.0;
            crtSpark.Effect = sparkGlow;
            rootGrid.Children.Add(crtSpark);

            this.Content = rootGrid;
        }

        public void AnimateClose(Action onCompleted = null)
        {
            if (isClosingAnimating) return;

            if (!EnableCloseAnimation)
            {
                this.Hide();
                if (onCompleted != null) onCompleted();
                return;
            }

            isClosingAnimating = true;

            if (crtBeam != null)
            {
                crtBeam.Opacity = 1.0;
                crtBeam.Visibility = Visibility.Visible;
            }
            if (crtSpark != null)
            {
                crtSpark.Opacity = 0.0;
                crtSpark.Visibility = Visibility.Visible;
            }

            Storyboard sb = new Storyboard();

            // 1. Initial Voltage Flicker & Drop
            DoubleAnimationUsingKeyFrames animFlicker = new DoubleAnimationUsingKeyFrames();
            animFlicker.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            animFlicker.KeyFrames.Add(new LinearDoubleKeyFrame(0.72, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(35))));
            animFlicker.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(70))));
            animFlicker.KeyFrames.Add(new SplineDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140))));
            animFlicker.KeyFrames.Add(new SplineDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(270))));
            Storyboard.SetTarget(animFlicker, rootGrid);
            Storyboard.SetTargetProperty(animFlicker, new PropertyPath(UIElement.OpacityProperty));
            sb.Children.Add(animFlicker);

            // 2. Vertical Collapse (ScaleY: 1.0 -> 0.003 in 150ms)
            DoubleAnimation animScaleY = new DoubleAnimation();
            animScaleY.From = 1.0;
            animScaleY.To = 0.003;
            animScaleY.Duration = new Duration(TimeSpan.FromMilliseconds(150));
            animScaleY.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            Storyboard.SetTarget(animScaleY, rootScale);
            Storyboard.SetTargetProperty(animScaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
            sb.Children.Add(animScaleY);

            // 3. Horizontal Squeeze (ScaleX: slight bloom to 1.04 at 140ms, then collapse to 0.0 at 270ms)
            DoubleAnimationUsingKeyFrames animScaleX = new DoubleAnimationUsingKeyFrames();
            animScaleX.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            animScaleX.KeyFrames.Add(new SplineDoubleKeyFrame(1.04, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140))));
            animScaleX.KeyFrames.Add(new SplineDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(270))));
            Storyboard.SetTarget(animScaleX, rootScale);
            Storyboard.SetTargetProperty(animScaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
            sb.Children.Add(animScaleX);

            // 4. Beam Glow Surge & Fade
            if (crtBeam != null)
            {
                DoubleAnimationUsingKeyFrames animBeamOp = new DoubleAnimationUsingKeyFrames();
                animBeamOp.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
                animBeamOp.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60))));
                animBeamOp.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160))));
                animBeamOp.KeyFrames.Add(new SplineDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(270))));
                Storyboard.SetTarget(animBeamOp, crtBeam);
                Storyboard.SetTargetProperty(animBeamOp, new PropertyPath(UIElement.OpacityProperty));
                sb.Children.Add(animBeamOp);
            }

            // 5. Central Spark Flare when collapsing into center
            if (crtSpark != null)
            {
                DoubleAnimationUsingKeyFrames animSpark = new DoubleAnimationUsingKeyFrames();
                animSpark.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
                animSpark.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140))));
                animSpark.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
                animSpark.KeyFrames.Add(new SplineDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(270))));
                Storyboard.SetTarget(animSpark, crtSpark);
                Storyboard.SetTargetProperty(animSpark, new PropertyPath(UIElement.OpacityProperty));
                sb.Children.Add(animSpark);
            }

            // 6. Window Neon Shadow Flash
            if (winShadow != null)
            {
                DoubleAnimationUsingKeyFrames animShadow = new DoubleAnimationUsingKeyFrames();
                animShadow.KeyFrames.Add(new LinearDoubleKeyFrame(24, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
                animShadow.KeyFrames.Add(new SplineDoubleKeyFrame(55, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140))));
                animShadow.KeyFrames.Add(new SplineDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(270))));
                Storyboard.SetTarget(animShadow, winShadow);
                Storyboard.SetTargetProperty(animShadow, new PropertyPath(DropShadowEffect.BlurRadiusProperty));
                sb.Children.Add(animShadow);
            }

            sb.Completed += delegate {
                this.Hide();
                rootScale.ScaleX = 1.0;
                rootScale.ScaleY = 1.0;
                rootGrid.Opacity = 1.0;
                if (winShadow != null) winShadow.BlurRadius = 24;
                if (crtBeam != null) { crtBeam.Visibility = Visibility.Collapsed; crtBeam.Opacity = 1.0; }
                if (crtSpark != null) { crtSpark.Visibility = Visibility.Collapsed; crtSpark.Opacity = 0.0; }
                isClosingAnimating = false;
                if (onCompleted != null) onCompleted();
            };

            sb.Begin();
        }

        public void AnimateOpen()
        {
            if (isClosingAnimating) return;

            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();

            if (!EnableCloseAnimation)
            {
                rootScale.ScaleX = 1.0;
                rootScale.ScaleY = 1.0;
                rootGrid.Opacity = 1.0;
                return;
            }

            rootScale.ScaleX = 0.0;
            rootScale.ScaleY = 0.004;
            rootGrid.Opacity = 0.0;

            if (crtBeam != null)
            {
                crtBeam.Opacity = 1.0;
                crtBeam.Visibility = Visibility.Visible;
            }
            if (crtSpark != null)
            {
                crtSpark.Opacity = 1.0;
                crtSpark.Visibility = Visibility.Visible;
            }

            Storyboard sb = new Storyboard();

            // 1. Horizontal beam expansion (0 to 130ms: ScaleX 0.0 -> 1.03)
            DoubleAnimation animScaleX = new DoubleAnimation();
            animScaleX.From = 0.0;
            animScaleX.To = 1.03;
            animScaleX.Duration = new Duration(TimeSpan.FromMilliseconds(130));
            animScaleX.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            Storyboard.SetTarget(animScaleX, rootScale);
            Storyboard.SetTargetProperty(animScaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
            sb.Children.Add(animScaleX);

            // Opacity fade in
            DoubleAnimation animOp = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(100)));
            Storyboard.SetTarget(animOp, rootGrid);
            Storyboard.SetTargetProperty(animOp, new PropertyPath(UIElement.OpacityProperty));
            sb.Children.Add(animOp);

            // 2. Vertical unfold with micro-bounce (110ms to 290ms: ScaleY 0.004 -> 1.0)
            DoubleAnimation animScaleY = new DoubleAnimation();
            animScaleY.From = 0.004;
            animScaleY.To = 1.0;
            animScaleY.BeginTime = TimeSpan.FromMilliseconds(110);
            animScaleY.Duration = new Duration(TimeSpan.FromMilliseconds(180));
            animScaleY.EasingFunction = new BackEase { Amplitude = 0.22, EasingMode = EasingMode.EaseOut };
            Storyboard.SetTarget(animScaleY, rootScale);
            Storyboard.SetTargetProperty(animScaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
            sb.Children.Add(animScaleY);

            // Beam fade out as content reveals
            if (crtBeam != null)
            {
                DoubleAnimation animBeamFade = new DoubleAnimation(1.0, 0.0, new Duration(TimeSpan.FromMilliseconds(150)));
                animBeamFade.BeginTime = TimeSpan.FromMilliseconds(130);
                Storyboard.SetTarget(animBeamFade, crtBeam);
                Storyboard.SetTargetProperty(animBeamFade, new PropertyPath(UIElement.OpacityProperty));
                sb.Children.Add(animBeamFade);
            }
            if (crtSpark != null)
            {
                DoubleAnimation animSparkFade = new DoubleAnimation(1.0, 0.0, new Duration(TimeSpan.FromMilliseconds(110)));
                Storyboard.SetTarget(animSparkFade, crtSpark);
                Storyboard.SetTargetProperty(animSparkFade, new PropertyPath(UIElement.OpacityProperty));
                sb.Children.Add(animSparkFade);
            }

            sb.Completed += delegate {
                rootScale.ScaleX = 1.0;
                rootScale.ScaleY = 1.0;
                rootGrid.Opacity = 1.0;
                if (crtBeam != null) { crtBeam.Visibility = Visibility.Collapsed; crtBeam.Opacity = 1.0; }
                if (crtSpark != null) { crtSpark.Visibility = Visibility.Collapsed; crtSpark.Opacity = 0.0; }
            };

            sb.Begin();
        }

        public void ShutdownApplication()
        {
            if (this.IsVisible && EnableCloseAnimation)
            {
                this.AnimateClose(delegate {
                    if (widget != null && widget.IsVisible)
                    {
                        widget.AnimateHide(delegate {
                            CleanupAndShutdown();
                        });
                    }
                    else
                    {
                        CleanupAndShutdown();
                    }
                });
            }
            else if (widget != null && widget.IsVisible && EnableCloseAnimation)
            {
                widget.AnimateHide(delegate {
                    CleanupAndShutdown();
                });
            }
            else
            {
                CleanupAndShutdown();
            }
        }

        private void CleanupAndShutdown()
        {
            try {
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }
            } catch { }
            System.Windows.Application.Current.Shutdown();
        }

        private void ApplyMasterActive(bool isInitial = false)
        {
            if (isMasterActive)
            {
                if (isInitial)
                {
                    widget.Show();
                    widget.AttachToDesktop();
                }
                else
                {
                    widget.AnimateShow();
                }
                if (btnHeaderStatus != null)
                {
                    btnHeaderStatus.Content = "Widget: ON";
                    btnHeaderStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
                    btnHeaderStatus.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(12, 32, 40));
                    btnHeaderStatus.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 229, 255));
                }
            }
            else
            {
                if (isInitial)
                {
                    widget.Hide();
                }
                else
                {
                    widget.AnimateHide();
                }
                if (btnHeaderStatus != null)
                {
                    btnHeaderStatus.Content = "Widget: OFF";
                    btnHeaderStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 77, 109));
                    btnHeaderStatus.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 10, 18));
                    btnHeaderStatus.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 24, 38));
                }
            }
        }

        private void SelectTheme(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "Getsuga";
            widget.SetGlowTheme(key);
            foreach (var item in themeItems)
            {
                item.SetSelected(item.Key != null && item.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            }
            SaveConfig();
        }

        private StackPanel CreateCyberSectionHeader(string title, string statusText)
        {
            StackPanel headerStack = new StackPanel();
            headerStack.Margin = new Thickness(0, 12, 0, 6);

            DockPanel topRow = new DockPanel();

            StackPanel leftGroup = new StackPanel();
            leftGroup.Orientation = System.Windows.Controls.Orientation.Horizontal;
            leftGroup.VerticalAlignment = VerticalAlignment.Center;

            // Vertical neon indicator bar
            Border bar = new Border();
            bar.Width = 3.5;
            bar.Height = 13;
            bar.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 42, 85));
            bar.CornerRadius = new CornerRadius(1);
            bar.Margin = new Thickness(0, 0, 7, 0);
            leftGroup.Children.Add(bar);

            TextBlock tbTitle = new TextBlock();
            tbTitle.Text = title.ToUpper();
            tbTitle.FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Consolas, sans-serif");
            tbTitle.FontWeight = FontWeights.Bold;
            tbTitle.FontSize = 12.5;
            tbTitle.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 51, 85));
            leftGroup.Children.Add(tbTitle);

            topRow.Children.Add(leftGroup);

            if (!string.IsNullOrEmpty(statusText))
            {
                TextBlock tbStatus = new TextBlock();
                tbStatus.Text = statusText;
                tbStatus.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
                tbStatus.FontSize = 10;
                tbStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(142, 117, 130));
                tbStatus.VerticalAlignment = VerticalAlignment.Center;
                tbStatus.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                DockPanel.SetDock(tbStatus, Dock.Right);
                topRow.Children.Add(tbStatus);
            }

            headerStack.Children.Add(topRow);

            Border divider = new Border();
            divider.Height = 1;
            divider.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 240, 255));
            divider.Opacity = 0.22;
            divider.Margin = new Thickness(0, 5, 0, 4);
            headerStack.Children.Add(divider);

            return headerStack;
        }

        private TextBlock CreateCyberFieldLabel(string text)
        {
            TextBlock tb = new TextBlock();
            tb.Text = "> " + text;
            tb.FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace");
            tb.FontSize = 10.5;
            tb.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
            tb.Margin = new Thickness(4, 5, 0, 3);
            return tb;
        }

        private TextBlock CreateAmberCallout(string text)
        {
            TextBlock tb = new TextBlock();
            tb.Text = text;
            tb.FontFamily = new System.Windows.Media.FontFamily("Consolas, Segoe UI, sans-serif");
            tb.FontSize = 10;
            tb.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11));
            tb.Margin = new Thickness(4, 3, 0, 6);
            tb.TextWrapping = TextWrapping.Wrap;
            return tb;
        }

        private System.Windows.Controls.Button CreateCyberButton(string text, double height = 28)
        {
            System.Windows.Controls.Button btn = new System.Windows.Controls.Button();
            btn.Content = text;
            btn.Height = height;
            btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 77, 109));
            btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 10, 18));
            btn.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 42, 85));
            btn.BorderThickness = new Thickness(1);
            btn.FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New, monospace");
            btn.FontWeight = FontWeights.Bold;
            btn.FontSize = 10.5;
            btn.Cursor = System.Windows.Input.Cursors.Hand;
            btn.Margin = new Thickness(2);

            SolidColorBrush defaultBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 10, 18));
            SolidColorBrush defaultBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 42, 85));
            SolidColorBrush defaultFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 77, 109));

            SolidColorBrush hoverBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 16, 34));
            SolidColorBrush hoverBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 85, 119));
            SolidColorBrush hoverFg = System.Windows.Media.Brushes.White;

            btn.MouseEnter += delegate {
                btn.Background = hoverBg;
                btn.BorderBrush = hoverBorder;
                btn.Foreground = hoverFg;
            };
            btn.MouseLeave += delegate {
                btn.Background = defaultBg;
                btn.BorderBrush = defaultBorder;
                btn.Foreground = defaultFg;
            };

            return btn;
        }

        private System.Windows.Controls.Button CreateSmallPresetButton(string text, double scale)
        {
            System.Windows.Controls.Button btn = CreateCyberButton(text, 26);
            btn.Click += delegate {
                sliderMasterScale.Value = scale;
            };
            return btn;
        }

        private System.Windows.Controls.Button CreateTitleButton(string type, bool isClose)
        {
            System.Windows.Controls.Button btn = new System.Windows.Controls.Button();
            btn.Width = 40;
            btn.Height = 36;
            btn.Background = System.Windows.Media.Brushes.Transparent;
            btn.BorderThickness = new Thickness(0);
            btn.Cursor = System.Windows.Input.Cursors.Hand;

            System.Windows.Shapes.Path iconPath = new System.Windows.Shapes.Path();
            iconPath.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 77, 109));
            iconPath.StrokeThickness = 1.8;
            iconPath.StrokeStartLineCap = PenLineCap.Round;
            iconPath.StrokeEndLineCap = PenLineCap.Round;
            iconPath.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            iconPath.VerticalAlignment = VerticalAlignment.Center;

            if (type == "min")
            {
                iconPath.Data = Geometry.Parse("M 0 0 L 12 0");
            }
            else
            {
                iconPath.Data = Geometry.Parse("M 0 0 L 10 10 M 10 0 L 0 10");
            }

            btn.Content = iconPath;

            SolidColorBrush defaultBg = System.Windows.Media.Brushes.Transparent;
            SolidColorBrush hoverBg = isClose ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 16, 28));
            SolidColorBrush defaultStroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 77, 109));
            SolidColorBrush hoverStroke = System.Windows.Media.Brushes.White;

            btn.MouseEnter += delegate {
                btn.Background = hoverBg;
                iconPath.Stroke = hoverStroke;
            };
            btn.MouseLeave += delegate {
                btn.Background = defaultBg;
                iconPath.Stroke = defaultStroke;
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

        private void SetupTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Gothic Clock Mainframe";

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
            {
                try { this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(iconPath)); } catch { }
                try { trayIcon.Icon = new Icon(iconPath); } catch { trayIcon.Icon = SystemIcons.Application; }
            }
            else
            {
                trayIcon.Icon = SystemIcons.Application;
            }

            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate {
                this.AnimateOpen();
            };

            System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu();
            menu.MenuItems.Add(new System.Windows.Forms.MenuItem("Open Mainframe", delegate {
                this.AnimateOpen();
            }));
            menu.MenuItems.Add(new System.Windows.Forms.MenuItem("Toggle Widget (ON/OFF)", delegate {
                isMasterActive = !isMasterActive;
                ApplyMasterActive(false);
                SaveConfig();
            }));
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(new System.Windows.Forms.MenuItem("Exit Completely", delegate {
                this.Dispatcher.BeginInvoke(new Action(delegate {
                    ShutdownApplication();
                }));
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
                            else if (k == "AlwaysOnTop") widget.IsAlwaysOnTop = bool.Parse(v);
                            else if (k == "GlowTheme") widget.GlowTheme = v;
                            else if (k == "GlowRadius") widget.GlowRadius = double.Parse(v);
                            else if (k == "MotionStyle") widget.MotionStyle = v;
                            else if (k == "ColonStyle") widget.ColonStyle = v;
                            else if (k == "ShowDivider") widget.ShowDivider = bool.Parse(v);
                            else if (k == "ShowGlass") widget.ShowGlassBackplate = bool.Parse(v);
                            else if (k == "EnableCloseAnim") EnableCloseAnimation = bool.Parse(v);
                        }
                    }
                }
                catch { }
            }

            sliderMasterScale.Value = widget.MasterScale;
            txtMasterScaleVal.Text = Math.Round(widget.MasterScale * 100) + "%";

            sliderSpacing.Value = widget.LineSpacing;
            txtSpacingVal.Text = widget.LineSpacing + "px";

            toggleAnim.IsChecked = widget.EnableAnimation;
            sliderAnimSpeed.Value = widget.AnimationDurationMs;
            txtAnimSpeedVal.Text = widget.AnimationDurationMs + "ms";

            toggleShowTime.IsChecked = widget.ShowTime;
            sliderTimeSize.Value = widget.TimeFontSize;
            txtTimeSizeVal.Text = widget.TimeFontSize + "px";
            toggle12Hour.IsChecked = widget.Is12Hour;
            toggleSeconds.IsChecked = widget.ShowSeconds;

            toggleShowDay.IsChecked = widget.ShowDay;
            sliderDaySize.Value = widget.DayFontSize;
            txtDaySizeVal.Text = widget.DayFontSize + "px";
            toggleTodayPrefix.IsChecked = widget.ShowTodayPrefix;

            toggleShowDate.IsChecked = widget.ShowDate;
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

            toggleDivider.IsChecked = widget.ShowDivider;
            toggleGlass.IsChecked = widget.ShowGlassBackplate;
            if (toggleCloseAnimation != null) toggleCloseAnimation.IsChecked = EnableCloseAnimation;

            toggleStayDesktop.IsChecked = widget.StayOnDesktop;
            if (toggleAlwaysOnTop != null) toggleAlwaysOnTop.IsChecked = widget.IsAlwaysOnTop;

            widget.RefreshAppearance();
            SelectTheme(widget.GlowTheme);
            SnapPosition(defaultLeft, defaultTop);

            ApplyMasterActive(true);
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
                    sw.WriteLine("AlwaysOnTop=" + widget.IsAlwaysOnTop);
                    sw.WriteLine("GlowTheme=" + widget.GlowTheme);
                    sw.WriteLine("GlowRadius=" + widget.GlowRadius);
                    sw.WriteLine("MotionStyle=" + widget.MotionStyle);
                    sw.WriteLine("ColonStyle=" + widget.ColonStyle);
                    sw.WriteLine("ShowDivider=" + widget.ShowDivider);
                    sw.WriteLine("ShowGlass=" + widget.ShowGlassBackplate);
                    sw.WriteLine("EnableCloseAnim=" + EnableCloseAnimation);
                }
            }
            catch { }
        }

        private void PerformUninstall()
        {
            MessageBoxResult res = System.Windows.MessageBox.Show(
                "Are you sure you want to completely uninstall Gothic Clock?\n\nThis will remove all application files, shortcuts, and startup entries from your system.",
                "Confirm Uninstall - Gothic Clock",
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
                            controlPanel.AnimateOpen();
                        }));
                    }
                }));
                listenThread.IsBackground = true;
                listenThread.Start();

                if (!isSilent)
                {
                    controlPanel.AnimateOpen();
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
