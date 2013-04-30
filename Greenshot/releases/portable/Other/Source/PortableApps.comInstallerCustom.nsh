!macro CustomCodePostInstall
CopyFiles /SILENT "$INSTDIR\App\Greenshot\Greenshot.exe.config" "$INSTDIR\"
ReadINIStr $0 "$INSTDIR\App\AppInfo\appinfo.ini" "Version" "PackageVersion"
ExecShell "open" "http://getgreenshot.org/thank-you/?language=en-US&version=$0"
!macroend