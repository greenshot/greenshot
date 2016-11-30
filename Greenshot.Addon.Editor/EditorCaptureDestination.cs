//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Log;
using Greenshot.Addon.Editor.Configuration;
using Greenshot.Addon.Editor.Drawing;
using Greenshot.Addon.Editor.Extensions;
using Greenshot.Addon.Editor.Forms;
using Greenshot.Addon.Editor.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Core.Configuration;
using Greenshot.Core.Interfaces;
using Greenshot.Legacy.Extensions;

#endregion

namespace Greenshot.Addon.Editor
{
	/// <summary>
	///     The EditorCaptureDestination will export the capture to the editor
	/// </summary>
	public sealed class EditorCaptureDestination : ICaptureDestination
	{
		private static readonly LogSource Log = new LogSource();

		public IEditorConfiguration EditorConfiguration { get; set; }

		/// <summary>
		/// The editor, if we want to reuse.
		/// </summary>
		public IImageEditor Editor { get; set; }

		public Task ExportCaptureAsync(ICaptureFlow captureFlow, CancellationToken cancellationToken = new CancellationToken())
		{
			// Make sure we collect the garbage before opening the screenshot
			GC.Collect();
			GC.WaitForPendingFinalizers();

			var capture = captureFlow.Capture;
			if (capture == null)
			{
				throw new ArgumentNullException(nameof(capture));
			}

			bool modified = capture.Modified;
			if (Editor == null)
			{
				ISurface surface;
				if (capture.CaptureDetails?.Filename?.ToLower().EndsWith("." + OutputFormat.greenshot) == true)
				{
					// Only a file, create a surface from the filename and continue!
					surface = new Surface();
					surface = SurfaceExtensions.LoadGreenshotSurface(capture.CaptureDetails.Filename, surface);
					surface.CaptureDetails = capture.CaptureDetails;
				}
				else
				{
					surface = new Surface(capture);
				}
				bool reusedEditor = false;
				if (EditorConfiguration.ReuseEditor)
				{
					foreach (var openedEditor in ImageEditorForm.Editors)
					{
						if (openedEditor.Surface.Modified)
						{
							continue;
						}
						openedEditor.Surface = surface;
						reusedEditor = true;
						Editor = openedEditor;
						break;
					}
				}
				if (!reusedEditor)
				{
					var editorForm = new ImageEditorForm(surface, !surface.Modified); // Output made??
					Editor = editorForm;
					if (!string.IsNullOrEmpty(surface.CaptureDetails.Filename))
					{
						editorForm.SetImagePath(surface.CaptureDetails.Filename);
					}
					// TODO: Async??
					editorForm.Show();
					editorForm.Activate();
					Log.Debug().WriteLine("Finished opening Editor");

				}
			}
			else
			{
				using (Image image = capture.GetImageForExport())
				{
					Editor.Surface.AddImageContainer(image, 10, 10);
				}
				// Workaround for the modified flag when using the editor.
				capture.Modified = modified;
			}

			var editor = Editor as Form;

			return editor.WaitForClosedAsync(cancellationToken: cancellationToken);
		}

		public string Name => BuildInDestinations.Editor.ToString();
	}
}