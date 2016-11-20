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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

#endregion

namespace Greenshot.Addon.Extensions
{
	public static class ControlExtensions
	{
		/// <summary>
		///     Extension to call show, than await for the hide of a WPF Window
		/// </summary>
		/// <param name="window"></param>
		/// <param name="token"></param>
		/// <returns>Task to await</returns>
		public static async Task ShowAsync(this Window window, CancellationToken token = default(CancellationToken))
		{
			var taskCompletionSource = new TaskCompletionSource<bool>();
			// show the dialog asynchronously 
			// (presumably on the next iteration of the message loop)
			using (token.Register(() => taskCompletionSource.TrySetCanceled(), true))
			{
				DependencyPropertyChangedEventHandler visibilityHandler = (s, e) =>
				{
					if ((bool) e.NewValue == false)
					{
						taskCompletionSource.TrySetResult(true);
					}
				};
				window.IsVisibleChanged += visibilityHandler;
				try
				{
					SynchronizationContext.Current.Post(_ => window.ShowDialog(), null);
					await taskCompletionSource.Task;
				}
				finally
				{
					window.IsVisibleChanged -= visibilityHandler;
				}
			}
		}

		/// <summary>
		///     Extension to await for the ShowDialog of a WPF Window
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
					SynchronizationContext.Current.Post(_ => window.ShowDialog(), null);
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
		///     Return a matrix which allows to scale device coordinates to WPF coordinates
		/// </summary>
		/// <param name="visual">Visual to get the matrix from</param>
		/// <returns>Matrix</returns>
		public static Matrix TransformFromDevice(this Visual visual)
		{
			var presentationsource = PresentationSource.FromVisual(visual);

			if (presentationsource?.CompositionTarget != null) // make sure it's connected
			{
				return presentationsource.CompositionTarget.TransformFromDevice;
			}
			using (var source = new HwndSource(new HwndSourceParameters()))
			{
				if (source.CompositionTarget != null)
				{
					return source.CompositionTarget.TransformFromDevice;
				}
			}
			return Matrix.Identity;
		}

		/// <summary>
		///     Return a matrix which allows to scale device coordinates from WPF coordinates
		/// </summary>
		/// <param name="visual">Visual to get the matrix from</param>
		/// <returns>Matrix</returns>
		public static Matrix TransformToDevice(this Visual visual)
		{
			var presentationsource = PresentationSource.FromVisual(visual);

			if (presentationsource?.CompositionTarget != null) // make sure it's connected
			{
				return presentationsource.CompositionTarget.TransformToDevice;
			}
			using (var source = new HwndSource(new HwndSourceParameters()))
			{
				if (source.CompositionTarget != null)
				{
					return source.CompositionTarget.TransformToDevice;
				}
			}
			return Matrix.Identity;
		}

		/// <summary>
		///     Waits asynchronously for the Window to close
		/// </summary>
		/// <param name="window">The Window to wait for cancellation.</param>
		/// <param name="cancellationToken">
		///     A cancellation token. If invoked, the task will return
		///     immediately as canceled.
		/// </param>
		/// <returns>A Task representing waiting for the Window to close.</returns>
		public static Task WaitForClosedAsync(this Window window, CancellationToken cancellationToken = default(CancellationToken))
		{
			var taskCompletionSource = new TaskCompletionSource<object>();
			window.Closed += (sender, args) => taskCompletionSource.TrySetResult(null);
			if (cancellationToken != default(CancellationToken))
			{
				cancellationToken.Register(taskCompletionSource.SetCanceled);
			}

			return taskCompletionSource.Task;
		}
	}
}