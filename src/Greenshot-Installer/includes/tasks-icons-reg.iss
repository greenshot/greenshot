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

[Run]
Filename: "{app}\{#ExeName}.exe"; Description: "{cm:startgreenshot}"; Parameters: "{code:GetParamsForGS}"; WorkingDir: "{app}"; Flags: nowait postinstall runasoriginaluser; Check: NotAlreadyRestarted
Filename: "https://getgreenshot.org/thank-you/?language={language}&version={#Version}"; Flags: shellexec runasoriginaluser

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
