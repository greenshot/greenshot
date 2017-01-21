using Greenshot.IniFile;
using GreenshotPlugin.Core;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GreenshotPlugin.Effects
{
    /// <summary>
    /// default effect.
    /// </summary>
    public class DefaultEffect : IEffect
    {
        public DefaultEffect()
        {
            this.Reset();
        }

        public Color Color
        {
            get;
            set;
        }

        public int Width
        {
            get;
            set;
        }

        public Image Apply(Image sourceImage, Matrix matrix)
        {
            return ImageHelper.CreateBorder(sourceImage, Width, Color, sourceImage.PixelFormat, matrix);
        }

        public void Reset()
        {
            var coreConfiguration = IniConfig.GetIniSection<CoreConfiguration>();
            if (null == coreConfiguration || string.IsNullOrEmpty(coreConfiguration.DestinationDefaultBorderEffect?.Trim()))
            {
                return;
            }
            var defaultBorderEffect = Newtonsoft.Json.JsonConvert.DeserializeObject<DefaultBorderEffect>(coreConfiguration.DestinationDefaultBorderEffect);
            if (null == defaultBorderEffect)
            {
                return;
            }
            this.Color = defaultBorderEffect.Color;
            this.Width = defaultBorderEffect.Width;
        }
    }
}