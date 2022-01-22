using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using FontStyle = SixLabors.Fonts.FontStyle;
using Image = System.Drawing.Image;

namespace Greenshot.Editor.Drawing
{
    internal static class EmojiRenderer
    {
        private static SixLabors.Fonts.FontFamily? _fontFamily;

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

            var textOptions = new TextOptions(font) {  Origin = new SixLabors.ImageSharp.PointF(0, iconSize / 2.0f), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };

            image.Mutate(x => x.DrawText(textOptions, emoji, SixLabors.ImageSharp.Color.Black));
            return image;
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
    }
}
