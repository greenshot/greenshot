@echo off
echo File preparations
echo Getting current Version
cd ..
tools\TortoiseSVN\SubWCRev.exe ..\ releases\additional_files\readme.template.txt releases\additional_files\readme.txt
tools\TortoiseSVN\SubWCRev.exe ..\ releases\innosetup\setup.iss releases\innosetup\setup-SVN.iss
tools\TortoiseSVN\SubWCRev.exe ..\ releases\package_zip.bat releases\package_zip-SVN.bat
tools\TortoiseSVN\SubWCRev.exe ..\ releases\appinfo.ini.template releases\portable\App\AppInfo\appinfo.ini
tools\TortoiseSVN\SubWCRev.exe ..\ AssemblyInfo.cs.template AssemblyInfo.cs
rem Plugins
tools\TortoiseSVN\SubWCRev.exe ..\GreenshotBoxPlugin ..\GreenshotBoxPlugin\Properties\AssemblyInfo.cs.template ..\GreenshotBoxPlugin\Properties\AssemblyInfo.cs
tools\TortoiseSVN\SubWCRev.exe ..\GreenshotConfluencePlugin ..\GreenshotConfluencePlugin\Properties\AssemblyInfo.cs.template ..\GreenshotConfluencePlugin\Properties\AssemblyInfo.cs
tools\TortoiseSVN\SubWCRev.exe ..\GreenshotDropboxPlugin ..\GreenshotDropboxPlugin\Properties\AssemblyInfo.cs.template ..\GreenshotDropboxPlugin\Properties\AssemblyInfo.cs
tools\TortoiseSVN\SubWCRev.exe ..\GreenshotExternalCommandPlugin ..\GreenshotExternalCommandPlugin\Properties\AssemblyInfo.cs.template ..\GreenshotExternalCommandPlugin\Properties\AssemblyInfo.cs
tools\TortoiseSVN\SubWCRev.exe ..\GreenshotFlickrPlugin ..\GreenshotFlickrPlugin\Properties\AssemblyInfo.cs.template ..\GreenshotFlickrPlugin\Properties\AssemblyInfo.cs
tools\TortoiseSVN\SubWCRev.exe ..\GreenshotImgurPlugin ..\GreenshotImgurPlugin\Properties\AssemblyInfo.cs.template ..\GreenshotImgurPlugin\Properties\AssemblyInfo.cs
tools\TortoiseSVN\SubWCRev.exe ..\GreenshotJiraPlugin ..\GreenshotJiraPlugin\Properties\AssemblyInfo.cs.template ..\GreenshotJiraPlugin\Properties\AssemblyInfo.cs
tools\TortoiseSVN\SubWCRev.exe ..\GreenshotOCRPlugin ..\GreenshotOCRPlugin\Properties\AssemblyInfo.cs.template ..\GreenshotOCRPlugin\Properties\AssemblyInfo.cs
tools\TortoiseSVN\SubWCRev.exe ..\GreenshotOfficePlugin ..\GreenshotOfficePlugin\Properties\AssemblyInfo.cs.template ..\GreenshotOfficePlugin\Properties\AssemblyInfo.cs
tools\TortoiseSVN\SubWCRev.exe ..\GreenshotPhotobucketPlugin ..\GreenshotPhotobucketPlugin\Properties\AssemblyInfo.cs.template ..\GreenshotPhotobucketPlugin\Properties\AssemblyInfo.cs
tools\TortoiseSVN\SubWCRev.exe ..\GreenshotPicasaPlugin ..\GreenshotPicasaPlugin\Properties\AssemblyInfo.cs.template ..\GreenshotPicasaPlugin\Properties\AssemblyInfo.cs
tools\TortoiseSVN\SubWCRev.exe ..\GreenshotPlugin ..\GreenshotPlugin\Properties\AssemblyInfo.cs.template ..\GreenshotPlugin\Properties\AssemblyInfo.cs
tools\TortoiseSVN\SubWCRev.exe ..\PluginExample ..\PluginExample\Properties\AssemblyInfo.cs.template ..\PluginExample\Properties\AssemblyInfo.cs
cd releases
echo Starting Greenshot BUILD
\Windows\Microsoft.NET\Framework\v3.5\MSBuild ..\Greenshot.sln /t:Clean;Build /p:Configuration=Release /p:Platform="Any CPU" > build.log
if %ERRORLEVEL% GEQ 1 (
echo An error occured, please check the build log!
pause
exit -1
)
cd ..
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
call package_zip-SVN.bat
echo Building portable
pause
call package_portable.bat
echo Finshed
pause