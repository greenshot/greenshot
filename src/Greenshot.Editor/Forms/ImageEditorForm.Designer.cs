/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using Greenshot.Base.Controls;
using Greenshot.Editor.Controls;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Forms {
	partial class ImageEditorForm {
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		private System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImageEditorForm));

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
			this.cropModeButton = new BindableToolStripDropDownButton();
			this.pixelizeToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.blurToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.defaultCropModeToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.verticalCropModeToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.horizontalCropModeToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.autoCropModeToolStripMenuItem = new GreenshotToolStripMenuItem();
			this.highlightModeButton = new BindableToolStripDropDownButton();
			this.textHighlightMenuItem = new GreenshotToolStripMenuItem();
			this.areaHighlightMenuItem = new GreenshotToolStripMenuItem();
			this.grayscaleHighlightMenuItem = new GreenshotToolStripMenuItem();
			this.magnifyMenuItem = new GreenshotToolStripMenuItem();
			this.btnFillColor = new ToolStripColorButton();
			this.btnLineColor = new ToolStripColorButton();
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
			this.previewQualityLabel = new GreenshotToolStripLabel();
			this.previewQualityUpDown = new ToolStripNumericUpDown();
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
			this.zoomMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.zoomInMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoomOutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoomMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.zoomBestFitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoomMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.zoom25MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoom50MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoom66MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoom75MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoomMenuSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.zoomActualSizeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoomMenuSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.zoom200MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoom300MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoom400MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoom600MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoomStatusDropDownBtn = new System.Windows.Forms.ToolStripDropDownButton();
			this.zoomMainMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusStripSpacer = new System.Windows.Forms.ToolStripStatusLabel();
			this.topToolStripContainer.BottomToolStripPanel.SuspendLayout();
			this.topToolStripContainer.ContentPanel.SuspendLayout();
			this.topToolStripContainer.LeftToolStripPanel.SuspendLayout();
			this.topToolStripContainer.TopToolStripPanel.SuspendLayout();
			this.topToolStripContainer.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.toolsToolStrip.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.zoomMenuStrip.SuspendLayout();
			this.destinationsToolStrip.SuspendLayout();
			this.propertiesToolStrip.SuspendLayout();
			this.fileSavedStatusContextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// topToolStripContainer
			// 
            this.topToolStripContainer.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.topToolStripContainer.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
									this.statusLabel,
									this.statusStripSpacer,
									this.zoomStatusDropDownBtn});
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
			this.toolsToolStrip.ImageScalingSize = coreConfiguration.IconSize;
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
			this.btnCursor.Image = ((System.Drawing.Image)(resources.GetObject("btnCursor.Image")));
			this.btnCursor.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCursor.LanguageKey = "editor_cursortool";
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
			this.btnRect.Image = ((System.Drawing.Image)(resources.GetObject("btnRect.Image")));
			this.btnRect.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnRect.LanguageKey = "editor_drawrectangle";
			this.btnRect.Name = "btnRect";
			this.btnRect.Click += new System.EventHandler(this.BtnRectClick);
			// 
			// btnEllipse
			// 
			this.btnEllipse.CheckOnClick = true;
			this.btnEllipse.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnEllipse.Image = ((System.Drawing.Image)(resources.GetObject("btnEllipse.Image")));
			this.btnEllipse.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnEllipse.LanguageKey = "editor_drawellipse";
			this.btnEllipse.Name = "btnEllipse";
			this.btnEllipse.Click += new System.EventHandler(this.BtnEllipseClick);
			// 
			// btnLine
			// 
			this.btnLine.CheckOnClick = true;
			this.btnLine.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnLine.Image = ((System.Drawing.Image)(resources.GetObject("btnLine.Image")));
			this.btnLine.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnLine.LanguageKey = "editor_drawline";
			this.btnLine.Name = "btnLine";
			this.btnLine.Click += new System.EventHandler(this.BtnLineClick);
			// 
			// btnArrow
			// 
			this.btnArrow.CheckOnClick = true;
			this.btnArrow.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnArrow.Image = ((System.Drawing.Image)(resources.GetObject("btnArrow.Image")));
			this.btnArrow.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnArrow.LanguageKey = "editor_drawarrow";
			this.btnArrow.Name = "btnArrow";
			this.btnArrow.Click += new System.EventHandler(this.BtnArrowClick);
			// 
			// btnFreehand
			// 
			this.btnFreehand.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnFreehand.Image = ((System.Drawing.Image)(resources.GetObject("btnFreehand.Image")));
			this.btnFreehand.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnFreehand.LanguageKey = "editor_drawfreehand";
			this.btnFreehand.Name = "btnFreehand";
			this.btnFreehand.Click += new System.EventHandler(this.BtnFreehandClick);
			// 
			// btnText
			// 
			this.btnText.CheckOnClick = true;
			this.btnText.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnText.Image = ((System.Drawing.Image)(resources.GetObject("btnText.Image")));
			this.btnText.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnText.LanguageKey = "editor_drawtextbox";
			this.btnText.Name = "btnText";
			this.btnText.Click += new System.EventHandler(this.BtnTextClick);
			// 
			// btnSpeechBubble
			// 
			this.btnSpeechBubble.CheckOnClick = true;
			this.btnSpeechBubble.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnSpeechBubble.Image = ((System.Drawing.Image)(resources.GetObject("btnSpeechBubble.Image")));
			this.btnSpeechBubble.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnSpeechBubble.LanguageKey = "editor_speechbubble";
			this.btnSpeechBubble.Name = "btnSpeechBubble";
			this.btnSpeechBubble.Click += new System.EventHandler(this.BtnSpeechBubbleClick);
			// 
			// btnStepLabel
			// 
			this.btnStepLabel.CheckOnClick = true;
			this.btnStepLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnStepLabel.Image = ((System.Drawing.Image)(resources.GetObject("btnStepLabel01.Image")));
			this.btnStepLabel.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnStepLabel.LanguageKey = "editor_counter";
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
			this.btnHighlight.Image = ((System.Drawing.Image)(resources.GetObject("btnHighlight.Image")));
			this.btnHighlight.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnHighlight.LanguageKey = "editor_drawhighlighter";
			this.btnHighlight.Name = "btnHighlight";
			this.btnHighlight.Click += new System.EventHandler(this.BtnHighlightClick);
			// 
			// btnObfuscate
			// 
			this.btnObfuscate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnObfuscate.Image = ((System.Drawing.Image)(resources.GetObject("btnObfuscate.Image")));
			this.btnObfuscate.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnObfuscate.LanguageKey = "editor_obfuscate";
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
			this.toolStripSplitButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripSplitButton1.Image")));
			this.toolStripSplitButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripSplitButton1.LanguageKey = "editor_effects";
			this.toolStripSplitButton1.Name = "toolStripSplitButton1";
			this.toolStripSplitButton1.ShowDropDownArrow = false;
			this.toolStripSplitButton1.Text = "toolStripSplitButton1";
			// 
			// addBorderToolStripMenuItem
			// 
			this.addBorderToolStripMenuItem.LanguageKey = "editor_border";
			this.addBorderToolStripMenuItem.Name = "addBorderToolStripMenuItem";
			this.addBorderToolStripMenuItem.Click += new System.EventHandler(this.AddBorderToolStripMenuItemClick);
			// 
			// addDropshadowToolStripMenuItem
			// 
			this.addDropshadowToolStripMenuItem.LanguageKey = "editor_image_shadow";
			this.addDropshadowToolStripMenuItem.Name = "addDropshadowToolStripMenuItem";
			this.addDropshadowToolStripMenuItem.MouseUp += AddDropshadowToolStripMenuItemMouseUp;
			// 
			// tornEdgesToolStripMenuItem
			// 
			this.tornEdgesToolStripMenuItem.LanguageKey = "editor_torn_edge";
			this.tornEdgesToolStripMenuItem.Name = "tornEdgesToolStripMenuItem";
			this.tornEdgesToolStripMenuItem.MouseUp += TornEdgesToolStripMenuItemMouseUp;
			// 
			// grayscaleToolStripMenuItem
			// 
			this.grayscaleToolStripMenuItem.LanguageKey = "editor_grayscale";
			this.grayscaleToolStripMenuItem.Name = "grayscaleToolStripMenuItem";
			this.grayscaleToolStripMenuItem.Click += new System.EventHandler(this.GrayscaleToolStripMenuItemClick);
			// 
			// invertToolStripMenuItem
			// 
			this.invertToolStripMenuItem.LanguageKey = "editor_invert";
			this.invertToolStripMenuItem.Name = "invertToolStripMenuItem";
			this.invertToolStripMenuItem.Click += new System.EventHandler(this.InvertToolStripMenuItemClick);
			// 
			// btnResize
			// 
			this.btnResize.Name = "btnResize";
			this.btnResize.Click += new System.EventHandler(this.BtnResizeClick);
			this.btnResize.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnResize.Image = ((System.Drawing.Image)(resources.GetObject("btnResize.Image")));
			this.btnResize.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnResize.LanguageKey = "editor_resize";
			// 
			// toolStripSeparator13
			// 
			this.toolStripSeparator13.Name = "toolStripSeparator13";
			// 
			// btnCrop
			// 
			this.btnCrop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnCrop.Image = ((System.Drawing.Image)(resources.GetObject("btnCrop.Image")));
			this.btnCrop.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCrop.LanguageKey = "editor_crop";
			this.btnCrop.Name = "btnCrop";
			this.btnCrop.Click += new System.EventHandler(this.BtnCropClick);
			// 
			// rotateCwToolstripButton
			// 
			this.rotateCwToolstripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.rotateCwToolstripButton.Image = ((System.Drawing.Image)(resources.GetObject("rotateCwToolstripButton.Image")));
			this.rotateCwToolstripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.rotateCwToolstripButton.LanguageKey = "editor_rotatecw";
			this.rotateCwToolstripButton.Name = "rotateCwToolstripButton";
			this.rotateCwToolstripButton.Click += new System.EventHandler(this.RotateCwToolstripButtonClick);
			// 
			// rotateCcwToolstripButton
			// 
			this.rotateCcwToolstripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.rotateCcwToolstripButton.Image = ((System.Drawing.Image)(resources.GetObject("rotateCcwToolstripButton.Image")));
			this.rotateCcwToolstripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.rotateCcwToolstripButton.LanguageKey = "editor_rotateccw";
			this.rotateCcwToolstripButton.Name = "rotateCcwToolstripButton";
			this.rotateCcwToolstripButton.Click += new System.EventHandler(this.RotateCcwToolstripButtonClick);
			// 
			// menuStrip1
			// 
			this.menuStrip1.ClickThrough = true;
			this.menuStrip1.ImageScalingSize = coreConfiguration.IconSize;
			this.menuStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.menuStrip1.Stretch = true;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.fileStripMenuItem,
									this.editToolStripMenuItem,
									this.objectToolStripMenuItem,
									this.pluginToolStripMenuItem,
									this.zoomMainMenuItem,
									this.helpToolStripMenuItem});
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.BackColor = System.Drawing.SystemColors.Control;
			this.menuStrip1.TabIndex = 1;
			// 
			// fileStripMenuItem
			// 
			this.fileStripMenuItem.LanguageKey = "editor_file";
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
									this.insert_window_toolstripmenuitem});
			this.editToolStripMenuItem.LanguageKey = "editor_edit";
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
			this.editToolStripMenuItem.Text = "Edit";
			this.editToolStripMenuItem.Click += new System.EventHandler(this.EditToolStripMenuItemClick);
			// 
			// undoToolStripMenuItem
			// 
			this.undoToolStripMenuItem.Enabled = false;
			this.undoToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("undoToolStripMenuItem.Image")));
			this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
			this.undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
			this.undoToolStripMenuItem.Text = "Undo";
			this.undoToolStripMenuItem.Click += new System.EventHandler(this.UndoToolStripMenuItemClick);
			// 
			// redoToolStripMenuItem
			// 
			this.redoToolStripMenuItem.Enabled = false;
			this.redoToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("redoToolStripMenuItem.Image")));
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
			this.cutToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("cutToolStripMenuItem.Image")));
			this.cutToolStripMenuItem.LanguageKey = "editor_cuttoclipboard";
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.cutToolStripMenuItem.Click += new System.EventHandler(this.CutToolStripMenuItemClick);
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.Enabled = false;
			this.copyToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("copyToolStripMenuItem.Image")));
			this.copyToolStripMenuItem.LanguageKey = "editor_copytoclipboard";
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.CopyToolStripMenuItemClick);
			// 
			// pasteToolStripMenuItem
			// 
			this.pasteToolStripMenuItem.Enabled = false;
			this.pasteToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("pasteToolStripMenuItem.Image")));
			this.pasteToolStripMenuItem.LanguageKey = "editor_pastefromclipboard";
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
			this.duplicateToolStripMenuItem.LanguageKey = "editor_duplicate";
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
			this.preferencesToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("preferencesToolStripMenuItem.Image")));
			this.preferencesToolStripMenuItem.LanguageKey = "contextmenu_settings";
			this.preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
			this.preferencesToolStripMenuItem.Click += new System.EventHandler(this.PreferencesToolStripMenuItemClick);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			// 
			// insert_window_toolstripmenuitem
			// 
			this.insert_window_toolstripmenuitem.LanguageKey = "editor_insertwindow";
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
			this.objectToolStripMenuItem.LanguageKey = "editor_object";
			this.objectToolStripMenuItem.Name = "objectToolStripMenuItem";
			this.objectToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
			this.objectToolStripMenuItem.Text = "Object";
			// 
			// addRectangleToolStripMenuItem
			// 
			this.addRectangleToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("addRectangleToolStripMenuItem.Image")));
			this.addRectangleToolStripMenuItem.LanguageKey = "editor_drawrectangle";
			this.addRectangleToolStripMenuItem.Name = "addRectangleToolStripMenuItem";
			this.addRectangleToolStripMenuItem.Click += new System.EventHandler(this.AddRectangleToolStripMenuItemClick);
			// 
			// addEllipseToolStripMenuItem
			// 
			this.addEllipseToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("addEllipseToolStripMenuItem.Image")));
			this.addEllipseToolStripMenuItem.LanguageKey = "editor_drawellipse";
			this.addEllipseToolStripMenuItem.Name = "addEllipseToolStripMenuItem";
			this.addEllipseToolStripMenuItem.Click += new System.EventHandler(this.AddEllipseToolStripMenuItemClick);
			// 
			// drawLineToolStripMenuItem
			// 
			this.drawLineToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("drawLineToolStripMenuItem.Image")));
			this.drawLineToolStripMenuItem.LanguageKey = "editor_drawline";
			this.drawLineToolStripMenuItem.Name = "drawLineToolStripMenuItem";
			this.drawLineToolStripMenuItem.Click += new System.EventHandler(this.DrawLineToolStripMenuItemClick);
			// 
			// drawArrowToolStripMenuItem
			// 
			this.drawArrowToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("drawArrowToolStripMenuItem.Image")));
			this.drawArrowToolStripMenuItem.LanguageKey = "editor_drawarrow";
			this.drawArrowToolStripMenuItem.Name = "drawArrowToolStripMenuItem";
			this.drawArrowToolStripMenuItem.Click += new System.EventHandler(this.DrawArrowToolStripMenuItemClick);
			// 
			// drawFreehandToolStripMenuItem
			// 
			this.drawFreehandToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("drawFreehandToolStripMenuItem.Image")));
			this.drawFreehandToolStripMenuItem.LanguageKey = "editor_drawfreehand";
			this.drawFreehandToolStripMenuItem.Name = "drawFreehandToolStripMenuItem";
			this.drawFreehandToolStripMenuItem.Click += new System.EventHandler(this.DrawFreehandToolStripMenuItemClick);
			// 
			// addTextBoxToolStripMenuItem
			// 
			this.addTextBoxToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("addTextBoxToolStripMenuItem.Image")));
			this.addTextBoxToolStripMenuItem.LanguageKey = "editor_drawtextbox";
			this.addTextBoxToolStripMenuItem.Name = "addTextBoxToolStripMenuItem";
			this.addTextBoxToolStripMenuItem.Click += new System.EventHandler(this.AddTextBoxToolStripMenuItemClick);
            // 
            // addSpeechBubbleToolStripMenuItem
            // 
            this.addSpeechBubbleToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnSpeechBubble.Image")));
            this.addSpeechBubbleToolStripMenuItem.LanguageKey = "editor_speechbubble";
            this.addSpeechBubbleToolStripMenuItem.Name = "addSpeechBubbleToolStripMenuItem";
            this.addSpeechBubbleToolStripMenuItem.Click += new System.EventHandler(this.AddSpeechBubbleToolStripMenuItemClick);
            // 
            // addCounterToolStripMenuItem
            // 
            this.addCounterToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnStepLabel01.Image")));
            this.addCounterToolStripMenuItem.LanguageKey = "editor_counter";
            this.addCounterToolStripMenuItem.Name = "addCounterToolStripMenuItem";
            this.addCounterToolStripMenuItem.Click += new System.EventHandler(this.AddCounterToolStripMenuItemClick);
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			// 
			// selectAllToolStripMenuItem
			// 
			this.selectAllToolStripMenuItem.LanguageKey = "editor_selectall";
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			this.selectAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.selectAllToolStripMenuItem.Click += new System.EventHandler(this.SelectAllToolStripMenuItemClick);
			// 
			// removeObjectToolStripMenuItem
			// 
			this.removeObjectToolStripMenuItem.Enabled = false;
			this.removeObjectToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("removeObjectToolStripMenuItem.Image")));
			this.removeObjectToolStripMenuItem.LanguageKey = "editor_deleteelement";
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
			this.arrangeToolStripMenuItem.LanguageKey = "editor_arrange";
			this.arrangeToolStripMenuItem.Name = "arrangeToolStripMenuItem";
			// 
			// upToTopToolStripMenuItem
			// 
			this.upToTopToolStripMenuItem.Enabled = false;
			this.upToTopToolStripMenuItem.LanguageKey = "editor_uptotop";
			this.upToTopToolStripMenuItem.Name = "upToTopToolStripMenuItem";
			this.upToTopToolStripMenuItem.ShortcutKeyDisplayString = "Home";
			this.upToTopToolStripMenuItem.Click += new System.EventHandler(this.UpToTopToolStripMenuItemClick);
			// 
			// upOneLevelToolStripMenuItem
			// 
			this.upOneLevelToolStripMenuItem.Enabled = false;
			this.upOneLevelToolStripMenuItem.LanguageKey = "editor_uponelevel";
			this.upOneLevelToolStripMenuItem.Name = "upOneLevelToolStripMenuItem";
			this.upOneLevelToolStripMenuItem.ShortcutKeyDisplayString = "PgUp";
			this.upOneLevelToolStripMenuItem.Click += new System.EventHandler(this.UpOneLevelToolStripMenuItemClick);
			// 
			// downOneLevelToolStripMenuItem
			// 
			this.downOneLevelToolStripMenuItem.Enabled = false;
			this.downOneLevelToolStripMenuItem.LanguageKey = "editor_downonelevel";
			this.downOneLevelToolStripMenuItem.Name = "downOneLevelToolStripMenuItem";
			this.downOneLevelToolStripMenuItem.ShortcutKeyDisplayString = "PgDn";
			this.downOneLevelToolStripMenuItem.Click += new System.EventHandler(this.DownOneLevelToolStripMenuItemClick);
			// 
			// downToBottomToolStripMenuItem
			// 
			this.downToBottomToolStripMenuItem.Enabled = false;
			this.downToBottomToolStripMenuItem.LanguageKey = "editor_downtobottom";
			this.downToBottomToolStripMenuItem.Name = "downToBottomToolStripMenuItem";
			this.downToBottomToolStripMenuItem.ShortcutKeyDisplayString = "End";
			this.downToBottomToolStripMenuItem.Click += new System.EventHandler(this.DownToBottomToolStripMenuItemClick);
			// 
			// saveElementsToolStripMenuItem
			// 
			this.saveElementsToolStripMenuItem.LanguageKey = "editor_save_objects";
			this.saveElementsToolStripMenuItem.Name = "saveElementsToolStripMenuItem";
			this.saveElementsToolStripMenuItem.Click += new System.EventHandler(this.SaveElementsToolStripMenuItemClick);
			// 
			// loadElementsToolStripMenuItem
			// 
			this.loadElementsToolStripMenuItem.LanguageKey = "editor_load_objects";
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
			this.helpToolStripMenuItem1.Image = ((System.Drawing.Image)(resources.GetObject("helpToolStripMenuItem1.Image")));
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
			this.destinationsToolStrip.ClickThrough = true;
			this.destinationsToolStrip.ImageScalingSize = coreConfiguration.IconSize;
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
			this.btnSave.Image = ((System.Drawing.Image)(resources.GetObject("btnSave.Image")));
			this.btnSave.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnSave.LanguageKey = "editor_save";
			this.btnSave.Name = "btnSave";
			this.btnSave.Click += new System.EventHandler(this.BtnSaveClick);
			// 
			// btnClipboard
			// 
			this.btnClipboard.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnClipboard.Image = ((System.Drawing.Image)(resources.GetObject("btnClipboard.Image")));
			this.btnClipboard.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnClipboard.LanguageKey = "editor_copyimagetoclipboard";
			this.btnClipboard.Name = "btnClipboard";
			this.btnClipboard.Click += new System.EventHandler(this.BtnClipboardClick);
			// 
			// btnPrint
			// 
			this.btnPrint.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnPrint.Image = ((System.Drawing.Image)(resources.GetObject("btnPrint.Image")));
			this.btnPrint.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnPrint.LanguageKey = "editor_print";
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
			this.btnDelete.Image = ((System.Drawing.Image)(resources.GetObject("btnDelete.Image")));
			this.btnDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnDelete.LanguageKey = "editor_deleteelement";
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
			this.btnCut.Image = ((System.Drawing.Image)(resources.GetObject("btnCut.Image")));
			this.btnCut.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCut.LanguageKey = "editor_cuttoclipboard";
			this.btnCut.Name = "btnCut";
			this.btnCut.Click += new System.EventHandler(this.BtnCutClick);
			// 
			// btnCopy
			// 
			this.btnCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnCopy.Enabled = false;
			this.btnCopy.Image = ((System.Drawing.Image)(resources.GetObject("btnCopy.Image")));
			this.btnCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCopy.LanguageKey = "editor_copytoclipboard";
			this.btnCopy.Name = "btnCopy";
			this.btnCopy.Click += new System.EventHandler(this.BtnCopyClick);
			// 
			// btnPaste
			// 
			this.btnPaste.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnPaste.Enabled = false;
			this.btnPaste.Image = ((System.Drawing.Image)(resources.GetObject("btnPaste.Image")));
			this.btnPaste.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnPaste.LanguageKey = "editor_pastefromclipboard";
			this.btnPaste.Name = "btnPaste";
			this.btnPaste.Click += new System.EventHandler(this.BtnPasteClick);
			// 
			// btnUndo
			// 
			this.btnUndo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnUndo.Enabled = false;
			this.btnUndo.Image = ((System.Drawing.Image)(resources.GetObject("btnUndo.Image")));
			this.btnUndo.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnUndo.Name = "btnUndo";
			this.btnUndo.Click += new System.EventHandler(this.BtnUndoClick);
			// 
			// btnRedo
			// 
			this.btnRedo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnRedo.Enabled = false;
			this.btnRedo.Image = ((System.Drawing.Image)(resources.GetObject("btnRedo.Image")));
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
			this.btnSettings.Image = ((System.Drawing.Image)(resources.GetObject("btnSettings.Image")));
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
			this.btnHelp.Image = ((System.Drawing.Image)(resources.GetObject("btnHelp.Image")));
			this.btnHelp.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnHelp.LanguageKey = "contextmenu_help";
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Text = "Help";
			this.btnHelp.Click += new System.EventHandler(this.BtnHelpClick);
			// 
			// propertiesToolStrip
			// 
			this.propertiesToolStrip.ClickThrough = true;
			this.propertiesToolStrip.ImageScalingSize = coreConfiguration.IconSize;
			this.propertiesToolStrip.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertiesToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.propertiesToolStrip.MinimumSize = new System.Drawing.Size(150, coreConfiguration.IconSize.Height + 10);
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
									this.previewQualityLabel,
									this.previewQualityUpDown,
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
									this.cropModeButton,
									this.counterLabel,
									this.counterUpDown});
			// 
			// obfuscateModeButton
			// 
			this.obfuscateModeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.obfuscateModeButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.pixelizeToolStripMenuItem,
									this.blurToolStripMenuItem});
			this.obfuscateModeButton.Image = ((System.Drawing.Image)(resources.GetObject("obfuscateModeButton.Image")));
			this.obfuscateModeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.obfuscateModeButton.LanguageKey = "editor_obfuscate_mode";
			this.obfuscateModeButton.Name = "obfuscateModeButton";
			this.obfuscateModeButton.SelectedTag = FilterContainer.PreparedFilter.BLUR;
			this.obfuscateModeButton.Tag = FilterContainer.PreparedFilter.BLUR;
			// 
			this.obfuscateModeButton.DropDownItemClicked += FilterPresetDropDownItemClicked;
			// pixelizeToolStripMenuItem
			// 
			this.pixelizeToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("pixelizeToolStripMenuItem.Image")));
			this.pixelizeToolStripMenuItem.LanguageKey = "editor_obfuscate_pixelize";
			this.pixelizeToolStripMenuItem.Name = "pixelizeToolStripMenuItem";
			this.pixelizeToolStripMenuItem.Tag = FilterContainer.PreparedFilter.PIXELIZE;
			// 
			// blurToolStripMenuItem
			// 
			this.blurToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("blurToolStripMenuItem.Image")));
			this.blurToolStripMenuItem.LanguageKey = "editor_obfuscate_blur";
			this.blurToolStripMenuItem.Name = "blurToolStripMenuItem";
			this.blurToolStripMenuItem.Tag = FilterContainer.PreparedFilter.BLUR;

			// 
			// cropModeButton
			// 
			this.cropModeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.cropModeButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.defaultCropModeToolStripMenuItem,
									this.verticalCropModeToolStripMenuItem,
									this.horizontalCropModeToolStripMenuItem,
									this.autoCropModeToolStripMenuItem});
			this.cropModeButton.Image = ((System.Drawing.Image)(resources.GetObject("btnCrop.Image")));
			this.cropModeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cropModeButton.LanguageKey = "editor_crop_mode";
			this.cropModeButton.Name = "cropModeButton";
			this.cropModeButton.SelectedTag = CropContainer.CropModes.Default;
			this.cropModeButton.Tag = CropContainer.CropModes.Default;
            this.cropModeButton.DropDownItemClicked += CropStyleDropDownItemClicked;
			// 
			// defaultCropStyleToolStripMenuItem
			// 
			this.defaultCropModeToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnCrop.Image")));
			this.defaultCropModeToolStripMenuItem.LanguageKey = "editor_cropmode_default";
			this.defaultCropModeToolStripMenuItem.Name = "defaultCropModeToolStripMenuItem";
			this.defaultCropModeToolStripMenuItem.Tag = CropContainer.CropModes.Default;

			// 
			// verticalCropStyleToolStripMenuItem
			// 
			this.verticalCropModeToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("CropVertical.Image")));
			this.verticalCropModeToolStripMenuItem.LanguageKey = "editor_cropmode_vertical";
			this.verticalCropModeToolStripMenuItem.Name = "verticalCropModeToolStripMenuItem";
			this.verticalCropModeToolStripMenuItem.Tag = CropContainer.CropModes.Vertical;

			// 
			// horizontalCropStyleToolStripMenuItem
			// 
			this.horizontalCropModeToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("CropHorizontal.Image")));
			this.horizontalCropModeToolStripMenuItem.LanguageKey = "editor_cropmode_horizontal";
			this.horizontalCropModeToolStripMenuItem.Name = "horizontalCropModeToolStripMenuItem";
			this.horizontalCropModeToolStripMenuItem.Tag = CropContainer.CropModes.Horizontal;

			// 
			// autoCropModeToolStripMenuItem
			// 
			this.autoCropModeToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("AutoCrop.Image")));
			this.autoCropModeToolStripMenuItem.LanguageKey = "editor_cropmode_auto";
			this.autoCropModeToolStripMenuItem.Name = "autoCropModeToolStripMenuItem";
			this.autoCropModeToolStripMenuItem.Tag = CropContainer.CropModes.AutoCrop;

			// 
			// highlightModeButton
			// 
			this.highlightModeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.highlightModeButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.textHighlightMenuItem,
									this.areaHighlightMenuItem,
									this.grayscaleHighlightMenuItem,
									this.magnifyMenuItem});
			this.highlightModeButton.Image = ((System.Drawing.Image)(resources.GetObject("highlightModeButton.Image")));
			this.highlightModeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.highlightModeButton.LanguageKey = "editor_highlight_mode";
			this.highlightModeButton.Name = "highlightModeButton";
			this.highlightModeButton.SelectedTag = FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT;
			this.highlightModeButton.Tag = FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT;
            this.highlightModeButton.DropDownItemClicked += FilterPresetDropDownItemClicked;
			// 
			// textHighlightMenuItem
			// 
			this.textHighlightMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("textHighlightMenuItem.Image")));
			this.textHighlightMenuItem.LanguageKey = "editor_highlight_text";
			this.textHighlightMenuItem.Name = "textHighlightMenuItem";
			this.textHighlightMenuItem.Tag = FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT;
			// 
			// areaHighlightMenuItem
			// 
			this.areaHighlightMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("areaHighlightMenuItem.Image")));
			this.areaHighlightMenuItem.LanguageKey = "editor_highlight_area";
			this.areaHighlightMenuItem.Name = "areaHighlightMenuItem";
			this.areaHighlightMenuItem.Tag = FilterContainer.PreparedFilter.AREA_HIGHLIGHT;
			// 
			// grayscaleHighlightMenuItem
			// 
			this.grayscaleHighlightMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("grayscaleHighlightMenuItem.Image")));
			this.grayscaleHighlightMenuItem.LanguageKey = "editor_highlight_grayscale";
			this.grayscaleHighlightMenuItem.Name = "grayscaleHighlightMenuItem";
			this.grayscaleHighlightMenuItem.Tag = FilterContainer.PreparedFilter.GRAYSCALE;
			// 
			// magnifyMenuItem
			// 
			this.magnifyMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("magnifyMenuItem.Image")));
			this.magnifyMenuItem.LanguageKey = "editor_highlight_magnify";
			this.magnifyMenuItem.Name = "magnifyMenuItem";
			this.magnifyMenuItem.Tag = FilterContainer.PreparedFilter.MAGNIFICATION;
			// 
			// btnFillColor
			// 
			this.btnFillColor.BackColor = System.Drawing.Color.Transparent;
			this.btnFillColor.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnFillColor.Image = ((System.Drawing.Image)(resources.GetObject("btnFillColor.Image")));
			this.btnFillColor.LanguageKey = "editor_backcolor";
			this.btnFillColor.Name = "btnFillColor";
			this.btnFillColor.SelectedColor = System.Drawing.Color.Transparent;
			// 
			// btnLineColor
			// 
			this.btnLineColor.BackColor = System.Drawing.Color.Transparent;
			this.btnLineColor.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnLineColor.Image = ((System.Drawing.Image)(resources.GetObject("btnLineColor.Image")));
			this.btnLineColor.LanguageKey = "editor_forecolor";
			this.btnLineColor.Name = "btnLineColor";
			this.btnLineColor.SelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(203)))), ((int)(((byte)(222)))), ((int)(((byte)(250)))));
			// 
			// counterLabel
			// 
			this.counterLabel.LanguageKey = "editor_counter_startvalue";
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
			this.lineThicknessLabel.LanguageKey = "editor_thickness";
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
			this.fontFamilyComboBox.MaxDropDownItems = 20;
			this.fontFamilyComboBox.Name = "fontFamilyComboBox";
			this.fontFamilyComboBox.Size = new System.Drawing.Size(200, 20);
			this.fontFamilyComboBox.Text = "Aharoni";
			this.fontFamilyComboBox.Padding = new System.Windows.Forms.Padding(2,0,0,2);
			this.fontFamilyComboBox.GotFocus += new System.EventHandler(this.ToolBarFocusableElementGotFocus);
			this.fontFamilyComboBox.LostFocus += new System.EventHandler(this.ToolBarFocusableElementLostFocus);
            this.fontFamilyComboBox.PropertyChanged += FontPropertyChanged;
			// 
			// fontSizeLabel
			// 
			this.fontSizeLabel.LanguageKey = "editor_fontsize";
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
			this.fontBoldButton.Image = ((System.Drawing.Image)(resources.GetObject("fontBoldButton.Image")));
			this.fontBoldButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.fontBoldButton.LanguageKey = "editor_bold";
			this.fontBoldButton.Name = "fontBoldButton";
			this.fontBoldButton.Text = "Bold";
			this.fontBoldButton.Click += new System.EventHandler(this.FontBoldButtonClick);
			// 
			// fontItalicButton
			// 
			this.fontItalicButton.CheckOnClick = true;
			this.fontItalicButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.fontItalicButton.Image = ((System.Drawing.Image)(resources.GetObject("fontItalicButton.Image")));
			this.fontItalicButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.fontItalicButton.LanguageKey = "editor_italic";
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
			this.textVerticalAlignmentButton.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignMiddle.Image")));
			this.textVerticalAlignmentButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.textVerticalAlignmentButton.LanguageKey = "editor_align_vertical";
			this.textVerticalAlignmentButton.Name = "textVerticalAlignmentButton";
			this.textVerticalAlignmentButton.SelectedTag = System.Drawing.StringAlignment.Center;
			this.textVerticalAlignmentButton.Tag = System.Drawing.StringAlignment.Center;
			// 
			// alignTopToolStripMenuItem
			// 
			this.alignTopToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignTopToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignTop.Image")));
			this.alignTopToolStripMenuItem.LanguageKey = "editor_align_top";
			this.alignTopToolStripMenuItem.Name = "alignTopToolStripMenuItem";
			this.alignTopToolStripMenuItem.Tag = System.Drawing.StringAlignment.Near;
			// 
			// alignMiddleToolStripMenuItem
			// 
			this.alignMiddleToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignMiddleToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignMiddle.Image")));
			this.alignMiddleToolStripMenuItem.LanguageKey = "editor_align_middle";
			this.alignMiddleToolStripMenuItem.Name = "alignMiddleToolStripMenuItem";
			this.alignMiddleToolStripMenuItem.Tag = System.Drawing.StringAlignment.Center;
			// 
			// alignBottomToolStripMenuItem
			// 
			this.alignBottomToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignBottomToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignBottom.Image")));
			this.alignBottomToolStripMenuItem.LanguageKey = "editor_align_bottom";
			this.alignBottomToolStripMenuItem.Name = "alignBottomToolStripMenuItem";
			this.alignBottomToolStripMenuItem.Tag = System.Drawing.StringAlignment.Far;
			// 
			// blurRadiusLabel
			// 
			this.blurRadiusLabel.LanguageKey = "editor_blur_radius";
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
			this.brightnessLabel.LanguageKey = "editor_brightness";
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
			// previewQualityLabel
			// 
			this.previewQualityLabel.LanguageKey = "editor_preview_quality";
			this.previewQualityLabel.Name = "previewQualityLabel";
			this.previewQualityLabel.Text = "Preview quality";
			// 
			// previewQualityUpDown
			// 
			this.previewQualityUpDown.DecimalPlaces = 0;
			this.previewQualityUpDown.Increment = new decimal(new int[] {
									10,
									0,
									0,
									0});
			this.previewQualityUpDown.Maximum = new decimal(new int[] {
									100,
									0,
									0,
									0});
			this.previewQualityUpDown.Minimum = new decimal(new int[] {
									10,
									0,
									0,
									0});
			this.previewQualityUpDown.Name = "previewQualityUpDown";
			this.previewQualityUpDown.Text = "50";
			this.previewQualityUpDown.Value = new decimal(new int[] {
									50,
									0,
									0,
									0});
			this.previewQualityUpDown.GotFocus += new System.EventHandler(this.ToolBarFocusableElementGotFocus);
			this.previewQualityUpDown.LostFocus += new System.EventHandler(this.ToolBarFocusableElementLostFocus);
			// 
			// magnificationFactorLabel
			// 
			this.magnificationFactorLabel.LanguageKey = "editor_magnification_factor";
			this.magnificationFactorLabel.Name = "magnificationFactorLabel";
			this.magnificationFactorLabel.Tag = FilterContainer.PreparedFilter.MAGNIFICATION;
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
			this.pixelSizeLabel.LanguageKey = "editor_pixel_size";
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
			this.arrowHeadsLabel.LanguageKey = "editor_pixel_size";
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
			this.arrowHeadsDropDownButton.Image = ((System.Drawing.Image)(resources.GetObject("arrowHeadsDropDownButton.Image")));
			this.arrowHeadsDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.arrowHeadsDropDownButton.LanguageKey = "editor_arrowheads";
			this.arrowHeadsDropDownButton.Name = "arrowHeadsDropDownButton";
			// 
			// arrowHeadStartMenuItem
			// 
			this.arrowHeadStartMenuItem.LanguageKey = "editor_arrowheads_start";
			this.arrowHeadStartMenuItem.Name = "arrowHeadStartMenuItem";
			this.arrowHeadStartMenuItem.Tag = ArrowContainer.ArrowHeadCombination.START_POINT;
			this.arrowHeadStartMenuItem.Click += new System.EventHandler(this.ArrowHeadsToolStripMenuItemClick);
			// 
			// arrowHeadEndMenuItem
			// 
			this.arrowHeadEndMenuItem.LanguageKey = "editor_arrowheads_end";
			this.arrowHeadEndMenuItem.Name = "arrowHeadEndMenuItem";
			this.arrowHeadEndMenuItem.Tag = ArrowContainer.ArrowHeadCombination.END_POINT;
			this.arrowHeadEndMenuItem.Click += new System.EventHandler(this.ArrowHeadsToolStripMenuItemClick);
			// 
			// arrowHeadBothMenuItem
			// 
			this.arrowHeadBothMenuItem.LanguageKey = "editor_arrowheads_both";
			this.arrowHeadBothMenuItem.Name = "arrowHeadBothMenuItem";
			this.arrowHeadBothMenuItem.Tag = ArrowContainer.ArrowHeadCombination.BOTH;
			this.arrowHeadBothMenuItem.Click += new System.EventHandler(this.ArrowHeadsToolStripMenuItemClick);
			// 
			// arrowHeadNoneMenuItem
			// 
			this.arrowHeadNoneMenuItem.LanguageKey = "editor_arrowheads_none";
			this.arrowHeadNoneMenuItem.Name = "arrowHeadNoneMenuItem";
			this.arrowHeadNoneMenuItem.Tag = ArrowContainer.ArrowHeadCombination.NONE;
			this.arrowHeadNoneMenuItem.Click += new System.EventHandler(this.ArrowHeadsToolStripMenuItemClick);
			// 
			// shadowButton
			// 
			this.shadowButton.CheckOnClick = true;
			this.shadowButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.shadowButton.Image = ((System.Drawing.Image)(resources.GetObject("shadowButton.Image")));
			this.shadowButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.shadowButton.LanguageKey = "editor_shadow";
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
			this.btnConfirm.Image = ((System.Drawing.Image)(resources.GetObject("btnConfirm.Image")));
			this.btnConfirm.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnConfirm.LanguageKey = "editor_confirm";
			this.btnConfirm.Name = "btnConfirm";
			this.btnConfirm.Text = "Confirm";
			this.btnConfirm.Click += new System.EventHandler(this.BtnConfirmClick);
			// 
			// btnCancel
			// 
			this.btnCancel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnCancel.Image = ((System.Drawing.Image)(resources.GetObject("btnCancel.Image")));
			this.btnCancel.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCancel.LanguageKey = "editor_cancel";
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
			this.closeToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("closeToolStripMenuItem.Image")));
			this.closeToolStripMenuItem.LanguageKey = "editor_close";
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
			this.copyPathMenuItem.LanguageKey = "editor_copypathtoclipboard";
			this.copyPathMenuItem.Name = "copyPathMenuItem";
			this.copyPathMenuItem.Click += new System.EventHandler(this.CopyPathMenuItemClick);
			// 
			// openDirectoryMenuItem
			// 
			this.openDirectoryMenuItem.LanguageKey = "editor_opendirinexplorer";
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
			this.textHorizontalAlignmentButton.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignCenter.Image")));
			this.textHorizontalAlignmentButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.textHorizontalAlignmentButton.LanguageKey = "editor_align_horizontal";
			this.textHorizontalAlignmentButton.Name = "textHorizontalAlignmentButton";
			this.textHorizontalAlignmentButton.SelectedTag = System.Drawing.StringAlignment.Center;
			this.textHorizontalAlignmentButton.Tag = System.Drawing.StringAlignment.Center;
			// 
			// alignLeftToolStripMenuItem
			// 
			this.alignLeftToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignLeftToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignLeft.Image")));
			this.alignLeftToolStripMenuItem.LanguageKey = "editor_align_left";
			this.alignLeftToolStripMenuItem.Name = "alignLeftToolStripMenuItem";
			this.alignLeftToolStripMenuItem.Tag = System.Drawing.StringAlignment.Near;
			// 
			// alignCenterToolStripMenuItem
			// 
			this.alignCenterToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignCenterToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignCenter.Image")));
			this.alignCenterToolStripMenuItem.LanguageKey = "editor_align_center";
			this.alignCenterToolStripMenuItem.Name = "alignCenterToolStripMenuItem";
			this.alignCenterToolStripMenuItem.Tag = System.Drawing.StringAlignment.Center;
			// 
			// alignRightToolStripMenuItem
			// 
			this.alignRightToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignRightToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignRight.Image")));
			this.alignRightToolStripMenuItem.LanguageKey = "editor_align_right";
			this.alignRightToolStripMenuItem.Name = "alignRightToolStripMenuItem";
			this.alignRightToolStripMenuItem.Tag = System.Drawing.StringAlignment.Far;
			// 
			// zoomMainMenuItem
			// 
			this.zoomMainMenuItem.DropDown = this.zoomMenuStrip;
			this.zoomMainMenuItem.Name = "zoomMainMenuItem";
			this.zoomMainMenuItem.Size = new System.Drawing.Size(51, 20);
			this.zoomMainMenuItem.Text = "Zoom";
			// 
			// zoomMenuStrip
			// 
			this.zoomMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.zoomInMenuItem,
			this.zoomOutMenuItem,
			this.zoomMenuSeparator1,
			this.zoomBestFitMenuItem,
			this.zoomMenuSeparator2,
			this.zoom25MenuItem,
			this.zoom50MenuItem,
			this.zoom66MenuItem,
			this.zoom75MenuItem,
			this.zoomMenuSeparator3,
			this.zoomActualSizeMenuItem,
			this.zoomMenuSeparator4,
			this.zoom200MenuItem,
			this.zoom300MenuItem,
			this.zoom400MenuItem,
			this.zoom600MenuItem});
			this.zoomMenuStrip.Name = "zoomMenuStrip";
			this.zoomMenuStrip.Size = new System.Drawing.Size(210, 292);
			// 
			// zoomInMenuItem
			// 
			this.zoomInMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("zoomInMenuItem.Image")));
			this.zoomInMenuItem.Name = "zoomInMenuItem";
			this.zoomInMenuItem.ShortcutKeyDisplayString = "Ctrl++";
			this.zoomInMenuItem.Size = new System.Drawing.Size(209, 22);
			this.zoomInMenuItem.Text = "Zoom In";
			this.zoomInMenuItem.Click += new System.EventHandler(this.ZoomInMenuItemClick);
			// 
			// zoomOutMenuItem
			// 
			this.zoomOutMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("zoomOutMenuItem.Image")));
			this.zoomOutMenuItem.Name = "zoomOutMenuItem";
			this.zoomOutMenuItem.ShortcutKeyDisplayString = "Ctrl+-";
			this.zoomOutMenuItem.Size = new System.Drawing.Size(209, 22);
			this.zoomOutMenuItem.Text = "Zoom Out";
			this.zoomOutMenuItem.Click += new System.EventHandler(this.ZoomOutMenuItemClick);
			// 
			// zoomMenuSeparator1
			// 
			this.zoomMenuSeparator1.Name = "zoomMenuSeparator1";
			this.zoomMenuSeparator1.Size = new System.Drawing.Size(206, 6);
			// 
			// zoomBestFitMenuItem
			// 
			this.zoomBestFitMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("zoomBestFitMenuItem.Image")));
			this.zoomBestFitMenuItem.Name = "zoomBestFitMenuItem";
			this.zoomBestFitMenuItem.ShortcutKeyDisplayString = "Ctrl+9";
			this.zoomBestFitMenuItem.Size = new System.Drawing.Size(209, 22);
			this.zoomBestFitMenuItem.Text = "Best Fit";
			this.zoomBestFitMenuItem.Click += new System.EventHandler(this.ZoomBestFitMenuItemClick);
			// 
			// zoomMenuSeparator2
			// 
			this.zoomMenuSeparator2.Name = "zoomMenuSeparator2";
			this.zoomMenuSeparator2.Size = new System.Drawing.Size(206, 6);
			// 
			// zoom25MenuItem
			// 
			this.zoom25MenuItem.Name = "zoom25MenuItem";
			this.zoom25MenuItem.Size = new System.Drawing.Size(209, 22);
			this.zoom25MenuItem.Tag = "1/4";
			this.zoom25MenuItem.Text = "25%";
			this.zoom25MenuItem.Click += new System.EventHandler(this.ZoomSetValueMenuItemClick);
			// 
			// zoom50MenuItem
			// 
			this.zoom50MenuItem.Name = "zoom50MenuItem";
			this.zoom50MenuItem.Size = new System.Drawing.Size(209, 22);
			this.zoom50MenuItem.Tag = "1/2";
			this.zoom50MenuItem.Text = "50%";
			this.zoom50MenuItem.Click += new System.EventHandler(this.ZoomSetValueMenuItemClick);
			// 
			// zoom66MenuItem
			// 
			this.zoom66MenuItem.Name = "zoom66MenuItem";
			this.zoom66MenuItem.Size = new System.Drawing.Size(209, 22);
			this.zoom66MenuItem.Tag = "2/3";
			this.zoom66MenuItem.Text = "66%";
			this.zoom66MenuItem.Click += new System.EventHandler(this.ZoomSetValueMenuItemClick);
			// 
			// zoom75MenuItem
			// 
			this.zoom75MenuItem.Name = "zoom75MenuItem";
			this.zoom75MenuItem.Size = new System.Drawing.Size(209, 22);
			this.zoom75MenuItem.Tag = "3/4";
			this.zoom75MenuItem.Text = "75%";
			this.zoom75MenuItem.Click += new System.EventHandler(this.ZoomSetValueMenuItemClick);
			// 
			// zoomMenuSeparator3
			// 
			this.zoomMenuSeparator3.Name = "zoomMenuSeparator3";
			this.zoomMenuSeparator3.Size = new System.Drawing.Size(206, 6);
			// 
			// zoomActualSizeMenuItem
			// 
			this.zoomActualSizeMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("zoomActualSizeMenuItem.Image")));
			this.zoomActualSizeMenuItem.Name = "zoomActualSizeMenuItem";
			this.zoomActualSizeMenuItem.ShortcutKeyDisplayString = "Ctrl+0";
			this.zoomActualSizeMenuItem.Size = new System.Drawing.Size(209, 22);
			this.zoomActualSizeMenuItem.Tag = "1/1";
			this.zoomActualSizeMenuItem.Text = "100% - Actual Size";
			this.zoomActualSizeMenuItem.Click += new System.EventHandler(this.ZoomSetValueMenuItemClick);
			// 
			// zoomMenuSeparator4
			// 
			this.zoomMenuSeparator4.Name = "zoomMenuSeparator4";
			this.zoomMenuSeparator4.Size = new System.Drawing.Size(206, 6);
			// 
			// zoom200MenuItem
			// 
			this.zoom200MenuItem.Name = "zoom200MenuItem";
			this.zoom200MenuItem.Size = new System.Drawing.Size(209, 22);
			this.zoom200MenuItem.Tag = "2/1";
			this.zoom200MenuItem.Text = "200%";
			this.zoom200MenuItem.Click += new System.EventHandler(this.ZoomSetValueMenuItemClick);
			// 
			// zoom300MenuItem
			// 
			this.zoom300MenuItem.Name = "zoom300MenuItem";
			this.zoom300MenuItem.Size = new System.Drawing.Size(209, 22);
			this.zoom300MenuItem.Tag = "3/1";
			this.zoom300MenuItem.Text = "300%";
			this.zoom300MenuItem.Click += new System.EventHandler(this.ZoomSetValueMenuItemClick);
			// 
			// zoom400MenuItem
			// 
			this.zoom400MenuItem.Name = "zoom400MenuItem";
			this.zoom400MenuItem.Size = new System.Drawing.Size(209, 22);
			this.zoom400MenuItem.Tag = "4/1";
			this.zoom400MenuItem.Text = "400%";
			this.zoom400MenuItem.Click += new System.EventHandler(this.ZoomSetValueMenuItemClick);
			// 
			// zoom600MenuItem
			// 
			this.zoom600MenuItem.Name = "zoom600MenuItem";
			this.zoom600MenuItem.Size = new System.Drawing.Size(209, 22);
			this.zoom600MenuItem.Tag = "6/1";
			this.zoom600MenuItem.Text = "600%";
			this.zoom600MenuItem.Click += new System.EventHandler(this.ZoomSetValueMenuItemClick);
			// 
			// statusStripSpacer
			// 
			this.statusStripSpacer.Name = "statusStripSpacer";
			this.statusStripSpacer.Size = new System.Drawing.Size(599, 19);
			this.statusStripSpacer.Spring = true;
			// 
			// zoomStatusDropDownBtn
			// 
			this.zoomStatusDropDownBtn.DropDown = this.zoomMenuStrip;
			this.zoomStatusDropDownBtn.Image = ((System.Drawing.Image)(resources.GetObject("zoomStatusDropDownBtn.Image")));
			this.zoomStatusDropDownBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.zoomStatusDropDownBtn.Name = "zoomStatusDropDownBtn";
			this.zoomStatusDropDownBtn.Size = new System.Drawing.Size(64, 22);
			this.zoomStatusDropDownBtn.Text = "100%";
			// 
			// ImageEditorForm
			// 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(785, 485);
			this.Controls.Add(this.topToolStripContainer);
			this.KeyPreview = true;
			this.LanguageKey = "editor_title";
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
			this.zoomMenuStrip.ResumeLayout(false);
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
		private BindableToolStripDropDownButton cropModeButton;
		private GreenshotToolStripMenuItem defaultCropModeToolStripMenuItem;
		private GreenshotToolStripMenuItem verticalCropModeToolStripMenuItem;
		private GreenshotToolStripMenuItem horizontalCropModeToolStripMenuItem;
		private GreenshotToolStripMenuItem autoCropModeToolStripMenuItem;
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
		private ToolStripNumericUpDown previewQualityUpDown;
		private GreenshotToolStripLabel previewQualityLabel;
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
		private System.Windows.Forms.ContextMenuStrip zoomMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem zoomInMenuItem;
		private System.Windows.Forms.ToolStripMenuItem zoomOutMenuItem;
		private System.Windows.Forms.ToolStripSeparator zoomMenuSeparator1;
		private System.Windows.Forms.ToolStripMenuItem zoomBestFitMenuItem;
		private System.Windows.Forms.ToolStripSeparator zoomMenuSeparator2;
		private System.Windows.Forms.ToolStripMenuItem zoom25MenuItem;
		private System.Windows.Forms.ToolStripMenuItem zoom50MenuItem;
		private System.Windows.Forms.ToolStripMenuItem zoom66MenuItem;
		private System.Windows.Forms.ToolStripMenuItem zoom75MenuItem;
		private System.Windows.Forms.ToolStripSeparator zoomMenuSeparator3;
		private System.Windows.Forms.ToolStripMenuItem zoomActualSizeMenuItem;
		private System.Windows.Forms.ToolStripSeparator zoomMenuSeparator4;
		private System.Windows.Forms.ToolStripMenuItem zoom200MenuItem;
		private System.Windows.Forms.ToolStripMenuItem zoom300MenuItem;
		private System.Windows.Forms.ToolStripMenuItem zoom400MenuItem;
		private System.Windows.Forms.ToolStripMenuItem zoom600MenuItem;
		private System.Windows.Forms.ToolStripDropDownButton zoomStatusDropDownBtn;
		private System.Windows.Forms.ToolStripMenuItem zoomMainMenuItem;
		private System.Windows.Forms.ToolStripStatusLabel statusStripSpacer;
	}
}
