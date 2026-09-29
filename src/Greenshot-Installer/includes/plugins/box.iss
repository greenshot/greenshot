[Components]
Name: "plugins\box"; Description: {cm:box}; Types: full custom; Flags: disablenouninstallwarning

[Files]
; The plugin and the libraries only it uses; the build removed its copies of the files Greenshot itself installs
Source: {#PluginDir}\Greenshot.Plugin.Box\*.dll; DestDir: {app}\Plugins\Greenshot.Plugin.Box; Components: plugins\box; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.Box\Languages\language_box*.xml; DestDir: {app}\Languages\Plugins\Box; Components: plugins\box; Flags: {#DefaultInstallFlags};

[CustomMessages]
box=Box plug-in
en.box=Box plug-in
it.box=Plugin Box
ptBR.box=Plug-in do Box
tr.box=Box eklentisi
