[Components]
Name: "plugins\divoom"; Description: {cm:divoom}; Types: full custom; Flags: disablenouninstallwarning

[Files]
; The plugin and the libraries only it uses; the build removed its copies of the files Greenshot itself installs
Source: {#PluginDir}\Greenshot.Plugin.Divoom\*.dll; DestDir: {app}\Plugins\Greenshot.Plugin.Divoom; Components: plugins\divoom; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.Divoom\Languages\greenshot.divoom.*.ini; DestDir: {app}\Languages; Components: plugins\divoom; Flags: {#DefaultInstallFlags};

[CustomMessages]
divoom=Divoom plug-in (beta)
en.divoom=Divoom plug-in (beta)
de.divoom=Divoom-Plug-in (Beta)
