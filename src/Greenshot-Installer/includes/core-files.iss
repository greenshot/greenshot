; Every program file by name, no wildcards: scripts\check-core-files.ps1 fails the build when the build output has one that
; isn't listed here (or the other way around), so nothing a package brings along ships by accident.
[Files]
Source: {#ReleaseDir}\Greenshot.exe; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Greenshot.Base.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Greenshot.Editor.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Greenshot.exe.config; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\log4net.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\log4net.xml; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.HttpExtensions.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.HttpExtensions.JsonNet.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Ini.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Log.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.AppRestartManager.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.Automation.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.Clipboard.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.Com.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.Common.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.DesktopWindowsManager.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.Dpi.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.Gdi32.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.Icons.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.Input.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.Kernel32.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.Messages.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.Multimedia.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.Shell32.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Dapplo.Windows.User32.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\SixLabors.ImageSharp.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\SixLabors.ImageSharp.Drawing.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\SixLabors.Fonts.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.Buffers.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.CommandLine.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.ComponentModel.Annotations.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.Memory.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.Numerics.Vectors.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.Reactive.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.Reactive.Linq.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.Reflection.TypeExtensions.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.Runtime.CompilerServices.Unsafe.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.Text.Encoding.CodePages.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.Threading.Tasks.Extensions.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\System.ValueTuple.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Svg.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\ExCSS.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\HtmlAgilityPack.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Newtonsoft.Json.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Microsoft.Toolkit.Uwp.Notifications.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Hardcodet.NotifyIcon.Wpf.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Microsoft.Bcl.AsyncInterfaces.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: {#ReleaseDir}\Microsoft.Bcl.HashCode.dll; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
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
Source: {#ReleaseDir}\greenshot-cli.exe; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}

; Core language files
Source: {#LanguagesDir}\*nl-NL*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: greenshot; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*en-US*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: greenshot; Flags: {#DefaultInstallFlags};
Source: {#LanguagesDir}\*de-DE*; Excludes: "*installer*,*website*"; DestDir: {app}\Languages; Components: greenshot; Flags: {#DefaultInstallFlags};

[Components]
Name: "disablesnippingtool"; Description: {cm:disablewin11snippingtool}; Flags: disablenouninstallwarning; Types: default full custom
Name: "greenshot"; Description: "Greenshot"; Types: default full compact custom; Flags: fixed
