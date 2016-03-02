/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using System.ComponentModel;
using System.Windows;

namespace Greenshot.Addon.Confluence.Support
{
	public class TranslationData : IWeakEventListener, INotifyPropertyChanged
	{
		#region Private Members

		private readonly string _key;

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="TranslationData"/> class.
		/// </summary>
		/// <param name="key">The key.</param>
		public TranslationData(string key)
		{
			_key = key;
			LanguageChangedEventManager.AddListener(TranslationManager.Instance, this);
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="TranslationData"/> is reclaimed by garbage collection.
		/// </summary>
		~TranslationData()
		{
			LanguageChangedEventManager.RemoveListener(TranslationManager.Instance, this);
		}

		public object Value
		{
			get
			{
				return TranslationManager.Instance.Translate(_key);
			}
		}

		#region IWeakEventListener Members

		public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			if (managerType == typeof (LanguageChangedEventManager))
			{
				OnLanguageChanged(sender, e);
				return true;
			}
			return false;
		}

		private void OnLanguageChanged(object sender, EventArgs e)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs("Value"));
			}
		}

		#endregion

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}