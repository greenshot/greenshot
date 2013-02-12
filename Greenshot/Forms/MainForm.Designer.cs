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
				if (copyData != null) {
					copyData.Dispose();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.contextmenu_capturearea = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.contextmenu_capturelastregion = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.contextmenu_capturewindow = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.contextmenu_capturefullscreen = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.contextmenu_captureie = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripListCaptureSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.contextmenu_capturewindowfromlist = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.contextmenu_captureiefromlist = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripOtherSourcesSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.contextmenu_captureclipboard = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.contextmenu_openfile = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripOpenFolderSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.contextmenu_openrecentcapture = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripPluginSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.contextmenu_quicksettings = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.contextmenu_settings = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripMiscSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.contextmenu_help = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.contextmenu_donate = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.contextmenu_about = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripCloseSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.contextmenu_exit = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
			this.backgroundWorkerTimer = new System.Windows.Forms.Timer(this.components);
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
									this.toolStripListCaptureSeparator,
									this.contextmenu_capturewindowfromlist,
									this.contextmenu_captureiefromlist,
									this.toolStripOtherSourcesSeparator,
									this.contextmenu_captureclipboard,
									this.contextmenu_openfile,
									this.toolStripOpenFolderSeparator,
									this.contextmenu_openrecentcapture,
									this.toolStripPluginSeparator,
									this.contextmenu_quicksettings,
									this.contextmenu_settings,
									this.toolStripMiscSeparator,
									this.contextmenu_help,
									this.contextmenu_donate,
									this.contextmenu_about,
									this.toolStripCloseSeparator,
									this.contextmenu_exit});
			this.contextMenu.Name = "contextMenu";
			this.contextMenu.Size = new System.Drawing.Size(171, 392);
			this.contextMenu.Closing += new System.Windows.Forms.ToolStripDropDownClosingEventHandler(this.ContextMenuClosing);
			this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ContextMenuOpening);
			// 
			// contextmenu_capturearea
			// 
			this.contextmenu_capturearea.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_capturearea.Image")));
			this.contextmenu_capturearea.Name = "contextmenu_capturearea";
			this.contextmenu_capturearea.ShortcutKeyDisplayString = "Print";
			this.contextmenu_capturearea.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_capturearea.Click += new System.EventHandler(this.CaptureAreaToolStripMenuItemClick);
			// 
			// contextmenu_capturelastregion
			// 
			this.contextmenu_capturelastregion.Enabled = false;
			this.contextmenu_capturelastregion.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_capturelastregion.Image")));
			this.contextmenu_capturelastregion.Name = "contextmenu_capturelastregion";
			this.contextmenu_capturelastregion.ShortcutKeyDisplayString = "Shift + Print";
			this.contextmenu_capturelastregion.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_capturelastregion.Click += new System.EventHandler(this.Contextmenu_capturelastregionClick);
			// 
			// contextmenu_capturewindow
			// 
			this.contextmenu_capturewindow.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_capturewindow.Image")));
			this.contextmenu_capturewindow.Name = "contextmenu_capturewindow";
			this.contextmenu_capturewindow.ShortcutKeyDisplayString = "Alt + Print";
			this.contextmenu_capturewindow.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_capturewindow.Click += new System.EventHandler(this.Contextmenu_capturewindow_Click);
			// 
			// contextmenu_capturefullscreen
			// 
			this.contextmenu_capturefullscreen.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_capturefullscreen.Image")));
			this.contextmenu_capturefullscreen.Name = "contextmenu_capturefullscreen";
			this.contextmenu_capturefullscreen.ShortcutKeyDisplayString = "Ctrl + Print";
			this.contextmenu_capturefullscreen.Size = new System.Drawing.Size(170, 22);
			// 
			// contextmenu_captureie
			// 
			this.contextmenu_captureie.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_captureie.Image")));
			this.contextmenu_captureie.Name = "contextmenu_captureie";
			this.contextmenu_captureie.ShortcutKeyDisplayString = "Ctrl + Shift + Print";
			this.contextmenu_captureie.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_captureie.Click += new System.EventHandler(this.Contextmenu_captureie_Click);
			// 
			// toolStripListCaptureSeparator
			// 
			this.toolStripListCaptureSeparator.Name = "toolStripListCaptureSeparator";
			this.toolStripListCaptureSeparator.Size = new System.Drawing.Size(167, 6);
			// 
			// contextmenu_capturewindowfromlist
			// 
			this.contextmenu_capturewindowfromlist.Name = "contextmenu_capturewindowfromlist";
			this.contextmenu_capturewindowfromlist.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_capturewindowfromlist.DropDownClosed += new System.EventHandler(this.CaptureWindowFromListMenuDropDownClosed);
			this.contextmenu_capturewindowfromlist.DropDownOpening += new System.EventHandler(this.CaptureWindowFromListMenuDropDownOpening);
			// 
			// contextmenu_captureiefromlist
			// 
			this.contextmenu_captureiefromlist.Name = "contextmenu_captureiefromlist";
			this.contextmenu_captureiefromlist.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_captureiefromlist.DropDownOpening += new System.EventHandler(this.CaptureIEMenuDropDownOpening);
			// 
			// toolStripOtherSourcesSeparator
			// 
			this.toolStripOtherSourcesSeparator.Name = "toolStripOtherSourcesSeparator";
			this.toolStripOtherSourcesSeparator.Size = new System.Drawing.Size(167, 6);
			// 
			// contextmenu_captureclipboard
			// 
			this.contextmenu_captureclipboard.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_captureclipboard.Image")));
			this.contextmenu_captureclipboard.Name = "contextmenu_captureclipboard";
			this.contextmenu_captureclipboard.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_captureclipboard.Click += new System.EventHandler(this.CaptureClipboardToolStripMenuItemClick);
			// 
			// contextmenu_openfile
			// 
			this.contextmenu_openfile.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_openfile.Image")));
			this.contextmenu_openfile.Name = "contextmenu_openfile";
			this.contextmenu_openfile.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_openfile.Click += new System.EventHandler(this.OpenFileToolStripMenuItemClick);
			// 
			// toolStripOpenFolderSeparator
			// 
			this.toolStripOpenFolderSeparator.Name = "toolStripOpenFolderSeparator";
			this.toolStripOpenFolderSeparator.Size = new System.Drawing.Size(167, 6);
			// 
			// contextmenu_openrecentcapture
			// 
			this.contextmenu_openrecentcapture.Name = "contextmenu_openrecentcapture";
			this.contextmenu_openrecentcapture.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_openrecentcapture.Click += new System.EventHandler(this.Contextmenu_OpenRecent);
			// 
			// toolStripPluginSeparator
			// 
			this.toolStripPluginSeparator.Name = "toolStripPluginSeparator";
			this.toolStripPluginSeparator.Size = new System.Drawing.Size(167, 6);
			this.toolStripPluginSeparator.Tag = "PluginsAreAddedBefore";
			// 
			// contextmenu_quicksettings
			// 
			this.contextmenu_quicksettings.Name = "contextmenu_quicksettings";
			this.contextmenu_quicksettings.Size = new System.Drawing.Size(170, 22);
			// 
			// contextmenu_settings
			// 
			this.contextmenu_settings.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_settings.Image")));
			this.contextmenu_settings.Name = "contextmenu_settings";
			this.contextmenu_settings.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_settings.Click += new System.EventHandler(this.Contextmenu_settingsClick);
			// 
			// toolStripMiscSeparator
			// 
			this.toolStripMiscSeparator.Name = "toolStripMiscSeparator";
			this.toolStripMiscSeparator.Size = new System.Drawing.Size(167, 6);
			// 
			// contextmenu_help
			// 
			this.contextmenu_help.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_help.Image")));
			this.contextmenu_help.Name = "contextmenu_help";
			this.contextmenu_help.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_help.Click += new System.EventHandler(this.Contextmenu_helpClick);
			// 
			// contextmenu_donate
			// 
			this.contextmenu_donate.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_donate.Image")));
			this.contextmenu_donate.Name = "contextmenu_donate";
			this.contextmenu_donate.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_donate.Click += new System.EventHandler(this.Contextmenu_donateClick);
			// 
			// contextmenu_about
			// 
			this.contextmenu_about.Name = "contextmenu_about";
			this.contextmenu_about.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_about.Click += new System.EventHandler(this.Contextmenu_aboutClick);
			// 
			// toolStripCloseSeparator
			// 
			this.toolStripCloseSeparator.Name = "toolStripCloseSeparator";
			this.toolStripCloseSeparator.Size = new System.Drawing.Size(167, 6);
			// 
			// contextmenu_exit
			// 
			this.contextmenu_exit.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_exit.Image")));
			this.contextmenu_exit.Name = "contextmenu_exit";
			this.contextmenu_exit.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_exit.Click += new System.EventHandler(this.Contextmenu_exitClick);
			// 
			// notifyIcon
			// 
			this.notifyIcon.ContextMenuStrip = this.contextMenu;
			this.notifyIcon.Text = "Greenshot";
			this.notifyIcon.MouseUp += new System.Windows.Forms.MouseEventHandler(this.NotifyIconClickTest);
			// 
			// backgroundWorkerTimer
			// 
			this.backgroundWorkerTimer.Enabled = true;
			this.backgroundWorkerTimer.Interval = 300000;
			this.backgroundWorkerTimer.Tick += new System.EventHandler(this.BackgroundWorkerTimerTick);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(0, 0);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.LanguageKey = "application_title";
			this.Name = "MainForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
			this.Activated += new System.EventHandler(this.MainFormActivated);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormFormClosing);
			this.contextMenu.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_captureiefromlist;
		private System.Windows.Forms.ToolStripSeparator toolStripOtherSourcesSeparator;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_capturewindowfromlist;
		private System.Windows.Forms.ToolStripSeparator toolStripListCaptureSeparator;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_openrecentcapture;
		private System.Windows.Forms.Timer backgroundWorkerTimer;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_captureie;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_donate;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_openfile;
		private System.Windows.Forms.ToolStripSeparator toolStripPluginSeparator;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_captureclipboard;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_quicksettings;
		private System.Windows.Forms.ToolStripSeparator toolStripMiscSeparator;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_help;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_capturewindow;
		private System.Windows.Forms.ToolStripSeparator toolStripOpenFolderSeparator;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_about;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_capturefullscreen;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_capturelastregion;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_capturearea;
		private System.Windows.Forms.NotifyIcon notifyIcon;
		private System.Windows.Forms.ToolStripSeparator toolStripCloseSeparator;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_exit;
		private System.Windows.Forms.ContextMenuStrip contextMenu;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_settings;
	}
}
