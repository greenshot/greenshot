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
using Dapplo.Log;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Forms;
using Greenshot.Addons.Resources;
using Greenshot.Gfx;

namespace Greenshot.Addon.LegacyEditor
{
    /// <summary>
    ///     Description of EditorDestination.
    /// </summary> 
    [Destination("Editor", 1)]
    public class EditorDestination : AbstractDestination
	{
	    private readonly ExportNotification _exportNotification;
	    private readonly EditorFactory _editorFactory;
	    private readonly IEditorLanguage _editorLanguage;
	    private static readonly LogSource Log = new LogSource();
		private static readonly IBitmapWithNativeSupport greenshotIcon = BitmapWrapper.FromBitmap(GreenshotResources.Instance.GetGreenshotIcon().ToBitmap());
	    private readonly IImageEditor _editor;

        /// <summary>
        /// Default constructor so we can initiate this from MEF
        /// </summary>
	    public EditorDestination(
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
            ExportNotification exportNotification,
            EditorFactory editorFactory,
            IEditorLanguage editorLanguage) : base(coreConfiguration, greenshotLanguage)
        {
            _exportNotification = exportNotification;
            _editorFactory = editorFactory;
            _editorLanguage = editorLanguage;
        }

        public EditorDestination(
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
            ExportNotification exportNotification,
            EditorFactory editorFactory,
            IEditorLanguage editorLanguage,
            IImageEditor editor) : this(coreConfiguration, greenshotLanguage, exportNotification, editorFactory, editorLanguage)
		{
		    _editorFactory = editorFactory;
		    _editor = editor;
		}

	    public override string Description
		{
			get
			{
				if (_editor == null)
				{
					return _editorLanguage.SettingsDestinationEditor;
				}
				return _editorLanguage.SettingsDestinationEditor + " - " + _editor.CaptureDetails.Title;
			}
		}

	    public override bool IsDynamic => true;

	    public override IBitmapWithNativeSupport DisplayIcon => greenshotIcon;

	    public override IEnumerable<IDestination> DynamicDestinations()
		{
		    return _editorFactory.Editors.Select(someEditor => new EditorDestination(CoreConfiguration, GreenshotLanguage, _exportNotification, _editorFactory, _editorLanguage, someEditor));
		}

	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			// Make sure we collect the garbage before opening the screenshot
			GC.Collect();
			GC.WaitForPendingFinalizers();

			var modified = surface.Modified;

		    if (_editor != null)
		    {
		        try
		        {
		            using (var image = surface.GetBitmapForExport())
		            {
		                _editor.Surface.AddImageContainer(image, 10, 10);
		            }
		            exportInformation.ExportMade = true;
		        }
		        catch (Exception e)
		        {
		            Log.Error().WriteLine(e);
		            exportInformation.ErrorMessage = e.Message;
		        }
            }
		    else
		    {
		        _editorFactory.CreateOrReuse(surface, captureDetails);
		        exportInformation.ExportMade = true;
            }

		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
            // Workaround for the modified flag when using the editor.
            surface.Modified = modified;
			return exportInformation;
		}
	}
}