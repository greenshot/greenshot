//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text;

#endregion

namespace Greenshot.Core.Gfx
{
	public class EffectConverter : TypeConverter
	{
		// Fix to prevent BUG-1753
		private readonly NumberFormatInfo numberFormatInfo = new NumberFormatInfo();

		public EffectConverter()
		{
			numberFormatInfo.NumberDecimalSeparator = ".";
			numberFormatInfo.NumberGroupSeparator = ",";
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
						float darkness;
						// Fix to prevent BUG-1753
						if ((pair[1] != null) && float.TryParse(pair[1], NumberStyles.Float, numberFormatInfo, out darkness))
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
						Point shadowOffset = new Point();
						int shadowOffsetX;
						int shadowOffsetY;
						string[] coordinates = pair[1].Split(',');
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
			string[] values = valuesString.Split('|');
			foreach (string nameValuePair in values)
			{
				string[] pair = nameValuePair.Split(':');
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
						string[] edges = pair[1].Split(',');
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

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if ((value != null) && (value.GetType() == typeof(string)))
			{
				string settings = value as string;
				if (settings.Contains("ToothHeight"))
				{
					return ConvertTo(context, culture, value, typeof(TornEdgeEffect));
				}
				return ConvertTo(context, culture, value, typeof(DropShadowEffect));
			}
			return base.ConvertFrom(context, culture, value);
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
			if (value.GetType() == typeof(string))
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

		private void RetrieveDropShadowEffectValues(DropShadowEffect effect, StringBuilder sb)
		{
			// Fix to prevent BUG-1753 is to use the numberFormatInfo
			sb.AppendFormat("Darkness:{0}|ShadowSize:{1}|ShadowOffset:{2},{3}", effect.Darkness.ToString("F2", numberFormatInfo), effect.ShadowSize, effect.ShadowOffset.X, effect.ShadowOffset.Y);
		}

		private void RetrieveTornEdgeEffectValues(TornEdgeEffect effect, StringBuilder sb)
		{
			sb.AppendFormat("GenerateShadow:{0}|ToothHeight:{1}|HorizontalToothRange:{2}|VerticalToothRange:{3}|Edges:{4},{5},{6},{7}", effect.GenerateShadow, effect.ToothHeight, effect.HorizontalToothRange, effect.VerticalToothRange, effect.Edges[0], effect.Edges[1], effect.Edges[2], effect.Edges[3]);
		}
	}
}