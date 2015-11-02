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

using System;
using System.Drawing;
using System.Windows.Forms;
using GreenshotPlugin.Core;
using Dapplo.Config.Ini;
using log4net;
using System.Threading.Tasks;
using System.Threading;
using Dapplo.Config.Language;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Interfaces;

namespace Greenshot.Destinations
{
	/// <summary>
	/// Description of ClipboardDestination.
	/// </summary>
	public class ClipboardLegacyDestination : AbstractLegacyDestination
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (ClipboardLegacyDestination));
		private static readonly ICoreConfiguration conf = IniConfig.Current.Get<ICoreConfiguration>();
		private static readonly IGreenshotLanguage language = LanguageLoader.Current.Get<IGreenshotLanguage>();
		public const string DESIGNATION = "Clipboard";

		public override string Designation
		{
			get
			{
				return DESIGNATION;
			}
		}

		public override string Description
		{
			get
			{
				return language.SettingsDestinationClipboard;
			}
		}

		public override int Priority
		{
			get
			{
				return 2;
			}
		}

		public override Keys EditorShortcutKeys
		{
			get
			{
				return Keys.Control | Keys.Shift | Keys.C;
			}
		}

		public override Image DisplayIcon
		{
			get
			{
				return GreenshotResources.GetImage("Clipboard.Image");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="capture"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var exportInformation = new ExportInformation
			{
				DestinationDesignation = Designation, DestinationDescription = Description
			};
			try
			{
				// There is not much that can work async for the Clipboard
				await Task.Factory.StartNew(() =>
				{
					ClipboardHelper.SetClipboardData(capture);
				}, token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
				exportInformation.ExportMade = true;
			}
			catch (Exception)
			{
				exportInformation.ErrorMessage = language.EditorClipboardfailed;
			}

			return exportInformation;
		}
	}
}