/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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


namespace Greenshot.Base.Interfaces.Editor
{
    /// <summary>
    /// A panel in the area at the right of the canvas of the editor, e.g. Selection (the properties of the selected elements) or
    /// a stamp library. One panel is shown at a time, the user switches between them. Register it as a service
    /// (IPluginServices.AddServices&lt;IEditorPanel&gt;), the editor creates its view model and view once per editor window,
    /// the first time the panel is shown.
    /// The view model doesn't use a UI toolkit (only INotifyPropertyChanged and ICommand), so moving the editor to another toolkit
    /// only needs new views.
    /// </summary>
    public interface IEditorPanel
    {
        /// <summary>
        /// A key which doesn't change, it's used for the stored state, e.g. "Greenshot.Selection"
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The translated name, shown in the panel list and read by a screen reader
        /// </summary>
        string Title { get; }

        /// <summary>
        /// The position in the panel list, lower first
        /// </summary>
        int Order { get; }

        /// <summary>
        /// The translated text of the menu item which shows the panel (in the Plugins menu of the editor), null for none
        /// </summary>
        string MenuText { get; }

        /// <summary>
        /// The keys which show the panel in the editor, e.g. "Ctrl+Shift+K", null for none. Shown with the menu item.
        /// </summary>
        string Shortcut { get; }

        /// <summary>
        /// Whether the panel can be used in this editor window, e.g. false when a feature is turned off
        /// </summary>
        bool IsAvailable(IEditorPanelContext context);

        /// <summary>
        /// The view model of the panel for one editor window, it follows the window through the events of the context.
        /// When it implements IDisposable it is disposed when the window closes.
        /// </summary>
        object CreateViewModel(IEditorPanelContext context);

        /// <summary>
        /// The view for the view model, in the UI toolkit of the editor: a WPF FrameworkElement today
        /// </summary>
        object CreateView(object viewModel);
    }
}
