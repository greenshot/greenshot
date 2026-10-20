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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.Stamps.ViewModels;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Plugin.Stamps.Views;

/// <summary>
/// The stamps: search, a preview of each, name and tags of the selected one, and what can be done with it.
/// Shown for an editor window it is owned by it, so it stays above it, and it is placed at its right edge.
/// </summary>
public partial class StampsWindow : Window
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(StampsWindow));
    private Point _dragStart;
    private bool _dragging;

    internal StampsWindow(StampsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        // The keyboard goes to the search, typing filters at once
        Loaded += (s, e) => SearchBox.Focus();
        DragOver += OnDragOver;
        Drop += OnDrop;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private StampsViewModel ViewModel => (StampsViewModel)DataContext;

    private static IStampsLanguage StampsTexts => Texts.Get<IStampsLanguage>();

    private IntPtr Handle => new WindowInteropHelper(this).EnsureHandle();

    /// <summary>
    /// Make the editor window the owner: this window stays above it. IntPtr.Zero for none.
    /// </summary>
    internal void SetOwner(IntPtr ownerHandle)
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Owner != ownerHandle)
        {
            helper.Owner = ownerHandle;
        }
    }

    /// <summary>
    /// Place the window at the right edge of the editor window, as high as it: outside when there is room on its screen, else over it
    /// </summary>
    internal void PlaceNextTo(IntPtr editorHandle)
    {
        // The visible frame, without the invisible borders for resizing
        if (!DwmApi.GetExtendedFrameBounds(editorHandle, out NativeRect editor) || editor.Width <= 0)
        {
            editor = InteropWindowFactory.CreateFor(editorHandle).GetInfo().Bounds;
        }

        var display = DisplayInfo.AllDisplayInfos.FirstOrDefault(d => d.Bounds.Contains(editor.Location)) ?? DisplayInfo.AllDisplayInfos.FirstOrDefault();
        var own = InteropWindowFactory.CreateFor(Handle).GetInfo().Bounds;
        if (display == null || editor.Width <= 0 || own.Width <= 0)
        {
            return;
        }

        var workArea = display.WorkingArea;
        int width = own.Width;
        int height = Math.Min(Math.Max(editor.Height, own.Height / 2), workArea.Height);
        int x = editor.Right + width <= workArea.Right ? editor.Right : Math.Max(workArea.Left, editor.Right - width);
        int y = Math.Max(workArea.Top, Math.Min(editor.Top, workArea.Bottom - height));
        User32Api.SetWindowPos(Handle, IntPtr.Zero, x, y, width, height, WindowPos.SWP_NOZORDER | WindowPos.SWP_NOACTIVATE);
    }

    /// <summary>
    /// Bring the window to the front with the keyboard in the search
    /// </summary>
    internal void ShowAndFocus()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        bool control = Keyboard.Modifiers == ModifierKeys.Control;
        switch (e.Key)
        {
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
            case Key.F when control:
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
                break;
            case Key.F5:
                ViewModel.Reload();
                e.Handled = true;
                break;
            // In a text field Ctrl+V pastes text
            case Key.V when control && Keyboard.FocusedElement is not TextBox:
                Paste();
                e.Handled = true;
                break;
        }
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Down or Key.Enter)
        {
            ViewModel.SelectedStamp ??= ViewModel.Stamps.Cast<StampViewModel>().FirstOrDefault();
            FocusSelectedStamp();
            e.Handled = true;
        }
    }

    private void StampList_KeyDown(object sender, KeyEventArgs e)
    {
        var stamp = ViewModel.SelectedStamp;
        if (stamp == null)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                ViewModel.Insert(stamp);
                e.Handled = true;
                break;
            case Key.Delete:
                AskAndDelete(stamp);
                e.Handled = true;
                break;
            case Key.F2:
                NameBox.Focus();
                NameBox.SelectAll();
                e.Handled = true;
                break;
            case Key.C when Keyboard.Modifiers == ModifierKeys.Control:
                ViewModel.CopyAsync(stamp).FireAndLog("Copy a stamp", Log);
                e.Handled = true;
                break;
        }
    }

    private void StampList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemAt(e.OriginalSource) is { } stamp)
        {
            ViewModel.Insert(stamp);
        }
    }

    private void StampList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => _dragStart = e.GetPosition(null);

    /// <summary>
    /// Dragging a stamp gives other programs, and the editor, its file
    /// </summary>
    private void StampList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragging)
        {
            return;
        }

        var position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (ItemAt(e.OriginalSource) is not { } stamp)
        {
            return;
        }

        _dragging = true;
        try
        {
            // Only the file: the editor keeps an SVG a vector then, an image format would come first
            var data = new DataObject(DataFormats.FileDrop, new[] { stamp.Stamp.FullPath });
            DragDrop.DoDragDrop(StampList, data, DragDropEffects.Copy);
        }
        finally
        {
            _dragging = false;
        }
    }

    private StampViewModel ItemAt(object source) =>
        source is DependencyObject element ? (ItemsControl.ContainerFromElement(StampList, element) as ListBoxItem)?.DataContext as StampViewModel : null;

    private void DetailsBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
        {
            BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty)?.UpdateSource();
            e.Handled = true;
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = !_dragging && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    /// <summary>
    /// Image files dropped onto the window are added
    /// </summary>
    private void OnDrop(object sender, DragEventArgs e)
    {
        if (_dragging || e.Data.GetData(DataFormats.FileDrop) is not string[] files)
        {
            return;
        }

        if (ViewModel.AddFiles(files.Where(ViewModel.Library.IsStampFile)) > 0)
        {
            FocusSelectedStamp();
        }

        e.Handled = true;
    }

    private void Insert_Click(object sender, RoutedEventArgs e) => ViewModel.Insert(ViewModel.SelectedStamp);

    private void Copy_Click(object sender, RoutedEventArgs e) => ViewModel.CopyAsync(ViewModel.SelectedStamp).FireAndLog("Copy a stamp", Log);

    private void Delete_Click(object sender, RoutedEventArgs e) => AskAndDelete(ViewModel.SelectedStamp);

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        string patterns = string.Join(";", ViewModel.Library.Extensions.OrderBy(extension => extension).Select(extension => "*" + extension));
        var dialog = new OpenFileDialog
        {
            Title = StampsTexts.AddTitle,
            Multiselect = true,
            Filter = $"{StampsTexts.ImageFiles} ({patterns})|{patterns}"
        };
        if (dialog.ShowDialog(this) == true && ViewModel.AddFiles(dialog.FileNames) > 0)
        {
            FocusSelectedStamp();
        }
    }

    private void Paste_Click(object sender, RoutedEventArgs e) => Paste();

    private void OpenFolder_Click(object sender, RoutedEventArgs e) => ViewModel.OpenFolder();

    private void Paste()
    {
        async System.Threading.Tasks.Task PasteAndFocusAsync()
        {
            await ViewModel.PasteAsync();
            FocusSelectedStamp();
        }

        PasteAndFocusAsync().FireAndLog("Paste a stamp", Log);
    }

    private void AskAndDelete(StampViewModel stamp)
    {
        if (stamp == null)
        {
            return;
        }

        if (ThemedMessageBox.Show(this, string.Format(StampsTexts.DeleteQuestion, stamp.Name), StampsTexts.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        ViewModel.Delete(stamp);
        FocusSelectedStamp();
    }

    /// <summary>
    /// The keyboard to the selected stamp (new content gets the focus), or to the list when there is none
    /// </summary>
    private void FocusSelectedStamp()
    {
        var stamp = ViewModel.SelectedStamp;
        if (stamp == null)
        {
            StampList.Focus();
            return;
        }

        StampList.ScrollIntoView(stamp);
        StampList.UpdateLayout();
        if (StampList.ItemContainerGenerator.ContainerFromItem(stamp) is ListBoxItem item)
        {
            item.Focus();
        }
    }
}
