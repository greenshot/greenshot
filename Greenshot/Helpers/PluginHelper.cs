/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using log4net;

namespace Greenshot.Helpers {
	/// <summary>
	/// The PluginHelper takes care of all plugin related functionality
	/// </summary>
	[Serializable]
	public class PluginHelper : IGreenshotHost {
		private static readonly ILog Log = LogManager.GetLogger(typeof(PluginHelper));
		private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();

		private static readonly string PluginPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),Application.ProductName);
		private static readonly string ApplicationPath = Path.GetDirectoryName(Application.ExecutablePath);
		private static readonly string PafPath =  Path.Combine(Application.StartupPath, @"App\Greenshot");

        public static PluginHelper Instance { get; } = new PluginHelper();

        public void Shutdown() {
            foreach(var plugin in SimpleServiceProvider.Current.GetAllInstances<IGreenshotPlugin>()) {
				plugin.Shutdown();
				plugin.Dispose();
			}
		}

		/// <summary>
		/// Add plugins to the ListView
		/// </summary>
		/// <param name="listView"></param>
		public void FillListView(ListView listView) {
            foreach (var plugin in SimpleServiceProvider.Current.GetAllInstances<IGreenshotPlugin>())
            {
                var pluginAttribute = plugin.GetType().GetCustomAttribute<PluginAttribute>();
				var item = new ListViewItem(pluginAttribute.Name)
				{
					Tag = pluginAttribute
				};
                var assembly = plugin.GetType().Assembly;

                var company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
				item.SubItems.Add(assembly.GetName().Version.ToString());
				item.SubItems.Add(company.Company);
				item.SubItems.Add(assembly.Location);
				listView.Items.Add(item);
			}
		}
		
		public bool IsSelectedItemConfigurable(ListView listView) {
			if (listView.SelectedItems.Count <= 0)
			{
				return false;
			}
			var pluginAttribute = (PluginAttribute)listView.SelectedItems[0].Tag;
			return pluginAttribute != null && pluginAttribute.Configurable;
		}
		
		public void ConfigureSelectedItem(ListView listView) {
			if (listView.SelectedItems.Count <= 0)
			{
				return;
			}
			var pluginAttribute = (PluginAttribute)listView.SelectedItems[0].Tag;
			if (pluginAttribute == null)
			{
				return;
			}

            var plugin = SimpleServiceProvider.Current.GetAllInstances<IGreenshotPlugin>().FirstOrDefault(p =>
                p.GetType().GetCustomAttribute<PluginAttribute>().Name == pluginAttribute.Name);
			plugin?.Configure();
		}

        /// <summary>
		/// Create a Thumbnail
		/// </summary>
		/// <param name="image">Image of which we need a Thumbnail</param>
		/// <param name="width">Thumbnail width</param>
		/// <param name="height">Thumbnail height</param>
		/// <returns>Image with Thumbnail</returns>
		public Image GetThumbnail(Image image, int width, int height) {
			return image.GetThumbnailImage(width, height,  ThumbnailCallback, IntPtr.Zero);
		}

		///  <summary>
		/// Required for GetThumbnail, but not used
		/// </summary>
		/// <returns>true</returns>
		private bool ThumbnailCallback() {
			return true;
		}

		public ExportInformation ExportCapture(bool manuallyInitiated, string designation, ISurface surface, ICaptureDetails captureDetails) {
			return DestinationHelper.ExportCapture(manuallyInitiated, designation, surface, captureDetails);
		}

		/// <summary>
		/// Make Capture with specified Handler
		/// </summary>
		/// <param name="captureMouseCursor">bool false if the mouse should not be captured, true if the configuration should be checked</param>
		/// <param name="destination">IDestination</param>
		public void CaptureRegion(bool captureMouseCursor, IDestination destination) {
			CaptureHelper.CaptureRegion(captureMouseCursor, destination);
		}

		/// <summary>
		/// Use the supplied image, and handle it as if it's captured.
		/// </summary>
		/// <param name="captureToImport">Image to handle</param>
		public void ImportCapture(ICapture captureToImport)
        {
            var mainForm = SimpleServiceProvider.Current.GetInstance<Form>();
            mainForm.BeginInvoke((MethodInvoker)delegate {
				CaptureHelper.ImportCapture(captureToImport);
			});
		}
		
		/// <summary>
		/// Get an ICapture object, so the plugin can modify this
		/// </summary>
		/// <returns></returns>
		public ICapture GetCapture(Image imageToCapture) {
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


        /// <summary>
		/// Private helper to find the plugins in the path
		/// </summary>
		/// <param name="path">string</param>
		/// <returns>IEnumerable with plugin files</returns>
		private IEnumerable<string> FindPluginsOnPath(string path)
        {
            var pluginFiles = Enumerable.Empty<string>();
            if (!Directory.Exists(path)) return pluginFiles;
            try
			{
                pluginFiles = Directory.GetFiles(path, "*Plugin.dll", SearchOption.AllDirectories)
					// Skip the GreenshotPlugin.dll itself
                    .Where(p => CultureInfo.CurrentCulture.CompareInfo.IndexOf(p, "GreenshotPlugin.dll", CompareOptions.IgnoreCase) < 0);
            } catch (Exception ex) {
                Log.Error("Error loading plugin: ", ex);
            }

            return pluginFiles;

        }

		/// <summary>
		/// Load the plugins
		/// </summary>
		public void LoadPlugins() {
			List<string> pluginFiles = new List<string>();

			if (IniConfig.IsPortable) {
                pluginFiles.AddRange(FindPluginsOnPath(PafPath));
			} else {
                pluginFiles.AddRange(FindPluginsOnPath(PluginPath));
                pluginFiles.AddRange(FindPluginsOnPath(ApplicationPath));
			}

			// Loop over the list of available files and get the Plugin Attributes
			foreach (string pluginFile in pluginFiles) {

				try {
					Assembly assembly = Assembly.LoadFrom(pluginFile);

                    var assemblyName = assembly.GetName().Name;

					var pluginEntryName = $"{assemblyName}.{assemblyName.Replace("Greenshot", string.Empty)}";
					var pluginEntryType = assembly.GetType(pluginEntryName, false, true);
                    var pluginAttribute = pluginEntryType.GetCustomAttribute<PluginAttribute>();

                    if (CoreConfig.ExcludePlugins != null && CoreConfig.ExcludePlugins.Contains(pluginAttribute.Name))
                    {
                        Log.WarnFormat("Exclude list: {0}", string.Join(",", CoreConfig.ExcludePlugins));
                        Log.WarnFormat("Skipping the excluded plugin {0} with version {1} from {2}", pluginAttribute.Name, assembly.GetName().Version, pluginFile);
                        continue;
                    }

                    IGreenshotPlugin plugin = (IGreenshotPlugin)Activator.CreateInstance(pluginEntryType);
                    if (plugin != null)
                    {
                        if (plugin.Initialize())
                        {
							SimpleServiceProvider.Current.AddService(plugin);
                        }
                        else
                        {

                            Log.InfoFormat("Plugin {0} not initialized!", pluginAttribute.Name);
                        }
                    }
                    else
                    {
                        Log.ErrorFormat("Can't create an instance of {0} from \"{1}\"", assembly.GetName().Name + ".GreenshotPlugin", pluginFile);
                    }
                }
                catch (Exception e)
                {
                    Log.ErrorFormat("Can't load Plugin: {0}", pluginFile);
					Log.Error(e);
                }
			}
		}
    }
}
