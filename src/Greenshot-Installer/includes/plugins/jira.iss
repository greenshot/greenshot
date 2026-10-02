[Components]
Name: "plugins\jira"; Description: {cm:jira}; Types: full custom; Flags: disablenouninstallwarning

[Files]
; The plugin and the libraries only it uses; the build removed its copies of the files Greenshot itself installs
Source: {#PluginDir}\Greenshot.Plugin.Jira\*.dll; DestDir: {app}\Plugins\Greenshot.Plugin.Jira; Components: plugins\jira; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.Jira\Languages\language_jira*.xml; DestDir: {app}\Languages\Plugins\Jira; Components: plugins\jira; Flags: {#DefaultInstallFlags};

[CustomMessages]
jira=Jira plug-in
en.jira=Jira plug-in
de.jira=Jira Plug-in
es.jira=Extensión para Jira
fi.jira=Jira-liitännäinen
fr.jira=Greffon Jira
it.jira=Plugin Jira
lv.jira=Jira spraudnis
nl.jira=Jira plug-in
nn.jira=Jira-tillegg
ptBR.jira=Plug-in do Jira
ru.jira=Плагин Jira
sr.jira=Прикључак за Џиру
sv.jira=Jira-insticksprogram
tr.jira=Jira eklentisi
uk.jira=Плагін Jira
cn.jira=Jira插件
