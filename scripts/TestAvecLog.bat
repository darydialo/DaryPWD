@echo off
echo ========================================
echo Test de DaryPWD avec logging
echo ========================================
echo.

cd /d "%~dp0\bin\Release"

if exist "DaryPWD.exe" (
    echo Suppression de l'ancien fichier de log...
    if exist "DaryPWD.log" del "DaryPWD.log"
    
    echo.
    echo Lancement de DaryPWD.exe...
    echo Attendez 5 secondes puis fermez l'application si elle s'affiche...
    echo.
    
    start "" "DaryPWD.exe"
    timeout /t 5 /nobreak > nul
    
    echo.
    echo ========================================
    echo Contenu du fichier de log:
    echo ========================================
    echo.
    
    if exist "DaryPWD.log" (
        type "DaryPWD.log"
    ) else (
        echo ERREUR: Le fichier de log n'a pas ete cree!
        echo Cela signifie que l'application n'a pas demarre.
    )
    
    echo.
    echo ========================================
    echo Vérification des processus DaryPWD:
    echo ========================================
    tasklist | findstr /i "DaryPWD"
    
) else (
    echo ERREUR: DaryPWD.exe introuvable dans bin\Release\
)

echo.
pause

