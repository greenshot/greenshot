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
using System.Collections;
using Greenshot.Interop;

namespace GreenshotOCR {
	[ComProgId("MODI.Document")]
	public interface ModiDocu : Common {
		void Close(bool SaveCall);
		IImages Images{
			get;
		}
		void Create(string file);
		void OCR(ModiLanguage language, bool Orientimage, bool StraightenImage);
		void SaveAs(string filename, FileFormat fileFormat, CompressionLevel compressionLevel);
	}

	public interface Common : IDisposable {
		ModiDocu Application { get; }
	}

	public interface ILayout : Common{
		string Text {
			get;
		}
	}
	public interface IImage : Common {
		ILayout Layout {
			get;
		}
	}

	public interface IImages : Common, IEnumerable{
		int Count {
			get;
		}
		IImage this [int index] {
			get;
		}
		new IEnumerator GetEnumerator();
	}

	public enum ModiLanguage {
		CHINESE_SIMPLIFIED = 2052,
		CHINESE_TRADITIONAL = 1028,
		CZECH = 5,
		DANISH = 6,
		DUTCH = 19,
		ENGLISH = 9,
		FINNISH = 11,
		FRENCH = 12,
		GERMAN = 7,
		GREEK = 8,
		HUNGARIAN = 14,
		ITALIAN = 16,
		JAPANESE = 17,
		KOREAN = 18,
		NORWEGIAN = 20,
		POLISH = 21,
		PORTUGUESE = 22,
		RUSSIAN = 25,
		SPANISH = 10,
		SWEDISH = 29,
		TURKISH = 31,
		SYSDEFAULT = 2048
	}
	
	public enum CompressionLevel {
		miCOMP_LEVEL_LOW = 0,
		miCOMP_LEVEL_MEDIUM = 1,
		miCOMP_LEVEL_HIGH = 2
	}
	public enum FileFormat {
		miFILE_FORMAT_DEFAULTVALUE = -1,
		miFILE_FORMAT_TIFF = 1,
		miFILE_FORMAT_TIFF_LOSSLESS = 2,
		miFILE_FORMAT_MDI = 4
	}
}
