; The GUID of the Active Setup component, in [Registry] and [Run] as {{{#AutostartComponentGuid}} (a single "{" starts a constant)
#define AutostartComponentGuid "17A6D9DC-E818-4278-992A-167EF0FAC433"
; Raise it only to set up the start with Windows for every user once more, also for those who switched it off since
#define AutostartComponentVersion "1,0"
; rundll32.exe with these parameters writes the Run value of the current user from autostart.inf: no window, no administrator rights, see docs/vNext/autostart.md
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

; The start with Windows is always in the Run key of the user: autostart.inf in {app} writes it, see AutostartParameters, [Run] and docs/vNext/autostart.md.
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
[Messages]
; The warning about "for me only" (see the English and German texts above) in the other languages of the setup
cn.PrivilegesRequiredOverrideText1=%1 可以为所有用户安装（需要管理员权限），或仅为您安装。%n%n仅为您安装时，%1 位于您的用户配置文件中，您运行的任何程序都可以更改其文件。仅当无法为所有用户安装时才选择此项。
cn.PrivilegesRequiredOverrideCurrentUser=仅为我安装(&M)（安全性较低）
es.PrivilegesRequiredOverrideText1=%1 puede ser instalado para todos los usuarios (requiere privilegios administrativos), o solo para Ud.%n%nSi se instala solo para usted, %1 queda en su perfil de usuario, donde cualquier programa que ejecute puede modificar sus archivos. Elija esta opción solo si no puede instalarlo para todos los usuarios.
es.PrivilegesRequiredOverrideCurrentUser=Instalar para &mí solamente (menos seguro)
fi.PrivilegesRequiredOverrideText1=%1 voidaan asentaa kaikille käyttäjille (vaatii järjestelmänvalvojan oikeudet) tai vain sinun käyttöösi.%n%nVain sinulle asennettuna %1 sijaitsee käyttäjäprofiilissasi, jossa mikä tahansa suorittamasi ohjelma voi muuttaa sen tiedostoja. Valitse tämä vain, jos et voi asentaa sitä kaikille käyttäjille.
fi.PrivilegesRequiredOverrideCurrentUser=Asenna vain &minun käyttööni (vähemmän turvallinen)
fr.PrivilegesRequiredOverrideText1=%1 peut être installé pour tous les utilisateurs (nécessite des privilèges administrateur), ou seulement pour vous.%n%nInstallé pour vous seul, %1 se trouve dans votre profil utilisateur, où tout programme que vous exécutez peut modifier ses fichiers. Ne choisissez cette option que si vous ne pouvez pas l'installer pour tous les utilisateurs.
fr.PrivilegesRequiredOverrideCurrentUser=Installer seulement pour &moi (moins sûr)
it.PrivilegesRequiredOverrideText1=%1 può essere installato per tutti gli utenti (richiede privilegi di amministratore), o solo per l'utente attuale.%n%nSe installato solo per l'utente attuale, %1 si trova nel profilo utente, dove qualsiasi programma eseguito può modificarne i file. Scegli questa opzione solo se non puoi installarlo per tutti gli utenti.
it.PrivilegesRequiredOverrideCurrentUser=Installa solo per l'&utente attuale (meno sicuro)
lv.PrivilegesRequiredOverrideText1=%1 var tikt uzstādīts vai nu visiem lietotājiem (nepieciešamas administratora privilēģijas), vai arī tikai Jums.%n%nJa %1 ir uzstādīts tikai Jums, tas atrodas Jūsu lietotāja profilā, kur jebkura programma, ko palaižat, var mainīt tā failus. Izvēlieties šo tikai tad, ja nevarat to uzstādīt visiem lietotājiem.
lv.PrivilegesRequiredOverrideCurrentUser=Uzstādīt tikai &man (mazāk droši)
nl.PrivilegesRequiredOverrideText1=%1 kan geïnstalleerd worden voor alle gebruikers (vereist beheerdersrechten), of voor u alleen.%n%nAlleen voor u geïnstalleerd staat %1 in uw gebruikersprofiel, waar elk programma dat u uitvoert de bestanden kan wijzigen. Kies dit alleen als u het niet voor alle gebruikers kunt installeren.
nl.PrivilegesRequiredOverrideCurrentUser=Installeer voor &mij alleen (minder veilig)
nn.PrivilegesRequiredOverrideText1=%1 kan installerast for alle brukarar (krev administratorrettar) eller berre for deg.%n%nInstallert berre for deg ligg %1 i brukarprofilen din, der alle program du køyrer kan endre filene. Vel dette berre om du ikkje kan installere for alle brukarar.
nn.PrivilegesRequiredOverrideCurrentUser=Installer berre for &meg (mindre trygt)
ptBR.PrivilegesRequiredOverrideText1=O %1 pode ser instalado pra todos os usuários (requer privilégios administrativos) ou só pra você.%n%nInstalado só para você, o %1 fica no seu perfil de usuário, onde qualquer programa que você executa pode alterar os arquivos dele. Escolha isso só se não puder instalar para todos os usuários.
ptBR.PrivilegesRequiredOverrideCurrentUser=Instalar só &pra mim (menos seguro)
ru.PrivilegesRequiredOverrideText1=%1 может быть установлена либо для всех пользователей (требуются привилегии администратора), либо только для вас.%n%nПри установке только для вас %1 находится в вашем профиле, где любая запускаемая вами программа может изменить её файлы. Выбирайте этот вариант, только если не можете установить её для всех пользователей.
ru.PrivilegesRequiredOverrideCurrentUser=Установить только для &меня (менее безопасно)
sr.PrivilegesRequiredOverrideText1=%1 може да се инсталира за све кориснике (захтева администраторске привилегије) или само за вас.%n%nАко је инсталиран само за вас, %1 се налази у вашем корисничком профилу, где сваки програм који покренете може да мења његове датотеке. Изаберите ово само ако не можете да га инсталирате за све кориснике.
sr.PrivilegesRequiredOverrideCurrentUser=Инсталирај само за &мене (мање безбедно)
sv.PrivilegesRequiredOverrideText1=%1 kan installeras för alla användare (kräver administratörsbehörighet) eller bara för dig.%n%nInstallerad bara för dig ligger %1 i din användarprofil, där alla program du kör kan ändra dess filer. Välj detta bara om du inte kan installera för alla användare.
sv.PrivilegesRequiredOverrideCurrentUser=Installera för &mig enbart (mindre säkert)
tr.PrivilegesRequiredOverrideText1=%1 tüm kullanıcılar için (yönetici izinleri gerekir) ya da yalnızca sizin hesabınız için kurulabilir.%n%nYalnızca sizin için kurulduğunda %1 kullanıcı profilinizde bulunur; orada çalıştırdığınız her program dosyalarını değiştirebilir. Bunu yalnızca tüm kullanıcılar için kuramıyorsanız seçin.
tr.PrivilegesRequiredOverrideCurrentUser=&Yalnızca geçerli kullanıcı için kurulsun (daha az güvenli)
uk.PrivilegesRequiredOverrideText1=%1 може бути встановлено для всіх користувачів (потребує права адміністратора), або тільки для вас.%n%nЯкщо встановити тільки для вас, %1 буде у вашому профілі користувача, де будь-яка програма, яку ви запускаєте, може змінити його файли. Обирайте це, тільки якщо не можете встановити для всіх користувачів.
uk.PrivilegesRequiredOverrideCurrentUser=Встановити тільки для мене (менш безпечно)

[CustomMessages]
; The texts of the start with Windows and of the older Greenshot installed "for me only" (see the English and German texts above) in the other languages
cn.startupallusers=为此计算机的所有用户（每个人都可以在设置中关闭）
cn.startupcurrentuser=仅为我
cn.removeolderperuser=已为您单独安装了旧版 Greenshot %1，位于 %2。%n%n此 Greenshot 将为所有用户安装，位于 %3。旧版不再需要：在您的用户配置文件中，您运行的任何程序都可以更改它的文件，并且它可能会代替此版本启动。您的设置将保留。%n%n现在删除旧版 Greenshot？如果它正在运行，将先将其关闭。
cn.removeolderperuserfailed=无法删除 %1 中的旧版 Greenshot。您可以稍后在 Windows 设置的“应用”中将其卸载。
es.startupallusers=Para todos los usuarios de este equipo (cada uno puede desactivarlo en la configuración)
es.startupcurrentuser=Solo para mí
es.removeolderperuser=Hay un Greenshot %1 anterior instalado solo para usted, en %2.%n%nEste Greenshot se instala para todos los usuarios, en %3. El anterior ya no es necesario: en su perfil, cualquier programa que ejecute puede modificar sus archivos, y podría iniciarse en lugar de este. Su configuración se conserva.%n%n¿Quitar ahora el Greenshot anterior? Si se está ejecutando, se cerrará primero.
es.removeolderperuserfailed=No se pudo quitar el Greenshot anterior en %1. Puede desinstalarlo más tarde en la Configuración de Windows, en Aplicaciones.
fi.startupallusers=Kaikille tämän tietokoneen käyttäjille (kukin voi poistaa sen käytöstä asetuksista)
fi.startupcurrentuser=Vain minulle
fi.removeolderperuser=Vanhempi Greenshot %1 on asennettu vain sinulle, kansioon %2.%n%nTämä Greenshot asennetaan kaikille käyttäjille, kansioon %3. Vanhempaa ei enää tarvita: profiilissasi mikä tahansa suorittamasi ohjelma voi muuttaa sen tiedostoja, ja se voisi käynnistyä tämän sijaan. Asetuksesi säilyvät.%n%nPoistetaanko vanhempi Greenshot nyt? Jos se on käynnissä, se suljetaan ensin.
fi.removeolderperuserfailed=Vanhempaa Greenshotia kansiossa %1 ei voitu poistaa. Voit poistaa sen myöhemmin Windowsin asetuksista kohdasta Sovellukset.
fr.startupallusers=Pour tous les utilisateurs de cet ordinateur (chacun peut le désactiver dans les paramètres)
fr.startupcurrentuser=Seulement pour moi
fr.removeolderperuser=Une version antérieure de Greenshot %1 est installée pour vous seul, dans %2.%n%nCe Greenshot est installé pour tous les utilisateurs, dans %3. L'ancienne version n'est plus nécessaire : dans votre profil, tout programme que vous exécutez peut modifier ses fichiers, et elle pourrait être lancée à la place de celle-ci. Vos paramètres sont conservés.%n%nSupprimer l'ancienne version de Greenshot maintenant ? Si elle est en cours d'exécution, elle sera d'abord fermée.
fr.removeolderperuserfailed=L'ancienne version de Greenshot dans %1 n'a pas pu être supprimée. Vous pouvez la désinstaller plus tard dans les Paramètres de Windows, sous Applications.
it.startupallusers=Per tutti gli utenti di questo computer (ognuno può disattivarlo nelle impostazioni)
it.startupcurrentuser=Solo per me
it.removeolderperuser=Una versione precedente di Greenshot %1 è installata solo per te, in %2.%n%nQuesto Greenshot viene installato per tutti gli utenti, in %3. Quella precedente non serve più: nel tuo profilo qualsiasi programma che esegui può modificarne i file, e potrebbe essere avviata al posto di questa. Le tue impostazioni restano.%n%nRimuovere ora la versione precedente di Greenshot? Se è in esecuzione, verrà prima chiusa.
it.removeolderperuserfailed=Non è stato possibile rimuovere la versione precedente di Greenshot in %1. Puoi disinstallarla in seguito nelle Impostazioni di Windows, in App.
lv.startupallusers=Visiem šī datora lietotājiem (katrs to var izslēgt iestatījumos)
lv.startupcurrentuser=Tikai man
lv.removeolderperuser=Vecāka Greenshot %1 versija ir uzstādīta tikai Jums, mapē %2.%n%nŠis Greenshot tiek uzstādīts visiem lietotājiem, mapē %3. Vecākā versija vairs nav vajadzīga: Jūsu profilā jebkura programma, ko palaižat, var mainīt tās failus, un tā varētu tikt palaista šīs vietā. Jūsu iestatījumi saglabājas.%n%nNoņemt vecāko Greenshot tagad? Ja tā darbojas, tā vispirms tiks aizvērta.
lv.removeolderperuserfailed=Vecāko Greenshot mapē %1 neizdevās noņemt. Varat to atinstalēt vēlāk Windows iestatījumos, sadaļā Programmas.
nl.startupallusers=Voor alle gebruikers van deze computer (iedereen kan het uitschakelen in de instellingen)
nl.startupcurrentuser=Alleen voor mij
nl.removeolderperuser=Een oudere Greenshot %1 is alleen voor u geïnstalleerd, in %2.%n%nDeze Greenshot wordt voor alle gebruikers geïnstalleerd, in %3. De oudere is niet meer nodig: in uw profiel kan elk programma dat u uitvoert de bestanden ervan wijzigen, en hij zou in plaats van deze gestart kunnen worden. Uw instellingen blijven behouden.%n%nDe oudere Greenshot nu verwijderen? Als hij actief is, wordt hij eerst afgesloten.
nl.removeolderperuserfailed=De oudere Greenshot in %1 kon niet worden verwijderd. U kunt hem later verwijderen in de Windows-instellingen, onder Apps.
nn.startupallusers=For alle brukarane av denne datamaskinen (kvar brukar kan slå det av i innstillingane)
nn.startupcurrentuser=Berre for meg
nn.removeolderperuser=Ein eldre Greenshot %1 er installert berre for deg, i %2.%n%nDenne Greenshot blir installert for alle brukarar, i %3. Den eldre trengst ikkje lenger: i profilen din kan alle program du køyrer endre filene, og han kan bli starta i staden for denne. Innstillingane dine blir verande.%n%nFjerne den eldre Greenshot no? Viss han køyrer, blir han lukka først.
nn.removeolderperuserfailed=Den eldre Greenshot i %1 kunne ikkje fjernast. Du kan avinstallere han seinare i Windows-innstillingane, under Appar.
ptBR.startupallusers=Para todos os usuários deste computador (cada um pode desativar nas configurações)
ptBR.startupcurrentuser=Só para mim
ptBR.removeolderperuser=Um Greenshot %1 mais antigo está instalado só para você, em %2.%n%nEste Greenshot é instalado para todos os usuários, em %3. O mais antigo não é mais necessário: no seu perfil, qualquer programa que você executa pode alterar os arquivos dele, e ele poderia ser iniciado no lugar deste. Suas configurações são mantidas.%n%nRemover o Greenshot mais antigo agora? Se ele estiver em execução, será fechado antes.
ptBR.removeolderperuserfailed=O Greenshot mais antigo em %1 não pôde ser removido. Você pode desinstalá-lo depois nas Configurações do Windows, em Aplicativos.
ru.startupallusers=Для всех пользователей этого компьютера (каждый может отключить это в настройках)
ru.startupcurrentuser=Только для меня
ru.removeolderperuser=Более старая версия Greenshot %1 установлена только для вас, в %2.%n%nЭтот Greenshot устанавливается для всех пользователей, в %3. Старая версия больше не нужна: в вашем профиле любая запускаемая вами программа может изменить её файлы, и она может запуститься вместо этой. Ваши настройки сохранятся.%n%nУдалить старую версию Greenshot сейчас? Если она запущена, она будет сначала закрыта.
ru.removeolderperuserfailed=Не удалось удалить старую версию Greenshot в %1. Вы можете удалить её позже в параметрах Windows, в разделе «Приложения».
sr.startupallusers=За све кориснике овог рачунара (свако може да га искључи у подешавањима)
sr.startupcurrentuser=Само за мене
sr.removeolderperuser=Старији Greenshot %1 је инсталиран само за вас, у %2.%n%nОвај Greenshot се инсталира за све кориснике, у %3. Старији више није потребан: у вашем профилу сваки програм који покренете може да мења његове датотеке, и могао би да се покрене уместо овог. Ваша подешавања остају.%n%nУклонити старији Greenshot сада? Ако је покренут, прво ће бити затворен.
sr.removeolderperuserfailed=Старији Greenshot у %1 није могао да се уклони. Можете га касније деинсталирати у подешавањима оперативног система Windows, под Апликације.
sv.startupallusers=För alla användare av den här datorn (var och en kan stänga av det i inställningarna)
sv.startupcurrentuser=Bara för mig
sv.removeolderperuser=En äldre Greenshot %1 är installerad bara för dig, i %2.%n%nDen här Greenshot installeras för alla användare, i %3. Den äldre behövs inte längre: i din profil kan alla program du kör ändra dess filer, och den kan startas i stället för den här. Dina inställningar finns kvar.%n%nTa bort den äldre Greenshot nu? Om den körs stängs den först.
sv.removeolderperuserfailed=Den äldre Greenshot i %1 kunde inte tas bort. Du kan avinstallera den senare i Windows-inställningarna, under Appar.
tr.startupallusers=Bu bilgisayarın tüm kullanıcıları için (her biri ayarlardan kapatabilir)
tr.startupcurrentuser=Yalnızca benim için
tr.removeolderperuser=Eski bir Greenshot %1 yalnızca sizin için kurulu, %2 klasöründe.%n%nBu Greenshot tüm kullanıcılar için %3 klasörüne kuruluyor. Eski sürüme artık gerek yok: profilinizde çalıştırdığınız her program dosyalarını değiştirebilir ve bunun yerine o başlatılabilir. Ayarlarınız korunur.%n%nEski Greenshot şimdi kaldırılsın mı? Çalışıyorsa önce kapatılır.
tr.removeolderperuserfailed=%1 klasöründeki eski Greenshot kaldırılamadı. Daha sonra Windows Ayarları'nda, Uygulamalar altında kaldırabilirsiniz.
uk.startupallusers=Для всіх користувачів цього комп'ютера (кожен може вимкнути це в налаштуваннях)
uk.startupcurrentuser=Тільки для мене
uk.removeolderperuser=Старішу версію Greenshot %1 встановлено тільки для вас, у %2.%n%nЦей Greenshot встановлюється для всіх користувачів, у %3. Старіша версія більше не потрібна: у вашому профілі будь-яка програма, яку ви запускаєте, може змінити її файли, і вона може запуститися замість цієї. Ваші налаштування збережуться.%n%nВидалити старішу версію Greenshot зараз? Якщо вона запущена, її спочатку буде закрито.
uk.removeolderperuserfailed=Не вдалося видалити старішу версію Greenshot у %1. Ви можете видалити її пізніше в параметрах Windows, у розділі «Програми».
