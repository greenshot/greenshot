// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Gfx.Effects
{
    /// <summary>
    /// This is the TypeConverter for the effects, taking care of serializing these to the .ini file
    /// </summary>
	public class EffectConverter : TypeConverter
	{
		// Fix to prevent BUG-1753
		private readonly NumberFormatInfo _numberFormatInfo = new NumberFormatInfo();

        /// <summary>
        /// Default constructor
        /// </summary>
		public EffectConverter()
		{
			_numberFormatInfo.NumberDecimalSeparator = ".";
			_numberFormatInfo.NumberGroupSeparator = ",";
		}

        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof(string))
			{
				return true;
			}
			return base.CanConvertFrom(context, sourceType);
		}

        /// <inheritdoc />
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(string) || destinationType == typeof(DropShadowEffect) || destinationType == typeof(TornEdgeEffect) || base.CanConvertTo(context, destinationType);
		}

        /// <inheritdoc />
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			// to string
			if (destinationType == typeof(string))
			{
				var sb = new StringBuilder();
			    switch (value)
			    {
			        case TornEdgeEffect tornEdgeEffect:
			            RetrieveDropShadowEffectValues(tornEdgeEffect, sb);
			            sb.Append("|");
			            RetrieveTornEdgeEffectValues(tornEdgeEffect, sb);
			            return sb.ToString();
			        case DropShadowEffect dropShadowEffect:
			            RetrieveDropShadowEffectValues(dropShadowEffect, sb);
			            return sb.ToString();
			    }
			}
			// from string
		    if (!(value is string settings))
		    {
		        return base.ConvertTo(context, culture, value, destinationType);
		    }
		    if (destinationType == typeof(DropShadowEffect))
		    {
		        var effect = new DropShadowEffect();
		        ApplyDropShadowEffectValues(settings, effect);
		        return effect;
		    }
		    if (destinationType == typeof(TornEdgeEffect))
		    {
		        var effect = new TornEdgeEffect();
		        ApplyDropShadowEffectValues(settings, effect);
		        ApplyTornEdgeEffectValues(settings, effect);
		        return effect;
		    }
		    return base.ConvertTo(context, culture, value, destinationType);
		}

        /// <inheritdoc />
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
		    if (!(value is string settings))
		    {
		        return base.ConvertFrom(context, culture, value);
		    }
		    return ConvertTo(context, culture, settings, settings.Contains("ToothHeight") ? typeof(TornEdgeEffect) : typeof(DropShadowEffect));
		}

        /// <inheritdoc />
        private void ApplyDropShadowEffectValues(string valuesString, DropShadowEffect effect)
		{
			var values = valuesString.Split('|');
			foreach (var nameValuePair in values)
			{
				var pair = nameValuePair.Split(':');
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
						var shadowOffset = new NativePoint();
					    var coordinates = pair[1].Split(',');
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
			var values = valuesString.Split('|');
			foreach (var nameValuePair in values)
			{
				var pair = nameValuePair.Split(':');
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
						var edges = pair[1].Split(',');
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