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

using System.Collections.Specialized;
using System.Text;

namespace GreenshotPlugin.Extensions
{
	public static class NameValueCollectionExtensions
	{
		/// <summary>
		/// Create a query string from a NameValueCollection
		/// </summary>
		/// <param name="nameValueCollection"></param>
		/// <returns>?name1=value1&name2=value2 etc...</returns>
		public static string ToQueryString(this NameValueCollection nameValueCollection)
		{
			var queryBuilder = new StringBuilder();

			for (int i = 0; i < nameValueCollection.Count; i++)
			{
				var key = nameValueCollection.AllKeys[i];
				queryBuilder.AppendFormat(i < nameValueCollection.Count - 1 ? "{0}={1}&" : "{0}={1}", key, nameValueCollection[key]);
			}

			return queryBuilder.ToString();
		}
	}
}
