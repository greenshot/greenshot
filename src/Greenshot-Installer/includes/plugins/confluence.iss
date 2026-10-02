[Components]
Name: "plugins\confluence"; Description: {cm:confluence}; Types: full custom; Flags: disablenouninstallwarning

[Files]
; The plugin and the libraries only it uses; the build removed its copies of the files Greenshot itself installs
Source: {#PluginDir}\Greenshot.Plugin.Confluence\*.dll; DestDir: {app}\Plugins\Greenshot.Plugin.Confluence; Components: plugins\confluence; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.Confluence\Languages\language_confluence*.xml; DestDir: {app}\Languages\Plugins\Confluence; Components: plugins\confluence; Flags: {#DefaultInstallFlags};

[CustomMessages]
confluence=Confluence plug-in
en.confluence=Confluence plug-in
de.confluence=Confluence Plug-in
es.confluence=Extensión para Confluence
fi.confluence=Confluence-liitännäinen
fr.confluence=Greffon Confluence
it.confluence=Plugin Confluence
lv.confluence=Confluence spraudnis
nl.confluence=Confluence plug-in
nn.confluence=Confluence-tillegg
ptBR.confluence=Plug-in do Confluence
ru.confluence=Плагин Confluence
sr.confluence=Прикључак за Конфлуенс
sv.confluence=Confluence-insticksprogram
tr.confluence=Confluence eklentisi
uk.confluence=Плагін Confluence
cn.confluence=Confluence插件
