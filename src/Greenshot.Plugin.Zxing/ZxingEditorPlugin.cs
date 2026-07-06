/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.Zxing;

public class ZxingEditorPlugin : IEditorPlugin
{
    private readonly IZxingConfiguration _config;

    public ZxingEditorPlugin(IZxingConfiguration config)
    {
        _config = config;
    }

    public void InitializeEditor(Form editorForm, ToolStripMenuItem pluginMenu, ISurface surface)
    {
        var menuItem = new ToolStripMenuItem("Insert QR / Barcode...", null, (s, e) =>
        {
            using (var form = new ZxingEditorForm())
            {
                if (form.ShowDialog(editorForm) == DialogResult.OK && form.GeneratedBitmap != null)
                {
                    var container = surface.AddImageContainer(form.GeneratedBitmap, 50, 50);
                    var model = new ZxingModel();
                    form.PopulateModel(model);
                    container.Tag = model;
                }
            }
        });

        pluginMenu.DropDownItems.Add(menuItem);
    }
}
