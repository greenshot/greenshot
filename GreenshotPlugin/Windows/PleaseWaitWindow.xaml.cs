/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.Integration;

namespace GreenshotPlugin.Windows {
	/// <summary>
	/// Interaction logic for PleaseWaitWindow.xaml
	/// </summary>
	public partial class PleaseWaitWindow : Window, IProgress<int>, INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		private int _progressValue = 0;
		private bool _isIndeterminate = true;
		private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private string _text;

		public CancellationToken Token {
			get {
				return _cancellationTokenSource.Token;
			}
		}

		public string Text {
			get {
				return _text;
			}
			set {
				if (_text != value) {
					_text = value;
					if (PropertyChanged != null) {
						PropertyChanged(this, new PropertyChangedEventArgs("Text"));
					}
				}
			}
		}

		public bool IsIndeterminate {
			get {
				return _isIndeterminate;
			}
			set {
				if (_isIndeterminate != value) {
					_isIndeterminate = value;
					if (PropertyChanged != null) {
						PropertyChanged(this, new PropertyChangedEventArgs("IsIndeterminate"));
					}
				}
			}
		}

		public int ProgressValue {
			get {
				return _progressValue;
			}
			set {
				if (_progressValue != value) {
					_progressValue = value;
					if (PropertyChanged != null) {
						PropertyChanged(this, new PropertyChangedEventArgs("ProgressValue"));
					}
				}
			}
		}

		/// <summary>
		/// Create and show a Please-Wait window, which will allow us to terminate the upload
		/// </summary>
		/// <param name="title">title of the window</param>
		/// <param name="text">text for the window</param>
		/// <param name="asyncFunction">async Function to run</param>
		/// <param name="isIndeterminate">Tell if there is a progress reporting</param>
		/// <returns>Task to wait for</returns>
		public static async Task CreateAndShowAsync(string title, string text, Func<IProgress<int>, CancellationToken, Task> asyncFunction, bool isIndeterminate = true) {
			var pleaseWaitWindow = new PleaseWaitWindow();
			ElementHost.EnableModelessKeyboardInterop(pleaseWaitWindow);
			pleaseWaitWindow.Title = title;
			pleaseWaitWindow.Text = text;
			pleaseWaitWindow.IsIndeterminate = isIndeterminate;
			pleaseWaitWindow.Show();
			await asyncFunction(pleaseWaitWindow, pleaseWaitWindow.Token).ConfigureAwait(false);
			pleaseWaitWindow.Close();
		}

		private PleaseWaitWindow() {
			DataContext = this;
			InitializeComponent();
		}

		public void Report(int value) {
			if (!Dispatcher.CheckAccess()) {
				Dispatcher.Invoke(() => Report(value));
				return;
			}
			ProgressValue = value;
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			_cancellationTokenSource.Cancel();
		}
	}
}
