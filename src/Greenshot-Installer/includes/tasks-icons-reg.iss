[Tasks]
Name: startup; Description: {cm:startup}

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

; Create the startup entries if requested to do so
Root: HKA; Subkey: Software\Microsoft\Windows\CurrentVersion\Run; ValueType: string; ValueName: {#ExeName}; ValueData: """{app}\{#ExeName}.exe"""; Flags: uninsdeletevalue noerror; Tasks: startup

; File types. Every file is opened through greenshot-proxy.exe --file, Greenshot then runs the recipes whose OpenFile trigger
; matches the extension (the built-in "Open file" recipe opens it in the editor).
; The ProgIDs must not be called "greenshot": registry keys are case-insensitive, that key is the greenshot: URL protocol.
; Greenshot.File: our own .greenshot format, Greenshot is its default application.
Root: HKA; Subkey: Software\Classes\.greenshot; ValueType: string; ValueName: ""; ValueData: "Greenshot.File"; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\.greenshot\OpenWithProgids; ValueType: string; ValueName: "Greenshot.File"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty noerror
Root: HKA; Subkey: Software\Classes\Greenshot.File; ValueType: string; ValueName: ""; ValueData: "Greenshot File"; Flags: uninsdeletekey noerror
Root: HKA; Subkey: Software\Classes\Greenshot.File\DefaultIcon; ValueType: string; ValueName: ""; ValueData: """{app}\Greenshot.exe"",0"; Flags: noerror
Root: HKA; Subkey: Software\Classes\Greenshot.File\shell\open\command; ValueType: string; ValueName: ""; ValueData: """{app}\greenshot-proxy.exe"" --file ""%1"""; Flags: noerror
; Greenshot.Image: the image formats Greenshot can open. Greenshot is only added to "Open with" of these types, it does not take
; over their default application; the user can choose Greenshot as default in Settings > Default apps (see Capabilities below).
Root: HKA; Subkey: Software\Classes\Greenshot.Image; ValueType: string; ValueName: ""; ValueData: "Image (Greenshot)"; Flags: uninsdeletekey noerror
Root: HKA; Subkey: Software\Classes\Greenshot.Image\DefaultIcon; ValueType: string; ValueName: ""; ValueData: """{app}\Greenshot.exe"",0"; Flags: noerror
Root: HKA; Subkey: Software\Classes\Greenshot.Image\shell\open; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "Greenshot"; Flags: noerror
Root: HKA; Subkey: Software\Classes\Greenshot.Image\shell\open\command; ValueType: string; ValueName: ""; ValueData: """{app}\greenshot-proxy.exe"" --file ""%1"""; Flags: noerror
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

; "Open with > Choose another app": shows the proxy as "Greenshot", only for the supported types
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "Greenshot"; Flags: uninsdeletekey noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\DefaultIcon; ValueType: string; ValueName: ""; ValueData: """{app}\Greenshot.exe"",0"; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\shell\open\command; ValueType: string; ValueName: ""; ValueData: """{app}\greenshot-proxy.exe"" --file ""%1"""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".greenshot"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".png"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".jpg"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".jpeg"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".bmp"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".gif"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".tif"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".tiff"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".webp"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".ico"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".svg"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".jxr"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".wdp"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".emf"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".wmf"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\Applications\greenshot-proxy.exe\SupportedTypes; ValueType: string; ValueName: ".tga"; ValueData: ""; Flags: noerror

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
Filename: "{app}\{#ExeName}.exe"; Description: "{cm:startgreenshot}"; Parameters: "{code:GetParamsForGS}"; WorkingDir: "{app}"; Flags: nowait postinstall runasoriginaluser; Check: NotAlreadyRestarted

[UninstallRun]
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\{#ExeName}.exe"" /nologo"; Flags: runhidden waituntilterminated; RunOnceId: "NgenUninstall"; Check: IsAdminInstallMode

[CustomMessages]
default=Default installation
startup=Start {#ExeName} with Windows start
startgreenshot=Start {#ExeName}
UninstallIconDescription=Uninstall
ShowLicense=Show license
ShowReadme=Show Readme
disablewin11snippingtool=Disable Win11 default PrtScr snipping tool

en.default=Default installation
en.startgreenshot=Start {#ExeName}
en.startup=Start {#ExeName} with Windows start
en.UninstallIconDescription=Uninstall
en.ShowLicense=Show license
en.ShowReadme=Show Readme
en.disablewin11snippingtool=Disable Win11 default PrtScr snipping tool

de.default=Standard installation
de.startgreenshot={#ExeName} starten
de.startup={#ExeName} starten wenn Windows hochfährt
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
