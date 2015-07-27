using GreenshotPlugin.UnmanagedHelpers;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GreenshotPlugin.Core {
	public static class ControlExtension {

		/// <summary>
		/// Waits asynchronously for the Toolstrip to close
		/// </summary>
		/// <param name="toolStripDropDown">The ToolStripDropDown to wait for cancellation.</param>
		/// <param name="cancellationToken">A cancellation token. If invoked, the task will return 
		/// immediately as canceled.</param>
		/// <returns>A Task representing waiting for the ToolStripDropDown to close.</returns>
		public static Task WaitForClosedAsync(this ToolStripDropDown toolStripDropDown, CancellationToken cancellationToken = default(CancellationToken)) {
			var taskCompletionSource = new TaskCompletionSource<object>();
			toolStripDropDown.Closed += (sender, args) => taskCompletionSource.TrySetResult(null);
			if (cancellationToken != default(CancellationToken)) {
				cancellationToken.Register(taskCompletionSource.SetCanceled);
			}

			return taskCompletionSource.Task;
		}

		/// <summary>
		/// This method will show the supplied context menu at the mouse cursor, also makes sure it has focus and it's not visible in the taskbar.
		/// </summary>
		/// <param name="toolStripDropDown"></param>
		public static void ShowAtCursor(this ToolStripDropDown toolStripDropDown) {
			// find a suitable location
			var location = Cursor.Position;
			var rectangle = new Rectangle(location, toolStripDropDown.Size);

			rectangle.Intersect(WindowCapture.GetScreenBounds());
			if (rectangle.Height < toolStripDropDown.Height) {
				location.Offset(-40, -(rectangle.Height - toolStripDropDown.Height));
			} else {
				location.Offset(-40, -10);
			}

			// This prevents the problem that the context menu shows in the task-bar
			User32.SetForegroundWindow(PluginUtils.Host.NotifyIcon.ContextMenuStrip.Handle);
			toolStripDropDown.Show(location);
			toolStripDropDown.Focus();
		}
	}
}
