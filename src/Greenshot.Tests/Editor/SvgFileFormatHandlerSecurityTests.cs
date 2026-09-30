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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormatHandlers;
using Svg;
using Xunit;

namespace Greenshot.Tests.Editor
{
    public class SvgFileFormatHandlerSecurityTests
    {
        public SvgFileFormatHandlerSecurityTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void SvgFileFormatHandler_StaticInitialization_DisablesExternalImagesAndElements()
        {
            var handler = new SvgFileFormatHandler();
            Assert.NotNull(handler);

            Assert.Equal(ExternalType.None, SvgDocument.ResolveExternalImages);
            Assert.Equal(ExternalType.None, SvgDocument.ResolveExternalElements);
            Assert.Equal(ExternalType.None, SvgDocument.ResolveExternalXmlEntites);
        }

        [Fact]
        public void TryLoadFromStream_WithExternalImageReference_DoesNotIssueHttpRequest()
        {
            // A loopback TCP listener that stays open for the whole test acts as the canary: any HTTP request for the
            // external image needs a TCP connection to it. Binding to port 0 lets the OS pick a free port, and because the
            // listener is never closed in between, no other process can take the port (unlike probing for a "free" port first).
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            int connectionCount = 0;

            var acceptThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        using (listener.AcceptTcpClient())
                        {
                            Interlocked.Increment(ref connectionCount);
                        }
                    }
                }
                catch (SocketException)
                {
                    // Expected when the listener is stopped
                }
                catch (ObjectDisposedException)
                {
                    // Expected when the listener is stopped
                }
                catch (InvalidOperationException)
                {
                    // Expected when the listener is stopped
                }
            })
            {
                IsBackground = true
            };
            acceptThread.Start();

            try
            {
                string svgContent = $@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""100"">
  <image href=""http://127.0.0.1:{port}/canary.png"" width=""100"" height=""100"" />
  <rect width=""100"" height=""100"" fill=""red"" />
</svg>";

                var handler = new SvgFileFormatHandler();
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(svgContent)))
                {
                    bool success = handler.TryLoadFromStream(ms, ".svg", out var bitmap);
                    Assert.True(success);
                    Assert.NotNull(bitmap);
                    bitmap.Dispose();
                }

                // Also verify SvgContainer
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(svgContent)))
                {
                    var container = new SvgContainer(ms, null);
                    Assert.NotNull(container);
                }

                // Also verify LoadDrawablesFromStream
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(svgContent)))
                {
                    var drawables = new System.Collections.Generic.List<Greenshot.Base.Interfaces.Drawing.IDrawableContainer>(handler.LoadDrawablesFromStream(ms, ".svg", null));
                    Assert.NotEmpty(drawables);
                }

                // Allow brief window for any rogue async request
                Thread.Sleep(200);

                Assert.Equal(0, Volatile.Read(ref connectionCount));
            }
            finally
            {
                listener.Stop();
                acceptThread.Join(2000);
            }
        }
    }
}
