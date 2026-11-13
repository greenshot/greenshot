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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.Editor.Standalone.Settings;
using Greenshot.Editor.Standalone.Controls;
using Greenshot.Editor.Standalone.Diagnostics;
using Greenshot.Editor.Standalone.Files;
using Greenshot.Editor.Standalone.Stamps;
using Greenshot.Editor.Standalone.Tools;
using Greenshot.Editor.Standalone.ViewModels;

namespace Greenshot.Editor.Standalone.Views
{
    /// <summary>
    /// The window of the editor. It gives the view model its dialogs, the clipboard and the zoom of the canvas, and maps the
    /// keys of the tools and the canvas (which don't go through key bindings, so they don't fire while a text box has the focus).
    /// </summary>
    public partial class EditorWindow : Window, IEditorView
    {
        // Tells a paste that the image on the clipboard came with elements this editor copied
        private static readonly DataFormat<string> MarkerFormat = DataFormat.CreateStringApplicationFormat("greenshot-editor-elements");
        private readonly EditorWindowViewModel _viewModel;
        private bool _closing;
        private bool _refreshing;

        /// <summary>
        /// For the XAML loader and the previewer of the IDE
        /// </summary>
        public EditorWindow() : this(new EditorWindowViewModel())
        {
        }

        public EditorWindow(EditorWindowViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
            DataContext = _viewModel;
            _viewModel.Attach(this);
            var settings = EditorSettings.Current;
            if (settings.WindowWidth >= MinWidth && settings.WindowHeight >= MinHeight)
            {
                Width = settings.WindowWidth;
                Height = settings.WindowHeight;
            }

            if (settings.WindowMaximized)
            {
                WindowState = WindowState.Maximized;
            }

            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DropEvent, OnDrop);
            // Before the controls: the keys of the tools work wherever the focus is, except while text is typed
            AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            Activated += (_, _) => RefreshGreenshot();
        }

        public EditorWindowViewModel ViewModel => _viewModel;

        public PointD VisibleCenter => Canvas.VisibleCenter;

        private static FilePickerFileType DocumentType => new(EditorTexts.Text.DocumentsFilter) { Patterns = new[] { "*" + DocumentFiles.DocumentExtension } };

        private static FilePickerFileType ImageType => new(EditorTexts.Text.ImagesFilter) { Patterns = DocumentFiles.ImageExtensions.Select(extension => "*" + extension).ToList() };

        private async Task<IStorageFolder> StartFolderAsync(string directory)
        {
            directory ??= EditorSettings.Current.LastDirectory;
            return string.IsNullOrEmpty(directory) || !Directory.Exists(directory) ? null : await StorageProvider.TryGetFolderFromPathAsync(directory);
        }

