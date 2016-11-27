using System.Runtime.InteropServices;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using Dapplo.Windows.Structs;
using System.Drawing;

namespace Greenshot.CaptureCore
{
	/// <summary>
	/// Capture the cursor
	/// </summary>
	public class CaptureCursor
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		///     This method will capture the current Cursor by using User32 Code
		/// </summary>
		/// <param name="screenOffset">Point with the offset of the screen</param>
		/// <returns>Cursor info</returns>
		public CursorInfo Capture(Point screenOffset)
		{
			Log.Debug().WriteLine("Capturing the mouse cursor.");
			var cursorResult = new CursorInfo();

			var cursorInfo = new CURSORINFO();
			cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);
			if (User32.GetCursorInfo(out cursorInfo))
			{
				if ((cursorInfo.flags & CursorInfoFlags.CURSOR_SHOWING) == CursorInfoFlags.CURSOR_SHOWING)
				{
					var cursor = new Cursor(cursorInfo.hCursor);
					cursorResult.Cursor = cursor;
					var cursorLocation = User32.GetCursorLocation();
					// Allign cursor location to Bitmap coordinates (instead of Screen coordinates)
					var x = (int)cursorLocation.X - cursor.HotSpot.X - screenOffset.X;
					var y = (int)cursorLocation.Y - cursor.HotSpot.Y - screenOffset.Y;
					// Set the location
					cursorResult.Location = new Point(x, y);
				}
			}
			return cursorResult;
		}

	}
}
