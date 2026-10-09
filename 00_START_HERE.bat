@echo off
setlocal
set "DIR=%~dp0"
set "LOG=%TEMP%\VolantesCursorClick.log"
set "READY=%TEMP%\VolantesCursorClick.ready"
set "PIDFILE=%TEMP%\VolantesCursorClick.pid"
set "PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"

title Volantes Click Effects v7
cls
echo ==============================================
echo        VOLANTES CLICK EFFECTS v7 EVENT OPTIMIZED
echo ==============================================
echo.
echo LEFT CLICK  = BLUE WAVES
echo RIGHT CLICK = RED WAVES
echo No polling loop. WPF visual preserved.
echo.

if not exist "%PS%" goto :nops
if exist "%DIR%CursorClick.exe" goto :native
if not exist "%DIR%CursorClick.ps1" goto :noscript

call "%DIR%01_STOP.bat" silent >nul 2>nul
del /q "%LOG%" 2>nul
del /q "%READY%" 2>nul
del /q "%PIDFILE%" 2>nul

start "VolantesCursorClick" /min "%PS%" -NoLogo -NoProfile -ExecutionPolicy Bypass -STA -WindowStyle Hidden -File "%DIR%CursorClick.ps1"

echo Starting click effects...
for /L %%I in (1,1,50) do (
  if exist "%READY%" goto :ok
  if exist "%LOG%" findstr /C:"FATAL ERROR" "%LOG%" >nul 2>nul && goto :fail
  >nul ping 127.0.0.1 -n 1 -w 200
)
goto :fail

:ok
echo.
echo [OK] Click effects are running.
echo.
echo Test now:
echo   LEFT CLICK  - two BLUE rings
echo   RIGHT CLICK - two RED rings
echo.
echo There should be NO effect while only moving the mouse.
pause
exit /b 0

:native
start "" "%DIR%CursorClick.exe"
echo CursorClick started. Use the tray icon for settings.
timeout /t 2 /nobreak >nul
exit /b 0

:fail
echo.
echo [FAILED] Click effects could not start.
echo.
if exist "%LOG%" (
  echo ---------------- LOG ----------------
  type "%LOG%"
  echo -------------------------------------
) else (
  echo No log was created.
)
echo.
echo Run RUN_VISIBLE_DEBUG.cmd and send the error text.
pause
exit /b 1

:nops
echo [ERROR] Windows PowerShell was not found.
pause
exit /b 1

:noscript
echo [ERROR] CursorClick.ps1 was not found.
pause
exit /b 1
