[Components]
Name: "greenshot\dropbox"; Description: {cm:dropbox}; Types: full custom; Flags: disablenouninstallwarning

[Files]
Source: {#PluginDir}\Greenshot.Plugin.DropBox\Greenshot.Plugin.DropBox.dll; DestDir: {app}\Plugins\DropBox; Components: greenshot\dropbox; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.DropBox\Languages\language_dropbox*.xml; DestDir: {app}\Languages\Plugins\DropBox; Components: greenshot\dropbox; Flags: {#DefaultInstallFlags};

[CustomMessages]
dropbox=Dropbox plug-in
en.dropbox=Dropbox plug-in
it.dropbox=Plugin Dropbox
ptBR.dropbox=Plug-in do Dropbox
tr.dropbox=Dropbox eklentisi
