[Components]
Name: "greenshot\externalcommand"; Description: {cm:externalcommand}; Types: full custom; Flags: disablenouninstallwarning

[Files]
Source: {#PluginDir}\Greenshot.Plugin.ExternalCommand\Greenshot.Plugin.ExternalCommand.dll; DestDir: {app}\Plugins\ExternalCommand; Components: greenshot\externalcommand; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.ExternalCommand\Languages\language_externalcommand*.xml; DestDir: {app}\Languages\Plugins\ExternalCommand; Components: greenshot\externalcommand; Flags: {#DefaultInstallFlags};

[CustomMessages]
externalcommand=Open with external command plug-in
en.externalcommand=Open with external command plug-in
de.externalcommand=Externes Kommando Plug-in
es.externalcommand=Extensión para abrir con programas externos
fi.externalcommand=Avaa Ulkoinen komento-liitännäisellä
fr.externalcommand=Ouvrir avec le greffon de commande externe
it.externalcommand=Apri con comando esterno plugin
lv.externalcommand=Pielāgotu darbību spraudnis
nl.externalcommand=Openen met extern commando plug-in
nn.externalcommand=Tillegg for å opne med ekstern kommando
ptBR.externalcommand=Plug-in Abrir com comando externo
ru.externalcommand=Открыть с плагином с помощью внешней команды
sr.externalcommand=Отвори са прикључком за спољне наредбе
sv.externalcommand=Öppna med externt kommando-insticksprogram
tr.externalcommand=Harici komut eklentisiyle aç
uk.externalcommand=Плагін запуску зовнішньої команди
cn.externalcommand=使用外部命令打开插件
