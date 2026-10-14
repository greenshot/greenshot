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
using Greenshot.Base.Interfaces;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// Choosing the notifications of the platform: the first available provider by priority
    /// </summary>
    public class NotificationServiceProvidersTests
    {
        [Fact]
        public void CreateService_TakesTheAvailableProviderWithTheHighestPriority()
        {
            var toasts = new FakeService();
            var balloons = new FakeService();

            var service = NotificationServiceProviders.CreateService(new INotificationServiceProvider[]
            {
                new FakeProvider(0, () => balloons),
                new FakeProvider(100, () => toasts)
            });

            Assert.Same(toasts, service);
        }

        [Fact]
        public void CreateService_TriesTheNextProvider_WhenOneIsNotAvailableOrFails()
        {
            var balloons = new FakeService();

            var service = NotificationServiceProviders.CreateService(new INotificationServiceProvider[]
            {
                new FakeProvider(100, () => null),
                new FakeProvider(50, () => throw new InvalidOperationException("No registration")),
                new FakeProvider(0, () => balloons)
            });

            Assert.Same(balloons, service);
        }

        [Fact]
        public void CreateService_IsNull_WhenNoneIsAvailable()
        {
            Assert.Null(NotificationServiceProviders.CreateService(new INotificationServiceProvider[] { new FakeProvider(0, () => null) }));
        }

        private sealed class FakeProvider : INotificationServiceProvider
        {
            private readonly Func<INotificationService> _create;

            public FakeProvider(int priority, Func<INotificationService> create)
            {
                Priority = priority;
                _create = create;
            }

            public int Priority { get; }

            public INotificationService TryCreate() => _create();
        }

        private sealed class FakeService : INotificationService
        {
            public bool ShowsCaptures => false;

            public IReadOnlyCollection<string> GetShownCaptureIds() => Array.Empty<string>();

            public event EventHandler<NotificationActivatedEventArgs> CaptureActivated
            {
                add { }
                remove { }
            }

            public void ShowNotification(Notification notification)
            {
            }

            public void ShowWarningMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
            {
            }

            public void ShowErrorMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
            {
            }

            public void ShowInfoMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
            {
            }
        }
    }
}
