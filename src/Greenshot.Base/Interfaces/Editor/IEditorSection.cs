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
    /// A section of the Selection panel: a part of the properties of the selected elements, or of the tool which is picked.
    /// The panel shows the sections which apply, stacked by <see cref="Order"/>, e.g. size and position on top and the emoji
    /// of a selected emoji below. Register it as a service (IPluginServices.AddServices&lt;IEditorSection&gt;), e.g. for the
    /// elements of a plugin. View model and view are created once per editor window, the first time the section applies.
    /// </summary>
    public interface IEditorSection
    {
        /// <summary>
        /// A key which doesn't change, it's used for the stored state (collapsed or not), e.g. "Greenshot.Emoji"
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The translated heading of the section
        /// </summary>
        string Title { get; }

        /// <summary>
        /// The position in the panel, lower first, see <see cref="EditorSectionOrder"/>
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Whether the section is shown for the current selection and tool of the context
        /// </summary>
        bool AppliesTo(IEditorPanelContext context);

        /// <summary>
        /// true: when the section starts to apply, the editor shows the Selection panel by itself (without taking the focus),
        /// also when the user closed it before: a choice is needed. The panel never closes by itself.
        /// Only for sections which are needed to use a tool, like the emoji to place.
        /// </summary>
        bool ShowsPanel { get; }

        /// <summary>
        /// true: the section gets the height of the panel which the other sections leave (at least a small part), e.g. a list to pick from.
        /// Its view must then scroll its content itself. false: the section is as high as its content.
        /// When several sections ask for it, the first one in the panel gets it.
        /// </summary>
        bool FillsHeight { get; }

        /// <summary>
        /// The view model of the section for one editor window, without a UI toolkit (see <see cref="IEditorPanel"/>).
        /// When it implements IDisposable it is disposed when the window closes.
        /// </summary>
        object CreateViewModel(IEditorPanelContext context);

        /// <summary>
        /// The view for the view model, in the UI toolkit of the editor: a WPF FrameworkElement today
        /// </summary>
        object CreateView(object viewModel);
    }

    /// <summary>
    /// Where a section goes in the Selection panel
    /// </summary>
    public static class EditorSectionOrder
    {
        /// <summary>
        /// Size and position: small and the same for every element, so it's always at the same place
        /// </summary>
        public const int SizeAndPosition = 0;

        /// <summary>
        /// Copy and apply style and size: for every element, right below size and position
        /// </summary>
        public const int StyleAndSize = 10;

        /// <summary>
        /// The sections of the kind of element, e.g. the emoji of an emoji
        /// </summary>
        public const int Element = 100;

        /// <summary>
        /// Everything else
        /// </summary>
        public const int Other = 1000;
    }
}
