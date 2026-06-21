/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.Generic;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Base.Core;

public class DetectedOcrLine : IOcrLineFeature
{
    public NativeRect Bounds { get; private set; }
    public string FeatureType => "OcrLine";
    public string Text { get; }
    public string ToolTipText => Text;
    public List<OcrWordInfo> Words { get; }

    public DetectedOcrLine(NativeRect bounds, string text, List<OcrWordInfo> words)
    {
        Bounds = bounds;
        Text = text;
        Words = words;
    }

    public void Offset(int x, int y)
    {
        Bounds = Bounds.Offset(x, y);

        foreach (var word in Words)
        {
            word.Bounds = word.Bounds.Offset(x, y);
        }
    }
}
