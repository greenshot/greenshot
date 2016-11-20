//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Dapplo.Log;
using Dapplo.Windows.Native;
using Dapplo.Windows.Structs;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Controls;
using Greenshot.Addon.Core;
using Greenshot.Addon.Editor.Drawing;
using Greenshot.Addon.Editor.Drawing.Fields.Binding;
using Greenshot.Addon.Editor.Helpers;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Drawing;
using Greenshot.Addon.Interfaces.Forms;
using Greenshot.CaptureCore;
using Greenshot.Core;
using Greenshot.Core.Configuration;
using Greenshot.Core.Gfx;

#endregion

namespace Greenshot.Addon.Editor.Forms
{
	/// <summary>
	///     Description of ImageEditorForm.
	/// </summary>
	public partial class ImageEditorForm : BaseForm, IImageEditor
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly IEditorConfiguration editorConfiguration = IniConfig.Current.Get<IEditorConfiguration>();
		private static readonly IEditorLanguage editorLanguage = LanguageLoader.Current.Get<IGreenshotLanguage>();

		private static readonly List<string> ignoreDestinations = new List<string>
		{
			BuildInDestinationEnum.Picker.ToString(), BuildInDestinationEnum.Editor.ToString()
		};

		private static readonly List<IImageEditor> editorList = new List<IImageEditor>();

		private static readonly string[] SupportedClipboardFormats =
		{
			typeof(string).FullName, "Text", typeof(IDrawableContainerList).FullName
		};

		private readonly List<BidirectionalBinding> _bindings = new List<BidirectionalBinding>();

		// whether part of the editor controls are disabled depending on selected item(s)
		private bool _controlsDisabledDueToConfirmable;
		private bool _originalBoldCheckState;
		private bool _originalItalicCheckState;

		private Surface _surface;
		private GreenshotToolStripButton[] _toolbarButtons;

		public ImageEditorForm(ISurface iSurface, bool outputMade)
		{
			editorList.Add(this);

			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			ManualLanguageApply = true;
			InitializeComponent();

			Load += (sender, eventArgs) =>
			{
				// Create export buttons via dispatcher
				this.InvokeAsync(() =>
				{
					// TODO: Fix!
					foreach (IDestination destination in new IDestination[] {})
					{
						//if (destination.Priority <= 2)
						//{
						//	continue;
						//}
						//if (!destination.IsActive)
						//{
						//	continue;
						//}
						//if (destination.DisplayIcon == null)
						//{
						//	continue;
						//}
						try
						{
							//AddDestinationButton(null);
						}
						catch (Exception addingException)
						{
							Log.Warn().WriteLine("Problem adding destination {0}", destination.Designation);
							Log.Warn().WriteLine(addingException);
						}
					}
				});
			};

			// Events
			addBorderToolStripMenuItem.Click += async (sender, e) => await AddBorderToolStripMenuItemClickAsync();
			addDropshadowToolStripMenuItem.Click += async (sender, e) => await AddDropshadowToolStripMenuItemClickAsync();
			KeyDown += async (sender, e) => await ImageEditorFormKeyDownAsync(sender, e);
			rotateCwToolstripButton.Click += async (sender, e) => await RotateCwToolstripButtonClickAsync();
			tornEdgesToolStripMenuItem.Click += async (sender, e) => await TornEdgesToolStripMenuItemClickAsync();
			grayscaleToolStripMenuItem.Click += async (sender, e) => await GrayscaleToolStripMenuItemClickAsync();
			invertToolStripMenuItem.Click += async (sender, e) => await InvertToolStripMenuItemClickAsync();
			btnResize.Click += async (sender, e) => await BtnResizeClickAsync();
			rotateCcwToolstripButton.Click += async (sender, e) => await RotateCcwToolstripButtonClickAsync();

			// Make sure the editor is placed on the same location as the last editor was on close
			new WindowDetails(Handle).WindowPlacement = GetEditorPlacement();

			// init surface
			Surface = iSurface;
			// Intial "saved" flag for asking if the image needs to be save
			_surface.Modified = !outputMade;

			UpdateUI();

			// Workaround: As the cursor is (mostly) selected on the surface a funny artifact is visible, this fixes it.
			HideToolstripItems();
		}

		public static List<IImageEditor> Editors
		{
			get
			{
				try
				{
					editorList.Sort((e1, e2) => string.Compare(e1.Surface.CaptureDetails.Title, e2.Surface.CaptureDetails.Title, StringComparison.Ordinal));
				}
				catch (Exception ex)
				{
					Log.Warn().WriteLine(ex, "Sorting of editors failed.");
				}
				return editorList;
			}
		}

		/// <summary>
		///     An Implementation for the IImageEditor, this way Plugins have access to the HWND handles wich can be used with
		///     Win32 API calls.
		/// </summary>
		public IWin32Window WindowHandle
		{
			get { return this; }
		}

		public ISurface Surface
		{
			get { return _surface; }
			set { SetSurface(value); }
		}

		private async Task AddBorderToolStripMenuItemClickAsync(CancellationToken token = default(CancellationToken))
		{
			await _surface.ApplyBitmapEffectAsync(new BorderEffect(), token);
			UpdateUndoRedoSurfaceDependencies();
		}

		/// <summary>
		///     This is used when the dropshadow button is used
		/// </summary>
		private async Task AddDropshadowToolStripMenuItemClickAsync(CancellationToken token = default(CancellationToken))
		{
			var dropShadowEffect = editorConfiguration.DropShadowEffectSettings;
			var result = new DropShadowSettingsForm(dropShadowEffect).ShowDialog(this);
			if (result == DialogResult.OK)
			{
				await _surface.ApplyBitmapEffectAsync(dropShadowEffect, token);
				UpdateUndoRedoSurfaceDependencies();
			}
		}

