/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Windows.Media.Imaging;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// This is the System.Windows.Media.Imaging (WPF) file format handler, which uses WIC
    /// </summary>
    public class WpfFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WpfFileFormatHandler));
        private List<string> LoadFromStreamExtensions { get; } = new() { ".jxr", ".wdp", ".wmp", ".heic", ".heif" };
        private List<string> SaveToStreamExtensions { get; } = new() { ".jxr" };
        
        public WpfFileFormatHandler()
        {
            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = LoadFromStreamExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = LoadFromStreamExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = SaveToStreamExtensions;
        }

        /// <inheritdoc />
        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null)
        {
            try
            {
                var bitmapSource = bitmap.ToBitmapSource();
                var bitmapFrame = BitmapFrame.Create(bitmapSource);
                var jpegXrEncoder = new WmpBitmapEncoder();
                jpegXrEncoder.Frames.Add(bitmapFrame);
                // TODO: Support supplying a quality
                //jpegXrEncoder.ImageQualityLevel = quality / 100f;
                jpegXrEncoder.Save(destination);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't save image as JPEG XR: ", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            try
            {
                var bitmapDecoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                var bitmapSource = bitmapDecoder.Frames[0];
                bitmap = bitmapSource.ToBitmap();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't load image: ", ex);
            }

            bitmap = null;
            return false;
        }
    }
}
