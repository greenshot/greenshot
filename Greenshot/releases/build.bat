@echo off
echo Starting Greenshot BUILD
\Windows\Microsoft.NET\Framework\v3.5\MSBuild ..\Greenshot.sln /t:Clean;Build /p:Configuration=Release /p:Platform="Any CPU" > build.log
if %ERRORLEVEL% GEQ 1 (
echo An error occured, please check the build log!
pause
exit -1
)
echo Installer preparation start after key press
pause
package.bat