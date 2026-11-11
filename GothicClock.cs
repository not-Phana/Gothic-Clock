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
    // 1. DESKTOP CLOCK WIDGET WINDOW (ROLLING TICKER ANIMATION + WIN+D IMMUNITY)
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

        // UI Elements
        private StackPanel timePanel;
        private TextBlock txtHourMin;
        private Grid secBox;
        private TextBlock txtSecCurrent;
        private TextBlock txtSecIncoming;
        private TranslateTransform transSecCurrent;
        private TranslateTransform transSecIncoming;
        private TextBlock txtAmPm;

        private TextBlock txtDay;
        private TextBlock txtDate;
        private StackPanel stack;
        private Border mainBorder;
        private ScaleTransform scaleTransform;
        private DispatcherTimer timer;

        private string lastSecondStr = "";

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

        public bool EnableTimeAnimation = true;
        public int AnimationDurationMs = 320;

        public double MasterScale = 0.85;
        public bool IsMoveMode = false;
        public bool StayOnDesktop = true;

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
            // TIME PANEL WITH SLIDE-DOWN TICKER ANIMATION
            // =================================================================
            timePanel = new StackPanel();
            timePanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
            timePanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

            // Hours & Minutes (e.g., "01:34:")
            txtHourMin = new TextBlock();
            txtHourMin.FontFamily = gothicFont;
            txtHourMin.FontSize = TimeFontSize;
            txtHourMin.Foreground = textBrush;
            txtHourMin.VerticalAlignment = VerticalAlignment.Center;
            txtHourMin.Effect = shadow;

            // Seconds Container (Clipped for smooth slide-down roll)
            secBox = new Grid();
            secBox.ClipToBounds = true;
            secBox.VerticalAlignment = VerticalAlignment.Center;
            secBox.Height = TimeFontSize * 1.35;

            transSecCurrent = new TranslateTransform();
            txtSecCurrent = new TextBlock();
            txtSecCurrent.FontFamily = gothicFont;
            txtSecCurrent.FontSize = TimeFontSize;
            txtSecCurrent.Foreground = textBrush;
            txtSecCurrent.TextAlignment = TextAlignment.Center;
            txtSecCurrent.VerticalAlignment = VerticalAlignment.Center;
            txtSecCurrent.RenderTransform = transSecCurrent;
            txtSecCurrent.Effect = shadow;

            transSecIncoming = new TranslateTransform();
            txtSecIncoming = new TextBlock();
            txtSecIncoming.FontFamily = gothicFont;
            txtSecIncoming.FontSize = TimeFontSize;
            txtSecIncoming.Foreground = textBrush;
            txtSecIncoming.TextAlignment = TextAlignment.Center;
            txtSecIncoming.VerticalAlignment = VerticalAlignment.Center;
            txtSecIncoming.RenderTransform = transSecIncoming;
            txtSecIncoming.Opacity = 0;
            txtSecIncoming.Effect = shadow;

            secBox.Children.Add(txtSecCurrent);
            secBox.Children.Add(txtSecIncoming);

            // AM / PM (e.g., " AM")
            txtAmPm = new TextBlock();
            txtAmPm.FontFamily = gothicFont;
            txtAmPm.FontSize = TimeFontSize;
            txtAmPm.Foreground = textBrush;
            txtAmPm.VerticalAlignment = VerticalAlignment.Center;
            txtAmPm.Effect = shadow;

            timePanel.Children.Add(txtHourMin);
            timePanel.Children.Add(secBox);
            timePanel.Children.Add(txtAmPm);

            // Element 2: Day
            txtDay = new TextBlock();
            txtDay.FontFamily = gothicFont;
            txtDay.FontSize = DayFontSize;
            txtDay.Foreground = textBrush;
            txtDay.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            txtDay.TextAlignment = TextAlignment.Center;
            txtDay.Margin = new Thickness(0, LineSpacing, 0, LineSpacing);
            txtDay.Effect = shadow;

            // Element 3: Date
            txtDate = new TextBlock();
            txtDate.FontFamily = gothicFont;
            txtDate.FontSize = DateFontSize;
            txtDate.Foreground = textBrush;
            txtDate.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            txtDate.TextAlignment = TextAlignment.Center;
            txtDate.Effect = shadow;

            stack.Children.Add(timePanel);
            stack.Children.Add(txtDay);
            stack.Children.Add(txtDate);

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

        public void RefreshAppearance()
        {
            timePanel.Visibility = ShowTime ? Visibility.Visible : Visibility.Collapsed;
            txtDay.Visibility = ShowDay ? Visibility.Visible : Visibility.Collapsed;
            txtDate.Visibility = ShowDate ? Visibility.Visible : Visibility.Collapsed;

            txtHourMin.FontSize = TimeFontSize;
            txtSecCurrent.FontSize = TimeFontSize;
            txtSecIncoming.FontSize = TimeFontSize;
            secBox.Height = TimeFontSize * 1.35;
            txtAmPm.FontSize = TimeFontSize;

            txtDay.FontSize = DayFontSize;
            txtDate.FontSize = DateFontSize;

            txtDay.Margin = new Thickness(0, LineSpacing, 0, LineSpacing);

            scaleTransform.ScaleX = MasterScale;
            scaleTransform.ScaleY = MasterScale;

            secBox.Visibility = ShowSeconds ? Visibility.Visible : Visibility.Collapsed;

            UpdateClock();
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
                mainBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
                mainBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(1, 255, 255, 255));
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

            // 1. Time Update
            if (ShowTime)
            {
                string hourFormat = Is12Hour ? "hh:mm" : "HH:mm";
                string hourMin = now.ToString(hourFormat);
                string newSec = now.ToString("ss");
                string amPm = Is12Hour ? (" " + now.ToString("tt")) : "";

                if (ShowSeconds)
                {
                    txtHourMin.Text = hourMin + ":";
                    txtAmPm.Text = amPm;

                    if (newSec != lastSecondStr)
                    {
                        if (!string.IsNullOrEmpty(lastSecondStr) && EnableTimeAnimation)
                        {
                            AnimateSeconds(lastSecondStr, newSec);
                        }
                        else
                        {
                            txtSecCurrent.Text = newSec;
                            transSecCurrent.Y = 0;
                            txtSecCurrent.Opacity = 1;
                            txtSecIncoming.Opacity = 0;
                        }
                        lastSecondStr = newSec;
                    }
                }
                else
                {
                    txtHourMin.Text = hourMin;
                    txtAmPm.Text = amPm;
                }
            }

            // 2. Day Update
            if (ShowDay)
            {
                string dayName = now.ToString("dddd");
                txtDay.Text = ShowTodayPrefix ? ("Today is " + dayName) : dayName;
            }

            // 3. Date Update
            if (ShowDate)
            {
                txtDate.Text = now.ToString(DateFormat);
            }
        }

        // =====================================================================
        // SLIDE-DOWN TICKER ANIMATION (Current goes down, Incoming drops from top)
        // =====================================================================
        private void AnimateSeconds(string oldSec, string newSec)
        {
            txtSecCurrent.Text = oldSec;
            txtSecIncoming.Text = newSec;

            double travelDistance = TimeFontSize * 0.95;

            // Reset start positions
            transSecCurrent.Y = 0;
            transSecIncoming.Y = -travelDistance;

            txtSecCurrent.Opacity = 1.0;
            txtSecIncoming.Opacity = 0.0;

            Duration dur = new Duration(TimeSpan.FromMilliseconds(AnimationDurationMs));
            CubicEase ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            // 1. Old second slides down and fades out
            DoubleAnimation animOutY = new DoubleAnimation(0, travelDistance, dur) { EasingFunction = ease };
            DoubleAnimation animOutOp = new DoubleAnimation(1.0, 0.0, dur) { EasingFunction = ease };

            // 2. New second slides down from top into place and fades in
            DoubleAnimation animInY = new DoubleAnimation(-travelDistance, 0, dur) { EasingFunction = ease };
            DoubleAnimation animInOp = new DoubleAnimation(0.0, 1.0, dur) { EasingFunction = ease };

            transSecCurrent.BeginAnimation(TranslateTransform.YProperty, animOutY);
            txtSecCurrent.BeginAnimation(UIElement.OpacityProperty, animOutOp);

            transSecIncoming.BeginAnimation(TranslateTransform.YProperty, animInY);
            txtSecIncoming.BeginAnimation(UIElement.OpacityProperty, animInOp);
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

            // Block Win+D ShowDesktop hide command
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
    // 2. MODERN CONTROL PANEL WITH ANIMATION SETTINGS
    // =========================================================================
    public class ControlPanelWindow : Window
    {
        private WidgetWindow widget;
        private NotifyIcon trayIcon;
        private string configPath;

        private System.Windows.Controls.Button btnToggleAll;
        private System.Windows.Controls.Button btnMoveMode;

        // Master scale & spacing
        private System.Windows.Controls.Slider sliderMasterScale;
        private TextBlock txtMasterScaleVal;
        private System.Windows.Controls.Slider sliderSpacing;
        private TextBlock txtSpacingVal;

        // Element 1: Time & Animation
        private System.Windows.Controls.CheckBox chkShowTime;
        private System.Windows.Controls.Slider sliderTimeSize;
        private TextBlock txtTimeSizeVal;
        private System.Windows.Controls.CheckBox chk12Hour;
        private System.Windows.Controls.CheckBox chkShowSeconds;
        private System.Windows.Controls.CheckBox chkEnableAnim;
        private System.Windows.Controls.Slider sliderAnimSpeed;
        private TextBlock txtAnimSpeedVal;

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

                <!-- Dark Sleek Transparent ScrollBar -->
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

                <!-- Modern Cyan/Blue Dark Slider -->
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
            txtSub.Text = "Rolling Ticker Animation & Control Center";
            txtSub.FontSize = 11;
            txtSub.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(130, 140, 155));
            txtSub.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            txtSub.Margin = new Thickness(0, 2, 0, 14);

            content.Children.Add(txtTitle);
            content.Children.Add(txtSub);

            // =================================================================
            // CARD 1: MASTER CONTROLS & GLOBAL SIZING
            // =================================================================
            Border cardMaster = CreateCard();
            StackPanel spMaster = new StackPanel();

            TextBlock lblMaster = CreateCardHeader("Master Controls (All Three Elements)");

            btnToggleAll = CreateStyledButton("All Elements: ACTIVE (ON)", System.Windows.Media.Color.FromRgb(30, 120, 60));
            btnToggleAll.Margin = new Thickness(0, 0, 0, 10);
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

            UniformGrid gridPresets = new UniformGrid();
            gridPresets.Columns = 5;
            gridPresets.Margin = new Thickness(0, 0, 0, 10);

            gridPresets.Children.Add(CreateSmallPresetButton("Tiny (40%)", 0.40));
            gridPresets.Children.Add(CreateSmallPresetButton("Small (65%)", 0.65));
            gridPresets.Children.Add(CreateSmallPresetButton("Norm (85%)", 0.85));
            gridPresets.Children.Add(CreateSmallPresetButton("Large (120%)", 1.20));
            gridPresets.Children.Add(CreateSmallPresetButton("Huge (180%)", 1.80));

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
            spMaster.Children.Add(lblScale);
            spMaster.Children.Add(dpMasterScale);
            spMaster.Children.Add(gridPresets);
            spMaster.Children.Add(lblSpacing);
            spMaster.Children.Add(dpSpacing);
            cardMaster.Child = spMaster;
            content.Children.Add(cardMaster);

            // =================================================================
            // CARD 2: ELEMENT 1 - TIME & ROLLING ANIMATION
            // =================================================================
            Border cardTime = CreateCard();
            StackPanel spTime = new StackPanel();

            TextBlock lblTimeTitle = CreateCardHeader("Element 1: Time (Clock & Rolling Ticker)");

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

            // Animation Settings
            chkEnableAnim = CreateCheckBox("Slide-Down Ticker Animation (Old second drops down, new enters from top)", true);
            chkEnableAnim.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            chkEnableAnim.FontWeight = FontWeights.Medium;
            chkEnableAnim.Click += delegate {
                widget.EnableTimeAnimation = chkEnableAnim.IsChecked == true;
                SaveConfig();
            };

            TextBlock lblAnimSpeed = CreateFieldLabel("Animation Speed (ms):");
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
            spTime.Children.Add(chkEnableAnim);
            spTime.Children.Add(lblAnimSpeed);
            spTime.Children.Add(dpAnimSpeed);
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
                            else if (k == "EnableAnim") widget.EnableTimeAnimation = bool.Parse(v);
                            else if (k == "AnimDuration") widget.AnimationDurationMs = int.Parse(v);
                            else if (k == "ShowDay") widget.ShowDay = bool.Parse(v);
                            else if (k == "DayFontSize") widget.DayFontSize = double.Parse(v);
                            else if (k == "ShowTodayPrefix") widget.ShowTodayPrefix = bool.Parse(v);
                            else if (k == "ShowDate") widget.ShowDate = bool.Parse(v);
                            else if (k == "DateFontSize") widget.DateFontSize = double.Parse(v);
                            else if (k == "DateFormat") widget.DateFormat = v;
                            else if (k == "Active") isMasterActive = bool.Parse(v);
                            else if (k == "StayDesktop") widget.StayOnDesktop = bool.Parse(v);
                        }
                    }
                }
                catch { }
            }

            // Sync controls
            sliderMasterScale.Value = widget.MasterScale;
            txtMasterScaleVal.Text = Math.Round(widget.MasterScale * 100) + "%";

            sliderSpacing.Value = widget.LineSpacing;
            txtSpacingVal.Text = widget.LineSpacing + "px";

            chkShowTime.IsChecked = widget.ShowTime;
            sliderTimeSize.Value = widget.TimeFontSize;
            txtTimeSizeVal.Text = widget.TimeFontSize + "px";
            chk12Hour.IsChecked = widget.Is12Hour;
            chkShowSeconds.IsChecked = widget.ShowSeconds;

            chkEnableAnim.IsChecked = widget.EnableTimeAnimation;
            sliderAnimSpeed.Value = widget.AnimationDurationMs;
            txtAnimSpeedVal.Text = widget.AnimationDurationMs + "ms";

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
                    sw.WriteLine("EnableAnim=" + widget.EnableTimeAnimation);
                    sw.WriteLine("AnimDuration=" + widget.AnimationDurationMs);
                    sw.WriteLine("ShowDay=" + widget.ShowDay);
                    sw.WriteLine("DayFontSize=" + widget.DayFontSize);
                    sw.WriteLine("ShowTodayPrefix=" + widget.ShowTodayPrefix);
                    sw.WriteLine("ShowDate=" + widget.ShowDate);
                    sw.WriteLine("DateFontSize=" + widget.DateFontSize);
                    sw.WriteLine("DateFormat=" + widget.DateFormat);
                    sw.WriteLine("Active=" + isMasterActive);
                    sw.WriteLine("StayDesktop=" + widget.StayOnDesktop);
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
    }
}
