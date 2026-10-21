[Components]
Name: "plugins\atlassian"; Description: {cm:atlassian}; Types: full custom; Flags: disablenouninstallwarning

[Files]
; The plugin and the libraries only it uses; the build removed its copies of the files Greenshot itself installs
Source: {#PluginDir}\Greenshot.Plugin.Atlassian\*.dll; DestDir: {app}\Plugins\Greenshot.Plugin.Atlassian; Components: plugins\atlassian; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.Atlassian\Languages\greenshot.jira.*.ini; DestDir: {app}\Languages; Components: plugins\atlassian; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.Atlassian\Languages\greenshot.confluence.*.ini; DestDir: {app}\Languages; Components: plugins\atlassian; Flags: {#DefaultInstallFlags};

[CustomMessages]
; Jira and Confluence were two plug-ins up to 1.4
atlassian=Jira and Confluence plug-in
en.atlassian=Jira and Confluence plug-in
de.atlassian=Jira- und Confluence-Plug-in
