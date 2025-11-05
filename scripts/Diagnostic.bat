@echo off
echo ========================================
echo Diagnostic DaryPWD
echo ========================================
echo.

echo Verification de .NET Framework...
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo .NET Framework 4.x detecte
) else (
    echo ATTENTION: .NET Framework 4.x peut ne pas etre installe
)

echo.
echo Tentative d'execution de DaryPWD...
echo.

cd /d "%~dp0\bin\Release"

if exist "DaryPWD.exe" (
    echo Execution directe...
    DaryPWD.exe
    
    if %ERRORLEVEL% NEQ 0 (
        echo.
        echo ERREUR: Code de retour %ERRORLEVEL%
    ) else (
        echo.
        echo Application fermee normalement.
    )
) else (
    echo ERREUR: DaryPWD.exe introuvable
)

echo.
pause

