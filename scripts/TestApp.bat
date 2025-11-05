@echo off
echo ========================================
echo Test de DaryPWD.exe
echo ========================================
echo.

cd /d "%~dp0"

if exist "bin\Release\DaryPWD.exe" (
    echo Execution de DaryPWD.exe...
    echo.
    echo Si l'application ne s'affiche pas, une erreur sera affichee.
    echo.
    start "" "bin\Release\DaryPWD.exe"
    timeout /t 3 /nobreak > nul
    echo.
    echo Application lancee. Verifiez si la fenetre s'affiche.
) else (
    echo ERREUR: DaryPWD.exe introuvable dans bin\Release\
    echo Veuillez compiler le projet d'abord.
)

echo.
pause

