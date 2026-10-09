@echo off
setlocal
set "PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
echo Searching for old Volantes CursorClick.ps1 processes...
taskkill /IM CursorClick.exe /T /F >nul 2>nul
"%PS%" -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "$me=$PID; Get-CimInstance Win32_Process ^| Where-Object { $_.ProcessId -ne $me -and $_.CommandLine -like '*CursorClick.ps1*' } ^| ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue; Write-Host ('Stopped PID ' + $_.ProcessId) }"
del /q "%TEMP%\VolantesCursorClick.pid" 2>nul
del /q "%TEMP%\VolantesCursorClick.ready" 2>nul
echo Done.
pause
