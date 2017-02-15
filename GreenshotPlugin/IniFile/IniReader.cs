#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System.Collections.Generic;
using System.IO;
using System.Text;

#endregion

namespace GreenshotPlugin.IniFile
{
	/// <summary>
	/// </summary>
	public static class IniReader
	{
		private const string SectionStart = "[";
		private const string SectionEnd = "]";
		private const string Comment = ";";
		private static readonly char[] Assignment = {'='};

		/// <summary>
		///     Read an ini file to a Dictionary, each key is a section and the value is a Dictionary with name and values.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="encoding"></param>
		/// <returns></returns>
		public static IDictionary<string, IDictionary<string, string>> Read(string path, Encoding encoding)
		{
			var ini = new Dictionary<string, IDictionary<string, string>>();
			using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024))
			{
				using (var streamReader = new StreamReader(fileStream, encoding))
				{
					IDictionary<string, string> nameValues = new Dictionary<string, string>();
					while (!streamReader.EndOfStream)
					{
						var line = streamReader.ReadLine();
						if (line == null)
						{
							continue;
						}
						var cleanLine = line.Trim();
						if (cleanLine.Length == 0 || cleanLine.StartsWith(Comment))
						{
							continue;
						}
						if (cleanLine.StartsWith(SectionStart))
						{
							var section = line.Replace(SectionStart, string.Empty).Replace(SectionEnd, string.Empty).Trim();
							if (!ini.TryGetValue(section, out nameValues))
							{
								nameValues = new Dictionary<string, string>();
								ini.Add(section, nameValues);
							}
						}
						else
						{
							var keyvalueSplitter = line.Split(Assignment, 2);
							var name = keyvalueSplitter[0];
							var inivalue = keyvalueSplitter.Length > 1 ? keyvalueSplitter[1] : null;
							if (nameValues.ContainsKey(name))
							{
								nameValues[name] = inivalue;
							}
							else
							{
								nameValues.Add(name, inivalue);
							}
						}
					}
				}
			}
			return ini;
		}
	}
}