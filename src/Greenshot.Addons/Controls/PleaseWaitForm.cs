// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Log;
using Greenshot.Addons.Resources;

namespace Greenshot.Addons.Controls
{
	/// <summary>
	///     Description of PleaseWaitForm.
	/// </summary>
	public sealed partial class PleaseWaitForm : Form
	{
	    private readonly IGreenshotLanguage _greenshotLanguage;

	    /// <summary>
		///     Prevent the close-window button showing
		/// </summary>
		private const int CpNocloseButton = 0x200;

		private static readonly LogSource Log = new LogSource();
	    private readonly CancellationTokenSource _cancellationTokenSource;
	    private Thread _waitFor;

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
		public PleaseWaitForm(IGreenshotLanguage greenshotLanguage)
		{
		    _greenshotLanguage = greenshotLanguage;
		    //
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			Icon = GreenshotResources.Instance.GetGreenshotIcon();
		}

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="cancellationTokenSource">CancellationTokenSource</param>
	    public PleaseWaitForm(IGreenshotLanguage greenshotLanguage, CancellationTokenSource cancellationTokenSource = default) : this(greenshotLanguage)
	    {
	        _cancellationTokenSource = cancellationTokenSource;
        }

        /// <inheritdoc/>
        protected override CreateParams CreateParams
		{
			get
			{
				var createParams = base.CreateParams;
				createParams.ClassStyle = createParams.ClassStyle | CpNocloseButton;
				return createParams;
			}
		}

        /// <summary>
        /// Set the details
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <returns></returns>
	    public PleaseWaitForm SetDetails(string title, string text)
	    {
	        Text = title;
	        label_pleasewait.Text = text;
	        return this;
	    }

        /// <summary>
        ///     Show the "please wait" form, execute the code from the delegate and wait until execution finishes.
        ///     The supplied delegate will be wrapped with a try/catch so this method can return any exception that was thrown.
        /// </summary>
        /// <param name="title">The title of the form (and Thread)</param>
        /// <param name="text">The text in the form</param>
        /// <param name="executeTask">Task</param>
        public async Task ShowAndWait(string title, string text, Task executeTask)
        {
            SetDetails(title, text);
	        cancelButton.Text = _greenshotLanguage.Cancel;

	        Show();
	        await executeTask;
	        Close();
	    }

	    /// <summary>
            ///     Show the "please wait" form, execute the code from the delegate and wait until execution finishes.
            ///     The supplied delegate will be wrapped with a try/catch so this method can return any exception that was thrown.
            /// </summary>
            /// <param name="title">The title of the form (and Thread)</param>
            /// <param name="text">The text in the form</param>
            /// <param name="waitDelegate">delegate { with your code }</param>
            public void ShowAndWait(string title, string text, ThreadStart waitDelegate)
		{
		    SetDetails(title, text);
            cancelButton.Text = _greenshotLanguage.Cancel;

            // Make sure the form is shown.
            Show();

			// Variable to store the exception, if one is generated, from inside the thread.
			Exception threadException = null;
			try
			{
				// Wrap the passed delegate in a try/catch which makes it possible to save the exception
				_waitFor = new Thread(() =>
				    {
				        try
				        {
				            waitDelegate.Invoke();
				        }
				        catch (Exception ex)
				        {
				            Log.Error().WriteLine(ex, "invoke error:");
				            threadException = ex;
				        }
				    }
				)
				{
					Name = title,
					IsBackground = true
				};
				_waitFor.SetApartmentState(ApartmentState.STA);
				_waitFor.Start();

				// Wait until finished
				while (!_waitFor.Join(TimeSpan.FromMilliseconds(100)))
				{
                    // TODO: Remove this please!!!!
					Application.DoEvents();
				}
				Log.Debug().WriteLine("Finished {0}", title);
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex);
				throw;
			}
			finally
			{
				Close();
			}
			// Check if an exception occured, if so throw it
			if (threadException != null)
			{
				throw threadException;
			}
		}

		/// <summary>
		///     Called if the cancel button is clicked, will use Thread.Abort()
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CancelButtonClick(object sender, EventArgs e)
		{
			Log.Debug().WriteLine("Cancel clicked on {0}", Text);
			cancelButton.Enabled = false;
			_waitFor?.Abort();
		    _cancellationTokenSource?.Cancel();
        }
	}
}