/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026  Thomas Braun, Jens Klingen, Robin Krom
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

using System.Drawing;
using System.Windows.Forms;

namespace Greenshot.Editor.Controls
{
    /// <summary>
    /// Prevent having a gradient background in the toolstrip, and the overflow button
    /// See: https://stackoverflow.com/a/16926979
    /// In the dark theme all colors come from the <see cref="EditorTheme"/>, in light and high contrast the system colors stay.
    /// </summary>
    internal class CustomProfessionalColorTable : ProfessionalColorTable
    {
        private static EditorTheme Theme => EditorTheme.Current;

        private static bool IsDark => Theme.IsDark;

        public override Color ToolStripGradientBegin => Theme.Background;

        public override Color ToolStripGradientMiddle => Theme.Background;

        public override Color ToolStripGradientEnd => Theme.Background;

        public override Color OverflowButtonGradientBegin => Theme.Background;

        public override Color OverflowButtonGradientMiddle => Theme.Background;

        public override Color OverflowButtonGradientEnd => Theme.Background;

        public override Color MenuStripGradientBegin => IsDark ? Theme.Background : base.MenuStripGradientBegin;

        public override Color MenuStripGradientEnd => IsDark ? Theme.Background : base.MenuStripGradientEnd;

        public override Color StatusStripGradientBegin => IsDark ? Theme.Background : base.StatusStripGradientBegin;

        public override Color StatusStripGradientEnd => IsDark ? Theme.Background : base.StatusStripGradientEnd;

        public override Color ToolStripDropDownBackground => IsDark ? Theme.MenuBackground : base.ToolStripDropDownBackground;

        public override Color ToolStripBorder => IsDark ? Theme.Border : base.ToolStripBorder;

        public override Color MenuBorder => IsDark ? Theme.Border : base.MenuBorder;

        public override Color ImageMarginGradientBegin => IsDark ? Theme.MenuBackground : base.ImageMarginGradientBegin;

        public override Color ImageMarginGradientMiddle => IsDark ? Theme.MenuBackground : base.ImageMarginGradientMiddle;

        public override Color ImageMarginGradientEnd => IsDark ? Theme.MenuBackground : base.ImageMarginGradientEnd;

        public override Color MenuItemSelected => IsDark ? Theme.Hover : base.MenuItemSelected;

        public override Color MenuItemSelectedGradientBegin => IsDark ? Theme.Hover : base.MenuItemSelectedGradientBegin;

        public override Color MenuItemSelectedGradientEnd => IsDark ? Theme.Hover : base.MenuItemSelectedGradientEnd;

        public override Color MenuItemBorder => IsDark ? Theme.Border : base.MenuItemBorder;

        public override Color MenuItemPressedGradientBegin => IsDark ? Theme.Pressed : base.MenuItemPressedGradientBegin;

        public override Color MenuItemPressedGradientMiddle => IsDark ? Theme.Pressed : base.MenuItemPressedGradientMiddle;

        public override Color MenuItemPressedGradientEnd => IsDark ? Theme.Pressed : base.MenuItemPressedGradientEnd;

        public override Color ButtonSelectedHighlight => IsDark ? Theme.Hover : base.ButtonSelectedHighlight;

        public override Color ButtonSelectedHighlightBorder => IsDark ? Theme.Border : base.ButtonSelectedHighlightBorder;

        public override Color ButtonSelectedBorder => IsDark ? Theme.Border : base.ButtonSelectedBorder;

        public override Color ButtonSelectedGradientBegin => IsDark ? Theme.Hover : base.ButtonSelectedGradientBegin;

        public override Color ButtonSelectedGradientMiddle => IsDark ? Theme.Hover : base.ButtonSelectedGradientMiddle;

        public override Color ButtonSelectedGradientEnd => IsDark ? Theme.Hover : base.ButtonSelectedGradientEnd;

        public override Color ButtonPressedHighlight => IsDark ? Theme.Pressed : base.ButtonPressedHighlight;

        public override Color ButtonPressedHighlightBorder => IsDark ? Theme.Border : base.ButtonPressedHighlightBorder;

        public override Color ButtonPressedBorder => IsDark ? Theme.Border : base.ButtonPressedBorder;

        public override Color ButtonPressedGradientBegin => IsDark ? Theme.Pressed : base.ButtonPressedGradientBegin;

        public override Color ButtonPressedGradientMiddle => IsDark ? Theme.Pressed : base.ButtonPressedGradientMiddle;

        public override Color ButtonPressedGradientEnd => IsDark ? Theme.Pressed : base.ButtonPressedGradientEnd;

        public override Color ButtonCheckedHighlight => IsDark ? Theme.Pressed : base.ButtonCheckedHighlight;

        public override Color ButtonCheckedHighlightBorder => IsDark ? Theme.Border : base.ButtonCheckedHighlightBorder;

        public override Color ButtonCheckedGradientBegin => IsDark ? Theme.Pressed : base.ButtonCheckedGradientBegin;

        public override Color ButtonCheckedGradientMiddle => IsDark ? Theme.Pressed : base.ButtonCheckedGradientMiddle;

        public override Color ButtonCheckedGradientEnd => IsDark ? Theme.Pressed : base.ButtonCheckedGradientEnd;

        public override Color CheckBackground => IsDark ? Theme.Pressed : base.CheckBackground;

        public override Color CheckSelectedBackground => IsDark ? Theme.Hover : base.CheckSelectedBackground;

        public override Color CheckPressedBackground => IsDark ? Theme.Pressed : base.CheckPressedBackground;

        public override Color SeparatorDark => IsDark ? Theme.Border : base.SeparatorDark;

        public override Color SeparatorLight => IsDark ? Theme.Background : base.SeparatorLight;

        public override Color GripDark => IsDark ? Theme.Border : base.GripDark;

        public override Color GripLight => IsDark ? Theme.Background : base.GripLight;
    }

    /// <summary>
    /// ToolStripProfessionalRenderer without having a visual artifact
    /// See: https://stackoverflow.com/a/16926979 and https://stackoverflow.com/a/13418840
    /// The texts and arrows follow the <see cref="EditorTheme"/>.
    /// </summary>
    public class CustomToolStripProfessionalRenderer : ToolStripProfessionalRenderer
    {
        public CustomToolStripProfessionalRenderer() : base(new CustomProfessionalColorTable())
        {
            RoundedEdges = false;
        }

        /// <summary>
        /// By overriding the OnRenderToolStripBorder we can make the ToolStrip without border, opened menus keep theirs
        /// </summary>
        /// <param name="e"></param>
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown)
            {
                base.OnRenderToolStripBorder(e);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            var theme = EditorTheme.Current;
            if (theme.IsDark)
            {
                // Items with their own color (e.g. the HDR badge on its yellow background) keep it
                if (!e.Item.Enabled)
                {
                    e.TextColor = theme.Muted;
                }
                else if (e.TextColor == SystemColors.ControlText || e.TextColor == SystemColors.MenuText || e.TextColor == SystemColors.WindowText)
                {
                    e.TextColor = theme.Foreground;
                }
            }

            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            var theme = EditorTheme.Current;
            if (theme.IsDark)
            {
                e.ArrowColor = e.Item?.Enabled == false ? theme.Muted : theme.Foreground;
            }

            base.OnRenderArrow(e);
        }
    }
}
