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

using GreenshotPlugin.Core;
using Dapplo.Windows.Native;
using log4net;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace GreenshotPlugin.Extensions
{
	public static class ControlExtensions
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (ControlExtensions));

		/// <summary>
		/// Very simple extention which makes it easier to call BeginInvoke on a control with a lambda
		/// </summary>
		/// <param name="control"></param>
		/// <param name="action">Lambda</param>
		public static void AsyncInvoke(this Control control, Action action)
		{
			control.BeginInvoke(action);
		}

		/// <summary>
		/// Waits asynchronously for the Toolstrip to close
		/// </summary>
		/// <param name="toolStripDropDown">The ToolStripDropDown to wait for cancellation.</param>
		/// <param name="cancellationToken">A cancellation token. If invoked, the task will return 
		/// immediately as canceled.</param>
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
		/// This method will show the supplied context menu at the mouse cursor, also makes sure it has focus and it's not visible in the taskbar.
		/// </summary>
		/// <param name="toolStripDropDown"></param>
		public static void ShowAtCursor(this ToolStripDropDown toolStripDropDown)
		{
			// find a suitable location
			var location = Cursor.Position;
			var rectangle = new Rectangle(location, toolStripDropDown.Size);

			rectangle.Intersect(WindowCapture.GetScreenBounds());
			if (rectangle.Height < toolStripDropDown.Height)
			{
				location.Offset(-40, -(rectangle.Height - toolStripDropDown.Height));
			}
			else
			{
				location.Offset(-40, -10);
			}

			// This prevents the problem that the context menu shows in the task-bar
			User32.SetForegroundWindow(PluginUtils.Host.NotifyIcon.ContextMenuStrip.Handle);
			toolStripDropDown.Show(location);
			toolStripDropDown.Focus();
		}

		/// <summary>
		/// Extension to await for the ShowDialog of a WPF Window
		/// </summary>
		/// <param name="window"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public static async Task<bool?> ShowDialogAsync(this Window window, CancellationToken token = default(CancellationToken))
		{
			var taskCompletionSource = new TaskCompletionSource<bool>();
			// show the dialog asynchronously 
			// (presumably on the next iteration of the message loop)
			using (token.Register(() => taskCompletionSource.TrySetCanceled(), useSynchronizationContext: true))
			{
				EventHandler loadedHandler = (s, e) => taskCompletionSource.TrySetResult(true);

				window.Closed += loadedHandler;
				try
				{
					SynchronizationContext.Current.Post((_) => window.ShowDialog(), null);
					await taskCompletionSource.Task;
				}
				finally
				{
					window.Closed -= loadedHandler;
				}
			}
			return window.DialogResult;
		}

		/// <summary>
		/// Extension to call show, than await for the hide of a WPF Window
		/// </summary>
		/// <param name="window"></param>
		/// <param name="token"></param>
		/// <returns>Task to await</returns>
		public static async Task ShowAsync(this Window window, CancellationToken token = default(CancellationToken))
		{
			var taskCompletionSource = new TaskCompletionSource<bool>();
			// show the dialog asynchronously 
			// (presumably on the next iteration of the message loop)
			using (token.Register(() => taskCompletionSource.TrySetCanceled(), useSynchronizationContext: true))
			{
				DependencyPropertyChangedEventHandler visibilityHandler = (s, e) =>
				{
					if ((bool)e.NewValue == false)
					{
						taskCompletionSource.TrySetResult(true);
					}
				};
                window.IsVisibleChanged += visibilityHandler;
				try
				{
					SynchronizationContext.Current.Post((_) => window.ShowDialog(), null);
					await taskCompletionSource.Task;
				}
				finally
				{
					window.IsVisibleChanged -= visibilityHandler;
				}
			}
		}
	}
}