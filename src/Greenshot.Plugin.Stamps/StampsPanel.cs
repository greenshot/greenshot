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


using System;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Base.Languages;
using Greenshot.Plugin.Stamps.ViewModels;
using Greenshot.Plugin.Stamps.Views;

namespace Greenshot.Plugin.Stamps;

/// <summary>
/// The stamps as a panel of the editor: one view per editor window, all on the same stamps folder. Shown from the Plugins menu,
/// with Ctrl+Shift+K or from the panel list.
/// </summary>
internal sealed class StampsPanel : IEditorPanel
{
    public const string PanelId = "Greenshot.Stamps";

    private readonly Func<Library.StampLibrary> _library;

    /// <param name="library">the stamps folder, read the first time it's needed</param>
    public StampsPanel(Func<Library.StampLibrary> library)
    {
        _library = library;
    }

    public string Id => PanelId;

    public string Title => Texts.Get<IStampsLanguage>().Title;

    public int Order => 100;

    public string MenuText => Texts.Get<IStampsLanguage>().Menu;

    // Ctrl+Shift+M was taken as global hotkey by other programs (mute in meeting apps)
    public string Shortcut => "Ctrl+Shift+K";

    public bool IsAvailable(IEditorPanelContext context) => StampsPlugin.IsEnabled;

    public object CreateViewModel(IEditorPanelContext context) => StampsViewModel.ForEditor(_library(), context);

    public object CreateView(object viewModel)
    {
        var stampsViewModel = (StampsViewModel)viewModel;
        var view = new StampsView { DataContext = stampsViewModel };
        // Esc goes back to the capture, the panel stays
        view.CloseRequested += (s, e) => stampsViewModel.FocusTarget();
        return view;
    }
}
