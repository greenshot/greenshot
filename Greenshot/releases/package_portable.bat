@echo off
cd ..

del /q releases\GreenshotPortable*.paf.exe
del /s /q releases\portable\App\Greenshot\*
rmdir /s /q releases\portable\App\Greenshot\Languages
rmdir /s /q releases\portable\App\Greenshot\Plugins
del /q releases\portable\*.*

pause
mkdir releases\portable\App\Greenshot\Plugins
xcopy /S bin\Release\Plugins\* releases\portable\App\Greenshot\Plugins\
mkdir releases\portable\App\Greenshot\Languages
xcopy /S Languages\language*.xml releases\portable\App\Greenshot\Languages
xcopy /S Languages\help*.html releases\portable\App\Greenshot\Languages
copy Languages\help-en-US.html releases\portable\help.html
xcopy /S bin\Release\Languages\Plugins\* releases\portable\App\Greenshot\Languages\Plugins\

copy /B bin\Release\checksum.MD5 releases\portable\App\Greenshot
copy /B bin\Release\GreenshotPlugin.dll releases\portable\App\Greenshot
copy /B bin\Release\log4net.dll releases\portable\App\Greenshot
copy /B log4net-portable.xml releases\portable\App\Greenshot
copy /B bin\Release\Greenshot.exe releases\portable\
copy /B bin\Release\Greenshot.exe.config releases\portable\
xcopy /S releases\additional_files\*.txt releases\portable\App\Greenshot
del releases\portable\App\Greenshot\*.template
pause