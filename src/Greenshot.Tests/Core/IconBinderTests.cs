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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class IconBinderTests
    {
        private static IconSource CloseIcon => IconSource.FromKey(DestinationIcons.Resource("Close.Image"));

        private static bool IsDisposed(Image image)
        {
            try
            {
                _ = image.Width;
                return false;
            }
            catch (ArgumentException)
            {
                return true;
            }
        }

        [Fact]
        public void Bind_ItemOnAToolStrip_GetsTheIconInTheSizeOfTheToolStrip()
        {
            using var toolStrip = new ToolStrip();
            IconBinder.Apply(toolStrip, 24);
            var button = new ToolStripButton();
            toolStrip.Items.Add(button);

            IconBinder.Bind(button, CloseIcon);

            Assert.Equal(new Size(24, 24), button.Image.Size);
            Assert.Equal(ToolStripItemImageScaling.None, button.ImageScaling);
        }

        [Fact]
        public void Bind_BeforeTheItemIsAdded_GetsTheSizeWhenAdded()
        {
            using var toolStrip = new ToolStrip();
            IconBinder.Apply(toolStrip, 28);
            var button = new ToolStripButton();

            IconBinder.Bind(button, CloseIcon);
            toolStrip.Items.Add(button);

            Assert.Equal(new Size(28, 28), button.Image.Size);
        }

        [Fact]
        public void Apply_AnotherSize_ReplacesTheIconAndDisposesTheOldCopy()
        {
            using var toolStrip = new ToolStrip();
            IconBinder.Apply(toolStrip, 16);
            var button = new ToolStripButton();
            toolStrip.Items.Add(button);
            IconBinder.Bind(button, CloseIcon);
            var before = button.Image;

            IconBinder.Apply(toolStrip, 32);

            Assert.Equal(new Size(32, 32), button.Image.Size);
            Assert.Equal(new Size(32, 32), toolStrip.ImageScalingSize);
            Assert.True(IsDisposed(before));
        }

        [Fact]
        public void Apply_ReachesTheItemsOfDropDowns()
        {
            using var menuStrip = new MenuStrip();
            var menu = new ToolStripMenuItem("Menu");
            var child = new ToolStripMenuItem("Child");
            menu.DropDownItems.Add(child);
            menuStrip.Items.Add(menu);
            IconBinder.Bind(child, CloseIcon);

            IconBinder.Apply(menuStrip, 36);

            Assert.Equal(new Size(36, 36), child.Image.Size);
            Assert.Equal(new Size(36, 36), menu.DropDown.ImageScalingSize);
        }

        [Fact]
        public void BindLike_GivesEachItemItsOwnCopy()
        {
            using var toolStrip = new ToolStrip();
            IconBinder.Apply(toolStrip, 24);
            var item = new ToolStripButton();
            var dropDownButton = new ToolStripDropDownButton();
            toolStrip.Items.Add(item);
            toolStrip.Items.Add(dropDownButton);
            IconBinder.Bind(item, CloseIcon);

            Assert.True(IconBinder.BindLike(dropDownButton, item));
            Assert.NotSame(item.Image, dropDownButton.Image);

            // The icon of the drop down button stays valid when the other item goes away
            item.Dispose();
            Assert.False(IsDisposed(dropDownButton.Image));
        }

        [Fact]
        public void DisposingTheItem_DisposesItsCopy()
        {
            using var toolStrip = new ToolStrip();
            IconBinder.Apply(toolStrip, 24);
            var button = new ToolStripButton();
            toolStrip.Items.Add(button);
            IconBinder.Bind(button, CloseIcon);
            var image = button.Image;

            button.Dispose();

            Assert.True(IsDisposed(image));
        }

        [Fact]
        public void OwnedImage_IsScaledAndDisposedWithTheItem()
        {
            using var toolStrip = new ToolStrip();
            IconBinder.Apply(toolStrip, 24);
            var button = new ToolStripButton();
            toolStrip.Items.Add(button);
            var windowIcon = new Bitmap(16, 16, PixelFormat.Format32bppArgb);

            IconBinder.Bind(button, IconSource.FromImage(windowIcon));

            Assert.Equal(new Size(24, 24), button.Image.Size);
            button.Dispose();
            Assert.True(IsDisposed(windowIcon));
        }

        [Fact]
        public void Decorator_DrawsOnEveryNewCopy()
        {
            using var toolStrip = new ToolStrip();
            IconBinder.Apply(toolStrip, 16);
            var button = new DecoratedButton();
            toolStrip.Items.Add(button);
            IconBinder.Bind(button, CloseIcon);
            IconBinder.Apply(toolStrip, 32);

            Assert.Equal(2, button.Decorated);
            Assert.Equal(Color.Red.ToArgb(), ((Bitmap)button.Image).GetPixel(31, 31).ToArgb());
        }

        private sealed class DecoratedButton : ToolStripButton, IIconDecorator
        {
            public int Decorated { get; private set; }

            public void DecorateIcon(Image icon)
            {
                Decorated++;
                ((Bitmap)icon).SetPixel(icon.Width - 1, icon.Height - 1, Color.Red);
            }
        }
    }
}
