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
using Dapplo.Config.Ini;
using Greenshot.Forms;
using Greenshot.Plugin;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Core;
using GreenshotPlugin.Extensions;
using log4net;
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
		private static readonly ILog LOG = LogManager.GetLogger(typeof (PluginHelper));
		private static readonly ICoreConfiguration conf = IniConfig.Current.Get<ICoreConfiguration>();

		private static readonly string pluginPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName);
		private static readonly string applicationPath = Path.GetDirectoryName(Application.ExecutablePath);
		private static string pafPath = Path.Combine(Application.StartupPath, @"App\Greenshot");
		private static IDictionary<PluginAttribute, IGreenshotPlugin> plugins = new ConcurrentDictionary<PluginAttribute, IGreenshotPlugin>();
		private static readonly PluginHelper instance = new PluginHelper();
		private readonly StartupShutdownBootstrapper _startupShutdownBootstrapper = new StartupShutdownBootstrapper();

		public static PluginHelper Instance
		{
			get
			{
				return instance;
			}
		}

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

		public void Shutdown()
		{
			foreach (IGreenshotPlugin plugin in plugins.Values)
			{
				plugin.Shutdown();
				plugin.Dispose();
			}
			plugins.Clear();
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

		public bool isSelectedItemConfigurable(ListView listview)
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

		/// <summary>
		/// Create a Thumbnail
		/// </summary>
		/// <param name="image">Image of which we need a Thumbnail</param>
		/// <returns>Image with Thumbnail</returns>
		public Image GetThumbnail(Image image, int width, int height)
		{
			return image.GetThumbnailImage(width, height, ThumbnailCallback, IntPtr.Zero);
		}

		///  <summary>
		/// Required for GetThumbnail, but not used
		/// </summary>
		/// <returns>true</returns>
		private bool ThumbnailCallback()
		{
			return true;
		}

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
		/// <param name="imageToImport">Image to handle</param>
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
			Capture capture = new Capture(imageToCapture);
			capture.CaptureDetails = new CaptureDetails();
			capture.CaptureDetails.CaptureMode = CaptureMode.Import;
			capture.CaptureDetails.Title = "Imported";
			return capture;
		}

		#endregion

		#region Plugin loading

		public PluginAttribute FindPlugin(string name)
		{
			foreach (PluginAttribute pluginAttribute in plugins.Keys)
			{
				if (name.Equals(pluginAttribute.Name))
				{
					return pluginAttribute;
				}
			}
			return null;
		}

		private bool isNewer(string version1, string version2)
		{
			string[] version1Parts = version1.Split('.');
			string[] version2Parts = version2.Split('.');
			int parts = Math.Min(version1Parts.Length, version2Parts.Length);
			for (int i = 0; i < parts; i++)
			{
				int v1 = Convert.ToInt32(version1Parts[i]);
				int v2 = Convert.ToInt32(version2Parts[i]);
				if (v1 > v2)
				{
					return true;
				}
				if (v1 < v2)
				{
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// Private helper to find the plugins in the path
		/// </summary>
		/// <param name="pluginFiles"></param>
		/// <param name="path"></param>
		private void FindPluginsOnPath(IList<string> pluginFiles, String path)
		{
			if (Directory.Exists(path))
			{
				try
				{
					foreach (string pluginFile in Directory.EnumerateFiles(path, "*.gsp", SearchOption.AllDirectories))
					{
						pluginFiles.Add(pluginFile);
					}
				}
				catch (UnauthorizedAccessException)
				{
					return;
				}
				catch (Exception ex)
				{
					LOG.Error("Error loading plugin: ", ex);
				}
			}
		}

		/// <summary>
		/// Load the plugins
		/// </summary>
		public async Task LoadPluginsAsync(CancellationToken token = default(CancellationToken))
		{
			
			if (PortableHelper.IsPortable)
			{
				_startupShutdownBootstrapper.Add(pafPath, "*.gsp");
			}
			else
			{
				_startupShutdownBootstrapper.Add(pluginPath, "*.gsp");
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