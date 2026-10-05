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
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// Keeps the icons of tool strip items in the size of their tool strip.
    /// An item is bound to an <see cref="IconSource"/> instead of getting an image; <see cref="Apply"/> sets the size of a tool strip
    /// (and its drop downs) and gives every bound item a copy of its icon in exactly that size, so nothing is blurred by the tool strip.
    /// Every copy belongs to its item: the binder disposes it when the item gets another one or is disposed, never an image someone else uses.
    /// Only use it on the UI thread.
    /// </summary>
    public static class IconBinder
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IconBinder));
        // Weak, so items which are removed from a menu without being disposed can still be collected (as before the binder)
        private static readonly ConditionalWeakTable<ToolStripItem, Binding> Bindings = new ConditionalWeakTable<ToolStripItem, Binding>();
        private static readonly ConditionalWeakTable<ToolStripDropDownItem, object> HookedDropDownItems = new ConditionalWeakTable<ToolStripDropDownItem, object>();

        /// <summary>
        /// The cache the icons come from, the tests use their own
        /// </summary>
        public static SizedIconCache Cache { get; set; } = SizedIconCache.Default;

        private sealed class Binding
        {
            public IconSource Source;
            // The original of an owned image (e.g. a window icon), these don't go into the shared cache
            public Image OwnedOriginal;
            public Image Image;
            public int PixelSize;
            public bool Loading;
        }

        /// <summary>
        /// Show the icon of the source on the item, in the size of the tool strip the item is on.
        /// Binding the same source again does nothing, null removes the icon.
        /// </summary>
        /// <param name="item">ToolStripItem</param>
        /// <param name="source">IconSource</param>
        public static void Bind(ToolStripItem item, IconSource source)
        {
            if (item == null)
            {
                return;
            }

            if (source == null)
            {
                Unbind(item);
                item.Image = null;
                return;
            }

            if (Bindings.TryGetValue(item, out var binding))
            {
                if (binding.Source.Key == source.Key)
                {
                    return;
                }

                ReleaseOwned(binding);
                binding.Source = source;
                binding.PixelSize = 0;
            }
            else
            {
                binding = new Binding { Source = source };
                Bindings.Add(item, binding);
                item.Disposed += OnItemDisposed;
                // Items are often bound before they are added to a menu: they get the size of the menu when they are added
                item.OwnerChanged += OnItemOwnerChanged;
                if (item is ToolStripDropDownItem dropDownItem)
                {
                    HookDropDown(dropDownItem);
                }
            }

            // The image already has the right size, the tool strip must not scale it again
            item.ImageScaling = ToolStripItemImageScaling.None;
            Assign(item, binding, SizeOf(item));
        }

        /// <summary>
        /// Show the same icon as another item (e.g. a drop down button which shows the selected item), each item gets its own copy.
        /// </summary>
        /// <param name="target">ToolStripItem which gets the icon</param>
        /// <param name="from">ToolStripItem which has the icon</param>
        /// <returns>true when the icon of the other item is bound, false when the caller has to deal with the image itself</returns>
        public static bool BindLike(ToolStripItem target, ToolStripItem from)
        {
            if (target == null || from == null || !Bindings.TryGetValue(from, out var binding) || binding.Source.IsOwnedImage)
            {
                return false;
            }

            Bind(target, binding.Source);
            return true;
        }

        /// <summary>
        /// The item doesn't show a bound icon anymore, the copy it had is removed and disposed.
        /// </summary>
        /// <param name="item">ToolStripItem</param>
        public static void Unbind(ToolStripItem item)
        {
            if (item == null || !Bindings.TryGetValue(item, out var binding))
            {
                return;
            }

            Bindings.Remove(item);
            item.Disposed -= OnItemDisposed;
            item.OwnerChanged -= OnItemOwnerChanged;
            var image = binding.Image;
            if (image != null && ReferenceEquals(item.Image, image))
            {
                item.Image = null;
            }

            image?.Dispose();
            ReleaseOwned(binding);
        }

        /// <summary>
        /// Show the icons of the tool strip, and of the drop downs of its items, in the size.
        /// Drop downs which are filled later get the size when they open.
        /// </summary>
        /// <param name="toolStrip">ToolStrip</param>
        /// <param name="pixelSize">int with the width and height of the icons</param>
        public static void Apply(ToolStrip toolStrip, int pixelSize)
        {
            if (toolStrip == null || toolStrip.IsDisposed || pixelSize <= 0)
            {
                return;
            }

            toolStrip.SuspendLayout();
            try
            {
                toolStrip.ImageScalingSize = new Size(pixelSize, pixelSize);
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    if (Bindings.TryGetValue(item, out var binding))
                    {
                        Assign(item, binding, pixelSize);
                    }

                    if (item is not ToolStripDropDownItem dropDownItem)
                    {
                        continue;
                    }

                    HookDropDown(dropDownItem);
                    if (dropDownItem.HasDropDownItems)
                    {
                        Apply(dropDownItem.DropDown, pixelSize);
                    }
                }
            }
            finally
            {
                toolStrip.ResumeLayout(true);
            }
        }

        /// <summary>
        /// Show the icons of the menu in the size for the display at the location, and log which size that is
        /// </summary>
        /// <param name="toolStrip">ToolStrip</param>
        /// <param name="dpi">int with the DPI of the display the tool strip is shown on</param>
        /// <param name="name">string for the log</param>
        /// <returns>int with the size of the icons in pixels</returns>
        public static int ApplyForDpi(ToolStrip toolStrip, int dpi, string name)
        {
            int baseSize = IconSizing.ConfiguredBaseSize;
            int pixelSize = IconSizing.PixelSize(baseSize, dpi);
            if (toolStrip != null && toolStrip.ImageScalingSize.Height != pixelSize)
            {
                Log.Info($"Icons for {name}: dpi {dpi}, base {baseSize} -> {pixelSize} px");
            }

            Apply(toolStrip, pixelSize);
            return pixelSize;
        }

        /// <summary>
        /// Items which are added to a drop down when it opens (e.g. the window list) get the size of the menu they belong to
        /// </summary>
        private static void HookDropDown(ToolStripDropDownItem dropDownItem)
        {
            if (HookedDropDownItems.TryGetValue(dropDownItem, out _))
            {
                return;
            }

            HookedDropDownItems.Add(dropDownItem, null);
            // Registered after the handlers which fill the drop down, so it runs after them
            dropDownItem.DropDownOpening += (sender, _) =>
            {
                if (sender is ToolStripDropDownItem { Owner: { } owner } openingItem)
                {
                    Apply(openingItem.DropDown, owner.ImageScalingSize.Height);
                }
            };
        }

        /// <summary>
        /// The size of the tool strip the item is on, the configured size at 100% when it isn't on one yet
        /// </summary>
        private static int SizeOf(ToolStripItem item)
        {
            var owner = item.Owner;
            if (owner != null && owner.ImageScalingSize.Height > 0)
            {
                return owner.ImageScalingSize.Height;
            }

            return IconSizing.PixelSize(IconSizing.DefaultDpi);
        }

        private static void Assign(ToolStripItem item, Binding binding, int pixelSize)
        {
            if (item.IsDisposed)
            {
                return;
            }

            if (binding.PixelSize == pixelSize && binding.Image != null && ReferenceEquals(item.Image, binding.Image))
            {
                return;
            }

            binding.PixelSize = pixelSize;
            var copy = binding.Source.IsOwnedImage ? ScaleOwned(binding, pixelSize) : Cache.GetCopy(binding.Source, pixelSize);
            if (copy == null)
            {
                if (Cache.NeedsLoading(binding.Source, pixelSize))
                {
                    StartLoading(item, binding, pixelSize);
                }

                return;
            }

            if (item is IIconDecorator decorator)
            {
                decorator.DecorateIcon(copy);
            }

            var previous = binding.Image;
            binding.Image = copy;
            item.Image = copy;
            previous?.Dispose();
        }

        private static void StartLoading(ToolStripItem item, Binding binding, int pixelSize)
        {
            if (binding.Loading)
            {
                return;
            }

            binding.Loading = true;
            var source = binding.Source;
            LoadAndAssignAsync().FireAndLog($"Load icon {source}", Log);

            async System.Threading.Tasks.Task LoadAndAssignAsync()
            {
                try
                {
                    // Continues on the UI thread
                    await Cache.LoadAsync(source, pixelSize).ConfigureAwait(true);
                }
                finally
                {
                    binding.Loading = false;
                }

                if (item.IsDisposed || !Bindings.TryGetValue(item, out var current) || !ReferenceEquals(current, binding) || binding.Source != source)
                {
                    return;
                }

                // The size which is wanted now, the tool strip might have changed in the meantime (another size can need another original)
                int wanted = binding.PixelSize;
                binding.PixelSize = 0;
                Assign(item, binding, wanted);
            }
        }

        private static void OnItemOwnerChanged(object sender, EventArgs e)
        {
            if (sender is ToolStripItem { Owner: not null } item && Bindings.TryGetValue(item, out var binding))
            {
                Assign(item, binding, SizeOf(item));
            }
        }

        private static void OnItemDisposed(object sender, EventArgs e)
        {
            if (sender is not ToolStripItem item || !Bindings.TryGetValue(item, out var binding))
            {
                return;
            }

            Bindings.Remove(item);
            binding.Image?.Dispose();
            binding.Image = null;
            ReleaseOwned(binding);
        }

        private static Image ScaleOwned(Binding binding, int pixelSize)
        {
            binding.OwnedOriginal ??= binding.Source.Load(pixelSize);
            return binding.OwnedOriginal == null ? null : SizedIconCache.Scale(binding.OwnedOriginal, pixelSize);
        }

        private static void ReleaseOwned(Binding binding)
        {
            if (!binding.Source.IsOwnedImage)
            {
                return;
            }

            binding.Source.ReleaseOwnedImage();
            binding.OwnedOriginal?.Dispose();
            binding.OwnedOriginal = null;
        }
    }
}
