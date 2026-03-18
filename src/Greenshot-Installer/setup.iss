#define ExeName "Greenshot"
; Basic build version determined by nerdbank gitversioning, e.g. 1.2.345
#define Version GetEnv('BuildVersionSimple')
; Build version with optional suffix depending on branch, e.g. 1.2.345-g1tc033174ef
#define VersionEnhanced GetEnv('BuildVersionEnhanced')
#define SolutionDir ".."
#define GreenshotProjectDir "..\Greenshot"
#define LanguagesDir "..\Greenshot\Languages"
#define BinDir "bin\Release\net480"
#define ReleaseDir "..\Greenshot\bin\Release\net480"
#define PluginDir "..\Greenshot\bin\Release\net480\Plugins"
#define CertumThumbprint GetEnv('CertumThumbprint')
#define DefaultInstallFlags "overwritereadonly ignoreversion"

[Files]
Source: {#ReleaseDir}\Greenshot.exe; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Greenshot.Base.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Greenshot.Editor.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Greenshot.exe.config; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\log4net.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\log4net.xml; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.*.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\SixLabors.ImageSharp.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\SixLabors.ImageSharp.Drawing.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\SixLabors.Fonts.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.*.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Svg.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\ExCSS.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\HtmlAgilityPack.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Newtonsoft.Json.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Microsoft.Toolkit.*.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Microsoft.IO.RecyclableMemoryStream.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\checksum.SHA256; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Twemoji.Mozilla.ttf; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\emojis.xml; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: additional_files\installer.txt; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: additional_files\license.txt; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: additional_files\readme.txt; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}

; Core language files
Source: {#LanguagesDir}\*nl-NL*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: greenshot; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*en-US*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: greenshot; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*de-DE*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: greenshot; Flags: {#DefaultInstallFlags};

; Additional language files
Source: {#LanguagesDir}\*ar-SY*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\arSY; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*ca-CA*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\caCA; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*cs-CZ*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\csCZ; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*da-DK*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\daDK; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*de-x-franconia*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\dexfranconia; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*el-GR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\elGR; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*es-ES*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\esES; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*et-EE*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\etEE; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*fa-IR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\faIR; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*fi-FI*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\fiFI; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*fr-FR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\frFR; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*fr-QC*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\frQC; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*he-IL*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\heIL; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*hu-HU*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\huHU; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*id-ID*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\idID; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*it-IT*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\itIT; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*ja-JP*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\jaJP; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*ko-KR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\koKR; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*kab-DZ*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\kabDZ; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*lt-LT*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ltLT; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*lv-LV*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\lvLV; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*nn-NO*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\nnNO; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*pl-PL*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\plPL; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*pt-BR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ptBR; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*pt-PT*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ptPT; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*ro-RO*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\roRO; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*ru-RU*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ruRU; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*sk-SK*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\skSK; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*sl-SI*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\slSI; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*sr-RS*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\srRS; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*sv-SE*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\svSE; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*tr-TR*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\trTR; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*uk-UA*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\ukUA; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*vi-VN*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\viVN; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*zh-CN*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\zhCN; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*zh-TW*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: languages\zhTW; Flags: {#DefaultInstallFlags};

;Office Plugin
Source: {#PluginDir}\Greenshot.Plugin.Office\Greenshot.Plugin.Office.dll; DestDir: {app}\Plugins\Office; Components: plugins\office; Flags: {#DefaultInstallFlags};
;JIRA Plugin
Source: {#PluginDir}\Greenshot.Plugin.Jira\*Jira*.dll; DestDir: {app}\Plugins\Jira; Components: plugins\jira; Flags: {#DefaultInstallFlags};
Source: {#PluginDir}\Greenshot.Plugin.Jira\Dapplo.HttpExtensions.WinForms.dll; DestDir: {app}\Plugins\Jira; Components: plugins\jira; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.Jira\Languages\language_jira*.xml; DestDir: {app}\Languages\Plugins\Jira; Components: plugins\jira; Flags: {#DefaultInstallFlags};
;Imgur Plugin
Source: {#PluginDir}\Greenshot.Plugin.Imgur\Greenshot.Plugin.Imgur.dll; DestDir: {app}\Plugins\Imgur; Components: plugins\imgur; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.Imgur\Languages\language_imgur*.xml; DestDir: {app}\Languages\Plugins\Imgur; Components: plugins\imgur; Flags: {#DefaultInstallFlags};
;Box Plugin
Source: {#PluginDir}\Greenshot.Plugin.Box\Greenshot.Plugin.Box.dll; DestDir: {app}\Plugins\Box; Components: plugins\box; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.Box\Languages\language_box*.xml; DestDir: {app}\Languages\Plugins\Box; Components: plugins\box; Flags: {#DefaultInstallFlags};
;DropBox Plugin
Source: {#PluginDir}\Greenshot.Plugin.DropBox\Greenshot.Plugin.DropBox.dll; DestDir: {app}\Plugins\DropBox; Components: plugins\dropbox; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.DropBox\Languages\language_dropbox*.xml; DestDir: {app}\Languages\Plugins\DropBox; Components: plugins\dropbox; Flags: {#DefaultInstallFlags};
;Confluence Plugin
Source: {#PluginDir}\Greenshot.Plugin.Confluence\Greenshot.Plugin.Confluence.dll; DestDir: {app}\Plugins\Confluence; Components: plugins\confluence; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.Confluence\Languages\language_confluence*.xml; DestDir: {app}\Languages\Plugins\Confluence; Components: plugins\confluence; Flags: {#DefaultInstallFlags};
;ExternalCommand Plugin
Source: {#PluginDir}\Greenshot.Plugin.ExternalCommand\Greenshot.Plugin.ExternalCommand.dll; DestDir: {app}\Plugins\ExternalCommand; Components: plugins\externalcommand; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.ExternalCommand\Languages\language_externalcommand*.xml; DestDir: {app}\Languages\Plugins\ExternalCommand; Components: plugins\externalcommand; Flags: {#DefaultInstallFlags};

[Setup]
; changes associations is used when the installer installs new extensions, it clears the explorer icon cache
ChangesAssociations=yes
; Use the Windows Restart Manager to close Greenshot gracefully before installation (triggering
; WM_QUERYENDSESSION so it can save open editor state) and to restart it afterwards using the
; command line arguments registered via RegisterApplicationRestart (i.e. --restore).
CloseApplications=yes
RestartApplications=yes
AppId={#ExeName}
AppName={#ExeName}
AppPublisher={#ExeName}
AppPublisherURL=https://getgreenshot.org
AppSupportURL=https://getgreenshot.org
AppUpdatesURL=https://getgreenshot.org
AppVerName={#ExeName} {#Version}
AppVersion={#Version}
ArchitecturesInstallIn64BitMode=x64
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

SetupIconFile=..\Greenshot\icons\applicationIcon\icon.ico
#if CertumThumbprint  != ""
 OutputBaseFilename={#ExeName}-INSTALLER-{#VersionEnhanced}-UNSTABLE
  SignTool=SignTool sign /sha1 "{#CertumThumbprint}" /tr http://time.certum.pl /td sha256 /fd sha256 /v $f
  SignedUninstaller=yes
#else
  OutputBaseFilename={#ExeName}-INSTALLER-{#VersionEnhanced}-UNSTABLE-UNSIGNED
#endif
UninstallDisplayIcon={app}\{#ExeName}.exe
Uninstallable=yes
VersionInfoCompany={#ExeName}
VersionInfoProductName={#ExeName}
VersionInfoProductTextVersion={#VersionEnhanced}
VersionInfoTextVersion={#VersionEnhanced}
VersionInfoVersion={#Version}
; Reference a bitmap, max size 164x314
WizardImageFile=installer-large.bmp
; Reference a bitmap, max size 55x58
WizardSmallImageFile=installer-small.bmp
WizardStyle=modern
UninstallDisplayName={#ExeName}

[Registry]
; Delete all startup entries, so we don't have leftover values
Root: HKCU; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror;
Root: HKLM; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror;
Root: HKCU32; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror; Check: IsWin64()
Root: HKLM32; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror; Check: IsWin64()
Root: HKCU64; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror; Check: IsWin64()
Root: HKLM64; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror; Check: IsWin64()

; delete filetype mappings
; HKEY_LOCAL_USER - for current user only
Root: HKCU; Subkey: Software\Classes\.greenshot; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror;
Root: HKCU; Subkey: Software\Classes\Greenshot; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror;
; HKEY_LOCAL_MACHINE - for all users when admin (with the noerror this doesn't matter)
Root: HKLM; Subkey: Software\Classes\.greenshot; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror;
Root: HKLM; Subkey: Software\Classes\Greenshot; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror;

; Create the startup entries if requested to do so
Root: HKA; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: string; ValueName: {#ExeName}; ValueData: """{app}\{#ExeName}.exe"""; Flags: uninsdeletevalue noerror; Tasks: startup

; Register our own filetype for all users
Root: HKA; Subkey: Software\Classes\.greenshot; ValueType: string; ValueName: ""; ValueData: "Greenshot"; Flags: uninsdeletevalue noerror
Root: HKA; Subkey: Software\Classes\Greenshot; ValueType: string; ValueName: ""; ValueData: "Greenshot File"; Flags: uninsdeletevalue noerror
Root: HKA; Subkey: Software\Classes\Greenshot\DefaultIcon; ValueType: string; ValueName: ""; ValueData: """{app}\Greenshot.EXE,0"""; Flags: uninsdeletevalue noerror
Root: HKA; Subkey: Software\Classes\Greenshot\shell\open\command; ValueType: string; ValueName: ""; ValueData: """{app}\Greenshot.EXE"" --openfile ""%1"""; Flags: uninsdeletevalue noerror

; Disable the default PRTSCR Snipping Tool in Windows 11
Root: HKCU; Subkey: Control Panel\Keyboard; ValueType: dword; ValueName: "PrintScreenKeyForSnippingEnabled"; ValueData: "0"; Flags: uninsdeletevalue; Check: ShouldDisableSnippingTool

[Icons]
Name: {group}\{#ExeName}; Filename: {app}\{#ExeName}.exe; WorkingDir: {app}; AppUserModelID: "{#ExeName}"
Name: {group}\{cm:UninstallIconDescription} {#ExeName}; Filename: {uninstallexe}; WorkingDir: {app};
Name: {group}\{cm:ShowReadme}; Filename: {app}\readme.txt; WorkingDir: {app}
Name: {group}\{cm:ShowLicense}; Filename: {app}\license.txt; WorkingDir: {app}

[Languages]
Name: en; MessagesFile: compiler:Default.isl
Name: cn; MessagesFile: Languages\ChineseSimplified.isl
Name: de; MessagesFile: compiler:Languages\German.isl
Name: es; MessagesFile: compiler:Languages\Spanish.isl
Name: fi; MessagesFile: compiler:Languages\Finnish.isl
Name: fr; MessagesFile: compiler:Languages\French.isl
Name: it; MessagesFile: compiler:Languages\Italian.isl
Name: nl; MessagesFile: compiler:Languages\Dutch.isl
Name: lt; MessagesFile: Languages\Latvian.isl
Name: nn; MessagesFile: Languages\NorwegianNynorsk.isl
Name: ru; MessagesFile: compiler:Languages\Russian.isl
Name: sr; MessagesFile: Languages\SerbianCyrillic.isl
Name: sv; MessagesFile: Languages\Swedish.isl
Name: tr; MessagesFile: compiler:Languages\Turkish.isl
Name: uk; MessagesFile: compiler:Languages\Ukrainian.isl

[Tasks]
Name: startup; Description: {cm:startup}

[CustomMessages]
;Language names in the original language
dexfranconia=Frängisch (Deutsch)
arSY=العربية
caCA=Català
csCZ=Čeština
daDK=Dansk
elGR=ελληνικά
esES=Español
etEE=Eesti
faIR=پارسی
fiFI=Suomi
frFR=Français
frQC=Français - Québec
heIL=עִבְרִית
huHU=Magyar
idID=Bahasa Indonesia
itIT=Italiano
jaJP=日本語
kabDZ=Taqbaylit
koKR=한국어
ltLT=Lietuvių
lvLV=Latviski
nnNO=Nynorsk
plPL=Polski
ptBR=Português do Brasil
ptPT=Português de Portugal
roRO=Română
ruRU=Pусский
skSK=Slovenčina
slSI=Slovenščina
srRS=Српски
svSE=Svenska
trTR=Türkçe
ukUA=Українська
viVN=Việt
zhCN=简体中文
zhTW=繁體中文

en.box=Box plug-in
en.confluence=Confluence plug-in
en.default=Default installation
en.dropbox=Dropbox plug-in
en.externalcommand=Open with external command plug-in
en.imgur=Imgur plug-in (See: https://imgur.com)
en.jira=Jira plug-in
en.language=Additional languages
en.office=Microsoft Office plug-in
en.optimize=Optimizing performance, this may take a while.
en.startgreenshot=Start {#ExeName}
en.startup=Start {#ExeName} with Windows start
en.UninstallIconDescription=Uninstall
en.ShowLicense=Show license
en.ShowReadme=Show Readme
en.disablewin11snippingtool=Disable Win11 default PrtScr snipping tool

de.confluence=Confluence Plug-in
de.default=Standard installation
de.externalcommand=Externes Kommando Plug-in
de.imgur=Imgur Plug-in (Siehe: https://imgur.com)
de.jira=Jira Plug-in
de.language=Zusätzliche Sprachen
de.office=Microsoft Office Plug-in
de.optimize=Optimierung der Leistung, kann etwas dauern.
de.startgreenshot={#ExeName} starten
de.startup={#ExeName} starten wenn Windows hochfährt
de.disablewin11snippingtool=Deaktiviere das Standard Windows 11 Snipping Tool auf "Druck"

es.confluence=Extensión para Confluence
es.default=${default}
es.externalcommand=Extensión para abrir con programas externos
es.imgur=Extensión para Imgur (Ver https://imgur.com)
es.jira=Extensión para Jira
es.language=Idiomas adicionales
es.optimize=Optimizando rendimiento; por favor, espera.
es.startgreenshot=Lanzar {#ExeName}
es.startup=Lanzar {#ExeName} al iniciarse Windows

fi.confluence=Confluence-liitännäinen
fi.default=${default}
fi.externalcommand=Avaa Ulkoinen komento-liitännäisellä
fi.imgur=Imgur-liitännäinen (Katso: https://imgur.com)
fi.jira=Jira-liitännäinen
fi.language=Lisäkielet
fi.office=Microsoft-Office-liitännäinen
fi.optimize=Optimoidaan suorituskykyä, tämä voi kestää hetken.
fi.startgreenshot=Käynnistä {#ExeName}
fi.startup=Käynnistä {#ExeName} Windowsin käynnistyessä

fr.confluence=Greffon Confluence
fr.default=${default}
fr.externalcommand=Ouvrir avec le greffon de commande externe
fr.imgur=Greffon Imgur (Voir: https://imgur.com)
fr.jira=Greffon Jira
fr.language=Langues additionnelles
fr.office=Greffon Microsoft Office
fr.optimize=Optimisation des performances, Ceci peut prendre un certain temps.
fr.startgreenshot=Démarrer {#ExeName}
fr.startup=Lancer {#ExeName} au démarrage de Windows

it.box=Plugin Box
it.confluence=Plugin Confluence
it.default=Installazione predefinita
it.dropbox=Plugin Dropbox
it.externalcommand=Apri con comando esterno plugin
it.imgur=Plugin Imgur (vedi: https://imgur.com)
it.jira=Plugin Jira
it.language=Lingue aggiuntive
it.office=Plugin Microsoft Office
it.optimize=Ottimizzazione prestazioni (può richiedere tempo).
it.startgreenshot=Esegui {#ExeName}
it.startup=Esegui {#ExeName} all''avvio di Windows
it.UninstallIconDescription=Disinstalla
it.ShowLicense=Visualizza licenza (in inglese)
it.ShowReadme=Visualizza Readme (in inglese)
it.dexfranconia=Fräncofono (Tedesco)
it.arSY=Arabo (Siria)
it.caCA=Catalano
it.csCZ=Ceco
it.daDK=Danese
it.elGR=Greco
it.esES=Spagnolo
it.etEE=Eesti
it.faIR=Farsi (Iran)
it.fiFI=Suomi
it.frFR=Francese
it.frQC=Francese (Québec)
it.heIL=Ebraico (Israele)
it.huHU=Ungherese
it.idID=Bahasa Indonesia
it.itIT=Italiano
it.jaJP=Giapponese
it.kabDZ=Taqbaylit
it.koKR=Coreano
it.ltLT=Lituano
it.lvLV=Latviano
it.nnNO=Norvegese
it.plPL=Polacco
it.ptBR=Portoghese (Brasile)
it.ptPT=Portoghese (Portogallo)
it.roRO=Rumeno
it.ruRU=Russo
it.skSK=Slovacco
it.slSI=Sloveno
it.srRS=Serbo (Russia)
it.svSE=Svedese
it.trTR=Türco
it.ukUA=Ucraino
it.viVN=Vietnamita
it.zhCN=Cinese (Semplificato)
it.zhTW=Cinese (Taiwan)

lt.confluence=Confluence spraudnis
lt.default=${default}
lt.externalcommand=Pielāgotu darbību spraudnis
lt.imgur=Imgur spraudnis (Vairāk šeit: https://imgur.com)
lt.jira=Jira spraudnis
lt.language=Papildus valodas
lt.office=Microsoft Office spraudnis
lt.optimize=Uzlaboju veikstpēju, tas prasīs kādu laiciņu.
lt.startgreenshot=Palaist {#ExeName}
lt.startup=Palaist {#ExeName} uzsākot darbus

lt.confluence=Confluence spraudnis
lt.default=${default}
lt.externalcommand=Pielāgotu darbību spraudnis
lt.imgur=Imgur spraudnis (Vairāk šeit: https://imgur.com)
lt.jira=Jira spraudnis
lt.language=Papildus valodas
lt.office=Microsoft Office spraudnis
lt.optimize=Uzlaboju veikstpēju, tas prasīs kādu laiciņu.
lt.startgreenshot=Palaist {#ExeName}
lt.startup=Palaist {#ExeName} uzsākot darbus

nl.confluence=Confluence plug-in
nl.default=Standaardinstallatie
nl.externalcommand=Openen met extern commando plug-in
nl.imgur=Imgur plug-in (zie: https://imgur.com)
nl.jira=Jira plug-in
nl.language=Extra talen
nl.office=Microsoft Office plug-in
nl.optimize=Prestaties verbeteren, even geduld.
nl.startgreenshot={#ExeName} starten
nl.startup={#ExeName} automatisch starten met Windows

nn.confluence=Confluence-tillegg
nn.default=Default installation
nn.externalcommand=Tillegg for å opne med ekstern kommando
nn.imgur=Imgur-tillegg (sjå https://imgur.com)
nn.jira=Jira-tillegg
nn.language=Andre språk
nn.office=Microsoft Office Tillegg
nn.optimize=Optimaliserar ytelse, dette kan ta litt tid...
nn.startgreenshot=Start {#ExeName}
nn.startup=Start {#ExeName} når Windows startar

ru.confluence=Плагин Confluence
ru.default=${default}
ru.externalcommand=Открыть с плагином с помощью внешней команды
ru.imgur=Плагин Imgur (смотрите https://imgur.com/)
ru.jira=Плагин Jira
ru.language=Дополнительные языки
ru.office=Плагин Microsoft Office
ru.optimize=Идет оптимизация производительности, это может занять некоторое время.
ru.startgreenshot=Запустить {#ExeName}
ru.startup=Запускать {#ExeName} при старте Windows

sr.confluence=Прикључак за Конфлуенс
sr.default=${default}
sr.externalcommand=Отвори са прикључком за спољне наредбе
sr.imgur=Прикључак за Имиџер (https://imgur.com)
sr.jira=Прикључак за Џиру
sr.language=Додатни језици
sr.optimize=Оптимизујем перформансе…
sr.startgreenshot=Покрени Гриншот
sr.startup=Покрени програм са системом

sv.confluence=Confluence-insticksprogram
sv.externalcommand=Öppna med externt kommando-insticksprogram
sv.imgur=Imgur-insticksprogram (Se: https://imgur.com)
sv.jira=Jira-insticksprogram
sv.language=Ytterligare språk
sv.optimize=Optimerar prestanda, detta kan ta en stund.
sv.startgreenshot=Starta {#ExeName}
sv.startup=Starta {#ExeName} med Windows

tr.box=Box eklentisi
tr.confluence=Confluence eklentisi
tr.default=Varsayılan kurulum
tr.dropbox=Dropbox eklentisi
tr.externalcommand=Harici komut eklentisiyle aç
tr.imgur=Imgur eklentisi (Bkz: https://imgur.com)
tr.jira=Jira eklentisi
tr.language=Ek diller
tr.office=Microsoft Office eklentisi
tr.optimize=Performans ayarları yapılıyor, bu biraz zaman alabilir.
tr.startgreenshot={#ExeName} uygulamasını başlat
tr.startup={#ExeName} Windows açıldığında başlasın
tr.UninstallIconDescription=Uninstall
tr.ShowLicense=Show license
tr.ShowReadme=Show Readme
tr.disablewin11snippingtool=Win11 varsayılan ekran alıntısı aracını devre dışı bırakın

uk.confluence=Плагін Confluence
uk.default=${default}
uk.externalcommand=Плагін запуску зовнішньої команди
uk.imgur=Плагін Imgur (див.: https://imgur.com)
uk.jira=Плагін Jira
uk.language=Додаткові мови
uk.optimize=Оптимізація продуктивності, це може забрати час.
uk.startgreenshot=Запустити {#ExeName}
uk.startup=Запускати {#ExeName} під час запуску Windows

cn.confluence=Confluence插件
cn.default=${default}
cn.externalcommand=使用外部命令打开插件
cn.imgur=Imgur插件( (请访问： https://imgur.com))
cn.jira=Jira插件
cn.language=其它语言
cn.optimize=正在优化性能，这可能需要一点时间。
cn.startgreenshot=启动{#ExeName}
cn.startup=让{#ExeName}随Windows一起启动

[Types]
Name: "default"; Description: "{cm:default}"
Name: "full"; Description: "{code:FullInstall}"
Name: "compact"; Description: "{code:CompactInstall}"
Name: "custom"; Description: "{code:CustomInstall}"; Flags: iscustom

[Components]
Name: "disablesnippingtool"; Description: {cm:disablewin11snippingtool}; Flags: disablenouninstallwarning; Types: default full custom
Name: "greenshot"; Description: "Greenshot"; Types: default full compact custom; Flags: fixed
Name: "plugins\box"; Description: {cm:box}; Types: full custom; Flags: disablenouninstallwarning
Name: "plugins\confluence"; Description: {cm:confluence}; Types: full custom; Flags: disablenouninstallwarning
Name: "plugins\dropbox"; Description: {cm:dropbox}; Types: full custom; Flags: disablenouninstallwarning
Name: "plugins\externalcommand"; Description: {cm:externalcommand}; Types: full custom; Flags: disablenouninstallwarning
Name: "plugins\imgur"; Description: {cm:imgur}; Types: full custom; Flags: disablenouninstallwarning
Name: "plugins\jira"; Description: {cm:jira}; Types: full custom; Flags: disablenouninstallwarning
Name: "plugins\office"; Description: {cm:office}; Types: default full custom; Flags: disablenouninstallwarning
Name: "languages"; Description: {cm:language}; Types: full custom; Flags: disablenouninstallwarning
Name: "languages\arSY"; Description: {cm:arSY}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('d')
Name: "languages\caCA"; Description: {cm:caCA}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\csCZ"; Description: {cm:csCZ}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\daDK"; Description: {cm:daDK}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\dexfranconia"; Description: {cm:dexfranconia}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\elGR"; Description: {cm:elGR}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('4')
Name: "languages\esES"; Description: {cm:esES}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\etEE"; Description: {cm:etEE}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\faIR"; Description: {cm:faIR}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('d')
Name: "languages\fiFI"; Description: {cm:fiFI}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\frFR"; Description: {cm:frFR}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\frQC"; Description: {cm:frQC}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\heIL"; Description: {cm:heIL}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('c')
Name: "languages\huHU"; Description: {cm:huHU}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\idID"; Description: {cm:idID}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\itIT"; Description: {cm:itIT}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\jaJP"; Description: {cm:jaJP}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('7')
Name: "languages\kabDZ"; Description: {cm:kabDZ}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('8')
Name: "languages\koKR"; Description: {cm:koKR}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('8')
Name: "languages\ltLT"; Description: {cm:ltLT}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('3')
Name: "languages\lvLV"; Description: {cm:lvLV}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('3')
Name: "languages\nnNO"; Description: {cm:nnNO}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\plPL"; Description: {cm:plPL}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\ptBR"; Description: {cm:ptBR}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\ptPT"; Description: {cm:ptPT}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\roRO"; Description: {cm:roRO}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\ruRU"; Description: {cm:ruRU}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('5')
Name: "languages\skSK"; Description: {cm:skSK}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\slSI"; Description: {cm:slSI}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('2')
Name: "languages\srRS"; Description: {cm:srRS}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('5')
Name: "languages\svSE"; Description: {cm:svSE}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('1')
Name: "languages\trTR"; Description: {cm:trTR}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('6')
Name: "languages\ukUA"; Description: {cm:ukUA}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('5')
Name: "languages\viVN"; Description: {cm:viVN}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('e')
Name: "languages\zhCN"; Description: {cm:zhCN}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('a')
Name: "languages\zhTW"; Description: {cm:zhTW}; Types: full custom; Flags: disablenouninstallwarning; Check: hasLanguageGroup('9')

[Code]
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
		if Lowercase(currentParameter) = '--no-run' then begin
			foundNoRun := true;
			continue;
		end;

		if foundStart then begin
			parametersString := parametersString + ' ' + currentParameter;
			foundStart := false;
		end
		else begin
			if Lowercase(currentParameter) = '--language' then begin
				foundStart := true;
				foundLanguage := true;
				parametersString := parametersString + ' ' + currentParameter;
			end;
		end;
	end;
	if not foundLanguage then begin
		parametersString := parametersString + ' --language ' + ExpandConstant('{language}');
	end;
	if foundNoRun then begin
		parametersString := parametersString + ' --no-run';
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
	// Check for .NET and install 4.8.0 if we don't have it
	Result := IsDotNetInstalled(net48, 0); //Returns True if .NET Framework version 4.6.2 is installed, or a compatible version such as 4.8.0
	if not Result then
		SuppressibleMsgBox(FmtMessage(SetupMessage(msgWinVersionTooLowError), ['.NET Framework', '4.8.0']), mbCriticalError, MB_OK, IDOK);
end;

function ShouldDisableSnippingTool: Boolean;
begin
  Result := WizardIsComponentSelected('disablesnippingtool');
end;

/////////////////////////////////////////////////////////////////////
// Restart manager support. This is needed to restart Greenshot after installation, if it was running when the installer was launched.
/////////////////////////////////////////////////////////////////////
var
  AppWasRestarted: Boolean;

procedure UR_RestartManagerRestarted;
begin
  // This is a special internal callback. 
  // It fires if the Restart Manager actually restarts the app.
  AppWasRestarted := True;
end;

function NotAlreadyRestarted: Boolean;
begin
  // If AppWasRestarted is True, we return False to skip the [Run] entry.
  Result := not AppWasRestarted;
end;

[Run]
Filename: "{app}\{#ExeName}.exe"; Description: "{cm:startgreenshot}"; Parameters: "{code:GetParamsForGS}"; WorkingDir: "{app}"; Flags: nowait postinstall runasoriginaluser; Check: NotAlreadyRestarted
Filename: "https://getgreenshot.org/thank-you/?language={language}&version={#Version}"; Flags: shellexec runasoriginaluser

[InstallDelete]
// processed as the first step of installation.
// Delete plugins from Greenshot 1.2
Type: filesandordirs; Name: "{app}\Plugins\GreenshotBoxPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotConfluencePlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotDropboxPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotExternalCommandPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotFlickrPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotImgurPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotJiraPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotOCRPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotOfficePlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotPhotobucketPlugin"
Type: filesandordirs; Name: "{app}\Plugins\GreenshotPicasaPlugin"

// Newer 1.3 plugins
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Box"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Confluence"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Dropbox"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.ExternalCommand"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Flickr"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.GooglePhotos"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Imgur"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Jira"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Office"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Photobucket"
Type: filesandordirs; Name: "{app}\Plugins\Greenshot.Plugin.Win10"

// Cleanup directory if there are no plugins left
Name: {app}\Plugins; Type: dirifempty;

// Cleanup the main directory if there are no files left
Name: {app}; Type: dirifempty;

