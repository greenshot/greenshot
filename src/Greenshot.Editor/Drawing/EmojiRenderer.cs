﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Font = SixLabors.Fonts.Font;
using FontStyle = SixLabors.Fonts.FontStyle;
using Image = System.Drawing.Image;
using TextOptions = SixLabors.Fonts.TextOptions;

namespace Greenshot.Editor.Drawing
{
    internal static class EmojiRenderer
    {
        private static SixLabors.Fonts.FontFamily? _fontFamily;

        private static ConcurrentDictionary<string, BitmapSource> _iconCache = new ConcurrentDictionary<string, BitmapSource>();

        public static Image<Rgba32> GetImage(string emoji, int iconSize)
        {
            if (_fontFamily == null)
            {
                using var stream = Assembly.GetCallingAssembly().GetManifestResourceStream("Greenshot.Editor.Resources.TwemojiMozilla.ttf");
                var fontCollection = new FontCollection();
                fontCollection.Add(stream);
                if (fontCollection.TryGet("Twemoji Mozilla", out var fontFamily))
                {
                    _fontFamily = fontFamily;
                }
            }

            var font = _fontFamily.Value.CreateFont(iconSize, FontStyle.Regular);

            var image = new Image<Rgba32>(iconSize, iconSize);

            RenderEmoji(emoji, font, image);
            return image;
        }

        private static void RenderEmoji(string emoji, Font font, Image<Rgba32> image)
        {
            var verticalOffset = font.Size * 0.045f;
            var textOptions = new TextOptions(font)
            {
                Origin = new SixLabors.ImageSharp.PointF(0, font.Size / 2.0f - verticalOffset),
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center
            };

            image.Mutate(x => x.DrawText(textOptions, emoji, SixLabors.ImageSharp.Color.Black));
        }

        public static Image GetBitmap(string emoji, int iconSize)
        {
            using var image = GetImage(emoji, iconSize);
            return image.ToBitmap();
        }

        public static BitmapSource GetBitmapSource(string emoji, int iconSize)
        {
            using var image = GetImage(emoji, iconSize);
            return image.ToBitmapSource();
        }

        public static Bitmap ToBitmap(this Image<Rgba32> image)
        {
            using var memoryStream = new MemoryStream();

            var imageEncoder = image.GetConfiguration().ImageFormatsManager.FindEncoder(PngFormat.Instance);
            image.Save(memoryStream, imageEncoder);

            memoryStream.Seek(0, SeekOrigin.Begin);

            return new Bitmap(memoryStream);
        }

        public static BitmapSource ToBitmapSource(this Image<Rgba32> image)
        {
            using var memoryStream = new MemoryStream();

            var imageEncoder = image.GetConfiguration().ImageFormatsManager.FindEncoder(PngFormat.Instance);
            image.Save(memoryStream, imageEncoder);

            memoryStream.Seek(0, SeekOrigin.Begin);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = memoryStream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        public static void FillIconCache(IEnumerable<string> emojis)
        {
            Parallel.ForEach(emojis, emoji =>
            {
                GetIcon(emoji);
            });
        }

        public static BitmapSource GetIcon(string emoji)
        {
            return _iconCache.GetOrAdd(emoji, x => GetBitmapSource(x, 64));
        }
    }
}
