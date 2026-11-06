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


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Base.Languages;
using Greenshot.Editor.Configuration;
using log4net;

namespace Greenshot.Editor.Panels.ViewModels
{
    /// <summary>
    /// The Selection panel: the sections which apply to the selection and the tool, sorted by their order.
    /// No UI toolkit here, the view shows the items.
    /// </summary>
    internal sealed class SelectionPanelViewModel : INotifyPropertyChanged, IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SelectionPanelViewModel));
        private readonly IEditorPanelContext _context;
        private readonly IEditorConfiguration _configuration;
        private readonly List<EditorSectionItem> _items;

        public SelectionPanelViewModel(IEditorPanelContext context, IEnumerable<IEditorSection> sections, IEditorConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _items = sections.OrderBy(section => section.Order).Select(section => new EditorSectionItem(section, context, configuration)).ToList();
            _context.SelectionChanged += OnContextChanged;
            _context.DrawingModeChanged += OnContextChanged;
            _context.CurrentSurfaceChanged += OnContextChanged;
            Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The sections which apply now, top to bottom
        /// </summary>
        public ObservableCollection<EditorSectionItem> Sections { get; } = new();

        /// <summary>
        /// No section applies: the panel shows a hint
        /// </summary>
        public bool IsEmpty => Sections.Count == 0;

        public string EmptyText => Texts.Editor.PanelSelectionEmpty;

        private void OnContextChanged(object sender, EventArgs e) => Refresh();

        /// <summary>
        /// Show the sections which apply, keep the items (and their views) of the sections which stay
        /// </summary>
        internal void Refresh()
        {
            var wanted = _items.Where(item => Applies(item.Section)).ToList();
            for (int i = Sections.Count - 1; i >= 0; i--)
            {
                if (!wanted.Contains(Sections[i]))
                {
                    Sections.RemoveAt(i);
                }
            }

            for (int i = 0; i < wanted.Count; i++)
            {
                int current = Sections.IndexOf(wanted[i]);
                if (current < 0)
                {
                    Sections.Insert(i, wanted[i]);
                }
                else if (current != i)
                {
                    Sections.Move(current, i);
                }
            }

            OnPropertyChanged(nameof(IsEmpty));
        }

        private bool Applies(IEditorSection section)
        {
            try
            {
                return section.AppliesTo(_context);
            }
            catch (Exception ex)
            {
                // A broken section of a plugin doesn't break the editor
                Log.Warn($"The editor section {section.Id} failed", ex);
                return false;
            }
        }

        public void Dispose()
        {
            _context.SelectionChanged -= OnContextChanged;
            _context.DrawingModeChanged -= OnContextChanged;
            _context.CurrentSurfaceChanged -= OnContextChanged;
            foreach (var item in _items)
            {
                item.Dispose();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// One section of the Selection panel: its view model is created the first time it's shown, the expanded state is stored
    /// </summary>
    internal sealed class EditorSectionItem : INotifyPropertyChanged, IDisposable
    {
        private readonly IEditorPanelContext _context;
        private readonly IEditorConfiguration _configuration;
        private object _viewModel;

        public EditorSectionItem(IEditorSection section, IEditorPanelContext context, IEditorConfiguration configuration)
        {
            Section = section;
            _context = context;
            _configuration = configuration;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public IEditorSection Section { get; }

        public string Title => Section.Title;

        /// <summary>
        /// The view model of the section, created on first use
        /// </summary>
        public object ViewModel => _viewModel ??= Section.CreateViewModel(_context);

        /// <summary>
        /// The view of the toolkit, kept by the view of the panel so it isn't built again when the section comes back
        /// </summary>
        public object View { get; set; }

        /// <summary>
        /// Expanded, unless the user collapsed it (stored for all editor windows)
        /// </summary>
        public bool IsExpanded
        {
            get => !_configuration.CollapsedEditorSections.Contains(Section.Id);
            set
            {
                if (value == IsExpanded)
                {
                    return;
                }

                var collapsed = _configuration.CollapsedEditorSections.Where(id => id != Section.Id).ToList();
                if (!value)
                {
                    collapsed.Add(Section.Id);
                }

                _configuration.CollapsedEditorSections = collapsed;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }

        public void Dispose()
        {
            (_viewModel as IDisposable)?.Dispose();
            _viewModel = null;
            View = null;
        }
    }
}
