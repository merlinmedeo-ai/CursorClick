param(
    [switch]$DebugVisible,
    [switch]$EnableGlow,
    [ValidateRange(1, 6)][int]$WaveSlotCount = 3
)

$ErrorActionPreference = 'Stop'
$script:LogFile = Join-Path $env:TEMP 'VolantesCursorClick.log'
$script:ReadyFile = Join-Path $env:TEMP 'VolantesCursorClick.ready'
$script:PidFile = Join-Path $env:TEMP 'VolantesCursorClick.pid'

function Write-ClickLog([string]$Text) {
    try {
        $line = (Get-Date).ToString('s') + ' ' + $Text
        Add-Content -LiteralPath $script:LogFile -Value $line -Encoding ASCII
        if ($DebugVisible) { Write-Host $line }
    } catch {}
}

try {
    Set-Content -LiteralPath $script:LogFile -Value ((Get-Date).ToString('s') + ' START CursorClick v7 event-optimized FIXED') -Encoding ASCII
    Set-Content -LiteralPath $script:PidFile -Value $PID -Encoding ASCII
    Remove-Item -LiteralPath $script:ReadyFile -Force -ErrorAction SilentlyContinue

    if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') {
        throw 'PowerShell must be started with -STA.'
    }

    Add-Type -AssemblyName PresentationCore
    Add-Type -AssemblyName PresentationFramework
    Add-Type -AssemblyName WindowsBase

    $nativeSource = @'
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class VolantesClickNative {
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const int WH_MOUSE_LL = 14;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_APP_CLICK = 0x8001;
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    private struct ClickEvent {
        public int Button;
        public int X;
        public int Y;
        public ClickEvent(int button, int x, int y) { Button = button; X = x; Y = y; }
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    private static readonly LowLevelMouseProc _proc = HookCallback;
    private static IntPtr _hook = IntPtr.Zero;
    private static IntPtr _targetHwnd = IntPtr.Zero;
    private static readonly object _sync = new object();
    private static readonly Queue<ClickEvent> _clicks = new Queue<ClickEvent>();

    [DllImport("user32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError=true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll", SetLastError=true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError=true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError=true)]
    private static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public static void MakeClickThrough(IntPtr hwnd) {
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        ex |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLong(hwnd, GWL_EXSTYLE, ex);
    }

    public static bool InstallMouseHook(IntPtr targetHwnd) {
        if (_hook != IntPtr.Zero) return true;
        _targetHwnd = targetHwnd;
        IntPtr hMod = IntPtr.Zero;
        try {
            using (Process p = Process.GetCurrentProcess()) {
                hMod = GetModuleHandle(p.MainModule.ModuleName);
            }
        } catch { hMod = IntPtr.Zero; }
        _hook = SetWindowsHookEx(WH_MOUSE_LL, _proc, hMod, 0);
        return _hook != IntPtr.Zero;
    }

    public static void UninstallMouseHook() {
        if (_hook != IntPtr.Zero) {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }

    public static bool TryDequeue(out int button, out int x, out int y) {
        lock (_sync) {
            if (_clicks.Count == 0) { button = x = y = 0; return false; }
            ClickEvent e = _clicks.Dequeue();
            button = e.Button; x = e.X; y = e.Y; return true;
        }
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
        if (nCode >= 0) {
            int msg = wParam.ToInt32();
            int button = 0;
            if (msg == WM_LBUTTONDOWN) button = 1;
            else if (msg == WM_RBUTTONDOWN) button = 2;

            if (button != 0 && _targetHwnd != IntPtr.Zero) {
                MSLLHOOKSTRUCT data = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                lock (_sync) {
                    if (_clicks.Count >= 32) _clicks.Dequeue();
                    _clicks.Enqueue(new ClickEvent(button, data.pt.X, data.pt.Y));
                }
                PostMessage(_targetHwnd, WM_APP_CLICK, IntPtr.Zero, IntPtr.Zero);
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }
}
'@
    Add-Type -TypeDefinition $nativeSource -Language CSharp

    $script:Stopwatch = [Diagnostics.Stopwatch]::StartNew()
    $script:WaveSlots = New-Object System.Collections.ArrayList

    function New-ColorBrush([byte]$A,[byte]$R,[byte]$G,[byte]$B) {
        $c = [Windows.Media.Color]::FromArgb($A,$R,$G,$B)
        # Avoid constructor-overload ambiguity in Windows PowerShell 5.1.
        $brush = New-Object Windows.Media.SolidColorBrush
        $brush.Color = $c
        if ($brush.CanFreeze) { $brush.Freeze() }
        return $brush
    }

    $script:BlueBrush1 = New-ColorBrush 255 22 139 255
    $script:BlueBrush2 = New-ColorBrush 230 22 139 255
    $script:RedBrush1  = New-ColorBrush 255 255 55 55
    $script:RedBrush2  = New-ColorBrush 230 255 55 55
    $script:BlueColor = [Windows.Media.Color]::FromRgb(22,139,255)
    $script:RedColor  = [Windows.Media.Color]::FromRgb(255,55,55)

    function Set-WindowClickThrough($Window) {
        $helper = New-Object Windows.Interop.WindowInteropHelper -ArgumentList $Window
        $hwnd = $helper.EnsureHandle()
        [VolantesClickNative]::MakeClickThrough($hwnd)
        return $hwnd
    }

    function Position-WindowPhysical([IntPtr]$Hwnd, [int]$CenterX, [int]$CenterY) {
        $rect = New-Object 'VolantesClickNative+RECT'
        if ([VolantesClickNative]::GetWindowRect($Hwnd, [ref]$rect)) {
            $w = [Math]::Max(1, $rect.Right - $rect.Left)
            $h = [Math]::Max(1, $rect.Bottom - $rect.Top)
            $x = $CenterX - [int]($w / 2)
            $y = $CenterY - [int]($h / 2)
            [void][VolantesClickNative]::SetWindowPos($Hwnd, [VolantesClickNative]::HWND_TOPMOST, $x, $y, 0, 0, [VolantesClickNative]::SWP_NOSIZE -bor [VolantesClickNative]::SWP_NOACTIVATE)
        }
    }

    function Move-Offscreen([IntPtr]$Hwnd) {
        [void][VolantesClickNative]::SetWindowPos($Hwnd, [VolantesClickNative]::HWND_TOPMOST, -32000, -32000, 0, 0, [VolantesClickNative]::SWP_NOSIZE -bor [VolantesClickNative]::SWP_NOACTIVATE)
    }

    function Clear-WaveAnimations($slot) {
        $slot.Scale1.BeginAnimation([Windows.Media.ScaleTransform]::ScaleXProperty, $null)
        $slot.Scale1.BeginAnimation([Windows.Media.ScaleTransform]::ScaleYProperty, $null)
        $slot.Scale2.BeginAnimation([Windows.Media.ScaleTransform]::ScaleXProperty, $null)
        $slot.Scale2.BeginAnimation([Windows.Media.ScaleTransform]::ScaleYProperty, $null)
        $slot.Ring1.BeginAnimation([Windows.UIElement]::OpacityProperty, $null)
        $slot.Ring2.BeginAnimation([Windows.UIElement]::OpacityProperty, $null)
    }

    function New-WaveSlot([int]$Index) {
        $win = New-Object Windows.Window
        $win.Width = 120
        $win.Height = 120
        $win.Left = -32000
        $win.Top = -32000
        $win.WindowStyle = [Windows.WindowStyle]::None
        $win.ResizeMode = [Windows.ResizeMode]::NoResize
        $win.AllowsTransparency = $true
        $win.Background = [Windows.Media.Brushes]::Transparent
        $win.ShowInTaskbar = $false
        $win.Topmost = $true
        $win.ShowActivated = $false
        $win.Focusable = $false

        $canvas = New-Object Windows.Controls.Canvas
        $canvas.IsHitTestVisible = $false
        $win.Content = $canvas

        $ring1 = New-Object Windows.Shapes.Ellipse
        $ring1.Width = 70; $ring1.Height = 70
        $ring1.Stroke = $script:BlueBrush1
        $ring1.StrokeThickness = 2.8
        $ring1.Opacity = 0
        $origin1 = New-Object Windows.Point
        $origin1.X = 0.5; $origin1.Y = 0.5
        $ring1.RenderTransformOrigin = $origin1
        $scale1 = New-Object Windows.Media.ScaleTransform
        $scale1.ScaleX = 0.22; $scale1.ScaleY = 0.22
        $ring1.RenderTransform = $scale1
        $fx1 = $null
        if ($EnableGlow) {
            $fx1 = New-Object Windows.Media.Effects.DropShadowEffect
            $fx1.Color = $script:BlueColor; $fx1.BlurRadius = 9; $fx1.ShadowDepth = 0; $fx1.Opacity = 0.85
            $ring1.Effect = $fx1
        }
        [Windows.Controls.Canvas]::SetLeft($ring1,25)
        [Windows.Controls.Canvas]::SetTop($ring1,25)
        [void]$canvas.Children.Add($ring1)

        $ring2 = New-Object Windows.Shapes.Ellipse
        $ring2.Width = 70; $ring2.Height = 70
        $ring2.Stroke = $script:BlueBrush2
        $ring2.StrokeThickness = 2.5
        $ring2.Opacity = 0
        $origin2 = New-Object Windows.Point
        $origin2.X = 0.5; $origin2.Y = 0.5
        $ring2.RenderTransformOrigin = $origin2
        $scale2 = New-Object Windows.Media.ScaleTransform
        $scale2.ScaleX = 0.22; $scale2.ScaleY = 0.22
        $ring2.RenderTransform = $scale2
        $fx2 = $null
        if ($EnableGlow) {
            $fx2 = New-Object Windows.Media.Effects.DropShadowEffect
            $fx2.Color = $script:BlueColor; $fx2.BlurRadius = 8; $fx2.ShadowDepth = 0; $fx2.Opacity = 0.72
            $ring2.Effect = $fx2
        }
        [Windows.Controls.Canvas]::SetLeft($ring2,25)
        [Windows.Controls.Canvas]::SetTop($ring2,25)
        [void]$canvas.Children.Add($ring2)

        $win.Show()
        $hwnd = Set-WindowClickThrough $win
        Move-Offscreen $hwnd

        $slot = [PSCustomObject]@{
            Index=$Index; Window=$win; Hwnd=$hwnd;
            Ring1=$ring1; Scale1=$scale1; Fx1=$fx1;
            Ring2=$ring2; Scale2=$scale2; Fx2=$fx2;
            Busy=$false; LastStart=0.0; Timer=$null;
            Scale1Animation=$null; Opacity1Animation=$null;
            Scale2Animation=$null; Opacity2Animation=$null
        }

        $timer = New-Object Windows.Threading.DispatcherTimer
        $timer.Interval = [TimeSpan]::FromMilliseconds(620)
        $slot.Timer = $timer
        $timer.Add_Tick({
            $slot.Timer.Stop()
            Clear-WaveAnimations $slot
            $slot.Ring1.Opacity = 0
            $slot.Ring2.Opacity = 0
            $slot.Busy = $false
            Move-Offscreen $slot.Hwnd
        }.GetNewClosure())

        return $slot
    }

    function New-ScaleAnimation([double]$From,[double]$To,[double]$DurationMs,[double]$DelayMs) {
        $a = New-Object Windows.Media.Animation.DoubleAnimation
        $a.From = $From
        $a.To = $To
        $a.Duration = New-Object Windows.Duration ([TimeSpan]::FromMilliseconds($DurationMs))
        if ($DelayMs -gt 0) { $a.BeginTime = [TimeSpan]::FromMilliseconds($DelayMs) }
        $ease = New-Object Windows.Media.Animation.CubicEase
        $ease.EasingMode = [Windows.Media.Animation.EasingMode]::EaseOut
        $a.EasingFunction = $ease
        return $a
    }

    function New-OpacityAnimation([double]$From,[double]$To,[double]$DurationMs,[double]$DelayMs) {
        $a = New-Object Windows.Media.Animation.DoubleAnimation
        $a.From = $From
        $a.To = $To
        $a.Duration = New-Object Windows.Duration ([TimeSpan]::FromMilliseconds($DurationMs))
        if ($DelayMs -gt 0) { $a.BeginTime = [TimeSpan]::FromMilliseconds($DelayMs) }
        $ease = New-Object Windows.Media.Animation.PowerEase
        $ease.Power = 1.15
        $ease.EasingMode = [Windows.Media.Animation.EasingMode]::EaseOut
        $a.EasingFunction = $ease
        return $a
    }

    function Initialize-WaveAnimations($slot) {
        # Animation timelines are immutable templates. WPF creates a separate clock
        # for every BeginAnimation call, so the same four objects can be reused.
        $slot.Scale1Animation = New-ScaleAnimation 0.22 1.10 440 0
        $slot.Opacity1Animation = New-OpacityAnimation 0.98 0.0 440 0
        $slot.Scale2Animation = New-ScaleAnimation 0.22 1.10 440 130
        $slot.Opacity2Animation = New-OpacityAnimation 0.80 0.0 440 130
        foreach ($animation in @(
            $slot.Scale1Animation, $slot.Opacity1Animation,
            $slot.Scale2Animation, $slot.Opacity2Animation
        )) {
            if ($animation.CanFreeze) { $animation.Freeze() }
        }
    }

    function Start-ClickWave([int]$X,[int]$Y,[int]$Button) {
        $slot = $null
        foreach ($candidate in $script:WaveSlots) {
            if (-not $candidate.Busy) { $slot = $candidate; break }
        }
        if ($null -eq $slot) {
            $slot = $script:WaveSlots | Sort-Object LastStart | Select-Object -First 1
            $slot.Timer.Stop()
            Clear-WaveAnimations $slot
        }

        $slot.Busy = $true
        $slot.LastStart = [double]$script:Stopwatch.Elapsed.TotalMilliseconds

        if ($Button -eq 2) {
            $slot.Ring1.Stroke = $script:RedBrush1
            $slot.Ring2.Stroke = $script:RedBrush2
            if ($null -ne $slot.Fx1) { $slot.Fx1.Color = $script:RedColor }
            if ($null -ne $slot.Fx2) { $slot.Fx2.Color = $script:RedColor }
        } else {
            $slot.Ring1.Stroke = $script:BlueBrush1
            $slot.Ring2.Stroke = $script:BlueBrush2
            if ($null -ne $slot.Fx1) { $slot.Fx1.Color = $script:BlueColor }
            if ($null -ne $slot.Fx2) { $slot.Fx2.Color = $script:BlueColor }
        }

        Clear-WaveAnimations $slot
        $slot.Scale1.ScaleX = 0.22; $slot.Scale1.ScaleY = 0.22
        $slot.Scale2.ScaleX = 0.22; $slot.Scale2.ScaleY = 0.22
        $slot.Ring1.Opacity = 0
        $slot.Ring2.Opacity = 0
        Position-WindowPhysical $slot.Hwnd $X $Y

        $slot.Scale1.BeginAnimation([Windows.Media.ScaleTransform]::ScaleXProperty, $slot.Scale1Animation)
        $slot.Scale1.BeginAnimation([Windows.Media.ScaleTransform]::ScaleYProperty, $slot.Scale1Animation)
        $slot.Ring1.BeginAnimation([Windows.UIElement]::OpacityProperty, $slot.Opacity1Animation)
        $slot.Scale2.BeginAnimation([Windows.Media.ScaleTransform]::ScaleXProperty, $slot.Scale2Animation)
        $slot.Scale2.BeginAnimation([Windows.Media.ScaleTransform]::ScaleYProperty, $slot.Scale2Animation)
        $slot.Ring2.BeginAnimation([Windows.UIElement]::OpacityProperty, $slot.Opacity2Animation)

        $slot.Timer.Stop()
        $slot.Timer.Start()
    }

    # Pre-create all tiny transparent windows once. No WPF Window is created at click time.
    for ($i = 0; $i -lt $WaveSlotCount; $i++) {
        $slot = New-WaveSlot $i
        Initialize-WaveAnimations $slot
        [void]$script:WaveSlots.Add($slot)
    }

    # Hidden signal window: the low-level hook posts a tiny custom message here.
    $script:SignalWindow = New-Object Windows.Window
    $script:SignalWindow.Width = 1
    $script:SignalWindow.Height = 1
    $script:SignalWindow.Left = -32000
    $script:SignalWindow.Top = -32000
    $script:SignalWindow.WindowStyle = [Windows.WindowStyle]::None
    $script:SignalWindow.ResizeMode = [Windows.ResizeMode]::NoResize
    $script:SignalWindow.AllowsTransparency = $true
    $script:SignalWindow.Background = [Windows.Media.Brushes]::Transparent
    $script:SignalWindow.Opacity = 0
    $script:SignalWindow.ShowInTaskbar = $false
    $script:SignalWindow.ShowActivated = $false
    $script:SignalWindow.Focusable = $false
    $script:SignalWindow.Show()
    $signalHelper = New-Object Windows.Interop.WindowInteropHelper -ArgumentList $script:SignalWindow
    $script:SignalHwnd = $signalHelper.EnsureHandle()
    [VolantesClickNative]::MakeClickThrough($script:SignalHwnd)
    Move-Offscreen $script:SignalHwnd

    $script:HwndSource = [Windows.Interop.HwndSource]::FromHwnd($script:SignalHwnd)
    $script:MessageHook = [Windows.Interop.HwndSourceHook] {
        param([IntPtr]$hwnd, [int]$msg, [IntPtr]$wParam, [IntPtr]$lParam, [ref]$handled)
        if ($msg -eq [VolantesClickNative]::WM_APP_CLICK) {
            $button = 0; $x = 0; $y = 0
            while ([VolantesClickNative]::TryDequeue([ref]$button, [ref]$x, [ref]$y)) {
                Start-ClickWave $x $y $button
            }
            $handled.Value = $true
        }
        return [IntPtr]::Zero
    }
    $script:HwndSource.AddHook($script:MessageHook)

    if (-not [VolantesClickNative]::InstallMouseHook($script:SignalHwnd)) {
        throw 'Could not install WH_MOUSE_LL hook.'
    }

    Set-Content -LiteralPath $script:ReadyFile -Value ('READY PID=' + $PID) -Encoding ASCII
    Write-ClickLog ("READY event-driven; slots=$WaveSlotCount; glow=$EnableGlow; no polling timer")
    if ($DebugVisible) {
        Write-Host ''
        Write-Host 'READY. EVENT-DRIVEN MODE.' -ForegroundColor Green
        Write-Host 'LEFT CLICK = BLUE, RIGHT CLICK = RED.'
        Write-Host 'No 12 ms polling loop. Close this window to stop.'
    }

    [Windows.Threading.Dispatcher]::Run()
}
catch {
    Write-ClickLog ('FATAL ERROR: ' + ($_ | Out-String))
    if ($DebugVisible) {
        Write-Host ''
        Write-Host 'FATAL ERROR:' -ForegroundColor Red
        Write-Host ($_ | Out-String)
        Read-Host 'Press Enter to close'
    }
    exit 1
}
finally {
    try { [VolantesClickNative]::UninstallMouseHook() } catch {}
    try {
        if ($script:HwndSource -and $script:MessageHook) { $script:HwndSource.RemoveHook($script:MessageHook) }
    } catch {}
    try {
        foreach ($slot in $script:WaveSlots) { $slot.Timer.Stop(); $slot.Window.Close() }
    } catch {}
    try { if ($script:SignalWindow) { $script:SignalWindow.Close() } } catch {}
    try { Remove-Item -LiteralPath $script:ReadyFile -Force -ErrorAction SilentlyContinue } catch {}
    try { Remove-Item -LiteralPath $script:PidFile -Force -ErrorAction SilentlyContinue } catch {}
}
