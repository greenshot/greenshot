[Components]
Name: "plugins\pdf"; Description: {cm:pdf}; Types: default full custom; Flags: disablenouninstallwarning

[Files]
; The plugin and the libraries only it uses; the build removed its copies of the files Greenshot itself installs
Source: {#SolutionDir}\Greenshot.Plugin.Pdf\Languages\greenshot.pdf.*.ini; DestDir: {app}\Languages; Components: plugins\pdf; Flags: {#DefaultInstallFlags};

[CustomMessages]
pdf=PDF export plug-in
en.pdf=PDF export plug-in
de.pdf=PDF-Export-Plug-in
