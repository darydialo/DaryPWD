@echo off
echo ========================================
echo Push vers GitHub - DaryPWD
echo ========================================
echo.

REM Vérifier si un remote existe déjà
git remote -v >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Remote existant trouve:
    git remote -v
    echo.
    set /p ACTION="Voulez-vous utiliser ce remote? (O/N): "
    if /i "%ACTION%"=="N" (
        set /p REMOTE_URL="Entrez l'URL de votre repository GitHub: "
        git remote set-url origin %REMOTE_URL%
    )
) else (
    set /p REMOTE_URL="Entrez l'URL de votre repository GitHub: "
    git remote add origin %REMOTE_URL%
)

echo.
echo ========================================
echo Pushing vers GitHub...
echo ========================================
echo.

git push -u origin main

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Push reussi !
    echo ========================================
) else (
    echo.
    echo ========================================
    echo Erreur lors du push !
    echo.
    echo Assurez-vous que:
    echo 1. Vous avez cree le repository sur GitHub
    echo 2. L'URL est correcte
    echo 3. Vous etes authentifie (token ou SSH)
    echo ========================================
)

pause

