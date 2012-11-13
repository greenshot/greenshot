@echo off
cd ..
echo Getting current Version
tools\TortoiseSVN\SubWCRev.exe ..\ releases\additional_files\readme.template.txt releases\additional_files\readme.txt
tools\TortoiseSVN\SubWCRev.exe ..\ releases\innosetup\setup.iss releases\innosetup\setup-SVN.iss
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
echo ;dummy config, used to make greenshot store the configuration in this directory  > releases\NO-INSTALLER\greenshot.ini
echo ;In this file you should add your default settings  > releases\NO-INSTALLER\greenshot-defaults.ini
echo ;In this file you should add your fixed settings > releases\NO-INSTALLER\greenshot-fixed.ini
xcopy /S bin\Release\Plugins releases\NO-INSTALLER\Plugins\
copy /B bin\Release\checksum.MD5 releases\NO-INSTALLER
copy /B bin\Release\Greenshot.exe releases\NO-INSTALLER
copy /B bin\Release\Greenshot.exe.config releases\NO-INSTALLER
copy /B bin\Release\GreenshotPlugin.dll releases\NO-INSTALLER
copy /B bin\Release\log4net.dll releases\NO-INSTALLER
copy /B log4net-portable.xml releases\NO-INSTALLER\log4net.xml
xcopy /S releases\additional_files\*.txt releases\NO-INSTALLER
mkdir releases\NO-INSTALLER\Languages
xcopy /S languages\language*.xml releases\NO-INSTALLER\Languages\
xcopy /S languages\help*.html releases\NO-INSTALLER\Languages\
xcopy /S bin\Release\Languages\Plugins releases\NO-INSTALLER\Languages\Plugins\
cd releases\NO-INSTALLER
del /s *.pdb
del /s *.bak
del /s *installer*.xml
del /s *website*.xml
del /s *template.txt
..\..\tools\7zip\7za.exe a -x!.SVN -r ..\Greenshot-NO-INSTALLER.zip *
cd ..\..
echo Cleanup after key press
pause

rmdir /s /q releases\NO-INSTALLER
echo Finshed
pause