/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Configuration;
using Greenshot.Destinations;
using Greenshot.Drawing;
using Greenshot.Drawing.Fields;
using Greenshot.Drawing.Fields.Binding;
using Greenshot.Forms;
using Greenshot.Help;
using Greenshot.Helpers;
using Greenshot.IniFile;
using Greenshot.Plugin;
using Greenshot.Plugin.Drawing;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.Effects;
using GreenshotPlugin.Interfaces.Drawing;
using GreenshotPlugin.UnmanagedHelpers;
using log4net;

namespace Greenshot {
	/// <summary>
	/// Description of ImageEditorForm.
	/// </summary>
	public partial class ImageEditorForm : BaseForm, IImageEditor {
		private static readonly ILog Log = LogManager.GetLogger(typeof(ImageEditorForm));
		private static readonly EditorConfiguration EditorConfiguration = IniConfig.GetIniSection<EditorConfiguration>();
		private static readonly List<string> IgnoreDestinations = new List<string> { PickerDestination.DESIGNATION, EditorDestination.DESIGNATION };
		private static readonly List<IImageEditor> EditorList = new List<IImageEditor>();

		private Surface _surface;
		private GreenshotToolStripButton[] _toolbarButtons;
		
		private static readonly string[] SupportedClipboardFormats = {typeof(string).FullName, "Text", typeof(IDrawableContainerList).FullName};

		private bool _originalBoldCheckState;
		private bool _originalItalicCheckState;
		
		// whether part of the editor controls are disabled depending on selected item(s)
		private bool _controlsDisabledDueToConfirmable;

		/// <summary>
		/// An Implementation for the IImageEditor, this way Plugins have access to the HWND handles wich can be used with Win32 API calls.
		/// </summary>
		public IWin32Window WindowHandle => this;

		public static List<IImageEditor> Editors {
			get {
				try {
					EditorList.Sort((e1, e2) => string.Compare(e1.Surface.CaptureDetails.Title, e2.Surface.CaptureDetails.Title, StringComparison.Ordinal));
				} catch(Exception ex) {
					Log.Warn("Sorting of editors failed.", ex);
				}
				return EditorList;
			}
		}

		public ImageEditorForm(ISurface iSurface, bool outputMade) {
			EditorList.Add(this);

			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			ManualLanguageApply = true;
			InitializeComponent();

			Load += delegate {
				var thread = new Thread(AddDestinations)
				{
					Name = "add destinations"
				};
				thread.Start();
			};

			// Make sure the editor is placed on the same location as the last editor was on close
			// But only if this still exists, else it will be reset (BUG-1812)
			WindowPlacement editorWindowPlacement = EditorConfiguration.GetEditorPlacement();
			Rectangle screenbounds = WindowCapture.GetScreenBounds();
			if (!screenbounds.Contains(editorWindowPlacement.NormalPosition))
			{
				EditorConfiguration.ResetEditorPlacement();
			}
			// ReSharper disable once UnusedVariable
			WindowDetails thisForm = new WindowDetails(Handle)
			{
				WindowPlacement = EditorConfiguration.GetEditorPlacement()
			};

			// init surface
			Surface = iSurface;
			// Intial "saved" flag for asking if the image needs to be save
			_surface.Modified = !outputMade;

			UpdateUi();

			// Workaround: As the cursor is (mostly) selected on the surface a funny artifact is visible, this fixes it.
			HideToolstripItems();
		}

		/// <summary>
		/// Remove the current surface
		/// </summary>
		private void RemoveSurface() {
			if (_surface == null)
			{
				return;
			}
			panel1.Controls.Remove(_surface);
			_surface.Dispose();
			_surface = null;
		}

		/// <summary>
		/// Change the surface
		/// </summary>
		/// <param name="newSurface"></param>
		private void SetSurface(ISurface newSurface) {
			if (Surface != null && Surface.Modified) {
				throw new ApplicationException("Surface modified");
			}

			RemoveSurface();

			panel1.Height = 10;
			panel1.Width = 10;
			_surface = newSurface as Surface;
			if (_surface != null)
			{
				panel1.Controls.Add(_surface);
			}
			Image backgroundForTransparency = GreenshotResources.getImage("Checkerboard.Image");
			if (_surface != null)
			{
				_surface.TransparencyBackgroundBrush = new TextureBrush(backgroundForTransparency, WrapMode.Tile);

				_surface.MovingElementChanged += delegate {
					RefreshEditorControls();
				};
				_surface.DrawingModeChanged += surface_DrawingModeChanged;
				_surface.SurfaceSizeChanged += SurfaceSizeChanged;
				_surface.SurfaceMessage += SurfaceMessageReceived;
				_surface.FieldAggregator.FieldChanged += FieldAggregatorFieldChanged;
				SurfaceSizeChanged(Surface, null);

				BindFieldControls();
				RefreshEditorControls();
				// Fix title
				if (_surface?.CaptureDetails?.Title != null) {
					Text = _surface.CaptureDetails.Title + " - " + Language.GetString(LangKey.editor_title);
				}
			}
			Activate();
			WindowDetails.ToForeground(Handle);
		}

		private void UpdateUi() {
			// Disable access to the settings, for feature #3521446
			preferencesToolStripMenuItem.Visible = !coreConfiguration.DisableSettings;
			toolStripSeparator12.Visible = !coreConfiguration.DisableSettings;
			toolStripSeparator11.Visible = !coreConfiguration.DisableSettings;
			btnSettings.Visible = !coreConfiguration.DisableSettings;

			// Make sure Double-buffer is enabled
			SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

			// resizing the panel is futile, since it is docked. however, it seems
			// to fix the bug (?) with the vscrollbar not being able to shrink to
			// a smaller size than the initial panel size (as set by the forms designer)
			panel1.Height = 10;

			fontFamilyComboBox.PropertyChanged += FontPropertyChanged;
			
			obfuscateModeButton.DropDownItemClicked += FilterPresetDropDownItemClicked;
			highlightModeButton.DropDownItemClicked += FilterPresetDropDownItemClicked;

			_toolbarButtons = new[] { btnCursor, btnRect, btnEllipse, btnText, btnLine, btnArrow, btnFreehand, btnHighlight, btnObfuscate, btnCrop, btnStepLabel, btnSpeechBubble };
			//toolbarDropDownButtons = new ToolStripDropDownButton[]{btnBlur, btnPixeliate, btnTextHighlighter, btnAreaHighlighter, btnMagnifier};

			pluginToolStripMenuItem.Visible = pluginToolStripMenuItem.DropDownItems.Count > 0;
			
			// Workaround: for the MouseWheel event which doesn't get to the panel
			MouseWheel += PanelMouseWheel;

			// Make sure the value is set correctly when starting
			counterUpDown.Value = Surface.CounterStart;
			ApplyLanguage();
		}

