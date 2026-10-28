/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using Avalonia;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Editor;

namespace Greenshot.Linux.Editing;

/// <summary>
/// One image in the editor, shown as a tab of an editor window: the surface with what belongs to it (the controller, the view,
/// the property bar, the file, the status message and the scroll position). A tab can move to another window.
/// The EditorTab of the Windows editor.
/// </summary>
public sealed class EditorTab : Greenshot.Linux.Recipes.IImageEditor, IDisposable
{
    internal EditorTab(IEditorHost host, EditorSurface surface, CaptureDetails details, string? filePath)
    {
        Surface = surface;
        CaptureDetails = details;
        FilePath = filePath;
        Controller = new EditorController(surface, host.EditorSettings);
        Canvas = new EditorCanvas(Controller);
        PropertyBar = new PropertyBar(Controller, host.EditorSettings);
    }

    public EditorSurface Surface { get; }

    public CaptureDetails CaptureDetails { get; }

    internal EditorController Controller { get; }

    internal EditorCanvas Canvas { get; }

    internal PropertyBar PropertyBar { get; }

    /// <summary>
    /// The file it was opened from or saved to
    /// </summary>
    internal string? FilePath { get; set; }

    /// <summary>
    /// The last message (export result), shown in the status bar while the tab is selected
    /// </summary>
    internal string? StatusText { get; set; }

    /// <summary>
    /// The scroll position, kept while another tab is shown
    /// </summary>
    internal Vector ScrollOffset { get; set; }

    /// <summary>
    /// The window which shows the tab
    /// </summary>
    internal EditorWindow? Window { get; set; }

    /// <summary>
    /// The text of the tab: the file name when it was saved or opened, else the title of the capture
    /// </summary>
    public string Title => FilePath != null ? Path.GetFileName(FilePath) : string.IsNullOrEmpty(CaptureDetails.Title) ? Texts.Editor("new_image") : CaptureDetails.Title;

    /// <summary>
    /// The full path when it was saved, else the title; for the tooltip of the tab
    /// </summary>
    public string Description => FilePath ?? Title;

    /// <summary>
    /// A recipe of the Recipes menu sends its result back to this image (TargetEditor CurrentEditor): it can be undone
    /// </summary>
    public void ReplaceContent(EditorSurface result)
    {
        Surface.Checkpoint();
        Surface.ReplaceImage(result.Image.Copy(), SkiaSharp.SKMatrix.Identity);
        Surface.LoadElements(result.SerializeElements());
        Surface.IsModified = true;
        Surface.Invalidate();
    }

    public void Dispose() => Surface.Dispose();
}
