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

namespace GreenshotPlugin.Interop
{
	/// <summary>
	///     An attribute to specifiy the ProgID of the COM class to create. (As suggested by Kristen Wegner)
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface)]
	public sealed class ComProgIdAttribute : Attribute
	{
		/// <summary>Constructor</summary>
		/// <param name="value">The COM ProgID.</param>
		public ComProgIdAttribute(string value)
		{
			Value = value;
		}

		/// <summary>
		///     Returns the COM ProgID
		/// </summary>
		public string Value { get; }

		/// <summary>
		///     Extracts the attribute from the specified type.
		/// </summary>
		/// <param name="interfaceType">
		///     The interface type.
		/// </param>
		/// <returns>
		///     The <see cref="ComProgIdAttribute" />.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="interfaceType" /> is <see langword="null" />.
		/// </exception>
		public static ComProgIdAttribute GetAttribute(Type interfaceType)
		{
			if (null == interfaceType)
			{
				throw new ArgumentNullException(nameof(interfaceType));
			}

			var attributeType = typeof(ComProgIdAttribute);
			var attributes = interfaceType.GetCustomAttributes(attributeType, false);

			if (0 == attributes.Length)
			{
				var interfaces = interfaceType.GetInterfaces();
				foreach (var t in interfaces)
				{
					interfaceType = t;
					attributes = interfaceType.GetCustomAttributes(attributeType, false);
					if (0 != attributes.Length)
					{
						break;
					}
				}
			}

			if (0 == attributes.Length)
			{
				return null;
			}
			return (ComProgIdAttribute) attributes[0];
		}
	}
}