//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Runtime.Serialization;
using Greenshot.Addon.Editor.Interfaces.Drawing;

#endregion

namespace Greenshot.Addon.Editor.Drawing
{
	/// <summary>
	///     Description of ObfuscateContainer.
	/// </summary>
	[Serializable]
	public class ObfuscateContainer : FilterContainer
	{
		private PreparedFilter _preparedFilter = PreparedFilter.PIXELIZE;

		public ObfuscateContainer(Surface parent) : base(parent)
		{
			Init();
		}

		[Field(FieldTypes.PREPARED_FILTER_OBFUSCATE)]
		public override PreparedFilter Filter
		{
			get { return _preparedFilter; }
			set
			{
				_preparedFilter = value;
				OnFieldPropertyChanged(FieldTypes.PREPARED_FILTER_OBFUSCATE);
				// Before we had to register to events to know if the value was changed
				ConfigurePreparedFilters();
			}
		}

		private void Init()
		{
			ConfigurePreparedFilters();
			CreateDefaultAdorners();
		}

		protected override void OnDeserialized(StreamingContext context)
		{
			Init();
		}
	}
}