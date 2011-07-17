#define ExeName "Greenshot"
#define Version "0.8.1.$WCREV$"

; Include the scripts to install .NET Framework 2.0
; See http://www.codeproject.com/KB/install/dotnetfx_innosetup_instal.aspx
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
Source: ..\additional_files\*; DestDir: {app}; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion
;Language files
Source: ..\..\bin\Release\Languages\*nl-NL*; DestDir: {app}\Languages; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*en-US*; DestDir: {app}\Languages; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*de-DE*; DestDir: {app}\Languages; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*cs-CZ*; DestDir: {app}\Languages; Components: languages\CZ; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*el-GR*; DestDir: {app}\Languages; Components: languages\GR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*es-ES*; DestDir: {app}\Languages; Components: languages\ES; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*fi-FI*; DestDir: {app}\Languages; Components: languages\FI; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*fr-FR*; DestDir: {app}\Languages; Components: languages\FR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*he-IL*; DestDir: {app}\Languages; Components: languages\IL; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*hu-HU*; DestDir: {app}\Languages; Components: languages\HU; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*it-IT*; DestDir: {app}\Languages; Components: languages\IT; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*ja-JP*; DestDir: {app}\Languages; Components: languages\JP; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*lt-LT*; DestDir: {app}\Languages; Components: languages\LT; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*pl-PL*; DestDir: {app}\Languages; Components: languages\PL; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*pt-BR*; DestDir: {app}\Languages; Components: languages\BR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*ru-RU*; DestDir: {app}\Languages; Components: languages\RU; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*sv-SE*; DestDir: {app}\Languages; Components: languages\SE; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*tr-TR*; DestDir: {app}\Languages; Components: languages\TR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*zh-CN*; DestDir: {app}\Languages; Components: languages\CN; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*zh-TW*; DestDir: {app}\Languages; Components: languages\TW; Flags: overwritereadonly ignoreversion replacesameversion;

