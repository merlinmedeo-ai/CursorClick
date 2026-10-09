$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $compiler)) { throw 'The .NET Framework C# compiler was not found.' }

$iconPath = Join-Path $root 'CursorClick.ico'
Add-Type -AssemblyName System.Drawing
$bitmap = New-Object Drawing.Bitmap 64,64
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([Drawing.Color]::Transparent)
$pen1 = New-Object Drawing.Pen ([Drawing.Color]::DodgerBlue),7
$pen2 = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(190,80,190,255)),4
$graphics.DrawEllipse($pen1,5,5,53,53)
$graphics.DrawEllipse($pen2,18,18,27,27)
$icon = [Drawing.Icon]::FromHandle($bitmap.GetHicon()).Clone()
$stream = [IO.File]::Create($iconPath)
$icon.Save($stream)
$stream.Dispose(); $icon.Dispose(); $pen1.Dispose(); $pen2.Dispose(); $graphics.Dispose(); $bitmap.Dispose()

& $compiler /nologo /target:winexe /optimize+ /platform:anycpu /win32icon:"$iconPath" /out:"$root\CursorClick.exe" /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll "$root\CursorClickApp.cs"
if ($LASTEXITCODE -ne 0) { throw "Compilation failed with exit code $LASTEXITCODE" }
Write-Host "Built: $root\CursorClick.exe" -ForegroundColor Green
