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
using System.Linq;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Plugin.Zxing.Drawing;

namespace Greenshot.Plugin.Zxing.ViewModels
{
    /// <summary>
    /// The Barcode section of the Selection panel for the one selected barcode: a change of a field changes the barcode, one undo
    /// step per change (typing in one field is one step). The fields work on a copy, a value the format can't encode (e.g. letters
    /// for EAN-13) isn't given to the barcode, the section shows why. No UI toolkit here.
    /// </summary>
    public sealed class BarcodeSectionViewModel : IDisposable
    {
        private readonly IEditorPanelContext _context;
        private BarcodeContainer _barcode;

        public BarcodeSectionViewModel(IEditorPanelContext context)
        {
            _context = context;
            Editor = new BarcodeEditorViewModel(new ZxingModel(), showsPreview: false);
            Editor.FieldChanged += OnFieldChanged;
            Refresh();
            _context.SelectionChanged += OnChanged;
            _context.CurrentSurfaceChanged += OnChanged;
            _context.ElementsChanged += OnChanged;
        }

        /// <summary>
        /// The fields of the selected barcode
        /// </summary>
        public BarcodeEditorViewModel Editor { get; }

        /// <summary>
        /// The one selected barcode, else null
        /// </summary>
        internal static BarcodeContainer SelectedBarcode(IEditorPanelContext context)
        {
            var selected = context.CurrentSurface?.SelectedElements;
            return selected?.Count == 1 && selected.First() is BarcodeContainer { Model: not null } barcode ? barcode : null;
        }

        /// <summary>
        /// Show the values of the selected barcode, e.g. after another one was selected or a change was undone
        /// </summary>
        private void Refresh()
        {
            _barcode = SelectedBarcode(_context);
            if (_barcode != null)
            {
                Editor.Model = _barcode.Model.Clone();
            }
        }

        private void OnChanged(object sender, EventArgs e) => Refresh();

        private void OnFieldChanged(object sender, string field)
        {
            var surface = _context.CurrentSurface;
            if (_barcode == null || surface == null || !Editor.IsValid)
            {
                return;
            }

            surface.MakeUndoable(new BarcodeChangeMemento(_barcode, field), true);
            _barcode.Invalidate();
            _barcode.Model.CopyFrom(Editor.Model);
            // Takes the margin and makes the barcode again
            _barcode.Margin = Editor.Model.Margin;
            _barcode.Invalidate();
            surface.Modified = true;
        }

        public void Dispose()
        {
            Editor.FieldChanged -= OnFieldChanged;
            _context.SelectionChanged -= OnChanged;
            _context.CurrentSurfaceChanged -= OnChanged;
            _context.ElementsChanged -= OnChanged;
        }
    }
}
