@echo off
echo Compilation de DaryPWD...
echo.

"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ..\DaryPWD.csproj /p:Configuration=Release /p:Platform=AnyCPU /t:Rebuild

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Compilation reussie !
    echo Executable disponible dans: ..\bin\Release\DaryPWD.exe
    echo ========================================
) else (
    echo.
    echo ========================================
    echo Erreur lors de la compilation !
    echo ========================================
    pause
    exit /b 1
)

pause

