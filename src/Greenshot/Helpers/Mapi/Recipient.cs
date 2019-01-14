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


#endregion

namespace Greenshot.Helpers.Mapi
{
    #region Public Recipient Class

    /// <summary>
    ///     Represents a Recipient for a MapiMailMessage.
    /// </summary>
    public class Recipient
	{
		#region Internal Methods

		/// <summary>
		///     Returns an interop representation of a recepient.
		/// </summary>
		/// <returns></returns>
		internal MapiRecipDesc GetInteropRepresentation()
		{
			var interop = new MapiRecipDesc();

			if (DisplayName == null)
			{
				interop.Name = Address;
			}
			else
			{
				interop.Name = DisplayName;
				interop.Address = Address;
			}

			interop.RecipientClass = (int) RecipientType;

			return interop;
		}

		#endregion Internal Methods

		#region Public Properties

		/// <summary>
		///     The email address of this recipient.
		/// </summary>
		public string Address;

		/// <summary>
		///     The display name of this recipient.
		/// </summary>
		public string DisplayName;

		/// <summary>
		///     How the recipient will receive this message (To, CC, BCC).
		/// </summary>
		public RecipientType RecipientType = RecipientType.To;

		#endregion Public Properties

		#region Constructors

		/// <summary>
		///     Creates a new recipient with the specified address.
		/// </summary>
		public Recipient(string address)
		{
			Address = address;
		}

		/// <summary>
		///     Creates a new recipient with the specified address and display name.
		/// </summary>
		public Recipient(string address, string displayName)
		{
			Address = address;
			DisplayName = displayName;
		}

		/// <summary>
		///     Creates a new recipient with the specified address and recipient type.
		/// </summary>
		public Recipient(string address, RecipientType recipientType)
		{
			Address = address;
			RecipientType = recipientType;
		}

		/// <summary>
		///     Creates a new recipient with the specified address, display name and recipient type.
		/// </summary>
		public Recipient(string address, string displayName, RecipientType recipientType)
		{
			Address = address;
			DisplayName = displayName;
			RecipientType = recipientType;
		}

		#endregion Constructors
	}

	#endregion Public RecipientCollection Class
}