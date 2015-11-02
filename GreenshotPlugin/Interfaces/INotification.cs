/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

namespace GreenshotPlugin.Interfaces
{
	/// <summary>
	/// Type of notifications
	/// </summary>
	public enum NotificationTypes {
		Cancel,
		Success,
		Fail,
	}

	/// <summary>
	/// Something to differenciate the sources
	/// </summary>
	public enum SourceTypes
	{
		Destination,
		Plugin,
		Service
	}

	/// <summary>
	/// The interface for the notification object
	/// </summary>
	public interface INotification
	{
		/// <summary>
		/// Notification type
		/// </summary>
		NotificationTypes NotificationType
		{
			get;
			set;
		}

		/// <summary>
		/// What is the source type of the notification
		/// </summary>
		SourceTypes SourceType
		{
			get;
			set;
		}

		/// <summary>
		/// What is the source of the notification
		/// </summary>
		string Source
		{
			get;
			set;
		}

		/// <summary>
		/// When was the notification
		/// </summary>
		DateTimeOffset Timestamp
		{
			get;
			set;
		}

		/// <summary>
		/// A translated description of what kind of notification
		/// </summary>
		string Text
		{
			get;
			set;
		}

		/// <summary>
		/// Details for an error
		/// </summary>
		string ErrorText
		{
			get;
			set;
		}

		/// <summary>
		/// When something is exported, this is where it went
		/// </summary>
		Uri ImageLocation
		{
			get;
			set;
		}

		/// <summary>
		/// The location of a thumbnail, if any
		/// </summary>
		Uri ThumbnailLocation
		{
			get;
			set;
		}
	}
}