		private void ArrowHeadsToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.ChangeFields(FieldTypes.ARROWHEADS, (ArrowHeadCombination) ((ToolStripMenuItem) sender).Tag);
		}

		private void AutoCropToolStripMenuItemClick(object sender, EventArgs e)
		{
			if (editorConfiguration.AutoCropDifference < 0)
			{
				editorConfiguration.AutoCropDifference = 0;
			}
			if (editorConfiguration.AutoCropDifference > 255)
			{
				editorConfiguration.AutoCropDifference = 255;
			}
			if (_surface.AutoCrop(editorConfiguration.AutoCropDifference))
			{
				RefreshFieldControls();
			}
		}

		private void BindFieldControls()
		{
			_bindings.Add(new BidirectionalBinding(btnFillColor, "SelectedColor", _surface, FieldTypes.FILL_COLOR, NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(btnLineColor, "SelectedColor", _surface, FieldTypes.LINE_COLOR, NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(lineThicknessUpDown, "Value", _surface, FieldTypes.LINE_THICKNESS, DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(counterUpDown, "Value", _surface, FieldTypes.COUNTER_START, DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(blurRadiusUpDown, "Value", _surface, FieldTypes.BLUR_RADIUS, DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(magnificationFactorUpDown, "Value", _surface, FieldTypes.MAGNIFICATION_FACTOR, DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(pixelSizeUpDown, "Value", _surface, FieldTypes.PIXEL_SIZE, DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(brightnessUpDown, "Value", _surface, FieldTypes.BRIGHTNESS, DecimalDoublePercentageConverter.GetInstance(), NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(fontFamilyComboBox, "Text", _surface, FieldTypes.FONT_FAMILY, NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(fontSizeUpDown, "Value", _surface, FieldTypes.FONT_SIZE, DecimalFloatConverter.GetInstance(), NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(fontBoldButton, "Checked", _surface, FieldTypes.FONT_BOLD, NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(fontItalicButton, "Checked", _surface, FieldTypes.FONT_ITALIC, NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(textHorizontalAlignmentButton, "SelectedTag", _surface, FieldTypes.TEXT_HORIZONTAL_ALIGNMENT, HorizontalAlignmentConverter.GetInstance(), NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(textVerticalAlignmentButton, "SelectedTag", _surface, FieldTypes.TEXT_VERTICAL_ALIGNMENT, VerticalAlignmentConverter.GetInstance(), NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(shadowButton, "Checked", _surface, FieldTypes.SHADOW, NotNullValidator.GetInstance()));
			_bindings.Add(new BidirectionalBinding(obfuscateModeButton, "SelectedTag", _surface, FieldTypes.PREPARED_FILTER_OBFUSCATE));
			_bindings.Add(new BidirectionalBinding(highlightModeButton, "SelectedTag", _surface, FieldTypes.PREPARED_FILTER_HIGHLIGHT));
		}

		private void BtnCancelClick(object sender, EventArgs e)
		{
			_surface.ConfirmSelectedConfirmableElements(false);
			RefreshFieldControls();
		}


		private void BtnConfirmClick(object sender, EventArgs e)
		{
			_surface.ConfirmSelectedConfirmableElements(true);
			RefreshFieldControls();
		}

		/// <summary>
		///     Open the resize settings from, and resize if ok was pressed
		/// </summary>
		private async Task BtnResizeClickAsync(CancellationToken token = default(CancellationToken))
		{
			var resizeEffect = new ResizeEffect(_surface.Image.Width, _surface.Image.Height, true);
			var result = new ResizeSettingsForm(resizeEffect).ShowDialog(this);
			if (result == DialogResult.OK)
			{
				await _surface.ApplyBitmapEffectAsync(resizeEffect, token);
				UpdateUndoRedoSurfaceDependencies();
			}
		}

		/// <summary>
		///     According to some information I found, the clear doesn't work correctly when the shortcutkeys are set?
		///     This helper method takes care of this.
		/// </summary>
		/// <param name="items"></param>
		private void ClearItems(ToolStripItemCollection items)
		{
			foreach (var item in items)
			{
				ToolStripMenuItem menuItem = item as ToolStripMenuItem;
				if ((menuItem != null) && (menuItem.ShortcutKeys != Keys.None))
				{
					menuItem.ShortcutKeys = Keys.None;
				}
			}
			items.Clear();
		}

		private void ClearToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.Clear(Color.Transparent);
			UpdateUndoRedoSurfaceDependencies();
		}

		private void Contextmenu_window_Click(object sender, EventArgs e)
		{
			ToolStripMenuItem clickedItem = (ToolStripMenuItem) sender;
			try
			{
				var windowToCapture = (WindowDetails) clickedItem.Tag;
				ICapture capture = new Capture();
				using (Graphics graphics = Graphics.FromHwnd(Handle))
				{
					capture.CaptureDetails.DpiX = graphics.DpiY;
					capture.CaptureDetails.DpiY = graphics.DpiY;
				}
				windowToCapture = windowToCapture.WindowToCapture();
				if (windowToCapture != null)
				{
					// TODO:
					//capture = CaptureHelper.CaptureWindow(windowToCapture, capture, coreConfiguration.WindowCaptureMode);
					if ((capture != null) && (capture.CaptureDetails != null) && (capture.Image != null))
					{
						((Bitmap) capture.Image).SetResolution(capture.CaptureDetails.DpiX, capture.CaptureDetails.DpiY);
						_surface.AddImageContainer((Bitmap) capture.Image, 100, 100);
					}
					Activate();
					WindowDetails.ToForeground(Handle);
				}

				if (capture != null)
				{
					capture.Dispose();
				}
			}
			catch (Exception exception)
			{
				Log.Error().WriteLine(exception, "Capturing window failed");
			}
		}

		private async void DestinationToolStripMenuItemClickAsync(object sender, EventArgs e)
		{
			IDestination clickedDestination = null;
			if (sender is Control)
			{
				Control clickedControl = sender as Control;
				if (clickedControl.ContextMenuStrip != null)
				{
					clickedControl.ContextMenuStrip.Show(Cursor.Position);
					return;
				}
				clickedDestination = (IDestination) clickedControl.Tag;
			}
			else if (sender is ToolStripMenuItem)
			{
				ToolStripMenuItem clickedMenuItem = sender as ToolStripMenuItem;
				clickedDestination = (IDestination) clickedMenuItem.Tag;
			}
			if (clickedDestination != null)
			{
				var exportInformation = await clickedDestination.Export(null, new Capture(_surface.Image), default(CancellationToken));
				if (exportInformation.NotificationType == NotificationTypes.Success)
				{
					_surface.Modified = false;
				}
			}
		}

		private void EditToolStripMenuItemClick(object sender, EventArgs e)
		{
			UpdateClipboardSurfaceDependencies();
			UpdateUndoRedoSurfaceDependencies();
		}

		/// <summary>
		///     Added for FEATURE-919, increasing the canvas by 25 pixels in every direction.
		/// </summary>
		private async Task EnlargeCanvasToolStripMenuItemClickAsync(CancellationToken token = default(CancellationToken))
		{
			await _surface.ApplyBitmapEffectAsync(new ResizeCanvasEffect(25, 25, 25, 25), token);
			UpdateUndoRedoSurfaceDependencies();
		}

		private void FileMenuDropDownOpening(object sender, EventArgs eventArgs)
		{
			ClearItems(fileStripMenuItem.DropDownItems);

			// Add the destinations
			foreach (IDestination destination in new IDestination[] {})
			{
				if (ignoreDestinations.Contains(destination.Designation))
				{
					continue;
				}
				if (!destination.IsEnabled)
				{
					continue;
				}

				// TODO: Fix
				ToolStripMenuItem item = null; //destination.CreateMenuItem(true, DestinationToolStripMenuItemClickAsync);
				if (item != null)
				{
					fileStripMenuItem.DropDownItems.Add(item);
					item.ShortcutKeys = Keys.None; //destination.EditorShortcutKeys;
				}
			}
			// add the elements after the destinations
			fileStripMenuItem.DropDownItems.Add(toolStripSeparator9);
			fileStripMenuItem.DropDownItems.Add(closeToolStripMenuItem);
		}

		protected void FilterPresetDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			RefreshFieldControls();
			Invalidate(true);
		}

		private void FontBoldButtonClick(object sender, EventArgs e)
		{
			_originalBoldCheckState = fontBoldButton.Checked;
		}

		private void FontItalicButtonClick(object sender, EventArgs e)
		{
			_originalItalicCheckState = fontItalicButton.Checked;
		}

		private void FontPropertyChanged(object sender, EventArgs e)
		{
			// in case we forced another FontStyle before, reset it first.
			if ((fontBoldButton != null) && (_originalBoldCheckState != fontBoldButton.Checked))
			{
				fontBoldButton.Checked = _originalBoldCheckState;
			}
			if ((fontItalicButton != null) && (_originalItalicCheckState != fontItalicButton.Checked))
			{
				fontItalicButton.Checked = _originalItalicCheckState;
			}

			FontFamily fam = fontFamilyComboBox.FontFamily;

			bool boldAvailable = fam.IsStyleAvailable(FontStyle.Bold);
			if (!boldAvailable)
			{
				_originalBoldCheckState = fontBoldButton.Checked;
				fontBoldButton.Checked = false;
			}
			fontBoldButton.Enabled = boldAvailable;

			bool italicAvailable = fam.IsStyleAvailable(FontStyle.Italic);
			if (!italicAvailable)
			{
				fontItalicButton.Checked = false;
			}
			fontItalicButton.Enabled = italicAvailable;

			bool regularAvailable = fam.IsStyleAvailable(FontStyle.Regular);
			if (!regularAvailable)
			{
				if (boldAvailable)
				{
					fontBoldButton.Checked = true;
				}
				else if (italicAvailable)
				{
					fontItalicButton.Checked = true;
				}
			}
		}

		/// <summary>
		///     Helper for getting the editor placement
		/// </summary>
		/// <returns>WindowPlacement</returns>
		private static WindowPlacement GetEditorPlacement()
		{
			WindowPlacement placement = WindowPlacement.Default;
			placement.NormalPosition = new RECT(editorConfiguration.WindowNormalPosition);
			placement.MaxPosition = new POINT(editorConfiguration.WindowMaxPosition);
			placement.MinPosition = new POINT(editorConfiguration.WindowMinPosition);
			placement.ShowCmd = editorConfiguration.ShowWindowCommand;
			placement.Flags = editorConfiguration.WindowPlacementFlags;
			return placement;
		}

		private async Task GrayscaleToolStripMenuItemClickAsync(CancellationToken token = default(CancellationToken))
		{
			await _surface.ApplyBitmapEffectAsync(new GrayscaleEffect(), token);
			UpdateUndoRedoSurfaceDependencies();
		}

		private void HideToolstripItems()
		{
			foreach (ToolStripItem toolStripItem in propertiesToolStrip.Items)
			{
				toolStripItem.Visible = false;
			}
		}

		private void ImageEditorFormResize(object sender, EventArgs e)
		{
			if ((Surface == null) || (Surface.Image == null) || (surfacePanel == null))
			{
				return;
			}
			Size imageSize = Surface.Image.Size;
			Size currentClientSize = surfacePanel.ClientSize;
			var canvas = Surface as Control;
			Panel panel = (Panel) canvas.Parent;
			if (panel == null)
			{
				return;
			}
			int offsetX = -panel.HorizontalScroll.Value;
			int offsetY = -panel.VerticalScroll.Value;
			if (canvas != null)
			{
				if (currentClientSize.Width > imageSize.Width)
				{
					canvas.Left = offsetX + (currentClientSize.Width - imageSize.Width)/2;
				}
				else
				{
					canvas.Left = offsetX + 0;
				}
			}
			if (canvas != null)
			{
				if (currentClientSize.Height > imageSize.Height)
				{
					canvas.Top = offsetY + (currentClientSize.Height - imageSize.Height)/2;
				}
				else
				{
					canvas.Top = offsetY + 0;
				}
			}
		}

		private void Insert_window_toolstripmenuitemMouseEnter(object sender, EventArgs e)
		{
			ToolStripMenuItem captureWindowMenuItem = (ToolStripMenuItem) sender;
			// TODO:
			//MainForm.Instance.AddCaptureWindowMenuItems(captureWindowMenuItem, Contextmenu_window_Click);	
		}

		private async Task InvertToolStripMenuItemClickAsync(CancellationToken token = default(CancellationToken))
		{
			await _surface.ApplyBitmapEffectAsync(new InvertEffect(), token);
			UpdateUndoRedoSurfaceDependencies();
		}

		private void LoadElementsToolStripMenuItemClick(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Greenshot templates (*.gst)|*.gst";
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				using (Stream streamRead = File.OpenRead(openFileDialog.FileName))
				{
					_surface.LoadElementsFromStream(streamRead);
				}
				_surface.Refresh();
			}
		}

		/// <summary>
		///     Workaround for having a border around the dropdown
		///     See: http://stackoverflow.com/questions/9560812/change-border-of-toolstripcombobox-with-flat-style
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PropertiesToolStrip_Paint(object sender, PaintEventArgs e)
		{
			using (Pen cbBorderPen = new Pen(SystemColors.ActiveBorder))
			{
				// Loop over all items in the propertiesToolStrip
				foreach (ToolStripItem item in propertiesToolStrip.Items)
				{
					ToolStripComboBox cb = item as ToolStripComboBox;
					// Only ToolStripComboBox that are visible
					if ((cb == null) || !cb.Visible)
					{
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
		///     refreshes all editor controls depending on selected elements and their fields
		/// </summary>
		private void RefreshEditorControls()
		{
			int stepLabels = _surface.CountStepLabels(null);
			Image icon;
			if (stepLabels <= 20)
			{
				icon = (Image) resources.GetObject(string.Format("btnStepLabel{0:00}.Image", stepLabels));
			}
			else
			{
				icon = (Image) resources.GetObject("btnStepLabel20+.Image");
			}
			btnStepLabel.Image = icon;
			addCounterToolStripMenuItem.Image = icon;

			// if a confirmable element is selected, we must disable most of the controls
			// since we demand confirmation or cancel for confirmable element
			if (_surface.IsElementWithFlagSelected(ElementFlag.CONFIRMABLE))
			{
				// disable most controls
				if (!_controlsDisabledDueToConfirmable)
				{
					ToolStripItemEndisabler.Disable(menuStrip1);
					ToolStripItemEndisabler.Disable(destinationsToolStrip);
					ToolStripItemEndisabler.Disable(toolsToolStrip);
					ToolStripItemEndisabler.Enable(closeToolStripMenuItem);
					ToolStripItemEndisabler.Enable(helpToolStripMenuItem);
					ToolStripItemEndisabler.Enable(aboutToolStripMenuItem);
					ToolStripItemEndisabler.Enable(preferencesToolStripMenuItem);
					_controlsDisabledDueToConfirmable = true;
				}
			}
			else if (_controlsDisabledDueToConfirmable)
			{
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
			if (arrangeToolStripMenuItem.Enabled)
			{
				upToTopToolStripMenuItem.Enabled = pull;
				upOneLevelToolStripMenuItem.Enabled = pull;
				downToBottomToolStripMenuItem.Enabled = push;
				downOneLevelToolStripMenuItem.Enabled = push;
			}

			// finally show/hide field controls depending on the fields of selected elements
			RefreshFieldControls();
		}

		/// <summary>
		///     shows/hides field controls (2nd toolbar on top) depending on fields of selected elements
		/// </summary>
		private void RefreshFieldControls()
		{
			propertiesToolStrip.SuspendLayout();
			if (_surface.HasSelectedElements || (_surface.DrawingMode != DrawingModes.None))
			{
				// Update bindings
				foreach (var binding in _bindings)
				{
					binding.Refresh();
				}
				lineThicknessLabel.Visible = lineThicknessUpDown.Visible;
				blurRadiusLabel.Visible = blurRadiusUpDown.Visible;
				magnificationFactorLabel.Visible = magnificationFactorUpDown.Visible;
				pixelSizeLabel.Visible = pixelSizeUpDown.Visible;
				brightnessLabel.Visible = brightnessUpDown.Visible;
				arrowHeadsLabel.Visible = arrowHeadsDropDownButton.Visible;
				fontSizeLabel.Visible = fontSizeUpDown.Visible;
				btnConfirm.Visible = btnCancel.Visible = _surface.IsElementWithFlagSelected(ElementFlag.CONFIRMABLE);
				counterLabel.Visible = counterUpDown.Visible = _surface.IsElementWithFlagSelected(ElementFlag.COUNTER);
			}
			else
			{
				HideToolstripItems();
			}
			propertiesToolStrip.ResumeLayout();
		}

		/// <summary>
		///     Remove the current surface
		/// </summary>
		private void RemoveSurface()
		{
			if (_surface != null)
			{
				surfacePanel.Controls.Remove(_surface);
				_surface.Dispose();
				_surface = null;
			}
		}

		private async Task RotateCcwToolstripButtonClickAsync(CancellationToken token = default(CancellationToken))
		{
			await _surface.ApplyBitmapEffectAsync(new RotateEffect(270), token);
			UpdateUndoRedoSurfaceDependencies();
		}

		private async Task RotateCwToolstripButtonClickAsync(CancellationToken token = default(CancellationToken))
		{
			await _surface.ApplyBitmapEffectAsync(new RotateEffect(90), token);
			UpdateUndoRedoSurfaceDependencies();
		}

		private void SaveElementsToolStripMenuItemClick(object sender, EventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Greenshot templates (*.gst)|*.gst";
			saveFileDialog.FileName = FilenameHelper.GetFilenameWithoutExtensionFromPattern(coreConfiguration.OutputFileFilenamePattern, _surface.CaptureDetails);
			DialogResult dialogResult = saveFileDialog.ShowDialog();
			if (dialogResult.Equals(DialogResult.OK))
			{
				using (Stream streamWrite = File.OpenWrite(saveFileDialog.FileName))
				{
					_surface.SaveElementsToStream(streamWrite);
				}
			}
		}

		private void SelectAllToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.SelectAllElements();
		}

		/// <summary>
		///     Helper for setting the editor placement
		/// </summary>
		public static void SetEditorPlacement(WindowPlacement placement)
		{
			editorConfiguration.WindowNormalPosition = placement.NormalPosition.ToRectangle();
			editorConfiguration.WindowMaxPosition = placement.MaxPosition.ToSystemDrawingPoint();
			editorConfiguration.WindowMinPosition = placement.MinPosition.ToSystemDrawingPoint();
			editorConfiguration.ShowWindowCommand = placement.ShowCmd;
			editorConfiguration.WindowPlacementFlags = placement.Flags;
		}

		public void SetImagePath(string fullpath)
		{
			// Check if the editor supports the format
			if ((fullpath != null) && (fullpath.EndsWith(".ico") || fullpath.EndsWith(".wmf")))
			{
				fullpath = null;
			}
			_surface.LastSaveFullPath = fullpath;

			if (fullpath == null)
			{
				return;
			}
			UpdateStatusLabel(string.Format(editorLanguage.EditorImagesaved, fullpath), fileSavedStatusContextMenu);
			Text = Path.GetFileName(fullpath) + " - " + editorLanguage.EditorTitle;
		}

		/// <summary>
		///     Change the surface
		/// </summary>
		/// <param name="newCapture"></param>
		private void SetSurface(ICapture newCapture)
		{
			if ((Surface != null) && Surface.Modified)
			{
				throw new ApplicationException("Surface modified");
			}

			RemoveSurface();

			surfacePanel.Height = 10;
			surfacePanel.Width = 10;
			_surface = newCapture as Surface;
			if (_surface == null)
			{
				return;
			}
			surfacePanel.Controls.Add(_surface);
			var backgroundForTransparency = GreenshotResources.GetImage("Checkerboard.Image");
			_surface.TransparencyBackgroundBrush = new TextureBrush(backgroundForTransparency, WrapMode.Tile);

			_surface.MovingElementChanged += delegate { RefreshEditorControls(); };
			_surface.DrawingModeChanged += surface_DrawingModeChanged;
			_surface.SurfaceSizeChanged += SurfaceSizeChanged;
			_surface.SurfaceMessage += SurfaceMessageReceived;
			SurfaceSizeChanged(Surface, null);

			BindFieldControls();
			RefreshEditorControls();
			// Fix title
			if ((_surface.CaptureDetails != null) && (_surface.CaptureDetails.Title != null))
			{
				Text = _surface.CaptureDetails.Title + " - " + editorLanguage.EditorTitle;
			}
			WindowDetails.ToForeground(Handle);
		}

		/// <summary>
		///     Added for FEATURE-919, to make the capture as small as possible again.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ShrinkCanvasToolStripMenuItemClick(object sender, EventArgs e)
		{
			Rectangle cropRectangle;
			using (Image tmpImage = Surface.GetImageForExport())
			{
				cropRectangle = ImageHelper.FindAutoCropRectangle(tmpImage, editorConfiguration.AutoCropDifference);
			}
			if (_surface.IsCropPossible(ref cropRectangle))
			{
				_surface.ApplyCrop(cropRectangle);
				UpdateUndoRedoSurfaceDependencies();
			}
		}

		private void surface_DrawingModeChanged(object source, SurfaceDrawingModeEventArgs eventArgs)
		{
			switch (eventArgs.DrawingMode)
			{
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

		/// <summary>
		///     This is the SufraceMessageEvent receiver which display a message in the status bar if the
		///     surface is exported. It also updates the title to represent the filename, if there is one.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void SurfaceMessageReceived(object sender, SurfaceMessageEventArgs eventArgs)
		{
			if (InvokeRequired)
			{
				Invoke(new SurfaceMessageReceivedThreadSafeDelegate(SurfaceMessageReceived), sender, eventArgs);
			}
			else
			{
				string dateTime = DateTime.Now.ToLongTimeString();
				// TODO: Fix that we only open files, like in the tooltip
				switch (eventArgs.MessageType)
				{
					case SurfaceMessageTyp.FileSaved:
						// Put the event message on the status label and attach the context menu
						UpdateStatusLabel(dateTime + " - " + eventArgs.Message, fileSavedStatusContextMenu);
						// Change title
						Text = eventArgs.Capture.CaptureDetails.StoredAt + " - " + editorLanguage.EditorTitle;
						break;
					default:
						// Put the event message on the status label
						UpdateStatusLabel(dateTime + " - " + eventArgs.Message);
						break;
				}
			}
		}

		/// <summary>
		///     This is called when the size of the surface chances, used for resizing and displaying the size information
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SurfaceSizeChanged(object sender, EventArgs e)
		{
			if (editorConfiguration.MatchSizeToCapture)
			{
				// Set editor's initial size to the size of the surface plus the size of the chrome
				Size imageSize = Surface.Image.Size;
				Size currentFormSize = Size;
				Size currentImageClientSize = surfacePanel.ClientSize;
				int minimumFormWidth = 650;
				int minimumFormHeight = 530;
				int newWidth = Math.Max(minimumFormWidth, currentFormSize.Width - currentImageClientSize.Width + imageSize.Width);
				int newHeight = Math.Max(minimumFormHeight, currentFormSize.Height - currentImageClientSize.Height + imageSize.Height);
				Size = new Size(newWidth, newHeight);
			}
			dimensionsLabel.Text = Surface.Image.Width + "x" + Surface.Image.Height;
			ImageEditorFormResize(sender, new EventArgs());
		}

		private void ToolBarFocusableElementGotFocus(object sender, EventArgs e)
		{
			_surface.KeysLocked = true;
		}

		private void ToolBarFocusableElementLostFocus(object sender, EventArgs e)
		{
			_surface.KeysLocked = false;
		}

		/// <summary>
		///     Call the torn edge effect
		/// </summary>
		/// <param name="token"></param>
		private async Task TornEdgesToolStripMenuItemClickAsync(CancellationToken token = default(CancellationToken))
		{
			var tornEdgeEffect = editorConfiguration.TornEdgeEffectSettings;
			var result = new TornEdgeSettingsForm(tornEdgeEffect).ShowDialog(this);
			if (result == DialogResult.OK)
			{
				await _surface.ApplyBitmapEffectAsync(tornEdgeEffect, token);
				UpdateUndoRedoSurfaceDependencies();
			}
		}

		private void UpdateUI()
		{
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
			surfacePanel.Height = 10;

			fontFamilyComboBox.PropertyChanged += FontPropertyChanged;

			obfuscateModeButton.DropDownItemClicked += FilterPresetDropDownItemClicked;
			highlightModeButton.DropDownItemClicked += FilterPresetDropDownItemClicked;

			_toolbarButtons = new[]
			{
				btnCursor, btnRect, btnEllipse, btnText, btnLine, btnArrow, btnFreehand, btnHighlight, btnObfuscate, btnCrop, btnStepLabel, btnSpeechBubble
			};
			//toolbarDropDownButtons = new ToolStripDropDownButton[]{btnBlur, btnPixeliate, btnTextHighlighter, btnAreaHighlighter, btnMagnifier};

			pluginToolStripMenuItem.Visible = pluginToolStripMenuItem.DropDownItems.Count > 0;

			// Workaround: for the MouseWheel event which doesn't get to the panel
			MouseWheel += PanelMouseWheel;

			ApplyLanguage();
		}

		private delegate void SurfaceMessageReceivedThreadSafeDelegate(object sender, SurfaceMessageEventArgs eventArgs);

		#region plugin interfaces

		public ToolStripMenuItem GetPluginMenuItem()
		{
			return pluginToolStripMenuItem;
		}

		public ToolStripMenuItem GetFileMenuItem()
		{
			return fileStripMenuItem;
		}

		#endregion

		#region filesystem options

		private async Task SaveAsync()
		{
			string destinationDesignation = BuildInDestinationEnum.FileNoDialog.ToString();
			if (_surface.LastSaveFullPath == null)
			{
				destinationDesignation = BuildInDestinationEnum.FileDialog.ToString();
			}
			await Task.Yield();
			// TODO: Fix
			// await LegacyDestinationHelper.ExportCaptureAsync(true, destinationDesignation, _surface);
		}

		private async void BtnSaveClickAsync(object sender, EventArgs e)
		{
			await SaveAsync();
		}

		private void BtnClipboardClick(object sender, EventArgs e)
		{
			// TODO: Fix
			// await LegacyDestinationHelper.ExportCaptureAsync(true, BuildInDestinationEnum.Clipboard.ToString(), _surface);
		}

		private void BtnPrintClick(object sender, EventArgs e)
		{
			// The BeginInvoke is a solution for the printdialog not having focus
			this.InvokeAsync(() =>
			{
				// TODO: Fix
				// await LegacyDestinationHelper.ExportCaptureAsync(true, BuildInDestinationEnum.Printer.ToString(), _surface);
			});
		}

		private void CloseToolStripMenuItemClick(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region drawing options

		private void BtnEllipseClick(object sender, EventArgs e)
		{
			_surface.DrawingMode = DrawingModes.Ellipse;
			RefreshFieldControls();
		}

		private void BtnCursorClick(object sender, EventArgs e)
		{
			_surface.DrawingMode = DrawingModes.None;
			RefreshFieldControls();
		}

		private void BtnRectClick(object sender, EventArgs e)
		{
			_surface.DrawingMode = DrawingModes.Rect;
			RefreshFieldControls();
		}

		private void BtnTextClick(object sender, EventArgs e)
		{
			_surface.DrawingMode = DrawingModes.Text;
			RefreshFieldControls();
		}

		private void BtnSpeechBubbleClick(object sender, EventArgs e)
		{
			_surface.DrawingMode = DrawingModes.SpeechBubble;
			RefreshFieldControls();
		}

		private void BtnStepLabelClick(object sender, EventArgs e)
		{
			_surface.DrawingMode = DrawingModes.StepLabel;
			RefreshFieldControls();
		}

		private void BtnLineClick(object sender, EventArgs e)
		{
			_surface.DrawingMode = DrawingModes.Line;
			RefreshFieldControls();
		}

		private void BtnArrowClick(object sender, EventArgs e)
		{
			_surface.DrawingMode = DrawingModes.Arrow;
			RefreshFieldControls();
		}

		private void BtnCropClick(object sender, EventArgs e)
		{
			_surface.DrawingMode = DrawingModes.Crop;
			RefreshFieldControls();
		}

		private void BtnHighlightClick(object sender, EventArgs e)
		{
			_surface.DrawingMode = DrawingModes.Highlight;
			RefreshFieldControls();
		}

		private void BtnObfuscateClick(object sender, EventArgs e)
		{
			_surface.DrawingMode = DrawingModes.Obfuscate;
			RefreshFieldControls();
		}

		private void BtnFreehandClick(object sender, EventArgs e)
		{
			_surface.DrawingMode = DrawingModes.Path;
			RefreshFieldControls();
		}

		private void SetButtonChecked(ToolStripButton btn)
		{
			UncheckAllToolButtons();
			btn.Checked = true;
		}

		private void UncheckAllToolButtons()
		{
			if (_toolbarButtons != null)
			{
				foreach (ToolStripButton butt in _toolbarButtons)
				{
					butt.Checked = false;
				}
			}
		}

		private void AddRectangleToolStripMenuItemClick(object sender, EventArgs e)
		{
			BtnRectClick(sender, e);
		}

		private void DrawFreehandToolStripMenuItemClick(object sender, EventArgs e)
		{
			BtnFreehandClick(sender, e);
		}

		private void AddEllipseToolStripMenuItemClick(object sender, EventArgs e)
		{
			BtnEllipseClick(sender, e);
		}

		private void AddTextBoxToolStripMenuItemClick(object sender, EventArgs e)
		{
			BtnTextClick(sender, e);
		}

		private void AddSpeechBubbleToolStripMenuItemClick(object sender, EventArgs e)
		{
			BtnSpeechBubbleClick(sender, e);
		}

		private void AddCounterToolStripMenuItemClick(object sender, EventArgs e)
		{
			BtnStepLabelClick(sender, e);
		}

		private void DrawLineToolStripMenuItemClick(object sender, EventArgs e)
		{
			BtnLineClick(sender, e);
		}

		private void DrawArrowToolStripMenuItemClick(object sender, EventArgs e)
		{
			BtnArrowClick(sender, e);
		}

		private void RemoveObjectToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.RemoveSelectedElements();
		}

		private void BtnDeleteClick(object sender, EventArgs e)
		{
			RemoveObjectToolStripMenuItemClick(sender, e);
		}

		#endregion

		#region copy&paste options

		private void CutToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.CutSelectedElements();
			UpdateClipboardSurfaceDependencies();
		}

		private void BtnCutClick(object sender, EventArgs e)
		{
			CutToolStripMenuItemClick(sender, e);
		}

		private void CopyToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.CopySelectedElements();
			UpdateClipboardSurfaceDependencies();
		}

		private void BtnCopyClick(object sender, EventArgs e)
		{
			CopyToolStripMenuItemClick(sender, e);
		}

		private void PasteToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.PasteElementFromClipboard();
			UpdateClipboardSurfaceDependencies();
		}

		private void BtnPasteClick(object sender, EventArgs e)
		{
			PasteToolStripMenuItemClick(sender, e);
		}

		private void UndoToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.Undo();
			UpdateUndoRedoSurfaceDependencies();
		}

		private void BtnUndoClick(object sender, EventArgs e)
		{
			UndoToolStripMenuItemClick(sender, e);
		}

		private void RedoToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.Redo();
			UpdateUndoRedoSurfaceDependencies();
		}

		private void BtnRedoClick(object sender, EventArgs e)
		{
			RedoToolStripMenuItemClick(sender, e);
		}

		private void DuplicateToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.DuplicateSelectedElements();
			UpdateClipboardSurfaceDependencies();
		}

		#endregion

		#region element properties

		private void UpOneLevelToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.PullElementsUp();
		}

		private void DownOneLevelToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.PushElementsDown();
		}

		private void UpToTopToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.PullElementsToTop();
		}

		private void DownToBottomToolStripMenuItemClick(object sender, EventArgs e)
		{
			_surface.PushElementsToBottom();
		}

		#endregion

		#region help

		private async void HelpToolStripMenuItem1Click(object sender, EventArgs e)
		{
			await HelpFileLoader.LoadHelpAsync();
		}

		private void AboutToolStripMenuItemClick(object sender, EventArgs e)
		{
			PluginUtils.Host.ShowAbout();
		}

		private void PreferencesToolStripMenuItemClick(object sender, EventArgs e)
		{
			PluginUtils.Host.ShowSettings();
		}

		private void BtnSettingsClick(object sender, EventArgs e)
		{
			PreferencesToolStripMenuItemClick(sender, e);
		}

		private void BtnHelpClick(object sender, EventArgs e)
		{
			HelpToolStripMenuItem1Click(sender, e);
		}

		#endregion

		#region image editor event handlers

		private void ImageEditorFormActivated(object sender, EventArgs e)
		{
			UpdateClipboardSurfaceDependencies();
			UpdateUndoRedoSurfaceDependencies();
		}

		private async void ImageEditorFormFormClosingAsync(object sender, FormClosingEventArgs e)
		{
			if (_surface.Modified && !editorConfiguration.SuppressSaveDialogAtClose)
			{
				// Make sure the editor is visible
				WindowDetails.ToForeground(Handle);

				MessageBoxButtons buttons = MessageBoxButtons.YesNoCancel;
				// Dissallow "CANCEL" if the application needs to shutdown
				if ((e.CloseReason == CloseReason.ApplicationExitCall) || (e.CloseReason == CloseReason.WindowsShutDown) || (e.CloseReason == CloseReason.TaskManagerClosing))
				{
					buttons = MessageBoxButtons.YesNo;
				}
				DialogResult result = MessageBox.Show(editorLanguage.EditorCloseOnSave, editorLanguage.EditorCloseOnSaveTitle, buttons, MessageBoxIcon.Question);
				if (result.Equals(DialogResult.Cancel))
				{
					e.Cancel = true;
					return;
				}
				if (result.Equals(DialogResult.Yes))
				{
					await SaveAsync();
					// Check if the save was made, if not it was cancelled so we cancel the closing
					if (_surface.Modified)
					{
						e.Cancel = true;
						return;
					}
				}
			}
			// persist our geometry string.
			SetEditorPlacement(new WindowDetails(Handle).WindowPlacement);

			// remove from the editor list
			editorList.Remove(this);

			_surface.Dispose();

			GC.Collect();
			if (coreConfiguration.MinimizeWorkingSetSize)
			{
				PsAPI.EmptyWorkingSet();
			}
		}

		private async Task ImageEditorFormKeyDownAsync(object sender, KeyEventArgs e, CancellationToken token = default(CancellationToken))
		{
			// Log.Debug().WriteLine("Got key event {0}, {1}", e.KeyCode, e.Modifiers);
			// avoid conflict with other shortcuts and
			// make sure there's no selected element claiming input focus
			if (e.Modifiers.Equals(Keys.None) && !_surface.KeysLocked)
			{
				switch (e.KeyCode)
				{
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
			}
			else if (e.Modifiers.Equals(Keys.Control))
			{
				switch (e.KeyCode)
				{
					case Keys.Z:
						UndoToolStripMenuItemClick(sender, e);
						break;
					case Keys.Y:
						RedoToolStripMenuItemClick(sender, e);
						break;
					case Keys.Q: // Dropshadow Ctrl + Q
						await AddDropshadowToolStripMenuItemClickAsync(token);
						break;
					case Keys.B: // Border Ctrl + B
						await AddBorderToolStripMenuItemClickAsync(token);
						break;
					case Keys.T: // Torn edge Ctrl + T
						await TornEdgesToolStripMenuItemClickAsync(token);
						break;
					case Keys.I: // Invert Ctrl + I
						await InvertToolStripMenuItemClickAsync(token);
						break;
					case Keys.G: // Grayscale Ctrl + G
						await GrayscaleToolStripMenuItemClickAsync(token);
						break;
					case Keys.Delete: // Grayscale Ctrl + Delete
						ClearToolStripMenuItemClick(sender, e);
						break;
					case Keys.Oemcomma: // Rotate CCW Ctrl + ,
						await RotateCcwToolstripButtonClickAsync(token);
						break;
					case Keys.OemPeriod: // Rotate CW Ctrl + .
						await RotateCwToolstripButtonClickAsync(token);
						break;
					case Keys.Add: // Ctrl + +
						await EnlargeCanvasToolStripMenuItemClickAsync(token);
						break;
					case Keys.Subtract: // Ctrl + -
						ShrinkCanvasToolStripMenuItemClick(sender, e);
						break;
				}
			}
		}

		/// <summary>
		///     This is a "work-around" for the MouseWheel event which doesn't get to the panel
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PanelMouseWheel(object sender, MouseEventArgs e)
		{
			surfacePanel.Focus();
		}

		#endregion

		#region key handling

		protected override bool ProcessKeyPreview(ref Message msg)
		{
			// disable default key handling if surface has requested a lock
			if (!_surface.KeysLocked)
			{
				return base.ProcessKeyPreview(ref msg);
			}
			return false;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keys)
		{
			// disable default key handling if surface has requested a lock
			if (!_surface.KeysLocked)
			{
				// Go through the destinations to check the EditorShortcut Keys
				// this way the menu entries don't need to be enabled.
				// This also fixes bugs #3526974 & #3527020
				foreach (var destination in new IDestination[] {})
				{
					if (ignoreDestinations.Contains(destination.Designation))
					{
						continue;
					}
					if (!destination.IsEnabled)
					{
					}

					// TODO: Destination Hotkeys?
					//if (destination.EditorShortcutKeys == keys)
					//{
					//	this.InvokeAsync(async () =>
					//	{
					//		await destination.ExportCaptureAsync(true, _surface).ConfigureAwait(false);
					//	});
					//	return true;
					//}
				}
				if (!_surface.ProcessCmdKey(keys))
				{
					return base.ProcessCmdKey(ref msg, keys);
				}
			}
			return false;
		}

		#endregion

		#region helpers

		private void UpdateUndoRedoSurfaceDependencies()
		{
			if (_surface == null)
			{
				return;
			}
			bool canUndo = _surface.CanUndo;
			btnUndo.Enabled = canUndo;
			undoToolStripMenuItem.Enabled = canUndo;
			// TODO: Include redo action
			string undoText = string.Format(editorLanguage.EditorUndo, "");
			btnUndo.Text = undoText;
			undoToolStripMenuItem.Text = undoText;

			bool canRedo = _surface.CanRedo;
			btnRedo.Enabled = canRedo;
			redoToolStripMenuItem.Enabled = canRedo;
			// TODO: Include redo action
			string redoText = string.Format(editorLanguage.EditorRedo, "");
			btnRedo.Text = redoText;
			redoToolStripMenuItem.Text = redoText;
		}

		private void UpdateClipboardSurfaceDependencies()
		{
			if (_surface == null)
			{
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

		private void UpdateStatusLabel(string text, ContextMenuStrip contextMenu)
		{
			statusLabel.Text = text;
			statusStrip1.ContextMenuStrip = contextMenu;
		}

		private void UpdateStatusLabel(string text)
		{
			UpdateStatusLabel(text, null);
		}

		private void ClearStatusLabel()
		{
			UpdateStatusLabel(null, null);
		}

		private void StatusLabelClicked(object sender, MouseEventArgs e)
		{
			var statusStrip = (StatusStrip) ((ToolStripStatusLabel) sender).Owner;
			if (statusStrip.ContextMenuStrip != null)
			{
				statusStrip.ContextMenuStrip.Show(statusStrip, e.X, e.Y);
			}
		}

		private void CopyPathMenuItemClick(object sender, EventArgs e)
		{
			ClipboardHelper.SetClipboardData(_surface.LastSaveFullPath);
		}

		private void OpenDirectoryMenuItemClick(object sender, EventArgs e)
		{
			var psi = new ProcessStartInfo("explorer")
			{
				Arguments = Path.GetDirectoryName(_surface.LastSaveFullPath), UseShellExecute = false
			};
			using (var p = new Process())
			{
				p.StartInfo = psi;
				p.Start();
			}
		}

		#endregion
	}
}