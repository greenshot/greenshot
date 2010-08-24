/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Forms;

using Greenshot.Capturing;
using Greenshot.Core;
using Greenshot.Plugin;
using Microsoft.Win32;

namespace GreenshotOCR {
	/// <summary>
	/// OCR Plugin Greenshot
	/// </summary>
	public class OcrPlugin : IGreenshotPlugin {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OcrPlugin));
		private const string MODI_OFFICE11 = @"Software\Microsoft\Office\11.0\MODI";
		private const string MODI_OFFICE12 = @"Software\Microsoft\Office\12.0\MODI";

		private IGreenshotPluginHost host;
		private ICaptureHost captureHost = null;
		private PluginAttribute myAttributes;
		private OCRConfiguration config;

		public OcrPlugin() { }

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		public void Initialize(IGreenshotPluginHost host, ICaptureHost captureHost, PluginAttribute myAttributes) {
			LOG.Debug("Initialize called of " + myAttributes.Name);
			this.host = (IGreenshotPluginHost)host;
			this.captureHost = captureHost;
			this.myAttributes = myAttributes;
			
			// Make sure the MODI-DLLs are found by adding a resolver
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(MyAssemblyResolver);

			if (!HasMODI()) {
				LOG.Warn("No MODI found!");
				return;
			}
			
			// Load configuration
			config = IniConfig.GetIniSection<OCRConfiguration>();
			if (config.IsDirty) {
				IniConfig.Save();
			}
			this.host.RegisterHotKey(3, 0x2C, new HotKeyHandler(MyHotkeyHandler));

			// Here we can hang ourselves to the main context menu!
			ToolStripMenuItem item = new ToolStripMenuItem();
			item.Text = "Region OCR";
			item.ShortcutKeyDisplayString = "Ctrl + Alt + Print";
			item.Click += new System.EventHandler(MainMenuClick);

			ContextMenuStrip contextMenu = host.MainMenu;
			bool addedItem = false;

			for(int i=0; i < contextMenu.Items.Count; i++) {
				if (contextMenu.Items[i].GetType() == typeof(ToolStripSeparator)) {
					contextMenu.Items.Insert(i, item);
					addedItem = true;
					break;
				}
			}
			if (!addedItem) {
				contextMenu.Items.Add(item);
			}
		}
		
		/// <summary>
		/// Implementation of the IGreenshotPlugin.Shutdown
		/// </summary>
		public void Shutdown() {
			LOG.Debug("Shutdown of " + myAttributes.Name);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			if (!HasMODI()) {
				MessageBox.Show("Sorry, is seems that Microsoft Office Document Imaging (MODI) is not installed, therefor the OCR Plugin cannot work.");
				return;
			}
			SettingsForm settingsForm = new SettingsForm(GetLanguages(), config);
			DialogResult result = settingsForm.ShowDialog();
			if (result == DialogResult.OK) {
				IniConfig.Save();
			}
		}

		/// <summary>
		/// This method helps to resolve the MODI DLL files
		/// </summary>
		/// <param name="sender">object which is starting the resolve</param>
		/// <param name="args">ResolveEventArgs describing the Assembly that needs to be found</param>
		/// <returns></returns>
		private Assembly MyAssemblyResolver(object sender, ResolveEventArgs args) {
			string dllPath = Path.GetDirectoryName(myAttributes.DllFile);
			string dllFilename = args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
			LOG.Debug("Resolving: " + dllFilename);
			if (dllFilename.StartsWith("MODI")) {
				return Assembly.LoadFile(Path.Combine(dllPath, dllFilename));
			}
			return null;
		}

		private void StartOCRRegion() {
			LOG.Debug("Starting OCR!");
			captureHost.MakeCapture(CaptureMode.Region, false, new CaptureHandler(DoOCR));
		}
		
		private void MyHotkeyHandler() {
			StartOCRRegion();
		}
		/// <summary>
		/// Is called when the OCR menu is selected
		/// </summary>
		/// <param name="sender">ContextMenu</param>
		/// <param name="e">EventArgs from ContextMenu</param>
		private void MainMenuClick(object sender, EventArgs e) {
			StartOCRRegion();
		}

		/// <summary>
		/// Handling of the CaptureTaken "event" from the ICaptureHost
		/// We do the OCR here!
		/// </summary>
		/// <param name="ImageOutputEventArgs">Has the Image and the capture details</param>
		private const int MIN_WIDTH = 130;
		private const int MIN_HEIGHT = 130;
		private void DoOCR(object sender, CaptureTakenEventArgs eventArgs) {
			if (eventArgs.Capture.Image == null) {
				return;
			}
			string file = host.GetFilename(OutputFormat.Bmp, eventArgs.Capture.CaptureDetails);
			string filePath = Path.Combine(Path.GetTempPath(),file);
			
			using (FileStream stream = File.Create(filePath)) {
				Image capturedImage = eventArgs.Capture.Image;
				if (capturedImage.Width < MIN_WIDTH || capturedImage.Height < MIN_HEIGHT) {
					LOG.Debug("Image not big enough, copying to bigger image");
					int newWidth = Math.Max(capturedImage.Width, MIN_WIDTH);
					int newHeight = Math.Max(capturedImage.Height, MIN_HEIGHT);
					using (Image tmpImage = new Bitmap(newWidth, newHeight, capturedImage.PixelFormat)) {
						using (Graphics graphics = Graphics.FromImage(tmpImage)) {
							graphics.Clear(Color.White);
							graphics.DrawImage(capturedImage, Point.Empty);
						}
						host.SaveToStream(tmpImage, stream, OutputFormat.Bmp, 100);
					}
				} else {
					host.SaveToStream(capturedImage, stream, OutputFormat.Bmp, 100);
				}
			}
		
			LOG.Debug("Saved tmp file to: " + filePath);

			string text = "";

			try {

				switch (CheckModiVersion()) {
					case ModiVersion.MODI11:
						// Instantiate the MODI.Document object
						MODI11.Document modi11Document = new MODI11.Document();
						// The Create method grabs the picture from disk snd prepares for OCR.
						modi11Document.Create(filePath);
						
						// Add progress bar here:
						//md.OnOCRProgress += ;
						
						// Do the OCR.
						modi11Document.OCR((MODI11.MiLANGUAGES)Enum.Parse(typeof(MODI11.MiLANGUAGES), config.Language), config.Orientimage, config.StraightenImage);
						// Get the first (and only image)
						MODI11.Image modi11Image = (MODI11.Image)modi11Document.Images[0];
						// Get the layout.
						MODI11.Layout modi11layout = modi11Image.Layout;
						text = modi11layout.Text;
						// Close the MODI.Document object.
						modi11Document.Close(false);
						break;
					case ModiVersion.MODI12:
						// Instantiate the MODI.Document object
						MODI12.Document modi12Document = new MODI12.Document();
						// The Create method grabs the picture from disk snd prepares for OCR.
						modi12Document.Create(filePath);
						
						// Add progress bar here:
						//md.OnOCRProgress += ;
						
						// Do the OCR.
						modi12Document.OCR((MODI12.MiLANGUAGES)Enum.Parse(typeof(MODI12.MiLANGUAGES), config.Language), config.Orientimage, config.StraightenImage);
						// Get the first (and only image)
						MODI12.Image modi12Image = (MODI12.Image)modi12Document.Images[0];
						// Get the layout.
						MODI12.Layout modi12layout = modi12Image.Layout;
						text = modi12layout.Text;
						// Close the MODI.Document object.
						modi12Document.Close(false);
						break;
					default:
						LOG.Error("Unknown MODI version!");
						break;
				}

				// Paste to Clipboard (the Plugin currently doesn't have access to the ClipboardHelper from Greenshot
				IDataObject ido = new DataObject();
				ido.SetData(DataFormats.Text, true, text);
				Clipboard.SetDataObject(ido, true);
			} catch (Exception e) {
				string message = "A problem occured while trying to OCR the region, this plugin is still in a experimental stage!!";
				LOG.Error(message, e);
				MessageBox.Show(message);
			} finally {
				if (File.Exists(filePath)) {
					LOG.Debug("Cleaning up tmp file: " + filePath);
					File.Delete(filePath);
				}
			}
		}
		
		private string [] GetLanguages() {
			string [] languages = null;
			switch (CheckModiVersion()) {
				case ModiVersion.MODI11:
					languages = Enum.GetNames(typeof(MODI11.MiLANGUAGES));
					break;
				case ModiVersion.MODI12:
					languages =Enum.GetNames(typeof(MODI12.MiLANGUAGES));
					break;
			}
			return languages;
		}

		private enum ModiVersion { None, MODI11, MODI12 };

		private ModiVersion CheckModiVersion() {
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(MODI_OFFICE11, false)) {
				if (key != null) {
					LOG.Debug("Found Modi V11 in registry: " + key.Name);
					return ModiVersion.MODI11;
				}
			}
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(MODI_OFFICE12, false)) {
				if (key != null) {
					LOG.Debug("Found Modi V12 in registry: " + key.Name);
					return ModiVersion.MODI12;
				}
			}
			return ModiVersion.None;
		}
		
		private bool HasMODI() {
			return CheckModiVersion() != ModiVersion.None;
		}
	}
}