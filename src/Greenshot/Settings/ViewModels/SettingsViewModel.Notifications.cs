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

using System.Collections.Generic;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Languages;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The notifications on the Capture tab: off, or how much the notification after an export shows.
    /// One drop-down for two settings: ShowTrayNotification (also in the tray menu) and ExportNotificationDetail.
    /// </summary>
    public partial class SettingsViewModel
    {
        private IList<NotificationChoice> _notificationChoices;

        /// <summary>
        /// The choices of the notification drop-down, null is "Off"
        /// </summary>
        public IList<NotificationChoice> NotificationChoices => _notificationChoices ??= new List<NotificationChoice>
        {
            new NotificationChoice(null, Texts.Settings.ShownotifyOff),
            new NotificationChoice(NotificationDetail.ErrorsOnly, Texts.Settings.ShownotifyErrors),
            new NotificationChoice(NotificationDetail.Short, Texts.Settings.ShownotifyShort),
            new NotificationChoice(NotificationDetail.Full, Texts.Settings.ShownotifyFull)
        };

        /// <summary>
        /// The chosen notifications
        /// </summary>
        public NotificationChoice SelectedNotificationChoice
        {
            get
            {
                NotificationDetail? detail = CoreConfiguration.ShowTrayNotification ? CoreConfiguration.ExportNotificationDetail : (NotificationDetail?)null;
                foreach (var choice in NotificationChoices)
                {
                    if (choice.Detail == detail)
                    {
                        return choice;
                    }
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                CoreConfiguration.ShowTrayNotification = value.Detail.HasValue;
                if (value.Detail.HasValue)
                {
                    CoreConfiguration.ExportNotificationDetail = value.Detail.Value;
                }

                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// One choice of the notification drop-down
    /// </summary>
    public sealed class NotificationChoice
    {
        public NotificationChoice(NotificationDetail? detail, string displayName)
        {
            Detail = detail;
            DisplayName = displayName;
        }

        /// <summary>
        /// What the notification after an export shows, null when there are no notifications
        /// </summary>
        public NotificationDetail? Detail { get; }

        public string DisplayName { get; }
    }
}
