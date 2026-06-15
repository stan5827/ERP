@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0seed-admin.ps1"
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" (
    echo.
    echo Seed failed with exit code %EXITCODE%.
    pause
)
endlocal & exit /b %EXITCODE%
