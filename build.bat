@echo off
echo ==============================================
echo     Compiling Gothic Clock & Control Panel
echo ==============================================
echo.
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /win32icon:"%~dp0app.ico" /out:"%~dp0GothicClock.exe" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xaml.dll /r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll /r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationCore.dll /r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationFramework.dll "%~dp0GothicClock.cs"
if %errorlevel% equ 0 (
    echo [OK] Compilation successful: GothicClock.exe
) else (
    echo [FAIL] Compilation failed.
)
pause
