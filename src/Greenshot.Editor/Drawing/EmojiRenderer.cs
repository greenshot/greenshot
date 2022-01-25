using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Font = SixLabors.Fonts.Font;
using FontFamily = SixLabors.Fonts.FontFamily;
using FontStyle = SixLabors.Fonts.FontStyle;
using Image = System.Drawing.Image;
using TextOptions = SixLabors.Fonts.TextOptions;

namespace Greenshot.Editor.Drawing
{
    internal static class EmojiRenderer
    {
        private static Lazy<FontFamily> _fontFamily = new Lazy<FontFamily>(() =>
        {
            using var stream = Assembly.GetCallingAssembly().GetManifestResourceStream("Greenshot.Editor.Resources.TwemojiMozilla.ttf");
            var fontCollection = new FontCollection();
            fontCollection.Add(stream);
            fontCollection.TryGet("Twemoji Mozilla", out var fontFamily);
            return fontFamily;
        });

        public static Image<Rgba32> GetImage(string emoji, int iconSize)
        {
            var font = _fontFamily.Value.CreateFont(iconSize, FontStyle.Regular);

            var image = new Image<Rgba32>(iconSize, iconSize);

            RenderEmoji(emoji, font, image);
            return image;
        }

        private static void RenderEmoji(string emoji, Font font, SixLabors.ImageSharp.Image image)
        {
            var verticalOffset = font.Size * 0.045f;
            var textOptions = new TextOptions(font)
            {
                Origin = new SixLabors.ImageSharp.PointF(font.Size / 2.0f, font.Size / 2.0f - verticalOffset),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
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
            var pixelFormat = PixelFormats.Bgra32;
            int width = iconSize;
            int height = iconSize;
            int stride = (width * pixelFormat.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[stride * height];

            var image = SixLabors.ImageSharp.Image.WrapMemory<Bgra32>(byteMemory: pixels, width: width, height: height);

            var font = _fontFamily.Value.CreateFont(iconSize, FontStyle.Regular);
            RenderEmoji(emoji, font, image);

            var source = BitmapSource.Create(pixelWidth: width, pixelHeight: height, dpiX: 96, dpiY: 96, pixelFormat: pixelFormat, palette: null, pixels: pixels, stride: stride);
            source.Freeze();

            return source;
        }

        public static Bitmap ToBitmap(this Image<Rgba32> image)
        {
            using var memoryStream = new MemoryStream();

            image.SaveAsPng(memoryStream);

            memoryStream.Seek(0, SeekOrigin.Begin);

            return new Bitmap(memoryStream);
        }
    }
}
