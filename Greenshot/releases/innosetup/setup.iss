#define ExeName "Greenshot"
#define Version "0.9.0.$WCREV$"

; Mutex is no longer needed!
;#define Mutex "F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08"

; Include the scripts to install .NET Framework 2.0
#include "scripts\products.iss"
#include "scripts\products\winversion.iss"
#include "scripts\products\fileversion.iss"
#include "scripts\products\msi20.iss"
#include "scripts\products\msi31.iss"
#include "scripts\products\dotnetfx20.iss"
#include "scripts\products\dotnetfx20lp.iss"
#include "scripts\products\dotnetfx20sp1.iss"
#include "scripts\products\dotnetfx20sp1lp.iss"
#include "scripts\products\dotnetfx20sp2.iss"
#include "scripts\products\dotnetfx20sp2lp.iss"

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

;------
; Add the "Files In Use Extension"
Source: IssProc\IssProc.dll; DestDir: {tmp}; Flags: dontcopy
; Add Files In Use Extension extra language file (you don t need to add this file if you are using english only)
Source: IssProc\IssProcLanguage.ini; DestDir: {tmp}; Flags: dontcopy
;------ Copy IssProc.dll in your app folder if you want to use it on unistall
Source: IssProc\IssProc.dll; DestDir: {app}
Source: IssProc\IssProcLanguage.ini; DestDir: {app}
;------
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
; changes associations is used when the installer installs new extensions, it  clears the explorer icon cache
;ChangesAssociations=yes
Compression=lzma/ultra64
InternalCompressLevel=ultra64
LanguageDetectionMethod=uilanguage
Uninstallable=true
UninstallDisplayIcon={app}\{#ExeName}.exe
VersionInfoCompany={#ExeName}
VersionInfoTextVersion={#Version}
VersionInfoVersion={#Version}
VersionInfoProductName={#ExeName}
PrivilegesRequired=poweruser
; Reference a bitmap, max size 164x314
WizardImageFile=installer-large.bmp
; Reference a bitmap, max size 55x58
WizardSmallImageFile=installer-small.bmp
MinVersion=,5.01.2600
[Registry]
Root: HKCU; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: string; ValueName: {#ExeName}; ValueData: {app}\{#ExeName}.exe; Permissions: users-modify; Flags: uninsdeletevalue; Tasks: startup
; Register our own filetype
;Root: HKCR; Subkey: ".gsb"; ValueType: string; ValueName: ""; ValueData: "GreenshotFile"; Flags: uninsdeletevalue
;Root: HKCR; Subkey: "GreenshotFile"; ValueType: string; ValueName: ""; ValueData: "Greenshot File"; Flags: uninsdeletekey
;Root: HKCR; Subkey: "GreenshotFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Greenshot.EXE,0"
;Root: HKCR; Subkey: "GreenshotFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Greenshot.EXE"" --openfile ""%1"""
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
// IssFindModule see http://raz-soft.com/display-english-posts-only/files-in-use-extension-for-inno-setup/
// IssFindModule called on install
function IssFindModule(hWnd: Integer; Modulename: PAnsiChar; Language: PAnsiChar; Silent: Boolean; CanIgnore: Boolean ): Integer;
external 'IssFindModule@files:IssProc.dll stdcall setuponly';

// IssFindModule called on uninstall
function IssFindModuleU(hWnd: Integer; Modulename: PAnsiChar; Language: PAnsiChar; Silent: Boolean; CanIgnore: Boolean ): Integer;
external 'IssFindModule@{app}\IssProc.dll stdcall uninstallonly';

// Don't install as long as Greenshot is running
function NextButtonClick(CurPage: Integer): Boolean;
var
  hWnd: Integer;
  sModuleName: String;
  sApp: String;
  nCode: Integer;
begin
	Result := true;
	if CurPage = wpReady then
	begin
		Result := false;
		ExtractTemporaryFile('IssProcLanguage.ini');
		hWnd := StrToInt(ExpandConstant('{wizardhwnd}'));
		sApp := ExpandConstant('{app}');
		// Check on all Greenshot binary files (plugins, exe & DLL's
		sModuleName := sApp + '\{#ExeName}.exe;' + sApp + '\{#ExeName}*.dll;' + sApp + '\*\*.dll;' + sApp + '\*\*.gsp';

		nCode:=IssFindModule(hWnd, sModuleName, ExpandConstant('{language}'), WizardSilent(), false);
		if nCode=1 then begin
			if WizardSilent() then begin
				while IssFindModule(hWnd, sModuleName, ExpandConstant('{language}'), WizardSilent(), false) = 1 do
				begin
					Exec('taskkill.exe', '/IM greenshot.exe', '', SW_HIDE, ewWaitUntilTerminated, nCode);
					Sleep(1200);
				end;

				Result := IssFindModule(hWnd, sModuleName, ExpandConstant('{language}'), WizardSilent(), false) = 0;
			end else begin
				PostMessage (WizardForm.Handle, $0010, 0, 0);
			end;
		end else if (nCode=0) or (nCode=2) then begin
			Result := true;
		end;
	end;

	// Check missing Dependencies
	ProductNextButtonClick(CurPage);
end;

function InitializeSetup(): Boolean;
var
	ErrorCode : Integer;
	NetFrameWorkInstalled : Boolean;
	MsgBoxResult : Boolean;
begin
	// Enhance installer otherwise .NET installations won't work
	msi20('2.0');
	msi31('3.0');
	
	//install .netfx 2.0 sp2 if possible; if not sp1 if possible; if not .netfx 2.0
	if minwinversion(5, 1) then begin
		dotnetfx20sp2();
		dotnetfx20sp2lp();
	end else begin
		if minwinversion(5, 0) and minwinspversion(5, 0, 4) then begin
			// kb835732();
			dotnetfx20sp1();
			dotnetfx20sp1lp();
		end else begin
			dotnetfx20();
			dotnetfx20lp();
		end;
	end;
	Result := true;
end;

function InitializeUninstall(): Boolean;
var
	sModuleName: String;
	nCode: Integer;
	sApp: String;
begin
    Result := false;
	sApp := ExpandConstant('{app}');

	// Check on all Greenshot binary files (plugins, exe & DLL's
	sModuleName := sApp + '\{#ExeName}.exe;' + sApp + '\{#ExeName}*.dll;' + sApp + '\*\*.dll;' + sApp + '\*\*.gsp';

	nCode:=IssFindModuleU(0, sModuleName, 'enu', false, false);
	if (nCode=0) then begin
          Result := true;
    end;

    // Unload the extension, otherwise it will not be deleted by the uninstaller
    UnloadDLL(ExpandConstant('{app}\IssProc.dll'));
end;
[Run]
Filename: {app}\{#ExeName}.exe; Description: {cm:startgreenshot}; Parameters: --configure Ui_Language={language}; WorkingDir: {app}; Flags: nowait postinstall runasoriginaluser
[InstallDelete]
Name: {app}; Type: filesandordirs; Languages: 
Name: {userstartup}\{#ExeName}.lnk; Type: files; Languages: 
Name: {commonstartup}\{#ExeName}.lnk; Type: files; Languages: 