		/// <summary>
		/// Workaround for having a border around the dropdown
		/// See: http://stackoverflow.com/questions/9560812/change-border-of-toolstripcombobox-with-flat-style
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PropertiesToolStrip_Paint(object sender, PaintEventArgs e) {
			using (Pen cbBorderPen = new Pen(SystemColors.ActiveBorder)) {
				// Loop over all items in the propertiesToolStrip
				foreach (ToolStripItem item in propertiesToolStrip.Items) {
					ToolStripComboBox cb = item as ToolStripComboBox;
					// Only ToolStripComboBox that are visible
					if (cb == null || !cb.Visible) {
						continue;
					}
					// Calculate the rectangle
					if (cb.ComboBox != null)
					{
						Rectangle r = new Rectangle(cb.ComboBox.Location.X - 1, cb.ComboBox.Location.Y - 1, cb.ComboBox.Size.Width + 1, cb.ComboBox.Size.Height + 1);

						// Draw the rectangle
						e.Graphics.DrawRectangle(cbBorderPen, r);
					}
				}
			}
		}

		/// <summary>
		/// Get all the destinations and display them in the file menu and the buttons
		/// </summary>
		private void AddDestinations() {
			Invoke((MethodInvoker)delegate {
				// Create export buttons 
				foreach(IDestination destination in DestinationHelper.GetAllDestinations()) {
					if (destination.Priority <= 2) {
						continue;
					}
					if (!destination.IsActive) {
						continue;
					}
					if (destination.DisplayIcon == null) {
						continue;
					}
					try {
						AddDestinationButton(destination);
					} catch (Exception addingException) {
						Log.WarnFormat("Problem adding destination {0}", destination.Designation);
						Log.Warn("Exception: ", addingException);
					}
				}
			});
		}

