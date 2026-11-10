[Components]
Name: "plugins\rapidocr"; Description: {cm:rapidocr}; Types: full custom; Flags: disablenouninstallwarning

[Files]
; Only the plugin and the managed part of ONNX Runtime: onnxruntime.dll and the models are downloaded when the user chooses RapidOCR
Source: {#PluginDir}\Greenshot.Plugin.RapidOcr\*.dll; DestDir: {app}\Plugins\Greenshot.Plugin.RapidOcr; Components: plugins\rapidocr; Flags: {#DefaultInstallFlags};
; The licenses of RapidOCR, the models and ONNX Runtime
Source: {#SolutionDir}\Greenshot.Plugin.RapidOcr\Licenses\*; DestDir: {app}\Plugins\Greenshot.Plugin.RapidOcr\Licenses; Components: plugins\rapidocr; Flags: {#DefaultInstallFlags};
Source: {#SolutionDir}\Greenshot.Plugin.RapidOcr\Languages\greenshot.rapidocr.*.ini; DestDir: {app}\Languages; Components: plugins\rapidocr; Flags: {#DefaultInstallFlags};

[CustomMessages]
rapidocr=RapidOCR text recognition plug-in (downloads its models when chosen)
en.rapidocr=RapidOCR text recognition plug-in (downloads its models when chosen)
de.rapidocr=RapidOCR-Texterkennung (lädt ihre Modelle herunter, wenn sie gewählt wird)
es.rapidocr=Extensión de reconocimiento de texto RapidOCR (descarga sus modelos al elegirla)
fi.rapidocr=RapidOCR-tekstintunnistuksen liitännäinen (lataa mallinsa, kun se valitaan)
fr.rapidocr=Greffon de reconnaissance de texte RapidOCR (télécharge ses modèles une fois choisi)
it.rapidocr=Plugin di riconoscimento del testo RapidOCR (scarica i suoi modelli quando viene scelto)
lv.rapidocr=RapidOCR teksta atpazīšanas spraudnis (lejupielādē modeļus, kad tiek izvēlēts)
nl.rapidocr=RapidOCR-tekstherkenning plug-in (downloadt de modellen wanneer gekozen)
nn.rapidocr=RapidOCR-tekstattkjenning (lastar ned modellane når det blir valt)
ptBR.rapidocr=Plug-in de reconhecimento de texto RapidOCR (baixa os modelos quando escolhido)
ru.rapidocr=Плагин распознавания текста RapidOCR (скачивает модели при выборе)
sr.rapidocr=Прикључак за препознавање текста RapidOCR (преузима моделе када се изабере)
sv.rapidocr=RapidOCR-textigenkänning (laddar ned sina modeller när det väljs)
tr.rapidocr=RapidOCR metin tanıma eklentisi (seçildiğinde modellerini indirir)
uk.rapidocr=Плагін розпізнавання тексту RapidOCR (завантажує моделі після вибору)
cn.rapidocr=RapidOCR 文本识别插件（选择后下载模型）

[UninstallDelete]
// The downloaded onnxruntime.dll and models next to greenshot.ini (only greenshot.ini itself is kept)
Type: filesandordirs; Name: "{userappdata}\Greenshot\RapidOCR"
