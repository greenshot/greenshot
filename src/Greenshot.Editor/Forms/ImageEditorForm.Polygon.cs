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

using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Controls;
using Greenshot.Base.Languages;
using Greenshot.Base.Interfaces;
namespace Greenshot.Editor.Forms
{
    public partial class ImageEditorForm
    {
        private ToolStripButton _polygonButton;

        private void InitializePolygonTool()
        {
            _polygonButton = new ToolStripButton(Texts.Editor.Drawpolygon)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                AccessibleName = Texts.Editor.Drawpolygon,
                ToolTipText = "Polygon: click vertices; click the start or press Enter to close; Escape cancels"
            };
            IconBinder.Bind(_polygonButton, IconSource.FromResource(typeof(ImageEditorForm), "btnPolygon.Image"));
            _polygonButton.Click += (s, e) =>
            {
                _surface.DrawingMode = DrawingModes.Polygon;
                FocusCanvas(fromKeyboard: false);
            };
            toolsToolStrip.Items.Insert(toolsToolStrip.Items.IndexOf(btnPolyline) + 1, _polygonButton);
            var menu = new ToolStripMenuItem(Texts.Editor.Drawpolygon);
            menu.Click += (s, e) => _polygonButton.PerformClick();
            drawPolylineToolStripMenuItem.Owner.Items.Insert(drawPolylineToolStripMenuItem.Owner.Items.IndexOf(drawPolylineToolStripMenuItem) + 1, menu);
        }
    }
}