// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Dapplo.Jira;
using Dapplo.Jira.Entities;
using Dapplo.Utils;

namespace Greenshot.Addon.Jira
{
	/// <summary>
	///     This is the bach for the IssueType bitmaps
	/// </summary>
	public class IssueTypeBitmapCache : AsyncMemoryCache<IssueType, BitmapSource>
	{
		private readonly IJiraClient _jiraClient;

		public IssueTypeBitmapCache(IJiraClient jiraClient)
		{
			_jiraClient = jiraClient;
			// Set the expire timeout to an hour
			ExpireTimeSpan = TimeSpan.FromHours(4);
		}

		protected override string CreateKey(IssueType keyObject)
		{
			return keyObject.Name;
		}

		protected override async Task<BitmapSource> CreateAsync(IssueType issueType, CancellationToken cancellationToken = new CancellationToken())
		{
			return await _jiraClient.Server.GetUriContentAsync<BitmapSource>(issueType.IconUri, cancellationToken).ConfigureAwait(false);
		}
	}
}