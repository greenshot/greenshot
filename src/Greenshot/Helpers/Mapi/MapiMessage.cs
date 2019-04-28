// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Runtime.InteropServices;

namespace Greenshot.Helpers.Mapi
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	internal class MapiMessage
	{
		public string ConversationID = null;
		public string DateReceived = null;
		public int FileCount;
		public IntPtr Files = IntPtr.Zero;
		public int Flags = 0;
		public string MessageType = null;
		public string NoteText;
		public IntPtr Originator = IntPtr.Zero;
		public int RecipientCount;
		public IntPtr Recipients = IntPtr.Zero;
		public int Reserved = 0;
		public string Subject;
	}
}