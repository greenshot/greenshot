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

using System;
using System.IO;
using MODI;

namespace GreenshotOCRCommand
{
	public class Program
	{
		private const string Usage = "<-c> | <path to image.bmp> [language] [orientimage] [straightenImage]";

		public static int Main(string[] args)
		{
			// to test
			// args = new[] { @"C:\localdata\test.bmp", "english" };
			if (args.Length == 0)
			{
				Console.WriteLine(Usage);
				return -1;
			}
			string filename = args[0];
			var language = MiLANGUAGES.miLANG_ENGLISH;
			if (args.Length >= 2)
			{
				try
				{
					language = (MiLANGUAGES) Enum.Parse(typeof (MiLANGUAGES), "miLANG_" + args[1].ToUpperInvariant().Replace("miLANG_", ""));
				}
// ReSharper disable once EmptyGeneralCatchClause
				catch
				{
					// Ignore and take english
				}
			}
			bool orientimage = true;
			if (args.Length >= 3)
			{
				orientimage = bool.Parse(args[2]);
			}
			bool straightenImage = true;
			if (args.Length >= 4)
			{
				straightenImage = bool.Parse(args[3]);
			}
			try
			{
				if (File.Exists(filename) || "-c".Equals(filename))
				{
					using (var document = DisposableCom.Create(new Document()))
					{
						if (document == null)
						{
							Console.WriteLine("MODI not installed");
							return -2;
						}
						if ("-c".Equals(filename))
						{
							return 0;
						}
						document.ComObject.Create(filename);
						document.ComObject.OCR(language, orientimage, straightenImage);
						using (var image = DisposableCom.Create((IImage) document.ComObject.Images[0]))
						{
							using (var layout = DisposableCom.Create(image.ComObject.Layout))
							{
								Console.WriteLine(layout.ComObject.Text);
							}
						}
						document.ComObject.Close();
						return 0;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			return -1;
		}
	}
}