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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Base.Wpf;

namespace Greenshot.Plugin.RecipeEditor.Dialogs
{
    /// <summary>
    /// Interaction logic for SelectEditorDialog.xaml
    /// </summary>
    public partial class SelectEditorDialog : Window
    {
        public class EditorItem
        {
            public IImageEditor Editor { get; }
            public string Title { get; }
            public string Dimensions { get; }
            public int ElementCount { get; }
            public string Description { get; }
            public ImageSource PreviewImage { get; }

            public EditorItem(IImageEditor editor)
            {
                Editor = editor;
                Title = editor.CaptureDetails?.Title ?? editor.Surface?.CaptureDetails?.Title ?? "Untitled Image";
                ElementCount = editor.Surface?.Elements?.Count ?? 0;
                int w = editor.Surface?.Image?.Width ?? 0;
                int h = editor.Surface?.Image?.Height ?? 0;
                Dimensions = (w > 0 && h > 0) ? $"{w} × {h} px" : "Unknown size";
                Description = $"{Dimensions} • {ElementCount} annotation{(ElementCount == 1 ? "" : "s")}";
                PreviewImage = CreatePreview(editor);
            }

            private static ImageSource CreatePreview(IImageEditor editor)
            {
                if (editor?.Surface == null) return null;
                System.Drawing.Image exported = null;
                try
                {
                    try
                    {
                        exported = editor.GetImageForExport();
                    }
                    catch
                    {
                        // Fallback to raw surface image if export fails
                    }

                    var sourceImg = exported ?? editor.Surface.Image;
                    if (sourceImg == null) return null;

                    using var ms = new MemoryStream();
                    try
                    {
                        sourceImg.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    catch
                    {
                        // Fallback for special pixel formats
                        using var bmp = new Bitmap(sourceImg.Width, sourceImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        using (var g = Graphics.FromImage(bmp))
                        {
                            g.DrawImage(sourceImg, 0, 0, sourceImg.Width, sourceImg.Height);
                        }
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    }

                    ms.Position = 0;
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = ms;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
                catch
                {
                    return null;
                }
                finally
                {
                    exported?.Dispose();
                }
            }
        }

        public IImageEditor SelectedEditor { get; private set; }

        public SelectEditorDialog(IEnumerable<IImageEditor> editors)
        {
            InitializeComponent();

            var items = editors?.Select(e => new EditorItem(e)).ToList() ?? new List<EditorItem>();
            EditorListBox.ItemsSource = items;

            SourceInitialized += (s, e) => ApplyImmersiveDarkMode();
            Loaded += (s, e) =>
            {
                Activate();
                Focus();
            };

            if (items.Count > 0)
            {
                EditorListBox.SelectedIndex = 0;
            }
            else
            {
                UpdatePreview();
            }
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void OnCloseTitleBarClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void EditorListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (EditorListBox.SelectedItem is EditorItem item)
            {
                PreviewTitleTextBlock.Text = item.Title;
                PreviewDimensionsBadge.Text = item.Dimensions;
                PreviewCountBadge.Text = $"{item.ElementCount} annotation{(item.ElementCount == 1 ? "" : "s")}";
                PreviewImage.Source = item.PreviewImage;
                PreviewImage.Visibility = item.PreviewImage != null ? Visibility.Visible : Visibility.Collapsed;
                NoPreviewTextBlock.Visibility = item.PreviewImage == null ? Visibility.Visible : Visibility.Collapsed;
                StatusTextBlock.Text = $"Editor {EditorListBox.SelectedIndex + 1} of {EditorListBox.Items.Count} selected";
                OkButton.IsEnabled = true;
            }
            else
            {
                PreviewTitleTextBlock.Text = "No editor selected";
                PreviewDimensionsBadge.Text = "0 × 0 px";
                PreviewCountBadge.Text = "0 annotations";
                PreviewImage.Source = null;
                PreviewImage.Visibility = Visibility.Collapsed;
                NoPreviewTextBlock.Visibility = Visibility.Visible;
                StatusTextBlock.Text = "No editor selected";
                OkButton.IsEnabled = false;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (EditorListBox.SelectedItem is EditorItem item)
            {
                SelectedEditor = item.Editor;
                DialogResult = true;
            }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void EditorListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EditorListBox.SelectedItem is EditorItem item)
            {
                SelectedEditor = item.Editor;
                DialogResult = true;
                Close();
            }
        }

        private void ApplyImmersiveDarkMode()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    int useImmersiveDarkMode = WpfThemeHelper.IsDarkMode ? 1 : 0;
                    int hr = DwmSetWindowAttribute(helper.Handle, 20, ref useImmersiveDarkMode, sizeof(int));
                    if (hr != 0)
                    {
                        DwmSetWindowAttribute(helper.Handle, 19, ref useImmersiveDarkMode, sizeof(int));
                    }
                }
            }
            catch
            {
                // Silently ignore if DWM call is unsupported
            }
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}
