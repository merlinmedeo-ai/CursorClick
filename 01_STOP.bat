@echo off
setlocal
taskkill /IM CursorClick.exe /T /F >nul 2>nul
set "PIDFILE=%TEMP%\VolantesCursorClick.pid"
set "READY=%TEMP%\VolantesCursorClick.ready"
set "SAVEDPID="

if exist "%PIDFILE%" set /p "SAVEDPID="<"%PIDFILE%"

if not defined SAVEDPID goto :notrunning

echo Stopping Volantes Click Effects PID %SAVEDPID%...
taskkill /PID %SAVEDPID% /T /F >nul 2>nul

if errorlevel 1 (
  echo [WARN] Process PID %SAVEDPID% was not found or could not be stopped.
) else (
  echo [OK] Click effects stopped.
)

del /q "%PIDFILE%" 2>nul
del /q "%READY%" 2>nul

if /I "%~1"=="silent" exit /b 0
pause
exit /b 0

:notrunning
del /q "%READY%" 2>nul
if /I "%~1"=="silent" exit /b 0
echo Click effects are not running ^(PID file not found^).
pause
exit /b 0
