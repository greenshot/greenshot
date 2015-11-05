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
using GongSolutions.Wpf.DragDrop;
using System;

namespace Greenshot.Windows
{
	/// <summary>
	/// Interaction logic for ExportWindow.xaml
	/// </summary>
	[Export]
	public partial class ExportWindow : Window, IDragSource
	{
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
				CapturedImage = _capture?.Image.ToBitmapSource();
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

		public void StartDrag(IDragInfo dragInfo)
		{
			dragInfo.Effects = DragDropEffects.Copy;
			dragInfo.Data = Capture.Image;
			dragInfo.DataObject = new DataObject();
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
	}
}
