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
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The notifications on the Capture tab: which are shown, and whether a toast shows a thumbnail of the capture
    /// </summary>
    public partial class SettingsViewModel
    {
        private IList<NotificationLevelChoice> _notificationLevels;

        /// <summary>
        /// The choices of the notifications drop-down
        /// </summary>
        public IList<NotificationLevelChoice> NotificationLevels => _notificationLevels ??= new List<NotificationLevelChoice>
        {
            new NotificationLevelChoice(NotificationLevel.All, Texts.Settings.NotificationsAll),
            new NotificationLevelChoice(NotificationLevel.Warnings, Texts.Settings.NotificationsWarnings),
            new NotificationLevelChoice(NotificationLevel.Errors, Texts.Settings.NotificationsErrors),
            new NotificationLevelChoice(NotificationLevel.None, Texts.Settings.NotificationsNone)
        };

        /// <summary>
        /// The chosen level: None turns ShowTrayNotification off (the switch of the tray menu) and keeps the level, so the
        /// tray menu turns them on again with it
        /// </summary>
        public NotificationLevel SelectedNotificationLevel
        {
            get => CoreConfiguration.ShowTrayNotification ? CoreConfiguration.NotificationLevel : NotificationLevel.None;
            set
            {
                if (SelectedNotificationLevel == value)
                {
                    return;
                }

                CoreConfiguration.ShowTrayNotification = value != NotificationLevel.None;
                if (value != NotificationLevel.None)
                {
                    CoreConfiguration.NotificationLevel = value;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(AreNotificationsShown));
            }
        }

        /// <summary>
        /// Whether any notification is shown, the thumbnail switch is only enabled then
        /// </summary>
        public bool AreNotificationsShown => SelectedNotificationLevel != NotificationLevel.None;

        /// <summary>
        /// Whether the notifications can show a capture (the toasts of Windows, not the balloons of the tray icon), the thumbnail
        /// switch is only offered then
        /// </summary>
        public bool CanShowNotificationThumbnail => SimpleServiceProvider.Current.GetInstance<INotificationService>(isOptional: true)?.ShowsCaptures == true;
    }

    /// <summary>
    /// One choice of the notifications drop-down
    /// </summary>
    public sealed class NotificationLevelChoice
    {
        public NotificationLevelChoice(NotificationLevel level, string displayName)
        {
            Level = level;
            DisplayName = displayName;
        }

        public NotificationLevel Level { get; }

        public string DisplayName { get; }
    }
}
