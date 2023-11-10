@echo off
set SERVICE_NAME="TB_Publisher"
set CURRENT_PATH=%CD%\TB_Publisher.exe
echo The current path is: %CURRENT_PATH%
sc query "%SERVICE_NAME%" > nul 2>&1
if %errorlevel% equ 0 (
    sc stop "%SERVICE_NAME%"
    sc delete "%SERVICE_NAME%"
    echo Delete service success
)