; The GUID of the Active Setup component, in [Registry] and [Run] as {{{#AutostartComponentGuid}} (a single "{" starts a constant)
#define AutostartComponentGuid "17A6D9DC-E818-4278-992A-167EF0FAC433"
; Raise it only to set up the start with Windows for every user once more, also for those who switched it off since
#define AutostartComponentVersion "1,0"
; rundll32.exe with these parameters writes the Run value of the current user from autostart.inf: no window, no administrator rights, see docs/autostart.md
#define AutostartParameters 'advpack.dll,LaunchINFSectionEx ""{app}\autostart.inf"",DefaultInstall,,4'

#ifdef IsLightEdition
  #define FileOpener "Greenshot.exe"
#else
  #define FileOpener "greenshot-proxy.exe"
#endif

[Tasks]
Name: startup; Description: {cm:startup}
; For an all-users install: every user gets the start with Windows at the next logon and can switch it off in the settings of Greenshot
Name: startup\allusers; Description: {cm:startupallusers}; Flags: exclusive; Check: IsAdminInstallMode
Name: startup\currentuser; Description: {cm:startupcurrentuser}; Flags: exclusive unchecked; Check: IsAdminInstallMode

[Icons]
Name: {group}\{#ExeName}; Filename: {app}\{#ExeName}.exe; WorkingDir: {app}; AppUserModelID: "{#ExeName}"
Name: {group}\{cm:UninstallIconDescription} {#ExeName}; Filename: {uninstallexe}; WorkingDir: {app};
Name: {group}\{cm:ShowReadme}; Filename: {app}\readme.txt; WorkingDir: {app}
Name: {group}\{cm:ShowLicense}; Filename: {app}\license.txt; WorkingDir: {app}

[Registry]
; Delete all startup entries, so we don't have leftover values
Root: HKCU; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror;
Root: HKLM; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror;
Root: HKCU32; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror; Check: IsWin64()
Root: HKLM32; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror; Check: IsWin64()
Root: HKCU64; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror; Check: IsWin64()
Root: HKLM64; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: none; ValueName: {#ExeName}; Flags: deletevalue noerror; Check: IsWin64()

; Released versions (up to 1.4) registered the .greenshot file type as ProgID "Greenshot" (Greenshot.exe --openfile), in HKCU for
; a per-user install and in HKLM for an all-users install. That key is the same as the greenshot: URL protocol key (registry keys
; are case-insensitive), and a per-user copy in HKCU hides the protocol registered in HKLM. Remove the old registration from both
; places before the file types and the protocol are registered again below (entries are processed in order).
Root: HKCU; Subkey: Software\Classes\Greenshot; Flags: deletekey noerror
Root: HKLM; Subkey: Software\Classes\Greenshot; Flags: deletekey noerror; Check: IsAdminInstallMode
Root: HKCU; Subkey: Software\Classes\.greenshot; ValueType: none; ValueName: ""; Flags: deletevalue noerror
Root: HKLM; Subkey: Software\Classes\.greenshot; ValueType: none; ValueName: ""; Flags: deletevalue noerror; Check: IsAdminInstallMode
; A choice made in "Open with > Always" still names the old ProgID; without it Explorer falls back to Greenshot.File
Root: HKCU; Subkey: Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.greenshot\UserChoice; Flags: deletekey noerror

; The start with Windows is always in the Run key of the user: autostart.inf in {app} writes it, see AutostartParameters, [Run] and docs/autostart.md.
; For every user Active Setup runs it once per user, at the next logon. Without that task the key is removed, so no new user gets it.
Root: HKLM; Subkey: Software\Microsoft\Active Setup\Installed Components\{{{#AutostartComponentGuid}}; Flags: deletekey noerror; Check: IsAdminInstallMode
Root: HKLM; Subkey: Software\Microsoft\Active Setup\Installed Components\{{{#AutostartComponentGuid}}; ValueType: string; ValueName: ""; ValueData: "{#ExeName}"; Flags: uninsdeletekey; Tasks: startup\allusers
Root: HKLM; Subkey: Software\Microsoft\Active Setup\Installed Components\{{{#AutostartComponentGuid}}; ValueType: string; ValueName: "ComponentID"; ValueData: "{#ExeName}"; Tasks: startup\allusers
Root: HKLM; Subkey: Software\Microsoft\Active Setup\Installed Components\{{{#AutostartComponentGuid}}; ValueType: string; ValueName: "StubPath"; ValueData: """{sys}\rundll32.exe"" {#AutostartParameters}"; Tasks: startup\allusers
Root: HKLM; Subkey: Software\Microsoft\Active Setup\Installed Components\{{{#AutostartComponentGuid}}; ValueType: string; ValueName: "Version"; ValueData: "{#AutostartComponentVersion}"; Tasks: startup\allusers
Root: HKLM; Subkey: Software\Microsoft\Active Setup\Installed Components\{{{#AutostartComponentGuid}}; ValueType: dword; ValueName: "IsInstalled"; ValueData: "1"; Tasks: startup\allusers

#ifndef IsLightEdition
; Greenshot registers its app id for toasts (notifications) when it runs, with the COM class which receives their clicks (ToastActivator)
Root: HKCU; Subkey: Software\Classes\AppUserModelId\Greenshot; Flags: uninsdeletekey dontcreatekey noerror
Root: HKCU; Subkey: Software\Classes\CLSID\{{DD8B2638-A274-4988-9CC7-23A6D40BACEB}; Flags: uninsdeletekey dontcreatekey noerror
#else
; Greenshot Light has no toasts: remove the registration of a full version installed before, and its "Open with" entry of the proxy
Root: HKCU; Subkey: Software\Classes\AppUserModelId\Greenshot; Flags: deletekey noerror
Root: HKCU; Subkey: Software\Classes\CLSID\{{DD8B2638-A274-4988-9CC7-23A6D40BACEB}; Flags: deletekey noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe; Flags: deletekey noerror
#endif

; File types. Every file is opened through greenshot-proxy.exe --file (Greenshot Light: Greenshot.exe --file, it has no proxy),
; Greenshot then runs the recipes whose OpenFile trigger
; matches the extension (the built-in "Open file" recipe opens it in the editor).
; The ProgIDs must not be called "greenshot": registry keys are case-insensitive, that key is the greenshot: URL protocol.
; Greenshot.File: our own .greenshot format, Greenshot is its default application.
Root: HKA; Subkey: Software\Classes\.greenshot; ValueType: string; ValueName: ""; ValueData: "Greenshot.File"; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.greenshot\OpenWithProgids; ValueType: string; ValueName: "Greenshot.File"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\Greenshot.File; ValueType: string; ValueName: ""; ValueData: "Greenshot File"; Flags: uninsdeletekey noerror
Root: HKA; Subkey: Software\Classes\Greenshot.File\DefaultIcon; ValueType: string; ValueName: ""; ValueData: """{app}\Greenshot.exe"",0"; Flags: noerror
Root: HKA; Subkey: Software\Classes\Greenshot.File\shell\open\command; ValueType: string; ValueName: ""; ValueData: """{app}\{#FileOpener}"" --file ""%1"""; Flags: noerror
; Greenshot.Image: the image formats Greenshot can open. Greenshot is only added to "Open with" of these types, it does not take
; over their default application; the user can choose Greenshot as default in Settings > Default apps (see Capabilities below).
Root: HKA; Subkey: Software\Classes\Greenshot.Image; ValueType: string; ValueName: ""; ValueData: "Image (Greenshot)"; Flags: uninsdeletekey noerror
Root: HKA; Subkey: Software\Classes\Greenshot.Image\DefaultIcon; ValueType: string; ValueName: ""; ValueData: """{app}\Greenshot.exe"",0"; Flags: noerror
Root: HKA; Subkey: Software\Classes\Greenshot.Image\shell\open; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "Greenshot"; Flags: noerror
Root: HKA; Subkey: Software\Classes\Greenshot.Image\shell\open\command; ValueType: string; ValueName: ""; ValueData: """{app}\{#FileOpener}"" --file ""%1"""; Flags: noerror
Root: HKA; Subkey: Software\Classes\.png\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.jpg\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.jpeg\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.bmp\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.gif\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.tif\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.tiff\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.webp\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.ico\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.svg\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.jxr\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.wdp\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.emf\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.wmf\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.tga\OpenWithProgids; ValueType: string; ValueName: "Greenshot.Image"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror

; "Open with > Choose another app": shows the proxy (Light: Greenshot.exe) as "Greenshot", only for the supported types
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "Greenshot"; Flags: uninsdeletekey noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\DefaultIcon; ValueType: string; ValueName: ""; ValueData: """{app}\Greenshot.exe"",0"; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\shell\open\command; ValueType: string; ValueName: ""; ValueData: """{app}\{#FileOpener}"" --file ""%1"""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".greenshot"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".png"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".jpg"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".jpeg"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".bmp"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".gif"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".tif"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".tiff"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".webp"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".ico"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".svg"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".jxr"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".wdp"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".emf"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".wmf"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\{#FileOpener}\SupportedTypes; ValueType: string; ValueName: ".tga"; ValueData: ""; Flags: noerror

; Settings > Default apps: lets the user make Greenshot the default application for these types
Root: HKA; Subkey: Software\Greenshot\Capabilities; ValueType: string; ValueName: "ApplicationName"; ValueData: "Greenshot"; Flags: uninsdeletekey noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Screenshot tool: opens images in the Greenshot editor or runs your recipes on them"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities; ValueType: string; ValueName: "ApplicationIcon"; ValueData: """{app}\Greenshot.exe"",0"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".greenshot"; ValueData: "Greenshot.File"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".png"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".jpg"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".jpeg"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".bmp"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".gif"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".tif"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".tiff"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".webp"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".ico"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".svg"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".jxr"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".wdp"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".emf"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".wmf"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\FileAssociations; ValueType: string; ValueName: ".tga"; ValueData: "Greenshot.Image"; Flags: noerror
Root: HKA; Subkey: Software\Greenshot; Flags: uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\RegisteredApplications; ValueType: string; ValueName: "Greenshot"; ValueData: "Software\Greenshot\Capabilities"; Flags: uninsdeletevalue noerror


; Disable the default PRTSCR Snipping Tool in Windows 11
Root: HKCU; Subkey: Control Panel\Keyboard; ValueType: dword; ValueName: "PrintScreenKeyForSnippingEnabled"; ValueData: "0"; Flags: uninsdeletevalue; Check: ShouldDisableSnippingTool

[Run]
; Native images (NGen) for Greenshot and the assemblies it references: less JIT at startup. Needs admin rights; queued, the .NET optimization service compiles them right away.
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\{#ExeName}.exe"" /queue:1 /nologo"; Flags: runhidden waituntilterminated; Check: IsAdminInstallMode
; The start with Windows for the user who started the setup, the HKCU of an administrator who elevated it may be another user's.
; With Active Setup, also mark it as done for that user, so the next logon doesn't do it again.
Filename: "{sys}\rundll32.exe"; Parameters: "{#AutostartParameters}"; WorkingDir: "{app}"; Flags: runasoriginaluser waituntilterminated; Tasks: startup
Filename: "{sys}\reg.exe"; Parameters: "add ""HKCU\Software\Microsoft\Active Setup\Installed Components\{{{#AutostartComponentGuid}}"" /v Version /t REG_SZ /d ""{#AutostartComponentVersion}"" /f"; Flags: runasoriginaluser runhidden waituntilterminated; Tasks: startup\allusers
Filename: "{app}\{#ExeName}.exe"; Description: "{cm:startgreenshot}"; Parameters: "{code:GetParamsForGS}"; WorkingDir: "{app}"; Flags: nowait postinstall runasoriginaluser; Check: NotAlreadyRestarted
Filename: "https://getgreenshot.org/thank-you/?language={language}&version={#Version}"; Flags: shellexec runasoriginaluser

[UninstallRun]
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\{#ExeName}.exe"" /nologo"; Flags: runhidden waituntilterminated; RunOnceId: "NgenUninstall"; Check: IsAdminInstallMode

[Messages]
; The choice "for all users" or "for me only" when the setup starts: Greenshot belongs in Program Files, where only administrators
; can change its files. Installed for one user it's in the profile, where every program the user runs can change or replace them.
en.PrivilegesRequiredOverrideText1=%1 can be installed for all users (requires administrative privileges), or for you only.%n%nInstalled for you only, %1 is in your user profile, where every program you run can change its files. Only choose this if you can't install it for all users.
en.PrivilegesRequiredOverrideCurrentUser=Install for &me only (less secure)
de.PrivilegesRequiredOverrideText1=%1 kann für alle Benutzer (erfordert Administrationsrechte) oder nur für Sie installiert werden.%n%nNur für Sie installiert, liegt %1 in Ihrem Benutzerprofil, wo jedes Programm, das Sie ausführen, seine Dateien ändern kann. Wählen Sie das nur, wenn Sie es nicht für alle Benutzer installieren können.
de.PrivilegesRequiredOverrideCurrentUser=Installation nur für &Sie (weniger sicher)
[CustomMessages]
default=Default installation
startup=Start {#ExeName} with Windows start
startupallusers=For every user of this computer (each can switch it off in the settings)
startupcurrentuser=Only for me
removeolderperuser=An older Greenshot %1 is installed for you only, in %2.%n%nThis Greenshot is installed for all users, in %3. The older one is not needed anymore: in your profile every program you run can change its files, and it could be started instead of this one. Your settings stay.%n%nRemove the older Greenshot now? If it is running, it is closed first.
removeolderperuserfailed=The older Greenshot in %1 could not be removed. You can uninstall it later in the Windows settings, under Apps.
startgreenshot=Start {#ExeName}
UninstallIconDescription=Uninstall
ShowLicense=Show license
ShowReadme=Show Readme
disablewin11snippingtool=Disable Win11 default PrtScr snipping tool

en.default=Default installation
en.startgreenshot=Start {#ExeName}
en.startup=Start {#ExeName} with Windows start
en.startupallusers=For every user of this computer (each can switch it off in the settings)
en.startupcurrentuser=Only for me
en.removeolderperuser=An older Greenshot %1 is installed for you only, in %2.%n%nThis Greenshot is installed for all users, in %3. The older one is not needed anymore: in your profile every program you run can change its files, and it could be started instead of this one. Your settings stay.%n%nRemove the older Greenshot now? If it is running, it is closed first.
en.removeolderperuserfailed=The older Greenshot in %1 could not be removed. You can uninstall it later in the Windows settings, under Apps.
en.UninstallIconDescription=Uninstall
en.ShowLicense=Show license
en.ShowReadme=Show Readme
en.disablewin11snippingtool=Disable Win11 default PrtScr snipping tool

de.default=Standard installation
de.startgreenshot={#ExeName} starten
de.startup={#ExeName} starten wenn Windows hochfährt
de.startupallusers=Für jeden Benutzer dieses Computers (jeder kann es in den Einstellungen abschalten)
de.startupcurrentuser=Nur für mich
de.removeolderperuser=Ein älteres Greenshot %1 ist nur für Sie installiert, in %2.%n%nDieses Greenshot wird für alle Benutzer installiert, in %3. Das ältere wird nicht mehr gebraucht: In Ihrem Profil kann jedes Programm, das Sie ausführen, seine Dateien ändern, und es könnte statt diesem gestartet werden. Ihre Einstellungen bleiben erhalten.%n%nDas ältere Greenshot jetzt entfernen? Falls es läuft, wird es vorher beendet.
de.removeolderperuserfailed=Das ältere Greenshot in %1 konnte nicht entfernt werden. Sie können es später in den Windows-Einstellungen unter Apps deinstallieren.
de.disablewin11snippingtool=Deaktiviere das Standard Windows 11 Snipping Tool auf "Druck"

es.startgreenshot=Lanzar {#ExeName}
es.startup=Lanzar {#ExeName} al iniciarse Windows

fi.startgreenshot=Käynnistä {#ExeName}
fi.startup=Käynnistä {#ExeName} Windowsin käynnistyessä

fr.startgreenshot=Démarrer {#ExeName}
fr.startup=Lancer {#ExeName} au démarrage de Windows

it.default=Installazione predefinita
it.startgreenshot=Esegui {#ExeName}
it.startup=Esegui {#ExeName} all''avvio di Windows
it.UninstallIconDescription=Disinstalla
it.ShowLicense=Visualizza licenza (in inglese)
it.ShowReadme=Visualizza Readme (in inglese)

lv.startgreenshot=Palaist {#ExeName}
lv.startup=Palaist {#ExeName} uzsākot darbus

nl.default=Standaardinstallatie
nl.startgreenshot={#ExeName} starten
nl.startup={#ExeName} automatisch starten met Windows

nn.default=Default installation
nn.startgreenshot=Start {#ExeName}
nn.startup=Start {#ExeName} når Windows startar

ptBR.default=Instalação Padrão
ptBR.startgreenshot=Iniciar {#ExeName}
ptBR.startup=Iniciar {#ExeName} com o Windows
ptBR.UninstallIconDescription=Desinstalar
ptBR.ShowLicense=Mostrar licença
ptBR.ShowReadme=Mostrar Leia-me
ptBR.disablewin11snippingtool=Desativar ferramenta de captura padrão PrtScr do Win11

ru.startgreenshot=Запустить {#ExeName}
ru.startup=Запускать {#ExeName} при старте Windows

sr.startgreenshot=Покрени Гриншот
sr.startup=Покрени програм са системом

sv.startgreenshot=Starta {#ExeName}
sv.startup=Starta {#ExeName} med Windows

tr.default=Varsayılan kurulum
tr.startgreenshot={#ExeName} uygulamasını başlat
tr.startup={#ExeName} Windows açıldığında başlasın
tr.UninstallIconDescription=Uninstall
tr.ShowLicense=Show license
tr.ShowReadme=Show Readme
tr.disablewin11snippingtool=Win11 varsayılan ekran alıntısı aracını devre dışı bırakın

uk.startgreenshot=Запустити {#ExeName}
uk.startup=Запускати {#ExeName} під час запуску Windows

cn.startgreenshot=启动{#ExeName}
cn.startup=让{#ExeName}随Windows一起启动