;Flickr Plugin
;Source: ..\..\bin\Release\Plugins\GreenshotFlickrPlugin\*; DestDir: {app}\Plugins\GreenshotFlickrPlugin; Components: plugins\flickr; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
;Source: ..\..\bin\Release\Languages\Plugins\GreenshotFlickrPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotFlickrPlugin; Components: plugins\flickr; Flags: overwritereadonly ignoreversion replacesameversion;
;OCR Plugin
Source: ..\..\bin\Release\Plugins\Greenshot-OCR-Plugin\*; DestDir: {app}\Plugins\Greenshot-OCR-Plugin; Components: plugins\ocr; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\Greenshot-OCR-Plugin\*; DestDir: {app}\Languages\Plugins\Greenshot-OCR-Plugin; Components: plugins\ocr; Flags: overwritereadonly ignoreversion replacesameversion;
;JIRA Plugin
Source: ..\..\bin\Release\Plugins\GreenshotJiraPlugin\*; DestDir: {app}\Plugins\GreenshotJiraPlugin; Components: plugins\jira; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotJiraPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotJiraPlugin; Components: plugins\jira; Flags: overwritereadonly ignoreversion replacesameversion;
;Imgur Plugin
Source: ..\..\bin\Release\Plugins\GreenshotImgurPlugin\*; DestDir: {app}\Plugins\GreenshotImgurPlugin; Components: plugins\imgur; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotImgurPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotImgurPlugin; Components: plugins\imgur; Flags: overwritereadonly ignoreversion replacesameversion;
;Confluence Plugin
;Source: ..\..\bin\Release\Plugins\GreenshotConfluencePlugin\*; DestDir: {app}\Plugins\GreenshotConfluencePlugin; Components: plugins\confluence; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
;Source: ..\..\bin\Release\Languages\Plugins\GreenshotConfluencePlugin\*; DestDir: {app}\Languages\Plugins\GreenshotConfluencePlugin; Components: plugins\confluence; Flags: overwritereadonly ignoreversion replacesameversion;
;Title-Fix Plugin
Source: ..\..\bin\Release\Plugins\Greenshot-TitleFix-Plugin\*; DestDir: {app}\Plugins\Greenshot-TitleFix-Plugin; Components: plugins\titlefix; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
[Setup]
; changes associations is used when the installer installs new extensions, it  clears the explorer icon cache
;ChangesAssociations=yes
AppId={#ExeName}
AppName={#ExeName}
AppMutex=F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08
AppPublisher={#ExeName}
AppPublisherURL=http://getgreenshot.org
AppSupportURL=http://getgreenshot.org
AppUpdatesURL=http://getgreenshot.org
AppVerName={#ExeName} {#Version}
AppVersion={#Version}
DefaultDirName={pf}\{#ExeName}
DefaultGroupName={#ExeName}
InfoBeforeFile=..\additional_files\readme.txt
LanguageDetectionMethod=uilanguage
MinVersion=,5.01.2600
OutputBaseFilename={#ExeName}-INSTALLER-UNSTABLE-{#Version}
OutputDir=..\
PrivilegesRequired=none
SetupIconFile=..\..\icons\applicationIcon\icon.ico
UninstallDisplayIcon={app}\{#ExeName}.exe
Uninstallable=true
VersionInfoCompany={#ExeName}
VersionInfoProductName={#ExeName}
VersionInfoTextVersion={#Version}
VersionInfoVersion={#Version}
; Reference a bitmap, max size 164x314
WizardImageFile=installer-large.bmp
; Reference a bitmap, max size 55x58
WizardSmallImageFile=installer-small.bmp
[Registry]
; Delete all startup entries, so we don't have leftover values
; HKEY_LOCAL_USER - for current user only
Root: HKCU; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue;
; HKEY_LOCAL_MACHINE - for all users
Root: HKLM; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue; Check: IsAdminLoggedOn
; Create the startup entries if requested to do so
; HKEY_LOCAL_USER - for current user only
Root: HKCU; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: string; ValueName: {#ExeName}; ValueData: {app}\{#ExeName}.exe; Permissions: users-modify; Flags: uninsdeletevalue; Tasks: startup; Check: not IsAdminLoggedOn
; HKEY_LOCAL_MACHINE - for all users
Root: HKLM; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: string; ValueName: {#ExeName}; ValueData: {app}\{#ExeName}.exe; Permissions: users-modify; Flags: uninsdeletevalue; Tasks: startup; Check: IsAdminLoggedOn
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
en.jira=Editor plug-in for Jira
de.jira=Editor plug-in für Jira
nl.jira=Editor plug-in voor Jira
en.ocr=OCR Plugin (needs Microsoft Office Document Imaging (MODI))
de.ocr=OCR Plugin (braucht Microsoft Office Document Imaging (MODI))
nl.ocr=OCR Plugin (heeft Microsoft Office Document Imaging (MODI) nodig)
en.imgur=Imgur Plugin (See: http://imgur.com)
de.imgur=Imgur Plugin (Sehe: http://imgur.com)
nl.imgur=Imgur Plugin (Zie: http://imgur.com)
en.language=Additional languages
de.language=Zusatz Sprachen
nl.language=Extra talen
[Components]
Name: "plugins"; Description: "Plugins"; Types: Full
Name: "plugins\ocr"; Description: {cm:ocr}; Types: Full;
Name: "plugins\jira"; Description: {cm:jira}; Types: Full
Name: "plugins\imgur"; Description: {cm:imgur}; Types: Full;
;Name: "plugins\confluence"; Description: "Confluence Plugin"; Types: Full
Name: "plugins\titlefix"; Description: {cm:titlefix}; Types: Full
;Name: "plugins\flickr"; Description: "Flickr Plugin"; Types: Full
Name: "languages"; Description: {cm:language}; Types: Full
Name: "languages\CZ"; Description: "Ceština"; Types: Full
Name: "languages\GR"; Description: "ελληνικά"; Types: Full
Name: "languages\ES"; Description: "Español"; Types: Full
Name: "languages\FI"; Description: "Suomi"; Types: Full
Name: "languages\FR"; Description: "Français"; Types: Full
Name: "languages\IL"; Description: "עִבְרִית"; Types: Full
Name: "languages\HU"; Description: "Magyar"; Types: Full
Name: "languages\IT"; Description: "Italiano"; Types: Full
Name: "languages\JP"; Description: "日本語"; Types: Full
Name: "languages\LT"; Description: "Lietuvių"; Types: Full
Name: "languages\PL"; Description: "Polski"; Types: Full
Name: "languages\BR"; Description: "Português do Brasil"; Types: Full
Name: "languages\RU"; Description: "Pусский"; Types: Full
Name: "languages\SE"; Description: "Svenska"; Types: Full
Name: "languages\TR"; Description: "Turkish"; Types: Full
Name: "languages\CN"; Description: "简体中文"; Types: Full
Name: "languages\TW"; Description: "繁體中文"; Types: Full
[Code]
// Build a list of greenshot parameters from the supplied installer parameters
function GetParamsForGS(argument: String): String;
var
	i: Integer;
	parametersString: String;
	currentParameter: String;
	equalsSignPos: Integer;
	foundStart: Boolean;
	foundNoRun: Boolean;
	foundLanguage: Boolean;
begin
	foundNoRun := false;
	foundLanguage := false;
	foundStart := false;
	for i:= 0 to ParamCount() do begin
		currentParameter := ParamStr(i);
		
		// check if norun is supplied
		if Lowercase(currentParameter) = '/norun' then begin
			foundNoRun := true;
			continue;
		end;

		if foundStart then begin
			parametersString := parametersString + ' ' + currentParameter;
			foundStart := false;
		end
		else begin
			if Lowercase(currentParameter) = '/language' then begin
				foundStart := true;
				foundLanguage := true;
				parametersString := parametersString + ' ' + currentParameter;
			end;
		end;
	end;
	if not foundLanguage then begin
		parametersString := parametersString + ' /language ' + ExpandConstant('{language}');
	end;
	if foundNoRun then begin
		parametersString := parametersString + ' /norun';
	end;
	// For debugging comment out the following
	//MsgBox(parametersString, mbInformation, MB_OK);

	Result := parametersString;
end;

function InitializeSetup(): Boolean;
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
[Run]
Filename: {app}\{#ExeName}.exe; Description: {cm:startgreenshot}; Parameters: {code:GetParamsForGS}; WorkingDir: {app}; Flags: nowait postinstall runasoriginaluser
[InstallDelete]
Name: {app}; Type: filesandordirs;