@ECHO OFF

REM Check and install python library
call pip install google-generativeai
call pip install pillow
call pip install grpcio

REM Get the current directory where the script is located
SET "CURRENT_DIR=%~dp0"

REM Move to Applio Folder
cd /d "%CURRENT_DIR%\.."
cd /d "%CD%\..\ChinoKafu\Applio"
call run-applio.bat

ECHO Run successfully
PAUSE