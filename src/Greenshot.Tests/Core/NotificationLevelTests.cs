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

using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// Which notifications a level shows
    /// </summary>
    public class NotificationLevelTests
    {
        [Theory]
        [InlineData(NotificationLevel.All, NotificationKind.Info, true)]
        [InlineData(NotificationLevel.All, NotificationKind.Warning, true)]
        [InlineData(NotificationLevel.All, NotificationKind.Error, true)]
        [InlineData(NotificationLevel.Warnings, NotificationKind.Info, false)]
        [InlineData(NotificationLevel.Warnings, NotificationKind.Warning, true)]
        [InlineData(NotificationLevel.Warnings, NotificationKind.Error, true)]
        [InlineData(NotificationLevel.Errors, NotificationKind.Info, false)]
        [InlineData(NotificationLevel.Errors, NotificationKind.Warning, false)]
        [InlineData(NotificationLevel.Errors, NotificationKind.Error, true)]
        [InlineData(NotificationLevel.None, NotificationKind.Info, false)]
        [InlineData(NotificationLevel.None, NotificationKind.Warning, false)]
        [InlineData(NotificationLevel.None, NotificationKind.Error, false)]
        public void Shows_ByKind(NotificationLevel level, NotificationKind kind, bool expected)
        {
            Assert.Equal(expected, level.Shows(kind));
        }
    }
}
