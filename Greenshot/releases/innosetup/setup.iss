#define ExeName "Greenshot"
#define Version "0.9.0.$WCREV$"
#define Mutex "F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08"
[Files]
Source: ..\..\bin\Release\*; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\..\bin\Release\Languages\*; DestDir: {app}\Languages; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\additional_files\*; DestDir: {app}; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion
;Flickr Plugin
;Source: ..\..\bin\Release\Plugins\GreenshotFlickrPlugin\*; DestDir: {app}\Plugins\GreenshotFlickrPlugin; Components: plugins\flickr; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
;Source: ..\..\bin\Release\Languages\Plugins\GreenshotFlickrPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotFlickrPlugin; Components: plugins\flickr; Flags: overwritereadonly ignoreversion replacesameversion;
;OCR Plugin
Source: ..\..\bin\Release\Plugins\Greenshot-OCR-Plugin\*; DestDir: {app}\Plugins\Greenshot-OCR-Plugin; Components: plugins\ocr; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\Greenshot-OCR-Plugin\*; DestDir: {app}\Languages\Plugins\Greenshot-OCR-Plugin; Components: plugins\ocr; Flags: overwritereadonly ignoreversion replacesameversion;
;JIRA Plugin
;Source: ..\..\bin\Release\Plugins\GreenshotJiraPlugin\*; DestDir: {app}\Plugins\GreenshotJiraPlugin; Components: plugins\jira; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
;Source: ..\..\bin\Release\Languages\Plugins\GreenshotJiraPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotJiraPlugin; Components: plugins\jira; Flags: overwritereadonly ignoreversion replacesameversion;
;Title-Fix Plugin
Source: ..\..\bin\Release\Plugins\Greenshot-TitleFix-Plugin\*; DestDir: {app}\Plugins\Greenshot-TitleFix-Plugin; Components: plugins\titlefix; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
[Setup]
OutputDir=..\
OutputBaseFilename={#ExeName}-INSTALLER-{#Version}
DefaultDirName={pf}\{#ExeName}
DefaultGroupName={#ExeName}
AppId={#ExeName}
AppName={#ExeName}
AppPublisher={#ExeName}
AppPublisherURL=http://getgreenshot.org
AppSupportURL=http://getgreenshot.org
AppUpdatesURL=http://getgreenshot.org
AppVerName={#ExeName} {#Version}
AppVersion={#Version}
Compression=lzma/ultra64
InternalCompressLevel=ultra64
LanguageDetectionMethod=uilanguage
Uninstallable=true
UninstallDisplayIcon={app}\{#ExeName}.exe
VersionInfoCompany={#ExeName}
VersionInfoTextVersion={#Version}
VersionInfoVersion={#Version}
VersionInfoProductName={#ExeName}
PrivilegesRequired=admin
; Reference a bitmap, max size 164x314
WizardImageFile=installer-large.bmp
; Reference a bitmap, max size 55x58
WizardSmallImageFile=installer-small.bmp
MinVersion=,5.01.2600
[Registry]
Root: HKCU; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: string; ValueName: {#ExeName}; ValueData: {app}\{#ExeName}.exe; Permissions: users-modify; Flags: uninsdeletevalue; Tasks: startup
[Icons]
Name: {group}\{#ExeName}; Filename: {app}\{#ExeName}.exe; WorkingDir: {app}
Name: {group}\Uninstall {#ExeName}; Filename: {app}\unins000.exe; WorkingDir: {app}
Name: {group}\Readme.txt; Filename: {app}\readme.txt; WorkingDir: {app}
Name: {group}\License.txt; Filename: {app}\license.txt; WorkingDir: {app}
[Languages]
Name: en; MessagesFile: compiler:Default.isl
Name: de; MessagesFile: compiler:Languages\German.isl
Name: nl; MessagesFile: compiler:Languages\Dutch.isl
[Tasks]
Name: startup; Description: {cm:startup}
[CustomMessages]
en.dotnetmissing=This setup requires the .NET Framework v2.0.%nDo you want to download the framework now?
de.dotnetmissing=Dieses Programm benötigt Microsoft .NET Framework v2.0.%nWollen Sie das Framework jetzt downloaden?
nl.dotnetmissing=Dit programma heeft Microsoft .NET Framework v2.0. nodig%nWilt u het Framework nu downloaden?
en.startup=Start {#ExeName} with Windows start
de.startup={#ExeName} starten wenn Windows hochfahrt
nl.startup=Start {#ExeName} wanneer Windows opstart
en.startgreenshot=Start {#ExeName}
de.startgreenshot={#ExeName} starten
nl.startgreenshot=Start {#ExeName}
en.titlefix=Title cleanup for Internet explorer and Firefox
de.titlefix=Titel aufräumen bei Internet explorer und Firefox
nl.titlefix=Titel opruimen bij Internet explorer en Firefox
en.ocr=OCR Plugin (needs Microsoft Office 2003 or 2007)
de.ocr=OCR Plugin (braucht Microsoft Office 2003 oder 2007)
nl.ocr=OCR Plugin (heeft Microsoft Office 2003 of 2007 nodig)
[Components]
Name: "plugins"; Description: "Plugins"; Types: Full
Name: "plugins\ocr"; Description: {cm:ocr}; Types: Full
;Name: "plugins\jira"; Description: "JIRA Plugin"; Types: Full
Name: "plugins\titlefix"; Description: {cm:titlefix}; Types: Full
;Name: "plugins\flickr"; Description: "Flickr Plugin"; Types: Full
[Code]
function InitializeSetup(): Boolean;
var
	ErrorCode : Integer;
	NetFrameWorkInstalled : Boolean;
	MsgBoxResult : Boolean;
	bMutex : Boolean;
	resultCode: Integer;
begin

	NetFrameWorkInstalled := RegKeyExists(HKLM, 'SOFTWARE\Microsoft\.NETFramework\policy\v2.0');
	if NetFrameWorkInstalled = true then
	begin
		bMutex:= CheckForMutexes ('Local\{#Mutex}');
		if bMutex = True then
		begin
			Exec('taskkill.exe', '/F /IM Greenshot.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
		end;
		Result := true;
	end;

	if NetFrameWorkInstalled = false then
	begin
		MsgBoxResult := MsgBox(ExpandConstant('{cm:dotnetmissing}'), mbConfirmation, MB_YESNO) = idYes;
		Result := false;
		if MsgBoxResult = true then
		begin
			ShellExec('open', 'http://download.microsoft.com/download/5/6/7/567758a3-759e-473e-bf8f-52154438565a/dotnetfx.exe', '','',SW_SHOWNORMAL,ewNoWait,ErrorCode);
		end;
	end;
end;

function InitializeUninstall():Boolean;
var
	bMutex : Boolean;
	resultCode: Integer;
begin
	bMutex:= CheckForMutexes ('Local\{#Mutex}');
	if bMutex = True then
	begin
		Exec(ExpandConstant('{app}\{#ExeName}.exe'), '--uninstall', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
	end;
	Result := True;
end;
[Run]
Filename: {app}\{#ExeName}.exe; Description: {cm:startgreenshot}; Parameters: --configure Ui_Language={language}; WorkingDir: {app}; Flags: nowait postinstall runasoriginaluser
[InstallDelete]
Name: {app}; Type: filesandordirs; Languages: 
Name: {userstartup}\{#ExeName}.lnk; Type: files; Languages: 
Name: {commonstartup}\{#ExeName}.lnk; Type: files; Languages: 
