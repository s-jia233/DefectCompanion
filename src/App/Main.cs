using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace DefectCompanion
{
    public sealed class Settings
    {
        public double Left = Double.NaN, Top = Double.NaN, Scale = 1.15;
        public bool Topmost = true, Quiet;
        public static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DefectCompanion");
        public static Settings Load()
        {
            var value = new Settings();
            try
            {
                foreach (var line in File.ReadAllLines(Path.Combine(Folder, "settings.txt")))
                {
                    var parts = line.Split('='); if (parts.Length != 2) continue;
                    double d;
                    if (Double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out d) && !Double.IsInfinity(d))
                    {
                        if (parts[0] == "left") value.Left = d;
                        if (parts[0] == "top") value.Top = d;
                        if (parts[0] == "scale" && d >= .75 && d <= 1.6) value.Scale = d;
                    }
                    if (parts[0] == "topmost") value.Topmost = parts[1] == "true";
                    if (parts[0] == "quiet") value.Quiet = parts[1] == "true";
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return value;
        }
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllLines(Path.Combine(Folder, "settings.txt"), new [] {
                    "left=" + Left.ToString(CultureInfo.InvariantCulture), "top=" + Top.ToString(CultureInfo.InvariantCulture),
                    "scale=" + Scale.ToString(CultureInfo.InvariantCulture), "topmost=" + Topmost.ToString().ToLowerInvariant(), "quiet=" + Quiet.ToString().ToLowerInvariant()
                });
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
    public static class Program
    {
        public static string ProbePath;
        public static void Probe(string message)
        { if (ProbePath != null) File.AppendAllText(ProbePath, DateTime.Now.ToString("O") + " " + message + Environment.NewLine); }
        public const string WindowTitle = "故障机器人 · 互动桌宠";
        public static readonly uint WakeMessage = RegisterWindowMessage("DefectCompanion.Restore.v1");
        private static Mutex instance;
        [STAThread]
        public static int Main(string[] args)
        {
            bool uiTest = args.Length == 2 && args[0] == "--ui-test";
            bool showcase = args.Length == 2 && args[0] == "--showcase";
            bool preview = args.Length == 2 && args[0] == "--preview" || showcase;
            bool benchmark = args.Length == 2 && args[0] == "--benchmark";
            if (args.Length == 2 && args[0] == "--diagnostics") ProbePath = args[1];
            bool created;
            instance = new Mutex(true, "Local\\DefectCompanion.Desktop.v1" + (preview || uiTest || benchmark ? ".Preview" : ""), out created);
            if (!created)
            {
                if (!preview) { IntPtr existing = FindWindow(null, WindowTitle); if (existing != IntPtr.Zero) PostMessage(existing, WakeMessage, IntPtr.Zero, IntPtr.Zero); }
                instance.Dispose(); return 0;
            }
            try
            {
                var app = new Application();
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e) {
                    try { Directory.CreateDirectory(Settings.Folder); File.WriteAllText(Path.Combine(Settings.Folder, "error.log"), e.Exception.ToString()); } catch { }
                    MessageBox.Show("桌宠遇到了问题，重新打开即可。\n详情保存在本地 DefectCompanion 文件夹。", "故障机器人");
                    app.Shutdown(1); e.Handled = true;
                };
                var window = new PetWindow(preview || uiTest || benchmark ? args[1] : null, uiTest, benchmark,showcase);
                Probe("before Run");
                app.Run(window);
                return 0;
            }
            catch (Exception error)
            {
                try { Directory.CreateDirectory(Settings.Folder); File.WriteAllText(Path.Combine(Settings.Folder, "error.log"), error.ToString()); } catch { }
                if (!preview) MessageBox.Show("未能启动故障机器人。\n" + error.Message, "故障机器人");
                return 1;
            }
            finally { if (created) instance.ReleaseMutex(); instance.Dispose(); }
        }
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string name);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string className, string windowName);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wp, IntPtr lp);
    }
    public sealed class PetWindow : Window
    {
        public readonly PetEngine Engine = new PetEngine();
        public readonly PetView View;
        public readonly Settings Options;
        private readonly FramePacer framePacer = new FramePacer();
        private bool rendering;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private Forms.NotifyIcon tray;
        private readonly string previewFolder;
        private bool held, dragging, charging;
        private Point pressScreen;
        private readonly DragDirection dragDirection = new DragDirection();
        private double startLeft, startTop, pressAt, pendingTap = -1;
        private int pressedOrb = -1;
        private double lastInteraction;
        private bool idleFrost;
        private CardPicker picker;
        private double pickerClosedAt = -10;
        private DateTime lastSave = DateTime.UtcNow;
        private DispatcherTimer frameFallback;
        internal double? PreviewTime;
        public double Seconds { get { return PreviewTime ?? clock.Elapsed.TotalSeconds; } }
        public bool IsDragging { get { return dragging; } }
        public double ChargeProgress { get { return charging ? Math.Min(1, (Seconds - pressAt - .5) / 1.3) : 0; } }

        public PetWindow(string preview, bool uiTest = false, bool benchmark = false,bool showcase = false)
        {
            previewFolder = preview; Options = preview == null ? Settings.Load() : new Settings();
            Title = Program.WindowTitle;
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true; Background = Brushes.Transparent;
            ShowInTaskbar = true; ShowActivated = preview == null; Topmost = Options.Topmost;
            UseLayoutRounding = true;
            View = new PetView(this);
            var host = new Viewbox { Stretch = Stretch.Uniform, Child = View };
            Content = host; SetSize(Options.Scale, false);
            var work = SystemParameters.WorkArea;
            Left = Double.IsNaN(Options.Left) ? work.Right - Width - 24 : Options.Left;
            Top = Double.IsNaN(Options.Top) ? work.Bottom - Height - 12 : Options.Top;
            SourceInitialized += delegate {
                Program.Probe("source initialized; visible=" + IsVisible);
                var handle = new WindowInteropHelper(this).Handle;
                var source = HwndSource.FromHwnd(handle);
                source.AddHook(WindowHook);
                if (Program.ProbePath != null)
                {
                    int cloak = 0; DwmGetWindowAttribute(handle, 14, out cloak, 4);
                    Program.Probe("hwnd=" + handle + "; exStyle=" + GetWindowLong(handle, -20).ToString("X") + "; cloaked=" + cloak + "; workArea=" + SystemParameters.WorkArea);
                }
            };
            View.MouseLeftButtonDown += PointerDown;
            View.MouseLeftButtonUp += PointerUp;
            View.MouseMove += PointerMove;
            View.LostMouseCapture += delegate { if (held) CancelPress(); };
            View.MouseRightButtonDown += delegate(object sender, MouseButtonEventArgs e) { ToggleCards(); e.Handled = true; };
            View.MouseEnter += delegate { View.Hover = true; };
            View.MouseLeave += delegate { View.Hover = false; View.UpdatePowerHover(new Point(-1,-1)); };
            Deactivated += delegate { if (held) CancelPress(); };
            Closed += delegate { StopAnimation(); if (previewFolder == null) Save(); if (tray != null) { tray.Visible = false; tray.Dispose(); } };
            IsVisibleChanged += delegate { if (previewFolder == null || benchmark) { if (IsVisible) StartAnimation(); else StopAnimation(); } };
            Loaded += delegate {
                ClampToDesktop();
                Program.Probe("loaded; visible=" + IsVisible + "; bounds=" + ActualWidth + "," + ActualHeight);
                if (previewFolder == null) { Program.Probe("before tray"); SetupTray(); Program.Probe("after tray"); StartAnimation(); }
                else if (benchmark) { StartAnimation(); PerformanceCheck.Run(this, previewFolder); }
                else if (uiTest) UiChecks.Run(this, previewFolder);
                else if (showcase) ShowcaseExport.Run(this,previewFolder);
                else RenderPreview();
            };
            ContentRendered += delegate { Program.Probe("content rendered"); };
            KeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) CancelPress(); };
        }
        private void Save()
        { Options.Left = Left; Options.Top = Top; Options.Save(); }
        private void ClampToDesktop()
        {
            Rect stage = new Rect(Left, Top, Width, Height);
            Rect target = SystemParameters.WorkArea;
            double bestArea = 0;
            foreach (var screen in Forms.Screen.AllScreens)
            {
                // Screen rectangles are converted to the current WPF coordinate space.
                var source = PresentationSource.FromVisual(this);
                double sx = source == null ? 1 : source.CompositionTarget.TransformFromDevice.M11;
                double sy = source == null ? 1 : source.CompositionTarget.TransformFromDevice.M22;
                var area = screen.WorkingArea;
                var rect = new Rect(area.Left * sx, area.Top * sy, area.Width * sx, area.Height * sy);
                var intersection = Rect.Intersect(stage, rect);
                if (!intersection.IsEmpty && intersection.Width * intersection.Height > bestArea)
                { bestArea = intersection.Width * intersection.Height; target = rect; }
            }
            Left = Math.Max(target.Left, Math.Min(Left, target.Right - Width));
            Top = Math.Max(target.Top, Math.Min(Top, target.Bottom - Height));
        }
        private void SetSize(double scale, bool save)
        { Options.Scale = scale; Width = 420 * scale; Height = 390 * scale; if (save) { ClampToDesktop(); Save(); } }
        private void StartAnimation()
        {
            if (rendering) return;
            rendering = true; framePacer.Reset(); CompositionTarget.Rendering += OnCompositionFrame;
            if(frameFallback==null)
            {
                frameFallback=new DispatcherTimer(DispatcherPriority.Render) {Interval=TimeSpan.FromMilliseconds(8)};
                frameFallback.Tick+=delegate {if(framePacer.Due(Seconds))AdvanceFrame();};
            }
            frameFallback.Start();
        }
        private void StopAnimation()
        {
            if (!rendering) return;
            rendering = false; CompositionTarget.Rendering -= OnCompositionFrame;
            if(frameFallback!=null)frameFallback.Stop();
        }
        private void OnCompositionFrame(object sender, EventArgs e)
        {
            if(framePacer.Due(Seconds))AdvanceFrame();
        }
        private void AdvanceFrame()
        {
            if (Engine.Now == 0) Program.Probe("first tick; visible=" + IsVisible);
            if (!IsVisible) return;
            double now = Seconds;
            Engine.Tick(now);
            if (pendingTap >= 0 && now >= pendingTap) { pendingTap = -1; Use("zap"); }
            if (held && !dragging && pressedOrb < 0 && now - pressAt >= .55)
            {
                if (!charging) { charging = true; Engine.Animate("running", 30); Engine.Say("充电中…按住直到光环充满", 30); }
            }
            if (!held && picker == null && !idleFrost && now - lastInteraction > 45)
            {
                idleFrost = true; if (Engine.Orbs.Count < Engine.Capacity) Engine.Channel(OrbKind.Frost); Engine.Animate("waiting", 1.4);
                Engine.Say("我在这里。慢慢来。", 4);
            }
            if (!held && Engine.Pose == "idle" && idleFrost) Engine.Animate("waiting", .2);
            View.TakeEvents(); View.InvalidateVisual();
            if (previewFolder == null && (DateTime.UtcNow - lastSave).TotalSeconds > 15) { lastSave = DateTime.UtcNow; Save(); }
        }
        private void Touch()
        { lastInteraction = Seconds; idleFrost = false; Engine.Now = Seconds; }
        public void Use(string skill)
        { pendingTap = -1; Touch(); Engine.Use(skill); View.PlayCard(Engine.GeneratedSkill ?? skill); View.TakeEvents(); if (!rendering) View.InvalidateVisual(); }
        private void PointerDown(object sender, MouseButtonEventArgs e)
        {
            Point local = e.GetPosition(View);
            var power = View.PowerAt(local);
            if (power != null) { pendingTap=-1;Touch();Engine.Say(power.Tooltip,4); e.Handled=true;return; }
            if (picker != null) { picker.CancelSelection(); e.Handled = true; return; }
            if (Seconds - pickerClosedAt < .2) { e.Handled = true; return; }
            Touch();
            if (e.ClickCount == 2 && pendingTap >= 0 && Seconds <= pendingTap)
            {
                pendingTap = -1; Use("dualcast"); e.Handled = true; return;
            }
            held = true; dragging = false; charging = false;
            pressedOrb = View.OrbAt(local);
            pressAt = Seconds; pressScreen = PointToScreen(e.GetPosition(this));
            startLeft = Left; startTop = Top;
            View.CaptureMouse(); e.Handled = true;
        }
        private void PointerMove(object sender, MouseEventArgs e)
        {
            View.Pointer = e.GetPosition(View);
            View.UpdatePowerHover(View.Pointer);
            if (!held) return;
            Point screen = PointToScreen(e.GetPosition(this));
            double dx = screen.X - pressScreen.X, dy = screen.Y - pressScreen.Y;
            if (!dragging && !charging && pressedOrb < 0 && (Math.Abs(dx) > 7 || Math.Abs(dy) > 7))
            { dragging = true; pendingTap = -1; dragDirection.Begin(screen.X, dx > 0 ? 1 : -1); Engine.Animate(dragDirection.Direction > 0 ? "running-right" : "running-left", 30); Engine.Say("飞跃！带我去你旁边", 2); }
            if (dragging)
            {
                var source = PresentationSource.FromVisual(this);
                var matrix = source.CompositionTarget.TransformFromDevice;
                Left = startLeft + dx * matrix.M11; Top = startTop + dy * matrix.M22;
                Engine.Animate(dragDirection.Move(screen.X) > 0 ? "running-right" : "running-left", 30);
            }
        }
        private void PointerUp(object sender, MouseButtonEventArgs e)
        {
            if (!held) return;
            double duration = Seconds - pressAt;
            bool wasDrag = dragging, wasCharge = charging || duration >= .55;
            int orb = pressedOrb;
            held = false; dragging = false; charging = false; pressedOrb = -1;
            View.ReleaseMouseCapture(); Touch();
            if (wasDrag) { ClampToDesktop(); Engine.Animate("jumping", .75); Save(); }
            else if (orb >= 0)
            {
                if (View.OrbAt(e.GetPosition(View)) == orb) Engine.TapOrb(orb);
            }
            else if (wasCharge) { View.PlayCard("battery"); Engine.Charge(duration); }
            else pendingTap = Seconds + Math.Max(.25, Forms.SystemInformation.DoubleClickTime / 1000.0);
            View.TakeEvents(); if (!rendering) View.InvalidateVisual(); e.Handled = true;
        }
        private void CancelPress()
        {
            held = false; charging = false; dragging = false; pressedOrb = -1; pendingTap = -1;
            View.ReleaseMouseCapture(); Engine.Animate("idle", 0);
        }
        public void ToggleCards()
        {
            if (picker != null) { picker.CancelSelection(); return; }
            if (Seconds - pickerClosedAt < .2) return;
            CancelPress(); Touch();
            picker = new CardPicker(this, Use);
            picker.Closed += delegate { picker = null; pickerClosedAt = Seconds; };
            picker.Show(); picker.Activate();
        }
        public void OpenSettings()
        {
            CancelPress(); Touch();
            var menu = new ContextMenu { FontFamily = new FontFamily("Microsoft YaHei UI"), FontSize = 13, MinWidth = 245 };
            var sizes = new MenuItem { Header = "桌宠大小" };
            foreach (double scale in new [] { .85, 1.15, 1.45 })
            {
                double choice = scale;
                var item = new MenuItem { Header = scale < 1 ? "小" : scale < 1.3 ? "中" : "大", IsChecked = Math.Abs(Options.Scale - scale) < .01 };
                item.Click += delegate { SetSize(choice, true); }; sizes.Items.Add(item);
            }
            menu.Items.Add(sizes);
            var top = Add(menu, "始终置顶", delegate { Topmost = !Topmost; Options.Topmost = Topmost; Save(); }); top.IsChecked = Topmost;
            var quiet = Add(menu, "安静模式（减少闪光和粒子）", delegate { Options.Quiet = !Options.Quiet; Save(); }); quiet.IsChecked = Options.Quiet;
            Add(menu, "怎么玩", ShowHelp);
            Add(menu, "暂时收起（托盘可找回）", delegate { Hide(); });
            Add(menu, "退出", delegate { Close(); });
            menu.PlacementTarget = View; menu.IsOpen = true;
        }
        private static MenuItem Add(ContextMenu menu, string title, Action action)
        { var item = new MenuItem { Header = title }; item.Click += delegate { action(); }; menu.Items.Add(item); return item; }
        private void SetupTray()
        {
            tray = new Forms.NotifyIcon { Text = "故障机器人 · 点我生雷球", Visible = true };
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("pet.ico")) tray.Icon = new System.Drawing.Icon(stream);
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("显示桌宠 / 找回位置", null, delegate { Dispatcher.Invoke(new Action(delegate { Show(); ClampToDesktop(); Engine.Say("我回来啦！", 2); })); });
            menu.Items.Add("选择卡牌", null, delegate { Dispatcher.Invoke(new Action(delegate { Show(); ToggleCards(); })); });
            menu.Items.Add("大小与设置", null, delegate { Dispatcher.Invoke(new Action(delegate { Show(); OpenSettings(); })); });
            menu.Items.Add("怎么玩", null, delegate { Dispatcher.Invoke(new Action(ShowHelp)); });
            menu.Items.Add("退出", null, delegate { Dispatcher.Invoke(new Action(Close)); });
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { Dispatcher.Invoke(new Action(delegate { Show(); ClampToDesktop(); })); };
        }
        public void ShowHelp()
        {
            MessageBox.Show(this,
                "轻点机器人：电击，生成一个闪电球\n双击机器人：双重释放，最右球连续激发两次\n轻点任意球：激发这一颗球\n按住机器人不动：充电；约 2 秒可充两格\n按住并立即拖动：移动桌宠\n右键机器人：打开卡牌选择\n左键卡牌：出牌\n再次右键 / Esc / 点击别处：取消选牌\n\n三个球槽满时，最早生成的球会被激发。\n新球排在左侧，最早的球排在右侧。\n全息影像重演上一招；回响让下一招施放两次。\n卡牌可自由使用，桌宠效果显示在选牌界面下方。\n\n系统托盘中可调整大小、置顶、安静模式或退出。\n本程序离线运行，偏好仅保存在本机。\n\n《杀戮尖塔 2》故障机器人同人桌宠\n球、卡面和特效读取自本机游戏素材；非官方程序。",
                "故障机器人 · 怎么玩", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private IntPtr WindowHook(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp, ref bool handled)
        {
            if ((uint)msg == Program.WakeMessage)
            {
                Show(); ClampToDesktop(); Engine.Say("我在这里！", 3); handled = true;
                return IntPtr.Zero;
            }
            if (msg == 0x0084 && !held) // WM_NCHITTEST: effects and empty stage do not act as a giant mouse target.
            {
                long bits = lp.ToInt64();
                Point point = View.PointFromScreen(new Point((short)(bits & 65535), (short)((bits >> 16) & 65535)));
                if (!View.IsInteractive(point)) { handled = true; return new IntPtr(-1); }
            }
            return IntPtr.Zero;
        }
        private void RenderPreview()
        {
            Directory.CreateDirectory(previewFolder);
            var stage = 0;
            var previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            previewTimer.Tick += delegate {
                Engine.Tick(Seconds);
                if (stage == 0) { Use("rainbow"); Engine.Say("你好，搭档。点我，试试电击？", 30); }
                if (stage == 3) Use("battery");
                if (stage == 5) Use("dualcast");
                if (stage == 7) Use("frost");
                View.TakeEvents(); View.InvalidateVisual(); View.UpdateLayout();
                if (stage == 2 || stage == 4 || stage == 6 || stage == 8)
                {
                    var bitmap = new RenderTargetBitmap(420, 390, 96, 96, PixelFormats.Pbgra32); bitmap.Render(View);
                    var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
                    using (var file = File.Create(Path.Combine(previewFolder, "desktop-" + (stage / 2 - 1) + ".png"))) png.Save(file);
                }
                if (stage == 9) {
                    var cardPreview = new CardPicker(this, delegate(string skill) { });
                    cardPreview.SavePreview(Path.Combine(previewFolder, "card-picker.png")); cardPreview.Close();
                    previewTimer.Stop(); Close();
                }
                stage++;
            };
            previewTimer.Start();
        }
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out int value, int size);
    }
    public sealed class VisualEffect
    {
        public string Kind;
        public string Card;
        public OrbKind Orb;
        public int Strength;
        public double Start;
        public Point Target;
        public Point Origin;
        public Point? Launch;
    }
    public sealed partial class PetView : FrameworkElement
    {
        private readonly PetWindow owner;
        private readonly NativeAnimation native = new NativeAnimation();
        private readonly List<VisualEffect> effects = new List<VisualEffect>();
        private static readonly Typeface face = new Typeface(new FontFamily("Microsoft YaHei UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        private static readonly Dictionary<string, FormattedText> textCache = new Dictionary<string, FormattedText>();
        private string speechMessage;
        private FormattedText speechLayout;
        public Action<double> RenderedFrame;
        public int MotionSampleCount { get { return native.SampleCount; } }
        public bool NativeFramesReady { get { return native.FramesReady; } }
        public string NativeClipName { get { return native.ClipName; } }
        private BitmapSource CurrentImage { get { return native.Image; } }
        private CardInfo playedCard;
        private double cardStart = -10;
        private string castingSkill;
        public bool Hover;
        public Point Pointer = new Point(210, 220);
        public string AccessibleStatus
        {
            get {
                var kinds = new List<string>();
                foreach (var orb in owner.Engine.Orbs)
                    kinds.Add(orb.Kind == OrbKind.Lightning ? "闪电球" : orb.Kind == OrbKind.Frost ? "冰霜球" : orb.Kind == OrbKind.Dark ? "黑暗球" : "等离子球");
                return "故障机器人；充能球 " + kinds.Count + "/" + owner.Engine.Capacity + "：" + String.Join("、", kinds.ToArray()) + "；能量 " + owner.Engine.Energy + "/3；能力：" + PowerSummary + "；" + owner.Engine.Message;
            }
        }
        protected override AutomationPeer OnCreateAutomationPeer() { return new PetViewPeer(this); }
        public void PlayCard(string skill)
        {
            var card = GameArt.Card(skill);
            if(card==null)return;
            double now=owner.Seconds;
            effects.RemoveAll(a=>a.Kind!="channel" && a.Kind!="evoke" && a.Kind!="evoke-delay");
            playedCard=card;cardStart=now;castingSkill=skill;mod.Card(skill,now);native.Play(skill,now);
            KnowledgeCard(skill,now);
        }
        private Rect body { get { return characterTransform.TransformBounds(native.Bounds); } }
        public PetView(PetWindow window)
        {
            owner = window; Width = 420; Height = 390; Cursor = Cursors.Hand;
            if(!InstalledModArt.Ready)throw new InvalidOperationException("模组动画资源加载失败");
            if(!HotCoreArt.Ready)throw new InvalidOperationException("红热核心材质加载失败");
            if(!LearningHeadArt.Ready)throw new InvalidOperationException("电路头部材质加载失败");
            if(!LearningNeckArt.Ready)throw new InvalidOperationException("原版颈部电路材质加载失败");
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
        }
        public void TakeEvents()
        {
            foreach (var item in owner.Engine.Events)
            {
                mod.Event(item,owner.Seconds);
                ArtEvent(item,owner.Seconds);
                if(item.Kind=="ai-card") { PlayCard(item.Card);continue; }
                if(item.Kind=="reboot"){effects.Clear();channelOrigins.Clear();}
                if(item.Kind=="channel" && item.Card=="meteor_strike" && item.Created!=null)
                    channelOrigins[item.Created]=new Point(344,315);
                effects.Add(new VisualEffect { Kind = item.Kind, Card=item.Card,Orb = item.Orb, Strength = item.Strength,
                    Start = owner.Seconds + item.Delay + (owner.Seconds-cardStart<.06 && item.Kind!="channel" ? NativeAnimation.ReleaseDelay(castingSkill) : 0),
                    Origin = item.Slot >= 0 ? OrbPosition(item.Slot) : new Point(210, 229),
                    Target = new Point(Math.Max(45, Math.Min(375, Pointer.X)), Math.Max(100, Math.Min(300, Pointer.Y))) });
            }
            owner.Engine.Events.Clear();
            var expiredOrigins=new List<Orb>();
            foreach(var orb in channelOrigins.Keys)if(!owner.Engine.Orbs.Contains(orb))expiredOrigins.Add(orb);
            foreach(var orb in expiredOrigins)channelOrigins.Remove(orb);
            CleanupArtOrbs();
            effects.RemoveAll(delegate(VisualEffect effect) { return owner.Seconds - effect.Start > 2.2; });
        }
        public Point OrbPosition(int index)
        {
            // New orbs enter at the left. The oldest logical slot is at the right and evokes first.
            int slots=owner.Engine.Capacity;
            double angle=(-40-100.0*index/(slots-1))*Math.PI/180;
            double radiusX=104+(slots-3)*16,centerX=210+(slots-3)*12;
            double x=centerX+Math.Cos(angle)*radiusX;
            double y=233+Math.Sin(angle)*122+Math.Sin(owner.Seconds*1.6+index*1.8)*2;
            return new Point(x, y);
        }
        public int OrbAt(Point point)
        {
            for (int i = 0; i < owner.Engine.Orbs.Count; i++)
            { Point center = OrbPosition(i); if (owner.Engine.Now >= OrbVisualBirth(owner.Engine.Orbs[i]) && (point - center).Length < 24) return i; }
            return -1;
        }
        public bool IsInteractive(Point point)
        {
            if(PowerAt(point)!=null) return true;
            if (OrbAt(point) >= 0) return true;
            var inverse=characterTransform.Inverse;
            return inverse!=null && native.Hit(inverse.Transform(point));
        }
        protected override HitTestResult HitTestCore(PointHitTestParameters parameters)
        { return IsInteractive(parameters.HitPoint) ? new PointHitTestResult(this, parameters.HitPoint) : null; }
        private static Color ColorOf(OrbKind orb)
        {
            switch (orb) { case OrbKind.Frost: return Color.FromRgb(127, 232, 255); case OrbKind.Dark: return Color.FromRgb(185, 115, 255); case OrbKind.Plasma: return Color.FromRgb(255, 198, 95); default: return Color.FromRgb(239, 246, 104); }
        }
        private static SolidColorBrush Brush(Color color, double opacity)
        { var brush = new SolidColorBrush(color); brush.Opacity = Math.Max(0, Math.Min(1, opacity)); return brush; }
        private static void Line(DrawingContext dc, Color color, double opacity, double width, Point a, Point b)
        { dc.DrawLine(new Pen(Brush(color, opacity), width), a, b); }
        private static void Ring(DrawingContext dc, Point point, double radius, Color color, double opacity, double width)
        { dc.DrawEllipse(null, new Pen(Brush(color, opacity), width), point, radius, radius); }
        private static void Text(DrawingContext dc, string value, Point point, double size, Color color)
        {
            string key = value + "|" + size + "|" + color;
            FormattedText text;
            if (!textCache.TryGetValue(key, out text))
            {
                if (textCache.Count >= 64) textCache.Clear();
                text = new FormattedText(value, CultureInfo.GetCultureInfo("zh-CN"), FlowDirection.LeftToRight, face, size, Brush(color, 1), 1.0);
                textCache[key] = text;
            }
            dc.DrawText(text, point);
        }
        private void SelectFrame()
        {
            native.Select(owner.Seconds,owner.Engine.Pose,owner.Engine.PoseSince,owner.IsDragging);
        }
        protected override void OnRender(DrawingContext dc)
        {
            if (owner.Engine.Now == 0) Program.Probe("view rendered; size=" + ActualWidth + "," + ActualHeight);
            base.OnRender(dc); SelectFrame();
            double now = owner.Seconds;
            if (RenderedFrame != null) RenderedFrame(now);
            var engine = owner.Engine;
            double cardTime = now - cardStart;
            characterTransform=CharacterTransform(cardTime);
            if (playedCard != null && cardTime >= 0 && cardTime < 2.1)
            {
                double enter = Math.Min(1, cardTime / .16);
                double fade = Math.Min(enter, Math.Min(1, (2.1 - cardTime) / .35));
                dc.PushOpacity(fade);
                dc.PushTransform(new RotateTransform(-8 + (1 - enter) * -10, 68, 220));
                dc.DrawImage(GameArt.Bitmap("card-" + playedCard.Asset), new Rect(10 - (1 - enter) * 14, 137, 116, 160));
                dc.Pop(); dc.Pop();
            }
            DrawInstalledRear(dc,now);
            DrawArtRear(dc,cardTime,now);
            DrawElementalRear(dc,cardTime,now);
            DrawPressureRear(dc,cardTime,now);
            DrawMachineryRear(dc,cardTime,now);
            DrawKnowledgeRear(dc,cardTime,now);
            DrawCardAura(dc,cardTime);
            foreach(var effect in effects)
            {
                double t=now-effect.Start;
                if(effect.Kind=="echo" && t>=0 && t<1.3)
                    DrawArtEchoTrace(dc,t);
            }
            DrawCharacter(dc, cardTime);
            DrawHotCoreMaterial(dc,now);
            DrawLearningHead(dc,now);
            DrawInstalledFront(dc,now);
            DrawArtFront(dc,cardTime,now);
            DrawElementalFront(dc,cardTime);
            DrawPressureFront(dc,cardTime);
            DrawMachineryFront(dc,cardTime,now);
            DrawKnowledgeFront(dc,cardTime,now);
            DrawCardChoreography(dc,cardTime);
            DrawAbilities(dc, now);
            for (int i = 0; i < engine.Orbs.Count; i++)
            {
                Orb orb = engine.Orbs[i]; Point point = OrbPosition(i);
                Point origin;Point? source=channelOrigins.TryGetValue(orb,out origin)?(Point?)origin:null;
                double visibleAt=OrbVisualBirth(orb);
                if (engine.Now >= visibleAt) DrawOrb(dc, point, orb.Kind, engine.Now-visibleAt, now,source);
            }
            if (owner.ChargeProgress > 0)
            {
                double progress = owner.ChargeProgress;
                Point center = ActionAnchor(2);
                Ring(dc, center, 43, ColorOf(OrbKind.Plasma), .17, 7);
                for (int i = 0; i < (int)(progress * 40); i++)
                {
                    double a = -Math.PI / 2 + i * Math.PI * 2 / 40;
                    Line(dc, ColorOf(OrbKind.Plasma), 1, 3, new Point(center.X + Math.Cos(a) * 40, center.Y + Math.Sin(a) * 40), new Point(center.X + Math.Cos(a) * 47, center.Y + Math.Sin(a) * 47));
                }
            }
            foreach (var effect in effects) DrawEffect(dc, effect, now - effect.Start);
            bool showInfo = Hover || now < engine.MessageUntil || owner.ChargeProgress > 0;
            if (showInfo)
            {
                for (int i = 0; i < 3; i++)
                    dc.DrawRoundedRectangle(Brush(ColorOf(OrbKind.Plasma), i < engine.Energy ? .95 : .15), null, new Rect(185 + i * 18, 353, 12, 5), 2, 2);
            }
            DrawPowerBar(dc);
            bool upperEffectVisible=cardTime>=0 && (castingSkill=="overclock" && cardTime<2.10 || castingSkill=="machine_learning" && cardTime<2.60 || castingSkill=="plasma" && cardTime<2.50);
            if (!owner.Options.Quiet && now < engine.MessageUntil && !upperEffectVisible) DrawSpeech(dc, engine.Message);
        }
        private Transform CharacterTransform(double time)
        {
            double lean=0,shift=0,lift=0;
            if(owner.IsDragging) lean=owner.Engine.Pose=="running-left"?-4:4;
            else if(owner.Engine.Pose=="jumping")
                lift=-24*Math.Sin(Math.Min(1,Math.Max(0,(owner.Engine.Now-owner.Engine.PoseSince)/1.1))*Math.PI);
            if(time>=0 && time<1 && !owner.IsDragging)
            {
                if(castingSkill=="ftl")shift=22*Math.Sin(Math.Min(1,time/.8)*Math.PI);
                double release=CardAnimation.For(castingSkill).Release;
                if(CardAnimation.For(castingSkill).Clip=="attack" && castingSkill!="ftl")
                    shift=-2*Window(time,.12,.19,release)+3.5*Window(time-release,.035,.11,.4);
                if(castingSkill=="rocket")shift=-3*Window(time,.01,.11,.22)+6*Window(time-release,.008,.045,.10)-5*Window(time-release,.065,.13,.30);
                if(castingSkill=="claw"){shift=-4*Window(time,.06,.22,.30)+8*Window(time-.30,.025,.14,.32);lean=3*Window(time-.30,.03,.15,.38);}
                if(castingSkill=="ball_lightning")shift=-2*Window(time,.12,.55,.72)+5*Window(time-.72,.025,.13,.30);
                if(castingSkill=="hyperbeam")shift=-5*Window(time-release,.05,.36,.9);
            }
            if(!owner.IsDragging && time>=0)
            {
                if(castingSkill=="thunder"){double brace=Window(time,.08,.82,1.75);lean=3*brace;shift+=3*brace;}
                if(castingSkill=="turbo"){double dash=Window(time,.04,.34,.92);lean=8*dash;shift+=15*dash;lift-=8*dash;}
                if(castingSkill=="ice_lance"){double thrust=Window(time-.82,.035,.23,.56);lean=3*thrust;shift+=5*thrust;lift-=1.5*thrust;}
            }
            var transform=new TransformGroup();transform.Children.Add(new RotateTransform(lean,210,330));transform.Children.Add(new TranslateTransform(shift,lift));transform.Freeze();return transform;
        }
        private void DrawCharacter(DrawingContext dc,double time)
        {
            double opacity=1;
            if(castingSkill=="reboot" && time>=0 && time<1)opacity=time<.28?1-time*2.4:Math.Min(1,.25+(time-.28)*1.5);
            dc.PushTransform(characterTransform);
            dc.PushOpacity(opacity);native.Draw(dc,owner.Seconds,IsCardArt(castingSkill) || mod.Charge.Alpha(owner.Seconds)>0 || mod.Ice.Alpha(owner.Seconds)>0);dc.Pop();
            dc.Pop();
        }

        private void DrawSpeech(DrawingContext dc, string message)
        {
            Rect panel = new Rect(70, 26, 280, 62);
            dc.DrawRoundedRectangle(Brush(Color.FromRgb(15, 27, 38), .94), new Pen(Brush(Color.FromRgb(125, 206, 214), .65), 1), panel, 14, 14);
            if (speechMessage != message || speechLayout == null)
            {
                speechMessage = message;
                speechLayout = new FormattedText(message, CultureInfo.GetCultureInfo("zh-CN"), FlowDirection.LeftToRight, face, 12, Brush(Color.FromRgb(219, 242, 240), 1), 1.0);
                speechLayout.MaxTextWidth = 252; speechLayout.TextAlignment = TextAlignment.Center;
            }
            dc.DrawText(speechLayout, new Point(84, 26 + (62 - speechLayout.Height) / 2));
        }
        private void DrawOrb(DrawingContext dc, Point point, OrbKind kind, double age, double time,Point? source=null)
        {
            Color color = ColorOf(kind);
            double grow = Math.Min(1, age / .18);
            if (age < .32)
            {
                double u = Math.Min(1, age / .32), curve = u * u * (3 - 2 * u);
                Point hand=source ?? ActionAnchor(1);
                point = new Point(hand.X + (point.X - hand.X) * curve, hand.Y + (point.Y - hand.Y) * curve - Math.Sin(u * Math.PI) * 17);
                if(source.HasValue)
                {
                    Point trail=new Point(hand.X+(point.X-hand.X)*Math.Max(0,1-.12/Math.Max(.12,u)),hand.Y+(point.Y-hand.Y)*Math.Max(0,1-.12/Math.Max(.12,u)));
                    Line(dc,kind==OrbKind.Dark?artViolet:Color.FromRgb(83,240,238),.65,2.2,trail,point);
                    Line(dc,kind==OrbKind.Dark?Color.FromRgb(207,176,255):ColorOf(OrbKind.Plasma),.55,.8,new Point(trail.X+2,trail.Y),new Point(point.X+2,point.Y));
                }
            }
            double radius = (kind == OrbKind.Dark ? 20 + Math.Min(4, age * .08) : 21) * (.65 + .35 * grow);
            if (!owner.Options.Quiet) Glow(dc, point, radius + 4 + Math.Sin(time * 2) * 1.2, color, .16);
            dc.PushOpacity(grow);
            double tilt = kind == OrbKind.Lightning ? Math.Sin(time * 2.4) * 3 : kind == OrbKind.Plasma ? Math.Sin(time * 1.4) * 5 : 0;
            dc.PushTransform(new RotateTransform(tilt, point.X, point.Y));
            dc.DrawImage(GameArt.Bitmap("orb-" + GameArt.OrbName(kind)), new Rect(point.X - radius, point.Y - radius, radius * 2, radius * 2));
            dc.Pop(); dc.Pop();
        }
        private static void Glow(DrawingContext dc, Point point, double radius, Color color, double opacity)
        {
            var fill = new RadialGradientBrush(Color.FromArgb((byte)(Math.Max(0, Math.Min(1, opacity)) * 255), color.R, color.G, color.B), Colors.Transparent);
            dc.DrawEllipse(fill, null, point, radius, radius);
        }
        private static void Stamp(DrawingContext dc, BitmapSource image, Point p, double w, double h, double angle, double opacity)
        {
            dc.PushOpacity(Math.Max(0, Math.Min(1, opacity))); dc.PushTransform(new RotateTransform(angle, p.X, p.Y));
            dc.DrawImage(image, new Rect(p.X - w / 2, p.Y - h / 2, w, h)); dc.Pop(); dc.Pop();
        }
        private void Bolt(DrawingContext dc, Point start, Point end, double t, double opacity)
        {
            Vector delta = end - start;
            double angle = Math.Atan2(delta.Y, delta.X) * 180 / Math.PI - 90;
            int variant = ((int)(t * 35)) % 3;
            string name = variant == 0 ? "bolt-01b" : variant == 1 ? "bolt-03" : "bolt-04";
            var center = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            Stamp(dc, GameArt.Bitmap(name), center, 22, Math.Max(20, delta.Length), angle, opacity);
        }
        private void DrawEffect(DrawingContext dc, VisualEffect effect, double t)
        {
            double lifetime=effect.Kind=="lance" || effect.Kind=="glacier"?2.2:1.4;
            if (t < 0 || t > lifetime) return;
            double fade = Math.Pow(Math.Max(0, 1 - t / lifetime), 1.5);
            if (owner.Options.Quiet) fade *= .22;
            if(DrawEvokePulse(dc,effect,t,fade))return;
            if(DrawElementalEvent(dc,effect,t))return;
            if(DrawPressureEvent(dc,effect,t))return;
            if(DrawMachineryEvent(dc,effect,t))return;
            if(DrawArtEvent(dc,effect,t))return;
            Color color = ColorOf(effect.Orb); Point center = ActionAnchor(2);
            if (effect.Kind == "rocket")
            {
                if(t>.55)return;
                Point fist=ActionAnchor(1);
                if(t<.08) { Glow(dc,fist,14,ColorOf(OrbKind.Plasma),fade*.22);return; }
                Point impact=new Point(Math.Min(390,fist.X+24),fist.Y);
                Bolt(dc,fist,impact,t,fade*.55);
                Stamp(dc,GameArt.Sequence(2,Math.Min(8,(int)((t-.08)/.31*9))),impact,46,46,0,fade*.75);
            }
            else if (effect.Kind == "charge")
            {
                return;
            }
            else if (effect.Kind == "reboot")
            {
                if (t > .4) Glow(dc, new Point(210, 191), 22, Color.FromRgb(92, 221, 255), fade * .6);
            }
            else if (effect.Kind == "channel")
            {
                if (!owner.Options.Quiet && t < .42)
                {
                    for (int i = 0; i < 3; i++) { double a = i * Math.PI * 2 / 3 + t; double r = 21 * Math.Max(0, 1 - t / .42); Point q = new Point(effect.Origin.X + Math.Cos(a) * r, effect.Origin.Y + Math.Sin(a) * r); Stamp(dc, GameArt.Bitmap("fx-" + GameArt.OrbName(effect.Orb)), q, 5, 9, a * 180 / Math.PI, fade * .3); }
                }
            }
            else DrawCardEffect(dc, effect, t, fade);
        }
    }
    public sealed class PetViewPeer : FrameworkElementAutomationPeer
    {
        public PetViewPeer(PetView owner) : base(owner) { }
        protected override string GetNameCore() { return ((PetView)Owner).AccessibleStatus; }
        protected override string GetClassNameCore() { return "DefectCompanion"; }
        protected override AutomationControlType GetAutomationControlTypeCore() { return AutomationControlType.Custom; }
    }
}
