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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GreenshotLanguageEditor {
	public class LanguageFile {
	    public string FilePath {
			get {return Path.Combine(FileDir,FileName);}
			set {
				FileDir = Path.GetDirectoryName(value);
				FileName = Path.GetFileName(value);
			}
	    }
		public string FileDir {
			get;
			set;
		}
		public string FileName {
			get;
			set;
	    }
	    public string IETF {
	        get;
	        set;
	    }
	    public string Version {
	        get;
	        set;
	    }
	    public string Languagegroup {
	        get;
	        set;
	    }
	    public string Description {
	        get;
	        set;
	    }
	}
	/// <summary>
	/// Description of Language.
	/// </summary>
	public static class GreenshotLanguage {
		public static IEnumerable<LanguageFile> GetLanguageFiles(string languageFilePath, string languageFilePattern) {
	        var languageFiles = from languageFile in Directory.GetFiles(languageFilePath, languageFilePattern, SearchOption.AllDirectories).ToList<string>()
	                            from language in XDocument.Load(languageFile).Descendants("language")
	                            select new LanguageFile {
	                                FilePath = languageFile,
	                                Description = (string)language.Attribute("description"),
	                                IETF = (string)language.Attribute("ietf"),
	                                Version = (string)language.Attribute("version"),
	                                Languagegroup = (string)language.Attribute("languagegroup")
	                            };
	
	        foreach (LanguageFile languageFile in languageFiles) {
	            yield return languageFile;
	        }
	    }
		
	    public static IDictionary<string, string> ReadLanguageFile(LanguageFile languageFile) {
	        return (from resource in XDocument.Load(languageFile.FilePath).Descendants("resource")
	                where !string.IsNullOrEmpty(resource.Value)
	                select new {
	                    Name = (string)resource.Attribute("name"),
	                    Text = resource.Value.Trim()
	                }
	               ).ToDictionary(item => item.Name, item => item.Text);
	    }
	}
}
