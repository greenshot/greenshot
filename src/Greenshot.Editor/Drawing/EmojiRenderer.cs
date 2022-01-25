using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using FontFamily = SixLabors.Fonts.FontFamily;
using FontStyle = SixLabors.Fonts.FontStyle;
using Image = System.Drawing.Image;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
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
            var image = new Image<Rgba32>(iconSize, iconSize);

            RenderEmoji(emoji, iconSize, image);
            return image;
        }

        private static void RenderEmoji(string emoji, int iconSize, SixLabors.ImageSharp.Image image)
        {
            var font = _fontFamily.Value.CreateFont(iconSize, FontStyle.Regular);

            var verticalOffset = font.Size * 0.045f;
            var textOptions = new TextOptions(font)
            {
                Origin = new SixLabors.ImageSharp.PointF(font.Size / 2.0f, font.Size / 2.0f - verticalOffset),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            image.Mutate(x => x.DrawText(textOptions, emoji, SixLabors.ImageSharp.Color.Black));
        }

        public static Image GetBitmap(string emoji, int iconSize)
        {
            int width = iconSize;
            int height = iconSize;

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    var image = SixLabors.ImageSharp.Image.WrapMemory<Bgra32>((void*)bitmapData.Scan0, width: width, height: height);
                    RenderEmoji(emoji, iconSize, image);
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        public static BitmapSource GetBitmapSource(string emoji, int iconSize)
        {
            var pixelFormat = PixelFormats.Bgra32;
            int width = iconSize;
            int height = iconSize;
            int stride = (width * pixelFormat.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[stride * height];

            var image = SixLabors.ImageSharp.Image.WrapMemory<Bgra32>(byteMemory: pixels, width: width, height: height);

            RenderEmoji(emoji, iconSize, image);

            var source = BitmapSource.Create(pixelWidth: width, pixelHeight: height, dpiX: 96, dpiY: 96, pixelFormat: pixelFormat, palette: null, pixels: pixels, stride: stride);
            source.Freeze();

            return source;
        }
    }
}