        public async Task<string> AskOpenPathAsync(bool imagesOnly)
        {
            var all = new FilePickerFileType(EditorTexts.Text.SupportedFilter) { Patterns = DocumentType.Patterns.Concat(ImageType.Patterns).ToList() };
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                SuggestedStartLocation = await StartFolderAsync(null),
                FileTypeFilter = imagesOnly ? new[] { ImageType } : new[] { all, DocumentType, ImageType }
            });
            string path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
            if (path != null)
            {
                EditorSettings.Current.LastDirectory = Path.GetDirectoryName(path);
            }

            return path;
        }

        public async Task<string> AskSavePathAsync(string fileName, string directory)
        {
            FilePickerFileType Type(string name, params string[] patterns) => new(name) { Patterns = patterns };
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = fileName,
                SuggestedStartLocation = await StartFolderAsync(directory),
                DefaultExtension = DocumentFiles.DocumentExtension.TrimStart('.'),
                ShowOverwritePrompt = true,
                FileTypeChoices = new[]
                {
                    DocumentType, Type("PNG", "*.png"), Type("JPEG", "*.jpg", "*.jpeg"), Type("BMP", "*.bmp"), Type("GIF", "*.gif"), Type("TIFF", "*.tif", "*.tiff"), Type("WebP", "*.webp")
                }
            });
            return file?.TryGetLocalPath();
        }

        public Task<int> AskSaveChangesAsync(string title) => MessageDialog.ShowAsync(this, EditorTexts.Text.Title, string.Format(EditorTexts.Text.SaveChanges, title),
            new[] { EditorTexts.Text.SaveButton, EditorTexts.Text.DontSaveButton, EditorTexts.Text.CancelButton }, 0, 2);

        public Task ShowErrorAsync(string text) => MessageDialog.ShowAsync(this, EditorTexts.Text.Title, text, new[] { EditorTexts.Text.OkButton });

        public void Zoom(ZoomCommand command)
        {
            var tab = _viewModel.SelectedTab;
            if (tab == null)
            {
                return;
            }

            switch (command)
            {
                case ZoomCommand.In:
                    Canvas.ZoomTo(DocumentCanvas.NextZoom(tab.Zoom, true));
                    break;
                case ZoomCommand.Out:
                    Canvas.ZoomTo(DocumentCanvas.NextZoom(tab.Zoom, false));
                    break;
                case ZoomCommand.BestFit:
                    Canvas.ZoomBestFit();
                    break;
                default:
                    Canvas.ZoomTo(1);
                    break;
            }
        }

        public async Task CopyImageAsync(byte[] png, string marker)
        {
            if (Clipboard is not IClipboard clipboard)
            {
                return;
            }

            using var stream = new MemoryStream(png, false);
            // The clipboard asks for the bitmap when another program pastes: it lives until the next copy
            var bitmap = new Bitmap(stream);
            var item = new DataTransferItem();
            item.SetBitmap(bitmap);
            if (marker != null)
            {
                item.Set(MarkerFormat, marker);
            }

            var data = new DataTransfer();
            data.Add(item);
            await clipboard.SetDataAsync(data);
        }

        public async Task<(byte[] Image, string Marker)> ReadClipboardAsync()
        {
            if (Clipboard is not IClipboard clipboard)
            {
                return (null, null);
            }

            try
            {
                using var data = await clipboard.TryGetDataAsync();
                if (data == null)
                {
                    return (null, null);
                }

                string marker = data.Contains(MarkerFormat) ? await data.TryGetValueAsync(MarkerFormat) : null;
                if (data.Contains(DataFormat.File) && await data.TryGetFilesAsync() is { } files
                    && files.Select(file => file.TryGetLocalPath()).FirstOrDefault(path => path != null && IsImageFile(path)) is string imageFile)
                {
                    return (await File.ReadAllBytesAsync(imageFile), marker);
                }

                if (data.Contains(DataFormat.Bitmap))
                {
                    using var bitmap = await data.TryGetBitmapAsync();
                    if (bitmap != null)
                    {
                        using var stream = new MemoryStream();
                        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
                        return (stream.ToArray(), marker);
                    }
                }

                return (null, marker);
            }
            catch (Exception ex)
            {
                // Another program holds the clipboard, or what is on it can't be read as an image
                EditorLog.Warn("The clipboard could not be read", ex);
                return (null, null);
            }
        }

        public void BringToFront()
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
        }

        /// <summary>
        /// Asks Greenshot for its destinations, at most one question at a time
        /// </summary>
        private void RefreshGreenshot()
        {
            if (EditorSession.Current.Greenshot is { } greenshot && !_refreshing)
            {
                _refreshing = true;
                RefreshAsync().Forget("Asking Greenshot for its destinations");
            }

            async Task RefreshAsync()
            {
                try
                {
                    await EditorSession.Current.Greenshot.RefreshAsync();
                }
                finally
                {
                    _refreshing = false;
                }
            }
        }

        private void SendTo_Click(object sender, RoutedEventArgs e)
        {
            var menu = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedLeft };
            foreach (var destination in _viewModel.Destinations)
            {
                menu.Items.Add(new MenuItem { Header = destination.Name, Command = _viewModel.SendToCommand, CommandParameter = destination });
            }

            if (menu.Items.Count == 0)
            {
                menu.Items.Add(new MenuItem { Header = EditorTexts.Text.SendToUnavailable, IsEnabled = false });
                RefreshGreenshot();
            }

            menu.ShowAt(SendToButton);
        }

        private void StampsFlyout_Opening(object sender, EventArgs e) => _viewModel.Stamps.Refresh();

        private void Stamp_Click(object sender, RoutedEventArgs e) => StampsButton.Flyout?.Hide();

        private void Emoji_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Content: string emoji })
            {
                _viewModel.Properties.Emoji = emoji;
            }
        }

        private void EmojiBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox box)
            {
                _viewModel.Properties.Emoji = box.Text;
                e.Handled = true;
            }
        }

        /// <summary>
        /// The right button selects the element under it (when it isn't selected) and shows what can be done with it
        /// </summary>
        private void Canvas_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var tab = _viewModel.SelectedTab;
            if (tab == null || e.InitialPressMouseButton != MouseButton.Right)
            {
                return;
            }

            if (tab.ToolKind == ToolKind.Select)
            {
                var point = Canvas.ToCanvas(e.GetPosition(Canvas));
                var hit = HitTesting.ElementAt(tab.Document, point, 3 / tab.Zoom);
                if (hit != null && !tab.Selection.Contains(hit.Id))
                {
                    tab.Select(DocumentEdits.WithSets(tab.Document, new[] { hit.Id }));
                }
            }

            var menu = new MenuFlyout { Placement = PlacementMode.Pointer };
            void Add(string text, string icon, System.Windows.Input.ICommand command, object parameter = null) => menu.Items.Add(new MenuItem
            {
                Header = text,
                Icon = icon == null ? null : new Image { Source = EditorIcons.Get(icon), Width = 16, Height = 16 },
                Command = command,
                CommandParameter = parameter
            });

            Add(EditorTexts.Text.Cut, "Cut", _viewModel.CutCommand);
            Add(EditorTexts.Text.Copy, "Copy", _viewModel.CopyCommand);
            Add(EditorTexts.Text.Paste, "Paste", _viewModel.PasteCommand);
            Add(EditorTexts.Text.Duplicate, null, _viewModel.DuplicateCommand);
            Add(EditorTexts.Text.Delete, "Delete", _viewModel.DeleteCommand);
            menu.Items.Add(new Separator());
            Add(EditorTexts.Text.BringToFront, "BringToFront", _viewModel.ArrangeCommand, Arrangement.ToFront);
            Add(EditorTexts.Text.BringForward, "BringForward", _viewModel.ArrangeCommand, Arrangement.Forward);
            Add(EditorTexts.Text.SendBackward, "SendBackward", _viewModel.ArrangeCommand, Arrangement.Backward);
            Add(EditorTexts.Text.SendToBack, "SendToBack", _viewModel.ArrangeCommand, Arrangement.ToBack);
            menu.Items.Add(new Separator());
            Add(EditorTexts.Text.StampsFromSelection, null, _viewModel.SelectionToStampCommand);
            Add(EditorTexts.Text.SelectAll, null, _viewModel.SelectAllCommand);
            menu.ShowAt(Canvas, true);
            e.Handled = true;
        }

        /// <summary>
        /// True while keys are text: in a text box, a number box or an open list
        /// </summary>
        private bool IsTyping => FocusManager?.GetFocusedElement() is TextBox or ComboBox or ComboBoxItem or NumericUpDown;

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var tab = _viewModel.SelectedTab;
            if (e.Handled || tab == null || IsTyping || tab.EditingText != null)
            {
                return;
            }

            bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift), control = e.KeyModifiers.HasFlag(KeyModifiers.Control), alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
            // Copy, paste and undo are keys of the document here; in a text box they stay keys of the text
            if (control && !alt && EditKey(e.Key, shift) is System.Windows.Input.ICommand edit)
            {
                if (edit.CanExecute(null))
                {
                    edit.Execute(null);
                }

                e.Handled = true;
                return;
            }

            ToolKey? toolKey = e.Key switch
            {
                Key.Escape => ToolKey.Escape,
                Key.Enter => ToolKey.Enter,
                Key.Left => ToolKey.Left,
                Key.Right => ToolKey.Right,
                Key.Up => ToolKey.Up,
                Key.Down => ToolKey.Down,
                _ => null
            };
            // The arrow keys move the selection only while the canvas has the focus, elsewhere they move in the controls
            bool arrow = toolKey is ToolKey.Left or ToolKey.Right or ToolKey.Up or ToolKey.Down;
            bool inPanel = arrow && FocusManager?.GetFocusedElement() is Control focused && !ReferenceEquals(focused, Canvas);
            if (toolKey is ToolKey pressed && !inPanel && tab.Key(pressed, shift, control))
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape && tab.ToolKind != ToolKind.Select)
            {
                tab.ToolKind = ToolKind.Select;
                e.Handled = true;
                return;
            }

            if (control || alt || e.KeyModifiers.HasFlag(KeyModifiers.Meta))
            {
                return;
            }

            Arrangement? arrangement = e.Key switch
            {
                Key.Home => Arrangement.ToFront,
                Key.End => Arrangement.ToBack,
                Key.PageUp => Arrangement.Forward,
                Key.PageDown => Arrangement.Backward,
                _ => null
            };
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                tab.DeleteSelection();
                e.Handled = true;
            }
            else if (arrangement is Arrangement arrange)
            {
                tab.Arrange(arrange);
                e.Handled = true;
            }
            else if (ToolOf(e.Key) is ToolKind kind)
            {
                tab.ToolKind = kind;
                e.Handled = true;
            }
        }

        private System.Windows.Input.ICommand EditKey(Key key, bool shift) => key switch
        {
            Key.Z => shift ? _viewModel.RedoCommand : _viewModel.UndoCommand,
            Key.Y => _viewModel.RedoCommand,
            Key.X => _viewModel.CutCommand,
            Key.C when !shift => _viewModel.CopyCommand,
            Key.V => _viewModel.PasteCommand,
            Key.D => _viewModel.DuplicateCommand,
            Key.A => _viewModel.SelectAllCommand,
            _ => null
        };

        /// <summary>
        /// The keys of the tools, the same as in the editor of Greenshot
        /// </summary>
        private static ToolKind? ToolOf(Key key) => key switch
        {
            Key.R => ToolKind.Rectangle,
            Key.E => ToolKind.Ellipse,
            Key.L => ToolKind.Line,
            Key.A => ToolKind.Arrow,
            Key.F => ToolKind.Freehand,
            Key.T => ToolKind.Text,
            Key.S => ToolKind.SpeechBubble,
            Key.I => ToolKind.StepLabel,
            Key.J => ToolKind.Emoji,
            Key.H => ToolKind.Highlight,
            Key.O => ToolKind.Obfuscate,
            Key.U => ToolKind.CutMark,
            Key.C => ToolKind.Crop,
            _ => null
        };

        private static bool IsImageFile(string path) => DocumentFiles.ImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase) && File.Exists(path);

        private static IReadOnlyList<string> DroppedFiles(DragEventArgs e) =>
            (e.DataTransfer.TryGetFiles() ?? Array.Empty<IStorageItem>()).Select(file => file.TryGetLocalPath())
            .Where(path => path != null && (DocumentFiles.IsDocument(path) || IsImageFile(path))).ToList();

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.DragEffects = DroppedFiles(e).Count > 0 ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// A dropped document opens in a new tab, a dropped image is added to the document (with Ctrl, or without a document, it opens too)
        /// </summary>
        private void OnDrop(object sender, DragEventArgs e)
        {
            var files = DroppedFiles(e);
            bool open = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            var point = Canvas.ToCanvas(e.GetPosition(Canvas));
            e.Handled = true;
            DropAsync().Forget("Opening dropped files");

            async Task DropAsync()
            {
                foreach (string file in files)
                {
                    if (DocumentFiles.IsDocument(file) || _viewModel.SelectedTab == null || open)
                    {
                        await _viewModel.OpenFileAsync(file);
                        continue;
                    }

                    try
                    {
                        var resource = DocumentFiles.ImageResource(await File.ReadAllBytesAsync(file), out int width, out int height);
                        _viewModel.SelectedTab.AddImage(resource, width, height, point);
                    }
                    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
                    {
                        await ShowErrorAsync(string.Format(EditorTexts.Text.OpenFailed, file, ex.Message));
                    }
                }
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            if (e.Cancel || _closing)
            {
                return;
            }

            if (WindowState == WindowState.Normal)
            {
                EditorSettings.Current.WindowWidth = (int)Width;
                EditorSettings.Current.WindowHeight = (int)Height;
            }

            EditorSettings.Current.WindowMaximized = WindowState == WindowState.Maximized;
            if (_viewModel.Tabs.Count == 0)
            {
                return;
            }

            // Asking to save takes a dialog: the window stays until every tab was closed, then it closes for real
            e.Cancel = true;
            CloseTabsAsync().Forget("Closing the tabs");

            async Task CloseTabsAsync()
            {
                if (await _viewModel.CloseAllAsync())
                {
                    _closing = true;
                    Close();
                }
            }
        }
    }
}
