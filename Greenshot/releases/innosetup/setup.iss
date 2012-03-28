#define ExeName "Greenshot"
#define Version "0.9.0.$WCREV$"

; Include the scripts to install .NET Framework 2.0
; See http://www.codeproject.com/KB/install/dotnetfx_innosetup_instal.aspx
#include "scripts\products.iss"
#include "scripts\products\winversion.iss"
#include "scripts\products\fileversion.iss"
#include "scripts\products\msi20.iss"
#include "scripts\products\msi31.iss"
#include "scripts\products\dotnetfx20.iss"
#include "scripts\products\dotnetfx20sp1.iss"
#include "scripts\products\dotnetfx20sp2.iss"

[Files]
Source: ..\..\bin\Release\Greenshot.exe; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\..\bin\Release\GreenshotPlugin.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\..\bin\Release\Greenshot.exe.config; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\..\bin\Release\log4net.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\..\bin\Release\log4net.xml; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\..\bin\Release\checksum.MD5; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
;Source: ..\greenshot-defaults.ini; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\additional_files\installer.txt; DestDir: {app}; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion
Source: ..\additional_files\license.txt; DestDir: {app}; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion
Source: ..\additional_files\readme.txt; DestDir: {app}; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion
;Source: ..\additional_files\donate.bmp; Flags: dontcopy
; Core language files
Source: ..\..\bin\Release\Languages\*nl-NL*; DestDir: {app}\Languages; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*en-US*; DestDir: {app}\Languages; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*de-DE*; DestDir: {app}\Languages; Flags: overwritereadonly ignoreversion replacesameversion;
; Additional language files
Source: ..\..\bin\Release\Languages\*ar-SY*; DestDir: {app}\Languages; Components: languages\arSY; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*cs-CZ*; DestDir: {app}\Languages; Components: languages\csCZ; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*el-GR*; DestDir: {app}\Languages; Components: languages\elGR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*es-ES*; DestDir: {app}\Languages; Components: languages\esES; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*fa-IR*; DestDir: {app}\Languages; Components: languages\faIR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*fi-FI*; DestDir: {app}\Languages; Components: languages\fiFI; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*fr-FR*; DestDir: {app}\Languages; Components: languages\frFR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*he-IL*; DestDir: {app}\Languages; Components: languages\heIL; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*hu-HU*; DestDir: {app}\Languages; Components: languages\huHU; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*it-IT*; DestDir: {app}\Languages; Components: languages\itIT; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*ja-JP*; DestDir: {app}\Languages; Components: languages\jaJP; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*ko-KR*; DestDir: {app}\Languages; Components: languages\koKR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*lt-LT*; DestDir: {app}\Languages; Components: languages\ltLT; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*pl-PL*; DestDir: {app}\Languages; Components: languages\plPL; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*pt-BR*; DestDir: {app}\Languages; Components: languages\ptBR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*pt-PT*; DestDir: {app}\Languages; Components: languages\ptPT; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*ru-RU*; DestDir: {app}\Languages; Components: languages\ruRU; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*sv-SE*; DestDir: {app}\Languages; Components: languages\svSE; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*tr-TR*; DestDir: {app}\Languages; Components: languages\trTR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*uk-UA*; DestDir: {app}\Languages; Components: languages\ukUA; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*vi-VN*; DestDir: {app}\Languages; Components: languages\viVN; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*zh-CN*; DestDir: {app}\Languages; Components: languages\zhCN; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\*zh-TW*; DestDir: {app}\Languages; Components: languages\zhTW; Flags: overwritereadonly ignoreversion replacesameversion;

