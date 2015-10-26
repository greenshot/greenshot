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

using GreenshotPlugin.Interfaces;
using log4net;
using System;
using System.IO;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Addons;

namespace Greenshot.Helpers
{
	/// <summary>
	/// This startup/shutdown action starts the Greenshot "server", which allows to open files etc.
	/// </summary>
	[StartupAction, ShutdownAction]
	public class GreenshotServer : IGreenshotContract, IStartupAction, IShutdownAction
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (GreenshotServer));
		private ServiceHost _host;
		private const string PipeBaseEndpoint = "net.pipe://localhost/Greenshot/Server_";

		private static string Identity
		{
			get
			{
				return WindowsIdentity.GetCurrent()?.User?.ToString();
			}
		}

		/// <summary>
		/// This is the endpoint where the server is running
		/// </summary>
		public static string EndPoint
		{
			get
			{
				return $"{PipeBaseEndpoint}{Identity}";
			}
		}

		/// <summary>
		/// IStartupAction entry for starting
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task StartAsync(CancellationToken token = default(CancellationToken))
		{
			LOG.Debug("Starting Greenshot server");
			await Task.Run(() =>
			{
				_host = new ServiceHost(this, new[]
				{
					new Uri(PipeBaseEndpoint)
				});
				_host.AddServiceEndpoint(typeof (IGreenshotContract), new NetNamedPipeBinding(), EndPoint);
				_host.Open();
			}, token).ConfigureAwait(false);
			LOG.Debug("Started Greenshot server");
		}

		/// <summary>
		/// IShutdownAction entry, This stops the Greenshot server
		/// </summary>
		/// <param name="token"></param>
		/// <returns>Task</returns>
		public async Task ShutdownAsync(CancellationToken token = default(CancellationToken))
		{
			LOG.Debug("Stopping Greenshot server");
			await Task.Run(() =>
			{
				if (_host != null)
				{
					_host.Close();
					_host = null;
				}
			}, token).ConfigureAwait(false);
		}

		#region IGreenshotContract

		/// <summary>
		/// Exit Greenshot
		/// </summary>
		public void Exit()
		{
			Forms.MainForm.Instance.Exit();
		}

		/// <summary>
		/// Open a file into Greenshot
		/// </summary>
		/// <param name="filename"></param>
		public void OpenFile(string filename)
		{
			LOG.InfoFormat("Open file requested for: {0}", filename);

			if (File.Exists(filename))
			{
				Forms.MainForm.Instance.BeginInvoke(new Action(async () => await CaptureHelper.CaptureFileAsync(filename)));
			}
			else
			{
				LOG.Warn("No such file: " + filename);
			}
		}

		#endregion
	}
}