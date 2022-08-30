using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Effects;

namespace Greenshot.Base.Core
{
    public class EffectConverter : TypeConverter
    {
        // Fix to prevent BUG-1753
        private readonly NumberFormatInfo _numberFormatInfo = new NumberFormatInfo();

        public EffectConverter()
        {
            _numberFormatInfo.NumberDecimalSeparator = ".";
            _numberFormatInfo.NumberGroupSeparator = ",";
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return true;
            }

            if (destinationType == typeof(DropShadowEffect))
            {
                return true;
            }

            if (destinationType == typeof(TornEdgeEffect))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            // to string
            if (destinationType == typeof(string))
            {
                StringBuilder sb = new StringBuilder();
                if (value.GetType() == typeof(DropShadowEffect))
                {
                    DropShadowEffect effect = value as DropShadowEffect;
                    RetrieveDropShadowEffectValues(effect, sb);
                    return sb.ToString();
                }

                if (value.GetType() == typeof(TornEdgeEffect))
                {
                    TornEdgeEffect effect = value as TornEdgeEffect;
                    RetrieveDropShadowEffectValues(effect, sb);
                    sb.Append("|");
                    RetrieveTornEdgeEffectValues(effect, sb);
                    return sb.ToString();
                }
            }

            // from string
            if (value is string)
            {
                string settings = value as string;
                if (destinationType == typeof(DropShadowEffect))
                {
                    DropShadowEffect effect = new DropShadowEffect();
                    ApplyDropShadowEffectValues(settings, effect);
                    return effect;
                }

                if (destinationType == typeof(TornEdgeEffect))
                {
                    TornEdgeEffect effect = new TornEdgeEffect();
                    ApplyDropShadowEffectValues(settings, effect);
                    ApplyTornEdgeEffectValues(settings, effect);
                    return effect;
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string settings)
            {
                if (settings.Contains("ToothHeight"))
                {
                    return ConvertTo(context, culture, settings, typeof(TornEdgeEffect));
                }

                return ConvertTo(context, culture, settings, typeof(DropShadowEffect));
            }

            return base.ConvertFrom(context, culture, value);
        }

        private void ApplyDropShadowEffectValues(string valuesString, DropShadowEffect effect)
        {
            string[] values = valuesString.Split('|');
            foreach (string nameValuePair in values)
            {
                string[] pair = nameValuePair.Split(':');
                switch (pair[0])
                {
                    case "Darkness":
                        // Fix to prevent BUG-1753
                        if (pair[1] != null && float.TryParse(pair[1], NumberStyles.Float, _numberFormatInfo, out var darkness))
                        {
                            if (darkness <= 1.0)
                            {
                                effect.Darkness = darkness;
                            }
                        }

                        break;
                    case "ShadowSize":
                        if (int.TryParse(pair[1], out var shadowSize))
                        {
                            effect.ShadowSize = shadowSize;
                        }

                        break;
                    case "ShadowOffset":
                        NativePoint shadowOffset = new NativePoint();
                        string[] coordinates = pair[1].Split(',');
                        if (int.TryParse(coordinates[0], out var shadowOffsetX))
                        {
                            shadowOffset = shadowOffset.ChangeX(shadowOffsetX);
                        }

                        if (int.TryParse(coordinates[1], out var shadowOffsetY))
                        {
                            shadowOffset = shadowOffset.ChangeY(shadowOffsetY);
                        }

                        effect.ShadowOffset = shadowOffset;
                        break;
                }
            }
        }

        private void ApplyTornEdgeEffectValues(string valuesString, TornEdgeEffect effect)
        {
            string[] values = valuesString.Split('|');
            foreach (string nameValuePair in values)
            {
                string[] pair = nameValuePair.Split(':');
                switch (pair[0])
                {
                    case "GenerateShadow":
                        if (bool.TryParse(pair[1], out var generateShadow))
                        {
                            effect.GenerateShadow = generateShadow;
                        }

                        break;
                    case "ToothHeight":
                        if (int.TryParse(pair[1], out var toothHeight))
                        {
                            effect.ToothHeight = toothHeight;
                        }

                        break;
                    case "HorizontalToothRange":
                        if (int.TryParse(pair[1], out var horizontalToothRange))
                        {
                            effect.HorizontalToothRange = horizontalToothRange;
                        }

                        break;
                    case "VerticalToothRange":
                        if (int.TryParse(pair[1], out var verticalToothRange))
                        {
                            effect.VerticalToothRange = verticalToothRange;
                        }

                        break;
                    case "Edges":
                        string[] edges = pair[1].Split(',');
                        if (bool.TryParse(edges[0], out var edge))
                        {
                            effect.Edges[0] = edge;
                        }

                        if (bool.TryParse(edges[1], out edge))
                        {
                            effect.Edges[1] = edge;
                        }

                        if (bool.TryParse(edges[2], out edge))
                        {
                            effect.Edges[2] = edge;
                        }

                        if (bool.TryParse(edges[3], out edge))
                        {
                            effect.Edges[3] = edge;
                        }

                        break;
                }
            }
        }

        private void RetrieveDropShadowEffectValues(DropShadowEffect effect, StringBuilder sb)
        {
            // Fix to prevent BUG-1753 is to use the numberFormatInfo
            sb.AppendFormat("Darkness:{0}|ShadowSize:{1}|ShadowOffset:{2},{3}", effect.Darkness.ToString("F2", _numberFormatInfo), effect.ShadowSize, effect.ShadowOffset.X,
                effect.ShadowOffset.Y);
        }

        private void RetrieveTornEdgeEffectValues(TornEdgeEffect effect, StringBuilder sb)
        {
            sb.AppendFormat("GenerateShadow:{0}|ToothHeight:{1}|HorizontalToothRange:{2}|VerticalToothRange:{3}|Edges:{4},{5},{6},{7}", effect.GenerateShadow, effect.ToothHeight,
                effect.HorizontalToothRange, effect.VerticalToothRange, effect.Edges[0], effect.Edges[1], effect.Edges[2], effect.Edges[3]);
        }
    }
}