/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Greenshot.Editor.Standalone.Diagnostics;


namespace Greenshot.Editor.Standalone.ViewModels
{
    /// <summary>
    /// A command of a view model. <see cref="RequeryAll"/> asks every command again whether it can run, the view models call
    /// it when the selection, the tab or the document changed.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private static readonly List<WeakReference<RelayCommand>> Commands = new();
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null) : this(_ => execute(), canExecute == null ? null : _ => canExecute())
        {
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            lock (Commands)
            {
                Commands.Add(new WeakReference<RelayCommand>(this));
            }
        }

        /// <summary>
        /// A command which runs asynchronous work; what it throws is logged and given to <paramref name="failed"/>
        /// </summary>
        public static RelayCommand ForTask(Func<object, Task> execute, Func<object, bool> canExecute = null, Action<Exception> failed = null) =>
            new(parameter => execute(parameter).Forget("An editor command", failed), canExecute);

        public static RelayCommand ForTask(Func<Task> execute, Func<bool> canExecute = null, Action<Exception> failed = null) =>
            new(_ => execute().Forget("An editor command", failed), canExecute == null ? null : _ => canExecute());

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                _execute(parameter);
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        public static void RequeryAll()
        {
            RelayCommand[] alive;
            lock (Commands)
            {
                Commands.RemoveAll(reference => !reference.TryGetTarget(out _));
                alive = Commands.ConvertAll(reference => reference.TryGetTarget(out var command) ? command : null).ToArray();
            }

            foreach (var command in alive)
            {
                command?.RaiseCanExecuteChanged();
            }
        }
    }
}
