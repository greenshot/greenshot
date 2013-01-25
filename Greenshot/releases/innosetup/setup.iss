#define ExeName "Greenshot"
#define Version "1.1.0.$WCREV$"

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
Source: ..\..\bin\Release\Greenshot.exe; DestDir: {app}; Components: greenshot; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\..\bin\Release\GreenshotPlugin.dll; DestDir: {app}; Components: greenshot; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\..\bin\Release\Greenshot.exe.config; DestDir: {app}; Components: greenshot; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\..\bin\Release\log4net.dll; DestDir: {app}; Components: greenshot; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\..\log4net.xml; DestDir: {app}; Components: greenshot; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\..\bin\Release\checksum.MD5; DestDir: {app}; Components: greenshot; Flags: overwritereadonly ignoreversion replacesameversion
;Source: ..\greenshot-defaults.ini; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\additional_files\installer.txt; DestDir: {app}; Components: greenshot; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion
Source: ..\additional_files\license.txt; DestDir: {app}; Components: greenshot; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion
Source: ..\additional_files\readme.txt; DestDir: {app}; Components: greenshot; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion

; Core language files
Source: ..\..\Languages\*nl-NL*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: greenshot; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*en-US*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: greenshot; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*de-DE*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: greenshot; Flags: overwritereadonly ignoreversion replacesameversion;

; Additional language files
Source: ..\..\Languages\*ar-SY*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\arSY; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*cs-CZ*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\csCZ; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*da-DK*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\daDK; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*de-x-franconia*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\dexfranconia; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*el-GR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\elGR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*es-ES*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\esES; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*fa-IR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\faIR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*fi-FI*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\fiFI; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*fr-FR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\frFR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*fr-QC*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\frQC; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*he-IL*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\heIL; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*hu-HU*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\huHU; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*it-IT*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\itIT; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*ja-JP*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\jaJP; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*ko-KR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\koKR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*lt-LT*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ltLT; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*nn-NO*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\nnNO; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*pl-PL*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\plPL; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*pt-BR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ptBR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*pt-PT*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ptPT; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*ro-RO*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\roRO; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*ru-RU*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ruRU; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*sk-SK*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\skSK; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*sl-SI*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\slSI; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*sr-RS*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\srRS; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*sv-SE*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\svSE; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*tr-TR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\trTR; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*uk-UA*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ukUA; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*vi-VN*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\viVN; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*zh-CN*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\zhCN; Flags: overwritereadonly ignoreversion replacesameversion;
Source: ..\..\Languages\*zh-TW*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\zhTW; Flags: overwritereadonly ignoreversion replacesameversion;

