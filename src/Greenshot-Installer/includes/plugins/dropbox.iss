[Components]
Name: "plugins\dropbox"; Description: {cm:dropbox}; Types: full custom; Flags: disablenouninstallwarning

[Files]
; The plugin and the libraries only it uses; the build removed its copies of the files Greenshot itself installs
Source: {#PluginDir}\Greenshot.Plugin.Dropbox\*.dll; DestDir: {app}\Plugins\Greenshot.Plugin.Dropbox; Components: plugins\dropbox; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.DropBox\Languages\greenshot.dropbox.*.ini; DestDir: {app}\Languages; Components: plugins\dropbox; Flags: {#DefaultInstallFlags};

[CustomMessages]
dropbox=Dropbox plug-in
en.dropbox=Dropbox plug-in
it.dropbox=Plugin Dropbox
ptBR.dropbox=Plug-in do Dropbox
tr.dropbox=Dropbox eklentisi
