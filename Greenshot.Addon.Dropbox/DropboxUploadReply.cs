/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom,
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
using System.Runtime.Serialization;

namespace Greenshot.Addon.Dropbox
{
	[DataContract]
	internal class SharingInfo
	{
		[DataMember(Name = "read_only")]
		public bool ReadOnly { get; set; }
		[DataMember(Name = "parent_shared_folder_id")]
		public string ParentSharedFolderId { get; set; }
		[DataMember(Name = "modified_by")]
		public string ModifiedBy { get; set; }
	}

	internal class DropboxUploadReply
	{
		[DataMember(Name = "name")]
		public string Name { get; set; }

		[DataMember(Name = "path_lower")]
		public string PathLower { get; set; }

		[DataMember(Name = "path_display")]
		public string PathDisplay { get; set; }

		[DataMember(Name = "id")]
		public string Id { get; set; }

		[DataMember(Name = "client_modified")]
		public DateTimeOffset ClientModified { get; set; }

		[DataMember(Name = "server_modified")]
		public DateTimeOffset ServerModified { get; set; }

		[DataMember(Name = "rev")]
		public string Rev { get; set; }

		[DataMember(Name = "size")]
		public long Size { get; set; }

		[DataMember(Name = "sharing_info")]
		public SharingInfo SharingInfo { get; set; }
	}
}
