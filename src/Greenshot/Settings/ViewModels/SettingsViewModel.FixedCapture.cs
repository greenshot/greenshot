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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Languages;
using Greenshot.Capturing.Tools;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The fixed size capture on the Capture tab: its sizes (with a place), what it remembers, and whether it moves the mouse
    /// </summary>
    public partial class SettingsViewModel
    {
        private IList<FixedCaptureRememberChoice> _fixedCaptureRememberChoices;

        /// <summary>
        /// The sizes of the fixed size capture, in the order the number keys pick them, written back on save
        /// </summary>
        public ObservableCollection<FixedCaptureSizeItem> FixedCaptureSizes { get; private set; }

        /// <summary>
        /// The recent captures which aren't in the own sizes, to keep one of them
        /// </summary>
        public ObservableCollection<FixedCaptureSizeItem> RecentFixedCaptureSizes { get; private set; }

        public bool HasRecentFixedCaptureSizes => RecentFixedCaptureSizes.Count > 0;

        private void InitializeFixedCaptureSizes()
        {
            var sizes = FixedSizeCaptureTool.GetSizes(CoreConfiguration.FixedCaptureSizes, CoreConfiguration.RecentCaptureAreas);
            FixedCaptureSizes = new ObservableCollection<FixedCaptureSizeItem>(sizes.Where(entry => !entry.Recent).Select(entry => new FixedCaptureSizeItem(entry.Area)));
            RecentFixedCaptureSizes = new ObservableCollection<FixedCaptureSizeItem>(sizes.Where(entry => entry.Recent).Select(entry => new FixedCaptureSizeItem(entry.Area)));
        }

        /// <summary>
        /// A recent capture becomes one of the own sizes, at the end, written on save like the others
        /// </summary>
        public void KeepRecentFixedCaptureSize(FixedCaptureSizeItem item)
        {
            if (item == null || !RecentFixedCaptureSizes.Remove(item))
            {
                return;
            }
            AddFixedCaptureSize(item.Area);
            OnPropertyChanged(nameof(HasRecentFixedCaptureSizes));
        }

        /// <summary>
        /// Adds an entry at the end; false when it has no size or is already there
        /// </summary>
        public bool AddFixedCaptureSize(FixedCaptureArea area)
        {
            if (area.IsEmpty || FixedCaptureSizes.Any(item => item.Area == area))
            {
                return false;
            }
            FixedCaptureSizes.Add(new FixedCaptureSizeItem(area));
            return true;
        }

        /// <summary>
        /// Changes an entry in its place; when the new one is already in the list the changed one goes
        /// </summary>
        public void ChangeFixedCaptureSize(FixedCaptureSizeItem item, FixedCaptureArea area)
        {
            int index = FixedCaptureSizes.IndexOf(item);
            if (index < 0 || area.IsEmpty || item.Area == area)
            {
                return;
            }
            if (FixedCaptureSizes.Any(other => other.Area == area))
            {
                FixedCaptureSizes.RemoveAt(index);
                return;
            }
            FixedCaptureSizes[index] = new FixedCaptureSizeItem(area);
        }

        /// <summary>
        /// Writes the sizes of the fixed size capture back to the configuration
        /// </summary>
        public void SaveFixedCaptureSizes()
        {
            if (!CoreConfiguration.IsConstant(nameof(ICoreConfiguration.FixedCaptureSizes)))
            {
                CoreConfiguration.FixedCaptureSizes = FixedCaptureSizes.Select(item => item.Area.ToString()).ToList();
            }
        }

        /// <summary>
        /// The choices of the drop-down of what the fixed size capture remembers
        /// </summary>
        public IList<FixedCaptureRememberChoice> FixedCaptureRememberChoices => _fixedCaptureRememberChoices ??= new List<FixedCaptureRememberChoice>
        {
            new FixedCaptureRememberChoice(FixedCaptureRemember.Size, Texts.Settings.FixedCaptureRemembersSize),
            new FixedCaptureRememberChoice(FixedCaptureRemember.Screen, Texts.Settings.FixedCaptureRemembersScreen),
            new FixedCaptureRememberChoice(FixedCaptureRemember.Window, Texts.Settings.FixedCaptureRemembersWindow)
        };

        /// <summary>
        /// What a place of a new size is relative to: the main screen, each other screen which is connected now, or a program's window
        /// </summary>
        /// <param name="monitor">The screen of an entry which is changed, it is offered also when it isn't connected now</param>
        public static IList<FixedCapturePlaceChoice> GetFixedCapturePlaceChoices(NativePoint? monitor = null)
        {
            var choices = new List<FixedCapturePlaceChoice> { new FixedCapturePlaceChoice(Texts.Settings.FixedCaptureSizesMainScreen, null, false) };
            var displays = DisplayInfo.AllDisplayInfos;
            for (int i = 0; i < displays.Length; i++)
            {
                var origin = displays[i].Bounds.Location;
                if (origin.X == 0 && origin.Y == 0)
                {
                    continue;
                }
                string name = string.Format(CultureInfo.CurrentCulture, "{0} ({1} × {2})",
                    FormatScreen(i + 1), displays[i].Bounds.Width, displays[i].Bounds.Height);
                choices.Add(new FixedCapturePlaceChoice(name, origin, false));
            }
            if (monitor is { } entryMonitor && !choices.Any(choice => choice.Monitor == entryMonitor))
            {
                choices.Add(new FixedCapturePlaceChoice(FormatScreen(string.Format(CultureInfo.CurrentCulture, "({0}, {1})", entryMonitor.X, entryMonitor.Y)), entryMonitor, false));
            }
            choices.Add(new FixedCapturePlaceChoice(Texts.Settings.FixedCaptureSizesProgram, null, true));
            return choices;
        }

        /// <summary>
        /// The programs (e.g. notepad.exe) of the open application windows, to pick one for a place in its windows
        /// </summary>
        public static IList<string> GetProgramsOfOpenWindows() =>
            WindowHelper.GetTopLevelWindows()
                .Select(window => window.GetProcessName())
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name + ".exe")
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, System.StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static string FormatScreen(object number)
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, Texts.Settings.FixedCaptureSizesScreen, number);
            }
            catch (System.FormatException)
            {
                return System.Convert.ToString(number, CultureInfo.CurrentCulture);
            }
        }
    }

    /// <summary>
    /// One size of the fixed size capture in the settings, with its text
    /// </summary>
    public sealed class FixedCaptureSizeItem
    {
        public FixedCaptureSizeItem(FixedCaptureArea area)
        {
            Area = area;
        }

        public FixedCaptureArea Area { get; }

        public string DisplayText => FixedSizeCaptureTool.GetDisplayText(Area);
    }

    /// <summary>
    /// One choice of the drop-down of what the fixed size capture remembers
    /// </summary>
    public sealed class FixedCaptureRememberChoice
    {
        public FixedCaptureRememberChoice(FixedCaptureRemember value, string displayName)
        {
            Value = value;
            DisplayName = displayName;
        }

        public FixedCaptureRemember Value { get; }

        public string DisplayName { get; }
    }

    /// <summary>
    /// What the place of a new size is relative to
    /// </summary>
    public sealed class FixedCapturePlaceChoice
    {
        public FixedCapturePlaceChoice(string displayName, NativePoint? monitor, bool isProgram)
        {
            DisplayName = displayName;
            Monitor = monitor;
            IsProgram = isProgram;
        }

        public string DisplayName { get; }

        /// <summary>
        /// The top left corner of the screen, null for the main screen or a program
        /// </summary>
        public NativePoint? Monitor { get; }

        public bool IsProgram { get; }
    }
}
