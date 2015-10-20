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

using Greenshot.Forms;
using Greenshot.Plugin;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Core;
using GreenshotPlugin.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Greenshot.Helpers
{
	/// <summary>
	/// The PluginHelper takes care of all plugin related functionality
	/// </summary>
	[Export(typeof(IGreenshotHost))]
	public class PluginHelper : IGreenshotHost
	{
		public static PluginHelper Instance
		{
			set;
			get;
		}

		public PluginHelper()
		{
			PluginUtils.Host = this;
			Instance = this;
		}

		[ImportMany]
		public IEnumerable<Lazy<IGreenshotPlugin, IGreenshotPluginMetadata>> Plugins
		{
			get;
			set;
		} 

		public Form GreenshotForm
		{
			get
			{
				return MainForm.Instance;
			}
		}

		public NotifyIcon NotifyIcon
		{
			get
			{
				return MainForm.Instance.NotifyIcon;
			}
		}

		public void ShowAbout()
		{
			MainForm.Instance.ShowAbout();
		}

		public void ShowSettings()
		{
			MainForm.Instance.ShowSetting();
		}

		public bool HasPlugins()
		{
			var plugins = Plugins;

			return (plugins != null && plugins.Any());
		}

		// Add plugins to the Listview
		public void FillListview(ListView listview)
		{
			foreach (var plugin in Plugins.OrderBy(x => x.Metadata.Name))
			{
				var assembly = plugin.Value.GetType().Assembly;
				var version = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
				var item = new ListViewItem
				{
					Text = plugin.Metadata.Name,
					Tag = plugin.Value
				};
				item.SubItems.Add(version);
				item.SubItems.Add(plugin.Metadata.CreatedBy);
				item.SubItems.Add(assembly.Location);
				listview.Items.Add(item);
			}
		}

		public bool IsSelectedItemConfigurable(ListView listview)
		{
			if (listview.SelectedItems.Count > 0)
			{
				IConfigurablePlugin plugin = listview.SelectedItems[0].Tag as IConfigurablePlugin;
				return plugin != null;
			}
			return false;
		}

		public void ConfigureSelectedItem(ListView listview)
		{
			if (listview.SelectedItems.Count > 0)
			{
				var plugin = listview.SelectedItems[0].Tag as IConfigurablePlugin;
				if (plugin != null)
				{
					plugin.Configure();
				}
			}
		}

		#region Implementation of IGreenshotPluginHost

		public ContextMenuStrip MainMenu
		{
			get
			{
				return MainForm.Instance.MainMenu;
			}
		}

		public IDestination GetDestination(string designation)
		{
			return DestinationHelper.GetDestination(designation);
		}

		public List<IDestination> GetAllDestinations()
		{
			return DestinationHelper.GetAllDestinations();
		}

		/// <summary>
		/// Use the supplied image, and handle it as if it's captured.
		/// </summary>
		/// <param name="captureToImport">Capture to handle</param>
		public void ImportCapture(ICapture captureToImport)
		{
			MainForm.Instance.AsyncInvoke(async () =>
			{
				await CaptureHelper.ImportCaptureAsync(captureToImport).ConfigureAwait(false);
			});
		}

		/// <summary>
		/// Get an ICapture object, so the plugin can modify this
		/// </summary>
		/// <returns></returns>
		public ICapture GetCapture(Image imageToCapture)
		{
			var capture = new Capture(imageToCapture)
			{
				CaptureDetails = new CaptureDetails
				{
					CaptureMode = CaptureMode.Import, Title = "Imported"
				}
			};
			return capture;
		}

		#endregion
	}
}