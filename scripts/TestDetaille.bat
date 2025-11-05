@echo off
echo ========================================
echo Test de DaryPWD avec logging detaille
echo ========================================
echo.

cd /d "%~dp0\bin\Release"

if exist "DaryPWD.exe" (
    echo Suppression de l'ancien fichier de log...
    if exist "DaryPWD.log" del "DaryPWD.log"
    
    echo.
    echo Lancement de DaryPWD.exe avec capture de sortie...
    echo Attendez 10 secondes pour permettre le chargement complet...
    echo.
    
    start "" "DaryPWD.exe"
    timeout /t 10 /nobreak > nul
    
    echo.
    echo ========================================
    echo Contenu du fichier de log:
    echo ========================================
    echo.
    
    if exist "DaryPWD.log" (
        type "DaryPWD.log"
    ) else (
        echo ERREUR: Le fichier de log n'a pas ete cree!
    )
    
    echo.
    echo ========================================
    echo Processus DaryPWD en cours:
    echo ========================================
    tasklist | findstr /i "DaryPWD"
    
    echo.
    echo Si l'application est toujours en cours d'execution,
    echo elle peut etre bloquee dans l'extraction des mots de passe.
    echo Appuyez sur une touche pour fermer cette fenetre.
    
) else (
    echo ERREUR: DaryPWD.exe introuvable dans bin\Release\
)

pause

