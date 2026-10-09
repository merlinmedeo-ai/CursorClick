param([switch]$DebugVisible)

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
    Set-Content -LiteralPath $script:LogFile -Value ((Get-Date).ToString('s') + ' START CursorClick v7') -Encoding ASCII
    Set-Content -LiteralPath $script:PidFile -Value $PID -Encoding ASCII
    Remove-Item -LiteralPath $script:ReadyFile -Force -ErrorAction SilentlyContinue

    if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') {
        throw 'PowerShell must be started with -STA.'
    }

    Add-Type -AssemblyName PresentationCore
    Add-Type -AssemblyName PresentationFramework
    Add-Type -AssemblyName WindowsBase
    Add-Type -AssemblyName System.Windows.Forms

    $nativeSource = @'
using System;
using System.Runtime.InteropServices;
public static class VolantesClickNative {
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOACTIVATE = 0x0010;
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError=true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    public static void MakeClickThrough(IntPtr hwnd) {
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        ex |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLong(hwnd, GWL_EXSTYLE, ex);
    }
}
'@
    Add-Type -TypeDefinition $nativeSource -Language CSharp

    $script:Stopwatch = [Diagnostics.Stopwatch]::StartNew()
    $script:Waves = New-Object System.Collections.ArrayList
    $script:PrevLeftDown = $false
    $script:PrevRightDown = $false

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

    function New-ColorBrush([byte]$A,[byte]$R,[byte]$G,[byte]$B) {
        $c = [Windows.Media.Color]::FromArgb($A,$R,$G,$B)
        return New-Object Windows.Media.SolidColorBrush($c)
    }

    function New-ClickWave([int]$X,[int]$Y,[byte]$R,[byte]$G,[byte]$B) {
        $win = New-Object Windows.Window
        $win.Width = 120
        $win.Height = 120
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

        $color = [Windows.Media.Color]::FromRgb($R,$G,$B)

        $ring1 = New-Object Windows.Shapes.Ellipse
        $ring1.Width = 70; $ring1.Height = 70
        $ring1.Stroke = New-ColorBrush 255 $R $G $B
        $ring1.StrokeThickness = 2.8
        $ring1.Opacity = 0
        $ring1.RenderTransformOrigin = New-Object Windows.Point(0.5,0.5)
        $scale1 = New-Object Windows.Media.ScaleTransform(0.22,0.22)
        $ring1.RenderTransform = $scale1
        $fx1 = New-Object Windows.Media.Effects.DropShadowEffect
        $fx1.Color = $color; $fx1.BlurRadius = 9; $fx1.ShadowDepth = 0; $fx1.Opacity = 0.85
        $ring1.Effect = $fx1
        [Windows.Controls.Canvas]::SetLeft($ring1,25)
        [Windows.Controls.Canvas]::SetTop($ring1,25)
        [void]$canvas.Children.Add($ring1)

        $ring2 = New-Object Windows.Shapes.Ellipse
        $ring2.Width = 70; $ring2.Height = 70
        $ring2.Stroke = New-ColorBrush 230 $R $G $B
        $ring2.StrokeThickness = 2.5
        $ring2.Opacity = 0
        $ring2.RenderTransformOrigin = New-Object Windows.Point(0.5,0.5)
        $scale2 = New-Object Windows.Media.ScaleTransform(0.22,0.22)
        $ring2.RenderTransform = $scale2
        $fx2 = New-Object Windows.Media.Effects.DropShadowEffect
        $fx2.Color = $color; $fx2.BlurRadius = 8; $fx2.ShadowDepth = 0; $fx2.Opacity = 0.72
        $ring2.Effect = $fx2
        [Windows.Controls.Canvas]::SetLeft($ring2,25)
        [Windows.Controls.Canvas]::SetTop($ring2,25)
        [void]$canvas.Children.Add($ring2)

        $win.Show()
        $hwnd = Set-WindowClickThrough $win
        Position-WindowPhysical $hwnd $X $Y

        $obj = [PSCustomObject]@{
            Window=$win; Hwnd=$hwnd;
            Ring1=$ring1; Scale1=$scale1;
            Ring2=$ring2; Scale2=$scale2;
            Start=[double]$script:Stopwatch.Elapsed.TotalMilliseconds
        }
        [void]$script:Waves.Add($obj)
    }

    $timer = New-Object Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromMilliseconds(12)
    $timer.Add_Tick({
        try {
            $now = [double]$script:Stopwatch.Elapsed.TotalMilliseconds
            $pos = [System.Windows.Forms.Cursor]::Position

            $leftDown = (([VolantesClickNative]::GetAsyncKeyState(1) -band 0x8000) -ne 0)
            if ($leftDown -and -not $script:PrevLeftDown) {
                New-ClickWave $pos.X $pos.Y 22 139 255
            }
            $script:PrevLeftDown = $leftDown

            $rightDown = (([VolantesClickNative]::GetAsyncKeyState(2) -band 0x8000) -ne 0)
            if ($rightDown -and -not $script:PrevRightDown) {
                New-ClickWave $pos.X $pos.Y 255 55 55
            }
            $script:PrevRightDown = $rightDown

            for ($i = $script:Waves.Count - 1; $i -ge 0; $i--) {
                $w = $script:Waves[$i]
                $age = $now - $w.Start

                $p1 = [Math]::Max(0.0,[Math]::Min(1.0,$age / 440.0))
                $s1 = 0.22 + (0.88 * (1.0 - [Math]::Pow(1.0-$p1,3)))
                $w.Scale1.ScaleX=$s1; $w.Scale1.ScaleY=$s1
                $w.Ring1.Opacity = if ($age -ge 0 -and $age -le 440) { 0.98 * [Math]::Pow(1.0-$p1,1.15) } else { 0 }

                $age2 = $age - 130.0
                if ($age2 -ge 0) {
                    $p2 = [Math]::Max(0.0,[Math]::Min(1.0,$age2 / 440.0))
                    $s2 = 0.22 + (0.88 * (1.0 - [Math]::Pow(1.0-$p2,3)))
                    $w.Scale2.ScaleX=$s2; $w.Scale2.ScaleY=$s2
                    $w.Ring2.Opacity = if ($age2 -le 440) { 0.80 * [Math]::Pow(1.0-$p2,1.15) } else { 0 }
                } else {
                    $w.Ring2.Opacity = 0
                }

                if ($age -gt 620) {
                    $w.Window.Close()
                    $script:Waves.RemoveAt($i)
                }
            }
        } catch {
            Write-ClickLog ('TICK ERROR: ' + ($_ | Out-String))
        }
    })
    $timer.Start()

    Set-Content -LiteralPath $script:ReadyFile -Value ('READY PID=' + $PID) -Encoding ASCII
    Write-ClickLog 'READY - left click blue, right click red'
    if ($DebugVisible) {
        Write-Host ''
        Write-Host 'READY. LEFT CLICK = BLUE, RIGHT CLICK = RED.' -ForegroundColor Green
        Write-Host 'Close this window to stop the effect.'
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
    try { Remove-Item -LiteralPath $script:ReadyFile -Force -ErrorAction SilentlyContinue } catch {}
    try { Remove-Item -LiteralPath $script:PidFile -Force -ErrorAction SilentlyContinue } catch {}
}
