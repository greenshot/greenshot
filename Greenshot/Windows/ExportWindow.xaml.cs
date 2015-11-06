/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015  Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Destination;
using System.Windows.Controls;
using System.Windows.Media;
using GreenshotPlugin.Extensions;
using System;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using GreenshotPlugin.Interfaces.Plugin;
using GreenshotPlugin.Core;
using GreenshotPlugin.Configuration;

namespace Greenshot.Windows
{
	/// <summary>
	/// Interaction logic for ExportWindow.xaml
	/// </summary>
	[Export]
	public partial class ExportWindow : Window
	{
		private Point _dragStartPoint;
		private bool _dragInProgress;
        private ICapture _capture;

		public ObservableCollection<IDestination> Children
		{
			get;
			set;
		} = new ObservableCollection<IDestination>();

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
		}

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			Close();
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
			DialogResult = true;
			e.Handled = true;
        }

		private void DragInitialize(object sender, MouseButtonEventArgs e)
		{
			// Store the mouse position
			_dragStartPoint = e.GetPosition(null);
		}

		private void DragStart(object sender, MouseEventArgs e)
		{
			if (_dragInProgress)
			{
				return;
			}
			// Get the current mouse position
			Point mousePos = e.GetPosition(null);
			Vector diff = _dragStartPoint - mousePos;

			if (e.LeftButton == MouseButtonState.Pressed &&
				Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
				Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
			{
				_dragInProgress = true;
				DragDrop.DoDragDrop(sender as Image, CreateDataObject(), DragDropEffects.Copy);
				_dragInProgress = false;
			}
		}

		/// <summary>
		/// Create the drag/drop data format
		/// </summary>
		private IDataObject CreateDataObject()
		{
			var dataObject = new DataObject();
			MemoryStream dibStream;
			const int BITMAPFILEHEADER_LENGTH = 14;
			using (MemoryStream tmpBmpStream = new MemoryStream())
			{
				// Save image as BMP
				SurfaceOutputSettings bmpOutputSettings = new SurfaceOutputSettings(OutputFormat.bmp, 100, false);
				ImageOutput.SaveToStream(_capture, tmpBmpStream, bmpOutputSettings);

				dibStream = new MemoryStream();
				// Copy the source, but skip the "BITMAPFILEHEADER" which has a size of 14
				dibStream.Write(tmpBmpStream.GetBuffer(), BITMAPFILEHEADER_LENGTH, (int)tmpBmpStream.Length - BITMAPFILEHEADER_LENGTH);
			}
			dataObject.SetData(DataFormats.Dib, dibStream, true);
			return dataObject;
        }
	}
}
