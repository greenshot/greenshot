//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Runtime.Serialization;

#endregion

namespace Greenshot.Addon.Dropbox.Entities
{
	[DataContract]
	internal class SharingInfo
	{
		[DataMember(Name = "modified_by")]
		public string ModifiedBy { get; set; }

		[DataMember(Name = "parent_shared_folder_id")]
		public string ParentSharedFolderId { get; set; }

		[DataMember(Name = "read_only")]
		public bool ReadOnly { get; set; }
	}

	[DataContract]
	internal class UploadReply
	{
		[DataMember(Name = "client_modified")]
		public DateTimeOffset ClientModified { get; set; }

		[DataMember(Name = "id")]
		public string Id { get; set; }

		[DataMember(Name = "name")]
		public string Name { get; set; }

		[DataMember(Name = "path_display")]
		public string PathDisplay { get; set; }

		[DataMember(Name = "path_lower")]
		public string PathLower { get; set; }

		[DataMember(Name = "rev")]
		public string Rev { get; set; }

		[DataMember(Name = "server_modified")]
		public DateTimeOffset ServerModified { get; set; }

		[DataMember(Name = "sharing_info")]
		public SharingInfo SharingInfo { get; set; }

		[DataMember(Name = "size")]
		public long Size { get; set; }
	}
}