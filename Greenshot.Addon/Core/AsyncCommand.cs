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
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace Greenshot.Addon.Core
{
	public class AsyncCommand : ICommand
	{
		private readonly Predicate<object> _canExecute;
		private readonly Func<object, Task> _asyncExecute;
		private readonly bool _allowMultipleExecutionsAtOnce;

		private bool _isExecuting;

		public event EventHandler CanExecuteChanged;

		public AsyncCommand(Func<object, Task> execute)
		: this(execute, null, false)
		{
		}

		public AsyncCommand(Func<object, Task> execute, bool allowMultipleExecutionsAtOnce = false)
		: this(execute, null, allowMultipleExecutionsAtOnce)
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="asyncExecute">Function for the execute</param>
		/// <param name="canExecute">Predicate for the canExecute</param>
		/// <param name="allowMultipleExecutionsAtOnce">Whether or not this command can be executed several times in a row, or if it must wait until the previous command has been executed. Defaults to false.</param>
		public AsyncCommand(Func<object, Task> asyncExecute, Predicate<object> canExecute, bool allowMultipleExecutionsAtOnce = false)
		{
			_asyncExecute = asyncExecute;
			_canExecute = canExecute;
			_allowMultipleExecutionsAtOnce = allowMultipleExecutionsAtOnce;
		}

		public bool CanExecute(object parameter)
		{
			if (!_allowMultipleExecutionsAtOnce && _isExecuting)
			{
				return false;
			}

			if (_canExecute == null)
			{
				return true;
			}

			return _canExecute(parameter);
		}

		public async void Execute(object parameter)
		{
			_isExecuting = true;
			UpdateCanExecuteState();
			await ExecuteAsync(parameter);
			_isExecuting = false;
			UpdateCanExecuteState();
		}

		protected virtual async Task ExecuteAsync(object parameter)
		{
			if (_asyncExecute != null)
			{
				await _asyncExecute(parameter);
			}
		}

		public void UpdateCanExecuteState()
		{
			if (CanExecuteChanged != null)
			{
				Dispatcher.CurrentDispatcher.BeginInvoke((Action)(() => CanExecuteChanged(this, new EventArgs())));
			}
		}
	}

}
