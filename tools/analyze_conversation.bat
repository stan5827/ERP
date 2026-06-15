@echo off
chcp 65001 >nul
set PYTHONUTF8=1
set BRAIN_ROOT=C:\Users\%USERNAME%\.gemini\antigravity\brain

echo.
echo  ============================================================
echo    Antigravity 對話分析工具
echo  ============================================================
echo.

if "%1"=="" (
    python "%~dp0conversation_analyzer.py" --brain-root "%BRAIN_ROOT%" %*
) else (
    python "%~dp0conversation_analyzer.py" --brain-root "%BRAIN_ROOT%" %*
)

pause
