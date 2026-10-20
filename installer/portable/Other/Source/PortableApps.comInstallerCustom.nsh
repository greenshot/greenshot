!macro CustomCodePostInstall
ReadINIStr $0 "$INSTDIR\App\AppInfo\appinfo.ini" "Version" "DisplayVersion"
ExecShell "open" "https://getgreenshot.org/thank-you/?language=en-US&version=$0"
!macroend
