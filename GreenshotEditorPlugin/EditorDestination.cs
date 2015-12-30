/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

using GreenshotEditorPlugin.Drawing;
using GreenshotEditorPlugin.Forms;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Core;

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Forms;
using System.ComponentModel.Composition;
using System.Windows.Media.Imaging;
using GreenshotPlugin.Extensions;
using GreenshotPlugin.Interfaces.Destination;

namespace GreenshotEditorPlugin
{
	/// <summary>
	/// Description of EditorDestination.
	/// </summary>
	[Destination(EditorDesignation)]
	public sealed class EditorDestination : AbstractDestination
	{
		private const string EditorDesignation = "Editor";
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(EditorDestination));
		private static readonly BitmapSource GreenshotIcon = GreenshotResources.GetGreenshotIcon().ToBitmapSource();

		[Import]
		private IEditorConfiguration EditorConfiguration
		{
			get;
			set;
		}

		[Import]
		private IGreenshotLanguage GreenshotLanguage
		{
			get;
			set;
		}

		protected override void Initialize()
		{
			base.Initialize();
			Text = GreenshotLanguage.SettingsDestinationEditor;
			Designation = EditorDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, null, token);
			Icon = GreenshotIcon;
		}

		/// <summary>
		/// Load the current editors to export to
		/// </summary>
		/// <param name="caller1"></param>
		/// <param name="token"></param>
		/// <returns>Task</returns>
		public override Task RefreshAsync(IExportContext caller1, CancellationToken token = default(CancellationToken))
		{
			Children.Clear();
			foreach (var openEditor in ImageEditorForm.Editors)
			{
				var editorDestination = new EditorDestination
				{
					Text = openEditor.Surface.CaptureDetails.Title,
					Export = async (caller, capture, exportToken) => await ExportCaptureAsync(capture, openEditor, exportToken),
					Icon = GreenshotIcon,
					EditorConfiguration = EditorConfiguration,
					GreenshotLanguage = GreenshotLanguage
				};
				Children.Add(editorDestination);
			}
			return Task.FromResult(true);
		}

		private Task<INotification> ExportCaptureAsync(ICapture capture, IImageEditor editor, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = EditorDesignation,
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
					foreach (var openedEditor in ImageEditorForm.Editors)
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
						LOG.Error(e, "Editor export failed");
						returnValue.NotificationType = NotificationTypes.Fail;
						returnValue.ErrorText = e.Message;
						returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, EditorDesignation);
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
					LOG.Error(e, "Failed to add an image to an already opened editor");
					returnValue.NotificationType = NotificationTypes.Fail;
					returnValue.ErrorText = e.Message;
					returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, EditorDesignation);
                }
			}
			// Workaround for the modified flag when using the editor.
			capture.Modified = modified;
			return Task.FromResult<INotification>(returnValue);
		}
	}
}