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
using System.Drawing;
using System.Drawing.Drawing2D;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.Base.Effects
{
    /// <summary>
    /// ReduceColorsEffect
    /// </summary>
    public class ReduceColorsEffect : IEffect
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ReduceColorsEffect));

        public ReduceColorsEffect()
        {
            Reset();
        }

        public int Colors { get; set; }

        public void Reset()
        {
            Colors = 256;
        }

        public Image Apply(Image sourceImage, Matrix matrix)
        {
            using (WuQuantizer quantizer = new WuQuantizer((Bitmap) sourceImage))
            {
                int colorCount = quantizer.GetColorCount();
                if (colorCount > Colors)
                {
                    try
                    {
                        return quantizer.GetQuantizedImage(Colors);
                    }
                    catch (Exception e)
                    {
                        Log.Warn("Error occurred while Quantizing the image, ignoring and using original. Error: ", e);
                    }
                }
            }

            return null;
        }
    }
}