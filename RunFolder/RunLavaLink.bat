@ECHO OFF

REM Get current location
SET "CURRENT_DIR=%~dp0"

REM Move to LavaLink folder and run Lavalink
cd /d "%CURRENT_DIR%\.."
cd /d "%CD%\..\ChinoKafu\LavaLink"
call java -jar Lavalink.jar

ECHO Run successfully
PAUSE