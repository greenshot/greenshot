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
using System.Windows;

namespace GreenshotConfluencePlugin.Support
{
	public class LanguageChangedEventManager : WeakEventManager
	{
		public static void AddListener(TranslationManager source, IWeakEventListener listener)
		{
			CurrentManager.ProtectedAddListener(source, listener);
		}

		public static void RemoveListener(TranslationManager source, IWeakEventListener listener)
		{
			CurrentManager.ProtectedRemoveListener(source, listener);
		}

		private void OnLanguageChanged(object sender, EventArgs e)
		{
			DeliverEvent(sender, e);
		}

		protected override void StartListening(object source)
		{
			var manager = (TranslationManager) source;
			manager.LanguageChanged += OnLanguageChanged;
		}

		protected override void StopListening(Object source)
		{
			var manager = (TranslationManager) source;
			manager.LanguageChanged -= OnLanguageChanged;
		}

		private static LanguageChangedEventManager CurrentManager
		{
			get
			{
				Type managerType = typeof (LanguageChangedEventManager);
				var manager = (LanguageChangedEventManager) GetCurrentManager(managerType);
				if (manager == null)
				{
					manager = new LanguageChangedEventManager();
					SetCurrentManager(managerType, manager);
				}
				return manager;
			}
		}
	}
}