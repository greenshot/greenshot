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
using System.Windows;
using ControlzEx;

namespace Greenshot.Ui
{
	/// <summary>
	/// A list of all Greenshot own PackIcons
	/// </summary>
	public enum PackIconKindGreenshot
	{
		Greenshot
	}

	/// <summary>
	/// Define the PackIcons that are by Greenshot itself
	/// </summary>
	public class PackIconGreenshot : PackIconBase<PackIconKindGreenshot>
	{
		static PackIconGreenshot()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(PackIconGreenshot), new FrameworkPropertyMetadata(typeof(PackIconGreenshot)));
		}

		public PackIconGreenshot() : base(CreateIconData)
		{ }

		private static IDictionary<PackIconKindGreenshot, string> CreateIconData()
		{
			return new Dictionary<PackIconKindGreenshot, string>
			{
				{PackIconKindGreenshot.Greenshot, "M 12.2769,5.5385 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 10.1846,4.7385 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 8.0923,3.9385 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 5.9692,3.1385 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 3.8462,2.3077 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 3.0769,4.4308 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 0.9846,3.6308 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 2.2769,6.5231 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 0.1538,5.7231 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 1.4769,8.6154 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M -0.6462,7.8769 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 0.6769,10.7385 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M -0.1538,12.8308 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 1.9692,13.6308 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 4.0923,14.4000 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 6.2154,15.2308 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 11.1692,14.7385 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 9.0769,13.9385 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 11.9692,12.6462 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 9.8769,11.8154 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1 M 7.7846,11.0154 a 1,1 0 0 1 1,1 a 1,1 0 1 1 -1,-1"}
			};
		}
	}
}
