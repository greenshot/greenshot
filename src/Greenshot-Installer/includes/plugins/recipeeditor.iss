[Components]
Name: "greenshot\recipeeditor"; Description: {cm:recipeeditor}; Types: default full custom; Flags: disablenouninstallwarning

[Files]
Source: {#PluginDir}\Greenshot.Plugin.RecipeEditor\Greenshot.Plugin.RecipeEditor.dll; DestDir: {app}\Plugins\RecipeEditor; Components: greenshot\recipeeditor; Flags: {#DefaultInstallFlags};
Source: {#PluginDir}\Greenshot.Plugin.RecipeEditor\Nodify.dll; DestDir: {app}\Plugins\RecipeEditor; Components: greenshot\recipeeditor; Flags: {#DefaultInstallFlags};

[CustomMessages]
recipeeditor=Recipe Editor plug-in
en.recipeeditor=Recipe Editor plug-in
de.recipeeditor=Rezept-Editor Plug-in
