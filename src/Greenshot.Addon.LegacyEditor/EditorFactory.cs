// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using Greenshot.Addon.LegacyEditor.Forms;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Forms;

namespace Greenshot.Addon.LegacyEditor
{
    /// <summary>
    /// This provides a way to find and create the editors
    /// </summary>
    public class EditorFactory
    {
        private readonly IEditorConfiguration _editorConfiguration;
        private readonly Func<ImageEditorForm> _imageEditorFactory;
        private readonly IList<ImageEditorForm> _editorList = new List<ImageEditorForm>();

        public EditorFactory(
            IEditorConfiguration editorConfiguration,
            Func<ImageEditorForm> imageEditorFactory,
            Func<ISurface> surfaceExportFactory)
        {
            _editorConfiguration = editorConfiguration;
            _imageEditorFactory = imageEditorFactory;
            // Factory for surface objects
            ImageOutput.SurfaceFactory = surfaceExportFactory;
        }

        /// <summary>
        /// Returns the existing editors
        /// </summary>
        public IEnumerable<IImageEditor> Editors => _editorList.OrderBy(editor => editor?.CaptureDetails?.Title);

        /// <summary>
        /// Remove the editor for the available list
        /// </summary>
        /// <param name="imageEditor">ImageEditorForm</param>
        public void Remove(ImageEditorForm imageEditor)
        {
            _editorList.Remove(imageEditor);
        }

        /// <summary>
        /// Creates an editor, or reuses an existing one
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <param name="captureDetails">ICaptureDetails</param>
        /// <returns>IImageEditor</returns>
        public IImageEditor CreateOrReuse(ISurface surface, ICaptureDetails captureDetails = null)
        {
            ImageEditorForm editorToReturn;
            if (_editorConfiguration.ReuseEditor)
            {
                editorToReturn = _editorList.FirstOrDefault(e => !e.Surface.Modified);
                if (editorToReturn != null)
                {
                    editorToReturn.Surface = surface;
                    return editorToReturn;
                }
            }

            editorToReturn = _imageEditorFactory();
            editorToReturn.Surface = surface;
            _editorList.Add(editorToReturn);
            if (!string.IsNullOrEmpty(captureDetails?.Filename))
            {
                editorToReturn.SetImagePath(captureDetails.Filename);
            }
            editorToReturn.Show();
            editorToReturn.Activate();

            return editorToReturn;
        }
    }
}