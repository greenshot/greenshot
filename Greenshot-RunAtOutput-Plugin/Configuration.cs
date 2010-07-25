/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Xml;
using System.Xml.Serialization;

namespace RunAtOutput {
	/// <summary>
	/// Description of Configuration.
	/// </summary>
	[XmlRoot("RunAtOutput")]
	public class Configuration {
		public Configuration() {
			Commands = new List<Commando>();
		}
		[XmlElement("Commando")]
		public List<Commando> Commands {
			get;
			set;
		}
	}

	public class Commando {
		public Commando() {
		}
		[XmlAttribute("Name")]
		public string Name {
			get;
			set;
		}
		[XmlElement("Commandline")]
		public string Commandline {
			get;
			set;
		}
		[XmlElement("Arguments")]
		public string Arguments {
			get;
			set;
		}
		[XmlAttribute("Active")]
		public bool Active{
			get;
			set;
		}
	}
}
