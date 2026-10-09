[Components]
Name: "plugins\recipeeditor"; Description: {cm:recipeeditor}; Types: default full custom; Flags: disablenouninstallwarning

[Files]
; The plugin and the libraries only it uses; the build removed its copies of the files Greenshot itself installs
Source: {#PluginDir}\Greenshot.Plugin.RecipeEditor\*.dll; DestDir: {app}\Plugins\Greenshot.Plugin.RecipeEditor; Components: plugins\recipeeditor; Flags: {#DefaultInstallFlags};

[CustomMessages]
recipeeditor=Recipe Editor plug-in
en.recipeeditor=Recipe Editor plug-in
de.recipeeditor=Rezept-Editor Plug-in
ptBR.recipeeditor=Plug-in do Recipe Editor
