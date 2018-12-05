//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Dapplo.Config.Language;

namespace Greenshot.Addon.LegacyEditor
{
	[Language("Editor")]
	public interface IEditorLanguage : ILanguage
	{
	    string SettingsDestinationEditor { get; }

	    string EditorAlignBottom { get; }

		string EditorAlignCenter { get; }

		string EditorAlignHorizontal { get; }

		string EditorAlignLeft { get; }

		string EditorAlignMiddle { get; }

		string EditorAlignRight { get; }

		string EditorAlignTop { get; }

		string EditorAlignVertical { get; }

		string EditorArrange { get; }

		string EditorArrowheads { get; }

		string EditorArrowheadsBoth { get; }

		string EditorArrowheadsEnd { get; }

		string EditorArrowheadsNone { get; }

		string EditorArrowheadsStart { get; }

		string EditorAutocrop { get; }

		string EditorBackcolor { get; }

		string EditorBlurRadius { get; }

		string EditorBold { get; }

		string EditorBorder { get; }

		string EditorBrightness { get; }

		string EditorCancel { get; }

		string EditorClipboardfailed { get; }

		string EditorClose { get; }

		string EditorCloseOnSave { get; }

		string EditorCloseOnSaveTitle { get; }

		string EditorConfirm { get; }

		string EditorCopyimagetoclipboard { get; }

		string EditorCopypathtoclipboard { get; }

		string EditorCopytoclipboard { get; }

		string EditorCrop { get; }

		string EditorCursortool { get; }

		string EditorCuttoclipboard { get; }

		string EditorDeleteelement { get; }

		string EditorDownonelevel { get; }

		string EditorDowntobottom { get; }

		string EditorDrawarrow { get; }

		string EditorDrawellipse { get; }

		string EditorDrawfreehand { get; }

		string EditorDrawhighlighter { get; }

		string EditorDrawline { get; }

		string EditorDrawrectangle { get; }

		string EditorDrawtextbox { get; }

		string EditorDuplicate { get; }

		string EditorEdit { get; }

		string EditorEffects { get; }

		string EditorEmail { get; }

		string EditorFile { get; }

		string EditorFontsize { get; }

		string EditorForecolor { get; }

		string EditorGrayscale { get; }

		string EditorHighlightArea { get; }

		string EditorHighlightGrayscale { get; }

		string EditorHighlightMagnify { get; }

		string EditorHighlightMode { get; }

		string EditorHighlightText { get; }

		string EditorImagesaved { get; }

		string EditorInsertwindow { get; }

		string EditorItalic { get; }

		string EditorLoadObjects { get; }

		string EditorMagnificationFactor { get; }

		string EditorMatchCaptureSize { get; }

		string EditorObfuscate { get; }

		string EditorObfuscateBlur { get; }

		string EditorObfuscateMode { get; }

		string EditorObfuscatePixelize { get; }

		string EditorObject { get; }

		string EditorOpendirinexplorer { get; }

		string EditorPastefromclipboard { get; }

		string EditorPixelSize { get; }

		string EditorPreviewQuality { get; }

		string EditorPrint { get; }

		string EditorRedo { get; }

		string EditorResetsize { get; }

		string EditorResizePercent { get; }

		string EditorResizePixel { get; }

		string EditorSave { get; }

		string EditorSaveas { get; }

		string EditorSaveObjects { get; }

		string EditorSelectall { get; }

		string EditorSenttoprinter { get; }

		string EditorShadow { get; }

		string EditorStoredtoclipboard { get; }

		string EditorThickness { get; }

		string EditorTitle { get; }

		string EditorTornEdge { get; }

		string EditorUndo { get; }

		string EditorUponelevel { get; }

		string EditorUptotop { get; }
	}
}