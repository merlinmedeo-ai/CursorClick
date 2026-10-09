$ErrorActionPreference = 'SilentlyContinue'
Get-Process CursorClick | Stop-Process -Force
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'CursorClick' -Force
Remove-Item -LiteralPath (Join-Path ([Environment]::GetFolderPath('Programs')) 'CursorClick.lnk') -Force
$installDir = Join-Path $env:LOCALAPPDATA 'CursorClick'
if (Test-Path $installDir) { Remove-Item -LiteralPath $installDir -Recurse -Force }
Write-Host 'CursorClick uninstalled. User settings in AppData\Roaming were preserved.'
