@ECHO OFF

REM Get current location
SET "CURRENT_DIR=%~dp0"

REM Move to main bot folder
cd /d "%CURRENT_DIR%\.."
cd /d "%CD%\..\ChinoKafu\ChinoKafuu\bin\Debug\net7.0
call ChinoBot.exe

ECHO Run successfully
PAUSE
exit /b 0
