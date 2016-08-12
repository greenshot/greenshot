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
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Caliburn.Micro;

namespace Greenshot.Addon.Interfaces.Destination
{
	/// <summary>
	/// A simple base implementation for the IDestination
	/// </summary>
	public abstract class AbstractDestination : PropertyChangedBase, IDestination, IExportContext
	{
		private string _text;
		private string _shortcut;
		private bool _isEnabled = true;
		private Control _icon;

		/// <summary>
		/// Override, this will be called when all imports are available.
		/// </summary>
		protected virtual void Initialize()
		{
		}

		/// <summary>
		/// This will be called before the item is shown, so it can update it's children etc.
		/// </summary>
		/// <param name="caller">IExportContext</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task</returns>
		public virtual async Task RefreshAsync(IExportContext caller, CancellationToken cancellationToken = default(CancellationToken))
		{
			await Task.Yield();
		}

		/// <summary>
		/// This is the technical name of the destination, used for excluding or storing the configuration
		/// </summary>
		public virtual string Designation
		{
			get;
			protected set;
		}

		/// <summary>
		/// If the entry needs a shortcut in the destination picker, it can be set with this value
		/// </summary>
		public virtual string Shortcut
		{
			get
			{
				return _shortcut;
			}
			protected set
			{
				if (value != _shortcut)
				{
					NotifyOfPropertyChange(nameof(Shortcut));
					_shortcut = value;
				}
			}
		}

		/// <summary>
		/// This is the name of the destination in the settings and destination picker
		/// </summary>
		public virtual string Text
		{
			get
			{
				return _text;
			}
			protected set
			{
				if (value != _text)
				{
					_text = value;
					NotifyOfPropertyChange(nameof(Text));
				}
			}
		}

		/// <summary>
		/// When set to false, the entry is disabled in the destination picker
		/// </summary>
		public virtual bool IsEnabled
		{
			get
			{
				return _isEnabled;
			}
			protected set
			{
				if (value != _isEnabled)
				{
					_isEnabled = value;
					NotifyOfPropertyChange(nameof(IsEnabled));
				}
			}
		}

		/// <summary>
		/// This is the icon which is shown everywhere where the destination can be seen.
		/// Two known locations are the settings and the destination picker.
		/// </summary>
		public virtual Control Icon
		{
			get
			{
				return _icon;
			}
			protected set
			{
				if (!Equals(value, _icon))
				{
					_icon = value;
					NotifyOfPropertyChange(nameof(Icon));
				}
			}
		}

		/// <summary>
		/// Export a capture
		/// </summary>
		public virtual Func<IExportContext, ICapture, CancellationToken, Task<INotification>> Export
		{
			get;
			protected set;
		}

		/// <summary>
		/// This is a collection of child destinations, shown in the destination picker
		/// </summary>
		public virtual ObservableCollection<IDestination> Children
		{
			get;
			protected set;
		} = new ObservableCollection<IDestination>();

		/// <summary>
		/// This is the Windows handle of the caller
		/// Used by some exports
		/// </summary>
		public IntPtr Handle
		{
			get;
			set;
		} = IntPtr.Zero;

		/// <summary>
		/// the Progress interface can be used to present a progress bar
		/// </summary>
		public IProgress<int> Progress
		{
			get;
			set;
		}
	}
}
