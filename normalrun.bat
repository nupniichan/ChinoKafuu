@ECHO OFF

REM Set the path to the RunFolder
SET "RUN_FOLDER=%~dp0RunFolder"

REM Run each batch file in a new window
start "RunApplio" "%RUN_FOLDER%\RunApplio.bat"
call :CHECK_ERROR "Starting RunApplio.bat"

start "RunBot" "%RUN_FOLDER%\RunBot.bat"
call :CHECK_ERROR "Starting RunBot.bat"

start "RunLavaLink" "%RUN_FOLDER%\RunLavaLink.bat"
call :CHECK_ERROR "Starting RunLavaLink.bat"

ECHO All scripts started successfully
PAUSE
exit /b 0

:CHECK_ERROR
if %errorlevel% neq 0 (
    echo Error encountered during: %1
    PAUSE
    exit /b %errorlevel%
)
