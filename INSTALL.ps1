$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'CursorClick.exe'
if (-not (Test-Path $source)) { throw 'CursorClick.exe was not found next to INSTALL.ps1.' }
$installDir = Join-Path $env:LOCALAPPDATA 'CursorClick'
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Get-Process CursorClick -ErrorAction SilentlyContinue | Stop-Process -Force
Copy-Item -LiteralPath $source -Destination (Join-Path $installDir 'CursorClick.exe') -Force
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Programs')) 'CursorClick.lnk'))
$shortcut.TargetPath = Join-Path $installDir 'CursorClick.exe'
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = (Join-Path $installDir 'CursorClick.exe') + ',0'
$shortcut.Save()
Start-Process -FilePath (Join-Path $installDir 'CursorClick.exe')
Write-Host "Installed to $installDir" -ForegroundColor Green
