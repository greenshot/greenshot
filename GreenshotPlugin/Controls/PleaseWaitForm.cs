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
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

namespace GreenshotPlugin.Controls {
	/// <summary>
	/// Description of PleaseWaitForm.
	/// </summary>
	public partial class PleaseWaitForm : Form {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PleaseWaitForm));
		private Thread waitFor = null;
		private string title;
		public PleaseWaitForm() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();
		}
		
		/// <summary>
		/// Prevent the close button showing
		/// </summary>
		private const int CP_NOCLOSE_BUTTON = 0x200;
		protected override CreateParams CreateParams {
			get {
				CreateParams createParams = base.CreateParams;
				createParams.ClassStyle = createParams.ClassStyle | CP_NOCLOSE_BUTTON ;
				return createParams;
			}
		} 
		
		public void ShowAndWait(string title, string text, string cancelText, ThreadStart waitDelegate) {
			this.title = title;
			this.Text = title;
			this.label_pleasewait.Text = text;
			this.cancelButton.Text = cancelText;

			Show();
			
			Exception threadException = null;
			try {
				// Wrap the passed delegate in a try/catch which saves the exception
				waitFor = new Thread(new ThreadStart(
					delegate {
						try {
							waitDelegate.Invoke();
						} catch (Exception ex) {
							threadException = ex;
						}
					})
				);
				waitFor.Name = title;
				waitFor.IsBackground = true;
				waitFor.SetApartmentState(ApartmentState.STA);
				waitFor.Start();
	
				// Wait until finished
				while (!waitFor.Join(TimeSpan.FromMilliseconds(100))) {
					Application.DoEvents();
				}
				LOG.DebugFormat("Finished {0}", title);
			} catch (Exception ex) {
				LOG.Error(ex);
				throw ex;
			} finally {
				Close();
			}
			// Check if an exception occured, if so throw it
			if (threadException != null) {
				throw threadException;
			}
		}
		
		void CancelButtonClick(object sender, EventArgs e) {
			LOG.DebugFormat("Cancel clicked on {0}", title);
			cancelButton.Enabled = false;
			waitFor.Abort();
		}
	}
}
