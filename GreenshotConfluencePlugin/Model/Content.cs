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

namespace GreenshotConfluencePlugin.Model {
	public class Content {
		private static readonly Regex SpaceKeyRegEx = new Regex(@".*/(?<spacekey>[0-9a-zA-Z]+)$", RegexOptions.Compiled);
		/// <summary>
		/// Create a Content object from the result of the Content-Rest API call
		/// </summary>
		/// <param name="contentJson"></param>
		/// <returns>Filled Content instance</returns>
		public static Content CreateFromContent(dynamic contentJson) {
			return new Content().FillFromContent(contentJson);
		}

		/// <summary>
		/// Fill the content from the supplied Json
		/// </summary>
		/// <param name="contentJson">dynamic with Json</param>
		/// <returns>Content (this)</returns>
		public Content FillFromContent(dynamic contentJson) {
			Id = Convert.ToInt64(contentJson.id);
			ContentType = contentJson.type;
			Title = contentJson.title;
			if (contentJson.IsDefined("space")) {
				SpaceKey = contentJson.space.key;
			} else if (contentJson.IsDefined("_expandable")) {
				SpaceKey = SpaceKeyRegEx.Match((string)contentJson._expandable.space).Groups["spacekey"].Value; ;
			}
			return this;
		}

		public string ContentType {
			get;
			set;
		}
		public long Id {
			get;
			set;
		}
		public string Title {
			get;
			set;
		}
		public string SpaceKey {
			get;
			set;
		}
	}
}
