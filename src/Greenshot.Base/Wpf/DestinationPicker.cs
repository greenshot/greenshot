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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// The destination picker: a themed menu at the cursor with the destinations, the user picks where the capture goes.
    /// It stays open when the user clicks somewhere else, Escape or "Close" decline.
    /// </summary>
    public static class DestinationPicker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DestinationPicker));

        /// <summary>
        /// Show the picker and return the destination the user clicked, null when the user closed it.
        /// Runs on the UI thread, completes when the menu closes.
        /// </summary>
        public static Task<IDestination> ShowAsync(IReadOnlyList<IDestination> destinations, ICaptureDetails captureDetails, CancellationToken cancellationToken = default)
        {
            ThreadAssert.IsUi(nameof(ShowAsync));
            var picked = Tcs.Create<IDestination>();
            var menu = ThemedMenu.CreateContextMenu();
            // A click somewhere else doesn't decline, the user might want to look at something first
            menu.StaysOpen = true;

            void Close()
            {
                // Not while WPF is still handling the click of the item
                UiDispatcher.Current.InvokeAsync(() => menu.IsOpen = false, CancellationToken.None).FireAndLog("Close the destination picker", Log);
            }

            void Complete(IDestination destination)
            {
                if (picked.TrySetResult(destination))
                {
                    Close();
                }
            }

            var registration = cancellationToken.Register(() =>
            {
                if (picked.TrySetCanceled(cancellationToken))
                {
                    Close();
                }
            });
            _ = picked.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);

            // Closed in any other way (e.g. Escape): declined. WPF raises the Click of an item only after the menu closed and rendered,
            // so the decline waits until after that.
            menu.Closed += (_, _) => _ = menu.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => picked.TrySetResult(null)));
            menu.KeyDown += (_, args) =>
            {
                // Only reaches the menu when no sub menu used it
                if (args.Key == Key.Escape)
                {
                    args.Handled = true;
                    Complete(null);
                }
            };

            foreach (var destination in destinations.Where(d => d != null).OrderBy(d => d, DestinationComparer.Instance))
            {
                if (!destination.IsAvailableFor(captureDetails))
                {
                    continue;
                }

                menu.Items.Add(CreateItem(destination, captureDetails, Complete, true));
            }

            menu.Items.Add(ThemedMenu.CreateSeparator());
            var closeItem = ThemedMenu.CreateItem(Texts.Editor.Close, null, () => Complete(null));
            ThemedMenu.AssignIcon(closeItem, DestinationIcons.Resource("Close.Image"));
            menu.Items.Add(closeItem);

            ThemedMenu.ShowAtCursor(menu);
            return picked.Task;
        }

        /// <summary>
        /// Create the item for the destination, the dynamic destinations (e.g. the open documents) are loaded right away and become sub items when they arrive.
        /// </summary>
        /// <param name="destination">IDestination</param>
        /// <param name="captureDetails">The capture the menu is for, null when there is none</param>
        /// <param name="onClick">Called with the clicked (sub) destination</param>
        /// <param name="addDynamics">Add the dynamic destinations</param>
        public static MenuItem CreateItem(IDestination destination, ICaptureDetails captureDetails, Action<IDestination> onClick, bool addDynamics)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            var descriptor = destination.Descriptor;
            var target = new DestinationTarget(destination);
            var item = ThemedMenu.CreateItem(descriptor.DisplayName, null, () => onClick?.Invoke(target.Destination));
            ThemedMenu.AssignIcon(item, descriptor.IconKey);

            if (descriptor.HasDynamicDestinations && addDynamics)
            {
                // Until the dynamic destinations arrive a placeholder shows that there is more
                var placeholder = ThemedMenu.CreateItem("…");
                placeholder.IsEnabled = false;
                item.Items.Add(placeholder);
                AddDynamicDestinationsAsync(item, target, captureDetails, onClick).FireAndLog($"Dynamic destinations of {destination.Designation}", Log);
            }

            return item;
        }

        private static async Task AddDynamicDestinationsAsync(MenuItem item, DestinationTarget target, ICaptureDetails captureDetails, Action<IDestination> onClick)
        {
            var ui = UiDispatcher.Current;
            var destination = target.Destination;
            var subDestinations = await DestinationHelper.LoadDynamicDestinationsAsync(destination, captureDetails).ConfigureAwait(false);
            await ui.InvokeAsync(() =>
            {
                item.Items.Clear();
                var validSubDestinations = subDestinations.Where(d => d != null).OrderBy(d => d, DestinationComparer.Instance).ToList();
                if (validSubDestinations.Count == 0)
                {
                    // No dynamic destinations: the item stays a plain item for the destination itself
                    return;
                }

                if (destination.Descriptor.UseDynamicsOnly && validSubDestinations.Count == 1)
                {
                    // Only one: the item itself becomes that destination, without a sub menu
                    target.Destination = validSubDestinations[0];
                    item.Header = validSubDestinations[0].Descriptor.DisplayName;
                    return;
                }

                if (!destination.Descriptor.UseDynamicsOnly)
                {
                    // A sub menu header can't be clicked, the destination itself (e.g. a new document) is the first sub item
                    item.Items.Add(CreateItem(destination, captureDetails, onClick, false));
                    item.Items.Add(ThemedMenu.CreateSeparator());
                }

                foreach (var subDestination in validSubDestinations)
                {
                    item.Items.Add(CreateItem(subDestination, captureDetails, onClick, false));
                }
            }, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// The destination an item exports to, it changes when a destination with only dynamic destinations has exactly one
        /// </summary>
        private sealed class DestinationTarget
        {
            public DestinationTarget(IDestination destination)
            {
                Destination = destination;
            }

            public IDestination Destination { get; set; }
        }
    }
}
