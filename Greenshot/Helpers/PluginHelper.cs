#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Dapplo.Windows.Dpi;
using Greenshot.Forms;
using GreenshotPlugin.Core;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using Dapplo.Log;

#endregion

namespace Greenshot.Helpers
{
	/// <summary>
	///     The PluginHelper takes care of all plugin related functionality
	/// </summary>
	[Serializable]
	public class PluginHelper : IGreenshotHost
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();

		private static readonly string PluginPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName);
		private static readonly string ApplicationPath = Path.GetDirectoryName(Application.ExecutablePath);
		private static readonly string PafPath = Path.Combine(Application.StartupPath, @"App\Greenshot");
		private static readonly IDictionary<PluginAttribute, IGreenshotPlugin> plugins = new SortedDictionary<PluginAttribute, IGreenshotPlugin>();

		private PluginHelper()
		{
			PluginUtils.Host = this;
		}

		public static PluginHelper Instance { get; } = new PluginHelper();

		public Form GreenshotForm => MainForm.Instance;

		public NotifyIcon NotifyIcon => MainForm.Instance.NotifyIcon;

		public bool HasPlugins()
		{
			return plugins != null && plugins.Count > 0;
		}

		public void Shutdown()
		{
			foreach (var plugin in plugins.Values)
			{
				plugin.Shutdown();
				plugin.Dispose();
			}
			plugins.Clear();
		}

		// Add plugins to the Listview
		public void FillListview(ListView listview)
		{
			foreach (var pluginAttribute in plugins.Keys)
			{
				var item = new ListViewItem(pluginAttribute.Name)
				{
					Tag = pluginAttribute
				};
				item.SubItems.Add(pluginAttribute.Version);
				item.SubItems.Add(pluginAttribute.CreatedBy);
				item.SubItems.Add(pluginAttribute.DllFile);
				listview.Items.Add(item);
			}
		}

		public bool IsSelectedItemConfigurable(ListView listview)
		{
			if (listview.SelectedItems.Count <= 0)
			{
				return false;
			}
			var pluginAttribute = (PluginAttribute) listview.SelectedItems[0].Tag;
			return pluginAttribute != null && pluginAttribute.Configurable;
		}

		public void ConfigureSelectedItem(ListView listview)
		{
			if (listview.SelectedItems.Count <= 0)
			{
				return;
			}
			var pluginAttribute = (PluginAttribute) listview.SelectedItems[0].Tag;
			if (pluginAttribute == null)
			{
				return;
			}
			var plugin = plugins[pluginAttribute];
			plugin.Configure();
		}

		#region Implementation of IGreenshotPluginHost

		/// <summary>
		///     Create a Thumbnail
		/// </summary>
		/// <param name="image">Image of which we need a Thumbnail</param>
		/// <param name="width">Thumbnail width</param>
		/// <param name="height">Thumbnail height</param>
		/// <returns>Image with Thumbnail</returns>
		public Image GetThumbnail(Image image, int width, int height)
		{
			return image.GetThumbnailImage(width, height, ThumbnailCallback, IntPtr.Zero);
		}

		/// <summary>
		///     Required for GetThumbnail, but not used
		/// </summary>
		/// <returns>true</returns>
		private bool ThumbnailCallback()
		{
			return true;
		}

		public ContextMenuStrip MainMenu => MainForm.Instance.MainMenu;

		/// <summary>
		/// Make the DpiHandler available for plugins
		/// </summary>
		public DpiHandler ContextMenuDpiHandler => MainForm.Instance.ContextMenuDpiHandler;

		public IDictionary<PluginAttribute, IGreenshotPlugin> Plugins => plugins;

		public IDestination GetDestination(string designation)
		{
			return DestinationHelper.GetDestination(designation);
		}

		public List<IDestination> GetAllDestinations()
		{
			return DestinationHelper.GetAllDestinations();
		}

		public ExportInformation ExportCapture(bool manuallyInitiated, string designation, ISurface surface, ICaptureDetails captureDetails)
		{
			return DestinationHelper.ExportCapture(manuallyInitiated, designation, surface, captureDetails);
		}

		/// <summary>
		///     Make Capture with specified Handler
		/// </summary>
		/// <param name="captureMouseCursor">
		///     bool false if the mouse should not be captured, true if the configuration should be
		///     checked
		/// </param>
		/// <param name="destination">IDestination</param>
		public void CaptureRegion(bool captureMouseCursor, IDestination destination)
		{
			CaptureHelper.CaptureRegion(captureMouseCursor, destination);
		}

		/// <summary>
		///     Use the supplied image, and handle it as if it's captured.
		/// </summary>
		/// <param name="captureToImport">Image to handle</param>
		public void ImportCapture(ICapture captureToImport)
		{
			MainForm.Instance.BeginInvoke((MethodInvoker) delegate { CaptureHelper.ImportCapture(captureToImport); });
		}

		/// <summary>
		///     Get an ICapture object, so the plugin can modify this
		/// </summary>
		/// <returns></returns>
		public ICapture GetCapture(Image imageToCapture)
		{
			var capture = new Capture(imageToCapture)
			{
				CaptureDetails = new CaptureDetails
				{
					CaptureMode = CaptureMode.Import,
					Title = "Imported"
				}
			};
			return capture;
		}

		#endregion

		#region Plugin loading

		public PluginAttribute FindPlugin(string name)
		{
			foreach (var pluginAttribute in plugins.Keys)
			{
				if (name.Equals(pluginAttribute.Name))
				{
					return pluginAttribute;
				}
			}
			return null;
		}

		private bool IsNewer(string version1, string version2)
		{
			var version1Parts = version1.Split('.');
			var version2Parts = version2.Split('.');
			var parts = Math.Min(version1Parts.Length, version2Parts.Length);
			for (var i = 0; i < parts; i++)
			{
				var v1 = Convert.ToInt32(version1Parts[i]);
				var v2 = Convert.ToInt32(version2Parts[i]);
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
		///     Private helper to find the plugins in the path
		/// </summary>
		/// <param name="pluginFiles"></param>
		/// <param name="path"></param>
		private void FindPluginsOnPath(List<string> pluginFiles, string path)
		{
			if (Directory.Exists(path))
			{
				try
				{
					foreach (var pluginFile in Directory.GetFiles(path, "*.gsp", SearchOption.AllDirectories))
					{
						pluginFiles.Add(pluginFile);
					}
				}
				catch (UnauthorizedAccessException)
				{
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex, "Error loading plugin: ");
				}
			}
		}

		/// <summary>
		///     Load the plugins
		/// </summary>
		public void LoadPlugins()
		{
			var pluginFiles = new List<string>();

			if (IniConfig.IsPortable)
			{
				FindPluginsOnPath(pluginFiles, PafPath);
			}
			else
			{
				FindPluginsOnPath(pluginFiles, PluginPath);
				FindPluginsOnPath(pluginFiles, ApplicationPath);
			}

			var tmpAttributes = new Dictionary<string, PluginAttribute>();
			var tmpAssemblies = new Dictionary<string, Assembly>();
			// Loop over the list of available files and get the Plugin Attributes
			foreach (var pluginFile in pluginFiles)
			{
				//Log.Debug().WriteLine("Checking the following file for plugins: {0}", pluginFile);
				try
				{
					var assembly = Assembly.LoadFrom(pluginFile);
					var pluginAttributes = assembly.GetCustomAttributes(typeof(PluginAttribute), false) as PluginAttribute[];
					if (pluginAttributes.Length > 0)
					{
						var pluginAttribute = pluginAttributes[0];

						if (string.IsNullOrEmpty(pluginAttribute.Name))
						{
							var assemblyProductAttributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false) as AssemblyProductAttribute[];
							if (assemblyProductAttributes.Length > 0)
							{
								pluginAttribute.Name = assemblyProductAttributes[0].Product;
							}
							else
							{
								continue;
							}
						}
						if (string.IsNullOrEmpty(pluginAttribute.CreatedBy))
						{
							var assemblyCompanyAttributes = assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false) as AssemblyCompanyAttribute[];
							if (assemblyCompanyAttributes.Length > 0)
							{
								pluginAttribute.CreatedBy = assemblyCompanyAttributes[0].Company;
							}
							else
							{
								continue;
							}
						}
						pluginAttribute.Version = assembly.GetName().Version.ToString();
						pluginAttribute.DllFile = pluginFile;

						// check if this plugin is already available
						PluginAttribute checkPluginAttribute = null;
						if (tmpAttributes.ContainsKey(pluginAttribute.Name))
						{
							checkPluginAttribute = tmpAttributes[pluginAttribute.Name];
						}

						if (checkPluginAttribute != null)
						{
							Log.Warn().WriteLine("Duplicate plugin {0} found", pluginAttribute.Name);
							if (IsNewer(pluginAttribute.Version, checkPluginAttribute.Version))
							{
								// Found is newer
								tmpAttributes[pluginAttribute.Name] = pluginAttribute;
								tmpAssemblies[pluginAttribute.Name] = assembly;
								Log.Info().WriteLine("Loading the newer plugin {0} with version {1} from {2}", pluginAttribute.Name, pluginAttribute.Version, pluginAttribute.DllFile);
							}
							else
							{
								Log.Info().WriteLine("Skipping (as the duplicate is newer or same version) the plugin {0} with version {1} from {2}", pluginAttribute.Name, pluginAttribute.Version,
									pluginAttribute.DllFile);
							}
							continue;
						}
						if (CoreConfig.ExcludePlugins != null && CoreConfig.ExcludePlugins.Contains(pluginAttribute.Name))
						{
							Log.Warn().WriteLine("Exclude list: {0}", CoreConfig.ExcludePlugins.ToArray());
							Log.Warn().WriteLine("Skipping the excluded plugin {0} with version {1} from {2}", pluginAttribute.Name, pluginAttribute.Version, pluginAttribute.DllFile);
							continue;
						}
						if (CoreConfig.IncludePlugins != null && CoreConfig.IncludePlugins.Count > 0 && !CoreConfig.IncludePlugins.Contains(pluginAttribute.Name))
						{
							// Whitelist is set
							Log.Warn().WriteLine("Include list: {0}", CoreConfig.IncludePlugins.ToArray());
							Log.Warn().WriteLine("Skipping the not included plugin {0} with version {1} from {2}", pluginAttribute.Name, pluginAttribute.Version, pluginAttribute.DllFile);
							continue;
						}
						Log.Info().WriteLine("Loading the plugin {0} with version {1} from {2}", pluginAttribute.Name, pluginAttribute.Version, pluginAttribute.DllFile);
						tmpAttributes[pluginAttribute.Name] = pluginAttribute;
						tmpAssemblies[pluginAttribute.Name] = assembly;
					}
					else
					{
						Log.Error().WriteLine("Can't find the needed Plugin Attribute ({0}) in the assembly of the file \"{1}\", skipping this file.", typeof(PluginAttribute), pluginFile);
					}
				}
				catch (Exception e)
				{
					Log.Warn().WriteLine(e, "Can't load file: " + pluginFile);
				}
			}
			foreach (var pluginName in tmpAttributes.Keys)
			{
				try
				{
					var pluginAttribute = tmpAttributes[pluginName];
					var assembly = tmpAssemblies[pluginName];
					var entryType = assembly.GetType(pluginAttribute.EntryType);
					if (entryType == null)
					{
						Log.Error().WriteLine("Can't find the in the PluginAttribute referenced type {0} in \"{1}\"", pluginAttribute.EntryType, pluginAttribute.DllFile);
						continue;
					}
					try
					{
						var plugin = (IGreenshotPlugin) Activator.CreateInstance(entryType);
						if (plugin != null)
						{
							if (plugin.Initialize(this, pluginAttribute))
							{
								plugins.Add(pluginAttribute, plugin);
							}
							else
							{
								Log.Info().WriteLine("Plugin {0} not initialized!", pluginAttribute.Name);
							}
						}
						else
						{
							Log.Error().WriteLine("Can't create an instance of the in the PluginAttribute referenced type {0} from \"{1}\"", pluginAttribute.EntryType, pluginAttribute.DllFile);
						}
					}
					catch (Exception e)
					{
						Log.Error().WriteLine(e, "Can't load Plugin: " + pluginAttribute.Name);
					}
				}
				catch (Exception e)
				{
					Log.Error().WriteLine(e, "Can't load Plugin: " + pluginName);
				}
			}
		}

		#endregion
	}
}