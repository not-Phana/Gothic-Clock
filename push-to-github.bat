@echo off
title Push Gothic Clock to GitHub
cd /d "E:\Code\Gothic-Clock"
echo.
echo ========================================================
echo   PUSHING GOTHIC CLOCK (BLEACH EDITION) TO GITHUB
echo   Repository: https://github.com/not-Phana/Gothic-Clock.git
echo ========================================================
echo.
git push -u origin main
echo.
if %errorlevel% equ 0 (
    echo ========================================================
    echo   SUCCESS! Pushed to https://github.com/not-Phana/Gothic-Clock
    echo ========================================================
) else (
    echo ========================================================
    echo   If prompted, please sign in to GitHub in the browser.
    echo ========================================================
)
echo.
pause
