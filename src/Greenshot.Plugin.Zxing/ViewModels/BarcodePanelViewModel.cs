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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Plugin.Zxing.Drawing;

namespace Greenshot.Plugin.Zxing.ViewModels
{
    /// <summary>
    /// The Barcode panel of one editor window: the fields of a new barcode with a preview, Insert puts it into the shown capture.
    /// No UI toolkit here.
    /// </summary>
    public sealed class BarcodePanelViewModel : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// The Selection panel of the editor, it shows the Barcode section of the inserted barcode
        /// </summary>
        private const string SelectionPanelId = "Greenshot.Selection";

        private readonly IEditorPanelContext _context;

        public BarcodePanelViewModel(IEditorPanelContext context)
        {
            _context = context;
            Editor = new BarcodeEditorViewModel(new ZxingModel { RawText = "https://getgreenshot.org", EpcAmount = "10.00" });
            Editor.PropertyChanged += OnEditorChanged;
            _context.CurrentSurfaceChanged += OnCurrentSurfaceChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The fields of the barcode which is inserted next
        /// </summary>
        public BarcodeEditorViewModel Editor { get; }

        public bool CanInsert => Editor.IsValid && _context.CurrentSurface != null;

        /// <summary>
        /// Put the barcode into the visible part of the shown capture as one undo step, select it, show it in the Selection panel
        /// (most captures get one barcode, it's changed there) and give the keyboard to the canvas
        /// </summary>
        /// <returns>the new barcode, null when there is nothing to insert</returns>
        public BarcodeContainer Insert()
        {
            var surface = _context.CurrentSurface;
            if (!CanInsert)
            {
                return null;
            }

            var barcode = new BarcodeContainer(surface, Editor.Model.Clone());
            bool is2D = Editor.Is2D;
            barcode.Width = is2D ? 160 : 250;
            barcode.Height = is2D ? 160 : 80;
            barcode.RegenerateBarcode();
            surface.InsertElements(new[] { barcode });
            _context.ShowPanel(SelectionPanelId, false);
            // The new barcode is selected there, the keys move and size it
            _context.FocusCanvas();
            return barcode;
        }

        private void OnEditorChanged(object sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(CanInsert));

        private void OnCurrentSurfaceChanged(object sender, EventArgs e) => OnPropertyChanged(nameof(CanInsert));

        public void Dispose()
        {
            Editor.PropertyChanged -= OnEditorChanged;
            _context.CurrentSurfaceChanged -= OnCurrentSurfaceChanged;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
