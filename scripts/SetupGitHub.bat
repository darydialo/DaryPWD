@echo off
echo ========================================
echo Configuration et Push vers GitHub
echo ========================================
echo.

REM Vérifier si un remote existe
git remote -v >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Remote existant trouve:
    git remote -v
    echo.
    set /p ACTION="Voulez-vous utiliser ce remote? (O/N): "
    if /i "%ACTION%"=="N" (
        goto :SET_REMOTE
    ) else (
        goto :PUSH
    )
) else (
    echo Aucun remote configure.
    echo.
)

:SET_REMOTE
echo.
echo ========================================
echo Etape 1: Configuration du remote GitHub
echo ========================================
echo.
echo IMPORTANT: Vous devez d'abord creer le repository sur GitHub:
echo   1. Allez sur https://github.com/new
echo   2. Nom du repository: DaryPWD
echo   3. Description: Application d'extraction de mots de passe IE et Edge
echo   4. Public ou Private (selon votre choix)
echo   5. NE PAS cocher "Initialize with README"
echo   6. Cliquez sur "Create repository"
echo.
set /p REMOTE_URL="Entrez l'URL de votre repository GitHub (ex: https://github.com/VOTRE_USERNAME/DaryPWD.git): "

if "%REMOTE_URL%"=="" (
    echo ERREUR: URL vide!
    pause
    exit /b 1
)

echo.
echo Ajout du remote...
git remote add origin %REMOTE_URL%

if %ERRORLEVEL% EQU 0 (
    echo Remote ajoute avec succes!
    echo.
    git remote -v
) else (
    echo ERREUR lors de l'ajout du remote!
    echo Tentative de mise a jour...
    git remote set-url origin %REMOTE_URL%
)

:PUSH
echo.
echo ========================================
echo Etape 2: Push vers GitHub
echo ========================================
echo.
echo Branche actuelle:
git branch
echo.
echo Commits a pousser:
git log --oneline -5
echo.
echo.
set /p CONFIRM="Voulez-vous pousser vers GitHub maintenant? (O/N): "
if /i not "%CONFIRM%"=="O" (
    echo Operation annulee.
    pause
    exit /b 0
)

echo.
echo Push en cours...
echo.
git push -u origin main

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Push reussi !
    echo ========================================
    echo.
    echo Votre code est maintenant sur GitHub!
    echo Allez sur votre repository pour verifier.
) else (
    echo.
    echo ========================================
    echo ERREUR lors du push !
    echo ========================================
    echo.
    echo Causes possibles:
    echo   1. Authentification requise
    echo      - Utilisez votre Personal Access Token comme mot de passe
    echo      - Creer un token: https://github.com/settings/tokens
    echo   2. Repository inexistant sur GitHub
    echo      - Verifiez que le repository existe bien
    echo   3. URL incorrecte
    echo      - Verifiez l'URL du remote: git remote -v
    echo.
    echo Pour creer un token GitHub:
    echo   1. Allez sur https://github.com/settings/tokens
    echo   2. Cliquez sur "Generate new token (classic)"
    echo   3. Nommez-le (ex: DaryPWD)
    echo   4. Selectionnez le scope "repo"
    echo   5. Cliquez sur "Generate token"
    echo   6. COPIEZ le token (il ne sera affiche qu'une fois)
    echo   7. Utilisez-le comme mot de passe lors du push
    echo.
)

pause

