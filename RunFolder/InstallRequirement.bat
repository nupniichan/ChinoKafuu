@echo off
setlocal

set "currentPath=%cd%"
pip install -r "%currentPath%\requirements.txt"

echo Install requirements successfully
pause
