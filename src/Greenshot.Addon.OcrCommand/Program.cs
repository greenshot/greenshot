#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using Greenshot.Addon.OcrCommand.Modi;
#if DEBUG
using System.Diagnostics;
#endif
#endregion

namespace Greenshot.Addon.OcrCommand
{
	public static class Program
	{
		private const string Usage = "<-c> | <path to image.bmp> [language] [orientimage] [straightenImage]";

		public static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine(Usage);
				return -1;
			}
			var filename = args[0];
			var language = ModiLanguage.ENGLISH;
			if (args.Length >= 2)
			{
				language = (ModiLanguage) Enum.Parse(typeof(ModiLanguage), args[1]);
			}
			var orientimage = true;
			if (args.Length >= 3)
			{
				orientimage = bool.Parse(args[2]);
			}
			var straightenImage = true;
			if (args.Length >= 4)
			{
				straightenImage = bool.Parse(args[3]);
			}
			try
			{
				if (File.Exists(filename) || "-c".Equals(filename))
				{
					using (var document = COMWrapper.GetOrCreateInstance<IDocument>())
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
						document.Create(filename);
						document.OCR(language, orientimage, straightenImage);
						var modiImage = document.Images[0];
						var layout = modiImage.Layout;
						if (layout != null)
						{
#if DEBUG
							if (layout.Words != null)
							{
								foreach (var word in ToEnumerable(layout.Words))
								{
								    if (word.Rects == null)
								    {
								        continue;
								    }

								    foreach (var rect in ToEnumerable(word.Rects))
								    {
								        Debug.WriteLine($"Rect {rect.Left},{rect.Top},{rect.Right},{rect.Bottom} - Word {word.Text} : Confidence: {word.RecognitionConfidence}");
								    }
								}
							}
#endif
							if (layout.Text != null)
							{
								// For for BUG-1884:
								// Although trim is done in the OCR Plugin, it does make sense in the command too.
								Console.WriteLine(layout.Text.Trim());
							}
						}
						document.Close(false);
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

		/// <summary>
		///     Helper method
		/// </summary>
		/// <returns>IEnumerable of IMiRect</returns>
		private static IEnumerable<IMiRect> ToEnumerable(IMiRects rects)
		{
			for (var i = 0; i < rects.Count; i++)
			{
				yield return rects[i];
			}
		}

		/// <summary>
		///     Helper method
		/// </summary>
		/// <returns>IEnumerable of IWord</returns>
		private static IEnumerable<IWord> ToEnumerable(IWords words)
		{
			for (var i = 0; i < words.Count; i++)
			{
				yield return words[i];
			}
		}
	}
}