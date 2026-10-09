using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace CursorClickApp
{
    [Serializable]
    public sealed class AppSettings
    {
        public bool Enabled = true;
        public string Preset = "Normal";
        public string LeftColor = "#168BFF";
        public string RightColor = "#FF3737";
        public string MiddleColor = "#35C46A";
        public int Size = 92;
        public int Opacity = 90;
        public int Duration = 440;
        public int Waves = 2;
        public int RateLimitMs = 45;
        public bool PauseFullscreen = true;
        public bool PauseHighCpu = true;
        public int HighCpuPercent = 85;
        public bool StartWithWindows = false;
        public bool HoldEffects = true;
        public bool DoubleClickEffects = true;
        public bool Glow = false;
        public string LeftShape = "Circle";
        public string RightShape = "Circle";
        public string MiddleShape = "Circle";
        public string ExcludedApps = "";
        public bool WelcomeShown = false;
        public bool CursorGlow = false;
        public string CursorGlowColor = "#168BFF";
        public int CursorGlowSize = 42;
        public int CursorGlowOpacity = 38;
    }

    internal static class SettingsStore
    {
        internal static readonly string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CursorClick");
        internal static readonly string FilePath = Path.Combine(DirectoryPath, "settings.xml");

        internal static AppSettings Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new AppSettings();
                using (FileStream stream = File.OpenRead(FilePath))
                    return (AppSettings)new XmlSerializer(typeof(AppSettings)).Deserialize(stream);
            }
            catch { return new AppSettings(); }
        }

        internal static void Save(AppSettings value)
        {
            Directory.CreateDirectory(DirectoryPath);
            string temp = FilePath + ".tmp";
            using (FileStream stream = File.Create(temp))
                new XmlSerializer(typeof(AppSettings)).Serialize(stream, value);
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(temp, FilePath);
        }

        internal static AppSettings LoadFrom(string path)
        {
            using (FileStream stream = File.OpenRead(path)) return (AppSettings)new XmlSerializer(typeof(AppSettings)).Deserialize(stream);
        }

        internal static void SaveTo(string path, AppSettings value)
        {
            using (FileStream stream = File.Create(path)) new XmlSerializer(typeof(AppSettings)).Serialize(stream, value);
        }
    }

    internal static class Native
    {
        internal const int WH_MOUSE_LL = 14, WM_APP_SHOW = 0x8031, WM_HOTKEY = 0x0312;
        internal const int WM_MOUSEMOVE = 0x0200, WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202;
        internal const int WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205;
        internal const int WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208;
        internal const int MOD_CONTROL = 0x0002, MOD_ALT = 0x0001;
        internal const int SW_SHOWNORMAL = 1;
        internal const int MONITOR_DEFAULTTONEAREST = 2;
        internal delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)] internal struct POINT { internal int X, Y; }
        [StructLayout(LayoutKind.Sequential)] internal struct MSLLHOOKSTRUCT { internal POINT pt; internal uint mouseData, flags, time; internal UIntPtr extra; }
        [StructLayout(LayoutKind.Sequential)] internal struct RECT { internal int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] internal struct FILETIME { internal uint Low, High; }
        [StructLayout(LayoutKind.Sequential)] internal struct SIZE { internal int X, Y; internal SIZE(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] internal struct BLENDFUNCTION { internal byte Op, Flags, Alpha, Format; }

        [DllImport("user32.dll", SetLastError = true)] internal static extern IntPtr SetWindowsHookEx(int id, HookProc callback, IntPtr module, uint threadId);
        [DllImport("user32.dll", SetLastError = true)] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wp, IntPtr lp);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)] internal static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll", SetLastError = true)] internal static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hwnd, int id);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] internal static extern IntPtr FindWindow(string cls, string caption);
        [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);
        [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")] internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
        [DllImport("user32.dll")] internal static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);
        [DllImport("kernel32.dll")] internal static extern bool GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);
        [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
        [DllImport("user32.dll", EntryPoint = "SetProcessDpiAwarenessContext")] internal static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
        [DllImport("user32.dll")] internal static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] internal static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
        [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr obj);
        [DllImport("user32.dll")] internal static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr dstDc, ref POINT dst, ref SIZE size, IntPtr srcDc, ref POINT src, uint colorKey, ref BLENDFUNCTION blend, uint flags);

        [StructLayout(LayoutKind.Sequential)] internal struct MONITORINFO
        {
            internal int Size; internal RECT Monitor; internal RECT Work; internal uint Flags;
        }

        internal static ulong ToUInt64(FILETIME value) { return ((ulong)value.High << 32) | value.Low; }
    }

    internal sealed class ClickEvent
    {
        internal Point Point;
        internal MouseButtons Button;
        internal bool IsDouble;
        internal bool IsHold;
        internal bool IsMove;
    }

    internal sealed class MouseHook : IDisposable
    {
        private readonly Native.HookProc callback;
        private readonly Action<ClickEvent> sink;
        private IntPtr hook;
        private readonly Dictionary<MouseButtons, DateTime> downSince = new Dictionary<MouseButtons, DateTime>();
        private DateTime lastDown = DateTime.MinValue;
        private Point lastPoint;
        private MouseButtons lastButton;
        private int lastMoveTick;

        internal MouseHook(Action<ClickEvent> eventSink)
        {
            sink = eventSink;
            callback = HookCallback;
            using (Process process = Process.GetCurrentProcess())
            using (ProcessModule module = process.MainModule)
                hook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, callback, Native.GetModuleHandle(module.ModuleName), 0);
            if (hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        internal Dictionary<MouseButtons, DateTime> DownButtons { get { return downSince; } }
        internal bool TrackMoves { get; set; }

        private IntPtr HookCallback(int code, IntPtr wp, IntPtr lp)
        {
            if (code >= 0)
            {
                int message = wp.ToInt32();
                if (message == Native.WM_MOUSEMOVE)
                {
                    if (!TrackMoves) return Native.CallNextHookEx(hook, code, wp, lp);
                    int tick = Environment.TickCount;
                    if (unchecked(tick - lastMoveTick) >= 16)
                    {
                        lastMoveTick = tick;
                        Native.MSLLHOOKSTRUCT move = (Native.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lp, typeof(Native.MSLLHOOKSTRUCT));
                        sink(new ClickEvent { Point = new Point(move.pt.X, move.pt.Y), IsMove = true });
                    }
                    return Native.CallNextHookEx(hook, code, wp, lp);
                }
                MouseButtons button = MouseButtons.None;
                bool down = false, up = false;
                if (message == Native.WM_LBUTTONDOWN) { button = MouseButtons.Left; down = true; }
                else if (message == Native.WM_RBUTTONDOWN) { button = MouseButtons.Right; down = true; }
                else if (message == Native.WM_MBUTTONDOWN) { button = MouseButtons.Middle; down = true; }
                else if (message == Native.WM_LBUTTONUP) { button = MouseButtons.Left; up = true; }
                else if (message == Native.WM_RBUTTONUP) { button = MouseButtons.Right; up = true; }
                else if (message == Native.WM_MBUTTONUP) { button = MouseButtons.Middle; up = true; }
                if (button != MouseButtons.None)
                {
                    Native.MSLLHOOKSTRUCT data = (Native.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lp, typeof(Native.MSLLHOOKSTRUCT));
                    Point point = new Point(data.pt.X, data.pt.Y);
                    if (down)
                    {
                        DateTime now = DateTime.UtcNow;
                        bool dbl = button == lastButton && (now - lastDown).TotalMilliseconds <= SystemInformation.DoubleClickTime &&
                                   Math.Abs(point.X - lastPoint.X) <= SystemInformation.DoubleClickSize.Width &&
                                   Math.Abs(point.Y - lastPoint.Y) <= SystemInformation.DoubleClickSize.Height;
                        lock (downSince) { downSince[button] = now; }
                        lastDown = now; lastPoint = point; lastButton = button;
                        sink(new ClickEvent { Point = point, Button = button, IsDouble = dbl });
                    }
                    else if (up) lock (downSince) { downSince.Remove(button); }
                }
            }
            return Native.CallNextHookEx(hook, code, wp, lp);
        }

        public void Dispose() { if (hook != IntPtr.Zero) { Native.UnhookWindowsHookEx(hook); hook = IntPtr.Zero; } }
    }

    internal sealed class EffectForm : Form
    {
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private readonly Stopwatch clock = new Stopwatch();
        private AppSettings settings;
        private Color color;
        private int waves;
        private string shape;
        private Color cachedGlowColor;
        private int cachedGlowSize, cachedGlowOpacity;
        internal bool Active { get { return timer.Enabled; } }

        internal EffectForm()
        {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
            StartPosition = FormStartPosition.Manual; DoubleBuffered = true;
            timer.Interval = 16; timer.Tick += delegate
            {
                double totalDuration = settings.Duration + (waves - 1) * 95.0;
                RenderLayer();
                if (clock.ElapsedMilliseconds > totalDuration + 30) StopEffect();
            };
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams { get { CreateParams p = base.CreateParams; p.ExStyle |= 0x20 | 0x80 | 0x08000000 | 0x00080000; return p; } }

        internal void Play(Point screenPoint, Color value, string shapeName, AppSettings current, int waveCount, bool isDouble, bool isHold)
        {
            settings = current; color = value; shape = shapeName; waves = Math.Max(1, Math.Min(5, waveCount + (isDouble ? 1 : 0)));
            int extent = current.Size + (current.Glow ? 32 : 16);
            Size = new Size(extent, extent); Location = new Point(screenPoint.X - extent / 2, screenPoint.Y - extent / 2);
            clock.Restart(); if (!Visible) Show(); timer.Start(); RenderLayer();
        }

        private void StopEffect() { timer.Stop(); clock.Stop(); Hide(); }

        private void RenderLayer()
        {
            if (!IsHandleCreated || Width <= 0 || Height <= 0) return;
            using (Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent); graphics.SmoothingMode = SmoothingMode.AntiAlias;
                double now = clock.Elapsed.TotalMilliseconds;
                for (int i = 0; i < waves; i++)
                {
                    double progress = (now - i * 95.0) / settings.Duration;
                    if (progress < 0 || progress > 1) continue;
                    float eased = (float)(1 - Math.Pow(1 - progress, 3));
                    float diameter = settings.Size * (0.18f + 0.82f * eased);
                    int alpha = (int)(255 * settings.Opacity / 100.0 * (1 - progress));
                    RectangleF rect = new RectangleF((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);
                    DrawShape(graphics, rect, Color.FromArgb(Math.Max(0, alpha), color), settings.Glow);
                }
                PushBitmap(bitmap);
            }
        }

        private void DrawShape(Graphics g, RectangleF rect, Color drawColor, bool glow)
        {
            if (glow) using (Pen gp = new Pen(Color.FromArgb(drawColor.A / 4, drawColor), 9f)) DrawGeometry(g, gp, rect);
            using (Pen pen = new Pen(drawColor, 2.7f)) DrawGeometry(g, pen, rect);
            if (shape == "Dot") using (Brush b = new SolidBrush(drawColor)) g.FillEllipse(b, rect);
            if (shape == "Sparks")
            {
                float cx = Width / 2f, cy = Height / 2f, r1 = rect.Width * .35f, r2 = rect.Width * .55f;
                using (Pen pen = new Pen(drawColor, 2.2f)) for (int n = 0; n < 8; n++) { double a = n * Math.PI / 4; g.DrawLine(pen, cx + (float)Math.Cos(a) * r1, cy + (float)Math.Sin(a) * r1, cx + (float)Math.Cos(a) * r2, cy + (float)Math.Sin(a) * r2); }
            }
        }

        private void DrawGeometry(Graphics g, Pen pen, RectangleF rect)
        {
            if (shape == "Cross") { g.DrawLine(pen, rect.Left, rect.Top, rect.Right, rect.Bottom); g.DrawLine(pen, rect.Right, rect.Top, rect.Left, rect.Bottom); }
            else if (shape == "Wave") { g.DrawArc(pen, rect, 15, 150); g.DrawArc(pen, rect, 195, 150); }
            else if (shape != "Dot" && shape != "Sparks") g.DrawEllipse(pen, rect);
        }

        private void PushBitmap(Bitmap bitmap)
        {
            IntPtr screen = Native.GetDC(IntPtr.Zero), memory = Native.CreateCompatibleDC(screen), hBitmap = IntPtr.Zero, old = IntPtr.Zero;
            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0)); old = Native.SelectObject(memory, hBitmap);
                Native.POINT dst = new Native.POINT { X = Left, Y = Top }, src = new Native.POINT(); Native.SIZE sz = new Native.SIZE(Width, Height);
                Native.BLENDFUNCTION blend = new Native.BLENDFUNCTION { Op = 0, Flags = 0, Alpha = 255, Format = 1 };
                Native.UpdateLayeredWindow(Handle, screen, ref dst, ref sz, memory, ref src, 0, ref blend, 2);
            }
            finally { if (old != IntPtr.Zero) Native.SelectObject(memory, old); if (hBitmap != IntPtr.Zero) Native.DeleteObject(hBitmap); Native.DeleteDC(memory); Native.ReleaseDC(IntPtr.Zero, screen); }
        }

        internal void ShowCursorGlow(Point screenPoint, Color glowColor, int glowSize, int glowOpacity)
        {
            timer.Stop(); clock.Stop();
            int extent = glowSize + 24; Location = new Point(screenPoint.X - extent / 2, screenPoint.Y - extent / 2);
            if (Visible && cachedGlowColor == glowColor && cachedGlowSize == glowSize && cachedGlowOpacity == glowOpacity) return;
            cachedGlowColor = glowColor; cachedGlowSize = glowSize; cachedGlowOpacity = glowOpacity; Size = new Size(extent, extent);
            if (!Visible) Show();
            using (Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent); graphics.SmoothingMode = SmoothingMode.AntiAlias;
                float center = extent / 2f;
                for (int n = 8; n >= 1; n--)
                {
                    float d = glowSize + n * 2f; int alpha = Math.Max(1, glowOpacity * (9 - n) / 12);
                    using (Pen pen = new Pen(Color.FromArgb(alpha, glowColor), n * 1.8f)) graphics.DrawEllipse(pen, center - d / 2, center - d / 2, d, d);
                }
                using (Pen edge = new Pen(Color.FromArgb(Math.Min(180, glowOpacity * 3), glowColor), 1.5f)) graphics.DrawEllipse(edge, center - glowSize / 2f, center - glowSize / 2f, glowSize, glowSize);
                PushBitmap(bitmap);
            }
        }

        internal void HideCursorGlow() { timer.Stop(); Hide(); }
    }

    internal sealed class SettingsForm : Form
    {
        private readonly MainForm owner;
        private readonly ComboBox preset = new ComboBox();
        private readonly ComboBox leftShape = ShapeBox(), rightShape = ShapeBox(), middleShape = ShapeBox();
        private readonly TextBox left = new TextBox(), right = new TextBox(), middle = new TextBox();
        private readonly TextBox cursorGlowColor = new TextBox();
        private readonly TextBox excluded = new TextBox();
        private readonly NumericUpDown size = Num(40, 220), opacity = Num(10, 100), duration = Num(120, 1500), waves = Num(1, 5), rate = Num(0, 500), cpu = Num(40, 100);
        private readonly NumericUpDown cursorGlowSize = Num(16, 140), cursorGlowOpacity = Num(5, 100);
        private readonly CheckBox fullscreen = new CheckBox(), highCpu = new CheckBox(), startup = new CheckBox(), hold = new CheckBox(), doubleClick = new CheckBox(), glow = new CheckBox(), cursorGlow = new CheckBox();

        private static NumericUpDown Num(int min, int max) { return new NumericUpDown { Minimum = min, Maximum = max, Dock = DockStyle.Fill }; }
        private static ComboBox ShapeBox() { ComboBox box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill }; box.Items.AddRange(new object[] { "Circle", "Dot", "Cross", "Wave", "Sparks" }); return box; }
        internal SettingsForm(MainForm main)
        {
            owner = main; Text = "CursorClick — настройки"; Width = 500; Height = 720; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterScreen;
            TableLayoutPanel table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2, RowCount = 27, AutoScroll = true };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            preset.DropDownStyle = ComboBoxStyle.DropDownList; preset.Items.AddRange(new object[] { "Minimal", "Normal", "Bright" }); preset.Dock = DockStyle.Fill;
            PrepareColorBox(left); PrepareColorBox(right); PrepareColorBox(middle); PrepareColorBox(cursorGlowColor);
            Add(table, "Пресет", preset); Add(table, "Цвет левой кнопки", left); Add(table, "Цвет правой кнопки", right); Add(table, "Цвет средней кнопки", middle);
            Add(table, "Форма левой кнопки", leftShape); Add(table, "Форма правой кнопки", rightShape); Add(table, "Форма средней кнопки", middleShape);
            Add(table, "Размер (px)", Slider(size, 40, 220)); Add(table, "Прозрачность (%)", Slider(opacity, 10, 100)); Add(table, "Длительность (мс)", Slider(duration, 120, 1500)); Add(table, "Количество волн", waves); Add(table, "Лимит кликов (мс)", rate); Add(table, "Порог CPU (%)", cpu);
            Box(cursorGlow, "Постоянное свечение указателя"); table.Controls.Add(cursorGlow); table.SetColumnSpan(cursorGlow, 2);
            Add(table, "Цвет свечения указателя", cursorGlowColor); Add(table, "Размер свечения", Slider(cursorGlowSize, 16, 140)); Add(table, "Яркость свечения (%)", Slider(cursorGlowOpacity, 5, 100));
            excluded.Text = ""; Add(table, "Исключения (game.exe, app.exe)", excluded);
            Box(fullscreen, "Отключать в полноэкранных приложениях"); Box(highCpu, "Отключать при высокой загрузке CPU"); Box(startup, "Запускать вместе с Windows"); Box(hold, "Эффект при удержании кнопки"); Box(doubleClick, "Особый двойной клик"); Box(glow, "Свечение (увеличивает нагрузку)");
            foreach (CheckBox box in new[] { fullscreen, highCpu, startup, hold, doubleClick, glow }) { table.Controls.Add(box); table.SetColumnSpan(box, 2); }
            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
            Button save = new Button { Text = "Сохранить", AutoSize = true }; Button cancel = new Button { Text = "Отмена", AutoSize = true }; Button preview = new Button { Text = "Предпросмотр", AutoSize = true }; Button reset = new Button { Text = "Сбросить", AutoSize = true }; Button import = new Button { Text = "Импорт", AutoSize = true }; Button export = new Button { Text = "Экспорт", AutoSize = true };
            save.Click += delegate { Apply(); }; cancel.Click += delegate { Hide(); }; preview.Click += delegate { ApplyValues(false); owner.Preview(); }; reset.Click += delegate { LoadObject(new AppSettings()); }; import.Click += delegate { ImportSettings(); }; export.Click += delegate { ExportSettings(); };
            buttons.Controls.Add(save); buttons.Controls.Add(cancel); buttons.Controls.Add(preview); buttons.Controls.Add(reset); buttons.Controls.Add(export); buttons.Controls.Add(import); table.Controls.Add(buttons); table.SetColumnSpan(buttons, 2);
            Controls.Add(table); FormClosing += delegate(object s, FormClosingEventArgs e) { if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; if (ApplyValues(true)) Hide(); } };
            preset.SelectedIndexChanged += delegate { if (Visible && preset.Focused) ApplyPreset(preset.Text); };
        }

        private static void Add(TableLayoutPanel t, string label, Control control) { t.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }); t.Controls.Add(control); }
        private static void Box(CheckBox box, string text) { box.Text = text; box.AutoSize = true; }
        private static Control Slider(NumericUpDown number, int min, int max)
        {
            TableLayoutPanel panel = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, Height = 32 };
            TrackBar bar = new TrackBar { Minimum = min, Maximum = max, TickStyle = TickStyle.None, Dock = DockStyle.Fill, Value = (int)number.Value };
            number.Width = 64; number.Dock = DockStyle.Right;
            bar.ValueChanged += delegate { if (number.Value != bar.Value) number.Value = bar.Value; };
            number.ValueChanged += delegate { int value = Math.Max(bar.Minimum, Math.Min(bar.Maximum, (int)number.Value)); if (bar.Value != value) bar.Value = value; };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68)); panel.Controls.Add(bar); panel.Controls.Add(number); return panel;
        }
        private void PrepareColorBox(TextBox box)
        {
            box.ReadOnly = true; box.Cursor = Cursors.Hand; box.TextAlign = HorizontalAlignment.Center; box.Dock = DockStyle.Fill;
            box.Click += delegate { ChooseColor(box); };
        }
        private void ChooseColor(TextBox box)
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                try { dialog.Color = ColorTranslator.FromHtml(box.Text); } catch { dialog.Color = Color.DodgerBlue; }
                dialog.FullOpen = true;
                if (dialog.ShowDialog(this) == DialogResult.OK) SetColorVisual(box, ColorTranslator.ToHtml(dialog.Color));
            }
        }
        private static void SetColorVisual(TextBox box, string html)
        {
            Color color = ColorTranslator.FromHtml(html);
            box.Text = html; box.BackColor = color;
            box.ForeColor = color.GetBrightness() < 0.5f ? Color.White : Color.Black;
        }
        internal void LoadValues()
        {
            LoadObject(owner.Settings);
        }
        private void LoadObject(AppSettings s)
        {
            preset.SelectedItem = s.Preset; if (preset.SelectedIndex < 0) preset.SelectedIndex = 1;
            SetColorVisual(left, s.LeftColor); SetColorVisual(right, s.RightColor); SetColorVisual(middle, s.MiddleColor); size.Value = s.Size; opacity.Value = s.Opacity; duration.Value = s.Duration; waves.Value = s.Waves; rate.Value = s.RateLimitMs; cpu.Value = s.HighCpuPercent;
            SetColorVisual(cursorGlowColor, s.CursorGlowColor); cursorGlowSize.Value = s.CursorGlowSize; cursorGlowOpacity.Value = s.CursorGlowOpacity; cursorGlow.Checked = s.CursorGlow;
            leftShape.SelectedItem = s.LeftShape; rightShape.SelectedItem = s.RightShape; middleShape.SelectedItem = s.MiddleShape; if (leftShape.SelectedIndex < 0) leftShape.SelectedIndex = 0; if (rightShape.SelectedIndex < 0) rightShape.SelectedIndex = 0; if (middleShape.SelectedIndex < 0) middleShape.SelectedIndex = 0; excluded.Text = s.ExcludedApps;
            fullscreen.Checked = s.PauseFullscreen; highCpu.Checked = s.PauseHighCpu; startup.Checked = s.StartWithWindows; hold.Checked = s.HoldEffects; doubleClick.Checked = s.DoubleClickEffects; glow.Checked = s.Glow;
        }

        private void ApplyPreset(string name)
        {
            if (name == "Minimal") { size.Value = 70; opacity.Value = 72; duration.Value = 300; waves.Value = 1; glow.Checked = false; }
            else if (name == "Bright") { size.Value = 118; opacity.Value = 100; duration.Value = 560; waves.Value = 3; glow.Checked = true; }
            else { size.Value = 92; opacity.Value = 90; duration.Value = 440; waves.Value = 2; glow.Checked = false; }
        }

        private void Apply()
        {
            if (!ApplyValues(true)) return;
            Hide();
        }

        private bool ApplyValues(bool save)
        {
            try { ColorTranslator.FromHtml(left.Text); ColorTranslator.FromHtml(right.Text); ColorTranslator.FromHtml(middle.Text); ColorTranslator.FromHtml(cursorGlowColor.Text); }
            catch { MessageBox.Show("Цвет укажите в формате #RRGGBB.", "CursorClick", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            AppSettings s = owner.Settings; s.Preset = preset.Text; s.LeftColor = left.Text; s.RightColor = right.Text; s.MiddleColor = middle.Text; s.LeftShape = leftShape.Text; s.RightShape = rightShape.Text; s.MiddleShape = middleShape.Text; s.ExcludedApps = excluded.Text; s.Size = (int)size.Value; s.Opacity = (int)opacity.Value; s.Duration = (int)duration.Value; s.Waves = (int)waves.Value; s.RateLimitMs = (int)rate.Value; s.HighCpuPercent = (int)cpu.Value; s.PauseFullscreen = fullscreen.Checked; s.PauseHighCpu = highCpu.Checked; s.StartWithWindows = startup.Checked; s.HoldEffects = hold.Checked; s.DoubleClickEffects = doubleClick.Checked; s.Glow = glow.Checked; s.CursorGlow = cursorGlow.Checked; s.CursorGlowColor = cursorGlowColor.Text; s.CursorGlowSize = (int)cursorGlowSize.Value; s.CursorGlowOpacity = (int)cursorGlowOpacity.Value;
            if (save) owner.SaveAndApply(); return true;
        }

        private void ImportSettings()
        {
            using (OpenFileDialog dialog = new OpenFileDialog { Filter = "CursorClick preset (*.xml)|*.xml" })
                if (dialog.ShowDialog(this) == DialogResult.OK) try { LoadObject(SettingsStore.LoadFrom(dialog.FileName)); } catch (Exception ex) { MessageBox.Show("Не удалось импортировать: " + ex.Message); }
        }
        private void ExportSettings()
        {
            if (!ApplyValues(false)) return;
            using (SaveFileDialog dialog = new SaveFileDialog { Filter = "CursorClick preset (*.xml)|*.xml", FileName = "CursorClick-preset.xml" })
                if (dialog.ShowDialog(this) == DialogResult.OK) try { SettingsStore.SaveTo(dialog.FileName, owner.Settings); } catch (Exception ex) { MessageBox.Show("Не удалось экспортировать: " + ex.Message); }
        }
    }

    internal sealed class MainForm : Form
    {
        internal AppSettings Settings { get; private set; }
        private readonly NotifyIcon tray = new NotifyIcon();
        private readonly ToolStripMenuItem enabledItem = new ToolStripMenuItem("Эффекты включены");
        private readonly ToolStripMenuItem statsItem = new ToolStripMenuItem("CPU: 0% · RAM: 0 МБ");
        private readonly ToolStripMenuItem startupItem = new ToolStripMenuItem("Запуск с Windows");
        private readonly System.Windows.Forms.Timer statusTimer = new System.Windows.Forms.Timer(), holdTimer = new System.Windows.Forms.Timer();
        private readonly List<EffectForm> pool = new List<EffectForm>();
        private readonly EffectForm cursorGlowForm = new EffectForm();
        private MouseHook mouseHook;
        private SettingsForm settingsForm;
        private DateTime lastEffect = DateTime.MinValue;
        private readonly Dictionary<MouseButtons, DateTime> lastHoldPulse = new Dictionary<MouseButtons, DateTime>();
        private ulong oldIdle, oldKernel, oldUser;
        private double systemCpu;
        private TimeSpan oldProcessCpu;
        private DateTime oldProcessSample = DateTime.UtcNow;
        private bool pausedByEnvironment;

        internal MainForm()
        {
            Settings = SettingsStore.Load(); Text = "CursorClick.Control." + Environment.UserName; ShowInTaskbar = false; Opacity = 0; Size = new Size(1, 1); Location = new Point(-32000, -32000);
            CreateTray();
            for (int i = 0; i < 6; i++) pool.Add(new EffectForm());
            mouseHook = new MouseHook(QueueClick);
            mouseHook.TrackMoves = Settings.CursorGlow;
            statusTimer.Interval = 2000; statusTimer.Tick += delegate { UpdateEnvironment(); }; statusTimer.Start();
            holdTimer.Interval = 100; holdTimer.Tick += delegate { CheckHolds(); }; holdTimer.Start();
            Load += delegate
            {
                Native.RegisterHotKey(Handle, 1, Native.MOD_CONTROL | Native.MOD_ALT, (uint)Keys.C); Hide();
                if (!Settings.WelcomeShown) { Settings.WelcomeShown = true; SettingsStore.Save(Settings); tray.ShowBalloonTip(6000, "CursorClick запущен", "Двойной клик по значку открывает настройки. Ctrl+Alt+C включает или выключает эффекты.", ToolTipIcon.Info); }
            };
        }

        private void CreateTray()
        {
            Bitmap bitmap = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bitmap)) { g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.Transparent); using (Pen p = new Pen(Color.DodgerBlue, 4)) { g.DrawEllipse(p, 3, 3, 25, 25); g.DrawEllipse(p, 10, 10, 11, 11); } }
            tray.Icon = Icon.FromHandle(bitmap.GetHicon()); tray.Text = "CursorClick"; tray.Visible = true;
            ContextMenuStrip menu = new ContextMenuStrip();
            enabledItem.CheckOnClick = true; enabledItem.Checked = Settings.Enabled; enabledItem.Click += delegate { Settings.Enabled = enabledItem.Checked; SaveAndApply(); };
            ToolStripMenuItem styles = new ToolStripMenuItem("Стиль");
            foreach (string style in new[] { "Minimal", "Normal", "Bright" }) { ToolStripMenuItem item = new ToolStripMenuItem(style); item.Click += delegate(object s, EventArgs e) { ApplyPreset(((ToolStripMenuItem)s).Text); }; styles.DropDownItems.Add(item); }
            ToolStripMenuItem settings = new ToolStripMenuItem("Настройки…"); settings.Click += delegate { ShowSettings(); };
            startupItem.CheckOnClick = true; startupItem.Checked = Settings.StartWithWindows; startupItem.Click += delegate { Settings.StartWithWindows = startupItem.Checked; SaveAndApply(); };
            statsItem.Enabled = false;
            ToolStripMenuItem exit = new ToolStripMenuItem("Выход"); exit.Click += delegate { Close(); };
            menu.Items.Add(enabledItem); menu.Items.Add(styles); menu.Items.Add(settings); menu.Items.Add(startupItem); menu.Items.Add(new ToolStripSeparator()); menu.Items.Add(statsItem); menu.Items.Add(new ToolStripSeparator()); menu.Items.Add(exit);
            tray.ContextMenuStrip = menu; tray.DoubleClick += delegate { ShowSettings(); };
        }

        private void ApplyPreset(string name)
        {
            Settings.Preset = name;
            if (name == "Minimal") { Settings.Size = 70; Settings.Opacity = 72; Settings.Duration = 300; Settings.Waves = 1; Settings.Glow = false; }
            else if (name == "Bright") { Settings.Size = 118; Settings.Opacity = 100; Settings.Duration = 560; Settings.Waves = 3; Settings.Glow = true; }
            else { Settings.Size = 92; Settings.Opacity = 90; Settings.Duration = 440; Settings.Waves = 2; Settings.Glow = false; }
            SaveAndApply();
        }

        internal void SaveAndApply()
        {
            SettingsStore.Save(Settings); enabledItem.Checked = Settings.Enabled; startupItem.Checked = Settings.StartWithWindows;
            if (mouseHook != null) mouseHook.TrackMoves = Settings.CursorGlow;
            if (!Settings.CursorGlow || !Settings.Enabled) cursorGlowForm.HideCursorGlow();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (Settings.StartWithWindows)
                    {
                        string startupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CursorClick");
                        string startupExecutable = Path.Combine(startupDirectory, "CursorClick.exe");
                        Directory.CreateDirectory(startupDirectory);
                        if (!string.Equals(Path.GetFullPath(Application.ExecutablePath), Path.GetFullPath(startupExecutable), StringComparison.OrdinalIgnoreCase))
                            File.Copy(Application.ExecutablePath, startupExecutable, true);
                        key.SetValue("CursorClick", "\"" + startupExecutable + "\" --startup", RegistryValueKind.String);
                    }
                    else key.DeleteValue("CursorClick", false);
                }
            }
            catch (Exception ex) { tray.ShowBalloonTip(3000, "CursorClick", "Не удалось изменить автозапуск: " + ex.Message, ToolTipIcon.Warning); }
        }

        private void QueueClick(ClickEvent value)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke((MethodInvoker)delegate { ProcessClick(value); }); } catch { }
        }

        private void ProcessClick(ClickEvent value)
        {
            if (value.IsMove)
            {
                if (Settings.CursorGlow && Settings.Enabled && !pausedByEnvironment && !IsOwnWindowForeground() && !IsForegroundExcluded())
                    cursorGlowForm.ShowCursorGlow(value.Point, ColorTranslator.FromHtml(Settings.CursorGlowColor), Settings.CursorGlowSize, Settings.CursorGlowOpacity);
                else cursorGlowForm.HideCursorGlow();
                return;
            }
            if (!Settings.Enabled || pausedByEnvironment || IsOwnWindowForeground() || IsForegroundExcluded()) return;
            if (value.IsDouble && !Settings.DoubleClickEffects) value.IsDouble = false;
            DateTime now = DateTime.UtcNow;
            if (!value.IsDouble && (now - lastEffect).TotalMilliseconds < Settings.RateLimitMs) return;
            lastEffect = now;
            Color color = ColorTranslator.FromHtml(value.Button == MouseButtons.Right ? Settings.RightColor : value.Button == MouseButtons.Middle ? Settings.MiddleColor : Settings.LeftColor);
            string shape = value.Button == MouseButtons.Right ? Settings.RightShape : value.Button == MouseButtons.Middle ? Settings.MiddleShape : Settings.LeftShape;
            if (value.IsDouble) color = Color.Gold;
            EffectForm target = pool.Find(delegate(EffectForm f) { return !f.Active; });
            if (target == null) target = pool[0];
            target.Play(value.Point, color, shape, Settings, Settings.Waves, value.IsDouble, value.IsHold);
        }

        private bool IsOwnWindowForeground()
        {
            try
            {
                uint pid;
                Native.GetWindowThreadProcessId(Native.GetForegroundWindow(), out pid);
                return pid == (uint)Process.GetCurrentProcess().Id;
            }
            catch { return false; }
        }

        private bool IsForegroundExcluded()
        {
            if (string.IsNullOrWhiteSpace(Settings.ExcludedApps)) return false;
            try
            {
                uint pid; Native.GetWindowThreadProcessId(Native.GetForegroundWindow(), out pid);
                string name = Process.GetProcessById((int)pid).ProcessName + ".exe";
                foreach (string item in Settings.ExcludedApps.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    if (string.Equals(item.Trim(), name, StringComparison.OrdinalIgnoreCase) || string.Equals(item.Trim(), Path.GetFileNameWithoutExtension(name), StringComparison.OrdinalIgnoreCase)) return true;
            }
            catch { }
            return false;
        }

        internal void Preview()
        {
            Point center = settingsForm != null && settingsForm.Visible ? new Point(settingsForm.Left + settingsForm.Width / 2, settingsForm.Top + 90) : Cursor.Position;
            EffectForm target = pool.Find(delegate(EffectForm f) { return !f.Active; }); if (target == null) target = pool[0];
            target.Play(center, ColorTranslator.FromHtml(Settings.LeftColor), Settings.LeftShape, Settings, Settings.Waves, false, false);
        }

        private void CheckHolds()
        {
            if (!Settings.HoldEffects || mouseHook == null) return;
            List<KeyValuePair<MouseButtons, DateTime>> held;
            lock (mouseHook.DownButtons) held = new List<KeyValuePair<MouseButtons, DateTime>>(mouseHook.DownButtons);
            foreach (KeyValuePair<MouseButtons, DateTime> pair in held)
            {
                DateTime now = DateTime.UtcNow; DateTime last;
                if ((now - pair.Value).TotalMilliseconds < 500) continue;
                if (lastHoldPulse.TryGetValue(pair.Key, out last) && (now - last).TotalMilliseconds < 420) continue;
                lastHoldPulse[pair.Key] = now;
                ProcessClick(new ClickEvent { Button = pair.Key, Point = Cursor.Position, IsHold = true });
            }
        }

        private void UpdateEnvironment()
        {
            Native.FILETIME idle, kernel, user;
            if (Native.GetSystemTimes(out idle, out kernel, out user))
            {
                ulong i = Native.ToUInt64(idle), k = Native.ToUInt64(kernel), u = Native.ToUInt64(user);
                ulong total = (k - oldKernel) + (u - oldUser), idleDelta = i - oldIdle;
                if (oldKernel != 0 && total > 0) systemCpu = 100.0 * (total - idleDelta) / total;
                oldIdle = i; oldKernel = k; oldUser = u;
            }
            bool full = Settings.PauseFullscreen && IsFullscreenForeground();
            bool busy = Settings.PauseHighCpu && systemCpu >= Settings.HighCpuPercent;
            pausedByEnvironment = full || busy;
            if (pausedByEnvironment || !Settings.Enabled || !Settings.CursorGlow) cursorGlowForm.HideCursorGlow();
            using (Process process = Process.GetCurrentProcess())
            {
                DateTime now = DateTime.UtcNow;
                TimeSpan processCpu = process.TotalProcessorTime;
                double elapsed = (now - oldProcessSample).TotalSeconds;
                double appCpu = elapsed > 0 ? 100.0 * (processCpu - oldProcessCpu).TotalSeconds / (elapsed * Environment.ProcessorCount) : 0;
                oldProcessCpu = processCpu; oldProcessSample = now;
                statsItem.Text = string.Format("CPU: {0:0.0}% · система: {1:0}% · RAM: {2:0} МБ{3}", appCpu, systemCpu, process.WorkingSet64 / 1048576.0, pausedByEnvironment ? " · пауза" : "");
            }
            tray.Text = pausedByEnvironment ? "CursorClick — временно приостановлен" : Settings.Enabled ? "CursorClick — включён" : "CursorClick — выключен";
        }

        private bool IsFullscreenForeground()
        {
            IntPtr hwnd = Native.GetForegroundWindow(); if (hwnd == IntPtr.Zero || hwnd == Handle) return false;
            StringBuilder className = new StringBuilder(256);
            Native.GetClassName(hwnd, className, className.Capacity);
            string windowClass = className.ToString();
            // Windows desktop surfaces cover an entire monitor, but they are not
            // fullscreen applications and must never pause cursor effects.
            if (string.Equals(windowClass, "Progman", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(windowClass, "WorkerW", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(windowClass, "Shell_TrayWnd", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(windowClass, "Shell_SecondaryTrayWnd", StringComparison.OrdinalIgnoreCase)) return false;
            Native.RECT window; if (!Native.GetWindowRect(hwnd, out window)) return false;
            IntPtr monitor = Native.MonitorFromWindow(hwnd, Native.MONITOR_DEFAULTTONEAREST);
            Native.MONITORINFO info = new Native.MONITORINFO(); info.Size = Marshal.SizeOf(info);
            if (!Native.GetMonitorInfo(monitor, ref info)) return false;
            return window.Left <= info.Monitor.Left && window.Top <= info.Monitor.Top && window.Right >= info.Monitor.Right && window.Bottom >= info.Monitor.Bottom;
        }

        private void ShowSettings()
        {
            if (settingsForm == null || settingsForm.IsDisposed) settingsForm = new SettingsForm(this);
            settingsForm.LoadValues(); settingsForm.Show(); settingsForm.WindowState = FormWindowState.Normal; settingsForm.Activate();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY) { Settings.Enabled = !Settings.Enabled; SaveAndApply(); tray.ShowBalloonTip(1200, "CursorClick", Settings.Enabled ? "Эффекты включены" : "Эффекты выключены", ToolTipIcon.Info); }
            else if (m.Msg == Native.WM_APP_SHOW) ShowSettings();
            base.WndProc(ref m);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            statusTimer.Stop(); holdTimer.Stop(); if (mouseHook != null) mouseHook.Dispose(); Native.UnregisterHotKey(Handle, 1);
            foreach (EffectForm form in pool) form.Dispose(); cursorGlowForm.Dispose(); tray.Visible = false; tray.Dispose(); base.OnFormClosed(e);
        }
    }

    internal static class Program
    {
        private static readonly string CrashLog = Path.Combine(SettingsStore.DirectoryPath, "crash.log");
        private static void LogCrash(Exception error)
        {
            try { Directory.CreateDirectory(SettingsStore.DirectoryPath); File.AppendAllText(CrashLog, DateTime.Now.ToString("s") + " " + error + Environment.NewLine); } catch { }
        }

        [STAThread]
        private static void Main()
        {
            try { if (!Native.SetProcessDpiAwarenessContext(new IntPtr(-4))) Native.SetProcessDPIAware(); }
            catch (EntryPointNotFoundException) { Native.SetProcessDPIAware(); }
            bool created;
            using (Mutex mutex = new Mutex(true, "Local\\CursorClick.Native.SingleInstance", out created))
            {
                bool ownsMutex = created;
                if (!created)
                {
                    IntPtr existing = Native.FindWindow(null, "CursorClick.Control." + Environment.UserName);
                    if (existing != IntPtr.Zero) { Native.PostMessage(existing, Native.WM_APP_SHOW, IntPtr.Zero, IntPtr.Zero); return; }
                    // A forcibly closed or updating instance may leave the named
                    // mutex signalled a moment after its window has disappeared.
                    try { ownsMutex = mutex.WaitOne(3000); }
                    catch (AbandonedMutexException) { ownsMutex = true; }
                    if (!ownsMutex) return;
                }
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e) { LogCrash(e.Exception); MessageBox.Show("CursorClick восстановится после ошибки. Журнал: " + CrashLog, "CursorClick"); Application.Restart(); };
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { LogCrash(e.ExceptionObject as Exception ?? new Exception("Unknown fatal error")); };
                try { Application.Run(new MainForm()); }
                finally { if (ownsMutex) try { mutex.ReleaseMutex(); } catch { } }
            }
        }
    }
}
