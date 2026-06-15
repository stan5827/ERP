@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-all.ps1"
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" (
    echo.
    echo Start failed with exit code %EXITCODE%.
    pause
)
endlocal & exit /b %EXITCODE%
