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
using System.Threading;
using System.Windows.Threading;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.WpfEditor.Tools;
using Greenshot.WpfEditor.ViewModels;

namespace Greenshot.Tests.WpfEditor
{
    internal static class WpfEditorTestHelper
    {
        public static readonly Resource Capture = new(Guid.NewGuid(), "image/png", new byte[] { 1 }) { Width = 400, Height = 300 };

        public static GreenshotDocument NewDocument() => DocumentEdits.FromImage(Capture, 400, 300);

        public static ToolPointer At(double x, double y, bool shift = false, int clicks = 1) => new(new PointD(x, y), shift, false, false, 1, clicks);

        /// <summary>
        /// Drags with the tool of the tab from one point to the other
        /// </summary>
        public static void Drag(DocumentTabViewModel tab, double fromX, double fromY, double toX, double toY, bool shift = false)
        {
            tab.Tool.Down(At(fromX, fromY, shift));
            tab.Tool.Move(At((fromX + toX) / 2, (fromY + toY) / 2, shift));
            tab.Tool.Move(At(toX, toY, shift));
            tab.Tool.Up(At(toX, toY, shift));
        }

        /// <summary>
        /// The tab has a WPF bitmap, which needs an STA thread
        /// </summary>
        public static void RunOnSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null)
            {
                throw new Exception("Failed on the STA thread", failure);
            }
        }
    }
}
