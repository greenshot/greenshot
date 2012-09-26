/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Destinations;
using Greenshot.Drawing;
using Greenshot.Drawing.Fields;
using Greenshot.Drawing.Fields.Binding;
using Greenshot.Forms;
using Greenshot.Help;
using Greenshot.Helpers;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using System.Threading;
using System.Drawing.Imaging;
using Greenshot.Plugin.Drawing;

namespace Greenshot {
	/// <summary>
	/// Description of ImageEditorForm.
	/// </summary>
	public partial class ImageEditorForm : BaseForm, IImageEditor {
		
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImageEditorForm));
		private static EditorConfiguration editorConfiguration = IniConfig.GetIniSection<EditorConfiguration>();
		private static CoreConfiguration coreConf = IniConfig.GetIniSection<CoreConfiguration>();
		private static List<string> ignoreDestinations = new List<string>() {PickerDestination.DESIGNATION, EditorDestination.DESIGNATION};
		private static List<IImageEditor> editorList = new List<IImageEditor>();

		private Surface surface;
		private GreenshotPlugin.Controls.GreenshotToolStripButton[] toolbarButtons;
		
		private static string[] SUPPORTED_CLIPBOARD_FORMATS = {typeof(string).FullName, "Text", "DeviceIndependentBitmap", "Bitmap", typeof(DrawableContainerList).FullName};

		private bool originalBoldCheckState = false;
		private bool originalItalicCheckState = false;
		
		// whether part of the editor controls are disabled depending on selected item(s)
		private bool controlsDisabledDueToConfirmable = false;

		/// <summary>
		/// An Implementation for the IImageEditor, this way Plugins have access to the HWND handles wich can be used with Win32 API calls.
		/// </summary>
		public IWin32Window WindowHandle {
			get { return this; }
		}

		public static List<IImageEditor> Editors {
			get {
				return editorList;
			}
		}

		public ImageEditorForm(ISurface iSurface, bool outputMade) {
			editorList.Add(this);

			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			this.ManualLanguageApply = true;
			InitializeComponent();
			
			this.Load += delegate {
				new Thread(delegate() {AddDestinations();}).Start();
			};
			
			IniConfig.IniChanged += new FileSystemEventHandler(ReloadConfiguration);


			// init surface
			Surface = iSurface;
			// Intial "saved" flag for asking if the image needs to be save
			surface.Modified = !outputMade;
			updateUI();

			// Make sure the editor is placed on the same location as the last editor was on close
			WindowDetails thisForm = new WindowDetails(this.Handle);
			thisForm.SetWindowPlacement(editorConfiguration.GetEditorPlacement());

			// Workaround: As the cursor is (mostly) selected on the surface a funny artifact is visible, this fixes it.
			hideToolstripItems();
		}

		private void RemoveSurface() {
			if (surface != null) {
				panel1.Controls.Remove(surface as Control);
				this.surface.Dispose();
				this.surface = null;
			}
		}

		private void SetSurface(ISurface newSurface) {
			if (this.Surface != null && this.Surface.Modified) {
				throw new ApplicationException("Surface modified");
			}

			RemoveSurface();

			this.surface = newSurface as Surface;
			panel1.Controls.Add(surface as Surface);
			Image backgroundForTransparency = GreenshotPlugin.Core.GreenshotResources.getImage("Checkerboard.Image");
			this.surface.TransparencyBackgroundBrush = new TextureBrush(backgroundForTransparency, WrapMode.Tile);

			surface.MovingElementChanged += delegate {
				refreshEditorControls();
			};
			surface.DrawingModeChanged += new SurfaceDrawingModeEventHandler(surface_DrawingModeChanged);
			surface.SurfaceSizeChanged += new SurfaceSizeChangeEventHandler(SurfaceSizeChanged);
			surface.SurfaceMessage += new SurfaceMessageEventHandler(SurfaceMessageReceived);
			surface.FieldAggregator.FieldChanged += new FieldChangedEventHandler(FieldAggregatorFieldChanged);

			SurfaceSizeChanged(this.Surface);

			bindFieldControls();
			refreshEditorControls();
			// Fix title
			if (surface != null && surface.CaptureDetails != null && surface.CaptureDetails.Title != null) {
				this.Text = surface.CaptureDetails.Title + " - " + Language.GetString(LangKey.editor_title);
			}
		}

		private void updateUI() {
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();

			// Disable access to the settings, for feature #3521446
			preferencesToolStripMenuItem.Visible = !coreConf.DisableSettings;
			toolStripSeparator12.Visible = !coreConf.DisableSettings;
			toolStripSeparator11.Visible = !coreConf.DisableSettings;
			btnSettings.Visible = !coreConf.DisableSettings;

			// Make sure Double-buffer is enabled
			SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

			// resizing the panel is futile, since it is docked. however, it seems
			// to fix the bug (?) with the vscrollbar not being able to shrink to
			// a smaller size than the initial panel size (as set by the forms designer)
			panel1.Height = 10;

			this.fontFamilyComboBox.PropertyChanged += new PropertyChangedEventHandler(FontPropertyChanged);
			
			obfuscateModeButton.DropDownItemClicked += FilterPresetDropDownItemClicked;
			highlightModeButton.DropDownItemClicked += FilterPresetDropDownItemClicked;
			
			toolbarButtons = new GreenshotPlugin.Controls.GreenshotToolStripButton[] { btnCursor, btnRect, btnEllipse, btnText, btnLine, btnArrow, btnFreehand, btnHighlight, btnObfuscate, btnCrop };
			//toolbarDropDownButtons = new ToolStripDropDownButton[]{btnBlur, btnPixeliate, btnTextHighlighter, btnAreaHighlighter, btnMagnifier};

			pluginToolStripMenuItem.Visible = pluginToolStripMenuItem.DropDownItems.Count > 0;
			
			// Workaround: for the MouseWheel event which doesn't get to the panel
			this.MouseWheel += new MouseEventHandler( PanelMouseWheel);

			ApplyLanguage();
		}
		
		/// <summary>
		/// Get all the destinations and display them in the file menu and the buttons
		/// </summary>
		void AddDestinations() {
			this.Invoke((MethodInvoker)delegate {
				// Create export buttons 
				foreach(IDestination destination in DestinationHelper.GetAllDestinations()) {
					if (destination.Priority <= 2) {
						continue;
					}
					if (!destination.isActive) {
						continue;
					}
					if (destination.DisplayIcon == null) {
						continue;
					}
					try {
						AddDestinationButton(destination);
					} catch (Exception addingException) {
						LOG.WarnFormat("Problem adding destination {0}", destination.Designation);
						LOG.Warn("Exception: ", addingException);
					}
				}
				// Create the file menu, normally this is done when opening but if we don't do it now the short-cut keys are missing.
				// See Bugs #3526974 & #3527020
				FileMenuDropDownOpening(null, null);
			});
		}

		void AddDestinationButton(IDestination toolstripDestination) {
			if (toolstripDestination.isDynamic) {
				ToolStripSplitButton destinationButton = new ToolStripSplitButton();
				//ToolStripDropDownButton destinationButton = new ToolStripDropDownButton();
				destinationButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
				destinationButton.Size = new System.Drawing.Size(23, 22);
				destinationButton.Text = toolstripDestination.Description;
				destinationButton.Image = toolstripDestination.DisplayIcon;

				ToolStripMenuItem defaultItem = new ToolStripMenuItem(toolstripDestination.Description);
				defaultItem.Tag = toolstripDestination;
				defaultItem.Image = toolstripDestination.DisplayIcon;
				defaultItem.Click += delegate {
					toolstripDestination.ExportCapture(true, surface, surface.CaptureDetails);
				};
				
				// The ButtonClick, this is for the icon, gets the current default item
				destinationButton.ButtonClick += delegate(object sender, EventArgs e) {
					toolstripDestination.ExportCapture(true, surface, surface.CaptureDetails);
				};
				
				// Generate the entries for the drop down
				destinationButton.DropDownOpening += delegate(object sender, EventArgs e) {
					ClearItems(destinationButton.DropDownItems);
					destinationButton.DropDownItems.Add(defaultItem);

					List<IDestination> subDestinations = new List<IDestination>();
					subDestinations.AddRange(toolstripDestination.DynamicDestinations());
					if (subDestinations.Count > 0) {
						subDestinations.Sort();
						foreach(IDestination subDestination in subDestinations) {
							IDestination closureFixedDestination = subDestination;
							ToolStripMenuItem destinationMenuItem = new ToolStripMenuItem(closureFixedDestination.Description);
							destinationMenuItem.Tag = closureFixedDestination;
							destinationMenuItem.Image = closureFixedDestination.DisplayIcon;
							destinationMenuItem.Click += delegate {
								closureFixedDestination.ExportCapture(true, surface, surface.CaptureDetails);
							};
							destinationButton.DropDownItems.Add(destinationMenuItem);
						}
					}
				};

				toolStrip1.Items.Insert(toolStrip1.Items.IndexOf(toolStripSeparator16), destinationButton);
				
			} else {
				ToolStripButton destinationButton = new ToolStripButton();
				toolStrip1.Items.Insert(toolStrip1.Items.IndexOf(toolStripSeparator16), destinationButton);
				destinationButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
				destinationButton.Size = new System.Drawing.Size(23, 22);
				destinationButton.Text = toolstripDestination.Description;
				destinationButton.Image = toolstripDestination.DisplayIcon;
				destinationButton.Click += delegate(object sender, EventArgs e) {
					toolstripDestination.ExportCapture(true, surface, surface.CaptureDetails);
				};
			}
		}
		
		/// <summary>
		/// According to some information I found, the clear doesn't work correctly when the shortcutkeys are set?
		/// This helper method takes care of this.
		/// </summary>
		/// <param name="items"></param>
		private void ClearItems(ToolStripItemCollection items) {
			foreach(var item in items) {
				ToolStripMenuItem menuItem = item as ToolStripMenuItem;
				if (menuItem != null && menuItem.ShortcutKeys != Keys.None) {
					menuItem.ShortcutKeys = Keys.None;
				}
			}
			items.Clear();
		}

		void FileMenuDropDownOpening(object sender, EventArgs eventArgs) {
			ClearItems(this.fileStripMenuItem.DropDownItems);
			//this.fileStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			//						this.saveToolStripMenuItem,
			//						this.saveAsToolStripMenuItem,
			//						this.copyImageToClipboardToolStripMenuItem,
			//						this.printToolStripMenuItem});

			foreach(IDestination destination in DestinationHelper.GetAllDestinations()) {
				if (ignoreDestinations.Contains(destination.Designation)) {
					continue;
				}
				if (!destination.isActive) {
					continue;
				}
				
				ToolStripMenuItem item = destination.GetMenuItem(true, new EventHandler(DestinationToolStripMenuItemClick));
				item.ShortcutKeys = destination.EditorShortcutKeys;
				if (item != null) {
					fileStripMenuItem.DropDownItems.Add(item);
				}
			}
			// add the elements after the destinations
			this.fileStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
									this.toolStripSeparator9,
									this.closeToolStripMenuItem});
		}

		private void SurfaceMessageReceived(object sender, SurfaceMessageEventArgs eventArgs) {
			string dateTime = DateTime.Now.ToLongTimeString();
			// TODO: Fix that we only open files, like in the tooltip
			//if (eventArgs.MessageType == SurfaceMessageTyp.FileSaved || eventArgs.MessageType == SurfaceMessageTyp.UploadedUrl) {
			if (eventArgs.MessageType == SurfaceMessageTyp.FileSaved || eventArgs.MessageType == SurfaceMessageTyp.UploadedUri) {
				updateStatusLabel(dateTime + " - " + eventArgs.Message, fileSavedStatusContextMenu);
			} else {
				updateStatusLabel(dateTime + " - " + eventArgs.Message);
			}
		}

		private void SurfaceSizeChanged(object source) {
			if (editorConfiguration.MatchSizeToCapture) {
				// Set editor's initial size to the size of the surface plus the size of the chrome
				Size imageSize = this.Surface.Image.Size;
				Size currentFormSize = this.Size;
				Size currentImageClientSize = this.panel1.ClientSize;
				int minimumFormWidth = 650;
				int minimumFormHeight = 530;
				int newWidth = Math.Max(minimumFormWidth, (currentFormSize.Width - currentImageClientSize.Width) + imageSize.Width);
				int newHeight = Math.Max(minimumFormHeight, (currentFormSize.Height - currentImageClientSize.Height) + imageSize.Height);
				this.Size = new Size(newWidth, newHeight);
			}
			dimensionsLabel.Text = this.Surface.Image.Width + "x" + this.Surface.Image.Height;
		}

		private void ReloadConfiguration(object source, FileSystemEventArgs e) {
			this.Invoke((MethodInvoker) delegate {
				// Even update language when needed
				ApplyLanguage();

				// Fix title
				if (surface != null && surface.CaptureDetails != null && surface.CaptureDetails.Title != null) {
					this.Text = surface.CaptureDetails.Title + " - " + Language.GetString(LangKey.editor_title);
				}
			});
		}
		
		public ISurface Surface {
			get {
				return surface;
			}
			set {
				SetSurface(value);
			}
		}

		public void SetImagePath(string fullpath) {
			// Check if the editor supports the format
			if (fullpath != null && (fullpath.EndsWith(".ico") || fullpath.EndsWith(".wmf"))) {
				fullpath = null;
			}
			surface.LastSaveFullPath = fullpath;

			if (fullpath == null) {
				return;
			}
			updateStatusLabel(Language.GetFormattedString(LangKey.editor_imagesaved, fullpath), fileSavedStatusContextMenu);
			this.Text = Language.GetString(LangKey.editor_title) + " - " + Path.GetFileName(fullpath);
		}
		
		void surface_DrawingModeChanged(object source, DrawingModes drawingMode) {
			switch (drawingMode) {
				case DrawingModes.None:
					SetButtonChecked(btnCursor);
					break;
				case DrawingModes.Ellipse:
					SetButtonChecked(btnEllipse);
					break;
				case DrawingModes.Rect:
					SetButtonChecked(btnRect);
					break;
				case DrawingModes.Text:
					SetButtonChecked(btnText);
					break;
				case DrawingModes.Line:
					SetButtonChecked(btnLine);
					break;
				case DrawingModes.Arrow:
					SetButtonChecked(btnArrow);
					break;
				case DrawingModes.Crop:
					SetButtonChecked(btnCrop);
					break;
				case DrawingModes.Highlight:
					SetButtonChecked(btnHighlight);
					break;
				case DrawingModes.Obfuscate:
					SetButtonChecked(btnObfuscate);
					break;
				case DrawingModes.Path:
					SetButtonChecked(btnFreehand);
					break;
			}
		}

		#region plugin interfaces
		
		/**
		 * Interfaces for plugins, see GreenshotInterface for more details!
		 */
		
		public Image GetImageForExport() {
			return surface.GetImageForExport();
		}
		
		public ICaptureDetails CaptureDetails {
			get { return surface.CaptureDetails; }
		}
		
		public void SaveToStream(Stream stream, OutputSettings outputSettings) {
			using (Image image = surface.GetImageForExport()) {
				ImageOutput.SaveToStream(image, stream, outputSettings);
			}
		}

		public ToolStripMenuItem GetPluginMenuItem() {
			return pluginToolStripMenuItem;
		}

		public ToolStripMenuItem GetFileMenuItem() {
			return fileStripMenuItem;
		}
		#endregion
		
		#region filesystem options
		void BtnSaveClick(object sender, EventArgs e) {
			string destinationDesignation = Destinations.FileDestination.DESIGNATION;
			if (surface.LastSaveFullPath == null) {
				destinationDesignation = Destinations.FileWithDialogDestination.DESIGNATION;
			}
			DestinationHelper.ExportCapture(true, destinationDesignation, surface, surface.CaptureDetails);
		}
		
		void BtnClipboardClick(object sender, EventArgs e) {
			DestinationHelper.ExportCapture(true, Destinations.ClipboardDestination.DESIGNATION, surface, surface.CaptureDetails);
		}

		void BtnPrintClick(object sender, EventArgs e) {
			// The BeginInvoke is a solution for the printdialog not having focus
			this.BeginInvoke((MethodInvoker) delegate {
				DestinationHelper.ExportCapture(true, Destinations.PrinterDestination.DESIGNATION, surface, surface.CaptureDetails);
			});
		}

		void CloseToolStripMenuItemClick(object sender, System.EventArgs e) {
			this.Close();
		}
		#endregion
		
		#region drawing options
		void BtnEllipseClick(object sender, EventArgs e) {
			surface.DrawingMode = DrawingModes.Ellipse;
			refreshFieldControls();
		}
		
		void BtnCursorClick(object sender, EventArgs e) {
			surface.DrawingMode = DrawingModes.None;
			refreshFieldControls();
		}
		
		void BtnRectClick(object sender, EventArgs e) {
			surface.DrawingMode = DrawingModes.Rect;
			refreshFieldControls();
		}
		
		void BtnTextClick(object sender, EventArgs e) {
			surface.DrawingMode = DrawingModes.Text;
			refreshFieldControls();
		}
		
		void BtnLineClick(object sender, EventArgs e) {
			surface.DrawingMode = DrawingModes.Line;
			refreshFieldControls();
		}
		
		void BtnArrowClick(object sender, EventArgs e) {
			surface.DrawingMode = DrawingModes.Arrow;
			refreshFieldControls();
		}
		
		
		void BtnCropClick(object sender, EventArgs e) {
			surface.DrawingMode = DrawingModes.Crop;
			refreshFieldControls();
		}
		
		void BtnHighlightClick(object sender, EventArgs e) {
			surface.DrawingMode = DrawingModes.Highlight;
			refreshFieldControls();
		}
		
		void BtnObfuscateClick(object sender, EventArgs e) {
			surface.DrawingMode = DrawingModes.Obfuscate;
			refreshFieldControls();
		}

		void BtnFreehandClick(object sender, EventArgs e) {
			surface.DrawingMode = DrawingModes.Path;
			refreshFieldControls();
		}
		
		void SetButtonChecked(ToolStripButton btn) {
			UncheckAllToolButtons();
			btn.Checked = true;
		}
		
		private void UncheckAllToolButtons() {
			if (toolbarButtons != null) {
				foreach (ToolStripButton butt in toolbarButtons) {
					butt.Checked = false;
				}
			}
		}
		
		void AddRectangleToolStripMenuItemClick(object sender, System.EventArgs e) {
			BtnRectClick(sender, e);
		}

		void DrawFreehandToolStripMenuItemClick(object sender, System.EventArgs e) {
			BtnFreehandClick(sender, e);
		}
		
		void AddEllipseToolStripMenuItemClick(object sender, System.EventArgs e) {
			BtnEllipseClick(sender, e);
		}
		
		void AddTextBoxToolStripMenuItemClick(object sender, System.EventArgs e) {
			BtnTextClick(sender, e);
		}
		
		void DrawLineToolStripMenuItemClick(object sender, System.EventArgs e) {
			BtnLineClick(sender, e);
		}
		
		void DrawArrowToolStripMenuItemClick(object sender, EventArgs e) {
			BtnArrowClick(sender, e);
		}
		
		void DrawHighlightToolStripMenuItemClick(object sender, EventArgs e) {
			BtnHighlightClick(sender, e);
		}
		
		void BlurToolStripMenuItemClick(object sender, EventArgs e) {
			BtnObfuscateClick(sender, e);
		}
		
		void RemoveObjectToolStripMenuItemClick(object sender, System.EventArgs e) {
			surface.RemoveSelectedElements();
		}

		void BtnDeleteClick(object sender, EventArgs e) {
			RemoveObjectToolStripMenuItemClick(sender, e);
		}
		#endregion
		
		#region copy&paste options
		void CutToolStripMenuItemClick(object sender, System.EventArgs e) {
			surface.CutSelectedElements();
			updateClipboardSurfaceDependencies();
		}

		void BtnCutClick(object sender, System.EventArgs e) {
			CutToolStripMenuItemClick(sender, e);
		}
		
		void CopyToolStripMenuItemClick(object sender, System.EventArgs e) {
			surface.CopySelectedElements();
			updateClipboardSurfaceDependencies();
		}

		void BtnCopyClick(object sender, System.EventArgs e) {
			CopyToolStripMenuItemClick(sender, e);
		}
		
		void PasteToolStripMenuItemClick(object sender, System.EventArgs e) {
			surface.PasteElementFromClipboard();
			updateClipboardSurfaceDependencies();
		}

		void BtnPasteClick(object sender, System.EventArgs e) {
			PasteToolStripMenuItemClick(sender, e);
		}

		void UndoToolStripMenuItemClick(object sender, System.EventArgs e) {
			surface.Undo();
			updateUndoRedoSurfaceDependencies();
		}

		void BtnUndoClick(object sender, System.EventArgs e) {
			UndoToolStripMenuItemClick(sender, e);
		}

		void RedoToolStripMenuItemClick(object sender, System.EventArgs e) {
			surface.Redo();
			updateUndoRedoSurfaceDependencies();
		}

		void BtnRedoClick(object sender, System.EventArgs e) {
			RedoToolStripMenuItemClick(sender, e);
		}

		void DuplicateToolStripMenuItemClick(object sender, System.EventArgs e) {
			surface.DuplicateSelectedElements();
			updateClipboardSurfaceDependencies();
		}
		#endregion
		
		#region element properties
		void UpOneLevelToolStripMenuItemClick(object sender, EventArgs e) {
			surface.PullElementsUp();
		}
		
		void DownOneLevelToolStripMenuItemClick(object sender, EventArgs e) {
			surface.PushElementsDown();
		}
		
		void UpToTopToolStripMenuItemClick(object sender, EventArgs e) {
			surface.PullElementsToTop();
		}
		
		void DownToBottomToolStripMenuItemClick(object sender, EventArgs e) {
			surface.PushElementsToBottom();
		}
		
		
		#endregion
		
		#region help
		void HelpToolStripMenuItem1Click(object sender, System.EventArgs e) {
			HelpFileLoader.LoadHelp();
		}

		void AboutToolStripMenuItemClick(object sender, System.EventArgs e) {
			new AboutForm().Show();
		}

		void PreferencesToolStripMenuItemClick(object sender, System.EventArgs e) {
			MainForm.instance.ShowSetting();
		}

		void BtnSettingsClick(object sender, System.EventArgs e) {
			PreferencesToolStripMenuItemClick(sender, e);
		}

		void BtnHelpClick(object sender, System.EventArgs e) {
			HelpToolStripMenuItem1Click(sender, e);
		}
		#endregion
		
		#region image editor event handlers
		void ImageEditorFormActivated(object sender, EventArgs e) {
			updateClipboardSurfaceDependencies();
			updateUndoRedoSurfaceDependencies();
		}

		void ImageEditorFormFormClosing(object sender, FormClosingEventArgs e) {
			IniConfig.IniChanged -= new FileSystemEventHandler(ReloadConfiguration);
			if (surface.Modified && !editorConfiguration.SuppressSaveDialogAtClose) {
				// Make sure the editor is visible
				WindowDetails.ToForeground(this.Handle);

				MessageBoxButtons buttons = MessageBoxButtons.YesNoCancel;
				// Dissallow "CANCEL" if the application needs to shutdown
				if (e.CloseReason == CloseReason.ApplicationExitCall || e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.TaskManagerClosing) {
					buttons = MessageBoxButtons.YesNo;
				}
				DialogResult result = MessageBox.Show(Language.GetString(LangKey.editor_close_on_save), Language.GetString(LangKey.editor_close_on_save_title), buttons, MessageBoxIcon.Question);
				if (result.Equals(DialogResult.Cancel)) {
					e.Cancel = true;
					return;
				}
				if (result.Equals(DialogResult.Yes)) {
					BtnSaveClick(sender,e);
					// Check if the save was made, if not it was cancelled so we cancel the closing
					if (surface.Modified) {
						e.Cancel = true;
						return;
					}
				}
			}
			// persist our geometry string.
			editorConfiguration.SetEditorPlacement(new WindowDetails(this.Handle).GetWindowPlacement());
			IniConfig.Save();
			
			// remove from the editor list
			editorList.Remove(this);

			surface.Dispose();

			System.GC.Collect();
		}

		void ImageEditorFormKeyDown(object sender, KeyEventArgs e) {
			// LOG.Debug("Got key event "+e.KeyCode + ", " + e.Modifiers);
			// avoid conflict with other shortcuts and
			// make sure there's no selected element claiming input focus
			if(e.Modifiers.Equals(Keys.None) && !surface.KeysLocked) {
				switch(e.KeyCode) {
					case Keys.Escape:
						BtnCursorClick(sender, e);
						break;
					case Keys.R:
						BtnRectClick(sender, e);
						break;
					case Keys.E:
						BtnEllipseClick(sender, e);
						break;
					case Keys.L:
						BtnLineClick(sender, e);
						break;
					case Keys.F:
						BtnFreehandClick(sender, e);
						break;
					case Keys.A:
						BtnArrowClick(sender, e);
						break;
					case Keys.T:
						BtnTextClick(sender, e);
						break;
					case Keys.H:
						BtnHighlightClick(sender, e);
						break;
					case Keys.O:
						BtnObfuscateClick(sender, e);
						break;
					case Keys.C:
						BtnCropClick(sender, e);
						break;
					case Keys.P:
						//surface.PreviewMode = !surface.PreviewMode;
						break;
				}
			} else if (e.Modifiers.Equals(Keys.Control)) {
				switch (e.KeyCode) {
					case Keys.Z:
						surface.Undo();
						updateUndoRedoSurfaceDependencies();
						break;
					case Keys.Y:
						surface.Redo();
						updateUndoRedoSurfaceDependencies();
						break;
				}
			}
		}

		/// <summary>
		/// This is a "work-around" for the MouseWheel event which doesn't get to the panel
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PanelMouseWheel(object sender, MouseEventArgs e) {
			panel1.Focus();
		}
		#endregion
		
		#region key handling
		protected override bool ProcessKeyPreview(ref Message msg) {
			// disable default key handling if surface has requested a lock
			if (!surface.KeysLocked) {
				return base.ProcessKeyPreview(ref msg);
			}
			return false;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys k) {
			// disable default key handling if surface has requested a lock
			if (!surface.KeysLocked) {
				surface.ProcessCmdKey(k);
				return base.ProcessCmdKey(ref msg, k);
			}
			return false;
		}
		#endregion
		
		#region helpers
		
		private void updateUndoRedoSurfaceDependencies() {
			bool canUndo = surface.CanUndo;
			this.btnUndo.Enabled = canUndo;
			this.undoToolStripMenuItem.Enabled = canUndo;
			string undoAction = "";
			if (canUndo) {
				if (surface.UndoActionKey != LangKey.none) {
					undoAction = Language.GetString(surface.UndoActionKey);
				}
			}
			string undoText = Language.GetFormattedString(LangKey.editor_undo, undoAction);
			this.btnUndo.Text = undoText;
			this.undoToolStripMenuItem.Text = undoText;

			bool canRedo = surface.CanRedo;
			this.btnRedo.Enabled = canRedo;
			this.redoToolStripMenuItem.Enabled = canRedo;
			string redoAction = "";
			if (canRedo) {
                if (surface.RedoActionKey != LangKey.none) {
                    redoAction = Language.GetString(surface.RedoActionKey);
                }
			}
			string redoText = Language.GetFormattedString(LangKey.editor_redo, redoAction);
			this.btnRedo.Text = redoText;
			this.redoToolStripMenuItem.Text = redoText;

		}

		private void updateClipboardSurfaceDependencies() {
			// check dependencies for the Surface
			bool hasItems = surface.HasSelectedElements();
			bool actionAllowedForSelection = hasItems && !controlsDisabledDueToConfirmable;
			
			// buttons
			this.btnCut.Enabled = actionAllowedForSelection;
			this.btnCopy.Enabled = actionAllowedForSelection;
			this.btnDelete.Enabled = actionAllowedForSelection;

			// menus
			this.removeObjectToolStripMenuItem.Enabled = actionAllowedForSelection;
			this.copyToolStripMenuItem.Enabled = actionAllowedForSelection;
			this.cutToolStripMenuItem.Enabled = actionAllowedForSelection;
			this.duplicateToolStripMenuItem.Enabled = actionAllowedForSelection;

			// check dependencies for the Clipboard
			bool hasClipboard = ClipboardHelper.ContainsFormat(SUPPORTED_CLIPBOARD_FORMATS);
			this.btnPaste.Enabled = hasClipboard && !controlsDisabledDueToConfirmable;
			this.pasteToolStripMenuItem.Enabled = hasClipboard && !controlsDisabledDueToConfirmable;
		}

		#endregion
		
		#region status label handling
		private void updateStatusLabel(string text, ContextMenuStrip contextMenu) {
			statusLabel.Text = text;
			statusStrip1.ContextMenuStrip = contextMenu;
		}
		
		private void updateStatusLabel(string text) {
			updateStatusLabel(text, null);
		}
		private void clearStatusLabel() {
			updateStatusLabel(null, null);
		}
		
		void StatusLabelClicked(object sender, MouseEventArgs e) {
			ToolStrip ss = (StatusStrip)((ToolStripStatusLabel)sender).Owner;
			if(ss.ContextMenuStrip != null) {
				ss.ContextMenuStrip.Show(ss, e.X, e.Y);
			}
		}
		
		void CopyPathMenuItemClick(object sender, EventArgs e) {
			ClipboardHelper.SetClipboardData(surface.LastSaveFullPath);
		}
		
		void OpenDirectoryMenuItemClick(object sender, EventArgs e) {
			ProcessStartInfo psi = new ProcessStartInfo("explorer");
			psi.Arguments = Path.GetDirectoryName(surface.LastSaveFullPath);
			psi.UseShellExecute = false;
			Process p = new Process();
			p.StartInfo = psi;
			p.Start();
		}
		#endregion
		
		private void bindFieldControls() {
			new BidirectionalBinding(btnFillColor, "SelectedColor", surface.FieldAggregator.GetField(FieldType.FILL_COLOR), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(btnLineColor, "SelectedColor", surface.FieldAggregator.GetField(FieldType.LINE_COLOR), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(lineThicknessUpDown, "Value", surface.FieldAggregator.GetField(FieldType.LINE_THICKNESS), "Value", DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(blurRadiusUpDown, "Value", surface.FieldAggregator.GetField(FieldType.BLUR_RADIUS), "Value", DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(magnificationFactorUpDown, "Value", surface.FieldAggregator.GetField(FieldType.MAGNIFICATION_FACTOR), "Value", DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(pixelSizeUpDown, "Value", surface.FieldAggregator.GetField(FieldType.PIXEL_SIZE), "Value", DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(brightnessUpDown, "Value", surface.FieldAggregator.GetField(FieldType.BRIGHTNESS), "Value", DecimalDoublePercentageConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(fontFamilyComboBox, "Text", surface.FieldAggregator.GetField(FieldType.FONT_FAMILY), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(fontSizeUpDown, "Value", surface.FieldAggregator.GetField(FieldType.FONT_SIZE), "Value", DecimalFloatConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(fontBoldButton, "Checked", surface.FieldAggregator.GetField(FieldType.FONT_BOLD), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(fontItalicButton, "Checked", surface.FieldAggregator.GetField(FieldType.FONT_ITALIC), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(shadowButton, "Checked", surface.FieldAggregator.GetField(FieldType.SHADOW), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(previewQualityUpDown, "Value", surface.FieldAggregator.GetField(FieldType.PREVIEW_QUALITY), "Value", DecimalDoublePercentageConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(obfuscateModeButton, "SelectedTag", surface.FieldAggregator.GetField(FieldType.PREPARED_FILTER_OBFUSCATE), "Value");
			new BidirectionalBinding(highlightModeButton, "SelectedTag", surface.FieldAggregator.GetField(FieldType.PREPARED_FILTER_HIGHLIGHT), "Value");
		}
		
		/// <summary>
		/// shows/hides field controls (2nd toolbar on top) depending on fields of selected elements
		/// </summary>
		private void refreshFieldControls() {
			propertiesToolStrip.SuspendLayout();
			if(surface.HasSelectedElements() || surface.DrawingMode != DrawingModes.None) {
				FieldAggregator props = surface.FieldAggregator;
				btnFillColor.Visible = props.HasFieldValue(FieldType.FILL_COLOR);
				btnLineColor.Visible = props.HasFieldValue(FieldType.LINE_COLOR);
				lineThicknessLabel.Visible = lineThicknessUpDown.Visible = props.HasFieldValue(FieldType.LINE_THICKNESS);
				blurRadiusLabel.Visible = blurRadiusUpDown.Visible = props.HasFieldValue(FieldType.BLUR_RADIUS);
				previewQualityLabel.Visible = previewQualityUpDown.Visible = props.HasFieldValue(FieldType.PREVIEW_QUALITY);
				magnificationFactorLabel.Visible = magnificationFactorUpDown.Visible = props.HasFieldValue(FieldType.MAGNIFICATION_FACTOR);
				pixelSizeLabel.Visible = pixelSizeUpDown.Visible = props.HasFieldValue(FieldType.PIXEL_SIZE);
				brightnessLabel.Visible = brightnessUpDown.Visible = props.HasFieldValue(FieldType.BRIGHTNESS);
				arrowHeadsLabel.Visible = arrowHeadsDropDownButton.Visible = props.HasFieldValue(FieldType.ARROWHEADS);
				fontFamilyComboBox.Visible = props.HasFieldValue(FieldType.FONT_FAMILY);
				fontSizeLabel.Visible = fontSizeUpDown.Visible = props.HasFieldValue(FieldType.FONT_SIZE);
				fontBoldButton.Visible = props.HasFieldValue(FieldType.FONT_BOLD);
				fontItalicButton.Visible = props.HasFieldValue(FieldType.FONT_ITALIC);
				shadowButton.Visible = props.HasFieldValue(FieldType.SHADOW);
				btnConfirm.Visible = btnCancel.Visible = props.HasFieldValue(FieldType.FLAGS)
					&& ((FieldType.Flag)props.GetFieldValue(FieldType.FLAGS)&FieldType.Flag.CONFIRMABLE) == FieldType.Flag.CONFIRMABLE;
				
				obfuscateModeButton.Visible = props.HasFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE);
				highlightModeButton.Visible = props.HasFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT);
			} else {
				hideToolstripItems();
			}
			propertiesToolStrip.ResumeLayout();
		}
		
		private void hideToolstripItems() {
			foreach(ToolStripItem toolStripItem in propertiesToolStrip.Items) {
				toolStripItem.Visible = false;
			}
		}
		
		/// <summary>
		/// refreshes all editor controls depending on selected elements and their fields
		/// </summary>
		private void refreshEditorControls() {
			FieldAggregator props = surface.FieldAggregator;
			// if a confirmable element is selected, we must disable most of the controls
			// since we demand confirmation or cancel for confirmable element
			if (props.HasFieldValue(FieldType.FLAGS) && ((FieldType.Flag)props.GetFieldValue(FieldType.FLAGS) & FieldType.Flag.CONFIRMABLE) == FieldType.Flag.CONFIRMABLE) {
				// disable most controls
				if(!controlsDisabledDueToConfirmable) {
					ToolStripItemEndisabler.Disable(menuStrip1);
					ToolStripItemEndisabler.Disable(toolStrip1);
					ToolStripItemEndisabler.Disable(toolStrip2);
					ToolStripItemEndisabler.Enable(closeToolStripMenuItem);
					ToolStripItemEndisabler.Enable(helpToolStripMenuItem);
					ToolStripItemEndisabler.Enable(aboutToolStripMenuItem);
					ToolStripItemEndisabler.Enable(preferencesToolStripMenuItem);
					controlsDisabledDueToConfirmable = true;
				}
			} else if(controlsDisabledDueToConfirmable) {
				// re-enable disabled controls, confirmable element has either been confirmed or cancelled
				ToolStripItemEndisabler.Enable(menuStrip1);
				ToolStripItemEndisabler.Enable(toolStrip1);
				ToolStripItemEndisabler.Enable(toolStrip2);
				controlsDisabledDueToConfirmable = false;
			}
			
			// en/disable controls depending on whether an element is selected at all
			updateClipboardSurfaceDependencies();
			updateUndoRedoSurfaceDependencies();
			
			// en/disablearrage controls depending on hierarchy of selected elements
			bool actionAllowedForSelection = surface.HasSelectedElements() && !controlsDisabledDueToConfirmable;
			bool push = actionAllowedForSelection && surface.CanPushSelectionDown();
			bool pull = actionAllowedForSelection && surface.CanPullSelectionUp();
			this.arrangeToolStripMenuItem.Enabled = (push || pull);
			if (this.arrangeToolStripMenuItem.Enabled) {
				this.upToTopToolStripMenuItem.Enabled = pull;
				this.upOneLevelToolStripMenuItem.Enabled = pull;
				this.downToBottomToolStripMenuItem.Enabled = push;
				this.downOneLevelToolStripMenuItem.Enabled = push;
			}
			
			// finally show/hide field controls depending on the fields of selected elements
			refreshFieldControls();
		}
	
		
		void ArrowHeadsToolStripMenuItemClick(object sender, EventArgs e) {
			surface.FieldAggregator.GetField(FieldType.ARROWHEADS).Value = (ArrowContainer.ArrowHeadCombination)((ToolStripMenuItem)sender).Tag;
		}
		
		void EditToolStripMenuItemClick(object sender, EventArgs e) {
			updateClipboardSurfaceDependencies();
			updateUndoRedoSurfaceDependencies();
		}

		void FontPropertyChanged(object sender, EventArgs e) {
			// in case we forced another FontStyle before, reset it first.
			if(originalBoldCheckState != fontBoldButton.Checked) fontBoldButton.Checked = originalBoldCheckState;
			if(originalItalicCheckState != fontItalicButton.Checked) fontItalicButton.Checked = originalItalicCheckState;
			
            FontFamily fam = fontFamilyComboBox.FontFamily;
           
            bool boldAvailable = fam.IsStyleAvailable(FontStyle.Bold);
            if(!boldAvailable) {
            	originalBoldCheckState = fontBoldButton.Checked;
            	fontBoldButton.Checked = false;
            }
            fontBoldButton.Enabled = boldAvailable;
           
            bool italicAvailable = fam.IsStyleAvailable(FontStyle.Italic);
            if(!italicAvailable) fontItalicButton.Checked = false;
            fontItalicButton.Enabled = italicAvailable;
           
            bool regularAvailable = fam.IsStyleAvailable(FontStyle.Regular);
            if(!regularAvailable) {
                if(boldAvailable) {
                    fontBoldButton.Checked = true;
                } else if(italicAvailable) {
                    fontItalicButton.Checked = true;
                }
            }
        } 
		
		void FieldAggregatorFieldChanged(object sender, FieldChangedEventArgs e) {
			// in addition to selection, deselection of elements, we need to
			// refresh toolbar if prepared filter mode is changed
			if(e.Field.FieldType == FieldType.PREPARED_FILTER_HIGHLIGHT) {
				refreshFieldControls();
			}
		}
		
		void FontBoldButtonClick(object sender, EventArgs e) {
			originalBoldCheckState = fontBoldButton.Checked;
		}

		void FontItalicButtonClick(object sender, System.EventArgs e) {
			originalItalicCheckState = fontItalicButton.Checked;
		}
		
		void ToolBarFocusableElementGotFocus(object sender, System.EventArgs e) {
			surface.KeysLocked = true;
		}
		void ToolBarFocusableElementLostFocus(object sender, System.EventArgs e) {
			surface.KeysLocked = false;
		}
		
		void SaveElementsToolStripMenuItemClick(object sender, EventArgs e) {
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Greenshot templates (*.gst)|*.gst";
			saveFileDialog.FileName = FilenameHelper.GetFilenameWithoutExtensionFromPattern(coreConf.OutputFileFilenamePattern, surface.CaptureDetails);
			DialogResult dialogResult = saveFileDialog.ShowDialog();
			if(dialogResult.Equals(DialogResult.OK)) {
				using (Stream streamWrite = File.OpenWrite(saveFileDialog.FileName)) {
					surface.SaveElementsToStream(streamWrite);
				}
			}
		}
		
		void LoadElementsToolStripMenuItemClick(object sender, EventArgs e) {
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Greenshot templates (*.gst)|*.gst";
			if (openFileDialog.ShowDialog() == DialogResult.OK) {
				using (Stream streamRead = File.OpenRead(openFileDialog.FileName)) {
					surface.LoadElementsFromStream(streamRead);
				}
				surface.Refresh();
			}
		}

		void DestinationToolStripMenuItemClick(object sender, EventArgs e) {
			IDestination clickedDestination = null;
			if (sender is Control) {
				Control clickedControl = sender as Control;
				if (clickedControl.ContextMenuStrip != null) {
					clickedControl.ContextMenuStrip.Show(Cursor.Position);
					return;
				}
				clickedDestination = (IDestination)clickedControl.Tag;
			} else if (sender is ToolStripMenuItem) {
				ToolStripMenuItem clickedMenuItem = sender as ToolStripMenuItem;
				clickedDestination = (IDestination)clickedMenuItem.Tag;
			}
			if (clickedDestination != null) {
				ExportInformation exportInformation = clickedDestination.ExportCapture(true, surface, surface.CaptureDetails);
				if (exportInformation != null && exportInformation.ExportMade) {
					surface.Modified = false;
				}
			}
		}
		
		protected void FilterPresetDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
			refreshFieldControls();
			this.Invalidate(true);
		}
		
		void SelectAllToolStripMenuItemClick(object sender, EventArgs e) {
			surface.SelectAllElements();
		}

		
		void BtnConfirmClick(object sender, EventArgs e) {
			surface.ConfirmSelectedConfirmableElements(true);
			refreshFieldControls();
		}
		
		void BtnCancelClick(object sender, EventArgs e) {
			surface.ConfirmSelectedConfirmableElements(false);
			refreshFieldControls();
		}
		
		void Insert_window_toolstripmenuitemMouseEnter(object sender, EventArgs e) {
			ToolStripMenuItem captureWindowMenuItem = (ToolStripMenuItem)sender;
			MainForm.instance.AddCaptureWindowMenuItems(captureWindowMenuItem, Contextmenu_window_Click);	
		}

		void Contextmenu_window_Click(object sender, EventArgs e) {
			ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
			try {
				WindowDetails windowToCapture = (WindowDetails)clickedItem.Tag;
				ICapture capture = new Capture();
				using (Graphics graphics = Graphics.FromHwnd(this.Handle)) {
					capture.CaptureDetails.DpiX = graphics.DpiY;
					capture.CaptureDetails.DpiY = graphics.DpiY;
				}
				windowToCapture.Restore();
				windowToCapture = CaptureHelper.SelectCaptureWindow(windowToCapture);
				if (windowToCapture != null) {
					capture = CaptureHelper.CaptureWindow(windowToCapture, capture, coreConf.WindowCaptureMode);
					this.Activate();
					WindowDetails.ToForeground(this.Handle);
					if (capture!= null && capture.Image != null) {
						bool addShadow = false;
						if (addShadow) {
							Point offset = new Point(-1,-1);
							using (Bitmap shadowImage = ImageHelper.CreateShadow(capture.Image, 1f, 7, ref offset, PixelFormat.Format32bppArgb)) {
								surface.AddBitmapContainer(shadowImage, 100, 100);
							}
						} else {
							surface.AddBitmapContainer((Bitmap)capture.Image, 100, 100);
						}
					}
				}

				if (capture!= null) {
					capture.Dispose();
				}
			} catch (Exception exception) {
				LOG.Error(exception);
			}
		}
		
		void AutoCropToolStripMenuItemClick(object sender, EventArgs e) {
			if (surface.AutoCrop()) {
				refreshFieldControls();
			}
		}

		
		void AddBorderToolStripMenuItemClick(object sender, EventArgs e)
		{
			surface.ApplyBitmapEffect(Effects.Border);
			updateUndoRedoSurfaceDependencies();
		}
		
		void AddDropshadowToolStripMenuItemClick(object sender, EventArgs e)
		{
			surface.ApplyBitmapEffect(Effects.Shadow);
			updateUndoRedoSurfaceDependencies();
		}
		
		void TornEdgesToolStripMenuItemClick(object sender, EventArgs e)
		{
			surface.ApplyBitmapEffect(Effects.TornEdge);
			updateUndoRedoSurfaceDependencies();
		}
		
		void GrayscaleToolStripMenuItemClick(object sender, EventArgs e)
		{
			surface.ApplyBitmapEffect(Effects.Grayscale);
			updateUndoRedoSurfaceDependencies();
		}
		
		void RotateCwToolstripButtonClick(object sender, EventArgs e)
		{
			surface.ApplyBitmapEffect(Effects.RotateClockwise);
			updateUndoRedoSurfaceDependencies();
		}
		
		void RotateCcwToolstripButtonClick(object sender, EventArgs e)
		{
			surface.ApplyBitmapEffect(Effects.RotateCounterClockwise);
			updateUndoRedoSurfaceDependencies();
		}
		
		void InvertToolStripMenuItemClick(object sender, EventArgs e)
		{
			surface.ApplyBitmapEffect(Effects.Invert);
			updateUndoRedoSurfaceDependencies();
		}
	}
}
