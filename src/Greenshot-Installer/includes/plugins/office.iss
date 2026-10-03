[Components]
Name: "plugins\office"; Description: {cm:office}; Types: default full custom; Flags: disablenouninstallwarning

[Files]
; The plugin and the libraries only it uses; the build removed its copies of the files Greenshot itself installs
Source: {#PluginDir}\Greenshot.Plugin.Office\*.dll; DestDir: {app}\Plugins\Greenshot.Plugin.Office; Components: plugins\office; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.Office\Languages\greenshot.office.*.ini; DestDir: {app}\Languages; Components: plugins\office; Flags: {#DefaultInstallFlags};

[CustomMessages]
office=Microsoft Office plug-in
en.office=Microsoft Office plug-in
de.office=Microsoft Office Plug-in
fi.office=Microsoft-Office-liitännäinen
fr.office=Greffon Microsoft Office
it.office=Plugin Microsoft Office
lv.office=Microsoft Office spraudnis
nl.office=Microsoft Office plug-in
nn.office=Microsoft Office Tillegg
ptBR.office=Plug-in do Microsoft Office
ru.office=Плагин Microsoft Office
tr.office=Microsoft Office eklentisi
