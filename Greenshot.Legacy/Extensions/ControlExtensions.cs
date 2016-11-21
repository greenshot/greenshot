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
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Greenshot.Core;

#endregion

namespace Greenshot.Legacy.Extensions
{
	public static class ControlExtensions
	{
		/// <summary>
		///     Very simple extention which makes it easier to call BeginInvoke on a control with a lambda
		/// </summary>
		/// <param name="control"></param>
		/// <param name="action">Lambda</param>
		/// <param name="cancellationToken"></param>
		public static Task<T> InvokeAsync<T>(this Control control, Func<T> action, CancellationToken cancellationToken = default(CancellationToken))
		{
			var taskCompletionSource = new TaskCompletionSource<T>();
			if (cancellationToken != default(CancellationToken))
			{
				cancellationToken.Register(taskCompletionSource.SetCanceled);
			}

			control.BeginInvoke(new Action(() =>
			{
				var result = action();
				taskCompletionSource.SetResult(result);
			}));
			return taskCompletionSource.Task;
		}

		/// <summary>
		///     Very simple extention which makes it easier to call BeginInvoke on a control with a lambda
		/// </summary>
		/// <param name="control"></param>
		/// <param name="action">Lambda</param>
		/// <param name="cancellationToken"></param>
		public static Task InvokeAsync(this Control control, Action action, CancellationToken cancellationToken = default(CancellationToken))
		{
			var taskCompletionSource = new TaskCompletionSource<bool>();
			if (cancellationToken != default(CancellationToken))
			{
				cancellationToken.Register(taskCompletionSource.SetCanceled);
			}

			control.BeginInvoke(new Action(() =>
			{
				action();
				taskCompletionSource.SetResult(true);
			}));
			return taskCompletionSource.Task;
		}

		/// <summary>
		///     This method will show the supplied context menu at the mouse cursor, also makes sure it has focus and it's not
		///     visible in the taskbar.
		/// </summary>
		/// <param name="toolStripDropDown"></param>
		public static void ShowAtCursor(this ToolStripDropDown toolStripDropDown)
		{
			// find a suitable location
			var location = Cursor.Position;
			var rectangle = new Rectangle(location, toolStripDropDown.Size);

			rectangle.Intersect(ScreenHelper.GetScreenBounds());
			if (rectangle.Height < toolStripDropDown.Height)
			{
				location.Offset(-40, -(rectangle.Height - toolStripDropDown.Height));
			}
			else
			{
				location.Offset(-40, -10);
			}

			// This prevents the problem that the context menu shows in the task-bar
			// TODO: add again?
			// User32.SetForegroundWindow(PluginUtils.Host.NotifyIcon.ContextMenuStrip.Handle);
			toolStripDropDown.Show(location);
			toolStripDropDown.Focus();
		}

		/// <summary>
		///     Waits asynchronously for the Toolstrip to close
		/// </summary>
		/// <param name="toolStripDropDown">The ToolStripDropDown to wait for cancellation.</param>
		/// <param name="cancellationToken">A cancellation token. If invoked, the task will return immediately as canceled.</param>
		/// <returns>A Task representing waiting for the ToolStripDropDown to close.</returns>
		public static Task WaitForClosedAsync(this ToolStripDropDown toolStripDropDown, CancellationToken cancellationToken = default(CancellationToken))
		{
			var taskCompletionSource = new TaskCompletionSource<object>();
			toolStripDropDown.Closed += (sender, args) => taskCompletionSource.TrySetResult(null);
			if (cancellationToken != default(CancellationToken))
			{
				cancellationToken.Register(taskCompletionSource.SetCanceled);
			}

			return taskCompletionSource.Task;
		}

		/// <summary>
		///     Waits asynchronously for the Toolstrip to close
		/// </summary>
		/// <param name="form">The form to wait for cancellation.</param>
		/// <param name="cancellationToken">A cancellation token. If invoked, the task will return immediately as canceled.</param>
		/// <returns>A Task representing waiting for the ToolStripDropDown to close.</returns>
		public static Task WaitForClosedAsync(this Form form, CancellationToken cancellationToken = default(CancellationToken))
		{
			var taskCompletionSource = new TaskCompletionSource<object>();
			form.Closed += (sender, args) => taskCompletionSource.TrySetResult(null);
			if (cancellationToken != default(CancellationToken))
			{
				cancellationToken.Register(taskCompletionSource.SetCanceled);
			}

			return taskCompletionSource.Task;
		}
	}
}