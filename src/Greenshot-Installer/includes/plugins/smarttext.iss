[Components]
Name: "plugins\smarttext"; Description: {cm:smarttext}; Types: full custom; Flags: disablenouninstallwarning

[Files]
; The plugin and the libraries only it uses; the build removed its copies of the files Greenshot itself installs
Source: {#PluginDir}\Greenshot.Plugin.SmartText\*.dll; DestDir: {app}\Plugins\Greenshot.Plugin.SmartText; Components: plugins\smarttext; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.SmartText\Languages\greenshot.smarttext.*.ini; DestDir: {app}\Languages; Components: plugins\smarttext; Flags: {#DefaultInstallFlags};

[CustomMessages]
smarttext=Smart text plug-in (beta)
en.smarttext=Smart text plug-in (beta)
de.smarttext=Smart-Text-Plug-in (Beta)