;Office Plugin
Source: ..\..\bin\Release\Plugins\GreenshotOfficePlugin\GreenshotOfficePlugin.gsp; DestDir: {app}\Plugins\GreenshotOfficePlugin; Components: plugins\office; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
;OCR Plugin
Source: ..\..\bin\Release\Plugins\GreenshotOCRPlugin\GreenshotOCRPlugin.gsp; DestDir: {app}\Plugins\GreenshotOCRPlugin; Components: plugins\ocr; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Plugins\GreenshotOCRPlugin\GreenshotOCRCommand.exe; DestDir: {app}\Plugins\GreenshotOCRPlugin; Components: plugins\ocr; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotOCRPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotOCRPlugin; Components: plugins\ocr; Flags: overwritereadonly ignoreversion replacesameversion;
;JIRA Plugin
Source: ..\..\bin\Release\Plugins\GreenshotJiraPlugin\GreenshotJiraPlugin.gsp; DestDir: {app}\Plugins\GreenshotJiraPlugin; Components: plugins\jira; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotJiraPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotJiraPlugin; Components: plugins\jira; Flags: overwritereadonly ignoreversion replacesameversion;
;Imgur Plugin
Source: ..\..\bin\Release\Plugins\GreenshotImgurPlugin\GreenshotImgurPlugin.gsp; DestDir: {app}\Plugins\GreenshotImgurPlugin; Components: plugins\imgur; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotImgurPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotImgurPlugin; Components: plugins\imgur; Flags: overwritereadonly ignoreversion replacesameversion;
;Box Plugin
Source: ..\..\bin\Release\Plugins\GreenshotBoxPlugin\GreenshotBoxPlugin.gsp; DestDir: {app}\Plugins\GreenshotBoxPlugin; Components: plugins\box; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotBoxPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotBoxPlugin; Components: plugins\box; Flags: overwritereadonly ignoreversion replacesameversion;
;DropBox Plugin
Source: ..\..\bin\Release\Plugins\GreenshotDropBoxPlugin\GreenshotDropboxPlugin.gsp; DestDir: {app}\Plugins\GreenshotDropBoxPlugin; Components: plugins\dropbox; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotDropBoxPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotDropBoxPlugin; Components: plugins\dropbox; Flags: overwritereadonly ignoreversion replacesameversion;
;Flickr Plugin
Source: ..\..\bin\Release\Plugins\GreenshotFlickrPlugin\GreenshotFlickrPlugin.gsp; DestDir: {app}\Plugins\GreenshotFlickrPlugin; Components: plugins\flickr; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotFlickrPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotFlickrPlugin; Components: plugins\flickr; Flags: overwritereadonly ignoreversion replacesameversion;
;Photobucket Plugin
Source: ..\..\bin\Release\Plugins\GreenshotPhotobucketPlugin\GreenshotPhotobucketPlugin.gsp; DestDir: {app}\Plugins\GreenshotPhotobucketPlugin; Components: plugins\photobucket; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotPhotobucketPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotPhotobucketPlugin; Components: plugins\photobucket; Flags: overwritereadonly ignoreversion replacesameversion;
;Picasa Plugin
Source: ..\..\bin\Release\Plugins\GreenshotPicasaPlugin\GreenshotPicasaPlugin.gsp; DestDir: {app}\Plugins\GreenshotPicasaPlugin; Components: plugins\picasa; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotPicasaPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotPicasaPlugin; Components: plugins\picasa; Flags: overwritereadonly ignoreversion replacesameversion;
;Confluence Plugin
Source: ..\..\bin\Release\Plugins\GreenshotConfluencePlugin\GreenshotConfluencePlugin.gsp; DestDir: {app}\Plugins\GreenshotConfluencePlugin; Components: plugins\confluence; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotConfluencePlugin\*; DestDir: {app}\Languages\Plugins\GreenshotConfluencePlugin; Components: plugins\confluence; Flags: overwritereadonly ignoreversion replacesameversion;
;ExternalCommand Plugin
Source: ..\..\bin\Release\Plugins\GreenshotExternalCommandPlugin\GreenshotExternalCommandPlugin.gsp; DestDir: {app}\Plugins\GreenshotExternalCommandPlugin; Components: plugins\externalcommand; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
Source: ..\..\bin\Release\Languages\Plugins\GreenshotExternalCommandPlugin\*; DestDir: {app}\Languages\Plugins\GreenshotExternalCommandPlugin; Components: plugins\externalcommand; Flags: overwritereadonly ignoreversion replacesameversion;
;Network Import Plugin
;Source: ..\..\bin\Release\Plugins\GreenshotNetworkImportPlugin\*; DestDir: {app}\Plugins\GreenshotNetworkImportPlugin; Components: plugins\networkimport; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion;
[Setup]
; changes associations is used when the installer installs new extensions, it clears the explorer icon cache
ChangesAssociations=yes
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
DefaultDirName={code:DefDirRoot}\{#ExeName}
DefaultGroupName={#ExeName}
InfoBeforeFile=..\additional_files\readme.txt
LicenseFile=..\additional_files\license.txt
LanguageDetectionMethod=uilanguage
MinVersion=,5.01.2600
OutputBaseFilename={#ExeName}-INSTALLER-{#Version}-UNSTABLE
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
Root: HKCU; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror;
Root: HKLM; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror;
Root: HKCU32; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror; Check: IsWin64()
Root: HKLM32; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror; Check: IsWin64()
; Create the startup entries if requested to do so
; HKEY_LOCAL_USER - for current user only
Root: HKCU; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: string; ValueName: {#ExeName}; ValueData: {app}\{#ExeName}.exe; Permissions: users-modify; Flags: uninsdeletevalue noerror; Tasks: startup; Check: IsRegularUser
; HKEY_LOCAL_MACHINE - for all users
Root: HKLM; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: string; ValueName: {#ExeName}; ValueData: {app}\{#ExeName}.exe; Permissions: users-modify; Flags: uninsdeletevalue noerror; Tasks: startup; Check: not IsRegularUser
; Register our own filetype for admin
Root: HKLM; Subkey: Software\Classes\.greenshot; ValueType: string; ValueName: ""; ValueData: "Greenshot"; Flags: uninsdeletevalue noerror; Check: not IsRegularUser
Root: HKLM; Subkey: Software\Classes\Greenshot; ValueType: string; ValueName: ""; ValueData: "Greenshot File"; Flags: uninsdeletevalue noerror; Check: not IsRegularUser
Root: HKLM; Subkey: Software\Classes\Greenshot\DefaultIcon; ValueType: string; ValueName: ""; ValueData: "{app}\Greenshot.EXE,0"; Flags: uninsdeletevalue noerror; Check: not IsRegularUser
Root: HKLM; Subkey: Software\Classes\Greenshot\shell\open\command; ValueType: string; ValueName: ""; ValueData: """{app}\Greenshot.EXE"" --openfile ""%1"""; Flags: uninsdeletevalue noerror; Check: not IsRegularUser
; Register our own filetype for normal user
Root: HKCU; Subkey: Software\Classes\.greenshot; ValueType: string; ValueName: ""; ValueData: "Greenshot"; Flags: uninsdeletevalue noerror; Check: IsRegularUser
Root: HKCU; Subkey: Software\Classes\Greenshot; ValueType: string; ValueName: ""; ValueData: "Greenshot File"; Flags: uninsdeletevalue noerror; Check: IsRegularUser
Root: HKCU; Subkey: Software\Classes\Greenshot\DefaultIcon; ValueType: string; ValueName: ""; ValueData: "{app}\Greenshot.EXE,0"; Flags: uninsdeletevalue noerror; Check: IsRegularUser
Root: HKCU; Subkey: Software\Classes\Greenshot\shell\open\command; ValueType: string; ValueName: ""; ValueData: """{app}\Greenshot.EXE"" --openfile ""%1"""; Flags: uninsdeletevalue noerror; Check: IsRegularUser
[Icons]
Name: {group}\{#ExeName}; Filename: {app}\{#ExeName}.exe; WorkingDir: {app}
Name: {group}\Uninstall {#ExeName}; Filename: {uninstallexe}; WorkingDir: {app}; AppUserModelID: "{#ExeName}.{#ExeName}"
Name: {group}\Readme.txt; Filename: {app}\readme.txt; WorkingDir: {app}
Name: {group}\License.txt; Filename: {app}\license.txt; WorkingDir: {app}
[Languages]
Name: en; MessagesFile: compiler:Default.isl
Name: cn; MessagesFile: compiler:Languages\ChineseSimplified.isl
Name: de; MessagesFile: compiler:Languages\German.isl
Name: es; MessagesFile: compiler:Languages\Spanish.isl
Name: fr; MessagesFile: compiler:Languages\French.isl
Name: nl; MessagesFile: compiler:Languages\Dutch.isl
Name: nn; MessagesFile: compiler:Languages\NorwegianNynorsk.isl
Name: sr; MessagesFile: compiler:Languages\SerbianCyrillic.isl

[Tasks]
Name: startup; Description: {cm:startup}

[CustomMessages]

de.confluence=Confluence Plug-in
de.default=Standard installation
en.office=Microsoft Office Plug-in
de.externalcommand=Öffne mit ein externem Kommando Plug-in
de.imgur=Imgur Plug-in (Siehe: http://imgur.com)
de.jira=Jira Plug-in
de.language=Zusätzliche Sprachen
de.ocr=OCR Plug-in (benötigt Microsoft Office Document Imaging (MODI))
de.optimize=Optimierung der Leistung, kann etwas dauern.
de.startgreenshot={#ExeName} starten
de.startup={#ExeName} starten wenn Windows hochfährt

en.confluence=Confluence plug-in
en.default=Default installation
en.office=Microsoft Office plug-in
en.externalcommand=Open with external command plug-in
en.imgur=Imgur plug-in (See: http://imgur.com)
en.jira=Jira plug-in
en.language=Additional languages
en.ocr=OCR plug-in (needs Microsoft Office Document Imaging (MODI))
en.optimize=Optimizing performance, this may take a while.
en.startgreenshot=Start {#ExeName}
en.startup=Start {#ExeName} with Windows start

es.confluence=Extensión para Confluence
es.default=${default}
es.externalcommand=Extensión para abrir con programas externos
es.imgur=Extensión para Imgur (Ver http://imgur.com)
es.jira=Extensión para Jira
es.language=Idiomas adicionales
es.ocr=Extensión para OCR (necesita Microsoft Office Document Imaging (MODI))
es.optimize=Optimizando rendimiento; por favor, espera.
es.startgreenshot=Lanzar {#ExeName}
es.startup=Lanzar {#ExeName} al iniciarse Windows

fr.confluence=Greffon Confluence
fr.default=${default}
fr.office=Greffon Microsoft Office
fr.externalcommand=Ouvrir avec le greffon de commande externe
fr.imgur=Greffon Imgur (Voir: http://imgur.com)
fr.jira=Greffon Jira
fr.language=Langues additionnelles
fr.ocr=Greffon OCR (nécessite Document Imaging de Microsoft Office [MODI])
fr.optimize=Optimisation des performances, Ceci peut prendre un certain temps.
fr.startgreenshot=Démarrer {#ExeName}
fr.startup=Lancer {#ExeName} au démarrage de Windows

nl.confluence=Confluence plug-in
nl.default=Default installation
nl.office=Microsoft Office plug-in
nl.externalcommand=Open met externes commando plug-in
nl.imgur=Imgur plug-in (Zie: http://imgur.com)
nl.jira=Jira plug-in
nl.language=Extra talen
nl.ocr=OCR plug-in (heeft Microsoft Office Document Imaging (MODI) nodig)
nl.optimize=Prestaties verbeteren, kan even duren.
nl.startgreenshot=Start {#ExeName}
nl.startup=Start {#ExeName} wanneer Windows opstart

nn.confluence=Confluence-tillegg
nn.default=Default installation 
nn.office=Microsoft Office Tillegg
nn.externalcommand=Tillegg for å opne med ekstern kommando
nn.imgur=Imgur-tillegg (sjå http://imgur.com)
nn.jira=Jira-tillegg
nn.language=Andre språk
nn.ocr=OCR-tillegg (krev Microsoft Office Document Imaging (MODI))
nn.optimize=Optimaliserar ytelse, dette kan ta litt tid...
nn.startgreenshot=Start {#ExeName}
nn.startup=Start {#ExeName} når Windows startar

sr.confluence=Прикључак за Конфлуенс
sr.default=${default}
sr.externalcommand=Отвори са прикључком за спољне наредбе
sr.imgur=Прикључак за Имиџер (http://imgur.com)
sr.jira=Прикључак за Џиру
sr.language=Додатни језици
sr.ocr=OCR прикључак (захтева Microsoft Office Document Imaging (MODI))
sr.optimize=Оптимизујем перформансе…
sr.startgreenshot=Покрени Гриншот
sr.startup=Покрени програм са системом

cn.confluence=Confluence插件
cn.default=${default}
cn.externalcommand=使用外部命令打开插件
cn.imgur=Imgur插件( (请访问： http://imgur.com))
cn.jira=Jira插件
cn.language=其它语言
cn.ocr=OCR插件(需要Microsoft Office Document Imaging (MODI)的支持)
cn.optimize=正在优化性能，这可能需要一点时间。
cn.startgreenshot=启动{#ExeName}
cn.startup=让{#ExeName}随Windows一起启动


[Types]
Name: "default"; Description: "{cm:default}"
Name: "full"; Description: "{code:FullInstall}"
Name: "compact"; Description: "{code:CompactInstall}"
Name: "custom"; Description: "{code:CustomInstall}"; Flags: iscustom

[Components]
Name: "greenshot"; Description: "Greenshot"; Types: default full compact custom; Flags: fixed
Name: "plugins\office"; Description: {cm:office}; Types: default full custom; Flags: disablenouninstallwarning 
Name: "plugins\ocr"; Description: {cm:ocr}; Types: default full custom; Flags: disablenouninstallwarning 
Name: "plugins\jira"; Description: {cm:jira}; Types: full custom; Flags: disablenouninstallwarning 
Name: "plugins\imgur"; Description: {cm:imgur}; Types: default full custom; Flags: disablenouninstallwarning 
Name: "plugins\confluence"; Description: {cm:confluence}; Types: full custom; Flags: disablenouninstallwarning; Check: hasDotNet35FullOrHigher()
Name: "plugins\externalcommand"; Description: {cm:externalcommand}; Types: default full custom; Flags: disablenouninstallwarning 
;Name: "plugins\networkimport"; Description: "Network Import Plugin"; Types: full
Name: "plugins\box"; Description: "Box Plugin"; Types: full custom; Flags: disablenouninstallwarning
Name: "plugins\dropbox"; Description: "Dropbox Plugin"; Types: full custom; Flags: disablenouninstallwarning
Name: "plugins\flickr"; Description: "Flickr Plugin"; Types: full custom; Flags: disablenouninstallwarning 
Name: "plugins\picasa"; Description: "Picasa Plugin"; Types: full custom; Flags: disablenouninstallwarning 
Name: "plugins\photobucket"; Description: "Photobucket Plugin"; Types: full custom; Flags: disablenouninstallwarning 
Name: "languages"; Description: {cm:language}; Types: full custom; Flags: disablenouninstallwarning
Name: "languages\arSY"; Description: "العربية"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('d')
Name: "languages\csCZ"; Description: "Ceština"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\daDK"; Description: "Dansk"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\dexfranconia"; Description: "Frängisch (Deutsch)"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\elGR"; Description: "ελληνικά"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('4')
Name: "languages\esES"; Description: "Español"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\faIR"; Description: "پارسی"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('d')
Name: "languages\fiFI"; Description: "Suomi"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\frFR"; Description: "Français"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\frQC"; Description: "Français - Québec"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\heIL"; Description: "עִבְרִית"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('c')
Name: "languages\huHU"; Description: "Magyar"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\itIT"; Description: "Italiano"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\jaJP"; Description: "日本語"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('7')
Name: "languages\koKR"; Description: "한국의"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('8')
Name: "languages\ltLT"; Description: "Lietuvių"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('3')
Name: "languages\nnNO"; Description: "Nynorsk"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\plPL"; Description: "Polski"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\ptBR"; Description: "Português do Brasil"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\ptPT"; Description: "Português de Portugal"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\ruRU"; Description: "Pусский"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('5')
Name: "languages\roRO"; Description: "Română"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\skSK"; Description: "Slovenčina"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\slSI"; Description: "Slovenščina"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\srRS"; Description: "Српски"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('5')
Name: "languages\svSE"; Description: "Svenska"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\trTR"; Description: "Türk"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('6')
Name: "languages\ukUA"; Description: "Українська"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('5')
Name: "languages\viVN"; Description: "Việt"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('e')
Name: "languages\zhCN"; Description: "简体中文"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('a')
Name: "languages\zhTW"; Description: "繁體中文"; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('9')
[Code]
// Do we have a regular user trying to install this?
function IsRegularUser(): Boolean;
begin
	Result := not (IsAdminLoggedOn or IsPowerUserLoggedOn);
end;

// The following code is used to select the installation path, this is localappdata if non poweruser
function DefDirRoot(Param: String): String;
begin
	if IsRegularUser then
		Result := ExpandConstant('{localappdata}')
	else
		Result := ExpandConstant('{pf}')
end;
	

function FullInstall(Param : String) : String;
begin
	result := SetupMessage(msgFullInstallation);
end;

function CustomInstall(Param : String) : String;
begin
	result := SetupMessage(msgCustomInstallation);
end;

function CompactInstall(Param : String) : String;
begin
	result := SetupMessage(msgCompactInstallation);
end;
/////////////////////////////////////////////////////////////////////
// The following uninstall code was found at:
// http://stackoverflow.com/questions/2000296/innosetup-how-to-automatically-uninstall-previous-installed-version
// and than modified to work in a 32/64 bit environment
/////////////////////////////////////////////////////////////////////
function GetUninstallStrings(): array of String;
var
	sUnInstPath: String;
	sUnInstallString: String;
	asUninstallStrings : array of String;
	index : Integer;
begin
	sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
	sUnInstallString := '';
	index := 0;
	
	// Retrieve uninstall string from HKLM32 or HKCU32
	if RegQueryStringValue(HKLM32, sUnInstPath, 'UninstallString', sUnInstallString) then
	begin
		SetArrayLength(asUninstallStrings, index + 1);
		asUninstallStrings[index] := sUnInstallString;
		index := index +1;
	end;

	if RegQueryStringValue(HKCU32, sUnInstPath, 'UninstallString', sUnInstallString) then
	begin
		SetArrayLength(asUninstallStrings, index + 1);
		asUninstallStrings[index] := sUnInstallString;
		index := index +1;
	end;
	
	// Only for Windows with 64 bit support: Retrieve uninstall string from HKLM64 or HKCU64
	if IsWin64 then
	begin
		if RegQueryStringValue(HKLM64, sUnInstPath, 'UninstallString', sUnInstallString) then
		begin
			SetArrayLength(asUninstallStrings, index + 1);
			asUninstallStrings[index] := sUnInstallString;
			index := index +1;
		end;

		if RegQueryStringValue(HKCU64, sUnInstPath, 'UninstallString', sUnInstallString) then
		begin
			SetArrayLength(asUninstallStrings, index + 1);
			asUninstallStrings[index] := sUnInstallString;
			index := index +1;
		end;
	end;
	Result := asUninstallStrings;
end;

/////////////////////////////////////////////////////////////////////
procedure UnInstallOldVersions();
var
	sUnInstallString: String;
	index: Integer;
	isUninstallMade: Boolean;
	iResultCode : Integer;
	asUninstallStrings : array of String;
begin
	isUninstallMade := false;
	asUninstallStrings := GetUninstallStrings();
	for index := 0 to (GetArrayLength(asUninstallStrings) -1) do
	begin
		sUnInstallString := RemoveQuotes(asUninstallStrings[index]);
		if Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
			isUninstallMade := true;
	end;

	// Wait a few seconds to prevent installation issues, otherwise files are removed in one process while the other tries to link to them
	if (isUninstallMade) then
		Sleep(2000);
end;

/////////////////////////////////////////////////////////////////////
procedure CurStepChanged(CurStep: TSetupStep);
begin
	if (CurStep=ssInstall) then
	begin
		UnInstallOldVersions();
	end;
end;
/////////////////////////////////////////////////////////////////////
// End of unstall code
/////////////////////////////////////////////////////////////////////

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

function hasDotNet(version: string; service: cardinal): Boolean;
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1.4322'     .NET Framework 1.1
//    'v2.0.50727'    .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
var
    key: string;
    install, serviceCount: cardinal;
    success: boolean;
begin
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + version;
    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;
    // .NET 4.0 uses value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;
    result := success and (install = 1) and (serviceCount >= service);
end;

function hasDotNet20() : boolean;
begin
	Result := hasDotNet('v2.0.50727',0);
end;

function hasDotNet40() : boolean;
begin
	Result := hasDotNet('v4\Client',0) or hasDotNet('v4\Full',0);
end;

function hasDotNet35FullOrHigher() : boolean;
begin
	Result := hasDotNet('v3.5',0) or hasDotNet('v4\Full',0) or hasDotNet('4.5\Full',0);
end;

function hasDotNet35OrHigher() : boolean;
begin
	Result := hasDotNet('v3.5',0) or hasDotNet('v4\Client',0) or hasDotNet('v4\Full',0) or hasDotNet('4.5\Client',0) or hasDotNet('4.5\Full',0);
end;

function getNGENPath(argument: String) : String;
var
	installPath: string;
begin
	if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4.5\Client', 'InstallPath', installPath) then begin
		if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4.5\Full', 'InstallPath', installPath) then begin
			if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client', 'InstallPath', installPath) then begin
				if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'InstallPath', installPath) then begin
					// 3.5 doesn't have NGEN and is using the .net 2.0 installation
					installPath := ExpandConstant('{dotnet20}');
				end;
			end;
		end;
	end;
	Result := installPath;
end;

// Initialize the setup
function InitializeSetup(): Boolean;
begin
	// Only check for 2.0 and install if we don't have .net 3.5 or higher
	if not hasDotNet35OrHigher() then
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
	end;
	Result := true;
end;
[Run]
Filename: "{code:getNGENPath}\ngen.exe"; Parameters: "install ""{app}\{#ExeName}.exe"""; StatusMsg: "{cm:optimize}"; Flags: runhidden runasoriginaluser
Filename: "{code:getNGENPath}\ngen.exe"; Parameters: "install ""{app}\GreenshotPlugin.dll"""; StatusMsg: "{cm:optimize}"; Flags: runhidden runasoriginaluser
Filename: "{app}\{#ExeName}.exe"; Description: "{cm:startgreenshot}"; Parameters: "{code:GetParamsForGS}"; WorkingDir: "{app}"; Flags: nowait postinstall runasoriginaluser
Filename: "http://getgreenshot.org/thank-you/?language={language}&version={#Version}"; Flags: shellexec runasoriginaluser

[InstallDelete]
Name: {app}; Type: filesandordirs;

[UninstallRun]
Filename: "{code:GetNGENPath}\ngen.exe"; Parameters: "uninstall ""{app}\{#ExeName}.exe"""; StatusMsg: "Cleanup"; Flags: runhidden
Filename: "{code:GetNGENPath}\ngen.exe"; Parameters: "uninstall ""{app}\GreenshotPlugin.dll"""; StatusMsg: "Cleanup"; Flags: runhidden
