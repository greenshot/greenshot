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

using System.Drawing;
using System.Windows.Forms;
using GreenshotPlugin.Core;
using Dapplo.Config.Ini;
using log4net;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Config.Language;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Interfaces;

namespace Greenshot.Destinations
{
	/// <summary>
	/// Description of FileWithDialog.
	/// </summary>
	public class FileWithDialogLegacyDestination : AbstractLegacyDestination
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (FileWithDialogLegacyDestination));
		private static readonly ICoreConfiguration conf = IniConfig.Current.Get<ICoreConfiguration>();
		private static readonly IGreenshotLanguage language = LanguageLoader.Current.Get<IGreenshotLanguage>();

		public override string Designation
		{
			get
			{
				return BuildInDestinationEnum.FileDialog.ToString();
			}
		}

		public override string Description
		{
			get
			{
				return language.SettingsDestinationFileas;
			}
		}

		public override int Priority
		{
			get
			{
				return 0;
			}
		}

		public override Keys EditorShortcutKeys
		{
			get
			{
				return Keys.Control | Keys.Shift | Keys.S;
			}
		}

		public override Image DisplayIcon
		{
			get
			{
				return GreenshotResources.GetImage("Save.Image");
			}
		}

		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var exportInformation = new ExportInformation
			{
				DestinationDesignation = Designation, DestinationDescription = Description
			};
			string savedTo = await Task.Factory.StartNew(() => ImageOutput.SaveWithDialog(capture, capture.CaptureDetails), token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

			// Bug #2918756 don't overwrite path if SaveWithDialog returns null!
			if (savedTo != null)
			{
				exportInformation.ExportMade = true;
				exportInformation.Filepath = savedTo;
				capture.CaptureDetails.Filename = savedTo;
				conf.OutputFileAsFullpath = savedTo;
			}
			return exportInformation;
		}
	}
}