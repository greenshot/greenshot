using System.Diagnostics.CodeAnalysis;
using Dapplo.Config.Language;

namespace Greenshot.Addon.LegacyEditor.Configuration.Impl
{
    /// <summary>
    /// This implements IEditorLanguage and takes care of storing, all setters are replaced via AutoProperties.Fody
    /// </summary>
    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
#pragma warning disable CS1591
    public class EditorLanguageImpl : LanguageBase<IEditorLanguage>, IEditorLanguage
    {
        public string SettingsDestinationEditor { get; }
        public string EditorAlignBottom { get; }
        public string EditorAlignCenter { get; }
        public string EditorAlignHorizontal { get; }
        public string EditorAlignLeft { get; }
        public string EditorAlignMiddle { get; }
        public string EditorAlignRight { get; }
        public string EditorAlignTop { get; }
        public string EditorAlignVertical { get; }
        public string EditorArrange { get; }
        public string EditorArrowheads { get; }
        public string EditorArrowheadsBoth { get; }
        public string EditorArrowheadsEnd { get; }
        public string EditorArrowheadsNone { get; }
        public string EditorArrowheadsStart { get; }
        public string EditorAutocrop { get; }
        public string EditorBackcolor { get; }
        public string EditorBlurRadius { get; }
        public string EditorBold { get; }
        public string EditorBorder { get; }
        public string EditorBrightness { get; }
        public string EditorCancel { get; }
        public string EditorClipboardfailed { get; }
        public string EditorClose { get; }
        public string EditorCloseOnSave { get; }
        public string EditorCloseOnSaveTitle { get; }
        public string EditorConfirm { get; }
        public string EditorCopyimagetoclipboard { get; }
        public string EditorCopypathtoclipboard { get; }
        public string EditorCopytoclipboard { get; }
        public string EditorCrop { get; }
        public string EditorCursortool { get; }
        public string EditorCuttoclipboard { get; }
        public string EditorDeleteelement { get; }
        public string EditorDownonelevel { get; }
        public string EditorDowntobottom { get; }
        public string EditorDrawarrow { get; }
        public string EditorDrawellipse { get; }
        public string EditorDrawfreehand { get; }
        public string EditorDrawhighlighter { get; }
        public string EditorDrawline { get; }
        public string EditorDrawrectangle { get; }
        public string EditorDrawtextbox { get; }
        public string EditorDuplicate { get; }
        public string EditorEdit { get; }
        public string EditorEffects { get; }
        public string EditorEmail { get; }
        public string EditorFile { get; }
        public string EditorFontsize { get; }
        public string EditorForecolor { get; }
        public string EditorGrayscale { get; }
        public string EditorHighlightArea { get; }
        public string EditorHighlightGrayscale { get; }
        public string EditorHighlightMagnify { get; }
        public string EditorHighlightMode { get; }
        public string EditorHighlightText { get; }
        public string EditorImagesaved { get; }
        public string EditorInsertwindow { get; }
        public string EditorItalic { get; }
        public string EditorLoadObjects { get; }
        public string EditorMagnificationFactor { get; }
        public string EditorMatchCaptureSize { get; }
        public string EditorObfuscate { get; }
        public string EditorObfuscateBlur { get; }
        public string EditorObfuscateMode { get; }
        public string EditorObfuscatePixelize { get; }
        public string EditorObject { get; }
        public string EditorOpendirinexplorer { get; }
        public string EditorPastefromclipboard { get; }
        public string EditorPixelSize { get; }
        public string EditorPreviewQuality { get; }
        public string EditorPrint { get; }
        public string EditorRedo { get; }
        public string EditorResetsize { get; }
        public string EditorResizePercent { get; }
        public string EditorResizePixel { get; }
        public string EditorSave { get; }
        public string EditorSaveas { get; }
        public string EditorSaveObjects { get; }
        public string EditorSelectall { get; }
        public string EditorSenttoprinter { get; }
        public string EditorShadow { get; }
        public string EditorStoredtoclipboard { get; }
        public string EditorThickness { get; }
        public string EditorTitle { get; }
        public string EditorTornEdge { get; }
        public string EditorUndo { get; }
        public string EditorUponelevel { get; }
        public string EditorUptotop { get; }
    }
}
