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
using System.Text.RegularExpressions;

namespace GreenshotConfluencePlugin.Model
{
	/// <summary>
	/// Simple container for the Space
	/// </summary>
	public class Space
	{
		private static readonly Regex ContentIdRegEx = new Regex(@".*/(?<contentid>[0-9]+)$", RegexOptions.Compiled);

		/// <summary>
		/// Create a Space object from the result of the Space-Rest API call
		/// </summary>
		/// <param name="spaceJson"></param>
		/// <returns>Filled Space instance</returns>
		public static Space CreateFrom(dynamic spaceJson)
		{
			return new Space().FillFrom(spaceJson);
		}

		/// <summary>
		/// Fill the space from the supplied Json
		/// </summary>
		/// <param name="spaceJson">dynamic with Json</param>
		/// <returns>Space (this)</returns>
		public Space FillFrom(dynamic spaceJson)
		{
			SpaceId = Convert.ToInt64(spaceJson.id);
			SpaceKey = spaceJson.key;
			Name = spaceJson.name;
			ContentId = long.Parse(ContentIdRegEx.Match((string) spaceJson._expandable.homepage).Groups["contentid"].Value);
			return this;
		}

		public long SpaceId
		{
			get;
			set;
		}

		public string SpaceKey
		{
			get;
			set;
		}

		public string Name
		{
			get;
			set;
		}

		public long ContentId
		{
			get;
			set;
		}

		public bool IsPersonal
		{
			get
			{
				return SpaceKey.StartsWith("~");
			}
		}
	}
}