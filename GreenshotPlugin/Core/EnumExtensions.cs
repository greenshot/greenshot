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

using System;

#endregion

namespace GreenshotPlugin.Core
{
	public static class EnumerationExtensions
	{
		public static bool Has<T>(this Enum type, T value)
		{
			var underlyingType = Enum.GetUnderlyingType(value.GetType());
			try
			{
				if (underlyingType == typeof(int))
				{
					return ((int) (object) type & (int) (object) value) == (int) (object) value;
				}
				if (underlyingType == typeof(uint))
				{
					return ((uint) (object) type & (uint) (object) value) == (uint) (object) value;
				}
			}
			catch
			{
				// ignored
			}
			return false;
		}

		public static bool Is<T>(this Enum type, T value)
		{
			var underlyingType = Enum.GetUnderlyingType(value.GetType());
			try
			{
				if (underlyingType == typeof(int))
				{
					return (int) (object) type == (int) (object) value;
				}
				if (underlyingType == typeof(uint))
				{
					return (uint) (object) type == (uint) (object) value;
				}
			}
			catch
			{
				// ignored
			}
			return false;
		}

		/// <summary>
		///     Add a flag to an enum
		/// </summary>
		/// <param name="type"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static T Add<T>(this Enum type, T value)
		{
			var underlyingType = Enum.GetUnderlyingType(value.GetType());
			try
			{
				if (underlyingType == typeof(int))
				{
					return (T) (object) ((int) (object) type | (int) (object) value);
				}
				if (underlyingType == typeof(uint))
				{
					return (T) (object) ((uint) (object) type | (uint) (object) value);
				}
			}
			catch (Exception ex)
			{
				throw new ArgumentException($"Could not append value '{value}' to enumerated type '{typeof(T).Name}'.", ex);
			}
			throw new ArgumentException($"Could not append value '{value}' to enumerated type '{typeof(T).Name}'.");
		}

		/// <summary>
		///     Remove a flag from an enum type
		/// </summary>
		/// <param name="type"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static T Remove<T>(this Enum type, T value)
		{
			var underlyingType = Enum.GetUnderlyingType(value.GetType());
			try
			{
				if (underlyingType == typeof(int))
				{
					return (T) (object) ((int) (object) type & ~(int) (object) value);
				}
				if (underlyingType == typeof(uint))
				{
					return (T) (object) ((uint) (object) type & ~(uint) (object) value);
				}
			}
			catch (Exception ex)
			{
				throw new ArgumentException($"Could not remove value '{value}' from enumerated type '{typeof(T).Name}'.", ex);
			}
			throw new ArgumentException($"Could not remove value '{value}' from enumerated type '{typeof(T).Name}'.");
		}
	}
}