@echo off
echo Starting Greenshot BUILD
\Windows\Microsoft.NET\Framework\v3.5\MSBuild ..\Greenshot.sln /t:Clean;Build /p:Configuration=Release /p:Platform="Any CPU" > build.log
if %ERRORLEVEL% GEQ 1 (
echo An error occured, please check the build log!
pause
exit -1
)
echo File preparations
cd ..
echo Getting current Version
tools\TortoiseSVN\SubWCRev.exe ..\ releases\additional_files\readme.template.txt releases\additional_files\readme.txt
tools\TortoiseSVN\SubWCRev.exe ..\ releases\innosetup\setup.iss releases\innosetup\setup-SVN.iss
tools\TortoiseSVN\SubWCRev.exe ..\ releases\package.bat releases\package-SVN.bat
cd bin\Release
del *.log
echo Making MD5
..\..\tools\FileVerifier++\fvc.exe -c -a MD5 -r -o checksum.MD5 Greenshot.exe GreenshotPlugin.dll
cd ..\..\releases
echo Building installer after key press
pause
call build_installer.bat
echo Building zip after key press
pause
call package-SVN.bat
echo Finshed
pause