using System;
using System.IO;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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
    // 1. DESKTOP CLOCK WIDGET WINDOW (PINNED TO DESKTOP LAYER)
    // =========================================================================
    public class WidgetWindow : Window
    {
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;
        private const int SC_MINIMIZE = 0xF020;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_HIDEWINDOW = 0x0080;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private const int GWL_EXSTYLE = -20;
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
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private TextBlock txtTime;
        private TextBlock txtDay;
        private TextBlock txtDate;
        private Border mainBorder;
        private ScaleTransform scaleTransform;
        private DispatcherTimer timer;

        public bool IsMoveMode = false;
        public bool StayOnDesktop = true;
        public double CurrentScale = 0.85;

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
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += delegate { UpdateClock(); };
            timer.Start();

            UpdateClock();

            this.Loaded += delegate {
                SendToBottom();
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

            StackPanel stack = new StackPanel();
            stack.Orientation = System.Windows.Controls.Orientation.Vertical;
            stack.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            stack.Margin = new Thickness(4);

            txtTime = new TextBlock();
            txtTime.FontFamily = gothicFont;
            txtTime.FontSize = 26;
            txtTime.Foreground = textBrush;
            txtTime.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            txtTime.TextAlignment = TextAlignment.Center;
            txtTime.Effect = shadow;

            txtDay = new TextBlock();
            txtDay.FontFamily = gothicFont;
            txtDay.FontSize = 13;
            txtDay.Foreground = textBrush;
            txtDay.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            txtDay.TextAlignment = TextAlignment.Center;
            txtDay.Margin = new Thickness(0, 1, 0, 1);
            txtDay.Effect = shadow;

            txtDate = new TextBlock();
            txtDate.FontFamily = gothicFont;
            txtDate.FontSize = 12;
            txtDate.Foreground = textBrush;
            txtDate.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            txtDate.TextAlignment = TextAlignment.Center;
            txtDate.Effect = shadow;

            stack.Children.Add(txtTime);
            stack.Children.Add(txtDay);
            stack.Children.Add(txtDate);

            mainBorder = new Border();
            mainBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(1, 255, 255, 255));
            mainBorder.Padding = new Thickness(8, 4, 8, 4);
            mainBorder.CornerRadius = new CornerRadius(4);
            mainBorder.BorderThickness = new Thickness(1);
            mainBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;

            scaleTransform = new ScaleTransform(CurrentScale, CurrentScale);
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
                SendToBottom();
            }
        }

        public void ApplyScale(double scale)
        {
            CurrentScale = Math.Round(scale, 2);
            scaleTransform.ScaleX = CurrentScale;
            scaleTransform.ScaleY = CurrentScale;
        }

        public void SendToBottom()
        {
            if (StayOnDesktop && !IsMoveMode)
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            }
        }

        private void UpdateClock()
        {
            DateTime now = DateTime.Now;
            txtTime.Text = now.ToString("hh:mm:ss tt");
            txtDay.Text = "Today is " + now.ToString("dddd");
            txtDate.Text = now.ToString("MM/dd/yyyy");
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

            SendToBottom();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE && !IsMoveMode)
            {
                handled = true;
                return new IntPtr(MA_NOACTIVATE);
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
    // 2. MODERN DARK CONTROL PANEL WINDOW (CUSTOM TITLEBAR, SLIDER & SCROLLBAR)
    // =========================================================================
    public class ControlPanelWindow : Window
    {
        private WidgetWindow widget;
        private NotifyIcon trayIcon;
        private string configPath;

        private System.Windows.Controls.Button btnToggleWidget;
        private System.Windows.Controls.Button btnMoveMode;
        private System.Windows.Controls.Slider sliderScale;
        private TextBlock txtScaleLabel;
        private System.Windows.Controls.CheckBox chkAutoStart;
        private System.Windows.Controls.CheckBox chkStayDesktop;

        private bool isWidgetActive = true;

        public ControlPanelWindow(WidgetWindow widgetWin)
        {
            this.widget = widgetWin;

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            configPath = Path.Combine(appDir, "settings.ini");

            // Modern borderless window with custom dark title bar and drop shadow
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = System.Windows.Media.Brushes.Transparent;
            this.Title = "Gothic Clock - Control Panel";
            this.Width = 460;
            this.Height = 650;
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

                <!-- Sleek Dark Transparent ScrollBar (No ugly white background) -->
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

                <!-- Modern Cyan/Blue Dark Slider (No grey box) -->
                <Style TargetType=""Slider"">
                    <Setter Property=""Height"" Value=""26"" />
                    <Setter Property=""Template"">
                        <Setter.Value>
                            <ControlTemplate TargetType=""Slider"">
                                <Grid VerticalAlignment=""Center"">
                                    <Border Height=""6"" Background=""#262936"" CornerRadius=""3"" />
                                    <Track x:Name=""PART_Track"">
                                        <Track.Thumb>
                                            <Thumb Cursor=""Hand"">
                                                <Thumb.Template>
                                                    <ControlTemplate TargetType=""Thumb"">
                                                        <Grid Width=""18"" Height=""18"">
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
            // Outer window border with rounded corners and subtle drop shadow
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
            winShadow.Opacity = 0.6;
            windowBorder.Effect = winShadow;

            DockPanel mainDock = new DockPanel();

            // 1. Custom Sleek Dark Title Bar
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

            // Left: Title
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

            // Right: Window controls
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

            // 2. Scrollable Body Content
            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.Background = System.Windows.Media.Brushes.Transparent;
            scroll.BorderThickness = new Thickness(0);

            StackPanel contentStack = new StackPanel();
            contentStack.Margin = new Thickness(18, 14, 18, 18);

            // Header Banner
            TextBlock txtTitle = new TextBlock();
            txtTitle.Text = "GOTHIC CLOCK";
            txtTitle.FontFamily = new System.Windows.Media.FontFamily("Old English Text MT, Georgia, serif");
            txtTitle.FontSize = 26;
            txtTitle.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 240, 248));
            txtTitle.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

            TextBlock txtSub = new TextBlock();
            txtSub.Text = "Desktop Widget & System Manager";
            txtSub.FontSize = 12;
            txtSub.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(130, 140, 155));
            txtSub.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            txtSub.Margin = new Thickness(0, 2, 0, 16);

            contentStack.Children.Add(txtTitle);
            contentStack.Children.Add(txtSub);

            // Card 1: Power Status
            Border cardPower = CreateCard();
            StackPanel spPower = new StackPanel();

            TextBlock lblPower = CreateCardHeader("Widget Status");
            btnToggleWidget = CreateStyledButton("Desktop Widget: ACTIVE (ON)", System.Windows.Media.Color.FromRgb(30, 120, 60));
            btnToggleWidget.Click += delegate {
                isWidgetActive = !isWidgetActive;
                if (isWidgetActive)
                {
                    widget.Show();
                    widget.SendToBottom();
                    btnToggleWidget.Content = "Desktop Widget: ACTIVE (ON)";
                    btnToggleWidget.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 120, 60));
                }
                else
                {
                    widget.Hide();
                    btnToggleWidget.Content = "Desktop Widget: STOPPED (OFF)";
                    btnToggleWidget.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 75, 85));
                }
                SaveConfig();
            };

            spPower.Children.Add(lblPower);
            spPower.Children.Add(btnToggleWidget);
            cardPower.Child = spPower;
            contentStack.Children.Add(cardPower);

            // Card 2: Size Controls (Modern Slider & Buttons)
            Border cardSize = CreateCard();
            StackPanel spSize = new StackPanel();

            TextBlock lblSize = CreateCardHeader("Size Controls");

            UniformGrid gridSizes = new UniformGrid();
            gridSizes.Columns = 4;
            gridSizes.Margin = new Thickness(0, 0, 0, 10);

            System.Windows.Controls.Button btnTiny = CreateSmallButton("Tiny (50%)");
            btnTiny.Click += delegate { SetScale(0.50); };

            System.Windows.Controls.Button btnSmall = CreateSmallButton("Small (70%)");
            btnSmall.Click += delegate { SetScale(0.70); };

            System.Windows.Controls.Button btnNorm = CreateSmallButton("Normal (85%)");
            btnNorm.Click += delegate { SetScale(0.85); };

            System.Windows.Controls.Button btnLarge = CreateSmallButton("Large (120%)");
            btnLarge.Click += delegate { SetScale(1.20); };

            gridSizes.Children.Add(btnTiny);
            gridSizes.Children.Add(btnSmall);
            gridSizes.Children.Add(btnNorm);
            gridSizes.Children.Add(btnLarge);

            DockPanel dpSlider = new DockPanel();
            txtScaleLabel = new TextBlock();
            txtScaleLabel.Text = "85%";
            txtScaleLabel.Width = 48;
            txtScaleLabel.TextAlignment = TextAlignment.Right;
            txtScaleLabel.VerticalAlignment = VerticalAlignment.Center;
            txtScaleLabel.FontWeight = FontWeights.SemiBold;
            txtScaleLabel.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            DockPanel.SetDock(txtScaleLabel, Dock.Right);

            sliderScale = new System.Windows.Controls.Slider();
            sliderScale.Minimum = 0.35;
            sliderScale.Maximum = 1.80;
            sliderScale.Value = 0.85;
            sliderScale.TickFrequency = 0.05;
            sliderScale.IsSnapToTickEnabled = true;
            sliderScale.Margin = new Thickness(0, 0, 10, 0);
            sliderScale.ValueChanged += delegate {
                SetScale(sliderScale.Value);
            };

            dpSlider.Children.Add(txtScaleLabel);
            dpSlider.Children.Add(sliderScale);

            spSize.Children.Add(lblSize);
            spSize.Children.Add(gridSizes);
            spSize.Children.Add(dpSlider);
            cardSize.Child = spSize;
            contentStack.Children.Add(cardSize);

            // Card 3: Desktop Placement
            Border cardPos = CreateCard();
            StackPanel spPos = new StackPanel();

            TextBlock lblPos = CreateCardHeader("Desktop Position");

            UniformGrid gridPos = new UniformGrid();
            gridPos.Columns = 3;
            gridPos.Margin = new Thickness(0, 0, 0, 8);

            System.Windows.Controls.Button btnTR = CreateSmallButton("Top-Right");
            btnTR.Click += delegate { SnapPosition(SystemParameters.WorkArea.Right - (200 * widget.CurrentScale) - 20, 30); };

            System.Windows.Controls.Button btnTL = CreateSmallButton("Top-Left");
            btnTL.Click += delegate { SnapPosition(30, 30); };

            System.Windows.Controls.Button btnBR = CreateSmallButton("Bottom-Right");
            btnBR.Click += delegate { SnapPosition(SystemParameters.WorkArea.Right - (200 * widget.CurrentScale) - 20, SystemParameters.WorkArea.Bottom - (80 * widget.CurrentScale) - 30); };

            System.Windows.Controls.Button btnBL = CreateSmallButton("Bottom-Left");
            btnBL.Click += delegate { SnapPosition(30, SystemParameters.WorkArea.Bottom - (80 * widget.CurrentScale) - 30); };

            System.Windows.Controls.Button btnCenter = CreateSmallButton("Center");
            btnCenter.Click += delegate { SnapPosition((SystemParameters.WorkArea.Width - (200 * widget.CurrentScale)) / 2, (SystemParameters.WorkArea.Height - (80 * widget.CurrentScale)) / 2); };

            System.Windows.Controls.Button btnReset = CreateSmallButton("Reset");
            btnReset.Click += delegate { SnapPosition(SystemParameters.WorkArea.Right - 220, 40); };

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
            contentStack.Children.Add(cardPos);

            // Card 4: System Integration
            Border cardSys = CreateCard();
            StackPanel spSys = new StackPanel();

            TextBlock lblSys = CreateCardHeader("System Integration");

            chkAutoStart = new System.Windows.Controls.CheckBox();
            chkAutoStart.Content = "Start silently with Windows on boot";
            chkAutoStart.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 215, 225));
            chkAutoStart.Margin = new Thickness(0, 0, 0, 8);
            chkAutoStart.IsChecked = IsStartupEnabled();
            chkAutoStart.Click += delegate {
                SetStartupEnabled(chkAutoStart.IsChecked == true);
                SaveConfig();
            };

            chkStayDesktop = new System.Windows.Controls.CheckBox();
            chkStayDesktop.Content = "Stay at the Main Desktop layer (Under apps, immune to Win+D)";
            chkStayDesktop.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 215, 225));
            chkStayDesktop.IsChecked = true;
            chkStayDesktop.Click += delegate {
                widget.StayOnDesktop = (chkStayDesktop.IsChecked == true);
                widget.SendToBottom();
                SaveConfig();
            };

            spSys.Children.Add(lblSys);
            spSys.Children.Add(chkAutoStart);
            spSys.Children.Add(chkStayDesktop);
            cardSys.Child = spSys;
            contentStack.Children.Add(cardSys);

            // Card 5: Maintenance & Uninstall
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
            contentStack.Children.Add(cardUninst);

            scroll.Content = contentStack;
            mainDock.Children.Add(scroll);

            windowBorder.Child = mainDock;
            this.Content = windowBorder;
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
            btn.FontWeight = FontWeights.Normal;
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

        private void SetScale(double scale)
        {
            scale = Math.Round(scale, 2);
            sliderScale.Value = scale;
            txtScaleLabel.Text = Math.Round(scale * 100) + "%";
            widget.ApplyScale(scale);
            SaveConfig();
        }

        private void SnapPosition(double left, double top)
        {
            widget.Left = Math.Max(0, left);
            widget.Top = Math.Max(0, top);
            widget.SendToBottom();
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
            double defaultLeft = SystemParameters.WorkArea.Right - 220;
            double defaultTop = 40;
            double defaultScale = 0.85;

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
                            else if (k == "Scale") defaultScale = double.Parse(v);
                            else if (k == "Active") isWidgetActive = bool.Parse(v);
                            else if (k == "StayDesktop") widget.StayOnDesktop = bool.Parse(v);
                        }
                    }
                }
                catch { }
            }

            SetScale(defaultScale);
            SnapPosition(defaultLeft, defaultTop);

            if (!isWidgetActive)
            {
                widget.Hide();
                btnToggleWidget.Content = "Desktop Widget: STOPPED (OFF)";
                btnToggleWidget.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 75, 85));
            }
            else
            {
                widget.Show();
                widget.SendToBottom();
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
                    sw.WriteLine("Scale=" + widget.CurrentScale);
                    sw.WriteLine("Active=" + isWidgetActive);
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
    // 3. MAIN APPLICATION ENTRY POINT (SINGLE INSTANCE MUTEX)
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
