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

using Greenshot.Addon.LegacyEditor.Controls;
using Greenshot.Addon.LegacyEditor.Drawing;
using Greenshot.Addons.Controls;

namespace Greenshot.Addon.LegacyEditor.Forms {
	partial class ImageEditorForm {
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
            _destinationScaleHandler.Dispose();
            // Make sure that clipboard changes are not longer processed.
            _clipboardSubscription?.Dispose();
			// Remove all other stuff
			_disposables.Dispose();
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			this.topToolStripContainer = new System.Windows.Forms.ToolStripContainer();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.dimensionsLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.panel1 = new NonJumpingPanel();
			this.toolsToolStrip = new ToolStripEx();
			this.btnCursor = new GreenshotToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.btnRect = new GreenshotToolStripButton();
			this.btnEllipse = new GreenshotToolStripButton();
			this.btnLine = new GreenshotToolStripButton();
			this.btnArrow = new GreenshotToolStripButton();
			this.btnFreehand = new GreenshotToolStripButton();
			this.btnText = new GreenshotToolStripButton();
			this.btnSpeechBubble = new GreenshotToolStripButton();
			this.btnStepLabel = new GreenshotToolStripButton();
			this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
			this.btnHighlight = new GreenshotToolStripButton();
			this.btnObfuscate = new GreenshotToolStripButton();
			this.toolStripSplitButton1 = new GreenshotToolStripDropDownButton();
			this.addBorderToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.addDropshadowToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.tornEdgesToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.grayscaleToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.invertToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.btnResize = new GreenshotToolStripButton();
			this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
			this.btnCrop = new GreenshotToolStripButton();
			this.rotateCwToolstripButton = new GreenshotToolStripButton();
			this.rotateCcwToolstripButton = new GreenshotToolStripButton();
			this.menuStrip1 = new MenuStripEx();
			this.fileStripMenuItem = new GreenshotToolStripMenuItem();
			this.editToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator15 = new System.Windows.Forms.ToolStripSeparator();
			this.cutToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.copyToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.pasteToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.duplicateToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
			this.preferencesToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.autoCropToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.toolStripSeparator17 = new System.Windows.Forms.ToolStripSeparator();
			this.insert_window_toolstripmenuitem = new GreenshotToolStripMenuItem();
			this.objectToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.addRectangleToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.addEllipseToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.drawLineToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.drawArrowToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.drawFreehandToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.addTextBoxToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.addSpeechBubbleToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.addCounterToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.selectAllToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.removeObjectToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.arrangeToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.upToTopToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.upOneLevelToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.downOneLevelToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.downToBottomToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.saveElementsToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.loadElementsToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.pluginToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.helpToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.helpToolStripMenuItem1 = new GreenshotToolStripMenuItem();
			this.aboutToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.destinationsToolStrip = new ToolStripEx();
			this.btnSave = new GreenshotToolStripButton();
			this.btnClipboard = new GreenshotToolStripButton();
			this.btnPrint = new GreenshotToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.btnDelete = new GreenshotToolStripButton();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.btnCut = new GreenshotToolStripButton();
			this.btnCopy = new GreenshotToolStripButton();
			this.btnPaste = new GreenshotToolStripButton();
			this.btnUndo = new System.Windows.Forms.ToolStripButton();
			this.btnRedo = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.btnSettings = new GreenshotToolStripButton();
			this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator16 = new System.Windows.Forms.ToolStripSeparator();
			this.btnHelp = new GreenshotToolStripButton();
			this.propertiesToolStrip = new ToolStripEx();
			this.obfuscateModeButton = new BindableToolStripDropDownButton();
			this.pixelizeToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.blurToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.highlightModeButton = new BindableToolStripDropDownButton();
			this.textHighlightMenuItem = new GreenshotToolStripMenuItem();
			this.areaHighlightMenuItem = new GreenshotToolStripMenuItem();
			this.grayscaleHighlightMenuItem = new GreenshotToolStripMenuItem();
			this.magnifyMenuItem = new GreenshotToolStripMenuItem();
			this.btnFillColor = new ToolStripColorButton(_editorConfiguration, _greenshotLanguage);
			this.btnLineColor = new ToolStripColorButton(_editorConfiguration, _greenshotLanguage);
			this.lineThicknessLabel = new GreenshotToolStripLabel();
			this.lineThicknessUpDown = new ToolStripNumericUpDown();
			this.counterLabel = new GreenshotToolStripLabel();
			this.counterUpDown = new ToolStripNumericUpDown();
			this.fontFamilyComboBox = new FontFamilyComboBox();
			this.fontSizeLabel = new GreenshotToolStripLabel();
			this.fontSizeUpDown = new ToolStripNumericUpDown();
			this.fontBoldButton = new BindableToolStripButton();
			this.fontItalicButton = new BindableToolStripButton();
			this.textVerticalAlignmentButton = new BindableToolStripDropDownButton();
			this.alignTopToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.alignMiddleToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.alignBottomToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.blurRadiusLabel = new GreenshotToolStripLabel();
			this.blurRadiusUpDown = new ToolStripNumericUpDown();
			this.brightnessLabel = new GreenshotToolStripLabel();
			this.brightnessUpDown = new ToolStripNumericUpDown();
			this.magnificationFactorLabel = new GreenshotToolStripLabel();
			this.magnificationFactorUpDown = new ToolStripNumericUpDown();
			this.pixelSizeLabel = new GreenshotToolStripLabel();
			this.pixelSizeUpDown = new ToolStripNumericUpDown();
			this.arrowHeadsLabel = new GreenshotToolStripLabel();
			this.arrowHeadsDropDownButton = new GreenshotToolStripDropDownButton();
			this.arrowHeadStartMenuItem = new GreenshotToolStripMenuItem();
			this.arrowHeadEndMenuItem = new GreenshotToolStripMenuItem();
			this.arrowHeadBothMenuItem = new GreenshotToolStripMenuItem();
			this.arrowHeadNoneMenuItem = new GreenshotToolStripMenuItem();
			this.shadowButton = new BindableToolStripButton();
			this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.btnConfirm = new BindableToolStripButton();
			this.btnCancel = new BindableToolStripButton();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.closeToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.fileSavedStatusContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.copyPathMenuItem = new GreenshotToolStripMenuItem();
			this.openDirectoryMenuItem = new GreenshotToolStripMenuItem();
			this.textHorizontalAlignmentButton = new BindableToolStripDropDownButton();
			this.alignLeftToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.alignCenterToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.alignRightToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.topToolStripContainer.BottomToolStripPanel.SuspendLayout();
			this.topToolStripContainer.ContentPanel.SuspendLayout();
			this.topToolStripContainer.LeftToolStripPanel.SuspendLayout();
			this.topToolStripContainer.TopToolStripPanel.SuspendLayout();
			this.topToolStripContainer.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.toolsToolStrip.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.destinationsToolStrip.SuspendLayout();
			this.propertiesToolStrip.SuspendLayout();
			this.fileSavedStatusContextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// topToolStripContainer
			// 
			this.topToolStripContainer.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.topToolStripContainer.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			// 
			// topToolStripContainer.BottomToolStripPanel
			// 
			this.topToolStripContainer.BottomToolStripPanel.Controls.Add(this.statusStrip1);
			// 
			// topToolStripContainer.ContentPanel
			// 
			this.topToolStripContainer.ContentPanel.AutoScroll = true;
			this.topToolStripContainer.ContentPanel.Controls.Add(this.tableLayoutPanel1);
			this.topToolStripContainer.ContentPanel.Size = new System.Drawing.Size(761, 385);
			this.topToolStripContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.topToolStripContainer.LeftToolStripPanel.Join(this.toolsToolStrip,0);
			this.topToolStripContainer.Location = new System.Drawing.Point(0, 0);
			this.topToolStripContainer.Name = "toolStripContainer1";
			this.topToolStripContainer.Size = new System.Drawing.Size(785, 485);
			this.topToolStripContainer.TabIndex = 2;
			this.topToolStripContainer.Text = "toolStripContainer1";
			this.topToolStripContainer.TopToolStripPanel.Join(this.menuStrip1,0);
			this.topToolStripContainer.TopToolStripPanel.Join(this.destinationsToolStrip, 1);
			this.topToolStripContainer.TopToolStripPanel.Join(this.propertiesToolStrip, 2);
			// 
			// statusStrip1
			// 
			this.statusStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.dimensionsLabel,
									this.statusLabel});
			this.statusStrip1.Location = new System.Drawing.Point(0, 0);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(785, 24);
			this.statusStrip1.TabIndex = 3;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// dimensionsLabel
			// 
			this.dimensionsLabel.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
									| System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
									| System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.dimensionsLabel.BorderStyle = System.Windows.Forms.Border3DStyle.Sunken;
			this.dimensionsLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.dimensionsLabel.Name = "dimensionsLabel";
			this.dimensionsLabel.Text = "123x321";
			// 
			// statusLabel
			// 
			this.statusLabel.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
									| System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
									| System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.statusLabel.BorderStyle = System.Windows.Forms.Border3DStyle.Sunken;
			this.statusLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.statusLabel.Name = "statusLabel";
			this.statusLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.StatusLabelClicked);
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.BackColor = System.Drawing.SystemColors.Control;
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 1;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 385F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(761, 385);
			this.tableLayoutPanel1.TabIndex = 3;
			// 
			// panel1
			// 
			this.panel1.AutoScroll = true;
			this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(3, 3);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(755, 379);
			this.panel1.TabIndex = 2;
			// 
			// toolsToolStrip
			// 
			this.toolsToolStrip.ClickThrough = true;
			this.toolsToolStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.toolsToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolsToolStrip.Renderer = new CustomToolStripProfessionalRenderer();
			this.toolsToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.btnCursor,
									this.toolStripSeparator1,
									this.btnRect,
									this.btnEllipse,
									this.btnLine,
									this.btnArrow,
									this.btnFreehand,
									this.btnText,
									this.btnSpeechBubble,
									this.btnStepLabel,
									this.toolStripSeparator14,
									this.btnHighlight,
									this.btnObfuscate,
									this.toolStripSplitButton1,
									this.toolStripSeparator13,
									this.btnCrop,
									this.rotateCwToolstripButton,
									this.rotateCcwToolstripButton,
									this.btnResize});
			this.toolsToolStrip.Name = "toolsToolStrip";
			this.toolsToolStrip.Stretch = true;
			this.toolsToolStrip.TabIndex = 0;
			this.toolsToolStrip.BackColor = System.Drawing.SystemColors.Control;
			this.toolsToolStrip.OverflowButton.DropDown.BackColor = System.Drawing.SystemColors.Control;
			// 
			// btnCursor
			// 
			this.btnCursor.Checked = true;
			this.btnCursor.CheckOnClick = true;
			this.btnCursor.CheckState = System.Windows.Forms.CheckState.Checked;
			this.btnCursor.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnCursor.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCursor.LanguageKey = "editor.editor_cursortool";
			this.btnCursor.Name = "btnCursor";
			this.btnCursor.Click += new System.EventHandler(this.BtnCursorClick);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			// 
			// btnRect
			// 
			this.btnRect.CheckOnClick = true;
			this.btnRect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnRect.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnRect.LanguageKey = "editor.editor_drawrectangle";
			this.btnRect.Name = "btnRect";
			this.btnRect.Click += new System.EventHandler(this.BtnRectClick);
			// 
			// btnEllipse
			// 
			this.btnEllipse.CheckOnClick = true;
			this.btnEllipse.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnEllipse.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnEllipse.LanguageKey = "editor.editor_drawellipse";
			this.btnEllipse.Name = "btnEllipse";
			this.btnEllipse.Click += new System.EventHandler(this.BtnEllipseClick);
			// 
			// btnLine
			// 
			this.btnLine.CheckOnClick = true;
			this.btnLine.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnLine.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnLine.LanguageKey = "editor.editor_drawline";
			this.btnLine.Name = "btnLine";
			this.btnLine.Click += new System.EventHandler(this.BtnLineClick);
			// 
			// btnArrow
			// 
			this.btnArrow.CheckOnClick = true;
			this.btnArrow.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnArrow.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnArrow.LanguageKey = "editor.editor_drawarrow";
			this.btnArrow.Name = "btnArrow";
			this.btnArrow.Click += new System.EventHandler(this.BtnArrowClick);
			// 
			// btnFreehand
			// 
			this.btnFreehand.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnFreehand.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnFreehand.LanguageKey = "editor.editor_drawfreehand";
			this.btnFreehand.Name = "btnFreehand";
			this.btnFreehand.Click += new System.EventHandler(this.BtnFreehandClick);
			// 
			// btnText
			// 
			this.btnText.CheckOnClick = true;
			this.btnText.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnText.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnText.LanguageKey = "editor.editor_drawtextbox";
			this.btnText.Name = "btnText";
			this.btnText.Click += new System.EventHandler(this.BtnTextClick);
			// 
			// btnSpeechBubble
			// 
			this.btnSpeechBubble.CheckOnClick = true;
			this.btnSpeechBubble.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnSpeechBubble.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnSpeechBubble.LanguageKey = "editor.editor_speechbubble";
			this.btnSpeechBubble.Name = "btnSpeechBubble";
			this.btnSpeechBubble.Click += new System.EventHandler(this.BtnSpeechBubbleClick);
			// 
			// btnStepLabel
			// 
			this.btnStepLabel.CheckOnClick = true;
			this.btnStepLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnStepLabel.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnStepLabel.LanguageKey = "editor.editor_counter";
			this.btnStepLabel.Name = "btnStepLabel";
			this.btnStepLabel.Click += new System.EventHandler(this.BtnStepLabelClick);
			// 
			// toolStripSeparator14
			// 
			this.toolStripSeparator14.Name = "toolStripSeparator14";
			// 
			// btnHighlight
			// 
			this.btnHighlight.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnHighlight.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnHighlight.LanguageKey = "editor.editor_drawhighlighter";
			this.btnHighlight.Name = "btnHighlight";
			this.btnHighlight.Click += new System.EventHandler(this.BtnHighlightClick);
			// 
			// btnObfuscate
			// 
			this.btnObfuscate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnObfuscate.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnObfuscate.LanguageKey = "editor.editor_obfuscate";
			this.btnObfuscate.Name = "btnObfuscate";
			this.btnObfuscate.Click += new System.EventHandler(this.BtnObfuscateClick);
			// 
			// toolStripSplitButton1
			// 
			this.toolStripSplitButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripSplitButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.addBorderToolStripMenuItem,
									this.addDropshadowToolStripMenuItem,
									this.tornEdgesToolStripMenuItem,
									this.grayscaleToolStripMenuItem,
									this.invertToolStripMenuItem});
			this.toolStripSplitButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripSplitButton1.LanguageKey = "editor.editor_effects";
			this.toolStripSplitButton1.Name = "toolStripSplitButton1";
			this.toolStripSplitButton1.ShowDropDownArrow = false;
			this.toolStripSplitButton1.Text = "toolStripSplitButton1";
			// 
			// addBorderToolStripMenuItem
			// 
			this.addBorderToolStripMenuItem.LanguageKey = "editor.editor_border";
			this.addBorderToolStripMenuItem.Name = "addBorderToolStripMenuItem";
			this.addBorderToolStripMenuItem.Click += new System.EventHandler(this.AddBorderToolStripMenuItemClick);
			// 
			// addDropshadowToolStripMenuItem
			// 
			this.addDropshadowToolStripMenuItem.LanguageKey = "editor.editor_image_shadow";
			this.addDropshadowToolStripMenuItem.Name = "addDropshadowToolStripMenuItem";
			this.addDropshadowToolStripMenuItem.MouseUp += AddDropshadowToolStripMenuItemMouseUp;
			// 
			// tornEdgesToolStripMenuItem
			// 
			this.tornEdgesToolStripMenuItem.LanguageKey = "editor.editor_torn_edge";
			this.tornEdgesToolStripMenuItem.Name = "tornEdgesToolStripMenuItem";
			this.tornEdgesToolStripMenuItem.MouseUp += TornEdgesToolStripMenuItemMouseUp;
			// 
			// grayscaleToolStripMenuItem
			// 
			this.grayscaleToolStripMenuItem.LanguageKey = "editor.editor_grayscale";
			this.grayscaleToolStripMenuItem.Name = "grayscaleToolStripMenuItem";
			this.grayscaleToolStripMenuItem.Click += new System.EventHandler(this.GrayscaleToolStripMenuItemClick);
			// 
			// invertToolStripMenuItem
			// 
			this.invertToolStripMenuItem.LanguageKey = "editor.editor_invert";
			this.invertToolStripMenuItem.Name = "invertToolStripMenuItem";
			this.invertToolStripMenuItem.Click += new System.EventHandler(this.InvertToolStripMenuItemClick);
			// 
			// btnResize
			// 
			this.btnResize.Name = "btnResize";
			this.btnResize.Click += new System.EventHandler(this.BtnResizeClick);
			this.btnResize.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnResize.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnResize.LanguageKey = "editor.editor_resize";
			// 
			// toolStripSeparator13
			// 
			this.toolStripSeparator13.Name = "toolStripSeparator13";
			// 
			// btnCrop
			// 
			this.btnCrop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnCrop.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCrop.LanguageKey = "editor.editor_crop";
			this.btnCrop.Name = "btnCrop";
			this.btnCrop.Click += new System.EventHandler(this.BtnCropClick);
			// 
			// rotateCwToolstripButton
			// 
			this.rotateCwToolstripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.rotateCwToolstripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.rotateCwToolstripButton.LanguageKey = "editor.editor_rotatecw";
			this.rotateCwToolstripButton.Name = "rotateCwToolstripButton";
			this.rotateCwToolstripButton.Click += new System.EventHandler(this.RotateCwToolstripButtonClick);
			// 
			// rotateCcwToolstripButton
			// 
			this.rotateCcwToolstripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.rotateCcwToolstripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.rotateCcwToolstripButton.LanguageKey = "editor.editor_rotateccw";
			this.rotateCcwToolstripButton.Name = "rotateCcwToolstripButton";
			this.rotateCcwToolstripButton.Click += new System.EventHandler(this.RotateCcwToolstripButtonClick);
			// 
			// menuStrip1
			// 
			this.menuStrip1.ClickThrough = true;
			this.menuStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.menuStrip1.Stretch = true;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.fileStripMenuItem,
									this.editToolStripMenuItem,
									this.objectToolStripMenuItem,
									this.pluginToolStripMenuItem,
									this.helpToolStripMenuItem});
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.BackColor = System.Drawing.SystemColors.Control;
			this.menuStrip1.TabIndex = 1;
			// 
			// fileStripMenuItem
			// 
			this.fileStripMenuItem.LanguageKey = "editor.editor_file";
			this.fileStripMenuItem.Name = "fileStripMenuItem";
			this.fileStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileStripMenuItem.Text = "File";
			this.fileStripMenuItem.DropDownOpening += new System.EventHandler(this.FileMenuDropDownOpening);
			// Fix for BUG-1653, the DropDownOpening is not called when there are no children.
			this.fileStripMenuItem.DropDownItems.Add(toolStripSeparator9);
			// 
			// editToolStripMenuItem
			// 
			this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.undoToolStripMenuItem,
									this.redoToolStripMenuItem,
									this.toolStripSeparator15,
									this.cutToolStripMenuItem,
									this.copyToolStripMenuItem,
									this.pasteToolStripMenuItem,
									this.toolStripSeparator4,
									this.duplicateToolStripMenuItem,
									this.toolStripSeparator12,
									this.preferencesToolStripMenuItem,
									this.toolStripSeparator5,
									this.autoCropToolStripMenuItem,
									this.toolStripSeparator17,
									this.insert_window_toolstripmenuitem});
			this.editToolStripMenuItem.LanguageKey = "editor.editor_edit";
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
			this.editToolStripMenuItem.Text = "Edit";
			this.editToolStripMenuItem.Click += new System.EventHandler(this.EditToolStripMenuItemClick);
			// 
			// undoToolStripMenuItem
			// 
			this.undoToolStripMenuItem.Enabled = false;
			this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
			this.undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
			this.undoToolStripMenuItem.Text = "Undo";
			this.undoToolStripMenuItem.Click += new System.EventHandler(this.UndoToolStripMenuItemClick);
			// 
			// redoToolStripMenuItem
			// 
			this.redoToolStripMenuItem.Enabled = false;
			this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
			this.redoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
			this.redoToolStripMenuItem.Text = "Redo";
			this.redoToolStripMenuItem.Click += new System.EventHandler(this.RedoToolStripMenuItemClick);
			// 
			// toolStripSeparator15
			// 
			this.toolStripSeparator15.Name = "toolStripSeparator15";
			// 
			// cutToolStripMenuItem
			// 
			this.cutToolStripMenuItem.Enabled = false;
			this.cutToolStripMenuItem.LanguageKey = "editor.editor_cuttoclipboard";
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.cutToolStripMenuItem.Click += new System.EventHandler(this.CutToolStripMenuItemClick);
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.Enabled = false;
			this.copyToolStripMenuItem.LanguageKey = "editor.editor_copytoclipboard";
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.CopyToolStripMenuItemClick);
			// 
			// pasteToolStripMenuItem
			// 
			this.pasteToolStripMenuItem.Enabled = false;
			this.pasteToolStripMenuItem.LanguageKey = "editor.editor_pastefromclipboard";
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			this.pasteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.pasteToolStripMenuItem.Click += new System.EventHandler(this.PasteToolStripMenuItemClick);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			// 
			// duplicateToolStripMenuItem
			// 
			this.duplicateToolStripMenuItem.Enabled = false;
			this.duplicateToolStripMenuItem.LanguageKey = "editor.editor_duplicate";
			this.duplicateToolStripMenuItem.Name = "duplicateToolStripMenuItem";
			this.duplicateToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.duplicateToolStripMenuItem.Click += new System.EventHandler(this.DuplicateToolStripMenuItemClick);
			// 
			// toolStripSeparator12
			// 
			this.toolStripSeparator12.Name = "toolStripSeparator12";
			// 
			// preferencesToolStripMenuItem
			// 
			this.preferencesToolStripMenuItem.LanguageKey = "contextmenu_settings";
			this.preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
			this.preferencesToolStripMenuItem.Click += new System.EventHandler(this.PreferencesToolStripMenuItemClick);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			// 
			// autoCropToolStripMenuItem
			// 
			this.autoCropToolStripMenuItem.LanguageKey = "editor.editor_autocrop";
			this.autoCropToolStripMenuItem.Name = "autoCropToolStripMenuItem";
			this.autoCropToolStripMenuItem.Click += new System.EventHandler(this.AutoCropToolStripMenuItemClick);
			// 
			// toolStripSeparator17
			// 
			this.toolStripSeparator17.Name = "toolStripSeparator17";
			// 
			// insert_window_toolstripmenuitem
			// 
			this.insert_window_toolstripmenuitem.LanguageKey = "editor.editor_insertwindow";
			this.insert_window_toolstripmenuitem.Name = "insert_window_toolstripmenuitem";
			this.insert_window_toolstripmenuitem.MouseEnter += new System.EventHandler(this.Insert_window_toolstripmenuitemMouseEnter);
			// 
			// objectToolStripMenuItem
			// 
			this.objectToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.addRectangleToolStripMenuItem,
									this.addEllipseToolStripMenuItem,
									this.drawLineToolStripMenuItem,
									this.drawArrowToolStripMenuItem,
									this.drawFreehandToolStripMenuItem,
									this.addTextBoxToolStripMenuItem,
									this.addSpeechBubbleToolStripMenuItem,
									this.addCounterToolStripMenuItem,
									this.toolStripSeparator8,
									this.selectAllToolStripMenuItem,
									this.removeObjectToolStripMenuItem,
									this.toolStripSeparator7,
									this.arrangeToolStripMenuItem,
									this.saveElementsToolStripMenuItem,
									this.loadElementsToolStripMenuItem});
			this.objectToolStripMenuItem.LanguageKey = "editor.editor_object";
			this.objectToolStripMenuItem.Name = "objectToolStripMenuItem";
			this.objectToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
			this.objectToolStripMenuItem.Text = "Object";
			// 
			// addRectangleToolStripMenuItem
			// 
			this.addRectangleToolStripMenuItem.LanguageKey = "editor.editor_drawrectangle";
			this.addRectangleToolStripMenuItem.Name = "addRectangleToolStripMenuItem";
			this.addRectangleToolStripMenuItem.Click += new System.EventHandler(this.AddRectangleToolStripMenuItemClick);
			// 
			// addEllipseToolStripMenuItem
			// 
			this.addEllipseToolStripMenuItem.LanguageKey = "editor.editor_drawellipse";
			this.addEllipseToolStripMenuItem.Name = "addEllipseToolStripMenuItem";
			this.addEllipseToolStripMenuItem.Click += new System.EventHandler(this.AddEllipseToolStripMenuItemClick);
			// 
			// drawLineToolStripMenuItem
			// 
			this.drawLineToolStripMenuItem.LanguageKey = "editor.editor_drawline";
			this.drawLineToolStripMenuItem.Name = "drawLineToolStripMenuItem";
			this.drawLineToolStripMenuItem.Click += new System.EventHandler(this.DrawLineToolStripMenuItemClick);
			// 
			// drawArrowToolStripMenuItem
			// 
			this.drawArrowToolStripMenuItem.LanguageKey = "editor.editor_drawarrow";
			this.drawArrowToolStripMenuItem.Name = "drawArrowToolStripMenuItem";
			this.drawArrowToolStripMenuItem.Click += new System.EventHandler(this.DrawArrowToolStripMenuItemClick);
			// 
			// drawFreehandToolStripMenuItem
			// 
			this.drawFreehandToolStripMenuItem.LanguageKey = "editor.editor_drawfreehand";
			this.drawFreehandToolStripMenuItem.Name = "drawFreehandToolStripMenuItem";
			this.drawFreehandToolStripMenuItem.Click += new System.EventHandler(this.DrawFreehandToolStripMenuItemClick);
			// 
			// addTextBoxToolStripMenuItem
			// 
			this.addTextBoxToolStripMenuItem.LanguageKey = "editor.editor_drawtextbox";
			this.addTextBoxToolStripMenuItem.Name = "addTextBoxToolStripMenuItem";
			this.addTextBoxToolStripMenuItem.Click += new System.EventHandler(this.AddTextBoxToolStripMenuItemClick);
			// 
			// addSpeechBubbleToolStripMenuItem
			// 
			this.addSpeechBubbleToolStripMenuItem.LanguageKey = "editor.editor_speechbubble";
			this.addSpeechBubbleToolStripMenuItem.Name = "addSpeechBubbleToolStripMenuItem";
			this.addSpeechBubbleToolStripMenuItem.Click += new System.EventHandler(this.AddSpeechBubbleToolStripMenuItemClick);
			// 
			// addCounterToolStripMenuItem
			// 
			this.addCounterToolStripMenuItem.LanguageKey = "editor.editor_counter";
			this.addCounterToolStripMenuItem.Name = "addCounterToolStripMenuItem";
			this.addCounterToolStripMenuItem.Click += new System.EventHandler(this.AddCounterToolStripMenuItemClick);
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			// 
			// selectAllToolStripMenuItem
			// 
			this.selectAllToolStripMenuItem.LanguageKey = "editor.editor_selectall";
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			this.selectAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.selectAllToolStripMenuItem.Click += new System.EventHandler(this.SelectAllToolStripMenuItemClick);
			// 
			// removeObjectToolStripMenuItem
			// 
			this.removeObjectToolStripMenuItem.Enabled = false;
			this.removeObjectToolStripMenuItem.LanguageKey = "editor.editor_deleteelement";
			this.removeObjectToolStripMenuItem.Name = "removeObjectToolStripMenuItem";
			this.removeObjectToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
			this.removeObjectToolStripMenuItem.Click += new System.EventHandler(this.RemoveObjectToolStripMenuItemClick);
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			// 
			// arrangeToolStripMenuItem
			// 
			this.arrangeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.upToTopToolStripMenuItem,
									this.upOneLevelToolStripMenuItem,
									this.downOneLevelToolStripMenuItem,
									this.downToBottomToolStripMenuItem});
			this.arrangeToolStripMenuItem.Enabled = false;
			this.arrangeToolStripMenuItem.LanguageKey = "editor.editor_arrange";
			this.arrangeToolStripMenuItem.Name = "arrangeToolStripMenuItem";
			// 
			// upToTopToolStripMenuItem
			// 
			this.upToTopToolStripMenuItem.Enabled = false;
			this.upToTopToolStripMenuItem.LanguageKey = "editor.editor_uptotop";
			this.upToTopToolStripMenuItem.Name = "upToTopToolStripMenuItem";
			this.upToTopToolStripMenuItem.ShortcutKeyDisplayString = "Home";
			this.upToTopToolStripMenuItem.Click += new System.EventHandler(this.UpToTopToolStripMenuItemClick);
			// 
			// upOneLevelToolStripMenuItem
			// 
			this.upOneLevelToolStripMenuItem.Enabled = false;
			this.upOneLevelToolStripMenuItem.LanguageKey = "editor.editor_uponelevel";
			this.upOneLevelToolStripMenuItem.Name = "upOneLevelToolStripMenuItem";
			this.upOneLevelToolStripMenuItem.ShortcutKeyDisplayString = "PgUp";
			this.upOneLevelToolStripMenuItem.Click += new System.EventHandler(this.UpOneLevelToolStripMenuItemClick);
			// 
			// downOneLevelToolStripMenuItem
			// 
			this.downOneLevelToolStripMenuItem.Enabled = false;
			this.downOneLevelToolStripMenuItem.LanguageKey = "editor.editor_downonelevel";
			this.downOneLevelToolStripMenuItem.Name = "downOneLevelToolStripMenuItem";
			this.downOneLevelToolStripMenuItem.ShortcutKeyDisplayString = "PgDn";
			this.downOneLevelToolStripMenuItem.Click += new System.EventHandler(this.DownOneLevelToolStripMenuItemClick);
			// 
			// downToBottomToolStripMenuItem
			// 
			this.downToBottomToolStripMenuItem.Enabled = false;
			this.downToBottomToolStripMenuItem.LanguageKey = "editor.editor_downtobottom";
			this.downToBottomToolStripMenuItem.Name = "downToBottomToolStripMenuItem";
			this.downToBottomToolStripMenuItem.ShortcutKeyDisplayString = "End";
			this.downToBottomToolStripMenuItem.Click += new System.EventHandler(this.DownToBottomToolStripMenuItemClick);
			// 
			// saveElementsToolStripMenuItem
			// 
			this.saveElementsToolStripMenuItem.LanguageKey = "editor.editor_save_objects";
			this.saveElementsToolStripMenuItem.Name = "saveElementsToolStripMenuItem";
			this.saveElementsToolStripMenuItem.Click += new System.EventHandler(this.SaveElementsToolStripMenuItemClick);
			// 
			// loadElementsToolStripMenuItem
			// 
			this.loadElementsToolStripMenuItem.LanguageKey = "editor.editor_load_objects";
			this.loadElementsToolStripMenuItem.Name = "loadElementsToolStripMenuItem";
			this.loadElementsToolStripMenuItem.Click += new System.EventHandler(this.LoadElementsToolStripMenuItemClick);
			// 
			// pluginToolStripMenuItem
			// 
			this.pluginToolStripMenuItem.LanguageKey = "settings_plugins";
			this.pluginToolStripMenuItem.Name = "pluginToolStripMenuItem";
			this.pluginToolStripMenuItem.Text = "Plugins";
			this.pluginToolStripMenuItem.Visible = false;
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.helpToolStripMenuItem1,
									this.aboutToolStripMenuItem});
			this.helpToolStripMenuItem.LanguageKey = "contextmenu_help";
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Text = "Help";
			// 
			// helpToolStripMenuItem1
			// 
			this.helpToolStripMenuItem1.LanguageKey = "contextmenu_help";
			this.helpToolStripMenuItem1.Name = "helpToolStripMenuItem1";
			this.helpToolStripMenuItem1.ShortcutKeys = System.Windows.Forms.Keys.F1;
			this.helpToolStripMenuItem1.Click += new System.EventHandler(this.HelpToolStripMenuItem1Click);
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.LanguageKey = "contextmenu_about";
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Click += new System.EventHandler(this.AboutToolStripMenuItemClick);
            // 
            // destinationsToolStrip
		    // 
		    this.destinationsToolStrip.AutoSize = false;
            this.destinationsToolStrip.ClickThrough = true;
			this.destinationsToolStrip.Dock = System.Windows.Forms.DockStyle.Fill;
			this.destinationsToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.destinationsToolStrip.Name = "toolStrip1";
			this.destinationsToolStrip.Stretch = true;
			this.destinationsToolStrip.TabIndex = 0;
			this.destinationsToolStrip.Renderer = new CustomToolStripProfessionalRenderer();
			this.destinationsToolStrip.BackColor = System.Drawing.SystemColors.Control;
			this.destinationsToolStrip.OverflowButton.DropDown.BackColor = System.Drawing.SystemColors.Control;
			this.destinationsToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.btnSave,
									this.btnClipboard,
									this.btnPrint,
									this.toolStripSeparator2,
									this.btnDelete,
									this.toolStripSeparator3,
									this.btnCut,
									this.btnCopy,
									this.btnPaste,
									this.btnUndo,
									this.btnRedo,
									this.toolStripSeparator6,
									this.btnSettings,
									this.toolStripSeparator11,
									this.toolStripSeparator16,
									this.btnHelp});
			// 
			// btnSave
			// 
			this.btnSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnSave.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnSave.LanguageKey = "editor.editor_save";
			this.btnSave.Name = "btnSave";
			this.btnSave.Click += new System.EventHandler(this.BtnSaveClick);
			// 
			// btnClipboard
			// 
			this.btnClipboard.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnClipboard.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnClipboard.LanguageKey = "editor.editor_copyimagetoclipboard";
			this.btnClipboard.Name = "btnClipboard";
			this.btnClipboard.Click += new System.EventHandler(this.BtnClipboardClick);
			// 
			// btnPrint
			// 
			this.btnPrint.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnPrint.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnPrint.LanguageKey = "editor.editor_print";
			this.btnPrint.Name = "btnPrint";
			this.btnPrint.Text = "Print";
			this.btnPrint.Click += new System.EventHandler(this.BtnPrintClick);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			// 
			// btnDelete
			// 
			this.btnDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnDelete.Enabled = false;
			this.btnDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnDelete.LanguageKey = "editor.editor_deleteelement";
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.Click += new System.EventHandler(this.BtnDeleteClick);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			// 
			// btnCut
			// 
			this.btnCut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnCut.Enabled = false;
			this.btnCut.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCut.LanguageKey = "editor.editor_cuttoclipboard";
			this.btnCut.Name = "btnCut";
			this.btnCut.Click += new System.EventHandler(this.BtnCutClick);
			// 
			// btnCopy
			// 
			this.btnCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnCopy.Enabled = false;
			this.btnCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCopy.LanguageKey = "editor.editor_copytoclipboard";
			this.btnCopy.Name = "btnCopy";
			this.btnCopy.Click += new System.EventHandler(this.BtnCopyClick);
			// 
			// btnPaste
			// 
			this.btnPaste.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnPaste.Enabled = false;
			this.btnPaste.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnPaste.LanguageKey = "editor.editor_pastefromclipboard";
			this.btnPaste.Name = "btnPaste";
			this.btnPaste.Click += new System.EventHandler(this.BtnPasteClick);
			// 
			// btnUndo
			// 
			this.btnUndo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnUndo.Enabled = false;
			this.btnUndo.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnUndo.Name = "btnUndo";
			this.btnUndo.Click += new System.EventHandler(this.BtnUndoClick);
			// 
			// btnRedo
			// 
			this.btnRedo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnRedo.Enabled = false;
			this.btnRedo.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnRedo.Name = "btnRedo";
			this.btnRedo.Click += new System.EventHandler(this.BtnRedoClick);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			// 
			// btnSettings
			// 
			this.btnSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnSettings.LanguageKey = "contextmenu_settings";
			this.btnSettings.Name = "btnSettings";
			this.btnSettings.Click += new System.EventHandler(this.BtnSettingsClick);
			// 
			// toolStripSeparator11
			// 
			this.toolStripSeparator11.Name = "toolStripSeparator11";
			// 
			// toolStripSeparator16
			// 
			this.toolStripSeparator16.Name = "toolStripSeparator16";
			// 
			// btnHelp
			// 
			this.btnHelp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnHelp.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnHelp.LanguageKey = "contextmenu_help";
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Text = "Help";
			this.btnHelp.Click += new System.EventHandler(this.BtnHelpClick);
			// 
			// propertiesToolStrip
			// 
			this.propertiesToolStrip.AutoSize = false;
			this.propertiesToolStrip.ClickThrough = true;
			this.propertiesToolStrip.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertiesToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.propertiesToolStrip.Name = "propertiesToolStrip";
			this.propertiesToolStrip.Stretch = true;
			this.propertiesToolStrip.TabIndex = 2;
			this.propertiesToolStrip.Renderer = new CustomToolStripProfessionalRenderer();
			this.propertiesToolStrip.BackColor = System.Drawing.SystemColors.Control;
			this.propertiesToolStrip.OverflowButton.DropDown.BackColor = System.Drawing.SystemColors.Control;
			this.propertiesToolStrip.Paint += PropertiesToolStrip_Paint;
			this.propertiesToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.obfuscateModeButton,
									this.highlightModeButton,
									this.btnFillColor,
									this.btnLineColor,
									this.lineThicknessLabel,
									this.lineThicknessUpDown,
									this.fontFamilyComboBox,
									this.fontSizeLabel,
									this.fontSizeUpDown,
									this.fontBoldButton,
									this.fontItalicButton,
									this.textHorizontalAlignmentButton,
									this.textVerticalAlignmentButton,
									this.blurRadiusLabel,
									this.blurRadiusUpDown,
									this.brightnessLabel,
									this.brightnessUpDown,
									this.magnificationFactorLabel,
									this.magnificationFactorUpDown,
									this.pixelSizeLabel,
									this.pixelSizeUpDown,
									this.arrowHeadsLabel,
									this.arrowHeadsDropDownButton,
									this.shadowButton,
									this.toolStripSeparator,
									this.toolStripSeparator10,
									this.btnConfirm,
									this.btnCancel,
									this.counterLabel,
									this.counterUpDown});
			// 
			// obfuscateModeButton
			// 
			this.obfuscateModeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.obfuscateModeButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.pixelizeToolStripMenuItem,
									this.blurToolStripMenuItem});
			this.obfuscateModeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.obfuscateModeButton.LanguageKey = "editor.editor_obfuscate_mode";
			this.obfuscateModeButton.Name = "obfuscateModeButton";
			this.obfuscateModeButton.SelectedTag = PreparedFilter.Blur;
			this.obfuscateModeButton.Tag = PreparedFilter.Blur;
			// 
			// pixelizeToolStripMenuItem
			// 
			this.pixelizeToolStripMenuItem.LanguageKey = "editor.editor_obfuscate_pixelize";
			this.pixelizeToolStripMenuItem.Name = "pixelizeToolStripMenuItem";
			this.pixelizeToolStripMenuItem.Tag = PreparedFilter.Pixelize;
			// 
			// blurToolStripMenuItem
			// 
			this.blurToolStripMenuItem.LanguageKey = "editor.editor_obfuscate_blur";
			this.blurToolStripMenuItem.Name = "blurToolStripMenuItem";
			this.blurToolStripMenuItem.Tag = PreparedFilter.Blur;
			// 
			// highlightModeButton
			// 
			this.highlightModeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.highlightModeButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.textHighlightMenuItem,
									this.areaHighlightMenuItem,
									this.grayscaleHighlightMenuItem,
									this.magnifyMenuItem});
			this.highlightModeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.highlightModeButton.LanguageKey = "editor.editor_highlight_mode";
			this.highlightModeButton.Name = "highlightModeButton";
			this.highlightModeButton.SelectedTag = PreparedFilter.TextHightlight;
			this.highlightModeButton.Tag = PreparedFilter.TextHightlight;
			// 
			// textHighlightMenuItem
			// 
			this.textHighlightMenuItem.LanguageKey = "editor.editor_highlight_text";
			this.textHighlightMenuItem.Name = "textHighlightMenuItem";
			this.textHighlightMenuItem.Tag = PreparedFilter.TextHightlight;
			// 
			// areaHighlightMenuItem
			// 
			this.areaHighlightMenuItem.LanguageKey = "editor.editor_highlight_area";
			this.areaHighlightMenuItem.Name = "areaHighlightMenuItem";
			this.areaHighlightMenuItem.Tag = PreparedFilter.AreaHighlight;
			// 
			// grayscaleHighlightMenuItem
			// 
			this.grayscaleHighlightMenuItem.LanguageKey = "editor.editor_highlight_grayscale";
			this.grayscaleHighlightMenuItem.Name = "grayscaleHighlightMenuItem";
			this.grayscaleHighlightMenuItem.Tag = PreparedFilter.Grayscale;
			// 
			// magnifyMenuItem
			// 
			this.magnifyMenuItem.LanguageKey = "editor.editor_highlight_magnify";
			this.magnifyMenuItem.Name = "magnifyMenuItem";
			this.magnifyMenuItem.Tag = PreparedFilter.Magnification;
			// 
			// btnFillColor
			// 
			this.btnFillColor.LanguageKey = "editor.editor_backcolor";
			this.btnFillColor.Name = "btnFillColor";
			this.btnFillColor.SelectedColor = System.Drawing.Color.Transparent;
			this.btnFillColor.BackColor = System.Drawing.Color.Transparent;
			this.btnFillColor.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			// 
			// btnLineColor
			// 
			this.btnLineColor.BackColor = System.Drawing.Color.Transparent;
			this.btnLineColor.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnLineColor.LanguageKey = "editor.editor_forecolor";
			this.btnLineColor.Name = "btnLineColor";
			this.btnLineColor.SelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(203)))), ((int)(((byte)(222)))), ((int)(((byte)(250)))));
			// 
			// counterLabel
			// 
			this.counterLabel.LanguageKey = "editor.editor_counter_startvalue";
			this.counterLabel.Name = "counterLabel";
			// 
			// counterUpDown
			// 
			this.counterUpDown.DecimalPlaces = 0;
			this.counterUpDown.Increment = 1;
			this.counterUpDown.Maximum = 100;
			this.counterUpDown.Minimum = 0;
			this.counterUpDown.Name = "counterUpDown";
			this.counterUpDown.Text = "1";
			this.counterUpDown.Value = 1;
			this.counterUpDown.GotFocus += new System.EventHandler(this.ToolBarFocusableElementGotFocus);
			this.counterUpDown.LostFocus += new System.EventHandler(this.ToolBarFocusableElementLostFocus);
			// 
			// lineThicknessLabel
			// 
			this.lineThicknessLabel.LanguageKey = "editor.editor_thickness";
			this.lineThicknessLabel.Name = "lineThicknessLabel";
			// 
			// lineThicknessUpDown
			// 
			this.lineThicknessUpDown.DecimalPlaces = 0;
			this.lineThicknessUpDown.Increment = new decimal(new int[] {
									1,
									0,
									0,
									0});
			this.lineThicknessUpDown.Maximum = new decimal(new int[] {
									100,
									0,
									0,
									0});
			this.lineThicknessUpDown.Minimum = new decimal(new int[] {
									0,
									0,
									0,
									0});
			this.lineThicknessUpDown.Name = "lineThicknessUpDown";
			this.lineThicknessUpDown.Text = "0";
			this.lineThicknessUpDown.Value = new decimal(new int[] {
									0,
									0,
									0,
									0});
			this.lineThicknessUpDown.GotFocus += new System.EventHandler(this.ToolBarFocusableElementGotFocus);
			this.lineThicknessUpDown.LostFocus += new System.EventHandler(this.ToolBarFocusableElementLostFocus);
			// 
			// fontFamilyComboBox
			// 
			this.fontFamilyComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.fontFamilyComboBox.AutoSize = false;
			this.fontFamilyComboBox.MaxDropDownItems = 20;
			this.fontFamilyComboBox.Name = "fontFamilyComboBox";
			this.fontFamilyComboBox.Size = new System.Drawing.Size(200, 20);
			this.fontFamilyComboBox.Text = "Aharoni";
			this.fontFamilyComboBox.Padding = new System.Windows.Forms.Padding(2,0,0,2);
			this.fontFamilyComboBox.GotFocus += new System.EventHandler(this.ToolBarFocusableElementGotFocus);
			this.fontFamilyComboBox.LostFocus += new System.EventHandler(this.ToolBarFocusableElementLostFocus);
			// 
			// fontSizeLabel
			// 
			this.fontSizeLabel.LanguageKey = "editor.editor_fontsize";
			this.fontSizeLabel.Name = "fontSizeLabel";
			// 
			// fontSizeUpDown
			// 
			this.fontSizeUpDown.DecimalPlaces = 0;
			this.fontSizeUpDown.Increment = new decimal(new int[] {
									1,
									0,
									0,
									0});
			this.fontSizeUpDown.Maximum = new decimal(new int[] {
									500,
									0,
									0,
									0});
			this.fontSizeUpDown.Minimum = new decimal(new int[] {
									7,
									0,
									0,
									0});
			this.fontSizeUpDown.Name = "fontSizeUpDown";
			this.fontSizeUpDown.Text = "12";
			this.fontSizeUpDown.Value = new decimal(new int[] {
									12,
									0,
									0,
									0});
			this.fontSizeUpDown.GotFocus += new System.EventHandler(this.ToolBarFocusableElementGotFocus);
			this.fontSizeUpDown.LostFocus += new System.EventHandler(this.ToolBarFocusableElementLostFocus);
			// 
			// fontBoldButton
			// 
			this.fontBoldButton.CheckOnClick = true;
			this.fontBoldButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.fontBoldButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.fontBoldButton.LanguageKey = "editor.editor_bold";
			this.fontBoldButton.Name = "fontBoldButton";
			this.fontBoldButton.Text = "Bold";
			this.fontBoldButton.Click += new System.EventHandler(this.FontBoldButtonClick);
			// 
			// fontItalicButton
			// 
			this.fontItalicButton.CheckOnClick = true;
			this.fontItalicButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.fontItalicButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.fontItalicButton.LanguageKey = "editor.editor_italic";
			this.fontItalicButton.Name = "fontItalicButton";
			this.fontItalicButton.Text = "Italic";
			this.fontItalicButton.Click += new System.EventHandler(this.FontItalicButtonClick);
			// 
			// textVerticalAlignmentButton
			// 
			this.textVerticalAlignmentButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.alignTopToolStripMenuItem,
									this.alignMiddleToolStripMenuItem,
									this.alignBottomToolStripMenuItem});
			this.textVerticalAlignmentButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.textVerticalAlignmentButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.textVerticalAlignmentButton.LanguageKey = "editor.editor_align_vertical";
			this.textVerticalAlignmentButton.Name = "textVerticalAlignmentButton";
			this.textVerticalAlignmentButton.SelectedTag = System.Drawing.StringAlignment.Center;
			this.textVerticalAlignmentButton.Tag = System.Drawing.StringAlignment.Center;
			// 
			// alignTopToolStripMenuItem
			// 
			this.alignTopToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignTopToolStripMenuItem.LanguageKey = "editor.editor_align_top";
			this.alignTopToolStripMenuItem.Name = "alignTopToolStripMenuItem";
			this.alignTopToolStripMenuItem.Tag = System.Drawing.StringAlignment.Near;
			// 
			// alignMiddleToolStripMenuItem
			// 
			this.alignMiddleToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignMiddleToolStripMenuItem.LanguageKey = "editor.editor_align_middle";
			this.alignMiddleToolStripMenuItem.Name = "alignMiddleToolStripMenuItem";
			this.alignMiddleToolStripMenuItem.Tag = System.Drawing.StringAlignment.Center;
			// 
			// alignBottomToolStripMenuItem
			// 
			this.alignBottomToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignBottomToolStripMenuItem.LanguageKey = "editor.editor_align_bottom";
			this.alignBottomToolStripMenuItem.Name = "alignBottomToolStripMenuItem";
			this.alignBottomToolStripMenuItem.Tag = System.Drawing.StringAlignment.Far;
			// 
			// blurRadiusLabel
			// 
			this.blurRadiusLabel.LanguageKey = "editor.editor_blur_radius";
			this.blurRadiusLabel.Name = "blurRadiusLabel";
			this.blurRadiusLabel.Text = "Blur radius";
			// 
			// blurRadiusUpDown
			// 
			this.blurRadiusUpDown.DecimalPlaces = 0;
			this.blurRadiusUpDown.Increment = new decimal(new int[] {
									1,
									0,
									0,
									0});
			this.blurRadiusUpDown.Maximum = new decimal(new int[] {
									100,
									0,
									0,
									0});
			this.blurRadiusUpDown.Minimum = new decimal(new int[] {
									0,
									0,
									0,
									0});
			this.blurRadiusUpDown.Name = "blurRadiusUpDown";
			this.blurRadiusUpDown.Text = "1";
			this.blurRadiusUpDown.Value = new decimal(new int[] {
									1,
									0,
									0,
									0});
			this.blurRadiusUpDown.GotFocus += new System.EventHandler(this.ToolBarFocusableElementGotFocus);
			this.blurRadiusUpDown.LostFocus += new System.EventHandler(this.ToolBarFocusableElementLostFocus);
			// 
			// brightnessLabel
			// 
			this.brightnessLabel.LanguageKey = "editor.editor_brightness";
			this.brightnessLabel.Name = "brightnessLabel";
			this.brightnessLabel.Text = "Brightness";
			// 
			// brightnessUpDown
			// 
			this.brightnessUpDown.DecimalPlaces = 0;
			this.brightnessUpDown.Increment = new decimal(new int[] {
									5,
									0,
									0,
									0});
			this.brightnessUpDown.Maximum = new decimal(new int[] {
									200,
									0,
									0,
									0});
			this.brightnessUpDown.Minimum = new decimal(new int[] {
									0,
									0,
									0,
									0});
			this.brightnessUpDown.Name = "brightnessUpDown";
			this.brightnessUpDown.Text = "100";
			this.brightnessUpDown.Value = new decimal(new int[] {
									100,
									0,
									0,
									0});
			this.brightnessUpDown.GotFocus += new System.EventHandler(this.ToolBarFocusableElementGotFocus);
			this.brightnessUpDown.LostFocus += new System.EventHandler(this.ToolBarFocusableElementLostFocus);
			// 
			// magnificationFactorLabel
			// 
			this.magnificationFactorLabel.LanguageKey = "editor.editor_magnification_factor";
			this.magnificationFactorLabel.Name = "magnificationFactorLabel";
			this.magnificationFactorLabel.Tag = PreparedFilter.Magnification;
			// 
			// magnificationFactorUpDown
			// 
			this.magnificationFactorUpDown.DecimalPlaces = 0;
			this.magnificationFactorUpDown.Increment = new decimal(new int[] {
									2,
									0,
									0,
									0});
			this.magnificationFactorUpDown.Maximum = new decimal(new int[] {
									8,
									0,
									0,
									0});
			this.magnificationFactorUpDown.Minimum = new decimal(new int[] {
									2,
									0,
									0,
									0});
			this.magnificationFactorUpDown.Name = "magnificationFactorUpDown";
			this.magnificationFactorUpDown.Text = "2";
			this.magnificationFactorUpDown.Value = new decimal(new int[] {
									2,
									0,
									0,
									0});
			this.magnificationFactorUpDown.GotFocus += new System.EventHandler(this.ToolBarFocusableElementGotFocus);
			this.magnificationFactorUpDown.LostFocus += new System.EventHandler(this.ToolBarFocusableElementLostFocus);
			// 
			// pixelSizeLabel
			// 
			this.pixelSizeLabel.LanguageKey = "editor.editor_pixel_size";
			this.pixelSizeLabel.Name = "pixelSizeLabel";
			// 
			// pixelSizeUpDown
			// 
			this.pixelSizeUpDown.DecimalPlaces = 0;
			this.pixelSizeUpDown.Increment = new decimal(new int[] {
									1,
									0,
									0,
									0});
			this.pixelSizeUpDown.Maximum = new decimal(new int[] {
									100,
									0,
									0,
									0});
			this.pixelSizeUpDown.Minimum = new decimal(new int[] {
									2,
									0,
									0,
									0});
			this.pixelSizeUpDown.Name = "pixelSizeUpDown";
			this.pixelSizeUpDown.Text = "5";
			this.pixelSizeUpDown.Value = new decimal(new int[] {
									5,
									0,
									0,
									0});
			this.pixelSizeUpDown.GotFocus += new System.EventHandler(this.ToolBarFocusableElementGotFocus);
			this.pixelSizeUpDown.LostFocus += new System.EventHandler(this.ToolBarFocusableElementLostFocus);
			// 
			// arrowHeadsLabel
			// 
			this.arrowHeadsLabel.LanguageKey = "editor.editor_pixel_size";
			this.arrowHeadsLabel.Name = "arrowHeadsLabel";
			// 
			// arrowHeadsDropDownButton
			// 
			this.arrowHeadsDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.arrowHeadsDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.arrowHeadStartMenuItem,
									this.arrowHeadEndMenuItem,
									this.arrowHeadBothMenuItem,
									this.arrowHeadNoneMenuItem});
			this.arrowHeadsDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.arrowHeadsDropDownButton.LanguageKey = "editor.editor_arrowheads";
			this.arrowHeadsDropDownButton.Name = "arrowHeadsDropDownButton";
			// 
			// arrowHeadStartMenuItem
			// 
			this.arrowHeadStartMenuItem.LanguageKey = "editor.editor_arrowheads_start";
			this.arrowHeadStartMenuItem.Name = "arrowHeadStartMenuItem";
			this.arrowHeadStartMenuItem.Tag = ArrowContainer.ArrowHeadCombination.START_POINT;
			this.arrowHeadStartMenuItem.Click += new System.EventHandler(this.ArrowHeadsToolStripMenuItemClick);
			// 
			// arrowHeadEndMenuItem
			// 
			this.arrowHeadEndMenuItem.LanguageKey = "editor.editor_arrowheads_end";
			this.arrowHeadEndMenuItem.Name = "arrowHeadEndMenuItem";
			this.arrowHeadEndMenuItem.Tag = ArrowContainer.ArrowHeadCombination.END_POINT;
			this.arrowHeadEndMenuItem.Click += new System.EventHandler(this.ArrowHeadsToolStripMenuItemClick);
			// 
			// arrowHeadBothMenuItem
			// 
			this.arrowHeadBothMenuItem.LanguageKey = "editor.editor_arrowheads_both";
			this.arrowHeadBothMenuItem.Name = "arrowHeadBothMenuItem";
			this.arrowHeadBothMenuItem.Tag = ArrowContainer.ArrowHeadCombination.BOTH;
			this.arrowHeadBothMenuItem.Click += new System.EventHandler(this.ArrowHeadsToolStripMenuItemClick);
			// 
			// arrowHeadNoneMenuItem
			// 
			this.arrowHeadNoneMenuItem.LanguageKey = "editor.editor_arrowheads_none";
			this.arrowHeadNoneMenuItem.Name = "arrowHeadNoneMenuItem";
			this.arrowHeadNoneMenuItem.Tag = ArrowContainer.ArrowHeadCombination.NONE;
			this.arrowHeadNoneMenuItem.Click += new System.EventHandler(this.ArrowHeadsToolStripMenuItemClick);
			// 
			// shadowButton
			// 
			this.shadowButton.CheckOnClick = true;
			this.shadowButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.shadowButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.shadowButton.LanguageKey = "editor.editor_shadow";
			this.shadowButton.Name = "shadowButton";
			// 
			// toolStripSeparator
			// 
			this.toolStripSeparator.Name = "toolStripSeparator";
			// 
			// toolStripSeparator10
			// 
			this.toolStripSeparator10.Name = "toolStripSeparator10";
			// 
			// btnConfirm
			// 
			this.btnConfirm.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnConfirm.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnConfirm.LanguageKey = "editor.editor_confirm";
			this.btnConfirm.Name = "btnConfirm";
			this.btnConfirm.Text = "Confirm";
			this.btnConfirm.Click += new System.EventHandler(this.BtnConfirmClick);
			// 
			// btnCancel
			// 
			this.btnCancel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnCancel.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCancel.LanguageKey = "editor.editor_cancel";
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Text = "Cancel";
			this.btnCancel.Click += new System.EventHandler(this.BtnCancelClick);
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			// 
			// closeToolStripMenuItem
			// 
			this.closeToolStripMenuItem.LanguageKey = "editor.editor_close";
			this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
			this.closeToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.closeToolStripMenuItem.Click += new System.EventHandler(this.CloseToolStripMenuItemClick);
			// 
			// fileSavedStatusContextMenu
			// 
			this.fileSavedStatusContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.copyPathMenuItem,
									this.openDirectoryMenuItem});
			this.fileSavedStatusContextMenu.Name = "contextMenuStrip1";
			// 
			// copyPathMenuItem
			// 
			this.copyPathMenuItem.LanguageKey = "editor.editor_copypathtoclipboard";
			this.copyPathMenuItem.Name = "copyPathMenuItem";
			this.copyPathMenuItem.Click += new System.EventHandler(this.CopyPathMenuItemClick);
			// 
			// openDirectoryMenuItem
			// 
			this.openDirectoryMenuItem.LanguageKey = "editor.editor_opendirinexplorer";
			this.openDirectoryMenuItem.Name = "openDirectoryMenuItem";
			this.openDirectoryMenuItem.Click += new System.EventHandler(this.OpenDirectoryMenuItemClick);
			// 
			// textHorizontalAlignmentButton
			// 
			this.textHorizontalAlignmentButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.alignLeftToolStripMenuItem,
									this.alignCenterToolStripMenuItem,
									this.alignRightToolStripMenuItem});
			this.textHorizontalAlignmentButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.textHorizontalAlignmentButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.textHorizontalAlignmentButton.LanguageKey = "editor.editor_align_horizontal";
			this.textHorizontalAlignmentButton.Name = "textHorizontalAlignmentButton";
			this.textHorizontalAlignmentButton.SelectedTag = System.Drawing.StringAlignment.Center;
			this.textHorizontalAlignmentButton.Tag = System.Drawing.StringAlignment.Center;
			// 
			// alignLeftToolStripMenuItem
			// 
			this.alignLeftToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignLeftToolStripMenuItem.LanguageKey = "editor.editor_align_left";
			this.alignLeftToolStripMenuItem.Name = "alignLeftToolStripMenuItem";
			this.alignLeftToolStripMenuItem.Tag = System.Drawing.StringAlignment.Near;
			// 
			// alignCenterToolStripMenuItem
			// 
			this.alignCenterToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignCenterToolStripMenuItem.LanguageKey = "editor.editor_align_center";
			this.alignCenterToolStripMenuItem.Name = "alignCenterToolStripMenuItem";
			this.alignCenterToolStripMenuItem.Tag = System.Drawing.StringAlignment.Center;
			// 
			// alignRightToolStripMenuItem
			// 
			this.alignRightToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignRightToolStripMenuItem.LanguageKey = "editor.editor_align_right";
			this.alignRightToolStripMenuItem.Name = "alignRightToolStripMenuItem";
			this.alignRightToolStripMenuItem.Tag = System.Drawing.StringAlignment.Far;
            // 
            // ImageEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(785, 485);
			this.Controls.Add(this.topToolStripContainer);
			this.KeyPreview = true;
			this.LanguageKey = "editor.editor_title";
			this.Name = "ImageEditorForm";
			this.Activated += new System.EventHandler(this.ImageEditorFormActivated);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ImageEditorFormFormClosing);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ImageEditorFormKeyDown);
			this.Resize += new System.EventHandler(this.ImageEditorFormResize);
			this.topToolStripContainer.BottomToolStripPanel.ResumeLayout(true);
			this.topToolStripContainer.ContentPanel.ResumeLayout(true);
			this.topToolStripContainer.LeftToolStripPanel.ResumeLayout(true);
			this.topToolStripContainer.TopToolStripPanel.ResumeLayout(true);
			this.topToolStripContainer.ResumeLayout(true);
			this.statusStrip1.ResumeLayout(true);
			this.tableLayoutPanel1.ResumeLayout(true);
			this.toolsToolStrip.ResumeLayout(true);
			this.menuStrip1.ResumeLayout(true);
			this.destinationsToolStrip.ResumeLayout(true);
			this.propertiesToolStrip.ResumeLayout(true);
			this.fileSavedStatusContextMenu.ResumeLayout(true);
			this.ResumeLayout(false);
		}
		private GreenshotToolStripMenuItem alignRightToolStripMenuItem;
		private GreenshotToolStripMenuItem alignCenterToolStripMenuItem;
		private GreenshotToolStripMenuItem alignLeftToolStripMenuItem;
		private BindableToolStripDropDownButton textHorizontalAlignmentButton;
		private GreenshotToolStripMenuItem alignMiddleToolStripMenuItem;
		private GreenshotToolStripMenuItem alignBottomToolStripMenuItem;
		private GreenshotToolStripMenuItem alignTopToolStripMenuItem;
		private BindableToolStripDropDownButton textVerticalAlignmentButton;
		private GreenshotToolStripMenuItem invertToolStripMenuItem;
		private GreenshotToolStripButton btnResize;
		private GreenshotToolStripMenuItem grayscaleToolStripMenuItem;
		private GreenshotToolStripButton rotateCcwToolstripButton;
		private GreenshotToolStripButton rotateCwToolstripButton;
		private GreenshotToolStripMenuItem addBorderToolStripMenuItem;
		private GreenshotToolStripMenuItem tornEdgesToolStripMenuItem;
		private GreenshotToolStripMenuItem addDropshadowToolStripMenuItem;
		private GreenshotToolStripDropDownButton toolStripSplitButton1;
		private System.Windows.Forms.ToolStripStatusLabel dimensionsLabel;
		private GreenshotToolStripMenuItem insert_window_toolstripmenuitem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private GreenshotToolStripMenuItem grayscaleHighlightMenuItem;
		private GreenshotToolStripMenuItem areaHighlightMenuItem;
		private GreenshotToolStripMenuItem textHighlightMenuItem;
		private GreenshotToolStripMenuItem magnifyMenuItem;
		private GreenshotToolStripMenuItem arrowHeadStartMenuItem;
		private GreenshotToolStripMenuItem arrowHeadEndMenuItem;
		private GreenshotToolStripMenuItem arrowHeadBothMenuItem;
		private GreenshotToolStripMenuItem arrowHeadNoneMenuItem;
		private BindableToolStripButton btnCancel;
		private BindableToolStripButton btnConfirm;
		private GreenshotToolStripMenuItem selectAllToolStripMenuItem;
		private BindableToolStripDropDownButton highlightModeButton;
		private GreenshotToolStripMenuItem pixelizeToolStripMenuItem;
		private GreenshotToolStripMenuItem blurToolStripMenuItem;
		private BindableToolStripDropDownButton obfuscateModeButton;
		private GreenshotToolStripButton btnHighlight;
		private GreenshotToolStripMenuItem loadElementsToolStripMenuItem;
		private GreenshotToolStripMenuItem saveElementsToolStripMenuItem;
		private FontFamilyComboBox fontFamilyComboBox;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
		private BindableToolStripButton shadowButton;
		private BindableToolStripButton fontItalicButton;
		private BindableToolStripButton fontBoldButton;
		private ToolStripNumericUpDown fontSizeUpDown;
		private GreenshotToolStripLabel fontSizeLabel;
		private ToolStripNumericUpDown brightnessUpDown;
		private GreenshotToolStripLabel brightnessLabel;
		private GreenshotToolStripMenuItem pluginToolStripMenuItem;
		private GreenshotToolStripDropDownButton arrowHeadsDropDownButton;
		private GreenshotToolStripLabel arrowHeadsLabel;
		private ToolStripNumericUpDown pixelSizeUpDown;
		private GreenshotToolStripLabel pixelSizeLabel;
		private ToolStripNumericUpDown magnificationFactorUpDown;
		private GreenshotToolStripLabel magnificationFactorLabel;
		private ToolStripNumericUpDown blurRadiusUpDown;
		private GreenshotToolStripLabel blurRadiusLabel;
		private ToolStripEx propertiesToolStrip;
		private GreenshotToolStripLabel lineThicknessLabel;
		private ToolStripNumericUpDown lineThicknessUpDown;
		private GreenshotToolStripLabel counterLabel;
		private ToolStripNumericUpDown counterUpDown;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator14;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator15;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator16;
		private GreenshotToolStripButton btnFreehand;
		private GreenshotToolStripButton btnObfuscate;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator13;
		private GreenshotToolStripButton btnCrop;
		private GreenshotToolStripMenuItem openDirectoryMenuItem;
		private GreenshotToolStripMenuItem copyPathMenuItem;
		private System.Windows.Forms.ContextMenuStrip fileSavedStatusContextMenu;
		private GreenshotToolStripMenuItem downToBottomToolStripMenuItem;
		private GreenshotToolStripMenuItem upToTopToolStripMenuItem;
		private GreenshotToolStripMenuItem downOneLevelToolStripMenuItem;
		private GreenshotToolStripMenuItem upOneLevelToolStripMenuItem;
		private GreenshotToolStripMenuItem arrangeToolStripMenuItem;
		private GreenshotToolStripButton btnCursor;
		private ToolStripEx toolsToolStrip;
		private GreenshotToolStripButton btnArrow;
		private GreenshotToolStripMenuItem drawArrowToolStripMenuItem;
		private GreenshotToolStripMenuItem drawFreehandToolStripMenuItem;
		private GreenshotToolStripButton btnText;
		private GreenshotToolStripButton btnSpeechBubble;
		private GreenshotToolStripButton btnStepLabel;
		private GreenshotToolStripMenuItem drawLineToolStripMenuItem;
		private GreenshotToolStripButton btnLine;
		private GreenshotToolStripButton btnSettings;
		private GreenshotToolStripButton btnHelp;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
		private GreenshotToolStripMenuItem aboutToolStripMenuItem;
		private GreenshotToolStripMenuItem helpToolStripMenuItem1;
		private GreenshotToolStripMenuItem helpToolStripMenuItem;
		private GreenshotToolStripMenuItem preferencesToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
		private GreenshotToolStripMenuItem closeToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private GreenshotToolStripButton btnPrint;
		private GreenshotToolStripMenuItem duplicateToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private GreenshotToolStripMenuItem fileStripMenuItem;
		private GreenshotToolStripMenuItem removeObjectToolStripMenuItem;
		private GreenshotToolStripMenuItem addTextBoxToolStripMenuItem;
		private GreenshotToolStripMenuItem addSpeechBubbleToolStripMenuItem;
		private GreenshotToolStripMenuItem addCounterToolStripMenuItem;
		private GreenshotToolStripMenuItem addEllipseToolStripMenuItem;
		private GreenshotToolStripMenuItem addRectangleToolStripMenuItem;
		private GreenshotToolStripMenuItem objectToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
		private GreenshotToolStripMenuItem pasteToolStripMenuItem;
		private GreenshotToolStripMenuItem copyToolStripMenuItem;
		private GreenshotToolStripMenuItem cutToolStripMenuItem;
		private GreenshotToolStripMenuItem editToolStripMenuItem;
		private MenuStripEx menuStrip1;
		private System.Windows.Forms.ToolStripStatusLabel statusLabel;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private GreenshotToolStripButton btnCut;
		private GreenshotToolStripButton btnCopy;
		private GreenshotToolStripButton btnPaste;
		private System.Windows.Forms.ToolStripButton btnUndo;
		private System.Windows.Forms.ToolStripButton btnRedo;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private GreenshotToolStripButton btnClipboard;
		private GreenshotToolStripButton btnDelete;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private GreenshotToolStripButton btnEllipse;
		private GreenshotToolStripButton btnSave;
		private GreenshotToolStripButton btnRect;
		private System.Windows.Forms.ToolStripContainer topToolStripContainer;
		private ToolStripEx destinationsToolStrip;
		private NonJumpingPanel panel1;
		private ToolStripColorButton btnFillColor;
		private ToolStripColorButton btnLineColor;
		private GreenshotToolStripMenuItem autoCropToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator17;
	}
}
