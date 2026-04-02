@echo off
setlocal EnableExtensions

set "REPO_URL=https://github.com/VeridonNetzwerk/bt-audio-sink"
set "SOURCE_DIR=%~dp0"
set "SOURCE_DIR=%SOURCE_DIR:~0,-1%"
set "WORK_REPO=%SOURCE_DIR%\.push-repo"

if exist "%SOURCE_DIR%\.git" (
    set "PUSH_DIR=%SOURCE_DIR%"
    goto :push
)

echo [push.bat] Kein .git im aktuellen Ordner gefunden.
echo [push.bat] Verwende lokales Hilfs-Repo unter "%WORK_REPO%".

if not exist "%WORK_REPO%\.git" (
    git clone "%REPO_URL%" "%WORK_REPO%"
    if errorlevel 1 (
        echo [push.bat] Fehler beim Klonen von %REPO_URL%.
        exit /b 1
    )
) else (
    git config --global --add safe.directory "%WORK_REPO%" >nul 2>nul
)

robocopy "%SOURCE_DIR%" "%WORK_REPO%" /MIR /XD ".git" ".vs" "bin" "obj" "publish" "publish_new" ".tmp" ".push-repo" /XF "*.user" "*.suo" "*.wixpdb" "*.msi" >nul
if errorlevel 8 (
    echo [push.bat] Fehler beim Synchronisieren in das Hilfs-Repo.
    exit /b 1
)

set "PUSH_DIR=%WORK_REPO%"

:push
pushd "%PUSH_DIR%" || (
    echo [push.bat] Kann Ordner "%PUSH_DIR%" nicht oeffnen.
    exit /b 1
)

git add -A

git status
echo.
set /p confirm=Sind die Änderungen korrekt? (j/n): 
if /I not "%confirm%"=="j" (
    echo Abgebrochen.
    popd
    exit /b
)

set /p msg=Commit message (leave empty for 'update'): 
if "%msg%"=="" set msg=update

git commit -m "%msg%"
if errorlevel 1 (
    echo [push.bat] Nichts zu committen oder Commit fehlgeschlagen.
    popd
    exit /b 1
)

git push
set "ERR=%ERRORLEVEL%"
popd
exit /b %ERR%