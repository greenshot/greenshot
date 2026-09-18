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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Storage.Streams;
using Dapplo.Windows.Common.Structs;
using log4net;
using Microsoft.Win32.SafeHandles;

namespace Greenshot.Base.Core;

/// <summary>
/// Helper for interacting with Windows Store / MSIX / UWP applications,
/// including package resolution and application logo retrieval.
/// </summary>
public static class WindowsAppHelper
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(WindowsAppHelper));
    private static readonly ConcurrentDictionary<string, Image> LogoCache = new(StringComparer.OrdinalIgnoreCase);

    private const uint OpenExisting = 3;
    private const uint FileFlagOpenReparsePoint = 0x00200000;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint FsctlGetReparsePoint = 0x000900A8;
    private const uint IoReparseTagAppExecLink = 0x8000001B;

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        byte[] lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    /// <summary>
    /// Asynchronously retrieves the application logo image for the specified package at the requested size.
    /// </summary>
    /// <remarks>The logo is retrieved from the package's AppListEntry and returned in a square format
    /// based on the specified size. The method returns null if the package does not have an associated AppListEntry
    /// or if no logo is available.</remarks>
    /// <param name="package">The package for which to retrieve the application logo. This parameter cannot be null.</param>
    /// <param name="size">An optional parameter that specifies the desired size of the logo. If not provided, a default size of 64x64
    /// pixels is used.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an Image object representing the
    /// application logo, or null if no logo is available for the specified package.</returns>
    public static async Task<Image> GetAppxLogoAsync(Package package, NativeSize size = default)
    {
        if (package == null)
        {
            return null;
        }

        try
        {
            int targetDim = size.IsEmpty ? 64 : Math.Max(size.Width, size.Height);

            // 1. Try finding a direct unplated AppList/targetsize asset from package files (e.g. Assets/*AppList*.targetsize-*.png)
            var assetImage = TryLoadTargetSizeAsset(package, targetDim);
            if (assetImage != null)
            {
                return TrimExcessiveTransparentBorders(assetImage);
            }

            // 2. Fallback to WinRT AppListEntry DisplayInfo.GetLogo
            var entries = await package.GetAppListEntriesAsync().AsTask().ConfigureAwait(false);
            var entry = entries?.FirstOrDefault();
            if (entry == null)
            {
                return null;
            }

            // Retrieve display info for the entry
            var displayInfo = entry.DisplayInfo;
            if (displayInfo == null)
            {
                return null;
            }

            var requestedSize = size.IsEmpty
                ? new Windows.Foundation.Size(64, 64)
                : new Windows.Foundation.Size(size.Width, size.Height);

            // Request a logo at the desired size (square pixel size)
            var randomAccessStreamReference = displayInfo.GetLogo(requestedSize);
            if (randomAccessStreamReference == null)
            {
                return null;
            }

            using IRandomAccessStream randomAccessStream = await randomAccessStreamReference.OpenReadAsync().AsTask().ConfigureAwait(false);
            using Stream netStream = randomAccessStream.AsStreamForRead();
            using var rawImage = Image.FromStream(netStream);
            // Trim Start Menu tile padding so the icon fills the canvas nicely in menus/toolbars
            return TrimExcessiveTransparentBorders(rawImage);
        }
        catch (Exception ex)
        {
            Log.Debug("Unable to retrieve logo for package: " + package.Id?.FullName, ex);
            return null;
        }
    }

    /// <summary>
    /// Synchronously retrieves the application logo image for the specified package at the requested size.
    /// Safely executes on a thread-pool thread to prevent UI-thread deadlocks.
    /// </summary>
    /// <param name="package">The package for which to retrieve the logo.</param>
    /// <param name="size">Optional desired size of the logo.</param>
    /// <returns>The logo Image, or null if unavailable.</returns>
    public static Image GetAppxLogo(Package package, NativeSize size = default)
    {
        if (package == null)
        {
            return null;
        }

        try
        {
            return Task.Run(() => GetAppxLogoAsync(package, size)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Debug("Unable to retrieve Appx logo synchronously for package: " + package.Id?.FullName, ex);
            return null;
        }
    }

    /// <summary>
    /// Attempts to find a Windows App (UWP/MSIX) Package matching a command line or command name.
    /// Handles AppExecutionAliases, paths in WindowsApps, AppUserModelIds, Package Family Names,
    /// and DisplayNames.
    /// </summary>
    /// <param name="commandLine">The command line or executable path.</param>
    /// <param name="commandName">Optional user-facing command name.</param>
    /// <returns>The matching Package, or null if not found.</returns>
    public static Package FindPackage(string commandLine, string commandName = null)
    {
        try
        {
            var packageManager = new PackageManager();

            string expandedCmd = null;
            if (!string.IsNullOrWhiteSpace(commandLine))
            {
                expandedCmd = FilenameHelper.FillVariables(commandLine, true);
                expandedCmd = FilenameHelper.FillCmdVariables(expandedCmd, true);
            }

            // 1. Check if the path is an AppExecutionAlias reparse point (e.g. in %LOCALAPPDATA%\Microsoft\WindowsApps\)
            if (!string.IsNullOrEmpty(expandedCmd) && File.Exists(expandedCmd))
            {
                var packageFamily = ReadAppExecLinkPackageFamily(expandedCmd);
                if (!string.IsNullOrEmpty(packageFamily))
                {
                    var pkg = packageManager.FindPackagesForUser(string.Empty, packageFamily).LastOrDefault();
                    if (pkg != null)
                    {
                        return pkg;
                    }
                }

                // Check if path is inside WindowsApps directory (e.g. C:\Program Files\WindowsApps\<PackageFullName>\...)
                var pkgFullName = ExtractPackageFullNameFromPath(expandedCmd);
                if (!string.IsNullOrEmpty(pkgFullName))
                {
                    var pkg = packageManager.FindPackageForUser(string.Empty, pkgFullName);
                    if (pkg != null)
                    {
                        return pkg;
                    }
                }
            }

            // 2. Check if commandLine is an executable filename that has an AppExecutionAlias in %LOCALAPPDATA%\Microsoft\WindowsApps
            string exeName = null;
            if (!string.IsNullOrEmpty(expandedCmd))
            {
                exeName = Path.GetFileName(expandedCmd);
            }
            if (string.IsNullOrEmpty(exeName) && !string.IsNullOrEmpty(commandName))
            {
                exeName = commandName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? commandName : commandName + ".exe";
            }

            if (!string.IsNullOrEmpty(exeName))
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string aliasPath = Path.Combine(localAppData, "Microsoft", "WindowsApps", exeName);
                if (File.Exists(aliasPath))
                {
                    var packageFamily = ReadAppExecLinkPackageFamily(aliasPath);
                    if (!string.IsNullOrEmpty(packageFamily))
                    {
                        var pkg = packageManager.FindPackagesForUser(string.Empty, packageFamily).LastOrDefault();
                        if (pkg != null)
                        {
                            return pkg;
                        }
                    }
                }
            }

            // 3. Search packages by DisplayName, Package Name, or Family Name
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(commandName))
            {
                candidates.Add(commandName.Trim());
            }
            if (!string.IsNullOrWhiteSpace(commandLine))
            {
                string fn = Path.GetFileNameWithoutExtension(expandedCmd ?? commandLine);
                if (!string.IsNullOrWhiteSpace(fn) && !candidates.Contains(fn, StringComparer.OrdinalIgnoreCase))
                {
                    candidates.Add(fn.Trim());
                }
            }

            if (candidates.Count > 0)
            {
                var packages = packageManager.FindPackagesForUser(string.Empty)
                    .Where(p => !p.IsFramework)
                    .ToList();

                foreach (var candidate in candidates)
                {
                    // Exact DisplayName match
                    var match = packages.LastOrDefault(p => string.Equals(p.DisplayName, candidate, StringComparison.OrdinalIgnoreCase));
                    if (match != null) return match;

                    // Exact Id.Name match
                    match = packages.LastOrDefault(p => string.Equals(p.Id?.Name, candidate, StringComparison.OrdinalIgnoreCase));
                    if (match != null) return match;

                    // DisplayName starts with candidate (e.g. "Paint" matching "Paint")
                    match = packages.LastOrDefault(p => p.DisplayName != null && p.DisplayName.StartsWith(candidate, StringComparison.OrdinalIgnoreCase));
                    if (match != null) return match;

                    // Id.Name contains or starts with candidate
                    match = packages.LastOrDefault(p => p.Id?.Name != null && p.Id.Name.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (match != null) return match;

                    // Candidate starts with DisplayName
                    match = packages.LastOrDefault(p => !string.IsNullOrEmpty(p.DisplayName) && candidate.StartsWith(p.DisplayName, StringComparison.OrdinalIgnoreCase));
                    if (match != null) return match;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug("Error searching Windows App package", ex);
        }

        return null;
    }

    /// <summary>
    /// Retrieves a cached or fresh application logo for the specified command line or command name.
    /// </summary>
    /// <param name="commandLine">The command line or executable path.</param>
    /// <param name="commandName">Optional command name.</param>
    /// <param name="size">Optional desired size.</param>
    /// <returns>An Image if found, or null.</returns>
    public static Image GetAppLogo(string commandLine, string commandName = null, NativeSize size = default)
    {
        string cacheKey = $"{commandLine}|{commandName}|{size.Width}x{size.Height}";
        if (LogoCache.TryGetValue(cacheKey, out Image cachedImage))
        {
            return cachedImage;
        }

        try
        {
            // Run package discovery and logo extraction entirely on the thread pool (MTA).
            // This prevents WinRT COM objects created during FindPackage from binding to the STA UI thread,
            // which causes cross-apartment deadlocks/failures when GetAppxLogoAsync is awaited synchronously.
            return Task.Run(async () =>
            {
                var package = FindPackage(commandLine, commandName);
                if (package != null)
                {
                    var logo = await GetAppxLogoAsync(package, size).ConfigureAwait(false);
                    if (logo != null)
                    {
                        LogoCache[cacheKey] = logo;
                        return logo;
                    }
                }
                return null;
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Debug("Unable to retrieve Windows App logo for " + (commandLine ?? commandName), ex);
            return null;
        }
    }

    /// <summary>
    /// Reads the Package Family Name from a Windows AppExecutionAlias reparse point (IO_REPARSE_TAG_APPEXECLINK).
    /// </summary>
    private static string ReadAppExecLinkPackageFamily(string path)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                return null;
            }

            using var handle = CreateFile(
                path,
                0,
                FileShare.ReadWrite | FileShare.Delete,
                IntPtr.Zero,
                OpenExisting,
                FileFlagOpenReparsePoint | FileFlagBackupSemantics,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                return null;
            }

            byte[] outBuffer = new byte[16384];
            if (!DeviceIoControl(handle, FsctlGetReparsePoint, IntPtr.Zero, 0, outBuffer, (uint)outBuffer.Length, out uint bytesReturned, IntPtr.Zero))
            {
                return null;
            }

            if (bytesReturned < 12)
            {
                return null;
            }

            uint tag = BitConverter.ToUInt32(outBuffer, 0);
            if (tag != IoReparseTagAppExecLink)
            {
                return null;
            }

            // AppExecLink format:
            // 0..3: ReparseTag (0x8000001B)
            // 4..5: ReparseDataLength
            // 6..7: Reserved
            // 8..11: Version (uint)
            // 12..: Sequence of null-terminated UTF-16 strings:
            //   [0]: PackageFamilyName
            //   [1]: AppUserModelId
            //   [2]: Target executable
            int offset = 12;
            int start = offset;
            while (offset + 1 < bytesReturned && !(outBuffer[offset] == 0 && outBuffer[offset + 1] == 0))
            {
                offset += 2;
            }

            if (offset > start)
            {
                return Encoding.Unicode.GetString(outBuffer, start, offset - start);
            }
        }
        catch (Exception ex)
        {
            Log.Debug("Error reading AppExecLink for " + path, ex);
        }

        return null;
    }

    /// <summary>
    /// Extracts the package full name from a path inside the WindowsApps folder.
    /// E.g. C:\Program Files\WindowsApps\Microsoft.Paint_11.2605.81.0_x64__8wekyb3d8bbwe\PaintApp\mspaint.exe
    /// returns Microsoft.Paint_11.2605.81.0_x64__8wekyb3d8bbwe
    /// </summary>
    private static string ExtractPackageFullNameFromPath(string path)
    {
        try
        {
            int index = path.IndexOf(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;

            string sub = path.Substring(index + @"\WindowsApps\".Length);
            int nextSlash = sub.IndexOfAny(new[] { '\\', '/' });
            return nextSlash > 0 ? sub.Substring(0, nextSlash) : sub;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to find a direct AppList or targetsize asset from the package folder
    /// (e.g. Assets/*AppList*.targetsize-*.png) which is unplated and tightly cropped by design.
    /// </summary>
    private static Image TryLoadTargetSizeAsset(Package package, int desiredSize)
    {
        try
        {
            string installPath = package.InstalledLocation?.Path;
            if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
            {
                return null;
            }

            string assetsDir = Path.Combine(installPath, "Assets");
            if (!Directory.Exists(assetsDir))
            {
                assetsDir = installPath;
            }

            var files = Directory.GetFiles(assetsDir, "*targetsize*.png", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                return null;
            }

            var candidates = files.Where(f =>
                f.IndexOf("AppList", StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.IndexOf("Square44x44", StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.IndexOf("altform-unplated", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (candidates.Count == 0)
            {
                candidates = files.ToList();
            }

            string bestFile = null;
            int bestDiff = int.MaxValue;
            foreach (var file in candidates)
            {
                var match = System.Text.RegularExpressions.Regex.Match(file, @"targetsize-(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int sizeVal))
                {
                    int diff = Math.Abs(sizeVal - desiredSize);
                    if (file.IndexOf("altform-unplated", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        diff -= 1;
                    }
                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        bestFile = file;
                    }
                }
            }

            if (bestFile != null && File.Exists(bestFile))
            {
                using var stream = new MemoryStream(File.ReadAllBytes(bestFile));
                return Image.FromStream(stream);
            }
        }
        catch (Exception ex)
        {
            Log.Debug("Unable to load targetsize asset from package " + package.Id?.FullName, ex);
        }

        return null;
    }

    /// <summary>
    /// Trims excessive transparent padding around an icon image so that the actual graphic
    /// fills the frame cleanly (with a small proportional margin), avoiding the tiny-looking
    /// icons produced by UWP/WinRT tile logos which embed a small glyph in a 150x150 canvas.
    /// </summary>
    public static Image TrimExcessiveTransparentBorders(Image original)
    {
        if (original == null)
        {
            return null;
        }

        Bitmap bitmap = original as Bitmap;
        bool disposeBitmap = false;
        if (bitmap == null)
        {
            try
            {
                bitmap = new Bitmap(original);
                disposeBitmap = true;
            }
            catch
            {
                return original;
            }
        }

        try
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            if (width <= 1 || height <= 1)
            {
                return (Image)original.Clone();
            }

            int minX = width;
            int maxX = -1;
            int minY = height;
            int maxY = -1;

            var rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = null;
            try
            {
                bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int stride = bmpData.Stride;
                IntPtr scan0 = bmpData.Scan0;

                unsafe
                {
                    byte* ptr = (byte*)scan0;
                    for (int y = 0; y < height; y++)
                    {
                        byte* row = ptr + (y * stride);
                        for (int x = 0; x < width; x++)
                        {
                            byte alpha = row[(x * 4) + 3];
                            if (alpha > 15) // visible pixel threshold
                            {
                                if (x < minX) minX = x;
                                if (x > maxX) maxX = x;
                                if (y < minY) minY = y;
                                if (y > maxY) maxY = y;
                            }
                        }
                    }
                }
            }
            catch
            {
                return (Image)original.Clone();
            }
            finally
            {
                if (bmpData != null)
                {
                    bitmap.UnlockBits(bmpData);
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return (Image)original.Clone();
            }

            int contentWidth = maxX - minX + 1;
            int contentHeight = maxY - minY + 1;

            // If content already occupies 85%+ of canvas, keep original size
            if (contentWidth >= width * 0.85 && contentHeight >= height * 0.85)
            {
                return (Image)original.Clone();
            }

            int maxDim = Math.Max(contentWidth, contentHeight);
            int padding = Math.Max(1, (int)(maxDim * 0.05)); // 5% breathing margin
            int canvasSize = maxDim + (padding * 2);

            var trimmed = new Bitmap(canvasSize, canvasSize, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(trimmed))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                int destX = (canvasSize - contentWidth) / 2;
                int destY = (canvasSize - contentHeight) / 2;

                var srcRect = new Rectangle(minX, minY, contentWidth, contentHeight);
                var destRect = new Rectangle(destX, destY, contentWidth, contentHeight);
                g.DrawImage(bitmap, destRect, srcRect, GraphicsUnit.Pixel);
            }

            return trimmed;
        }
        finally
        {
            if (disposeBitmap)
            {
                bitmap.Dispose();
            }
        }
    }
}
