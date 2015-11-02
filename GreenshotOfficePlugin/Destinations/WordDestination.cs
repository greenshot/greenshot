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

using GreenshotOfficePlugin.OfficeExport;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Destination;
using GreenshotPlugin.Interfaces.Plugin;
using log4net;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotOfficePlugin.Destinations
{
	/// <summary>
	/// Description of WordDestination.
	/// </summary>
	[Destination(_wordDesignation)]
	public class WordDestination : AbstractDestination
	{
		private const string _wordDesignation = "Word";
		private static readonly ILog LOG = LogManager.GetLogger(typeof(WordDestination));

		[Import]
		public IOfficeConfiguration OfficeConfiguration
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

		public string DocumentCaption
		{
			get;
			set;
		}

		public override string Designation
		{
			get
			{
				return _wordDesignation;
			}
		}

		public WordDestination()
		{
			Export = async (capture, token) => await ExportCaptureAsync(capture, token);
			Text = Text = $"Export to {_wordDesignation}";
		}

		public Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			INotification returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = _wordDesignation,
				SourceType = SourceTypes.Destination,
				Text = $"Exported to {_wordDesignation}"
			};
			string tmpFile = capture.CaptureDetails.Filename;
			if (tmpFile == null || capture.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
			{
				tmpFile = ImageOutput.SaveNamedTmpFile(capture, capture.CaptureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
			}
			if (DocumentCaption != null)
			{
				try
				{
					WordExporter.InsertIntoExistingDocument(DocumentCaption, tmpFile);
				}
				catch (Exception)
				{
					try
					{
						WordExporter.InsertIntoExistingDocument(DocumentCaption, tmpFile);
					}
					catch (Exception ex)
					{
						LOG.Error(ex);
						returnValue.ErrorText = ex.Message;
						returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, _wordDesignation);
                        return Task.FromResult(returnValue);
					}
				}
			}
			else
			{
				// TODO:
				//if (!manuallyInitiated)
				if (false)
				{
					Children.Clear();
					foreach(var caption in WordExporter.GetWordDocuments().OrderBy(x => x))
					{
						Children.Add(new WordDestination { DocumentCaption = caption });
					}

					if (Children.Count > 0)
					{
						// Return the ExportInformation from the picker without processing, as this indirectly comes from us self
						// TODO:
						// return await ShowPickerMenuAsync(false, capture, destinations, token).ConfigureAwait(false);
					}
				}
				try
				{
					WordExporter.InsertIntoNewDocument(tmpFile, null, null);
				}
				catch (Exception)
				{
					// Retry once, just in case
					try
					{
						WordExporter.InsertIntoNewDocument(tmpFile, null, null);
					}
					catch (Exception ex)
					{
						LOG.Error(ex);
						returnValue.ErrorText = ex.Message;
						returnValue.Text = string.Format(GreenshotLanguage.DestinationExportFailed, _wordDesignation);
						return Task.FromResult(returnValue);
					}
				}
			}
			return Task.FromResult(returnValue);
		}
	}
}