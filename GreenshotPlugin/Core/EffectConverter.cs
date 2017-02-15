#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text;
using GreenshotPlugin.Effects;

#endregion

namespace GreenshotPlugin.Core
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
				var sb = new StringBuilder();
				if (value.GetType() == typeof(DropShadowEffect))
				{
					var effect = value as DropShadowEffect;
					RetrieveDropShadowEffectValues(effect, sb);
					return sb.ToString();
				}
				if (value.GetType() == typeof(TornEdgeEffect))
				{
					var effect = value as TornEdgeEffect;
					RetrieveDropShadowEffectValues(effect, sb);
					sb.Append("|");
					RetrieveTornEdgeEffectValues(effect, sb);
					return sb.ToString();
				}
			}
			// from string
			if (value is string)
			{
				var settings = value as string;
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
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			var settings = value as string;
			if (settings != null)
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
			var values = valuesString.Split('|');
			foreach (var nameValuePair in values)
			{
				var pair = nameValuePair.Split(':');
				switch (pair[0])
				{
					case "Darkness":
						float darkness;
						// Fix to prevent BUG-1753
						if (pair[1] != null && float.TryParse(pair[1], NumberStyles.Float, _numberFormatInfo, out darkness))
						{
							if (darkness <= 1.0)
							{
								effect.Darkness = darkness;
							}
						}
						break;
					case "ShadowSize":
						int shadowSize;
						if (int.TryParse(pair[1], out shadowSize))
						{
							effect.ShadowSize = shadowSize;
						}
						break;
					case "ShadowOffset":
						var shadowOffset = new Point();
						int shadowOffsetX;
						int shadowOffsetY;
						var coordinates = pair[1].Split(',');
						if (int.TryParse(coordinates[0], out shadowOffsetX))
						{
							shadowOffset.X = shadowOffsetX;
						}
						if (int.TryParse(coordinates[1], out shadowOffsetY))
						{
							shadowOffset.Y = shadowOffsetY;
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
						bool generateShadow;
						if (bool.TryParse(pair[1], out generateShadow))
						{
							effect.GenerateShadow = generateShadow;
						}
						break;
					case "ToothHeight":
						int toothHeight;
						if (int.TryParse(pair[1], out toothHeight))
						{
							effect.ToothHeight = toothHeight;
						}
						break;
					case "HorizontalToothRange":
						int horizontalToothRange;
						if (int.TryParse(pair[1], out horizontalToothRange))
						{
							effect.HorizontalToothRange = horizontalToothRange;
						}
						break;
					case "VerticalToothRange":
						int verticalToothRange;
						if (int.TryParse(pair[1], out verticalToothRange))
						{
							effect.VerticalToothRange = verticalToothRange;
						}
						break;
					case "Edges":
						var edges = pair[1].Split(',');
						bool edge;
						if (bool.TryParse(edges[0], out edge))
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