		private void AddDestinationButton(IDestination toolstripDestination) {
			if (toolstripDestination.IsDynamic) {
				ToolStripSplitButton destinationButton = new ToolStripSplitButton
				{
					DisplayStyle = ToolStripItemDisplayStyle.Image,
					Size = new Size(23, 22),
					Text = toolstripDestination.Description,
					Image = toolstripDestination.DisplayIcon
				};
				//ToolStripDropDownButton destinationButton = new ToolStripDropDownButton();

				ToolStripMenuItem defaultItem = new ToolStripMenuItem(toolstripDestination.Description)
				{
					Tag = toolstripDestination,
					Image = toolstripDestination.DisplayIcon
				};
				defaultItem.Click += delegate {
					toolstripDestination.ExportCapture(true, _surface, _surface.CaptureDetails);
				};
				
				// The ButtonClick, this is for the icon, gets the current default item
				destinationButton.ButtonClick += delegate {
					toolstripDestination.ExportCapture(true, _surface, _surface.CaptureDetails);
				};
				
				// Generate the entries for the drop down
				destinationButton.DropDownOpening += delegate
				{
					ClearItems(destinationButton.DropDownItems);
					destinationButton.DropDownItems.Add(defaultItem);

					List<IDestination> subDestinations = new List<IDestination>();
					subDestinations.AddRange(toolstripDestination.DynamicDestinations());
					if (subDestinations.Count > 0) {
						subDestinations.Sort();
						foreach(IDestination subDestination in subDestinations) {
							IDestination closureFixedDestination = subDestination;
							ToolStripMenuItem destinationMenuItem = new ToolStripMenuItem(closureFixedDestination.Description)
							{
								Tag = closureFixedDestination,
								Image = closureFixedDestination.DisplayIcon
							};
							destinationMenuItem.Click += delegate {
								closureFixedDestination.ExportCapture(true, _surface, _surface.CaptureDetails);
							};
							destinationButton.DropDownItems.Add(destinationMenuItem);
						}
					}
				};

				destinationsToolStrip.Items.Insert(destinationsToolStrip.Items.IndexOf(toolStripSeparator16), destinationButton);
				
			} else {
				ToolStripButton destinationButton = new ToolStripButton();
				destinationsToolStrip.Items.Insert(destinationsToolStrip.Items.IndexOf(toolStripSeparator16), destinationButton);
				destinationButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
				destinationButton.Size = new Size(23, 22);
				destinationButton.Text = toolstripDestination.Description;
				destinationButton.Image = toolstripDestination.DisplayIcon;
				destinationButton.Click += delegate {
					toolstripDestination.ExportCapture(true, _surface, _surface.CaptureDetails);
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

		private void FileMenuDropDownOpening(object sender, EventArgs eventArgs) {
			ClearItems(fileStripMenuItem.DropDownItems);

			// Add the destinations
			foreach(IDestination destination in DestinationHelper.GetAllDestinations()) {
				if (IgnoreDestinations.Contains(destination.Designation)) {
					continue;
				}
				if (!destination.IsActive) {
					continue;
				}
				
				ToolStripMenuItem item = destination.GetMenuItem(true, null, DestinationToolStripMenuItemClick);
				if (item != null) {
					item.ShortcutKeys = destination.EditorShortcutKeys;
					fileStripMenuItem.DropDownItems.Add(item);
				}
			}
			// add the elements after the destinations
			fileStripMenuItem.DropDownItems.Add(toolStripSeparator9);
			fileStripMenuItem.DropDownItems.Add(closeToolStripMenuItem);
		}

		private delegate void SurfaceMessageReceivedThreadSafeDelegate(object sender, SurfaceMessageEventArgs eventArgs);
		/// <summary>
		/// This is the SufraceMessageEvent receiver which display a message in the status bar if the
		/// surface is exported. It also updates the title to represent the filename, if there is one.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void SurfaceMessageReceived(object sender, SurfaceMessageEventArgs eventArgs) {
			if (InvokeRequired) {
				Invoke(new SurfaceMessageReceivedThreadSafeDelegate(SurfaceMessageReceived), sender, eventArgs);
			} else {
				string dateTime = DateTime.Now.ToLongTimeString();
				// TODO: Fix that we only open files, like in the tooltip
				switch (eventArgs.MessageType) {
					case SurfaceMessageTyp.FileSaved:
						// Put the event message on the status label and attach the context menu
						UpdateStatusLabel(dateTime + " - " + eventArgs.Message, fileSavedStatusContextMenu);
						// Change title
						Text = eventArgs.Surface.LastSaveFullPath + " - " + Language.GetString(LangKey.editor_title);
						break;
					default:
						// Put the event message on the status label
						UpdateStatusLabel(dateTime + " - " + eventArgs.Message);
						break;
				}
			}
		}

		/// <summary>
		/// This is called when the size of the surface chances, used for resizing and displaying the size information
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SurfaceSizeChanged(object sender, EventArgs e) {
			if (EditorConfiguration.MatchSizeToCapture) {
				// Set editor's initial size to the size of the surface plus the size of the chrome
				Size imageSize = Surface.Image.Size;
				Size currentFormSize = Size;
				Size currentImageClientSize = panel1.ClientSize;
				int minimumFormWidth = 650;
				int minimumFormHeight = 530;
				int newWidth = Math.Max(minimumFormWidth, currentFormSize.Width - currentImageClientSize.Width + imageSize.Width);
				int newHeight = Math.Max(minimumFormHeight, currentFormSize.Height - currentImageClientSize.Height + imageSize.Height);
				Size = new Size(newWidth, newHeight);
			}
			dimensionsLabel.Text = Surface.Image.Width + "x" + Surface.Image.Height;
			ImageEditorFormResize(sender, new EventArgs());
		}

		public ISurface Surface {
			get {
				return _surface;
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
			_surface.LastSaveFullPath = fullpath;

			if (fullpath == null) {
				return;
			}
			UpdateStatusLabel(Language.GetFormattedString(LangKey.editor_imagesaved, fullpath), fileSavedStatusContextMenu);
			Text = Path.GetFileName(fullpath) + " - " + Language.GetString(LangKey.editor_title);
		}

		private void surface_DrawingModeChanged(object source, SurfaceDrawingModeEventArgs eventArgs) {
			switch (eventArgs.DrawingMode) {
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
				case DrawingModes.SpeechBubble:
					SetButtonChecked(btnSpeechBubble);
					break;
				case DrawingModes.StepLabel:
					SetButtonChecked(btnStepLabel);
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
			return _surface.GetImageForExport();
		}
		
		public ICaptureDetails CaptureDetails => _surface.CaptureDetails;

		public ToolStripMenuItem GetPluginMenuItem() {
			return pluginToolStripMenuItem;
		}

		public ToolStripMenuItem GetFileMenuItem() {
			return fileStripMenuItem;
		}
		#endregion
		
		#region filesystem options

		private void BtnSaveClick(object sender, EventArgs e) {
			string destinationDesignation = FileDestination.DESIGNATION;
			if (_surface.LastSaveFullPath == null) {
				destinationDesignation = FileWithDialogDestination.DESIGNATION;
			}
			DestinationHelper.ExportCapture(true, destinationDesignation, _surface, _surface.CaptureDetails);
		}

		private void BtnClipboardClick(object sender, EventArgs e) {
			DestinationHelper.ExportCapture(true, ClipboardDestination.DESIGNATION, _surface, _surface.CaptureDetails);
		}

		private void BtnPrintClick(object sender, EventArgs e) {
			// The BeginInvoke is a solution for the printdialog not having focus
			BeginInvoke((MethodInvoker) delegate {
				DestinationHelper.ExportCapture(true, PrinterDestination.DESIGNATION, _surface, _surface.CaptureDetails);
			});
		}

		private void CloseToolStripMenuItemClick(object sender, EventArgs e) {
			Close();
		}
		#endregion
		
		#region drawing options

		private void BtnEllipseClick(object sender, EventArgs e) {
			_surface.DrawingMode = DrawingModes.Ellipse;
			RefreshFieldControls();
		}

		private void BtnCursorClick(object sender, EventArgs e) {
			_surface.DrawingMode = DrawingModes.None;
			RefreshFieldControls();
		}

		private void BtnRectClick(object sender, EventArgs e) {
			_surface.DrawingMode = DrawingModes.Rect;
			RefreshFieldControls();
		}

		private void BtnTextClick(object sender, EventArgs e) {
			_surface.DrawingMode = DrawingModes.Text;
			RefreshFieldControls();
		}

		private void BtnSpeechBubbleClick(object sender, EventArgs e) {
			_surface.DrawingMode = DrawingModes.SpeechBubble;
			RefreshFieldControls();
		}

		private void BtnStepLabelClick(object sender, EventArgs e) {
			_surface.DrawingMode = DrawingModes.StepLabel;
			RefreshFieldControls();
		}

		private void BtnLineClick(object sender, EventArgs e) {
			_surface.DrawingMode = DrawingModes.Line;
			RefreshFieldControls();
		}

		private void BtnArrowClick(object sender, EventArgs e) {
			_surface.DrawingMode = DrawingModes.Arrow;
			RefreshFieldControls();
		}

		private void BtnCropClick(object sender, EventArgs e) {
			_surface.DrawingMode = DrawingModes.Crop;
			RefreshFieldControls();
		}

		private void BtnHighlightClick(object sender, EventArgs e) {
			_surface.DrawingMode = DrawingModes.Highlight;
			RefreshFieldControls();
		}

		private void BtnObfuscateClick(object sender, EventArgs e) {
			_surface.DrawingMode = DrawingModes.Obfuscate;
			RefreshFieldControls();
		}

		private void BtnFreehandClick(object sender, EventArgs e) {
			_surface.DrawingMode = DrawingModes.Path;
			RefreshFieldControls();
		}

		private void SetButtonChecked(ToolStripButton btn) {
			UncheckAllToolButtons();
			btn.Checked = true;
		}
		
		private void UncheckAllToolButtons() {
			if (_toolbarButtons != null) {
				foreach (GreenshotToolStripButton butt in _toolbarButtons) {
					butt.Checked = false;
				}
			}
		}

		private void AddRectangleToolStripMenuItemClick(object sender, EventArgs e) {
			BtnRectClick(sender, e);
		}

		private void DrawFreehandToolStripMenuItemClick(object sender, EventArgs e) {
			BtnFreehandClick(sender, e);
		}

		private void AddEllipseToolStripMenuItemClick(object sender, EventArgs e) {
			BtnEllipseClick(sender, e);
		}

		private void AddTextBoxToolStripMenuItemClick(object sender, EventArgs e) {
			BtnTextClick(sender, e);
		}

		private void AddSpeechBubbleToolStripMenuItemClick(object sender, EventArgs e) {
			BtnSpeechBubbleClick(sender, e);
		}

		private void AddCounterToolStripMenuItemClick(object sender, EventArgs e) {
			BtnStepLabelClick(sender, e);
		}

		private void DrawLineToolStripMenuItemClick(object sender, EventArgs e) {
			BtnLineClick(sender, e);
		}

		private void DrawArrowToolStripMenuItemClick(object sender, EventArgs e) {
			BtnArrowClick(sender, e);
		}

		private void RemoveObjectToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.RemoveSelectedElements();
		}

		private void BtnDeleteClick(object sender, EventArgs e) {
			RemoveObjectToolStripMenuItemClick(sender, e);
		}
		#endregion
		
		#region copy&paste options

		private void CutToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.CutSelectedElements();
			UpdateClipboardSurfaceDependencies();
		}

		private void BtnCutClick(object sender, EventArgs e) {
			CutToolStripMenuItemClick(sender, e);
		}

		private void CopyToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.CopySelectedElements();
			UpdateClipboardSurfaceDependencies();
		}

		private void BtnCopyClick(object sender, EventArgs e) {
			CopyToolStripMenuItemClick(sender, e);
		}

		private void PasteToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.PasteElementFromClipboard();
			UpdateClipboardSurfaceDependencies();
		}

