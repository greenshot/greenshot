@echo off
echo Starting Greenshot BUILD
cd ..
echo Getting current Version
tools\TortoiseSVN\SubWCRev.exe . releases\innosetup\setup.iss releases\innosetup\setup-SVN.iss
cd bin\Release
del *.log
echo Making MD5
..\..\tools\FileVerifier++\fvc.exe -c -a MD5 -r -o checksum.MD5 Greenshot.exe GreenshotPlugin.dll
cd ..\..
echo Building installer after key press
pause
tools\innosetup\ISCC.exe releases\innosetup\setup-SVN.iss
echo Building ZIP after key press
pause
del releases\Greenshot-NO-INSTALLER.zip
mkdir releases\NO-INSTALLER
echo dummy config, used to make greenshot store the configuration in this directory  > releases\NO-INSTALLER\greenshot.ini
xcopy /S bin\Release\* releases\NO-INSTALLER
copy /B log4net-portable.xml releases\NO-INSTALLER\log4net.xml
xcopy /S releases\additional_files\* releases\NO-INSTALLER
cd releases\NO-INSTALLER
..\..\tools\7zip\7za.exe a -x!.SVN -r ..\Greenshot-NO-INSTALLER.zip *
cd ..\..
echo Cleanup after key press
pause
del /s /q releases\NO-INSTALLER
echo Finshed
pause