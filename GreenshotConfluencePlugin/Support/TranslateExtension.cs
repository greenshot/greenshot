#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Windows.Data;
using System.Windows.Markup;

#endregion

namespace TranslationByMarkupExtension
{
	/// <summary>
	///     The Translate Markup extension returns a binding to a TranslationData
	///     that provides a translated resource of the specified key
	/// </summary>
	public class TranslateExtension : MarkupExtension
	{
		#region Private Members

		#endregion

		#region Construction

		/// <summary>
		///     Initializes a new instance of the <see cref="TranslateExtension" /> class.
		/// </summary>
		/// <param name="key">The key.</param>
		public TranslateExtension(string key)
		{
			Key = key;
		}

		#endregion

		[ConstructorArgument("key")]
		public string Key { get; set; }

		/// <summary>
		///     See <see cref="MarkupExtension.ProvideValue" />
		/// </summary>
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			var binding = new Binding("Value")
			{
				Source = new TranslationData(Key)
			};
			return binding.ProvideValue(serviceProvider);
		}
	}
}