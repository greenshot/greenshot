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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Greenshot.Base.Wpf;
using Greenshot.Document;
using Greenshot.WpfEditor.Controls;
using Greenshot.WpfEditor.Files;
using Greenshot.WpfEditor.Tools;
using Greenshot.WpfEditor.ViewModels;
using Microsoft.Win32;

namespace Greenshot.WpfEditor.Views
{
    /// <summary>
    /// The window of the editor. It gives the view model its dialogs and the zoom of the canvas, and maps the keys of the
    /// tools and the canvas (which don't go through commands, so they don't fire while a text box has the focus).
    /// </summary>
    public partial class EditorWindow : Window, IEditorView
    {
        private readonly EditorWindowViewModel _viewModel;

        public EditorWindow(EditorWindowViewModel viewModel)
        {
            EditorTexts.Register();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
            DataContext = _viewModel;
            _viewModel.Attach(this);
            Drop += OnDrop;
            DragOver += OnDragOver;
            Canvas.ViewChanged += UpdateOverview;
            Overview.CenterRequested += (_, point) => Canvas.CenterOn(point);
        }

        /// <summary>
        /// The overview shows the whole canvas and the visible part, when not all of it is visible and there is room
        /// </summary>
        private void UpdateOverview()
        {
            bool show = Canvas.CanPan && Canvas.ActualWidth >= ZoomOverview.DefaultSize * 2 && Canvas.ActualHeight >= ZoomOverview.DefaultSize * 1.5;
            Overview.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (!show || _viewModel.SelectedTab is not { } tab)
            {
                return;
            }

            var area = tab.Composition.Area;
            Overview.ContentSize = new Size(area.Width, area.Height);
            Overview.VisibleArea = Canvas.VisibleArea;
            Overview.Content = Canvas.OverviewBrush;
        }

        public EditorWindowViewModel ViewModel => _viewModel;

        public PointD VisibleCenter => Canvas.VisibleCenter;

        public string AskOpenPath()
        {
            var images = string.Join(";", DocumentFiles.ImageExtensions.Select(extension => "*" + extension));
            var dialog = new OpenFileDialog
            {
                Filter = $"{EditorTexts.Wpf.SupportedFilter}|*{DocumentFiles.DocumentExtension};{images}|{EditorTexts.Wpf.DocumentsFilter}|*{DocumentFiles.DocumentExtension}|{EditorTexts.Wpf.ImagesFilter}|{images}",
                CheckFileExists = true
            };
            return dialog.ShowDialog(this) == true ? dialog.FileName : null;
        }

        public string AskSavePath(string fileName, string directory)
        {
            var dialog = new SaveFileDialog
            {
                FileName = fileName,
                InitialDirectory = directory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                AddExtension = true,
                DefaultExt = DocumentFiles.DocumentExtension,
                Filter = $"{EditorTexts.Wpf.DocumentsFilter}|*{DocumentFiles.DocumentExtension}|PNG|*.png|JPEG|*.jpg;*.jpeg|BMP|*.bmp|GIF|*.gif|TIFF|*.tif;*.tiff|WebP|*.webp"
            };
            return dialog.ShowDialog(this) == true ? dialog.FileName : null;
        }

        public int AskSaveChanges(string title) => ThemedMessageBox.ShowChoice(this, EditorTexts.Wpf.Title, string.Format(EditorTexts.Wpf.SaveChanges, title), MessageBoxImage.Question,
            new[] { EditorTexts.Wpf.SaveButton, EditorTexts.Wpf.DontSaveButton, EditorTexts.Wpf.CancelButton }, 0, 2);

        public void ShowError(string text) => ThemedMessageBox.Show(this, text, EditorTexts.Wpf.Title, MessageBoxButton.OK, MessageBoxImage.Warning);

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

        public void Print(GreenshotDocument document, string title)
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var bitmap = EditorClipboard.ToBitmapSource(document);
            double width = dialog.PrintableAreaWidth, height = dialog.PrintableAreaHeight;
            // Shrunk to the page when it is larger, never enlarged
            double scale = Math.Min(1, Math.Min(width / bitmap.Width, height / bitmap.Height));
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawImage(bitmap, new Rect((width - bitmap.Width * scale) / 2, 0, bitmap.Width * scale, bitmap.Height * scale));
            }

