/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
	partial class MainForm {
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
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
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.contextmenu_capturearea = new System.Windows.Forms.ToolStripMenuItem();
			this.contextmenu_capturelastregion = new System.Windows.Forms.ToolStripMenuItem();
			this.contextmenu_capturewindow = new System.Windows.Forms.ToolStripMenuItem();
			this.contextmenu_capturefullscreen = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.contextmenu_captureclipboard = new System.Windows.Forms.ToolStripMenuItem();
			this.contextmenu_openfile = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.contextmenu_quicksettings = new System.Windows.Forms.ToolStripMenuItem();
			this.contextmenu_settings = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.contextmenu_help = new System.Windows.Forms.ToolStripMenuItem();
			this.contextmenu_about = new System.Windows.Forms.ToolStripMenuItem();
			this.contextmenu_donate = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.contextmenu_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
			this.contextmenu_captureie = new System.Windows.Forms.ToolStripMenuItem();
			this.backgroundWorkerTimer = new System.Windows.Forms.Timer(this.components);
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.contextmenu_openrecentcapture = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// contextMenu
			// 
			this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.contextmenu_capturearea,
									this.contextmenu_capturelastregion,
									this.contextmenu_capturewindow,
									this.contextmenu_capturefullscreen,
									this.contextmenu_captureie,
									this.toolStripSeparator4,
									this.contextmenu_captureclipboard,
									this.contextmenu_openfile,
									this.toolStripSeparator2,
									this.contextmenu_openrecentcapture,
									this.toolStripSeparator5,
									this.contextmenu_quicksettings,
									this.contextmenu_settings,
									this.toolStripSeparator3,
									this.contextmenu_help,
									this.contextmenu_donate,
									this.contextmenu_about,
									this.toolStripSeparator1,
									this.contextmenu_exit});
			this.contextMenu.Name = "contextMenu";
			this.contextMenu.Size = new System.Drawing.Size(243, 314);
			this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ContextMenuOpening);
			this.contextMenu.Closing += new System.Windows.Forms.ToolStripDropDownClosingEventHandler(this.ContextMenuClosing);
			// 
			// contextmenu_capturearea
			// 
			this.contextmenu_capturearea.Name = "contextmenu_capturearea";
			this.contextmenu_capturearea.ShortcutKeyDisplayString = "Print";
			this.contextmenu_capturearea.Size = new System.Drawing.Size(242, 22);
			this.contextmenu_capturearea.Text = "Capture region";
			this.contextmenu_capturearea.Click += new System.EventHandler(this.CaptureAreaToolStripMenuItemClick);
			// 
			// contextmenu_capturelastregion
			// 
			this.contextmenu_capturelastregion.Enabled = false;
			this.contextmenu_capturelastregion.Name = "contextmenu_capturelastregion";
			this.contextmenu_capturelastregion.ShortcutKeyDisplayString = "Shift + Print";
			this.contextmenu_capturelastregion.Size = new System.Drawing.Size(242, 22);
			this.contextmenu_capturelastregion.Text = "Capture last region";
			this.contextmenu_capturelastregion.Click += new System.EventHandler(this.Contextmenu_capturelastregionClick);
			// 
			// contextmenu_capturewindow
			// 
			this.contextmenu_capturewindow.Name = "contextmenu_capturewindow";
			this.contextmenu_capturewindow.ShortcutKeyDisplayString = "Alt + Print";
			this.contextmenu_capturewindow.Size = new System.Drawing.Size(242, 22);
			this.contextmenu_capturewindow.Text = "Capture window";
			this.contextmenu_capturewindow.MouseEnter += new System.EventHandler(EnterCaptureWindowMenuItem);
			// 
			// contextmenu_capturefullscreen
			// 
			this.contextmenu_capturefullscreen.Name = "contextmenu_capturefullscreen";
			this.contextmenu_capturefullscreen.ShortcutKeyDisplayString = "Ctrl + Print";
			this.contextmenu_capturefullscreen.Size = new System.Drawing.Size(242, 22);
			this.contextmenu_capturefullscreen.Text = "Capture full screen";
			this.contextmenu_capturefullscreen.Click += new System.EventHandler(this.CaptureFullScreenToolStripMenuItemClick);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(239, 6);
			// 
			// contextmenu_captureclipboard
			// 
			this.contextmenu_captureclipboard.Name = "contextmenu_captureclipboard";
			this.contextmenu_captureclipboard.Size = new System.Drawing.Size(242, 22);
			this.contextmenu_captureclipboard.Text = "Capture clipboard";
			this.contextmenu_captureclipboard.Click += new System.EventHandler(this.CaptureClipboardToolStripMenuItemClick);
			// 
			// contextmenu_openfile
			// 
			this.contextmenu_openfile.Name = "contextmenu_openfile";
			this.contextmenu_openfile.Size = new System.Drawing.Size(242, 22);
			this.contextmenu_openfile.Text = "Open File";
			this.contextmenu_openfile.Click += new System.EventHandler(this.OpenFileToolStripMenuItemClick);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(239, 6);
			// 
			// contextmenu_quicksettings
			// 
			this.contextmenu_quicksettings.Name = "contextmenu_quicksettings";
			this.contextmenu_quicksettings.Size = new System.Drawing.Size(242, 22);
			this.contextmenu_quicksettings.Text = "Quick settings";
			// 
			// contextmenu_settings
			// 
			this.contextmenu_settings.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_settings.Image")));
			this.contextmenu_settings.Name = "contextmenu_settings";
			this.contextmenu_settings.Size = new System.Drawing.Size(242, 22);
			this.contextmenu_settings.Text = "Settings";
			this.contextmenu_settings.Click += new System.EventHandler(this.Contextmenu_settingsClick);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(239, 6);
			// 
			// contextmenu_help
			// 
			this.contextmenu_help.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_help.Image")));
			this.contextmenu_help.Name = "contextmenu_help";
			this.contextmenu_help.Size = new System.Drawing.Size(242, 22);
			this.contextmenu_help.Text = "Help";
			this.contextmenu_help.Click += new System.EventHandler(this.Contextmenu_helpClick);
			// 
			// contextmenu_about
			// 
			this.contextmenu_about.Name = "contextmenu_about";
			this.contextmenu_about.Size = new System.Drawing.Size(242, 22);
			this.contextmenu_about.Text = "About";
			this.contextmenu_about.Click += new System.EventHandler(this.Contextmenu_aboutClick);
			// 
			// contextmenu_donate
			// 
			this.contextmenu_donate.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_donate.Image")));
			this.contextmenu_donate.Name = "contextmenu_donate";
			this.contextmenu_donate.Size = new System.Drawing.Size(242, 22);
			this.contextmenu_donate.Text = "Support Greenshot";
			this.contextmenu_donate.Click += new System.EventHandler(this.Contextmenu_donateClick);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(239, 6);
			// 
			// contextmenu_exit
			// 
			this.contextmenu_exit.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_exit.Image")));
			this.contextmenu_exit.Name = "contextmenu_exit";
			this.contextmenu_exit.Size = new System.Drawing.Size(242, 22);
			this.contextmenu_exit.Text = "Exit";
			this.contextmenu_exit.Click += new System.EventHandler(this.Contextmenu_exitClick);
			// 
			// notifyIcon
			// 
			this.notifyIcon.ContextMenuStrip = this.contextMenu;
			this.notifyIcon.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();
			this.notifyIcon.Text = "Greenshot";
			this.notifyIcon.Visible = false;
			this.notifyIcon.Click += new System.EventHandler(this.NotifyIconClick);
			// 
			// contextmenu_captureie
			// 
			this.contextmenu_captureie.Name = "contextmenu_captureie";
			this.contextmenu_captureie.Size = new System.Drawing.Size(231, 22);
			this.contextmenu_captureie.Text = "Capture IE Tab";
			this.contextmenu_captureie.ShortcutKeyDisplayString = "Ctrl + Shift + Print";
			this.contextmenu_captureie.MouseEnter += new System.EventHandler(EnterCaptureIEMenuItem);
			// 
			// backgroundWorkerTimer
			// 
			this.backgroundWorkerTimer.Enabled = true;
			this.backgroundWorkerTimer.Interval = 300000;
			this.backgroundWorkerTimer.Tick += new System.EventHandler(this.BackgroundWorkerTimerTick);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(240, 6);
			// 
			// contextmenu_openrecentcapture
			// 
			this.contextmenu_openrecentcapture.Name = "contextmenu_openrecentcapture";
			this.contextmenu_openrecentcapture.Size = new System.Drawing.Size(243, 22);
			this.contextmenu_openrecentcapture.Text = "Open recent capture location";
			this.contextmenu_openrecentcapture.Click += new System.EventHandler(this.Contextmenu_OpenRecent);
			// 
			// MainForm
			// 
			this.ClientSize = new System.Drawing.Size(0, 0);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "MainForm";
			this.Text = "Greenshot";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormFormClosing);
			this.Activated += new System.EventHandler(this.MainFormActivated);
			this.contextMenu.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_openrecentcapture;
		private System.Windows.Forms.Timer backgroundWorkerTimer;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_captureie;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_donate;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_openfile;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_captureclipboard;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_quicksettings;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_help;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_capturewindow;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_about;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_capturefullscreen;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_capturelastregion;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_capturearea;
		private System.Windows.Forms.NotifyIcon notifyIcon;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_exit;
		private System.Windows.Forms.ContextMenuStrip contextMenu;
		private System.Windows.Forms.ToolStripMenuItem contextmenu_settings;
	}
}
