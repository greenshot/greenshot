/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GreenshotBoxPlugin {
	[DataContract]
	public class Authorization {
		[DataMember(Name = "access_token")]
		public string AccessToken { get; set; }
		[DataMember(Name = "expires_in")]
		public int ExpiresIn { get; set; }
		[DataMember(Name = "refresh_token")]
		public string RefreshToken { get; set; }
		[DataMember(Name = "token_type")]
		public string TokenType { get; set; }
	}
	[DataContract]
	public class SharedLink {
		[DataMember(Name = "url")]
		public string Url { get; set; }
		[DataMember(Name = "download_url")]
		public string DownloadUrl { get; set; }
	}

	[DataContract]
	public class FileEntry {
		[DataMember(Name = "id")]
		public string Id { get; set; }
		[DataMember(Name = "name")]
		public string Name { get; set; }
		[DataMember(Name = "shared_link")]
		public SharedLink SharedLink { get; set; }
	}

	[DataContract]
	public class Upload {
		[DataMember(Name = "entries")]
		public List<FileEntry> Entries { get; set; }
	}
}
