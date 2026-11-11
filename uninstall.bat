@echo off
echo ==============================================
echo       Uninstalling Gothic Clock...
echo ==============================================
echo.

echo [1/4] Stopping running processes...
taskkill /F /IM GothicClock.exe >nul 2>&1

echo [2/4] Removing startup registry entries...
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "GothicClockWidget" /f >nul 2>&1

echo [3/4] Removing shortcuts...
if exist "%USERPROFILE%\OneDrive\Desktop\Gothic Clock.lnk" del /f /q "%USERPROFILE%\OneDrive\Desktop\Gothic Clock.lnk"
if exist "%USERPROFILE%\Desktop\Gothic Clock.lnk" del /f /q "%USERPROFILE%\Desktop\Gothic Clock.lnk"
if exist "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Gothic Clock.lnk" del /f /q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Gothic Clock.lnk"

echo [4/4] Removing installed files...
set "APP_DIR=%LOCALAPPDATA%\Programs\GothicClock"
if exist "%APP_DIR%" (
    start /b cmd /c "timeout /t 1 /nobreak >nul & rmdir /s /q \"%APP_DIR%\""
)

echo.
echo ==============================================
echo  Gothic Clock has been completely uninstalled!
echo ==============================================
echo.
pause
