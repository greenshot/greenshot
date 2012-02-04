cd ..
tools\TortoiseSVN\SubWCRev.exe . releases\innosetup\setup.iss releases\innosetup\setup-SVN.iss
del bin\Release\*.config
del bin\Release\*.log
cd bin\Release
..\..\tools\FileVerifier++\fvc.exe -c -a MD5 -r -o checksum.MD5 Greenshot.exe GreenshotPlugin.dll
cd ..\..
pause
tools\innosetup\ISCC.exe releases\innosetup\setup-SVN.iss
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
del /s /q releases\NO-INSTALLER
pause