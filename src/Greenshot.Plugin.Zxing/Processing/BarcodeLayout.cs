/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Collections.Generic;
using System.Drawing;

namespace Greenshot.Plugin.Zxing.Processing
{
    /// <summary>
    /// What a barcode draws, without drawing it (see ZxingBarcodeGenerator.Layout): its size, its colors and its modules. Drawn with GDI+ for the
    /// editor (ZxingBarcodeGenerator.Render) and with ImageSharp for an HDR file (ZxingBarcodeGenerator.Draw), the same way.
    /// </summary>
    public sealed class BarcodeLayout
    {
        public BarcodeLayout(int width, int height, Color fore, Color back, IReadOnlyList<BarcodeModule> modules)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Fore = fore;
            Back = back;
            Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        }

        /// <summary>
        /// The size of the barcode in pixels
        /// </summary>
        public int Width { get; }

        public int Height { get; }

        /// <summary>
        /// The color of the modules
        /// </summary>
        public Color Fore { get; }

        /// <summary>
        /// The color under them, all of the barcode
        /// </summary>
        public Color Back { get; }

        /// <summary>
        /// The modules, in the coordinates of the barcode
        /// </summary>
        public IReadOnlyList<BarcodeModule> Modules { get; }
    }

    /// <summary>
    /// One module of a barcode: a square (or bar), or a dot of a rounded 2D code
    /// </summary>
    public readonly struct BarcodeModule
    {
        public BarcodeModule(RectangleF area, bool round)
        {
            Area = area;
            Round = round;
        }

        /// <summary>
        /// The area, for a dot the box around it
        /// </summary>
        public RectangleF Area { get; }

        public bool Round { get; }
    }
}
