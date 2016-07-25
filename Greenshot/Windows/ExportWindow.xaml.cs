/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using GongSolutions.Wpf.DragDrop;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Plugin;

namespace Greenshot.Windows
{
	/// <summary>
	/// Interaction logic for ExportWindow.xaml
	/// </summary>
	[Export]
	public partial class ExportWindow : Window, IDragSource
	{
        private ICapture _capture;
		private TaskCompletionSource<bool> _taskCompletionSource;

		/// <summary>
		/// All the destinations that needs showing
		/// </summary>
		public ObservableCollection<IDestination> Children
		{
			get;
			set;
		}

		public IDestination SelectedDestination
		{
			get;
			set;
		}

		public ICapture Capture
		{
			get
			{
				return _capture;
			}
			set
			{
				_capture = value;
				CapturedImage = _capture?.Image?.ToBitmapSource();
			}
		}

		public ImageSource CapturedImage
		{
			get;
			set;
		}

		public ExportWindow()
		{
			InitializeComponent();
			DataContext = this;
			SelectedDestination = null;
		}

		public async Task AwaitSelection()
		{
			if (!IsVisible)
			{
				Show();
			}
			_taskCompletionSource = new TaskCompletionSource<bool>();
			await _taskCompletionSource.Task;
			Hide();
		}

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			_taskCompletionSource.TrySetResult(true);
		}

		public void OnClick(object sender, RoutedEventArgs e)
		{
			var item = sender as MenuItem;
			var destination = item?.Tag as IDestination;
			if (destination == null)
			{
				return;
			}

			SelectedDestination = destination;
			e.Handled = true;
			_taskCompletionSource.TrySetResult(true);
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				_taskCompletionSource.TrySetResult(true);
			}
		}

		/// <summary>
		/// Create the drag/drop data format
		/// </summary>
		private IDataObject CreateDataObject()
		{
			var dataObject = new DataObject();
			MemoryStream dibStream;
			const int bitmapfileheaderLength = 14;
			using (var tmpBmpStream = new MemoryStream())
			{
				// Save image as BMP
				var bmpOutputSettings = new SurfaceOutputSettings(OutputFormat.bmp, 100, false);
				ImageOutput.SaveToStream(_capture, tmpBmpStream, bmpOutputSettings);

				dibStream = new MemoryStream();
				// Copy the source, but skip the "BITMAPFILEHEADER" which has a size of 14
				dibStream.Write(tmpBmpStream.GetBuffer(), bitmapfileheaderLength, (int)tmpBmpStream.Length - bitmapfileheaderLength);
			}
			dataObject.SetData(DataFormats.Dib, dibStream, true);
			return dataObject;
        }

		public void StartDrag(IDragInfo dragInfo)
		{
			dragInfo.Effects = DragDropEffects.Copy;
			dragInfo.Data = "Blub";
			dragInfo.DataObject = CreateDataObject();
        }

		public bool CanStartDrag(IDragInfo dragInfo)
		{
			return true;
		}

		public void Dropped(IDropInfo dropInfo)
		{
		}

		public void DragCancelled()
		{
		}

		public bool TryCatchOccurredException(Exception exception)
		{
			throw new NotImplementedException();
		}
	}
}
