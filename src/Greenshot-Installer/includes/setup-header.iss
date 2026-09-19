[Setup]
; changes associations is used when the installer installs new extensions, it clears the explorer icon cache
ChangesAssociations=yes
; Use the Windows Restart Manager to close Greenshot gracefully before installation (triggering
; WM_QUERYENDSESSION so it can save open editor state) and to restart it afterwards using the
; command line arguments registered via RegisterApplicationRestart (i.e. --restore).
CloseApplications=yes
RestartApplications=yes
AppId=Greenshot
AppName={#AppDisplayName}
AppPublisher={#ExeName}
AppPublisherURL=https://getgreenshot.org
AppSupportURL=https://getgreenshot.org
AppUpdatesURL=https://getgreenshot.org
AppVerName={#AppDisplayName} {#Version}
AppVersion={#Version}
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2/ultra64
SolidCompression=yes
DefaultDirName={autopf}\{#ExeName}
DefaultGroupName={#ExeName}
InfoBeforeFile=additional_files\readme.txt
LicenseFile=additional_files\gpl-3.0.rtf
LanguageDetectionMethod=uilanguage
MinVersion=10.0.10240
OutputDir=..\..\installer
; user may choose between all-users vs. current-user installation in a dialog or by using the /ALLUSERS flag (on the command line)
; in registry section, HKA will take care of the appropriate root key (HKLM vs. HKCU), see https://jrsoftware.org/ishelp/index.php?topic=admininstallmode
PrivilegesRequiredOverridesAllowed=dialog
; admin privileges not required, unless user chooses all-users installation
; the installer will ask for elevation if needed
PrivilegesRequired=admin
UsePreviousPrivileges=no
UsedUserAreasWarning=no
MissingMessagesWarning=no
NotRecognizedMessagesWarning=no

SetupIconFile=..\Greenshot\icons\applicationIcon\icon.ico
#if CertumThumbprint != ""
  OutputBaseFilename={#ExeName}{#OutputSuffix}-INSTALLER-{#VersionEnhanced}-UNSTABLE
  SignTool=SignTool sign /sha1 "{#CertumThumbprint}" /tr http://time.certum.pl /td sha256 /fd sha256 /v $f
  SignedUninstaller=yes
#else
  OutputBaseFilename={#ExeName}{#OutputSuffix}-INSTALLER-{#VersionEnhanced}-UNSTABLE-UNSIGNED
#endif
UninstallDisplayIcon={app}\{#ExeName}.exe
Uninstallable=yes
VersionInfoCompany={#ExeName}
VersionInfoProductName={#AppDisplayName}
VersionInfoProductTextVersion={#VersionEnhanced}
VersionInfoTextVersion={#VersionEnhanced}
VersionInfoVersion={#Version}
; Reference a bitmap, max size 164x314
WizardImageFile=installer-large.bmp
; Reference a bitmap, max size 55x58
WizardSmallImageFile=installer-small.bmp
WizardStyle=modern
UninstallDisplayName={#AppDisplayName}