;OCR Plugin
Source: ..\..\bin\Release\Plugins\Greenshot-OCR-Plugin\*; DestDir: {app}\Plugins\Greenshot-OCR-Plugin; Components: plugins\ocr; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\Greenshot-OCR-Plugin\*; DestDir: {app}\Languages\Plugins\Greenshot-OCR-Plugin; Components: plugins\ocr; Flags: overwritereadonly ignoreversion replacesameversion;
;JIRA Plugin
Source: ..\..\bin\Release\Plugins\GreenshotJiraPlugin\*; DestDir: {app}\Plugins\GreenshotJiraPlugin; Components: plugins\jira; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotJiraPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotJiraPlugin; Components: plugins\jira; Flags: overwritereadonly ignoreversion replacesameversion;
;Imgur Plugin
Source: ..\..\bin\Release\Plugins\GreenshotImgurPlugin\*; DestDir: {app}\Plugins\GreenshotImgurPlugin; Components: plugins\imgur; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotImgurPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotImgurPlugin; Components: plugins\imgur; Flags: overwritereadonly ignoreversion replacesameversion;
;Box Plugin
;Source: ..\..\bin\Release\Plugins\GreenshotBoxPlugin\*; DestDir: {app}\Plugins\GreenshotBoxPlugin; Components: plugins\box; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
;Source: ..\..\bin\Release\Languages\Plugins\GreenshotBoxPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotBoxPlugin; Components: plugins\box; Flags: overwritereadonly ignoreversion replacesameversion;
;DropBox Plugin
;Source: ..\..\bin\Release\Plugins\GreenshotDropBoxPlugin\*; DestDir: {app}\Plugins\GreenshotDropBoxPlugin; Components: plugins\dropbox; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
;Source: ..\..\bin\Release\Languages\Plugins\GreenshotDropBoxPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotDropBoxPlugin; Components: plugins\dropbox; Flags: overwritereadonly ignoreversion replacesameversion;
;Flickr Plugin
;Source: ..\..\bin\Release\Plugins\GreenshotFlickrPlugin\*; DestDir: {app}\Plugins\GreenshotFlickrPlugin; Components: plugins\flickr; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
;Source: ..\..\bin\Release\Languages\Plugins\GreenshotFlickrPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotFlickrPlugin; Components: plugins\flickr; Flags: overwritereadonly ignoreversion replacesameversion;
;Picasa Plugin
;Source: ..\..\bin\Release\Plugins\GreenshotPicasaPlugin\*; DestDir: {app}\Plugins\GreenshotPicasaPlugin; Components: plugins\picasa; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
;Source: ..\..\bin\Release\Languages\Plugins\GreenshotPicasaPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotPicasaPlugin; Components: plugins\picasa; Flags: overwritereadonly ignoreversion replacesameversion;
;Confluence Plugin
;Source: ..\..\bin\Release\Plugins\GreenshotConfluencePlugin\*; DestDir: {app}\Plugins\GreenshotConfluencePlugin; Components: plugins\confluence; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
;Source: ..\..\bin\Release\Languages\Plugins\GreenshotConfluencePlugin\*; DestDir: {app}\Languages\Plugins\GreenshotConfluencePlugin; Components: plugins\confluence; Flags: overwritereadonly ignoreversion replacesameversion;
;ExternalCommand Plugin
Source: ..\..\bin\Release\Plugins\GreenshotExternalCommandPlugin\*; DestDir: {app}\Plugins\GreenshotExternalCommandPlugin; Components: plugins\externalcommand; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
;Network Import Plugin
;Source: ..\..\bin\Release\Plugins\GreenshotNetworkImportPlugin\*; DestDir: {app}\Plugins\GreenshotNetworkImportPlugin; Components: plugins\networkimport; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
[Setup]
; changes associations is used when the installer installs new extensions, it clears the explorer icon cache
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
ArchitecturesInstallIn64BitMode=x64
Compression=lzma2/ultra64
SolidCompression=yes
DefaultDirName={pf}\{#ExeName}
DefaultGroupName={#ExeName}
InfoBeforeFile=..\additional_files\readme.txt
LicenseFile=..\additional_files\license.txt
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
de.startup={#ExeName} starten wenn Windows hochfährt
nl.startup=Start {#ExeName} wanneer Windows opstart
en.startgreenshot=Start {#ExeName}
de.startgreenshot={#ExeName} starten
nl.startgreenshot=Start {#ExeName}
en.gswebsite=Open {#ExeName}'s homepage
de.gswebsite={#ExeName} Homepage öffnen
nl.gswebsite=De {#ExeName} homepage openen
en.jira=Jira plug-in
de.jira=Jira Plug-in
nl.jira=Jira plug-in
en.confluence=Confluence plug-in
de.confluence=Confluence Plug-in
nl.confluence=Confluence plug-in
en.externalcommand=Open with external command plug-in
de.externalcommand=Öffne mit ein externem Kommando Plug-in
nl.externalcommand=Open met externes commando plug-in
en.ocr=OCR plug-in (needs Microsoft Office Document Imaging (MODI))
de.ocr=OCR Plug-in (benötigt Microsoft Office Document Imaging (MODI))
nl.ocr=OCR plug-in (heeft Microsoft Office Document Imaging (MODI) nodig)
en.imgur=Imgur plug-in (See: http://imgur.com)
de.imgur=Imgur Plug-in (Siehe: http://imgur.com)
nl.imgur=Imgur plug-in (Zie: http://imgur.com)
en.language=Additional languages
de.language=Zusätzliche Sprachen
nl.language=Extra talen
en.optimize=Optimizing performance, this may take a while.
de.optimize=Optimierung der Leistung, kann etwas dauern.
nl.optimize=Prestaties verbeteren, kan even duren.
en.supportus_caption=Support Greenshot
de.supportus_caption=Unterstütze Greenshot
nl.supportus_caption=Ondersteun Greenshot
en.supportus_description=Things you can do to support Greenshot
de.supportus_description=Was Sie tun können um Greenshot zu unterstützen
nl.supportus_description=Wat U doen kunt om Greenshot te ondersteuen
en.supportus_text=Support Greenshot with a donation
de.supportus_text=Unterstütze Greenshot mit einen Spende
nl.supportus_text=Ondersteun Greenshot met een donatie
[Components]
Name: "plugins"; Description: "Plugins"; Types: Full
Name: "plugins\ocr"; Description: {cm:ocr}; Types: Full;
Name: "plugins\jira"; Description: {cm:jira}; Types: Full
Name: "plugins\imgur"; Description: {cm:imgur}; Types: Full;
;Name: "plugins\confluence"; Description: {cm:confluence}; Types: Full
Name: "plugins\externalcommand"; Description: {cm:externalcommand}; Types: Full
;Name: "plugins\networkimport"; Description: "Network Import Plugin"; Types: Full
;Name: "plugins\box"; Description: "Box Plugin"; Types: Full
;Name: "plugins\dropbox"; Description: "DropBox Plugin"; Types: Full
;Name: "plugins\flickr"; Description: "Flickr Plugin"; Types: Full
;Name: "plugins\picasa"; Description: "Picasa Plugin"; Types: Full
Name: "languages"; Description: {cm:language}; Types: Full
Name: "languages\arSY"; Description: "العربية"; Types: Full; Check: hasLanguageGroup('d')
Name: "languages\csCZ"; Description: "Ceština"; Types: Full; Check: hasLanguageGroup('1')
Name: "languages\elGR"; Description: "ελληνικά"; Types: Full; Check: hasLanguageGroup('4')
Name: "languages\esES"; Description: "Español"; Types: Full; Check: hasLanguageGroup('1')
Name: "languages\faIR"; Description: "پارسی"; Types: Full; Check: hasLanguageGroup('d')
Name: "languages\fiFI"; Description: "Suomi"; Types: Full; Check: hasLanguageGroup('1')
Name: "languages\frFR"; Description: "Français"; Types: Full; Check: hasLanguageGroup('1')
Name: "languages\heIL"; Description: "עִבְרִית"; Types: Full; Check: hasLanguageGroup('c')
Name: "languages\huHU"; Description: "Magyar"; Types: Full; Check: hasLanguageGroup('2')
Name: "languages\itIT"; Description: "Italiano"; Types: Full; Check: hasLanguageGroup('1')
Name: "languages\jaJP"; Description: "日本語"; Types: Full; Check: hasLanguageGroup('7')
Name: "languages\koKR"; Description: "한국의"; Types: Full; Check: hasLanguageGroup('8')
Name: "languages\ltLT"; Description: "Lietuvių"; Types: Full; Check: hasLanguageGroup('3')
Name: "languages\plPL"; Description: "Polski"; Types: Full; Check: hasLanguageGroup('2')
Name: "languages\ptBR"; Description: "Português do Brasil"; Types: Full; Check: hasLanguageGroup('1')
Name: "languages\ptPT"; Description: "Português de Portugal"; Types: Full; Check: hasLanguageGroup('1')
Name: "languages\ruRU"; Description: "Pусский"; Types: Full; Check: hasLanguageGroup('5')
Name: "languages\svSE"; Description: "Svenska"; Types: Full; Check: hasLanguageGroup('1')
Name: "languages\trTR"; Description: "Türk"; Types: Full; Check: hasLanguageGroup('6')
Name: "languages\ukUA"; Description: "Українська"; Types: Full; Check: hasLanguageGroup('5')
Name: "languages\viVN"; Description: "Việt"; Types: Full; Check: hasLanguageGroup('e')
Name: "languages\zhCN"; Description: "简体中文"; Types: Full; Check: hasLanguageGroup('a')
Name: "languages\zhTW"; Description: "繁體中文"; Types: Full; Check: hasLanguageGroup('9')
[Code]

// Build a list of greenshot parameters from the supplied installer parameters
function GetParamsForGS(argument: String): String;
var
	i: Integer;
	parametersString: String;
	currentParameter: String;
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

// Check if language group is installed
function hasLanguageGroup(argument: String): Boolean;
var
	keyValue: String;
	returnValue: Boolean;
begin
	returnValue := true;
	if (RegQueryStringValue( HKLM, 'SYSTEM\CurrentControlSet\Control\Nls\Language Groups', argument, keyValue)) then begin
		if Length(keyValue) = 0 then begin
			returnValue := false;
		end;
	end;
	Result := returnValue;
end;

// Initialize the setup
function InitializeSetup(): Boolean;
begin
	// Enhance installer otherwise .NET installations won't work
	msi20('2.0');
	msi31('3.0');
	
	//install .netfx 2.0 sp2 if possible; if not sp1 if possible; if not .netfx 2.0
	if minwinversion(5, 1) then begin
		dotnetfx20sp2();
	end else begin
		if minwinversion(5, 0) and minwinspversion(5, 0, 4) then begin
			// kb835732();
			dotnetfx20sp1();
		end else begin
			dotnetfx20();
		end;
	end;
	Result := true;
end;
[Run]
Filename: "{dotnet20}\ngen.exe"; Parameters: "install ""{app}\{#ExeName}.exe"""; StatusMsg: "{cm:optimize}"; Flags: runhidden;
Filename: "{dotnet20}\ngen.exe"; Parameters: "install ""{app}\GreenshotPlugin.dll"""; StatusMsg: "{cm:optimize}"; Flags: runhidden;
Filename: "{app}\{#ExeName}.exe"; Description: "{cm:startgreenshot}"; Parameters: "{code:GetParamsForGS}"; WorkingDir: "{app}"; Flags: nowait postinstall runasoriginaluser
Filename: "http://getgreenshot.org/support/"; Description: "{cm:gswebsite}"; Flags: shellexec runascurrentuser

[InstallDelete]
Name: {app}; Type: filesandordirs;

[UninstallRun]
Filename: "{dotnet20}\ngen.exe"; Parameters: "uninstall ""{app}\{#ExeName}.exe"""; StatusMsg: "Cleanup"; Flags: runhidden;
Filename: "{dotnet20}\ngen.exe"; Parameters: "uninstall ""{app}\GreenshotPlugin.dll"""; StatusMsg: "Cleanup"; Flags: runhidden;