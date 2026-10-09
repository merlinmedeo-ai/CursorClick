@echo off
setlocal
set "DIR=%~dp0"
set "PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
call "%DIR%01_STOP.bat" silent >nul 2>nul
cls
echo ==============================================
echo        VISIBLE DEBUG - EVENT OPTIMIZED
echo ==============================================
echo.
echo This window stays open.
echo LEFT CLICK = BLUE, RIGHT CLICK = RED.
echo Press Ctrl+C or close this window to stop.
echo.
"%PS%" -NoLogo -NoProfile -ExecutionPolicy Bypass -STA -File "%DIR%CursorClick.ps1" -DebugVisible
pause
