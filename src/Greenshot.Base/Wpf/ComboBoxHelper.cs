/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// Helper to prevent unwanted auto-scrolling / jumping behavior in WPF ComboBox dropdowns
    /// when the mouse moves or hovers over items or boundaries.
    /// </summary>
    public static class ComboBoxHelper
    {
        private static bool _isInitialized;

        static ComboBoxHelper()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }
            _isInitialized = true;

            EventManager.RegisterClassHandler(
                typeof(ComboBoxItem),
                FrameworkElement.RequestBringIntoViewEvent,
                new RequestBringIntoViewEventHandler(OnComboBoxItemRequestBringIntoView));

            EventManager.RegisterClassHandler(
                typeof(ItemsPresenter),
                FrameworkElement.RequestBringIntoViewEvent,
                new RequestBringIntoViewEventHandler(OnItemsPresenterRequestBringIntoView));
        }

        private static void OnComboBoxItemRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // If the bring-into-view was triggered by keyboard navigation, allow it to scroll
            if (IsKeyboardNavigationActive())
            {
                return;
            }

            // Suppress auto-scrolling when hovering near edges or moving mouse over items
            e.Handled = true;
        }

        private static void OnItemsPresenterRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            if (sender is ItemsPresenter ip && ip.TemplatedParent is ComboBox)
            {
                if (IsKeyboardNavigationActive())
                {
                    return;
                }

                e.Handled = true;
            }
        }

        private static bool IsKeyboardNavigationActive()
        {
            if (Keyboard.IsKeyDown(Key.Down) ||
                Keyboard.IsKeyDown(Key.Up) ||
                Keyboard.IsKeyDown(Key.PageDown) ||
                Keyboard.IsKeyDown(Key.PageUp) ||
                Keyboard.IsKeyDown(Key.Home) ||
                Keyboard.IsKeyDown(Key.End) ||
                Keyboard.IsKeyDown(Key.Tab) ||
                Keyboard.IsKeyDown(Key.Left) ||
                Keyboard.IsKeyDown(Key.Right) ||
                Keyboard.IsKeyDown(Key.Enter) ||
                Keyboard.IsKeyDown(Key.Escape))
            {
                return true;
            }

            // Also check alphanumeric keys used for quick lookup / text search in dropdowns
            for (Key k = Key.A; k <= Key.Z; k++)
            {
                if (Keyboard.IsKeyDown(k))
                {
                    return true;
                }
            }
            for (Key k = Key.D0; k <= Key.D9; k++)
            {
                if (Keyboard.IsKeyDown(k))
                {
                    return true;
                }
            }
            for (Key k = Key.NumPad0; k <= Key.NumPad9; k++)
            {
                if (Keyboard.IsKeyDown(k))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
