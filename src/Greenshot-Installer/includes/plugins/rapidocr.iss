[Components]
Name: "plugins\rapidocr"; Description: {cm:rapidocr}; Types: full custom; Flags: disablenouninstallwarning

[Files]
; Only the plugin and the managed part of ONNX Runtime: onnxruntime.dll and the models are downloaded when the user chooses RapidOCR
Source: {#PluginDir}\Greenshot.Plugin.RapidOcr\*.dll; DestDir: {app}\Plugins\Greenshot.Plugin.RapidOcr; Components: plugins\rapidocr; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.RapidOcr\Languages\greenshot.rapidocr.*.ini; DestDir: {app}\Languages; Components: plugins\rapidocr; Flags: {#DefaultInstallFlags};

[CustomMessages]
rapidocr=RapidOCR text recognition plug-in (downloads its models when chosen)
en.rapidocr=RapidOCR text recognition plug-in (downloads its models when chosen)
de.rapidocr=RapidOCR-Texterkennung (lädt seine Modelle herunter, wenn sie gewählt wird)

[UninstallDelete]
// The downloaded onnxruntime.dll and models next to greenshot.ini (only greenshot.ini itself is kept)
Type: filesandordirs; Name: "{userappdata}\Greenshot\RapidOCR"
