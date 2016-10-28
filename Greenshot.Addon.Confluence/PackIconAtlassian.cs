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

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ControlzEx;
using Greenshot.Addon.Ui;

namespace Greenshot.Addon.Confluence
{
	/// <summary>
	/// A list of all Atlassian PackIcons
	/// </summary>
	public enum PackIconKindAtlassian
	{
		Confluence
	}

	/// <summary>
	/// Define the PackIcons that are by Atlassian itself
	/// </summary>
	public class PackIconAtlassian : PackIconBase<PackIconKindAtlassian>, INotifyPropertyChanged
	{
		private Size _iconSize = new Size(16, 16);
		private readonly IDictionary<PackIconKindAtlassian, Size> _sizeLookup = CreateIconSizes();


		static PackIconAtlassian()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(PackIconGreenshot), new FrameworkPropertyMetadata(typeof(PackIconGreenshot)));
		}

		public PackIconAtlassian() : base(CreateIconData)
		{
		}

		public new PackIconKindAtlassian Kind
		{
			get { return base.Kind; }
			set
			{
				base.Kind = value;
				IconSize = _sizeLookup[value];
			}
		}

		public Size IconSize
		{
			get { return _iconSize; }
			set
			{
				_iconSize = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Store the geomery data of the icons
		/// </summary>
		/// <returns>Dictionary to lookup the geomery data</returns>
		private static IDictionary<PackIconKindAtlassian, string> CreateIconData()
		{
			return new Dictionary<PackIconKindAtlassian, string>
			{
				{PackIconKindAtlassian.Confluence, "M124.4,133.1a107.8,107.8,0,0,0-7.2-12.1c-2-2.9-4.1-5.8-6.4-8.6a2.5,2.5,0,0,1,.2-3.2l2.1-2.1c9.6-9.8,19.5-19.9,27.1-30.8s12.6-22.6,13.7-34a3.5,3.5,0,0,0-1.3-3,3.4,3.4,0,0,0-2.1-.7l-1.2.2a167.7,167.7,0,0,1-21.7,6.5,4.3,4.3,0,0,0-3,2.6c-5,13.6-17.7,26.6-31,40.2A3.5,3.5,0,0,1,91,89.3a3.4,3.4,0,0,1-2.5-1.2c-13.3-13.6-26-26.5-31-40.2a4.3,4.3,0,0,0-3-2.6,169.2,169.2,0,0,1-21.7-6.7l-1.2-.2a3.4,3.4,0,0,0-3.4,3.7c1.1,11.3,5.6,22.5,13.7,34.1s17.5,21,27.1,30.8c17.7,18,34.3,35,35.8,54.7a3.4,3.4,0,0,0,3.4,3.2h20.7a3.4,3.4,0,0,0,3.4-3.6,62.5,62.5,0,0,0-1.6-11.2h0a1.4,1.4,0,0,1,0-.3,1.4,1.4,0,0,1,1.4-1.4h0.2c16.9,2.5,27.6,6.4,27.7,10.8h0c0,4.1-9.6,10.3-25.3,14.9h0a3.3,3.3,0,0,0,.9,6.4h0.7c21-4.9,35-13,35-22.3,0-11-18.8-20.7-46.9-25.2h0Z M68.8,122.4a3.4,3.4,0,0,0-2.4-1,3.4,3.4,0,0,0-2.8,1.5,105.4,105.4,0,0,0-5.9,10.2c-28,4.4-46.8,14.2-46.8,25.1,0,9.2,13.8,17.4,34.9,22.3h0.8a3.3,3.3,0,0,0,3.3-3.3,3.3,3.3,0,0,0-2.4-3.1h0c-15.6-4.5-25.3-10.7-25.3-14.9h0c0-4.4,10.7-8.3,27.6-10.8h0.2a1.4,1.4,0,0,1,1.4,1.4,1.4,1.4,0,0,1,0,.3h0a62.3,62.3,0,0,0-1.6,11.1A3.4,3.4,0,0,0,53,165H73.8a3.4,3.4,0,0,0,3.4-3.2A46,46,0,0,1,83,143.2a4.8,4.8,0,0,0-.4-5.2,191.6,191.6,0,0,0-13.8-15.7h0Z M88.6,71.2a3.3,3.3,0,0,0,2.4.9h0a3.3,3.3,0,0,0,2.4-.9,55.1,55.1,0,0,0,12.1-17.7,3.4,3.4,0,0,0,.3-1.3,3.2,3.2,0,0,0-3.1-3.2c-1.3,0-5.1.4-11.6,0.4h0c-6.5,0-10.3-.4-11.6-0.4a3.2,3.2,0,0,0-3.1,3.2,3.4,3.4,0,0,0,.3,1.3A55,55,0,0,0,88.6,71.2h0Z" }
			};
		}
		/// <summary>
		/// Store the icon sizes of the icons, as these icons don't all have the same size
		/// </summary>
		/// <returns>Dictionary to lookup the size</returns>
		private static IDictionary<PackIconKindAtlassian, Size> CreateIconSizes()
		{
			return new Dictionary<PackIconKindAtlassian, Size>
			{
				{PackIconKindAtlassian.Confluence, new Size(182,182) }
			};
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
