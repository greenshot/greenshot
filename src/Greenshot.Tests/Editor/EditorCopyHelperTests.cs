/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Windows.Forms;
using Greenshot.Editor.Helpers;
using Xunit;

namespace Greenshot.Tests.Editor
{
    public class EditorCopyHelperTests
    {
        [Fact]
        public void GetCopyTarget_WithSelection_CopiesTheElements()
        {
            Assert.Equal(EditorCopyTarget.SelectedElements, EditorCopyHelper.GetCopyTarget(hasSelectedElements: true));
        }

        [Fact]
        public void GetCopyTarget_WithoutSelection_CopiesTheImage()
        {
            Assert.Equal(EditorCopyTarget.Image, EditorCopyHelper.GetCopyTarget(hasSelectedElements: false));
        }

        [Fact]
        public void IsTextInput_TextBoxes_AreTextInput()
        {
            using var textBox = new TextBox();
            using var richTextBox = new RichTextBox();
            using var maskedTextBox = new MaskedTextBox();

            Assert.True(EditorCopyHelper.IsTextInput(textBox));
            Assert.True(EditorCopyHelper.IsTextInput(richTextBox));
            Assert.True(EditorCopyHelper.IsTextInput(maskedTextBox));
        }

        [Theory]
        [InlineData(ComboBoxStyle.DropDown, true)]
        [InlineData(ComboBoxStyle.Simple, true)]
        [InlineData(ComboBoxStyle.DropDownList, false)]
        public void IsTextInput_ComboBox_DependsOnTheStyle(ComboBoxStyle style, bool expected)
        {
            using var comboBox = new ComboBox { DropDownStyle = style };

            Assert.Equal(expected, EditorCopyHelper.IsTextInput(comboBox));
        }

        [Fact]
        public void IsTextInput_OtherControlsAndNull_AreNotTextInput()
        {
            using var panel = new Panel();
            using var button = new Button();

            Assert.False(EditorCopyHelper.IsTextInput(panel));
            Assert.False(EditorCopyHelper.IsTextInput(button));
            Assert.False(EditorCopyHelper.IsTextInput(null));
        }
    }
}
