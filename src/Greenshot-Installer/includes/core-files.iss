[Files]
Source: {#ReleaseDir}\Greenshot.exe; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Greenshot.Base.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Greenshot.Editor.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Greenshot.exe.config; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\log4net.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\log4net.xml; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.*.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\SixLabors.ImageSharp.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\SixLabors.ImageSharp.Drawing.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\SixLabors.Fonts.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.*.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Svg.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\ExCSS.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\HtmlAgilityPack.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Newtonsoft.Json.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Microsoft.Toolkit.*.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Microsoft.IO.RecyclableMemoryStream.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\checksum.SHA256; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\bom.json; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags} skipifsourcedoesntexist
Source: {#ReleaseDir}\bom.xml; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags} skipifsourcedoesntexist
Source: {#ReleaseDir}\manifest.spdx.json; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags} skipifsourcedoesntexist
Source: {#ReleaseDir}\manifest.spdx.json.sha256; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags} skipifsourcedoesntexist
Source: {#ReleaseDir}\Twemoji.Mozilla.ttf; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\emojis.xml; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: additional_files\installer.txt; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: additional_files\license.txt; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: additional_files\readme.txt; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\greenshot-proxy.exe; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\greenshot.com; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: additional_files\org.greenshot.proxy.json; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: additional_files\org.greenshot.proxy-firefox.json; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}

; Core language files
Source: {#LanguagesDir}\*nl-NL*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: greenshot; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*en-US*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: greenshot; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*de-DE*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: greenshot; Flags: {#DefaultInstallFlags};

[Components]
Name: "disablesnippingtool"; Description: {cm:disablewin11snippingtool}; Flags: disablenouninstallwarning; Types: default full custom
Name: "greenshot"; Description: "Greenshot"; Types: default full compact custom; Flags: fixed
