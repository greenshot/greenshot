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

using System.Collections;

namespace Greenshot.Helpers.Mapi
{
    /// <summary>
    ///     Represents a colleciton of recipients for a mail message.
    /// </summary>
    public class RecipientCollection : CollectionBase
	{
		/// <summary>
		///     Returns the recipient stored in this collection at the specified index.
		/// </summary>
		public Recipient this[int index] => (Recipient) List[index];

		/// <summary>
		///     Adds the specified recipient to this collection.
		/// </summary>
		public void Add(Recipient value)
		{
			List.Add(value);
		}

		/// <summary>
		///     Adds a new recipient with the specified address to this collection.
		/// </summary>
		public void Add(string address)
		{
			Add(new Recipient(address));
		}

		/// <summary>
		///     Adds a new recipient with the specified address and display name to this collection.
		/// </summary>
		public void Add(string address, string displayName)
		{
			Add(new Recipient(address, displayName));
		}

		/// <summary>
		///     Adds a new recipient with the specified address and recipient type to this collection.
		/// </summary>
		public void Add(string address, RecipientType recipientType)
		{
			Add(new Recipient(address, recipientType));
		}

		/// <summary>
		///     Adds a new recipient with the specified address, display name and recipient type to this collection.
		/// </summary>
		public void Add(string address, string displayName, RecipientType recipientType)
		{
			Add(new Recipient(address, displayName, recipientType));
		}

		internal InteropRecipientCollection GetInteropRepresentation()
		{
			return new InteropRecipientCollection(this);
		}
	}
}