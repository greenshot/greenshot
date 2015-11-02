/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using GreenshotEditorPlugin.Drawing;
using GreenshotEditorPlugin.Forms;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Core;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Forms;
using System.ComponentModel.Composition;
using GreenshotPlugin.Interfaces.Destination;

namespace GreenshotEditorPlugin
{
	/// <summary>
	/// Description of EditorDestination.
	/// </summary>
	[Destination(_editorDesignation)]
	public class EditorDestination : AbstractDestination
	{
		private const string _editorDesignation = "Editor";
		private static readonly ILog LOG = LogManager.GetLogger(typeof (EditorDestination));
		private IImageEditor editor = null;
		private static Image greenshotIcon = GreenshotResources.GetGreenshotIcon().ToBitmap();
		[Import]
		public IEditorConfiguration EditorConfiguration
		{
			get;
			set;
		}

		[Import]
		public IGreenshotLanguage GreenshotLanguage
		{
			get;
			set;
		}

		public override string Designation
		{
			get
			{
				return _editorDesignation;
			}
		}

		public EditorDestination()
		{
			Export = async (capture, token) => await ExportCaptureAsync(capture, token);
			Text = $"Export to {_editorDesignation}";
        }

		public Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = _editorDesignation,
				SourceType = SourceTypes.Destination,
				Text = Text
			};

			// Make sure we collect the garbage before opening the screenshot
			GC.Collect();
			GC.WaitForPendingFinalizers();

			bool modified = capture.Modified;
			if (editor == null)
			{
				ISurface surface;
				if (capture == null && capture.CaptureDetails.Filename != null && capture.CaptureDetails.Filename.ToLower().EndsWith("." + OutputFormat.greenshot))
				{
					// Only a file, create a surface from the filename and continue!
					surface = new Surface();
					surface = ImageOutput.LoadGreenshotSurface(capture.CaptureDetails.Filename, surface);
					surface.CaptureDetails = capture.CaptureDetails;
				}
				else
				{
					surface = new Surface(capture);
				}
				bool reusedEditor = false;
				if (EditorConfiguration.ReuseEditor)
				{
					foreach (IImageEditor openedEditor in ImageEditorForm.Editors)
					{
						if (!openedEditor.Surface.Modified)
						{
							openedEditor.Surface = surface;
							reusedEditor = true;
                            break;
						}
					}
				}
				if (!reusedEditor)
				{
					try
					{
						var editorForm = new ImageEditorForm(surface, !surface.Modified); // Output made??

						if (!string.IsNullOrEmpty(surface.CaptureDetails.Filename))
						{
							editorForm.SetImagePath(surface.CaptureDetails.Filename);
						}
						editorForm.Show();
						editorForm.Activate();
						LOG.Debug("Finished opening Editor");
					}
					catch (Exception e)
					{
						LOG.Error(e);
						returnValue.NotificationType = NotificationTypes.Fail;
						returnValue.ErrorText = e.Message;
						returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, _editorDesignation);
					}
				}
			}
			else
			{
				try
				{
					using (Image image = capture.GetImageForExport())
					{
						editor.Surface.AddImageContainer(image, 10, 10);
					}
				}
				catch (Exception e)
				{
					LOG.Error(e);
					returnValue.NotificationType = NotificationTypes.Fail;
					returnValue.ErrorText = e.Message;
					returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, _editorDesignation);
                }
			}
			// Workaround for the modified flag when using the editor.
			capture.Modified = modified;
			return Task.FromResult<INotification>(returnValue);
		}
	}
}