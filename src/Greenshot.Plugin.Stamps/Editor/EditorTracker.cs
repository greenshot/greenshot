/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;

namespace Greenshot.Plugin.Stamps.Editor;

/// <summary>
/// Adds "Stamps..." to the plugin menu of every editor window and knows which editor window was active last: the stamps go there.
/// Only this class knows the WinForms editor, the rest of the plugin uses <see cref="IEditorWindow"/>.
/// </summary>
internal sealed class EditorTracker : IEditorPlugin
{
    // In the editor only, Ctrl+Shift+M isn't used there
    private const Keys MenuShortcut = Keys.Control | Keys.Shift | Keys.M;

    // The open editor windows, the one which was active last at the end
    private readonly List<IEditorWindow> _editors = new();
    private readonly Action<IEditorWindow> _showStamps;

    /// <param name="showStamps">shows the stamps for the editor window</param>
    public EditorTracker(Action<IEditorWindow> showStamps)
    {
        _showStamps = showStamps;
    }

    /// <summary>
    /// The editor window which was active last, null when none is open
    /// </summary>
    public IEditorWindow ActiveEditor => _editors.LastOrDefault();

    /// <summary>
    /// Another editor window became the one which was active last, or the last one was closed. On the UI thread.
    /// </summary>
    public event EventHandler ActiveEditorChanged;

    public void InitializeEditor(Form editorForm, ToolStripMenuItem pluginMenu, Func<ISurface> currentSurface)
    {
        if (editorForm is not IEditorWindow editor)
        {
            return;
        }

        var menuItem = new ToolStripMenuItem(Texts.Get<IStampsLanguage>().Menu)
        {
            ShortcutKeys = MenuShortcut
        };
        menuItem.Click += (s, e) => _showStamps(editor);
        pluginMenu.DropDownItems.Add(menuItem);

        editor.Activated += (s, e) => MakeActive(editor);
        editor.Closed += (s, e) => Remove(editor);
        MakeActive(editor);
    }

    private void MakeActive(IEditorWindow editor)
    {
        if (ActiveEditor == editor)
        {
            return;
        }

        _editors.Remove(editor);
        _editors.Add(editor);
        ActiveEditorChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Remove(IEditorWindow editor)
    {
        bool wasActive = ActiveEditor == editor;
        _editors.Remove(editor);
        if (wasActive)
        {
            ActiveEditorChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
