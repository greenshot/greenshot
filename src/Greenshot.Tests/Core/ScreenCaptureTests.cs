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
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Capturing;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core;

/// <summary>
/// The order in which ScreenCapture tries its backends
/// </summary>
public class ScreenCaptureTests
{
    private sealed class FakeBackend : IScreenCaptureBackend
    {
        private readonly Func<Bitmap> _capture;

        public FakeBackend(string name, Func<Bitmap> capture, bool isAvailable = true)
        {
            Name = name;
            _capture = capture;
            IsAvailable = isAvailable;
        }

        public string Name { get; }
        public bool IsAvailable { get; }
        public bool CapturesWindowContentOnly => false;
        public int Calls { get; private set; }

        public Task<Bitmap> CaptureRectangleAsync(NativeRect captureBounds, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_capture());
        }

        public Task<Bitmap> CaptureWindowAsync(WindowDetails window, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_capture());
        }
    }

    private static Task<ScreenCaptureResult> CaptureAsync(IReadOnlyList<IScreenCaptureBackend> backends, CancellationToken cancellationToken = default)
    {
        return ScreenCapture.CaptureAsync(backends, backend => backend.CaptureRectangleAsync(new NativeRect(10, 20, 2, 2), cancellationToken), () => new NativePoint(10, 20), "test area", cancellationToken);
    }

    [Fact]
    public async Task FirstBackendWhichDelivers_IsUsed()
    {
        var unavailable = new FakeBackend("unavailable", () => new Bitmap(1, 1), isAvailable: false);
        var first = new FakeBackend("first", () => new Bitmap(2, 2));
        var second = new FakeBackend("second", () => new Bitmap(3, 3));

        var result = await CaptureAsync(new IScreenCaptureBackend[] { unavailable, first, second });

        using (result.Image)
        {
            Assert.Same(first, result.Backend);
            Assert.Equal(new NativePoint(10, 20), result.Location);
            Assert.Equal(0, unavailable.Calls);
            Assert.Equal(0, second.Calls);
        }
    }

    [Fact]
    public async Task FailingOrEmptyBackend_FallsBackToTheNext()
    {
        var failing = new FakeBackend("failing", () => throw new InvalidOperationException("no device"));
        var empty = new FakeBackend("empty", () => null);
        var fallback = new FakeBackend("fallback", () => new Bitmap(2, 2));

        var result = await CaptureAsync(new IScreenCaptureBackend[] { failing, empty, fallback });

        using (result.Image)
        {
            Assert.Same(fallback, result.Backend);
        }
    }

    [Fact]
    public async Task AllBackendsFailing_Throws()
    {
        var failing = new FakeBackend("failing", () => throw new InvalidOperationException("no device"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CaptureAsync(new IScreenCaptureBackend[] { failing }));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task NoBackendDelivering_ReturnsNull()
    {
        var empty = new FakeBackend("empty", () => null);

        Assert.Null(await CaptureAsync(new IScreenCaptureBackend[] { empty }));
    }

    [Fact]
    public async Task Cancellation_IsNotSwallowed()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancelling = new FakeBackend("cancelling", () =>
        {
            cancellationTokenSource.Cancel();
            throw new OperationCanceledException(cancellationTokenSource.Token);
        });
        var fallback = new FakeBackend("fallback", () => new Bitmap(2, 2));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CaptureAsync(new IScreenCaptureBackend[] { cancelling, fallback }, cancellationTokenSource.Token));
        Assert.Equal(0, fallback.Calls);
    }

    [Fact]
    public void DefaultBackends_AreGraphicsCaptureThenGdi()
    {
        Assert.Collection(ScreenCapture.Backends,
            backend => Assert.Equal(GraphicsCaptureBackend.BackendName, backend.Name),
            backend => Assert.Equal(GdiCaptureBackend.BackendName, backend.Name));
    }
}
