/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
namespace Greenshot {
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
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImageEditorForm));
			this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.dimensionsLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.panel1 = new GreenshotPlugin.Controls.NonJumpingPanel();
			this.toolStrip2 = new Greenshot.Controls.ToolStripEx();
			this.btnCursor = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.btnRect = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.btnEllipse = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.btnLine = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.btnArrow = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.btnFreehand = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.btnText = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
			this.btnHighlight = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.btnObfuscate = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.toolStripSplitButton1 = new GreenshotPlugin.Controls.GreenshotToolStripDropDownButton();
			this.addBorderToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.addDropshadowToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.tornEdgesToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.grayscaleToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.invertToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
			this.btnCrop = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.rotateCwToolstripButton = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.rotateCcwToolstripButton = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.menuStrip1 = new Greenshot.Controls.MenuStripEx();
			this.fileStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.editToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator15 = new System.Windows.Forms.ToolStripSeparator();
			this.cutToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.copyToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.pasteToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.duplicateToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
			this.preferencesToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.autoCropToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripSeparator17 = new System.Windows.Forms.ToolStripSeparator();
			this.insert_window_toolstripmenuitem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.objectToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.addRectangleToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.addEllipseToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.drawLineToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.drawArrowToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.drawFreehandToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.addTextBoxToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.selectAllToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.removeObjectToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.arrangeToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.upToTopToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.upOneLevelToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.downOneLevelToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.downToBottomToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.saveElementsToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.loadElementsToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.pluginToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.helpToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.helpToolStripMenuItem1 = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.aboutToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStrip1 = new Greenshot.Controls.ToolStripEx();
			this.btnSave = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.btnClipboard = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.btnPrint = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.btnDelete = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.btnCut = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.btnCopy = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.btnPaste = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.btnUndo = new System.Windows.Forms.ToolStripButton();
			this.btnRedo = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.btnSettings = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator16 = new System.Windows.Forms.ToolStripSeparator();
			this.btnHelp = new GreenshotPlugin.Controls.GreenshotToolStripButton();
			this.propertiesToolStrip = new Greenshot.Controls.ToolStripEx();
			this.obfuscateModeButton = new Greenshot.Controls.BindableToolStripDropDownButton();
			this.pixelizeToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.blurToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.highlightModeButton = new Greenshot.Controls.BindableToolStripDropDownButton();
			this.textHighlightMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.areaHighlightMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.grayscaleHighlightMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.magnifyMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.btnFillColor = new Greenshot.Controls.ToolStripColorButton();
			this.btnLineColor = new Greenshot.Controls.ToolStripColorButton();
			this.lineThicknessLabel = new GreenshotPlugin.Controls.GreenshotToolStripLabel();
			this.lineThicknessUpDown = new Greenshot.Controls.ToolStripNumericUpDown();
			this.fontFamilyComboBox = new Greenshot.Controls.FontFamilyComboBox();
			this.fontSizeLabel = new GreenshotPlugin.Controls.GreenshotToolStripLabel();
			this.fontSizeUpDown = new Greenshot.Controls.ToolStripNumericUpDown();
			this.fontBoldButton = new Greenshot.Controls.BindableToolStripButton();
			this.fontItalicButton = new Greenshot.Controls.BindableToolStripButton();
			this.textVerticalAlignmentButton = new Greenshot.Controls.BindableToolStripDropDownButton();
			this.alignTopToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.alignMiddleToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.alignBottomToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.blurRadiusLabel = new GreenshotPlugin.Controls.GreenshotToolStripLabel();
			this.blurRadiusUpDown = new Greenshot.Controls.ToolStripNumericUpDown();
			this.brightnessLabel = new GreenshotPlugin.Controls.GreenshotToolStripLabel();
			this.brightnessUpDown = new Greenshot.Controls.ToolStripNumericUpDown();
			this.previewQualityLabel = new GreenshotPlugin.Controls.GreenshotToolStripLabel();
			this.previewQualityUpDown = new Greenshot.Controls.ToolStripNumericUpDown();
			this.magnificationFactorLabel = new GreenshotPlugin.Controls.GreenshotToolStripLabel();
			this.magnificationFactorUpDown = new Greenshot.Controls.ToolStripNumericUpDown();
			this.pixelSizeLabel = new GreenshotPlugin.Controls.GreenshotToolStripLabel();
			this.pixelSizeUpDown = new Greenshot.Controls.ToolStripNumericUpDown();
			this.arrowHeadsLabel = new GreenshotPlugin.Controls.GreenshotToolStripLabel();
			this.arrowHeadsDropDownButton = new GreenshotPlugin.Controls.GreenshotToolStripDropDownButton();
			this.arrowHeadStartMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.arrowHeadEndMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.arrowHeadBothMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.arrowHeadNoneMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.shadowButton = new Greenshot.Controls.BindableToolStripButton();
			this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.btnConfirm = new Greenshot.Controls.BindableToolStripButton();
			this.btnCancel = new Greenshot.Controls.BindableToolStripButton();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.closeToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.fileSavedStatusContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.copyPathMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.openDirectoryMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.textHorizontalAlignmentButton = new Greenshot.Controls.BindableToolStripDropDownButton();
			this.alignLeftToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.alignCenterToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.alignRightToolStripMenuItem = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripContainer1.BottomToolStripPanel.SuspendLayout();
			this.toolStripContainer1.ContentPanel.SuspendLayout();
			this.toolStripContainer1.LeftToolStripPanel.SuspendLayout();
			this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
			this.toolStripContainer1.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.toolStrip2.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.propertiesToolStrip.SuspendLayout();
			this.fileSavedStatusContextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStripContainer1
			// 
			// 
			// toolStripContainer1.BottomToolStripPanel
			// 
			this.toolStripContainer1.BottomToolStripPanel.Controls.Add(this.statusStrip1);
			// 
			// toolStripContainer1.ContentPanel
			// 
			this.toolStripContainer1.ContentPanel.AutoScroll = true;
			this.toolStripContainer1.ContentPanel.Controls.Add(this.tableLayoutPanel1);
			this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(761, 385);
			this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			// 
			// toolStripContainer1.LeftToolStripPanel
			// 
			this.toolStripContainer1.LeftToolStripPanel.Controls.Add(this.toolStrip2);
			this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
			this.toolStripContainer1.Name = "toolStripContainer1";
			this.toolStripContainer1.Size = new System.Drawing.Size(785, 485);
			this.toolStripContainer1.TabIndex = 2;
			this.toolStripContainer1.Text = "toolStripContainer1";
			// 
			// toolStripContainer1.TopToolStripPanel
			// 
			this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.menuStrip1);
			this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
			this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.propertiesToolStrip);
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
			this.dimensionsLabel.Size = new System.Drawing.Size(52, 19);
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
			this.statusLabel.Size = new System.Drawing.Size(4, 19);
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
			// toolStrip2
			// 
			this.toolStrip2.ClickThrough = true;
			this.toolStrip2.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip2.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.btnCursor,
									this.toolStripSeparator1,
									this.btnRect,
									this.btnEllipse,
									this.btnLine,
									this.btnArrow,
									this.btnFreehand,
									this.btnText,
									this.toolStripSeparator14,
									this.btnHighlight,
									this.btnObfuscate,
									this.toolStripSplitButton1,
									this.toolStripSeparator13,
									this.btnCrop,
									this.rotateCwToolstripButton,
									this.rotateCcwToolstripButton});
			this.toolStrip2.Location = new System.Drawing.Point(0, 0);
			this.toolStrip2.Name = "toolStrip2";
			this.toolStrip2.Size = new System.Drawing.Size(24, 385);
			this.toolStrip2.Stretch = true;
			this.toolStrip2.TabIndex = 0;
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
			this.btnCursor.Size = new System.Drawing.Size(22, 20);
			this.btnCursor.Click += new System.EventHandler(this.BtnCursorClick);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(22, 6);
			// 
			// btnRect
			// 
			this.btnRect.CheckOnClick = true;
			this.btnRect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnRect.Image = ((System.Drawing.Image)(resources.GetObject("btnRect.Image")));
			this.btnRect.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnRect.LanguageKey = "editor_drawrectangle";
			this.btnRect.Name = "btnRect";
			this.btnRect.Size = new System.Drawing.Size(22, 20);
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
			this.btnEllipse.Size = new System.Drawing.Size(22, 20);
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
			this.btnLine.Size = new System.Drawing.Size(22, 20);
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
			this.btnArrow.Size = new System.Drawing.Size(22, 20);
			this.btnArrow.Click += new System.EventHandler(this.BtnArrowClick);
			// 
			// btnFreehand
			// 
			this.btnFreehand.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnFreehand.Image = ((System.Drawing.Image)(resources.GetObject("btnFreehand.Image")));
			this.btnFreehand.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnFreehand.LanguageKey = "editor_drawfreehand";
			this.btnFreehand.Name = "btnFreehand";
			this.btnFreehand.Size = new System.Drawing.Size(22, 20);
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
			this.btnText.Size = new System.Drawing.Size(22, 20);
			this.btnText.Click += new System.EventHandler(this.BtnTextClick);
			// 
			// toolStripSeparator14
			// 
			this.toolStripSeparator14.Name = "toolStripSeparator14";
			this.toolStripSeparator14.Size = new System.Drawing.Size(22, 6);
			// 
			// btnHighlight
			// 
			this.btnHighlight.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnHighlight.Image = ((System.Drawing.Image)(resources.GetObject("btnHighlight.Image")));
			this.btnHighlight.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnHighlight.LanguageKey = "editor_drawhighlighter";
			this.btnHighlight.Name = "btnHighlight";
			this.btnHighlight.Size = new System.Drawing.Size(22, 20);
			this.btnHighlight.Click += new System.EventHandler(this.BtnHighlightClick);
			// 
			// btnObfuscate
			// 
			this.btnObfuscate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnObfuscate.Image = ((System.Drawing.Image)(resources.GetObject("btnObfuscate.Image")));
			this.btnObfuscate.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnObfuscate.LanguageKey = "editor_obfuscate";
			this.btnObfuscate.Name = "btnObfuscate";
			this.btnObfuscate.Size = new System.Drawing.Size(22, 20);
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
			this.toolStripSplitButton1.Size = new System.Drawing.Size(22, 20);
			this.toolStripSplitButton1.Text = "toolStripSplitButton1";
			// 
			// addBorderToolStripMenuItem
			// 
			this.addBorderToolStripMenuItem.LanguageKey = "editor_border";
			this.addBorderToolStripMenuItem.Name = "addBorderToolStripMenuItem";
			this.addBorderToolStripMenuItem.Size = new System.Drawing.Size(67, 22);
			this.addBorderToolStripMenuItem.Click += new System.EventHandler(this.AddBorderToolStripMenuItemClick);
			// 
			// addDropshadowToolStripMenuItem
			// 
			this.addDropshadowToolStripMenuItem.LanguageKey = "editor_image_shadow";
			this.addDropshadowToolStripMenuItem.Name = "addDropshadowToolStripMenuItem";
			this.addDropshadowToolStripMenuItem.Size = new System.Drawing.Size(67, 22);
			this.addDropshadowToolStripMenuItem.Click += new System.EventHandler(this.AddDropshadowToolStripMenuItemClick);
			// 
			// tornEdgesToolStripMenuItem
			// 
			this.tornEdgesToolStripMenuItem.LanguageKey = "editor_torn_edge";
			this.tornEdgesToolStripMenuItem.Name = "tornEdgesToolStripMenuItem";
			this.tornEdgesToolStripMenuItem.Size = new System.Drawing.Size(67, 22);
			this.tornEdgesToolStripMenuItem.Click += new System.EventHandler(this.TornEdgesToolStripMenuItemClick);
			// 
			// grayscaleToolStripMenuItem
			// 
			this.grayscaleToolStripMenuItem.LanguageKey = "editor_grayscale";
			this.grayscaleToolStripMenuItem.Name = "grayscaleToolStripMenuItem";
			this.grayscaleToolStripMenuItem.Size = new System.Drawing.Size(67, 22);
			this.grayscaleToolStripMenuItem.Click += new System.EventHandler(this.GrayscaleToolStripMenuItemClick);
			// 
			// invertToolStripMenuItem
			// 
			this.invertToolStripMenuItem.LanguageKey = "editor_invert";
			this.invertToolStripMenuItem.Name = "invertToolStripMenuItem";
			this.invertToolStripMenuItem.Size = new System.Drawing.Size(67, 22);
			this.invertToolStripMenuItem.Click += new System.EventHandler(this.InvertToolStripMenuItemClick);
			// 
			// toolStripSeparator13
			// 
			this.toolStripSeparator13.Name = "toolStripSeparator13";
			this.toolStripSeparator13.Size = new System.Drawing.Size(22, 6);
			// 
			// btnCrop
			// 
			this.btnCrop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnCrop.Image = ((System.Drawing.Image)(resources.GetObject("btnCrop.Image")));
			this.btnCrop.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCrop.LanguageKey = "editor_crop";
			this.btnCrop.Name = "btnCrop";
			this.btnCrop.Size = new System.Drawing.Size(22, 20);
			this.btnCrop.Click += new System.EventHandler(this.BtnCropClick);
			// 
			// rotateCwToolstripButton
			// 
			this.rotateCwToolstripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.rotateCwToolstripButton.Image = ((System.Drawing.Image)(resources.GetObject("rotateCwToolstripButton.Image")));
			this.rotateCwToolstripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.rotateCwToolstripButton.LanguageKey = "editor_rotatecw";
			this.rotateCwToolstripButton.Name = "rotateCwToolstripButton";
			this.rotateCwToolstripButton.Size = new System.Drawing.Size(22, 20);
			this.rotateCwToolstripButton.Click += new System.EventHandler(this.RotateCwToolstripButtonClick);
			// 
			// rotateCcwToolstripButton
			// 
			this.rotateCcwToolstripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.rotateCcwToolstripButton.Image = ((System.Drawing.Image)(resources.GetObject("rotateCcwToolstripButton.Image")));
			this.rotateCcwToolstripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.rotateCcwToolstripButton.LanguageKey = "editor_rotateccw";
			this.rotateCcwToolstripButton.Name = "rotateCcwToolstripButton";
			this.rotateCcwToolstripButton.Size = new System.Drawing.Size(22, 20);
			this.rotateCcwToolstripButton.Click += new System.EventHandler(this.RotateCcwToolstripButtonClick);
			// 
			// menuStrip1
			// 
			this.menuStrip1.ClickThrough = true;
			this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.fileStripMenuItem,
									this.editToolStripMenuItem,
									this.objectToolStripMenuItem,
									this.pluginToolStripMenuItem,
									this.helpToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(785, 24);
			this.menuStrip1.TabIndex = 1;
			// 
			// fileStripMenuItem
			// 
			this.fileStripMenuItem.LanguageKey = "editor_file";
			this.fileStripMenuItem.Name = "fileStripMenuItem";
			this.fileStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileStripMenuItem.Text = "File";
			this.fileStripMenuItem.DropDownOpening += new System.EventHandler(this.FileMenuDropDownOpening);
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
			this.undoToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
			this.undoToolStripMenuItem.Text = "Undo";
			this.undoToolStripMenuItem.Click += new System.EventHandler(this.UndoToolStripMenuItemClick);
			// 
			// redoToolStripMenuItem
			// 
			this.redoToolStripMenuItem.Enabled = false;
			this.redoToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("redoToolStripMenuItem.Image")));
			this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
			this.redoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
			this.redoToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
			this.redoToolStripMenuItem.Text = "Redo";
			this.redoToolStripMenuItem.Click += new System.EventHandler(this.RedoToolStripMenuItemClick);
			// 
			// toolStripSeparator15
			// 
			this.toolStripSeparator15.Name = "toolStripSeparator15";
			this.toolStripSeparator15.Size = new System.Drawing.Size(141, 6);
			// 
			// cutToolStripMenuItem
			// 
			this.cutToolStripMenuItem.Enabled = false;
			this.cutToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("cutToolStripMenuItem.Image")));
			this.cutToolStripMenuItem.LanguageKey = "editor_cuttoclipboard";
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.cutToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
			this.cutToolStripMenuItem.Click += new System.EventHandler(this.CutToolStripMenuItemClick);
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.Enabled = false;
			this.copyToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("copyToolStripMenuItem.Image")));
			this.copyToolStripMenuItem.LanguageKey = "editor_copytoclipboard";
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.CopyToolStripMenuItemClick);
			// 
			// pasteToolStripMenuItem
			// 
			this.pasteToolStripMenuItem.Enabled = false;
			this.pasteToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("pasteToolStripMenuItem.Image")));
			this.pasteToolStripMenuItem.LanguageKey = "editor_pastefromclipboard";
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			this.pasteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.pasteToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
			this.pasteToolStripMenuItem.Click += new System.EventHandler(this.PasteToolStripMenuItemClick);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(141, 6);
			// 
			// duplicateToolStripMenuItem
			// 
			this.duplicateToolStripMenuItem.Enabled = false;
			this.duplicateToolStripMenuItem.LanguageKey = "editor_duplicate";
			this.duplicateToolStripMenuItem.Name = "duplicateToolStripMenuItem";
			this.duplicateToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.duplicateToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
			this.duplicateToolStripMenuItem.Click += new System.EventHandler(this.DuplicateToolStripMenuItemClick);
			// 
			// toolStripSeparator12
			// 
			this.toolStripSeparator12.Name = "toolStripSeparator12";
			this.toolStripSeparator12.Size = new System.Drawing.Size(141, 6);
			// 
			// preferencesToolStripMenuItem
			// 
			this.preferencesToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("preferencesToolStripMenuItem.Image")));
			this.preferencesToolStripMenuItem.LanguageKey = "contextmenu_settings";
			this.preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
			this.preferencesToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
			this.preferencesToolStripMenuItem.Click += new System.EventHandler(this.PreferencesToolStripMenuItemClick);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(141, 6);
			// 
			// autoCropToolStripMenuItem
			// 
			this.autoCropToolStripMenuItem.LanguageKey = "editor_autocrop";
			this.autoCropToolStripMenuItem.Name = "autoCropToolStripMenuItem";
			this.autoCropToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
			this.autoCropToolStripMenuItem.Click += new System.EventHandler(this.AutoCropToolStripMenuItemClick);
			// 
			// toolStripSeparator17
			// 
			this.toolStripSeparator17.Name = "toolStripSeparator17";
			this.toolStripSeparator17.Size = new System.Drawing.Size(141, 6);
			// 
			// insert_window_toolstripmenuitem
			// 
			this.insert_window_toolstripmenuitem.LanguageKey = "editor_insertwindow";
			this.insert_window_toolstripmenuitem.Name = "insert_window_toolstripmenuitem";
			this.insert_window_toolstripmenuitem.Size = new System.Drawing.Size(144, 22);
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
			this.addRectangleToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
			this.addRectangleToolStripMenuItem.Click += new System.EventHandler(this.AddRectangleToolStripMenuItemClick);
			// 
			// addEllipseToolStripMenuItem
			// 
			this.addEllipseToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("addEllipseToolStripMenuItem.Image")));
			this.addEllipseToolStripMenuItem.LanguageKey = "editor_drawellipse";
			this.addEllipseToolStripMenuItem.Name = "addEllipseToolStripMenuItem";
			this.addEllipseToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
			this.addEllipseToolStripMenuItem.Click += new System.EventHandler(this.AddEllipseToolStripMenuItemClick);
			// 
			// drawLineToolStripMenuItem
			// 
			this.drawLineToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("drawLineToolStripMenuItem.Image")));
			this.drawLineToolStripMenuItem.LanguageKey = "editor_drawline";
			this.drawLineToolStripMenuItem.Name = "drawLineToolStripMenuItem";
			this.drawLineToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
			this.drawLineToolStripMenuItem.Click += new System.EventHandler(this.DrawLineToolStripMenuItemClick);
			// 
			// drawArrowToolStripMenuItem
			// 
			this.drawArrowToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("drawArrowToolStripMenuItem.Image")));
			this.drawArrowToolStripMenuItem.LanguageKey = "editor_drawarrow";
			this.drawArrowToolStripMenuItem.Name = "drawArrowToolStripMenuItem";
			this.drawArrowToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
			this.drawArrowToolStripMenuItem.Click += new System.EventHandler(this.DrawArrowToolStripMenuItemClick);
			// 
			// drawFreehandToolStripMenuItem
			// 
			this.drawFreehandToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("drawFreehandToolStripMenuItem.Image")));
			this.drawFreehandToolStripMenuItem.LanguageKey = "editor_drawfreehand";
			this.drawFreehandToolStripMenuItem.Name = "drawFreehandToolStripMenuItem";
			this.drawFreehandToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
			this.drawFreehandToolStripMenuItem.Click += new System.EventHandler(this.DrawFreehandToolStripMenuItemClick);
			// 
			// addTextBoxToolStripMenuItem
			// 
			this.addTextBoxToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("addTextBoxToolStripMenuItem.Image")));
			this.addTextBoxToolStripMenuItem.LanguageKey = "editor_drawtextbox";
			this.addTextBoxToolStripMenuItem.Name = "addTextBoxToolStripMenuItem";
			this.addTextBoxToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
			this.addTextBoxToolStripMenuItem.Click += new System.EventHandler(this.AddTextBoxToolStripMenuItemClick);
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(106, 6);
			// 
			// selectAllToolStripMenuItem
			// 
			this.selectAllToolStripMenuItem.LanguageKey = "editor_selectall";
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			this.selectAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
			this.selectAllToolStripMenuItem.Click += new System.EventHandler(this.SelectAllToolStripMenuItemClick);
			// 
			// removeObjectToolStripMenuItem
			// 
			this.removeObjectToolStripMenuItem.Enabled = false;
			this.removeObjectToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("removeObjectToolStripMenuItem.Image")));
			this.removeObjectToolStripMenuItem.LanguageKey = "editor_deleteelement";
			this.removeObjectToolStripMenuItem.Name = "removeObjectToolStripMenuItem";
			this.removeObjectToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
			this.removeObjectToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
			this.removeObjectToolStripMenuItem.Click += new System.EventHandler(this.RemoveObjectToolStripMenuItemClick);
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(106, 6);
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
			this.arrangeToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
			// 
			// upToTopToolStripMenuItem
			// 
			this.upToTopToolStripMenuItem.Enabled = false;
			this.upToTopToolStripMenuItem.LanguageKey = "editor_uptotop";
			this.upToTopToolStripMenuItem.Name = "upToTopToolStripMenuItem";
			this.upToTopToolStripMenuItem.ShortcutKeyDisplayString = "Home";
			this.upToTopToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
			this.upToTopToolStripMenuItem.Click += new System.EventHandler(this.UpToTopToolStripMenuItemClick);
			// 
			// upOneLevelToolStripMenuItem
			// 
			this.upOneLevelToolStripMenuItem.Enabled = false;
			this.upOneLevelToolStripMenuItem.LanguageKey = "editor_uponelevel";
			this.upOneLevelToolStripMenuItem.Name = "upOneLevelToolStripMenuItem";
			this.upOneLevelToolStripMenuItem.ShortcutKeyDisplayString = "PgUp";
			this.upOneLevelToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
			this.upOneLevelToolStripMenuItem.Click += new System.EventHandler(this.UpOneLevelToolStripMenuItemClick);
			// 
			// downOneLevelToolStripMenuItem
			// 
			this.downOneLevelToolStripMenuItem.Enabled = false;
			this.downOneLevelToolStripMenuItem.LanguageKey = "editor_downonelevel";
			this.downOneLevelToolStripMenuItem.Name = "downOneLevelToolStripMenuItem";
			this.downOneLevelToolStripMenuItem.ShortcutKeyDisplayString = "PgDn";
			this.downOneLevelToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
			this.downOneLevelToolStripMenuItem.Click += new System.EventHandler(this.DownOneLevelToolStripMenuItemClick);
			// 
			// downToBottomToolStripMenuItem
			// 
			this.downToBottomToolStripMenuItem.Enabled = false;
			this.downToBottomToolStripMenuItem.LanguageKey = "editor_downtobottom";
			this.downToBottomToolStripMenuItem.Name = "downToBottomToolStripMenuItem";
			this.downToBottomToolStripMenuItem.ShortcutKeyDisplayString = "End";
			this.downToBottomToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
			this.downToBottomToolStripMenuItem.Click += new System.EventHandler(this.DownToBottomToolStripMenuItemClick);
			// 
			// saveElementsToolStripMenuItem
			// 
			this.saveElementsToolStripMenuItem.LanguageKey = "editor_save_objects";
			this.saveElementsToolStripMenuItem.Name = "saveElementsToolStripMenuItem";
			this.saveElementsToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
			this.saveElementsToolStripMenuItem.Click += new System.EventHandler(this.SaveElementsToolStripMenuItemClick);
			// 
			// loadElementsToolStripMenuItem
			// 
			this.loadElementsToolStripMenuItem.LanguageKey = "editor_load_objects";
			this.loadElementsToolStripMenuItem.Name = "loadElementsToolStripMenuItem";
			this.loadElementsToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
			this.loadElementsToolStripMenuItem.Click += new System.EventHandler(this.LoadElementsToolStripMenuItemClick);
			// 
			// pluginToolStripMenuItem
			// 
			this.pluginToolStripMenuItem.LanguageKey = "settings_plugins";
			this.pluginToolStripMenuItem.Name = "pluginToolStripMenuItem";
			this.pluginToolStripMenuItem.Size = new System.Drawing.Size(58, 20);
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
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.helpToolStripMenuItem.Text = "Help";
			// 
			// helpToolStripMenuItem1
			// 
			this.helpToolStripMenuItem1.Image = ((System.Drawing.Image)(resources.GetObject("helpToolStripMenuItem1.Image")));
			this.helpToolStripMenuItem1.LanguageKey = "contextmenu_help";
			this.helpToolStripMenuItem1.Name = "helpToolStripMenuItem1";
			this.helpToolStripMenuItem1.ShortcutKeys = System.Windows.Forms.Keys.F1;
			this.helpToolStripMenuItem1.Size = new System.Drawing.Size(86, 22);
			this.helpToolStripMenuItem1.Click += new System.EventHandler(this.HelpToolStripMenuItem1Click);
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.LanguageKey = "contextmenu_about";
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size(86, 22);
			this.aboutToolStripMenuItem.Click += new System.EventHandler(this.AboutToolStripMenuItemClick);
			// 
			// toolStrip1
			// 
			this.toolStrip1.ClickThrough = true;
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
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
			this.toolStrip1.Location = new System.Drawing.Point(0, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(785, 25);
			this.toolStrip1.Stretch = true;
			this.toolStrip1.TabIndex = 0;
			// 
			// btnSave
			// 
			this.btnSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnSave.Image = ((System.Drawing.Image)(resources.GetObject("btnSave.Image")));
			this.btnSave.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnSave.LanguageKey = "editor_save";
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(23, 22);
			this.btnSave.Click += new System.EventHandler(this.BtnSaveClick);
			// 
			// btnClipboard
			// 
			this.btnClipboard.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnClipboard.Image = ((System.Drawing.Image)(resources.GetObject("btnClipboard.Image")));
			this.btnClipboard.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnClipboard.LanguageKey = "editor_copyimagetoclipboard";
			this.btnClipboard.Name = "btnClipboard";
			this.btnClipboard.Size = new System.Drawing.Size(23, 22);
			this.btnClipboard.Click += new System.EventHandler(this.BtnClipboardClick);
			// 
			// btnPrint
			// 
			this.btnPrint.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnPrint.Image = ((System.Drawing.Image)(resources.GetObject("btnPrint.Image")));
			this.btnPrint.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnPrint.LanguageKey = "editor_print";
			this.btnPrint.Name = "btnPrint";
			this.btnPrint.Size = new System.Drawing.Size(23, 22);
			this.btnPrint.Text = "Print";
			this.btnPrint.Click += new System.EventHandler(this.BtnPrintClick);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// btnDelete
			// 
			this.btnDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnDelete.Enabled = false;
			this.btnDelete.Image = ((System.Drawing.Image)(resources.GetObject("btnDelete.Image")));
			this.btnDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnDelete.LanguageKey = "editor_deleteelement";
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.Size = new System.Drawing.Size(23, 22);
			this.btnDelete.Click += new System.EventHandler(this.BtnDeleteClick);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
			// 
			// btnCut
			// 
			this.btnCut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnCut.Enabled = false;
			this.btnCut.Image = ((System.Drawing.Image)(resources.GetObject("btnCut.Image")));
			this.btnCut.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnCut.LanguageKey = "editor_cuttoclipboard";
			this.btnCut.Name = "btnCut";
			this.btnCut.Size = new System.Drawing.Size(23, 22);
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
			this.btnCopy.Size = new System.Drawing.Size(23, 22);
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
			this.btnPaste.Size = new System.Drawing.Size(23, 22);
			this.btnPaste.Click += new System.EventHandler(this.BtnPasteClick);
			// 
			// btnUndo
			// 
			this.btnUndo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnUndo.Enabled = false;
			this.btnUndo.Image = ((System.Drawing.Image)(resources.GetObject("btnUndo.Image")));
			this.btnUndo.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnUndo.Name = "btnUndo";
			this.btnUndo.Size = new System.Drawing.Size(23, 22);
			this.btnUndo.Click += new System.EventHandler(this.BtnUndoClick);
			// 
			// btnRedo
			// 
			this.btnRedo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnRedo.Enabled = false;
			this.btnRedo.Image = ((System.Drawing.Image)(resources.GetObject("btnRedo.Image")));
			this.btnRedo.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnRedo.Name = "btnRedo";
			this.btnRedo.Size = new System.Drawing.Size(23, 22);
			this.btnRedo.Click += new System.EventHandler(this.BtnRedoClick);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(6, 25);
			// 
			// btnSettings
			// 
			this.btnSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnSettings.Image = ((System.Drawing.Image)(resources.GetObject("btnSettings.Image")));
			this.btnSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnSettings.LanguageKey = "contextmenu_settings";
			this.btnSettings.Name = "btnSettings";
			this.btnSettings.Size = new System.Drawing.Size(23, 22);
			this.btnSettings.Click += new System.EventHandler(this.BtnSettingsClick);
			// 
			// toolStripSeparator11
			// 
			this.toolStripSeparator11.Name = "toolStripSeparator11";
			this.toolStripSeparator11.Size = new System.Drawing.Size(6, 25);
			// 
			// toolStripSeparator16
			// 
			this.toolStripSeparator16.Name = "toolStripSeparator16";
			this.toolStripSeparator16.Size = new System.Drawing.Size(6, 25);
			// 
			// btnHelp
			// 
			this.btnHelp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnHelp.Image = ((System.Drawing.Image)(resources.GetObject("btnHelp.Image")));
			this.btnHelp.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnHelp.LanguageKey = "contextmenu_help";
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Size = new System.Drawing.Size(23, 22);
			this.btnHelp.Text = "Help";
			this.btnHelp.Click += new System.EventHandler(this.BtnHelpClick);
			// 
			// propertiesToolStrip
			// 
			this.propertiesToolStrip.ClickThrough = true;
			this.propertiesToolStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.propertiesToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
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
									this.btnCancel});
			this.propertiesToolStrip.Location = new System.Drawing.Point(0, 49);
			this.propertiesToolStrip.MinimumSize = new System.Drawing.Size(0, 27);
			this.propertiesToolStrip.Name = "propertiesToolStrip";
			this.propertiesToolStrip.Size = new System.Drawing.Size(785, 27);
			this.propertiesToolStrip.Stretch = true;
			this.propertiesToolStrip.TabIndex = 2;
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
			this.obfuscateModeButton.SelectedTag = Greenshot.Drawing.FilterContainer.PreparedFilter.BLUR;
			this.obfuscateModeButton.Size = new System.Drawing.Size(29, 24);
			this.obfuscateModeButton.Tag = Greenshot.Drawing.FilterContainer.PreparedFilter.BLUR;
			// 
			// pixelizeToolStripMenuItem
			// 
			this.pixelizeToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("pixelizeToolStripMenuItem.Image")));
			this.pixelizeToolStripMenuItem.LanguageKey = "editor_obfuscate_pixelize";
			this.pixelizeToolStripMenuItem.Name = "pixelizeToolStripMenuItem";
			this.pixelizeToolStripMenuItem.Size = new System.Drawing.Size(67, 22);
			this.pixelizeToolStripMenuItem.Tag = Greenshot.Drawing.FilterContainer.PreparedFilter.PIXELIZE;
			// 
			// blurToolStripMenuItem
			// 
			this.blurToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("blurToolStripMenuItem.Image")));
			this.blurToolStripMenuItem.LanguageKey = "editor_obfuscate_blur";
			this.blurToolStripMenuItem.Name = "blurToolStripMenuItem";
			this.blurToolStripMenuItem.Size = new System.Drawing.Size(67, 22);
			this.blurToolStripMenuItem.Tag = Greenshot.Drawing.FilterContainer.PreparedFilter.BLUR;
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
			this.highlightModeButton.SelectedTag = Greenshot.Drawing.FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT;
			this.highlightModeButton.Size = new System.Drawing.Size(29, 24);
			this.highlightModeButton.Tag = Greenshot.Drawing.FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT;
			// 
			// textHighlightMenuItem
			// 
			this.textHighlightMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("textHighlightMenuItem.Image")));
			this.textHighlightMenuItem.LanguageKey = "editor_highlight_text";
			this.textHighlightMenuItem.Name = "textHighlightMenuItem";
			this.textHighlightMenuItem.Size = new System.Drawing.Size(67, 22);
			this.textHighlightMenuItem.Tag = Greenshot.Drawing.FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT;
			// 
			// areaHighlightMenuItem
			// 
			this.areaHighlightMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("areaHighlightMenuItem.Image")));
			this.areaHighlightMenuItem.LanguageKey = "editor_highlight_area";
			this.areaHighlightMenuItem.Name = "areaHighlightMenuItem";
			this.areaHighlightMenuItem.Size = new System.Drawing.Size(67, 22);
			this.areaHighlightMenuItem.Tag = Greenshot.Drawing.FilterContainer.PreparedFilter.AREA_HIGHLIGHT;
			// 
			// grayscaleHighlightMenuItem
			// 
			this.grayscaleHighlightMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("grayscaleHighlightMenuItem.Image")));
			this.grayscaleHighlightMenuItem.LanguageKey = "editor_highlight_grayscale";
			this.grayscaleHighlightMenuItem.Name = "grayscaleHighlightMenuItem";
			this.grayscaleHighlightMenuItem.Size = new System.Drawing.Size(67, 22);
			this.grayscaleHighlightMenuItem.Tag = Greenshot.Drawing.FilterContainer.PreparedFilter.GRAYSCALE;
			// 
			// magnifyMenuItem
			// 
			this.magnifyMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("magnifyMenuItem.Image")));
			this.magnifyMenuItem.LanguageKey = "editor_highlight_magnify";
			this.magnifyMenuItem.Name = "magnifyMenuItem";
			this.magnifyMenuItem.Size = new System.Drawing.Size(67, 22);
			this.magnifyMenuItem.Tag = Greenshot.Drawing.FilterContainer.PreparedFilter.MAGNIFICATION;
			// 
			// btnFillColor
			// 
			this.btnFillColor.AutoSize = false;
			this.btnFillColor.BackColor = System.Drawing.Color.Transparent;
			this.btnFillColor.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnFillColor.Image = ((System.Drawing.Image)(resources.GetObject("btnFillColor.Image")));
			this.btnFillColor.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.btnFillColor.LanguageKey = "editor_backcolor";
			this.btnFillColor.Margin = new System.Windows.Forms.Padding(0);
			this.btnFillColor.Name = "btnFillColor";
			this.btnFillColor.SelectedColor = System.Drawing.Color.Transparent;
			this.btnFillColor.Size = new System.Drawing.Size(23, 24);
			// 
			// btnLineColor
			// 
			this.btnLineColor.AutoSize = false;
			this.btnLineColor.BackColor = System.Drawing.Color.Transparent;
			this.btnLineColor.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnLineColor.Image = ((System.Drawing.Image)(resources.GetObject("btnLineColor.Image")));
			this.btnLineColor.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.btnLineColor.LanguageKey = "editor_forecolor";
			this.btnLineColor.Name = "btnLineColor";
			this.btnLineColor.SelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(203)))), ((int)(((byte)(222)))), ((int)(((byte)(250)))));
			this.btnLineColor.Size = new System.Drawing.Size(23, 24);
			// 
			// lineThicknessLabel
			// 
			this.lineThicknessLabel.LanguageKey = "editor_thickness";
			this.lineThicknessLabel.Name = "lineThicknessLabel";
			this.lineThicknessLabel.Size = new System.Drawing.Size(0, 24);
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
			this.lineThicknessUpDown.Size = new System.Drawing.Size(41, 24);
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
			this.fontFamilyComboBox.AutoSize = false;
			this.fontFamilyComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.fontFamilyComboBox.MaxDropDownItems = 20;
			this.fontFamilyComboBox.Name = "fontFamilyComboBox";
			this.fontFamilyComboBox.Size = new System.Drawing.Size(200, 23);
			this.fontFamilyComboBox.Text = "Aharoni";
			this.fontFamilyComboBox.GotFocus += new System.EventHandler(this.ToolBarFocusableElementGotFocus);
			this.fontFamilyComboBox.LostFocus += new System.EventHandler(this.ToolBarFocusableElementLostFocus);
			// 
			// fontSizeLabel
			// 
			this.fontSizeLabel.LanguageKey = "editor_fontsize";
			this.fontSizeLabel.Name = "fontSizeLabel";
			this.fontSizeLabel.Size = new System.Drawing.Size(0, 24);
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
			this.fontSizeUpDown.Size = new System.Drawing.Size(41, 24);
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
			this.fontBoldButton.Size = new System.Drawing.Size(23, 24);
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
			this.fontItalicButton.Size = new System.Drawing.Size(23, 24);
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
			this.textVerticalAlignmentButton.SelectedTag = System.Windows.Forms.HorizontalAlignment.Center;
			this.textVerticalAlignmentButton.Size = new System.Drawing.Size(13, 24);
			this.textVerticalAlignmentButton.Tag = Greenshot.Plugin.VerticalAlignment.CENTER;
			// 
			// alignTopToolStripMenuItem
			// 
			this.alignTopToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignTopToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignTop.Image")));
			this.alignTopToolStripMenuItem.LanguageKey = "editor_align_top";
			this.alignTopToolStripMenuItem.Name = "alignTopToolStripMenuItem";
			this.alignTopToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.alignTopToolStripMenuItem.Tag = Greenshot.Plugin.VerticalAlignment.TOP;
			// 
			// alignMiddleToolStripMenuItem
			// 
			this.alignMiddleToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignMiddleToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignMiddle.Image")));
			this.alignMiddleToolStripMenuItem.LanguageKey = "editor_align_middle";
			this.alignMiddleToolStripMenuItem.Name = "alignMiddleToolStripMenuItem";
			this.alignMiddleToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.alignMiddleToolStripMenuItem.Tag = Greenshot.Plugin.VerticalAlignment.CENTER;
			// 
			// alignBottomToolStripMenuItem
			// 
			this.alignBottomToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignBottomToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignBottom.Image")));
			this.alignBottomToolStripMenuItem.LanguageKey = "editor_align_bottom";
			this.alignBottomToolStripMenuItem.Name = "alignBottomToolStripMenuItem";
			this.alignBottomToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.alignBottomToolStripMenuItem.Tag = Greenshot.Plugin.VerticalAlignment.BOTTOM;
			// 
			// blurRadiusLabel
			// 
			this.blurRadiusLabel.LanguageKey = "editor_blur_radius";
			this.blurRadiusLabel.Name = "blurRadiusLabel";
			this.blurRadiusLabel.Size = new System.Drawing.Size(63, 24);
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
			this.blurRadiusUpDown.Size = new System.Drawing.Size(41, 24);
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
			this.brightnessLabel.Size = new System.Drawing.Size(62, 24);
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
			this.brightnessUpDown.Size = new System.Drawing.Size(41, 24);
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
			this.previewQualityLabel.Size = new System.Drawing.Size(87, 24);
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
			this.previewQualityUpDown.Size = new System.Drawing.Size(41, 23);
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
			this.magnificationFactorLabel.Size = new System.Drawing.Size(0, 0);
			this.magnificationFactorLabel.Tag = Greenshot.Drawing.FilterContainer.PreparedFilter.MAGNIFICATION;
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
			this.magnificationFactorUpDown.Size = new System.Drawing.Size(29, 23);
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
			this.pixelSizeLabel.Size = new System.Drawing.Size(0, 0);
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
			this.pixelSizeUpDown.Size = new System.Drawing.Size(41, 23);
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
			this.arrowHeadsLabel.Size = new System.Drawing.Size(0, 0);
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
			this.arrowHeadsDropDownButton.Size = new System.Drawing.Size(29, 20);
			// 
			// arrowHeadStartMenuItem
			// 
			this.arrowHeadStartMenuItem.LanguageKey = "editor_arrowheads_start";
			this.arrowHeadStartMenuItem.Name = "arrowHeadStartMenuItem";
			this.arrowHeadStartMenuItem.Size = new System.Drawing.Size(67, 22);
			this.arrowHeadStartMenuItem.Tag = Greenshot.Drawing.ArrowContainer.ArrowHeadCombination.START_POINT;
			this.arrowHeadStartMenuItem.Click += new System.EventHandler(this.ArrowHeadsToolStripMenuItemClick);
			// 
			// arrowHeadEndMenuItem
			// 
			this.arrowHeadEndMenuItem.LanguageKey = "editor_arrowheads_end";
			this.arrowHeadEndMenuItem.Name = "arrowHeadEndMenuItem";
			this.arrowHeadEndMenuItem.Size = new System.Drawing.Size(67, 22);
			this.arrowHeadEndMenuItem.Tag = Greenshot.Drawing.ArrowContainer.ArrowHeadCombination.END_POINT;
			this.arrowHeadEndMenuItem.Click += new System.EventHandler(this.ArrowHeadsToolStripMenuItemClick);
			// 
			// arrowHeadBothMenuItem
			// 
			this.arrowHeadBothMenuItem.LanguageKey = "editor_arrowheads_both";
			this.arrowHeadBothMenuItem.Name = "arrowHeadBothMenuItem";
			this.arrowHeadBothMenuItem.Size = new System.Drawing.Size(67, 22);
			this.arrowHeadBothMenuItem.Tag = Greenshot.Drawing.ArrowContainer.ArrowHeadCombination.BOTH;
			this.arrowHeadBothMenuItem.Click += new System.EventHandler(this.ArrowHeadsToolStripMenuItemClick);
			// 
			// arrowHeadNoneMenuItem
			// 
			this.arrowHeadNoneMenuItem.LanguageKey = "editor_arrowheads_none";
			this.arrowHeadNoneMenuItem.Name = "arrowHeadNoneMenuItem";
			this.arrowHeadNoneMenuItem.Size = new System.Drawing.Size(67, 22);
			this.arrowHeadNoneMenuItem.Tag = Greenshot.Drawing.ArrowContainer.ArrowHeadCombination.NONE;
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
			this.shadowButton.Size = new System.Drawing.Size(23, 20);
			// 
			// toolStripSeparator
			// 
			this.toolStripSeparator.Name = "toolStripSeparator";
			this.toolStripSeparator.Size = new System.Drawing.Size(6, 27);
			// 
			// toolStripSeparator10
			// 
			this.toolStripSeparator10.Name = "toolStripSeparator10";
			this.toolStripSeparator10.Size = new System.Drawing.Size(6, 27);
			// 
			// btnConfirm
			// 
			this.btnConfirm.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnConfirm.Image = ((System.Drawing.Image)(resources.GetObject("btnConfirm.Image")));
			this.btnConfirm.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnConfirm.LanguageKey = "editor_confirm";
			this.btnConfirm.Name = "btnConfirm";
			this.btnConfirm.Size = new System.Drawing.Size(23, 20);
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
			this.btnCancel.Size = new System.Drawing.Size(23, 20);
			this.btnCancel.Text = "Cancel";
			this.btnCancel.Click += new System.EventHandler(this.BtnCancelClick);
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(304, 6);
			// 
			// closeToolStripMenuItem
			// 
			this.closeToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("closeToolStripMenuItem.Image")));
			this.closeToolStripMenuItem.LanguageKey = "editor_close";
			this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
			this.closeToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.closeToolStripMenuItem.Size = new System.Drawing.Size(307, 22);
			this.closeToolStripMenuItem.Click += new System.EventHandler(this.CloseToolStripMenuItemClick);
			// 
			// fileSavedStatusContextMenu
			// 
			this.fileSavedStatusContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.copyPathMenuItem,
									this.openDirectoryMenuItem});
			this.fileSavedStatusContextMenu.Name = "contextMenuStrip1";
			this.fileSavedStatusContextMenu.Size = new System.Drawing.Size(247, 48);
			// 
			// copyPathMenuItem
			// 
			this.copyPathMenuItem.LanguageKey = "editor_copypathtoclipboard";
			this.copyPathMenuItem.Name = "copyPathMenuItem";
			this.copyPathMenuItem.Size = new System.Drawing.Size(246, 22);
			this.copyPathMenuItem.Click += new System.EventHandler(this.CopyPathMenuItemClick);
			// 
			// openDirectoryMenuItem
			// 
			this.openDirectoryMenuItem.LanguageKey = "editor_opendirinexplorer";
			this.openDirectoryMenuItem.Name = "openDirectoryMenuItem";
			this.openDirectoryMenuItem.Size = new System.Drawing.Size(246, 22);
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
			this.textHorizontalAlignmentButton.SelectedTag = System.Windows.Forms.HorizontalAlignment.Center;
			this.textHorizontalAlignmentButton.Size = new System.Drawing.Size(13, 24);
			this.textHorizontalAlignmentButton.Tag = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// alignLeftToolStripMenuItem
			// 
			this.alignLeftToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignLeftToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignLeft.Image")));
			this.alignLeftToolStripMenuItem.LanguageKey = "editor_align_left";
			this.alignLeftToolStripMenuItem.Name = "alignLeftToolStripMenuItem";
			this.alignLeftToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.alignLeftToolStripMenuItem.Tag = System.Windows.Forms.HorizontalAlignment.Left;
			// 
			// alignCenterToolStripMenuItem
			// 
			this.alignCenterToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignCenterToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignCenter.Image")));
			this.alignCenterToolStripMenuItem.LanguageKey = "editor_align_center";
			this.alignCenterToolStripMenuItem.Name = "alignCenterToolStripMenuItem";
			this.alignCenterToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.alignCenterToolStripMenuItem.Tag = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// alignRightToolStripMenuItem
			// 
			this.alignRightToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.alignRightToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("btnAlignRight.Image")));
			this.alignRightToolStripMenuItem.LanguageKey = "editor_align_right";
			this.alignRightToolStripMenuItem.Name = "alignRightToolStripMenuItem";
			this.alignRightToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.alignRightToolStripMenuItem.Tag = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// ImageEditorForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(785, 485);
			this.Controls.Add(this.toolStripContainer1);
			this.KeyPreview = true;
			this.LanguageKey = "editor_title";
			this.Name = "ImageEditorForm";
			this.Activated += new System.EventHandler(this.ImageEditorFormActivated);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ImageEditorFormFormClosing);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ImageEditorFormKeyDown);
			this.Resize += new System.EventHandler(this.ImageEditorFormResize);
			this.toolStripContainer1.BottomToolStripPanel.ResumeLayout(false);
			this.toolStripContainer1.BottomToolStripPanel.PerformLayout();
			this.toolStripContainer1.ContentPanel.ResumeLayout(false);
			this.toolStripContainer1.LeftToolStripPanel.ResumeLayout(false);
			this.toolStripContainer1.LeftToolStripPanel.PerformLayout();
			this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.PerformLayout();
			this.toolStripContainer1.ResumeLayout(false);
			this.toolStripContainer1.PerformLayout();
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.toolStrip2.ResumeLayout(false);
			this.toolStrip2.PerformLayout();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.propertiesToolStrip.ResumeLayout(false);
			this.propertiesToolStrip.PerformLayout();
			this.fileSavedStatusContextMenu.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem alignRightToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem alignCenterToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem alignLeftToolStripMenuItem;
		private Greenshot.Controls.BindableToolStripDropDownButton textHorizontalAlignmentButton;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem alignMiddleToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem alignBottomToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem alignTopToolStripMenuItem;
		private Greenshot.Controls.BindableToolStripDropDownButton textVerticalAlignmentButton;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem invertToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem grayscaleToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripButton rotateCcwToolstripButton;
		private GreenshotPlugin.Controls.GreenshotToolStripButton rotateCwToolstripButton;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem addBorderToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem tornEdgesToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem addDropshadowToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripDropDownButton toolStripSplitButton1;
		private System.Windows.Forms.ToolStripStatusLabel dimensionsLabel;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem insert_window_toolstripmenuitem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem grayscaleHighlightMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem areaHighlightMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem textHighlightMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem magnifyMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem arrowHeadStartMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem arrowHeadEndMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem arrowHeadBothMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem arrowHeadNoneMenuItem;
		private Greenshot.Controls.BindableToolStripButton btnCancel;
		private Greenshot.Controls.BindableToolStripButton btnConfirm;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem selectAllToolStripMenuItem;
		private Greenshot.Controls.BindableToolStripDropDownButton highlightModeButton;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem pixelizeToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem blurToolStripMenuItem;
		private Greenshot.Controls.BindableToolStripDropDownButton obfuscateModeButton;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnHighlight;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem loadElementsToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem saveElementsToolStripMenuItem;
		private Greenshot.Controls.FontFamilyComboBox fontFamilyComboBox;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
		private Greenshot.Controls.BindableToolStripButton shadowButton;
		private Greenshot.Controls.BindableToolStripButton fontItalicButton;
		private Greenshot.Controls.BindableToolStripButton fontBoldButton;
		private Greenshot.Controls.ToolStripNumericUpDown fontSizeUpDown;
		private GreenshotPlugin.Controls.GreenshotToolStripLabel fontSizeLabel;
		private Greenshot.Controls.ToolStripNumericUpDown brightnessUpDown;
		private GreenshotPlugin.Controls.GreenshotToolStripLabel brightnessLabel;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem pluginToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripDropDownButton arrowHeadsDropDownButton;
		private GreenshotPlugin.Controls.GreenshotToolStripLabel arrowHeadsLabel;
		private Greenshot.Controls.ToolStripNumericUpDown pixelSizeUpDown;
		private GreenshotPlugin.Controls.GreenshotToolStripLabel pixelSizeLabel;
		private Greenshot.Controls.ToolStripNumericUpDown magnificationFactorUpDown;
		private GreenshotPlugin.Controls.GreenshotToolStripLabel magnificationFactorLabel;
		private Greenshot.Controls.ToolStripNumericUpDown previewQualityUpDown;
		private GreenshotPlugin.Controls.GreenshotToolStripLabel previewQualityLabel;
		private Greenshot.Controls.ToolStripNumericUpDown blurRadiusUpDown;
		private GreenshotPlugin.Controls.GreenshotToolStripLabel blurRadiusLabel;
		private Greenshot.Controls.ToolStripEx propertiesToolStrip;
		private GreenshotPlugin.Controls.GreenshotToolStripLabel lineThicknessLabel;
		private Greenshot.Controls.ToolStripNumericUpDown lineThicknessUpDown;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator14;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator15;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator16;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnFreehand;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnObfuscate;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator13;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnCrop;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem openDirectoryMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem copyPathMenuItem;
		private System.Windows.Forms.ContextMenuStrip fileSavedStatusContextMenu;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem downToBottomToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem upToTopToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem downOneLevelToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem upOneLevelToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem arrangeToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnCursor;
		private Greenshot.Controls.ToolStripEx toolStrip2;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnArrow;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem drawArrowToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem drawFreehandToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnText;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem drawLineToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnLine;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnSettings;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnHelp;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem aboutToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem helpToolStripMenuItem1;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem helpToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem preferencesToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem closeToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnPrint;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem duplicateToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem fileStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem removeObjectToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem addTextBoxToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem addEllipseToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem addRectangleToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem objectToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem pasteToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem copyToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem cutToolStripMenuItem;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem editToolStripMenuItem;
		private Greenshot.Controls.MenuStripEx menuStrip1;
		private System.Windows.Forms.ToolStripStatusLabel statusLabel;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnCut;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnCopy;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnPaste;
		private System.Windows.Forms.ToolStripButton btnUndo;
		private System.Windows.Forms.ToolStripButton btnRedo;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnClipboard;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnDelete;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnEllipse;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnSave;
		private GreenshotPlugin.Controls.GreenshotToolStripButton btnRect;
		private System.Windows.Forms.ToolStripContainer toolStripContainer1;
		private Greenshot.Controls.ToolStripEx toolStrip1;
		private GreenshotPlugin.Controls.NonJumpingPanel panel1;
		private Greenshot.Controls.ToolStripColorButton btnFillColor;
		private Greenshot.Controls.ToolStripColorButton btnLineColor;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem autoCropToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator17;
	}
}
