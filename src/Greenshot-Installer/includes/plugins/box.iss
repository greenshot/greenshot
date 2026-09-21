[Components]
Name: "greenshot\box"; Description: {cm:box}; Types: full custom; Flags: disablenouninstallwarning

[Files]
Source: {#PluginDir}\Greenshot.Plugin.Box\Greenshot.Plugin.Box.dll; DestDir: {app}\Plugins\Box; Components: greenshot\box; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.Box\Languages\language_box*.xml; DestDir: {app}\Languages\Plugins\Box; Components: greenshot\box; Flags: {#DefaultInstallFlags};

[CustomMessages]
box=Box plug-in
en.box=Box plug-in
it.box=Plugin Box
ptBR.box=Plug-in do Box
tr.box=Box eklentisi
