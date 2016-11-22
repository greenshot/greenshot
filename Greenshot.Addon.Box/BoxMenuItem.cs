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

using System.ComponentModel.Composition;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.CaliburnMicro.Menu;
using Greenshot.Addon.Box.Forms;
using MahApps.Metro.IconPacks;

#endregion

namespace Greenshot.Addon.Box
{
	/// <summary>
	///     This will add an extry for the exit to the context menu
	/// </summary>
	[Export("systray", typeof(IMenuItem))]
	public sealed class BoxMenuItem : MenuItem
	{
		[Import]
		private IBoxConfiguration BoxConfiguration { get; set; }

		[Import]
		private IBoxLanguage BoxLanguage { get; set; }

		public override void Click(IMenuItem clickedItem)
		{
			new SettingsForm(BoxConfiguration).ShowDialog();
		}

		public override void Initialize()
		{
			Id = "Box";
			// automatically update the DisplayName
			BoxLanguage.CreateBinding(this, nameof(IBoxLanguage.SettingsTitle));
			Icon = new PackIconMaterial
			{
				Kind = PackIconMaterialKind.Box
			};
		}
	}
}