            dialog.PrintVisual(visual, title);
        }

        private void SendTo_Click(object sender, RoutedEventArgs e)
        {
            var menu = ThemedMenu.CreateContextMenu();
            menu.PlacementTarget = SendToButton;
            menu.Placement = PlacementMode.Bottom;
            foreach (var destination in _viewModel.Destinations)
            {
                var item = destination;
                menu.Items.Add(ThemedMenu.CreateItem(item.Name, null, () => _viewModel.SendToCommand.Execute(item)));
            }

            menu.IsOpen = true;
        }

        private void File_Click(object sender, RoutedEventArgs e)
        {
            var menu = ThemedMenu.CreateContextMenu();
            menu.PlacementTarget = FileButton;
            menu.Placement = PlacementMode.Bottom;
            Add(menu, EditorTexts.Wpf.Open, "Open", _viewModel.OpenCommand);
            Add(menu, EditorTexts.Wpf.Save, "Save", _viewModel.SaveCommand);
            Add(menu, EditorTexts.Wpf.SaveAs, null, _viewModel.SaveAsCommand);
            menu.Items.Add(ThemedMenu.CreateSeparator());
            Add(menu, EditorTexts.Wpf.CopyImage, "CopyImage", _viewModel.CopyImageCommand);
            Add(menu, EditorTexts.Wpf.Print, "Print", _viewModel.PrintCommand);
            menu.Items.Add(ThemedMenu.CreateSeparator());
            Add(menu, EditorTexts.Wpf.CloseTab, "Close", _viewModel.CloseTabCommand);
            menu.IsOpen = true;
        }

        /// <summary>
        /// The right mouse button selects the element under it (when it isn't selected) and shows what can be done with it
        /// </summary>
        private void Canvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var tab = _viewModel.SelectedTab;
            if (tab == null)
            {
                return;
            }

            if (tab.ToolKind == ToolKind.Select)
            {
                var point = Canvas.ToCanvas(e.GetPosition(Canvas));
                var hit = Greenshot.Document.Editing.HitTesting.ElementAt(tab.Document, point, 3 / tab.Zoom);
                if (hit != null && !tab.Selection.Contains(hit.Id))
                {
                    tab.Select(Greenshot.Document.Editing.DocumentEdits.WithSets(tab.Document, new[] { hit.Id }));
                }
            }

            var menu = ThemedMenu.CreateContextMenu();
            Add(menu, EditorTexts.Wpf.Cut, "Cut", _viewModel.CutCommand);
            Add(menu, EditorTexts.Wpf.Copy, "Copy", _viewModel.CopyCommand);
            Add(menu, EditorTexts.Wpf.Paste, "Paste", _viewModel.PasteCommand);
            Add(menu, EditorTexts.Wpf.Duplicate, null, _viewModel.DuplicateCommand);
            Add(menu, EditorTexts.Wpf.Delete, "Delete", _viewModel.DeleteCommand);
            menu.Items.Add(ThemedMenu.CreateSeparator());
            Add(menu, EditorTexts.Wpf.BringToFront, "BringToFront", _viewModel.ArrangeCommand, Greenshot.Document.Editing.Arrangement.ToFront);
            Add(menu, EditorTexts.Wpf.BringForward, "BringForward", _viewModel.ArrangeCommand, Greenshot.Document.Editing.Arrangement.Forward);
            Add(menu, EditorTexts.Wpf.SendBackward, "SendBackward", _viewModel.ArrangeCommand, Greenshot.Document.Editing.Arrangement.Backward);
            Add(menu, EditorTexts.Wpf.SendToBack, "SendToBack", _viewModel.ArrangeCommand, Greenshot.Document.Editing.Arrangement.ToBack);
            menu.Items.Add(ThemedMenu.CreateSeparator());
            Add(menu, EditorTexts.Wpf.SelectAll, null, _viewModel.SelectAllCommand);
            menu.PlacementTarget = Canvas;
            menu.Placement = PlacementMode.MousePoint;
            menu.IsOpen = true;
            e.Handled = true;
        }

        private static void Add(ContextMenu menu, string text, string icon, ICommand command, object parameter = null)
        {
            var item = ThemedMenu.CreateItem(text, icon == null ? null : EditorIcons.Get(icon), () => command.Execute(parameter));
            item.IsEnabled = command.CanExecute(parameter);
            menu.Items.Add(item);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            var tab = _viewModel.SelectedTab;
            if (e.Handled || tab == null || Keyboard.FocusedElement is TextBoxBase or ComboBox || tab.EditingText != null)
            {
                return;
            }

            var modifiers = Keyboard.Modifiers;
            bool shift = modifiers.HasFlag(ModifierKeys.Shift), control = modifiers.HasFlag(ModifierKeys.Control);
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            ToolKey? toolKey = key switch
            {
                Key.Escape => ToolKey.Escape,
                Key.Enter => ToolKey.Enter,
                Key.Left => ToolKey.Left,
                Key.Right => ToolKey.Right,
                Key.Up => ToolKey.Up,
                Key.Down => ToolKey.Down,
                _ => null
            };
            // The arrow keys move the selection only while the canvas or nothing in the panel has the focus
            bool inPanel = Keyboard.FocusedElement is Control && toolKey is ToolKey.Left or ToolKey.Right or ToolKey.Up or ToolKey.Down;
            if (toolKey is ToolKey pressed && !inPanel && tab.Key(pressed, shift, control))
            {
                e.Handled = true;
                return;
            }

            if (key == Key.Escape && tab.ToolKind != ToolKind.Select)
            {
                tab.ToolKind = ToolKind.Select;
                e.Handled = true;
                return;
            }

            if (control || modifiers.HasFlag(ModifierKeys.Alt))
            {
                return;
            }

            switch (key)
            {
                case Key.Delete:
                    tab.DeleteSelection();
                    e.Handled = true;
                    return;
                case Key.Home:
                    tab.Arrange(Greenshot.Document.Editing.Arrangement.ToFront);
                    e.Handled = true;
                    return;
                case Key.End:
                    tab.Arrange(Greenshot.Document.Editing.Arrangement.ToBack);
                    e.Handled = true;
                    return;
                case Key.PageUp:
                    tab.Arrange(Greenshot.Document.Editing.Arrangement.Forward);
                    e.Handled = true;
                    return;
                case Key.PageDown:
                    tab.Arrange(Greenshot.Document.Editing.Arrangement.Backward);
                    e.Handled = true;
                    return;
            }

            if (ToolOf(key) is ToolKind kind)
            {
                tab.ToolKind = kind;
                e.Handled = true;
            }
        }

        /// <summary>
        /// The keys of the tools, the same as in the old editor
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

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DroppedFiles(e).Length > 0 ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// A dropped .greenshot opens in a tab, a dropped image is added to the document (or opens, with Ctrl)
        /// </summary>
        private void OnDrop(object sender, DragEventArgs e)
        {
            foreach (var file in DroppedFiles(e))
            {
                if (DocumentFiles.IsDocument(file) || _viewModel.SelectedTab == null || e.KeyStates.HasFlag(DragDropKeyStates.ControlKey))
                {
                    _viewModel.OpenFile(file);
                    continue;
                }

                try
                {
                    var resource = DocumentFiles.ImageResource(File.ReadAllBytes(file), out int width, out int height);
                    _viewModel.SelectedTab.AddImage(resource, width, height, Canvas.ToCanvas(e.GetPosition(Canvas)));
                }
                catch (Exception ex)
                {
                    ShowError(string.Format(EditorTexts.Wpf.OpenFailed, file, ex.Message));
                }
            }
        }

        private static string[] DroppedFiles(DragEventArgs e) =>
            e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files
                ? files.Where(file => DocumentFiles.IsDocument(file) || EditorClipboard.IsImageFile(file)).ToArray()
                : Array.Empty<string>();

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!e.Cancel && !_viewModel.CloseAll())
            {
                e.Cancel = true;
            }
        }
    }
}
