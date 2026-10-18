[Components]
Name: "plugins\customuploader"; Description: {cm:customuploader}; Types: full custom; Flags: disablenouninstallwarning

[Files]
; The plugin and the libraries only it uses; the build removed its copies of the files Greenshot itself installs
Source: {#PluginDir}\Greenshot.Plugin.CustomUploader\*.dll; DestDir: {app}\Plugins\Greenshot.Plugin.CustomUploader; Components: plugins\customuploader; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.CustomUploader\Languages\greenshot.customuploader.*.ini; DestDir: {app}\Languages; Components: plugins\customuploader; Flags: {#DefaultInstallFlags};

[CustomMessages]
customuploader=Custom uploader plug-in (beta)
en.customuploader=Custom uploader plug-in (beta)
