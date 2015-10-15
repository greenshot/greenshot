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

using Dapplo.Addons.Implementation;
using Greenshot.Forms;
using Greenshot.Plugin;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Core;
using GreenshotPlugin.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Greenshot.Helpers
{
	/// <summary>
	/// The PluginHelper takes care of all plugin related functionality
	/// </summary>
	[Serializable]
	public class PluginHelper : IGreenshotHost
	{
		private readonly StartupShutdownBootstrapper _startupShutdownBootstrapper = new StartupShutdownBootstrapper();

		public static PluginHelper Instance
		{
			get;
		} = new PluginHelper();

		private PluginHelper()
		{
			PluginUtils.Host = this;
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
			return (plugins != null && plugins.Count > 0);
		}

		public async Task ShutdownAsync(CancellationToken token = default(CancellationToken))
		{
			await _startupShutdownBootstrapper.ShutdownAsync(token);
		}

		// Add plugins to the Listview
		public void FillListview(ListView listview)
		{
			foreach (PluginAttribute pluginAttribute in plugins.Keys.OrderBy(x => x.Name))
			{
				ListViewItem item = new ListViewItem(pluginAttribute.Name);
				item.SubItems.Add(pluginAttribute.Version);
				item.SubItems.Add(pluginAttribute.CreatedBy);
				item.SubItems.Add(pluginAttribute.DllFile);
				item.Tag = pluginAttribute;
				listview.Items.Add(item);
			}
		}

		public bool IsSelectedItemConfigurable(ListView listview)
		{
			if (listview.SelectedItems.Count > 0)
			{
				PluginAttribute pluginAttribute = (PluginAttribute) listview.SelectedItems[0].Tag;
				if (pluginAttribute != null)
				{
					return pluginAttribute.Configurable;
				}
			}
			return false;
		}

		public void ConfigureSelectedItem(ListView listview)
		{
			if (listview.SelectedItems.Count > 0)
			{
				PluginAttribute pluginAttribute = (PluginAttribute) listview.SelectedItems[0].Tag;
				if (pluginAttribute != null)
				{
					IGreenshotPlugin plugin = plugins[pluginAttribute];
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

		public IDictionary<PluginAttribute, IGreenshotPlugin> Plugins
		{
			get
			{
				return plugins;
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

		#region Plugin loading

		public PluginAttribute FindPlugin(string name)
		{
			return plugins.Keys.FirstOrDefault(pluginAttribute => name.Equals(pluginAttribute.Name));
		}

		/// <summary>
		/// Load the plugins
		/// </summary>
		public async Task LoadPluginsAsync(CancellationToken token = default(CancellationToken))
		{
			
			if (PortableHelper.IsPortable)
			{
				var pafPath = Path.Combine(Application.StartupPath, @"App\Greenshot");
				_startupShutdownBootstrapper.Add(pafPath, "*.gsp");
			}
			else
			{
				var pluginPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName);
                _startupShutdownBootstrapper.Add(pluginPath, "*.gsp");

				var applicationPath  = Path.GetDirectoryName(Application.ExecutablePath);
				_startupShutdownBootstrapper.Add(applicationPath, "*.gsp");
			}
			// Make the IGreenshotHost available for the plugins
			_startupShutdownBootstrapper.Export<IGreenshotHost>(this);
			_startupShutdownBootstrapper.Run();

			await _startupShutdownBootstrapper.StartupAsync(token);
		}

		#endregion
	}
}