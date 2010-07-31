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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

using Greenshot.Capturing;
using Greenshot.Configuration;
using Greenshot.Drawing;
using Greenshot.Drawing.Fields;
using Greenshot.Drawing.Fields.Binding;
//using Greenshot.Help;
using Greenshot.Helpers;
using Greenshot.Plugin;
using Greenshot.Core;

namespace Greenshot.Forms {
	/// <summary>
	/// Description of ImageEditorForm.
	/// </summary>
	public partial class ImageEditorForm : Form, IImageEditor {
		
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImageEditorForm));

		private AppConfig conf = AppConfig.GetInstance();
		private ILanguage lang;
		
		private string lastSaveFullPath;
		
		public Surface surface;
		private System.Windows.Forms.ToolStripButton[] toolbarButtons;
		
		private static string[] SUPPORTED_CLIPBOARD_FORMATS = {typeof(string).FullName, "Text", "DeviceIndependentBitmap", "Bitmap", typeof(DrawableContainerList).FullName};

		private bool originalBoldCheckState = false;
		private bool originalItalicCheckState = false;
		
		// Holds state if the image is saved
		private bool saved;
		
		// whether part of the editor controls are disabled depending on selected item(s)
		private bool controlsDisabledDueToConfirmable = false;

		/// <summary>
		/// An Implementation for the IImageEditor, this way Plugins have access to the HWND handles wich can be used with Win32 API calls.
		/// </summary>
		public IWin32Window WindowHandle {
			get { return this; }
		}

		public ImageEditorForm(Surface surface, bool outputMade) {
			
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();

			// Restore to previous location
			FormHelper.RestoreGeometry(this, conf.Editor_WindowSize, conf.Editor_WindowLocation, conf.Editor_WindowState, conf.Editor_Previous_Screenbounds);

			// Intial "saved" flag for asking if the image needs to be save
			saved = outputMade;

			// resizing the panel is futile, since it is docked. however, it seems
			// to fix the bug (?) with the vscrollbar not being able to shrink to
			// a smaller size than the initial panel size (as set by the forms designer)
			panel1.Height = 10;
			
			// init surface
			this.surface = surface;
			surface.TabStop = false;
			
			surface.MovingElementChanged += delegate { refreshEditorControls(); };
			surface.Invalidated += new InvalidateEventHandler(InvalidatedSurface);
			surface.DrawingModeChanged += new SurfaceDrawingModeEventHandler(surface_DrawingModeChanged);

			this.fontFamilyComboBox.PropertyChanged += new PropertyChangedEventHandler(FontPropertyChanged);
			
			surface.FieldAggregator.FieldChanged += new FieldChangedEventHandler( FieldAggregatorFieldChanged );
			obfuscateModeButton.DropDownItemClicked += FilterPresetDropDownItemClicked;
			highlightModeButton.DropDownItemClicked += FilterPresetDropDownItemClicked;
			
			panel1.Controls.Add(surface);
			
			lang = Language.GetInstance();
			updateUI();
			
			bindFieldControls();
			refreshEditorControls();
			
			toolbarButtons = new ToolStripButton[]{btnCursor,btnRect,btnEllipse,btnText,btnLine,btnArrow,btnHighlight, btnObfuscate, btnCrop};
			//toolbarDropDownButtons = new ToolStripDropDownButton[]{btnBlur, btnPixeliate, btnTextHighlighter, btnAreaHighlighter, btnMagnifier};
			
			PluginHelper.instance.CreateImageEditorOpenEvent(this);
			pluginToolStripMenuItem.Visible = pluginToolStripMenuItem.DropDownItems.Count > 0;
			
			emailToolStripMenuItem.Enabled = MapiMailMessage.HasMAPI();
		}

		private void InvalidatedSurface(object sender, InvalidateEventArgs eventArgs) {
			saved = false;
		}

		private void updateUI() {
			string editorTitle = lang.GetString(LangKey.editor_title);
			if (surface != null && surface.CaptureDetails != null && surface.CaptureDetails.Title != null) {
				editorTitle = editorTitle + " - " + surface.CaptureDetails.Title;
			}
			this.Text = editorTitle;
			
			this.fileStripMenuItem.Text = lang.GetString(LangKey.editor_file);
			this.btnSave.Text = lang.GetString(LangKey.editor_save);
			this.saveToolStripMenuItem.Text = lang.GetString(LangKey.editor_save);
			this.saveAsToolStripMenuItem.Text = lang.GetString(LangKey.editor_saveas);
			this.btnClipboard.Text = lang.GetString(LangKey.editor_copyimagetoclipboard);
			this.copyImageToClipboardToolStripMenuItem.Text = lang.GetString(LangKey.editor_copyimagetoclipboard);
			this.emailToolStripMenuItem.Text = lang.GetString(LangKey.editor_email);
			this.btnEmail.Text = lang.GetString(LangKey.editor_email);

			this.btnPrint.Text = lang.GetString(LangKey.editor_print);
			this.printToolStripMenuItem.Text = lang.GetString(LangKey.editor_print)+"...";
			this.closeToolStripMenuItem.Text = lang.GetString(LangKey.editor_close);
			
			this.editToolStripMenuItem.Text = lang.GetString(LangKey.editor_edit);
			this.btnCursor.Text = lang.GetString(LangKey.editor_cursortool);
			this.btnRect.Text = lang.GetString(LangKey.editor_drawrectangle);
			this.addRectangleToolStripMenuItem.Text = lang.GetString(LangKey.editor_drawrectangle);
			this.btnEllipse.Text = lang.GetString(LangKey.editor_drawellipse);
			this.addEllipseToolStripMenuItem.Text = lang.GetString(LangKey.editor_drawellipse);
			this.btnText.Text = lang.GetString(LangKey.editor_drawtextbox);
			this.addTextBoxToolStripMenuItem.Text = lang.GetString(LangKey.editor_drawtextbox);
			this.btnLine.Text = lang.GetString(LangKey.editor_drawline);
			this.drawLineToolStripMenuItem.Text = lang.GetString(LangKey.editor_drawline);
			this.btnArrow.Text = lang.GetString(LangKey.editor_drawarrow);
			this.drawArrowToolStripMenuItem.Text = lang.GetString(LangKey.editor_drawarrow);
			this.btnHighlight.Text = lang.GetString(LangKey.editor_drawhighlighter);
			this.selectAllToolStripMenuItem.Text = lang.GetString(LangKey.editor_selectall);
			this.btnObfuscate.Text = lang.GetString(LangKey.editor_obfuscate);
			this.btnCrop.Text = lang.GetString(LangKey.editor_crop);
			this.btnDelete.Text = lang.GetString(LangKey.editor_deleteelement);
			this.removeObjectToolStripMenuItem.Text = lang.GetString(LangKey.editor_deleteelement);
			this.btnSettings.Text = lang.GetString(LangKey.contextmenu_settings);
			this.preferencesToolStripMenuItem.Text = lang.GetString(LangKey.contextmenu_settings);
			
			this.objectToolStripMenuItem.Text = lang.GetString(LangKey.editor_object);
			this.btnCut.Text = lang.GetString(LangKey.editor_cuttoclipboard);
			this.cutToolStripMenuItem.Text = lang.GetString(LangKey.editor_cuttoclipboard);
			this.btnCopy.Text = lang.GetString(LangKey.editor_copytoclipboard);
			this.copyToolStripMenuItem.Text = lang.GetString(LangKey.editor_copytoclipboard);
			this.btnPaste.Text = lang.GetString(LangKey.editor_pastefromclipboard);
			this.pasteToolStripMenuItem.Text = lang.GetString(LangKey.editor_pastefromclipboard);
			this.duplicateToolStripMenuItem.Text = lang.GetString(LangKey.editor_duplicate);
			
			this.arrangeToolStripMenuItem.Text = lang.GetString(LangKey.editor_arrange);
			this.upToTopToolStripMenuItem.Text = lang.GetString(LangKey.editor_uptotop);
			this.upOneLevelToolStripMenuItem.Text = lang.GetString(LangKey.editor_uponelevel);
			this.downOneLevelToolStripMenuItem.Text = lang.GetString(LangKey.editor_downonelevel);
			this.downToBottomToolStripMenuItem.Text = lang.GetString(LangKey.editor_downtobottom);
			this.btnLineColor.Text = lang.GetString(LangKey.editor_forecolor);
			this.btnFillColor.Text = lang.GetString(LangKey.editor_backcolor);
			this.lineThicknessLabel.Text = lang.GetString(LangKey.editor_thickness);
			this.arrowHeadsDropDownButton.Text = lang.GetString(LangKey.editor_arrowheads);
			
			this.helpToolStripMenuItem.Text = lang.GetString(LangKey.contextmenu_help);
			this.helpToolStripMenuItem1.Text = lang.GetString(LangKey.contextmenu_help);
			this.btnHelp.Text = lang.GetString(LangKey.contextmenu_help);
			
			this.aboutToolStripMenuItem.Text = lang.GetString(LangKey.contextmenu_about);
			
			this.copyPathMenuItem.Text = lang.GetString(LangKey.editor_copypathtoclipboard);
			this.openDirectoryMenuItem.Text = lang.GetString(LangKey.editor_opendirinexplorer);
			
			this.obfuscateModeButton.Text = lang.GetString(LangKey.editor_obfuscate_mode);
			this.highlightModeButton.Text = lang.GetString(LangKey.editor_highlight_mode);
			this.pixelizeToolStripMenuItem.Text = lang.GetString(LangKey.editor_obfuscate_pixelize);
			this.blurToolStripMenuItem.Text = lang.GetString(LangKey.editor_obfuscate_blur);
			this.textHighlightMenuItem.Text = lang.GetString(LangKey.editor_highlight_text);
			this.areaHighlightMenuItem.Text = lang.GetString(LangKey.editor_highlight_area);
			this.grayscaleHighlightMenuItem.Text = lang.GetString(LangKey.editor_highlight_grayscale);
			this.magnifyMenuItem.Text = lang.GetString(LangKey.editor_highlight_magnify);

			this.blurRadiusLabel.Text = lang.GetString(LangKey.editor_blur_radius);
			this.brightnessLabel.Text = lang.GetString(LangKey.editor_brightness);
			this.previewQualityLabel.Text = lang.GetString(LangKey.editor_preview_quality);
			this.magnificationFactorLabel.Text = lang.GetString(LangKey.editor_magnification_factor);
			this.pixelSizeLabel.Text = lang.GetString(LangKey.editor_pixel_size);
			this.arrowHeadsLabel.Text = lang.GetString(LangKey.editor_arrowheads);
			this.arrowHeadStartMenuItem.Text = lang.GetString(LangKey.editor_arrowheads_start);
			this.arrowHeadEndMenuItem.Text = lang.GetString(LangKey.editor_arrowheads_end);
			this.arrowHeadBothMenuItem.Text = lang.GetString(LangKey.editor_arrowheads_both);
			this.arrowHeadNoneMenuItem.Text = lang.GetString(LangKey.editor_arrowheads_none);
			this.shadowButton.Text = lang.GetString(LangKey.editor_shadow);


			this.fontSizeLabel.Text = lang.GetString(LangKey.editor_fontsize);
			this.fontBoldButton.Text = lang.GetString(LangKey.editor_bold);
			this.fontItalicButton.Text = lang.GetString(LangKey.editor_italic);

			this.btnConfirm.Text = lang.GetString(LangKey.editor_confirm);
			this.btnCancel.Text = lang.GetString(LangKey.editor_cancel);
			
			this.saveElementsToolStripMenuItem.Text = lang.GetString(LangKey.editor_save_objects);
			this.loadElementsToolStripMenuItem.Text = lang.GetString(LangKey.editor_load_objects);
		}
		
		public ISurface Surface {
			get {return surface;}
		}

		public void SetImagePath(string fullpath) {
			this.lastSaveFullPath = fullpath;
			if(fullpath == null) return;
			updateStatusLabel(lang.GetFormattedString(LangKey.editor_imagesaved,fullpath),fileSavedStatusContextMenu);
			this.Text = lang.GetString(LangKey.editor_title) + " - " + Path.GetFileName(fullpath);
			this.saveToolStripMenuItem.Enabled = true;
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
		
		public void SaveToStream(Stream stream, string extension, int quality) {
			using (Image image = surface.GetImageForExport()) {
				ImageOutput.SaveToStream(image, stream, extension, quality);
			}
		}

		public ToolStripMenuItem GetPluginMenuItem() {
			return pluginToolStripMenuItem;
		}
		#endregion
		
		#region filesystem options
		void SaveToolStripMenuItemClick(object sender, System.EventArgs e) {
			try {
				using (Image img = surface.GetImageForExport()) {
					ImageOutput.Save(img, lastSaveFullPath, surface.CaptureDetails);
					saved = true;
				}
				updateStatusLabel(lang.GetFormattedString(LangKey.editor_imagesaved,lastSaveFullPath),fileSavedStatusContextMenu);
			} catch(Exception) {
				MessageBox.Show(lang.GetFormattedString(LangKey.error_nowriteaccess,lastSaveFullPath).Replace(@"\\",@"\"), lang.GetString(LangKey.error));
			}
		}

		void BtnSaveClick(object sender, EventArgs e) {
			if (lastSaveFullPath != null) {
				SaveToolStripMenuItemClick(sender,e);
			} else {
				SaveAsToolStripMenuItemClick(sender, e);
			}
		}
		
		void SaveAsToolStripMenuItemClick(object sender, EventArgs eventArgs) {
			try {
				using (Image img = surface.GetImageForExport()) {
					// Bug #2918756 don't overwrite lastSaveFullPath if SaveWithDialog returns null!
					string savedTo = ImageOutput.SaveWithDialog(img, surface.CaptureDetails);
					if (savedTo != null) {
						saved = true;
						lastSaveFullPath = savedTo;
					}
				}
				
				if(lastSaveFullPath != null) {
					SetImagePath(lastSaveFullPath);
					updateStatusLabel(lang.GetFormattedString(LangKey.editor_imagesaved,lastSaveFullPath),fileSavedStatusContextMenu);
				} else {
					clearStatusLabel();
				}
			} catch (Exception e) {
				LOG.Error(lang.GetString(LangKey.error_save), e);
				MessageBox.Show(lang.GetString(LangKey.error_save), lang.GetString(LangKey.error));
			}
		}
		
		void CopyImageToClipboardToolStripMenuItemClick(object sender, System.EventArgs e) {
			try {
				using (Image img = surface.GetImageForExport()) {
					ClipboardHelper.SetClipboardData(img);
					saved = true;
				}
				updateStatusLabel(lang.GetString(LangKey.editor_storedtoclipboard));
			} catch (Exception) {
				updateStatusLabel(lang.GetString(LangKey.editor_clipboardfailed));
			}
		}

		void BtnClipboardClick(object sender, EventArgs e) {
			this.CopyImageToClipboardToolStripMenuItemClick(sender, e);
		}

		void PrintToolStripMenuItemClick(object sender, EventArgs e) {
			InvokePrint();
		}

		void BtnPrintClick(object sender, EventArgs e) {
			// commented for now: workaround seems to have issues on Vista :(
			//new ShowPrintDialogDelegate(InvokePrint).BeginInvoke(null,null);
			InvokePrint();
		}
		
		void InvokePrint() {
			using(Image img = surface.GetImageForExport()) {
				PrintHelper ph = new PrintHelper(img, surface.CaptureDetails);
				this.Hide();
				this.Show();
				PrinterSettings ps = ph.PrintWithDialog();
			
				if(ps != null) {
					saved = true;
					updateStatusLabel(lang.GetFormattedString(LangKey.editor_senttoprinter,ps.PrinterName));
				}
			}
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
		
		void SetButtonChecked(ToolStripButton btn) {
			UncheckAllToolButtons();
			btn.Checked = true;
		}
		
		private void UncheckAllToolButtons() {
			foreach (ToolStripButton butt in toolbarButtons) {
				butt.Checked = false;
			}
		}
		
		void AddRectangleToolStripMenuItemClick(object sender, System.EventArgs e) {
			BtnRectClick(sender, e);
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
			new HelpBrowserForm(conf.Ui_Language).Show();
		}

		void AboutToolStripMenuItemClick(object sender, System.EventArgs e) {
			new AboutForm().Show();
		}

		void PreferencesToolStripMenuItemClick(object sender, System.EventArgs e) {
			new SettingsForm().Show();
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
		}
		
		void ImageEditorFormFormClosing(object sender, FormClosingEventArgs e) {
			if (!saved) {
				MessageBoxButtons buttons = MessageBoxButtons.YesNoCancel;
				// Dissallow "CANCEL" if the application needs to shutdown
				if (e.CloseReason == CloseReason.ApplicationExitCall || e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.TaskManagerClosing) {
					buttons = MessageBoxButtons.YesNo;
				}
				DialogResult result = MessageBox.Show(lang.GetString(LangKey.editor_close_on_save), lang.GetString(LangKey.editor_close_on_save_title), buttons, MessageBoxIcon.Question);
				if (result.Equals(DialogResult.Cancel)) {
					e.Cancel = true;
					return;
				}
				if (result.Equals(DialogResult.Yes)) {
					if (lastSaveFullPath != null) {
						SaveToolStripMenuItemClick(sender,e);
					} else {
						SaveAsToolStripMenuItemClick(sender, e);
					}
					// Check if the save was made, if not it was cancelled so we cancel the closing
					if (!saved) {
						e.Cancel = true;
						return;
					}
				}
			}
			// persist our geometry string.
			FormHelper.StoreGeometry(this, out conf.Editor_WindowSize, out conf.Editor_WindowLocation, out conf.Editor_WindowState, out conf.Editor_Previous_Screenbounds);
			conf.Store();
			
			surface.Dispose();
			
			System.GC.Collect();
		}

		void ImageEditorFormKeyDown(object sender, KeyEventArgs e) {
			// LOG.Debug("Got key event "+e.KeyCode + ", " + e.Modifiers);
			// avoid conflict with other shortcuts and
			// make sure there's no selected element claiming input focus
			if(e.Modifiers.Equals(Keys.None) && !surface.KeysLocked) {
				if (Keys.Escape.Equals(e.KeyCode)) {
					BtnCursorClick(sender, e);
				} else if (Keys.R.Equals(e.KeyCode)) {
					BtnRectClick(sender, e);
				} else if (Keys.E.Equals(e.KeyCode)) {
					BtnEllipseClick(sender, e);
				} else if (Keys.L.Equals(e.KeyCode)) {
					BtnLineClick(sender, e);
				} else if (Keys.A.Equals(e.KeyCode)) {
					BtnArrowClick(sender, e);
				} else if (Keys.T.Equals(e.KeyCode)) {
					BtnTextClick(sender, e);
				} else if(Keys.H.Equals(e.KeyCode)) {
					BtnHighlightClick(sender, e);
				} else if(Keys.O.Equals(e.KeyCode)) {
					BtnObfuscateClick(sender, e);
				} else if(Keys.C.Equals(e.KeyCode)) {
					BtnCropClick(sender, e);
				}
			}
		}
		#endregion
		
		#region cursor key strokes
		protected override bool ProcessCmdKey(ref Message msg, Keys k) {
			// disable default key handling if surface has requested a lock
			if(!surface.KeysLocked) {
				surface.ProcessCmdKey(k);
				return base.ProcessCmdKey(ref msg, k);
			}
			return false;
		}
		#endregion
		
		#region helpers
		private void updateClipboardSurfaceDependencies() {
			// check dependencies for the Surface
			bool hasItems = surface.HasSelectedElements();
			bool actionAllowedForSelection = hasItems && !controlsDisabledDueToConfirmable;
			this.cutToolStripMenuItem.Enabled = actionAllowedForSelection;
			this.btnCut.Enabled = actionAllowedForSelection;

			this.btnCopy.Enabled = actionAllowedForSelection;
			this.copyToolStripMenuItem.Enabled = actionAllowedForSelection;

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
			Clipboard.SetText(lastSaveFullPath);
		}
		
		void OpenDirectoryMenuItemClick(object sender, EventArgs e) {
			ProcessStartInfo psi = new ProcessStartInfo("explorer");
			psi.Arguments = Path.GetDirectoryName(lastSaveFullPath);
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
					&& ((FieldFlag)props.GetFieldValue(FieldType.FLAGS)&FieldFlag.CONFIRMABLE) == FieldFlag.CONFIRMABLE;
				
				obfuscateModeButton.Visible = props.HasFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE);
				highlightModeButton.Visible = props.HasFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT);
			} else {
				foreach(ToolStripItem c in propertiesToolStrip.Items) {
					c.Visible = false;
				}
			}
			propertiesToolStrip.ResumeLayout();
		}
		
		/// <summary>
		/// refreshes all editor controls depending on selected elements and their fields
		/// </summary>
		private void refreshEditorControls() {
			FieldAggregator props = surface.FieldAggregator;
			// if a confirmable element is selected, we must disable most of the controls
			// since we demand confirmation or cancel for confirmable element
			if (props.HasFieldValue(FieldType.FLAGS) && ((FieldFlag)props.GetFieldValue(FieldType.FLAGS) & FieldFlag.CONFIRMABLE) == FieldFlag.CONFIRMABLE) {
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
			bool actionAllowedForSelection = surface.HasSelectedElements() && !controlsDisabledDueToConfirmable;
			this.btnCopy.Enabled = actionAllowedForSelection;
			this.btnCut.Enabled = actionAllowedForSelection;
			this.btnDelete.Enabled = actionAllowedForSelection;
			this.copyToolStripMenuItem.Enabled = actionAllowedForSelection;
			this.cutToolStripMenuItem.Enabled = actionAllowedForSelection;
			this.duplicateToolStripMenuItem.Enabled = actionAllowedForSelection;
			this.removeObjectToolStripMenuItem.Enabled = actionAllowedForSelection;
			
			// en/disablearrage controls depending on hierarchy of selected elements
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
			saveFileDialog.FileName = FilenameHelper.GetFilenameWithoutExtensionFromPattern(conf.Output_File_FilenamePattern, surface.CaptureDetails);
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
		
		void EmailToolStripMenuItemClick(object sender, EventArgs e) {
			using (Image img = surface.GetImageForExport()) {
				MapiMailMessage.SendImage(img, surface.CaptureDetails);
				saved = true;
			}
		}
		
		void BtnEmailClick(object sender, EventArgs e) {
			EmailToolStripMenuItemClick(sender, e);
		}
		
		protected void FilterPresetDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
			refreshFieldControls();
			
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
	}
}
