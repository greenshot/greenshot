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
		[DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
		private static extern int GdipBitmapApplyEffect(IntPtr bitmap, IntPtr effect, ref RECT rectOfInterest, bool useAuxData, IntPtr auxData, int auxDataSize);
		[DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
		private static extern int GdipSetEffectParameters(IntPtr effect, IntPtr parameters, uint size);
		[DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
		private static extern int GdipCreateEffect(Guid guid, out IntPtr effect);
		[DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
		private static extern int GdipDeleteEffect(IntPtr effect);
		private static Guid BlurEffectGuid = new Guid("{633C80A4-1843-482B-9EF2-BE2834C5FDD4}");

		internal static TResult GetPrivateField<TResult>(object obj, string fieldName) {
			if (obj == null) {
				return default(TResult);
			}
			Type ltType = obj.GetType();
			FieldInfo lfiFieldInfo = ltType.GetField( fieldName,System.Reflection.BindingFlags.GetField |System.Reflection.BindingFlags.Instance |System.Reflection.BindingFlags.NonPublic);
			if (lfiFieldInfo != null) {
				return (TResult)lfiFieldInfo.GetValue(obj);
			} else {
				throw new InvalidOperationException(string.Format("Instance field '{0}' could not be located in object of type '{1}'.",fieldName, obj.GetType().FullName));
			}
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
			try {
				BlurParams blurParams = new BlurParams();
				blurParams.Radius = radius;
				blurParams.ExpandEdges = false;
				IntPtr hBlurParams = Marshal.AllocHGlobal(Marshal.SizeOf(blurParams));
				Marshal.StructureToPtr(blurParams, hBlurParams, true);

				uint paramsSize = (uint)Marshal.SizeOf(blurParams);

				IntPtr hEffect = IntPtr.Zero;

				int status = GdipCreateEffect(BlurEffectGuid, out hEffect);
				GdipSetEffectParameters(hEffect, hBlurParams, paramsSize);

				//IntPtr hBitmap = destinationBitmap.GetHbitmap();
				IntPtr hBitmap = GetPrivateField<IntPtr>(destinationBitmap, "nativeImage");
				RECT rec = new RECT(area);
				GdipBitmapApplyEffect(hBitmap, hEffect, ref rec, false, IntPtr.Zero, 0);
				GdipDeleteEffect(hEffect);
				Marshal.FreeHGlobal(hBlurParams);
				return true;
			} catch (Exception ex) {
				return false;
			}
		}
	}
}