		private void BtnPasteClick(object sender, EventArgs e) {
			PasteToolStripMenuItemClick(sender, e);
		}

		private void UndoToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.Undo();
			UpdateUndoRedoSurfaceDependencies();
		}

		private void BtnUndoClick(object sender, EventArgs e) {
			UndoToolStripMenuItemClick(sender, e);
		}

		private void RedoToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.Redo();
			UpdateUndoRedoSurfaceDependencies();
		}

		private void BtnRedoClick(object sender, EventArgs e) {
			RedoToolStripMenuItemClick(sender, e);
		}

		private void DuplicateToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.DuplicateSelectedElements();
			UpdateClipboardSurfaceDependencies();
		}
		#endregion
		
		#region element properties

		private void UpOneLevelToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.PullElementsUp();
		}

		private void DownOneLevelToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.PushElementsDown();
		}

		private void UpToTopToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.PullElementsToTop();
		}

		private void DownToBottomToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.PushElementsToBottom();
		}
		
		
		#endregion
		
		#region help

		private void HelpToolStripMenuItem1Click(object sender, EventArgs e) {
			HelpFileLoader.LoadHelp();
		}

		private void AboutToolStripMenuItemClick(object sender, EventArgs e) {
			MainForm.Instance.ShowAbout();
		}

		private void PreferencesToolStripMenuItemClick(object sender, EventArgs e) {
			MainForm.Instance.ShowSetting();
		}

		private void BtnSettingsClick(object sender, EventArgs e) {
			PreferencesToolStripMenuItemClick(sender, e);
		}

		private void BtnHelpClick(object sender, EventArgs e) {
			HelpToolStripMenuItem1Click(sender, e);
		}
		#endregion
		
		#region image editor event handlers

		private void ImageEditorFormActivated(object sender, EventArgs e) {
			UpdateClipboardSurfaceDependencies();
			UpdateUndoRedoSurfaceDependencies();
		}

		private void ImageEditorFormFormClosing(object sender, FormClosingEventArgs e) {
			if (_surface.Modified && !EditorConfiguration.SuppressSaveDialogAtClose) {
				// Make sure the editor is visible
				WindowDetails.ToForeground(Handle);

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
					if (_surface.Modified) {
						e.Cancel = true;
						return;
					}
				}
			}
			// persist our geometry string.
			EditorConfiguration.SetEditorPlacement(new WindowDetails(Handle).WindowPlacement);
			IniConfig.Save();
			
			// remove from the editor list
			EditorList.Remove(this);

			_surface.Dispose();

			GC.Collect();
			if (coreConfiguration.MinimizeWorkingSetSize) {
				PsAPI.EmptyWorkingSet();
			}
		}

		private void ImageEditorFormKeyDown(object sender, KeyEventArgs e) {
			// LOG.Debug("Got key event "+e.KeyCode + ", " + e.Modifiers);
			// avoid conflict with other shortcuts and
			// make sure there's no selected element claiming input focus
			if(e.Modifiers.Equals(Keys.None) && !_surface.KeysLocked) {
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
					case Keys.S:
						BtnSpeechBubbleClick(sender, e);
						break;
					case Keys.I:
						BtnStepLabelClick(sender, e);
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
				}
			} else if (e.Modifiers.Equals(Keys.Control)) {
				switch (e.KeyCode) {
					case Keys.Z:
						UndoToolStripMenuItemClick(sender, e);
						break;
					case Keys.Y:
						RedoToolStripMenuItemClick(sender, e);
						break;
					case Keys.Q:	// Dropshadow Ctrl + Q
						AddDropshadowToolStripMenuItemMouseUp(sender, new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0));
						break;
					case Keys.B:	// Border Ctrl + B
						AddBorderToolStripMenuItemClick(sender, e);
						break;
					case Keys.T:	// Torn edge Ctrl + T
						TornEdgesToolStripMenuItemMouseUp(sender, new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0));
						break;
					case Keys.I:	// Invert Ctrl + I
						InvertToolStripMenuItemClick(sender, e);
						break;
					case Keys.G:	// Grayscale Ctrl + G
						GrayscaleToolStripMenuItemClick(sender, e);
						break;
					case Keys.Delete:	// Grayscale Ctrl + Delete
						ClearToolStripMenuItemClick(sender, e);
						break;
					case Keys.Oemcomma:	// Rotate CCW Ctrl + ,
						RotateCcwToolstripButtonClick(sender, e);
						break;
					case Keys.OemPeriod:    // Rotate CW Ctrl + .
						RotateCwToolstripButtonClick(sender, e);
						break;
					case Keys.Add:    // Ctrl + +
					case Keys.Oemplus:    // Ctrl + +
						EnlargeCanvasToolStripMenuItemClick(sender, e);
						break;
					case Keys.Subtract:    // Ctrl + -
					case Keys.OemMinus:    // Ctrl + -
						ShrinkCanvasToolStripMenuItemClick(sender, e);
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
			if (!_surface.KeysLocked) {
				return base.ProcessKeyPreview(ref msg);
			}
			return false;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keys) {
			// disable default key handling if surface has requested a lock
			if (!_surface.KeysLocked) {

				// Go through the destinations to check the EditorShortcut Keys
				// this way the menu entries don't need to be enabled.
				// This also fixes bugs #3526974 & #3527020
				foreach (IDestination destination in DestinationHelper.GetAllDestinations()) {
					if (IgnoreDestinations.Contains(destination.Designation)) {
						continue;
					}
					if (!destination.IsActive) {
						continue;
					}

					if (destination.EditorShortcutKeys == keys) {
						destination.ExportCapture(true, _surface, _surface.CaptureDetails);
						return true;
					}
				}
				if (!_surface.ProcessCmdKey(keys)) {
					return base.ProcessCmdKey(ref msg, keys);
				}
			}
			return false;
		}
		#endregion
		
		#region helpers
		
		private void UpdateUndoRedoSurfaceDependencies() {
			if (_surface == null) {
				return;
			}
			bool canUndo = _surface.CanUndo;
			btnUndo.Enabled = canUndo;
			undoToolStripMenuItem.Enabled = canUndo;
			string undoAction = "";
			if (canUndo) {
				if (_surface.UndoActionLanguageKey != LangKey.none) {
					undoAction = Language.GetString(_surface.UndoActionLanguageKey);
				}
			}
			string undoText = Language.GetFormattedString(LangKey.editor_undo, undoAction);
			btnUndo.Text = undoText;
			undoToolStripMenuItem.Text = undoText;

			bool canRedo = _surface.CanRedo;
			btnRedo.Enabled = canRedo;
			redoToolStripMenuItem.Enabled = canRedo;
			string redoAction = "";
			if (canRedo) {
				if (_surface.RedoActionLanguageKey != LangKey.none) {
					redoAction = Language.GetString(_surface.RedoActionLanguageKey);
				}
			}
			string redoText = Language.GetFormattedString(LangKey.editor_redo, redoAction);
			btnRedo.Text = redoText;
			redoToolStripMenuItem.Text = redoText;

		}

		private void UpdateClipboardSurfaceDependencies() {
			if (_surface == null) {
				return;
			}
			// check dependencies for the Surface
			bool hasItems = _surface.HasSelectedElements;
			bool actionAllowedForSelection = hasItems && !_controlsDisabledDueToConfirmable;
			
			// buttons
			btnCut.Enabled = actionAllowedForSelection;
			btnCopy.Enabled = actionAllowedForSelection;
			btnDelete.Enabled = actionAllowedForSelection;

			// menus
			removeObjectToolStripMenuItem.Enabled = actionAllowedForSelection;
			copyToolStripMenuItem.Enabled = actionAllowedForSelection;
			cutToolStripMenuItem.Enabled = actionAllowedForSelection;
			duplicateToolStripMenuItem.Enabled = actionAllowedForSelection;

			// check dependencies for the Clipboard
			bool hasClipboard = ClipboardHelper.ContainsFormat(SupportedClipboardFormats) || ClipboardHelper.ContainsImage();
			btnPaste.Enabled = hasClipboard && !_controlsDisabledDueToConfirmable;
			pasteToolStripMenuItem.Enabled = hasClipboard && !_controlsDisabledDueToConfirmable;
		}

		#endregion
		
		#region status label handling
		private void UpdateStatusLabel(string text, ContextMenuStrip contextMenu = null) {
			statusLabel.Text = text;
			statusStrip1.ContextMenuStrip = contextMenu;
		}

		private void ClearStatusLabel() {
			UpdateStatusLabel(null);
		}

		private void StatusLabelClicked(object sender, MouseEventArgs e) {
			ToolStrip ss = (StatusStrip)((ToolStripStatusLabel)sender).Owner;
			ss.ContextMenuStrip?.Show(ss, e.X, e.Y);
		}

		private void CopyPathMenuItemClick(object sender, EventArgs e) {
			ClipboardHelper.SetClipboardData(_surface.LastSaveFullPath);
		}

		private void OpenDirectoryMenuItemClick(object sender, EventArgs e) {
			ExplorerHelper.OpenInExplorer(_surface.LastSaveFullPath);
		}
		#endregion
		
		private void BindFieldControls() {
			// TODO: This is actually risky, if there are no references than the objects may be garbage collected
			new BidirectionalBinding(btnFillColor, "SelectedColor", _surface.FieldAggregator.GetField(FieldType.FILL_COLOR), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(btnLineColor, "SelectedColor", _surface.FieldAggregator.GetField(FieldType.LINE_COLOR), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(lineThicknessUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.LINE_THICKNESS), "Value", DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(blurRadiusUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.BLUR_RADIUS), "Value", DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(magnificationFactorUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.MAGNIFICATION_FACTOR), "Value", DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(pixelSizeUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.PIXEL_SIZE), "Value", DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(brightnessUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.BRIGHTNESS), "Value", DecimalDoublePercentageConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(fontFamilyComboBox, "Text", _surface.FieldAggregator.GetField(FieldType.FONT_FAMILY), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(fontSizeUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.FONT_SIZE), "Value", DecimalFloatConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(fontBoldButton, "Checked", _surface.FieldAggregator.GetField(FieldType.FONT_BOLD), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(fontItalicButton, "Checked", _surface.FieldAggregator.GetField(FieldType.FONT_ITALIC), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(textHorizontalAlignmentButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.TEXT_HORIZONTAL_ALIGNMENT), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(textVerticalAlignmentButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.TEXT_VERTICAL_ALIGNMENT), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(shadowButton, "Checked", _surface.FieldAggregator.GetField(FieldType.SHADOW), "Value", NotNullValidator.GetInstance());
			new BidirectionalBinding(previewQualityUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.PREVIEW_QUALITY), "Value", DecimalDoublePercentageConverter.GetInstance(), NotNullValidator.GetInstance());
			new BidirectionalBinding(obfuscateModeButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.PREPARED_FILTER_OBFUSCATE), "Value");
			new BidirectionalBinding(highlightModeButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.PREPARED_FILTER_HIGHLIGHT), "Value");
			new BidirectionalBinding(counterUpDown, "Value", _surface, "CounterStart", DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
		}

		/// <summary>
		/// shows/hides field controls (2nd toolbar on top) depending on fields of selected elements
		/// </summary>
		private void RefreshFieldControls() {
			propertiesToolStrip.SuspendLayout();
			if(_surface.HasSelectedElements || _surface.DrawingMode != DrawingModes.None) {
				FieldAggregator props = _surface.FieldAggregator;
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
				textHorizontalAlignmentButton.Visible = props.HasFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT);
				textVerticalAlignmentButton.Visible = props.HasFieldValue(FieldType.TEXT_VERTICAL_ALIGNMENT);
				shadowButton.Visible = props.HasFieldValue(FieldType.SHADOW);
				counterLabel.Visible = counterUpDown.Visible = props.HasFieldValue(FieldType.FLAGS)
					&& ((FieldFlag)props.GetFieldValue(FieldType.FLAGS) & FieldFlag.COUNTER) == FieldFlag.COUNTER;
				btnConfirm.Visible = btnCancel.Visible = props.HasFieldValue(FieldType.FLAGS)
					&& ((FieldFlag)props.GetFieldValue(FieldType.FLAGS) & FieldFlag.CONFIRMABLE) == FieldFlag.CONFIRMABLE;

				obfuscateModeButton.Visible = props.HasFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE);
				highlightModeButton.Visible = props.HasFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT);
			} else {
				HideToolstripItems();
			}
			propertiesToolStrip.ResumeLayout();
		}
		
		private void HideToolstripItems() {
			foreach(ToolStripItem toolStripItem in propertiesToolStrip.Items) {
				toolStripItem.Visible = false;
			}
		}
		
		/// <summary>
		/// refreshes all editor controls depending on selected elements and their fields
		/// </summary>
		private void RefreshEditorControls() {
			int stepLabels = _surface.CountStepLabels(null);
			Image icon;
			if (stepLabels <= 20) {
				icon = (Image)resources.GetObject($"btnStepLabel{stepLabels:00}.Image");
			} else {
				icon = (Image)resources.GetObject("btnStepLabel20+.Image");
			}
			btnStepLabel.Image = icon;
			addCounterToolStripMenuItem.Image = icon;

			FieldAggregator props = _surface.FieldAggregator;
			// if a confirmable element is selected, we must disable most of the controls
			// since we demand confirmation or cancel for confirmable element
			if (props.HasFieldValue(FieldType.FLAGS) && ((FieldFlag)props.GetFieldValue(FieldType.FLAGS) & FieldFlag.CONFIRMABLE) == FieldFlag.CONFIRMABLE)
			{
				// disable most controls
				if (!_controlsDisabledDueToConfirmable) {
					ToolStripItemEndisabler.Disable(menuStrip1);
					ToolStripItemEndisabler.Disable(destinationsToolStrip);
					ToolStripItemEndisabler.Disable(toolsToolStrip);
					ToolStripItemEndisabler.Enable(closeToolStripMenuItem);
					ToolStripItemEndisabler.Enable(helpToolStripMenuItem);
					ToolStripItemEndisabler.Enable(aboutToolStripMenuItem);
					ToolStripItemEndisabler.Enable(preferencesToolStripMenuItem);
					_controlsDisabledDueToConfirmable = true;
				}
			} else if(_controlsDisabledDueToConfirmable) {
				// re-enable disabled controls, confirmable element has either been confirmed or cancelled
				ToolStripItemEndisabler.Enable(menuStrip1);
				ToolStripItemEndisabler.Enable(destinationsToolStrip);
				ToolStripItemEndisabler.Enable(toolsToolStrip);
				_controlsDisabledDueToConfirmable = false;
			}
			
			// en/disable controls depending on whether an element is selected at all
			UpdateClipboardSurfaceDependencies();
			UpdateUndoRedoSurfaceDependencies();
			
			// en/disablearrage controls depending on hierarchy of selected elements
			bool actionAllowedForSelection = _surface.HasSelectedElements && !_controlsDisabledDueToConfirmable;
			bool push = actionAllowedForSelection && _surface.CanPushSelectionDown();
			bool pull = actionAllowedForSelection && _surface.CanPullSelectionUp();
			arrangeToolStripMenuItem.Enabled = push || pull;
			if (arrangeToolStripMenuItem.Enabled) {
				upToTopToolStripMenuItem.Enabled = pull;
				upOneLevelToolStripMenuItem.Enabled = pull;
				downToBottomToolStripMenuItem.Enabled = push;
				downOneLevelToolStripMenuItem.Enabled = push;
			}
			
			// finally show/hide field controls depending on the fields of selected elements
			RefreshFieldControls();
		}


		private void ArrowHeadsToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.FieldAggregator.GetField(FieldType.ARROWHEADS).Value = (ArrowContainer.ArrowHeadCombination)((ToolStripMenuItem)sender).Tag;
		}

		private void EditToolStripMenuItemClick(object sender, EventArgs e) {
			UpdateClipboardSurfaceDependencies();
			UpdateUndoRedoSurfaceDependencies();
		}

		private void FontPropertyChanged(object sender, EventArgs e) {
			// in case we forced another FontStyle before, reset it first.
			if (fontBoldButton != null && _originalBoldCheckState != fontBoldButton.Checked)
			{
				fontBoldButton.Checked = _originalBoldCheckState;
			}
			if (fontItalicButton != null && _originalItalicCheckState != fontItalicButton.Checked)
			{
				fontItalicButton.Checked = _originalItalicCheckState;
			}
			
			var fontFamily = fontFamilyComboBox.FontFamily;
		   
			bool boldAvailable = fontFamily.IsStyleAvailable(FontStyle.Bold);
			if (fontBoldButton != null)
			{
				if (!boldAvailable)
				{
					_originalBoldCheckState = fontBoldButton.Checked;
					fontBoldButton.Checked = false;
				}
				fontBoldButton.Enabled = boldAvailable;
			}

			bool italicAvailable = fontFamily.IsStyleAvailable(FontStyle.Italic);
			if (fontItalicButton != null)
			{
				if (!italicAvailable)
				{
					fontItalicButton.Checked = false;
				}
				fontItalicButton.Enabled = italicAvailable;
			}

			bool regularAvailable = fontFamily.IsStyleAvailable(FontStyle.Regular);
			if (regularAvailable)
			{
				return;
			}
			if (boldAvailable) {
				if (fontBoldButton != null)
				{
					fontBoldButton.Checked = true;
				}
			} else if(italicAvailable) {
				if (fontItalicButton != null)
				{
					fontItalicButton.Checked = true;
				}
			}
		}

		private void FieldAggregatorFieldChanged(object sender, FieldChangedEventArgs e) {
			// in addition to selection, deselection of elements, we need to
			// refresh toolbar if prepared filter mode is changed
			if(Equals(e.Field.FieldType, FieldType.PREPARED_FILTER_HIGHLIGHT)) {
				RefreshFieldControls();
			}
		}

		private void FontBoldButtonClick(object sender, EventArgs e) {
			_originalBoldCheckState = fontBoldButton.Checked;
		}

		private void FontItalicButtonClick(object sender, EventArgs e) {
			_originalItalicCheckState = fontItalicButton.Checked;
		}

		private void ToolBarFocusableElementGotFocus(object sender, EventArgs e) {
			_surface.KeysLocked = true;
		}

		private void ToolBarFocusableElementLostFocus(object sender, EventArgs e) {
			_surface.KeysLocked = false;
		}

		private void SaveElementsToolStripMenuItemClick(object sender, EventArgs e) {
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "Greenshot templates (*.gst)|*.gst",
				FileName = FilenameHelper.GetFilenameWithoutExtensionFromPattern(coreConfiguration.OutputFileFilenamePattern, _surface.CaptureDetails)
			};
			DialogResult dialogResult = saveFileDialog.ShowDialog();
			if(dialogResult.Equals(DialogResult.OK)) {
				using (Stream streamWrite = File.OpenWrite(saveFileDialog.FileName)) {
					_surface.SaveElementsToStream(streamWrite);
				}
			}
		}

		private void LoadElementsToolStripMenuItemClick(object sender, EventArgs e) {
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Greenshot templates (*.gst)|*.gst"
			};
			if (openFileDialog.ShowDialog() == DialogResult.OK) {
				using (Stream streamRead = File.OpenRead(openFileDialog.FileName)) {
					_surface.LoadElementsFromStream(streamRead);
				}
				_surface.Refresh();
			}
		}

		private void DestinationToolStripMenuItemClick(object sender, EventArgs e) {
			IDestination clickedDestination = null;
			var control = sender as Control;
			if (control != null) {
				Control clickedControl = control;
				if (clickedControl.ContextMenuStrip != null) {
					clickedControl.ContextMenuStrip.Show(Cursor.Position);
					return;
				}
				clickedDestination = (IDestination)clickedControl.Tag;
			}
			else
			{
				var item = sender as ToolStripMenuItem;
				if (item != null) {
					ToolStripMenuItem clickedMenuItem = item;
					clickedDestination = (IDestination)clickedMenuItem.Tag;
				}
			}
			ExportInformation exportInformation = clickedDestination?.ExportCapture(true, _surface, _surface.CaptureDetails);
			if (exportInformation != null && exportInformation.ExportMade) {
				_surface.Modified = false;
			}
		}
		
		protected void FilterPresetDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
			RefreshFieldControls();
			Invalidate(true);
		}

		private void SelectAllToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.SelectAllElements();
		}


		private void BtnConfirmClick(object sender, EventArgs e) {
			_surface.ConfirmSelectedConfirmableElements(true);
			RefreshFieldControls();
		}

		private void BtnCancelClick(object sender, EventArgs e) {
			_surface.ConfirmSelectedConfirmableElements(false);
			RefreshFieldControls();
		}

		private void Insert_window_toolstripmenuitemMouseEnter(object sender, EventArgs e) {
			ToolStripMenuItem captureWindowMenuItem = (ToolStripMenuItem)sender;
			MainForm.Instance.AddCaptureWindowMenuItems(captureWindowMenuItem, Contextmenu_window_Click);	
		}

		private void Contextmenu_window_Click(object sender, EventArgs e) {
			ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
			try {
				WindowDetails windowToCapture = (WindowDetails)clickedItem.Tag;
				ICapture capture = new Capture();
				using (Graphics graphics = Graphics.FromHwnd(Handle)) {
					capture.CaptureDetails.DpiX = graphics.DpiY;
					capture.CaptureDetails.DpiY = graphics.DpiY;
				}
				windowToCapture = CaptureHelper.SelectCaptureWindow(windowToCapture);
				if (windowToCapture != null) {
					capture = CaptureHelper.CaptureWindow(windowToCapture, capture, coreConfiguration.WindowCaptureMode);
					if (capture?.CaptureDetails != null && capture.Image != null) {
						((Bitmap)capture.Image).SetResolution(capture.CaptureDetails.DpiX, capture.CaptureDetails.DpiY);
						_surface.AddImageContainer((Bitmap)capture.Image, 100, 100);
					}
					Activate();
					WindowDetails.ToForeground(Handle);
				}

				capture?.Dispose();
			} catch (Exception exception) {
				Log.Error(exception);
			}
		}

		private void AutoCropToolStripMenuItemClick(object sender, EventArgs e) {
			if (_surface.AutoCrop()) {
				RefreshFieldControls();
			}
		}

		private void AddBorderToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.ApplyBitmapEffect(new BorderEffect());
			UpdateUndoRedoSurfaceDependencies();
		}

		/// <summary>
		/// Added for FEATURE-919, increasing the canvas by 25 pixels in every direction.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void EnlargeCanvasToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.ApplyBitmapEffect(new ResizeCanvasEffect(25, 25, 25, 25));
			UpdateUndoRedoSurfaceDependencies();
		}

		/// <summary>
		/// Added for FEATURE-919, to make the capture as small as possible again.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ShrinkCanvasToolStripMenuItemClick(object sender, EventArgs e)
		{
			Rectangle cropRectangle;
			using (Image tmpImage = GetImageForExport())
			{
				cropRectangle = ImageHelper.FindAutoCropRectangle(tmpImage, coreConfiguration.AutoCropDifference);
			}
			if (_surface.IsCropPossible(ref cropRectangle))
			{
				_surface.ApplyCrop(cropRectangle);
				UpdateUndoRedoSurfaceDependencies();
			}
		}

		/// <summary>
		/// This is used when the dropshadow button is used
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e">MouseEventArgs</param>
		private void AddDropshadowToolStripMenuItemMouseUp(object sender, MouseEventArgs e)
		{
			var dropShadowEffect = EditorConfiguration.DropShadowEffectSettings;
			bool apply;
			switch (e.Button)
			{
				case MouseButtons.Left:
					apply = true;
					break;
				case MouseButtons.Right:
					var result = new DropShadowSettingsForm(dropShadowEffect).ShowDialog(this);
					apply = result == DialogResult.OK;
					break;
				default:
					return;
			}
		
			if (apply)
			{
				_surface.ApplyBitmapEffect(dropShadowEffect);
				UpdateUndoRedoSurfaceDependencies();
			}
		}


		/// <summary>
		/// Open the resize settings from, and resize if ok was pressed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnResizeClick(object sender, EventArgs e) {
			var resizeEffect = new ResizeEffect(_surface.Image.Width, _surface.Image.Height, true);
			var result = new ResizeSettingsForm(resizeEffect).ShowDialog(this);
			if (result == DialogResult.OK) {
				_surface.ApplyBitmapEffect(resizeEffect);
				UpdateUndoRedoSurfaceDependencies();
			}
		}

		/// <summary>
		/// This is used when the torn-edge button is used
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e">MouseEventArgs</param>
		private void TornEdgesToolStripMenuItemMouseUp(object sender, MouseEventArgs e)
		{
			var tornEdgeEffect = EditorConfiguration.TornEdgeEffectSettings;
			bool apply;
			switch (e.Button)
			{
				case MouseButtons.Left:
					apply = true;
					break;
				case MouseButtons.Right:
					var result = new TornEdgeSettingsForm(tornEdgeEffect).ShowDialog(this);
					apply = result == DialogResult.OK;
					break;
				default:
					return;
			}

			if (apply)
			{
				_surface.ApplyBitmapEffect(tornEdgeEffect);
				UpdateUndoRedoSurfaceDependencies();
			}
		}

		private void GrayscaleToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.ApplyBitmapEffect(new GrayscaleEffect());
			UpdateUndoRedoSurfaceDependencies();
		}

		private void ClearToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.Clear(Color.Transparent);
			UpdateUndoRedoSurfaceDependencies();
		}

		private void RotateCwToolstripButtonClick(object sender, EventArgs e) {
			_surface.ApplyBitmapEffect(new RotateEffect(90));
			UpdateUndoRedoSurfaceDependencies();
		}

		private void RotateCcwToolstripButtonClick(object sender, EventArgs e) {
			_surface.ApplyBitmapEffect(new RotateEffect(270));
			UpdateUndoRedoSurfaceDependencies();
		}

		private void InvertToolStripMenuItemClick(object sender, EventArgs e) {
			_surface.ApplyBitmapEffect(new InvertEffect());
			UpdateUndoRedoSurfaceDependencies();
		}

		private void ImageEditorFormResize(object sender, EventArgs e) {
			if (Surface?.Image == null || panel1 == null) {
				return;
			}
			Size imageSize = Surface.Image.Size;
			Size currentClientSize = panel1.ClientSize;
			var canvas = Surface as Control;
			Panel panel = (Panel) canvas?.Parent;
			if (panel == null) {
				return;
			}
			int offsetX = -panel.HorizontalScroll.Value;
			int offsetY = -panel.VerticalScroll.Value;
			if (currentClientSize.Width > imageSize.Width) {
				canvas.Left = offsetX + (currentClientSize.Width - imageSize.Width) / 2;
			} else {
				canvas.Left = offsetX + 0;
			}
			if (currentClientSize.Height > imageSize.Height) {
				canvas.Top = offsetY + (currentClientSize.Height - imageSize.Height) / 2;
			} else {
				canvas.Top = offsetY + 0;
			}
		}

		private void titleToolStripMenuItem_Click(object sender, EventArgs e) {
			var dialog = new TitleDialog {Title = Text};
			if (dialog.ShowDialog() == DialogResult.OK) {
				Text = dialog.Title;
			}
		}
	}
}
