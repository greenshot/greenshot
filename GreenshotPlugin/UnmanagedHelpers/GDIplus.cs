/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;
using System.Reflection;

namespace GreenshotPlugin.UnmanagedHelpers {
	/// <summary>
	/// Contains members that specify the nature of a Gaussian blur.
	/// </summary>
	/// <remarks>Cannot be pinned with GCHandle due to bool value.</remarks>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal struct BlurParams {
		/// <summary>
		/// Real number that specifies the blur radius (the radius of the Gaussian convolution kernel) in 
		/// pixels. The radius must be in the range 0 through 255. As the radius increases, the resulting 
		/// bitmap becomes more blurry.
		/// </summary>
		public float Radius;

		/// <summary>
		/// Boolean value that specifies whether the bitmap expands by an amount equal to the blur radius. 
		/// If TRUE, the bitmap expands by an amount equal to the radius so that it can have soft edges. 
		/// If FALSE, the bitmap remains the same size and the soft edges are clipped.
		/// </summary>
		public bool ExpandEdges;
	}

	/// <summary>
	/// GDIplus Helpers
	/// </summary>
	public static class GDIplus {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(GDIplus));

		[DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
		private static extern int GdipBitmapApplyEffect(IntPtr bitmap, IntPtr effect, ref RECT rectOfInterest, bool useAuxData, IntPtr auxData, int auxDataSize);
		[DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
		private static extern int GdipSetEffectParameters(IntPtr effect, IntPtr parameters, uint size);
		[DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
		private static extern int GdipCreateEffect(Guid guid, out IntPtr effect);
		[DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
		private static extern int GdipDeleteEffect(IntPtr effect);
		private static Guid BlurEffectGuid = new Guid("{633C80A4-1843-482B-9EF2-BE2834C5FDD4}");

		// Constant "FieldInfo" for getting the nativeImage from the image
		private static readonly FieldInfo FIELD_INFO_NATIVE_IMAGE = typeof(Bitmap).GetField("nativeImage", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// Get the nativeImage field from the bitmap
		/// </summary>
		/// <param name="bitmap"></param>
		/// <returns></returns>
		private static IntPtr GetNativeImage(Bitmap bitmap) {
			return (IntPtr)FIELD_INFO_NATIVE_IMAGE.GetValue(bitmap);
		}

		/// <summary>
		/// Use the GDI+ blur effect on the bitmap
		/// </summary>
		/// <param name="destinationBitmap">Bitmap to apply the effect to</param>
		/// <param name="area">Rectangle to apply the blur effect to</param>
		/// <param name="radius">0-255</param>
		/// <returns>false if there is no GDI+ available or an exception occured</returns>
		public static bool ApplyBlur(Bitmap destinationBitmap, Rectangle area, float radius) {
			if (Environment.OSVersion.Version.Major < 6) {
				return false;
			}
			IntPtr hBlurParams = IntPtr.Zero;
			IntPtr hEffect = IntPtr.Zero;

			try {
				// Create a BlurParams struct and set the values
				BlurParams blurParams = new BlurParams();
				blurParams.Radius = radius;
				blurParams.ExpandEdges = false;

				// Allocate space in unmanaged memory
				hBlurParams = Marshal.AllocHGlobal(Marshal.SizeOf(blurParams));
				// Copy the structure to the unmanaged memory
				Marshal.StructureToPtr(blurParams, hBlurParams, true);

				// Create the GDI+ BlurEffect, using the Guid
				int status = GdipCreateEffect(BlurEffectGuid, out hEffect);

				// Set the blurParams to the effect
				GdipSetEffectParameters(hEffect, hBlurParams, (uint)Marshal.SizeOf(blurParams));

				// Somewhere it said we can use destinationBitmap.GetHbitmap(), this doesn't work!!
				// Get the private nativeImage property from the Bitmap
				IntPtr hBitmap = GetNativeImage(destinationBitmap);

				// Create a RECT from the Rectangle
				RECT rec = new RECT(area);
				// Apply the effect to the bitmap in the specified area
				GdipBitmapApplyEffect(hBitmap, hEffect, ref rec, false, IntPtr.Zero, 0);

				// Everything worked, return true
				return true;
			} catch (Exception ex) {
				LOG.Error("Problem using GdipBitmapApplyEffect: ", ex);
				return false;
			} finally {
				if (hEffect != null) {
					// Delete the effect
					GdipDeleteEffect(hEffect);
				}
				if (hBlurParams != IntPtr.Zero) {
					// Free the memory
					Marshal.FreeHGlobal(hBlurParams);
				}
			}
		}
	}
}
