@echo off
setlocal
cd /d "%~dp0"

echo Starting Word format checker...
echo URL: http://localhost:5088

netstat -ano | findstr /R /C:":5088 .*LISTENING" >nul
if %ERRORLEVEL% EQU 0 (
    echo The application is already running.
    start "" "http://localhost:5088"
    echo Press any key to close this window.
    pause >nul
    exit /b 0
)

start "" "http://localhost:5088"

dotnet run --urls "http://localhost:5088"

echo.
echo The application has stopped. Press any key to close this window.
pause >nul
