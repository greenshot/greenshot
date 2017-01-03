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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Dapplo.Addons;
using Dapplo.HttpExtensions;
using Dapplo.Log;
using Greenshot.Addon.Core;
using Greenshot.Core.Configuration;
using System.Reflection;
using System.Threading.Tasks;

#endregion

namespace Greenshot.Services
{
	/// <summary>
	///     This service takes care of checking for an update of the software or eventually blog entries, and notifies the user
	/// </summary>
	[StartupAction(StartupOrder = (int) GreenshotStartupOrder.Bootstrapper + 1)]
	[ShutdownAction]
	public class UpdateService : IStartupAction, IShutdownAction
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly Version CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
		private IDisposable _updateSubscription;
		private UpdateFeedInfo _currentUpdateFeedInfo;

		[Import]
		private IUpdateConfiguration UpdateConfiguration { get; set; }

		/// <summary>
		/// Stop the update checker
		/// </summary>
		public void Shutdown()
		{
			_updateSubscription.Dispose();
		}

		/// <summary>
		/// Start the update checker
		/// </summary>
		public void Start()
		{
			// Create the background checker
			_updateSubscription = Observable.Timer(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5))
				.ObserveOn(NewThreadScheduler.Default)
				.Subscribe(async l => await CheckForUpdateAsync());
		}

		/// <summary>
		///     This does the real update check
		/// </summary>
		private async Task CheckForUpdateAsync()
		{
			// Update check is turned off
			if (!UpdateConfiguration.CheckForUpdates)
			{
				return;
			}

			if (UpdateConfiguration.LastUpdateCheck != default(DateTimeOffset))
			{
				var checkTime = UpdateConfiguration.LastUpdateCheck;
				checkTime = checkTime.AddDays(UpdateConfiguration.UpdateCheckInterval);
				if (DateTimeOffset.Now.CompareTo(checkTime) < 0)
				{
					Log.Debug().WriteLine("No need to check RSS feed for updates, feed check will be after {0}", checkTime);
					return;
				}
				Log.Debug()
					.WriteLine("Update check is due, last check was {0} check needs to be made after {1} (which is one {2} later)", UpdateConfiguration.LastUpdateCheck, checkTime,
						UpdateConfiguration.UpdateCheckInterval);
			}

			try
			{
				await UpdatedProjectFeedInfoAsync().ConfigureAwait(false);
				// TODO: Implement the check logic
				var newerRelease = _currentUpdateFeedInfo?.Releases.OrderByDescending(artifact => artifact.Version).FirstOrDefault(artifact => artifact.Version > CurrentVersion);
				if (newerRelease != null)
				{
					// We have a newer release
				}
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex, "Couldn't check the project feed.");
			}

		}

		/// <summary>
		/// Read the project feed
		/// </summary>
		/// <returns>Task</returns>
		private async Task UpdatedProjectFeedInfoAsync()
		{
			_currentUpdateFeedInfo = await UpdateConfiguration.ProjectFeed.GetAsAsync<UpdateFeedInfo>();
			UpdateConfiguration.LastUpdateCheck = DateTimeOffset.Now;
		}
	}

	/// <summary>
	///     This describes a Greenshot artifact (exe)
	/// </summary>
	public class Artifact
	{
		public Version Version { get; set; }

		public string Title { get; set; }

		public Uri BlogUri { get; set; }
	}

	/// <summary>
	///     This describes a blog entry on our site
	/// </summary>
	public class Blog
	{
		public string Title { get; set; }

		public Uri BlogUri { get; set; }
	}

	/// <summary>
	///     This is the container for the project feed with artifacts and blog entries
	/// </summary>
	public class UpdateFeedInfo
	{
		public IEnumerable<Artifact> Releases { get; set; }
		public IEnumerable<Artifact> ReleaseCandidates { get; set; }
		public IEnumerable<Artifact> Betas { get; set; }
		public IEnumerable<Artifact> Blogs { get; set; }
	}
}