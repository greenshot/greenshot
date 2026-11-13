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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.Editor.Standalone.Settings;
using Greenshot.Editor.Standalone.Diagnostics;
using Greenshot.Editor.Standalone.Files;
using Greenshot.Editor.Standalone.Ipc;
using Greenshot.Editor.Standalone.Rendering;
using Greenshot.Editor.Standalone.Tools;
using SixLabors.ImageSharp;

namespace Greenshot.Editor.Standalone.ViewModels
{
    /// <summary>
    /// What the window of the editor needs from its view: dialogs, the clipboard and the zoom of the canvas
    /// </summary>
    public interface IEditorView
    {
        /// <summary>
        /// The file to open, null when the user cancelled
        /// </summary>
        Task<string> AskOpenPathAsync(bool imagesOnly);

        Task<string> AskSavePathAsync(string fileName, string directory);

        /// <summary>
        /// 0 save, 1 don't save, 2 cancel
        /// </summary>
        Task<int> AskSaveChangesAsync(string title);

        Task ShowErrorAsync(string text);

        void Zoom(ZoomCommand command);

        PointD VisibleCenter { get; }

        /// <summary>
        /// Puts the image (a PNG) on the clipboard, with a marker to recognize it by when it is pasted again (null for none)
        /// </summary>
        Task CopyImageAsync(byte[] png, string marker);

        /// <summary>
        /// The image on the clipboard as the bytes of an image file (null when there is none) and the marker it was copied with
        /// </summary>
        Task<(byte[] Image, string Marker)> ReadClipboardAsync();

        /// <summary>
        /// Brings the window to the front, e.g. when Greenshot hands over a capture
        /// </summary>
        void BringToFront();
    }

    public enum ZoomCommand
    {
        In,
        Out,
        BestFit,
        ActualSize
    }

    /// <summary>
    /// The editor window: its tabs, the tools, the properties panel and the commands of the menus
    /// </summary>
    public sealed class EditorWindowViewModel : ObservableObject
    {
        private DocumentTabViewModel _selectedTab;
        private IEditorView _view;
        private string _statusText = string.Empty;

        public EditorWindowViewModel()
        {
            Tools = Enum.GetValues<ToolKind>().Select(kind => new ToolViewModel(this, kind)).ToList();
            OpenCommand = Command(OpenAsync);
            SaveCommand = Command(() => SaveAsync(SelectedTab, false), () => SelectedTab != null);
            SaveAsCommand = Command(() => SaveAsync(SelectedTab, true), () => SelectedTab != null);
            CopyImageCommand = Command(CopyImageAsync, () => SelectedTab != null);
            CloseTabCommand = RelayCommand.ForTask(tab => CloseAsync(tab as DocumentTabViewModel ?? SelectedTab), null, Failed);
            UndoCommand = new RelayCommand(() => SelectedTab.Undo(), () => SelectedTab?.CanUndo == true);
            RedoCommand = new RelayCommand(() => SelectedTab.Redo(), () => SelectedTab?.CanRedo == true);
            CutCommand = Command(CutAsync, () => SelectedTab?.HasSelection == true);
            CopyCommand = Command(CopyAsync, () => SelectedTab != null);
            PasteCommand = Command(PasteAsync, () => SelectedTab != null);
            DeleteCommand = new RelayCommand(() => SelectedTab.DeleteSelection(), () => SelectedTab?.HasSelection == true);
            DuplicateCommand = new RelayCommand(() => SelectedTab.DuplicateSelection(), () => SelectedTab?.HasSelection == true);
            SelectAllCommand = new RelayCommand(() => { SelectedTab.ToolKind = ToolKind.Select; SelectedTab.SelectAll(); }, () => SelectedTab != null);
            ArrangeCommand = new RelayCommand(parameter => SelectedTab.Arrange((Arrangement)parameter), _ => SelectedTab?.HasSelection == true);
            TurnClockwiseCommand = new RelayCommand(() => SelectedTab.Turn(90), () => SelectedTab != null);
            TurnCounterClockwiseCommand = new RelayCommand(() => SelectedTab.Turn(270), () => SelectedTab != null);
            ZoomCommand = new RelayCommand(parameter => _view?.Zoom((ZoomCommand)parameter), _ => SelectedTab != null);
            InsertImageCommand = Command(InsertImageAsync, () => SelectedTab != null);
            AutoCropCommand = new RelayCommand(() => SelectedTab.AutoCrop(), () => SelectedTab != null);
            ApplyCropCommand = new RelayCommand(() => (SelectedTab?.Tool as CropTool)?.Apply(), () => SelectedTab?.Tool is CropTool);
            CancelCropCommand = new RelayCommand(() => SelectedTab.ToolKind = ToolKind.Select, () => SelectedTab?.Tool is CropTool);
            SelectToolCommand = new RelayCommand(parameter => SelectedTab.ToolKind = (ToolKind)parameter, _ => SelectedTab != null);
            SendToCommand = RelayCommand.ForTask(parameter => SendToAsync(parameter as EditorDestination), parameter => SelectedTab != null && parameter is EditorDestination, Failed);
            AddBorderCommand = new RelayCommand(() => Properties.AddBorder(), () => SelectedTab != null);
            AddDropShadowCommand = new RelayCommand(() => Properties.AddDropShadow(), () => SelectedTab != null);
            AddTornEdgesCommand = new RelayCommand(() => Properties.AddTornEdges(), () => SelectedTab != null);
            RemoveFrameCommand = new RelayCommand(() => Properties.RemoveFrame(), () => Properties.HasFrame);
            ResizeCommand = new RelayCommand(() => SelectedTab.Resize(ResizeWidth, ResizeHeight), () => SelectedTab != null && ResizeWidth > 0 && ResizeHeight > 0);
            PlaceStampCommand = RelayCommand.ForTask(parameter => PlaceStampAsync(parameter as StampViewModel), parameter => SelectedTab != null && parameter is StampViewModel, Failed);
            ImportStampCommand = Command(ImportStampAsync);
            SelectionToStampCommand = new RelayCommand(SelectionToStamp, () => SelectedTab?.HasSelection == true);
            if (EditorSession.Current.Greenshot is IGreenshotLink greenshot)
            {
                greenshot.Changed += GreenshotChanged;
            }
        }

        private RelayCommand Command(Func<Task> execute, Func<bool> canExecute = null) => RelayCommand.ForTask(execute, canExecute, Failed);

        public ObservableCollection<DocumentTabViewModel> Tabs { get; } = new();

        public IReadOnlyList<ToolViewModel> Tools { get; }

        public PropertiesViewModel Properties { get; } = new();

        public StampsViewModel Stamps { get; } = new();

        public ObservableCollection<LayerViewModel> Layers { get; } = new();

        public IReadOnlyList<EditorDestination> Destinations => EditorSession.Current.Greenshot?.Destinations ?? Array.Empty<EditorDestination>();

        public bool HasDestinations => Destinations.Count > 0;

        /// <summary>
        /// True when the editor was installed with a Greenshot, which may or may not run right now
        /// </summary>
        public bool CanSendTo => EditorSession.Current.Greenshot != null;

        /// <summary>
        /// Whether Greenshot is there to send documents to its destinations; empty when the editor wasn't installed with Greenshot
        /// </summary>
        public string GreenshotText => EditorSession.Current.Greenshot is not IGreenshotLink greenshot ? string.Empty
            : greenshot.IsConnected ? EditorTexts.Text.GreenshotConnected : EditorTexts.Text.GreenshotNotRunning;

        /// <summary>
        /// What the editor last did for the user, e.g. "Sent to Printer"
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set => Set(ref _statusText, value ?? string.Empty);
        }

        public bool HasTabs => Tabs.Count > 0;

        public DocumentTabViewModel SelectedTab
        {
            get => _selectedTab;
            set
            {
                var before = _selectedTab;
                if (!Set(ref _selectedTab, value))
                {
                    return;
                }

                if (before != null)
                {
                    before.EndTextEdit(null, true);
                    before.PropertyChanged -= TabPropertyChanged;
                    before.DocumentChanged -= DocumentChanged;
                    before.SelectionChanged -= StatusChanged;
                }

                if (value != null)
                {
                    value.PropertyChanged += TabPropertyChanged;
                    value.DocumentChanged += DocumentChanged;
                    value.SelectionChanged += StatusChanged;
                }

                Properties.Tab = value;
                DocumentChanged();
                foreach (var tool in Tools)
                {
                    tool.Refresh();
                }

                OnPropertyChanged(nameof(IsCropping));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public string WindowTitle => SelectedTab == null ? EditorTexts.Text.Title : $"{SelectedTab.DisplayTitle} - {EditorTexts.Text.Title}";

        public bool IsCropping => SelectedTab?.Tool is CropTool;

        public string ZoomText => SelectedTab == null ? string.Empty : string.Format(EditorTexts.Text.ZoomStatus, Math.Round(SelectedTab.Zoom * 100));

        public string SizeText
        {
            get
            {
                if (SelectedTab == null)
                {
                    return string.Empty;
                }

                var area = SelectedTab.Document.CanvasArea();
                return string.Format(EditorTexts.Text.SizeStatus, Math.Round(area.Width), Math.Round(area.Height));
            }
        }

        public string SelectionText => SelectedTab?.HasSelection == true ? string.Format(EditorTexts.Text.SelectionStatus, SelectedTab.Selection.Count) : string.Empty;

        public string UndoText => SelectedTab?.UndoDescription is string step ? string.Format(EditorTexts.Text.UndoStep, step) : EditorTexts.Text.Undo;

        public string RedoText => SelectedTab?.RedoDescription is string step ? string.Format(EditorTexts.Text.RedoStep, step) : EditorTexts.Text.Redo;

        private int _resizeWidth;
        private int _resizeHeight;
        private bool _keepRatio = true;

        public int ResizeWidth
        {
            get => _resizeWidth;
            set
            {
                if (Set(ref _resizeWidth, Math.Max(0, value)) && KeepRatio && SelectedTab != null)
                {
                    var area = SelectedTab.Document.CanvasArea();
                    _resizeHeight = area.Width > 0 ? (int)Math.Round(value * area.Height / area.Width) : _resizeHeight;
                    OnPropertyChanged(nameof(ResizeHeight));
                }

                RelayCommand.RequeryAll();
            }
        }

        public int ResizeHeight
        {
            get => _resizeHeight;
            set
            {
                if (Set(ref _resizeHeight, Math.Max(0, value)) && KeepRatio && SelectedTab != null)
                {
                    var area = SelectedTab.Document.CanvasArea();
                    _resizeWidth = area.Height > 0 ? (int)Math.Round(value * area.Width / area.Height) : _resizeWidth;
                    OnPropertyChanged(nameof(ResizeWidth));
                }

                RelayCommand.RequeryAll();
            }
        }

        public bool KeepRatio
        {
            get => _keepRatio;
            set => Set(ref _keepRatio, value);
        }

        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand CopyImageCommand { get; }
        public ICommand CloseTabCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand CutCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand DuplicateCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand ArrangeCommand { get; }
        public ICommand TurnClockwiseCommand { get; }
        public ICommand TurnCounterClockwiseCommand { get; }
        public ICommand ZoomCommand { get; }
        public ICommand InsertImageCommand { get; }
        public ICommand AutoCropCommand { get; }
        public ICommand ApplyCropCommand { get; }
        public ICommand CancelCropCommand { get; }
        public ICommand SelectToolCommand { get; }
        public ICommand SendToCommand { get; }
        public ICommand AddBorderCommand { get; }
        public ICommand AddDropShadowCommand { get; }
        public ICommand AddTornEdgesCommand { get; }
        public ICommand RemoveFrameCommand { get; }
        public ICommand ResizeCommand { get; }
        public ICommand PlaceStampCommand { get; }
        public ICommand ImportStampCommand { get; }
        public ICommand SelectionToStampCommand { get; }

        public void Attach(IEditorView view) => _view = view;

        /// <summary>
        /// Shows the document in a new tab
        /// </summary>
        public DocumentTabViewModel Add(GreenshotDocument document, string title, string filePath = null)
        {
            var tab = new DocumentTabViewModel(document, string.IsNullOrWhiteSpace(title) ? EditorTexts.Text.Untitled : title, filePath);
            Tabs.Add(tab);
            OnPropertyChanged(nameof(HasTabs));
            SelectedTab = tab;
            return tab;
        }

        /// <summary>
        /// Opens a document or an image in a new tab
        /// </summary>
        public async Task OpenFileAsync(string path)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(path);
                var document = DocumentFiles.Read(bytes, path);
                // An image becomes a new document, saving it again asks where
                Add(document, Path.GetFileNameWithoutExtension(path), DocumentFiles.IsDocument(path) ? path : null);
            }
            catch (Exception ex)
            {
                EditorLog.Warn($"Opening {path} failed", ex);
                await ShowErrorAsync(string.Format(EditorTexts.Text.OpenFailed, path, ex.Message));
            }
        }

        /// <summary>
        /// Opens a capture Greenshot handed over as a file. The file was only the way here: it is removed, and the tab is
        /// a new document which wasn't saved anywhere yet.
        /// </summary>
        public async Task OpenHandoverAsync(string path)
        {
            if (!EditorPaths.IsHandoverFile(path))
            {
                // Whoever started the editor named another file: it is opened like any file, and never removed
                EditorLog.Warn($"{path} is not a file of the handover folder, it is opened as a file");
                await OpenFileAsync(path);
                return;
            }

            try
            {
                var bytes = await File.ReadAllBytesAsync(path);
                var document = DocumentFiles.Read(bytes, path);
                Add(document, document.Metadata?.Title);
                _view?.BringToFront();
            }
            catch (Exception ex)
            {
                EditorLog.Warn($"Opening the capture {path} of Greenshot failed", ex);
                await ShowErrorAsync(string.Format(EditorTexts.Text.OpenFailed, path, ex.Message));
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    EditorLog.Warn($"The handed over file {path} could not be removed", ex);
                }
            }
        }

        private async Task OpenAsync()
        {
            if (_view != null && await _view.AskOpenPathAsync(false) is string path)
            {
                await OpenFileAsync(path);
            }
        }

        /// <summary>
        /// Saves the tab, true when it was saved
        /// </summary>
        public async Task<bool> SaveAsync(DocumentTabViewModel tab, bool askPath)
        {
            if (tab == null)
            {
                return false;
            }

            tab.EndTextEdit(null, true);
            string path = tab.FilePath;
            if (askPath || path == null)
            {
                if (_view == null)
                {
                    return false;
                }

                string directory = path == null ? EditorSettings.Current.LastDirectory : Path.GetDirectoryName(path);
                path = await _view.AskSavePathAsync(DocumentFiles.FileNameOf(tab.Title) + DocumentFiles.DocumentExtension, directory);
                if (path == null)
                {
                    return false;
                }
            }

            try
            {
                DocumentFiles.Save(tab.Committed, path);
                EditorSettings.Current.LastDirectory = Path.GetDirectoryName(path);
                if (DocumentFiles.IsDocument(path))
                {
                    tab.MarkSaved(path, Path.GetFileNameWithoutExtension(path));
                }
                else
                {
                    // An export as image doesn't keep the elements: the document itself isn't saved
                    tab.Title = Path.GetFileNameWithoutExtension(path);
                }

                StatusText = string.Format(EditorTexts.Text.SavedStatus, path);
                OnPropertyChanged(nameof(WindowTitle));
                return true;
            }
            catch (Exception ex)
            {
                EditorLog.Warn($"Saving {path} failed", ex);
                await ShowErrorAsync(string.Format(EditorTexts.Text.SaveFailed, path, ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Closes the tab, asking to save its changes. False when the user cancelled.
        /// </summary>
        public async Task<bool> CloseAsync(DocumentTabViewModel tab)
        {
            if (tab == null || !Tabs.Contains(tab))
            {
                return true;
            }

            if (tab.IsModified && _view != null)
            {
                SelectedTab = tab;
                int answer = await _view.AskSaveChangesAsync(tab.Title);
                if (answer == 2 || (answer == 0 && !await SaveAsync(tab, false)))
                {
                    return false;
                }
            }

            int index = Tabs.IndexOf(tab);
            if (index < 0)
            {
                return true;
            }

            Tabs.RemoveAt(index);
            OnPropertyChanged(nameof(HasTabs));
            if (SelectedTab == tab || SelectedTab == null)
            {
                SelectedTab = Tabs.Count == 0 ? null : Tabs[Math.Min(index, Tabs.Count - 1)];
            }

            tab.Dispose();
            return true;
        }

        /// <summary>
        /// Closes all tabs, false when the user kept one open
        /// </summary>
        public async Task<bool> CloseAllAsync()
        {
            foreach (var tab in Tabs.ToList())
            {
                if (!await CloseAsync(tab))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task CopyImageAsync()
        {
            var tab = SelectedTab;
            tab.EndTextEdit(null, true);
            if (_view != null)
            {
                await _view.CopyImageAsync(DocumentFiles.ToPng(tab.Committed), null);
                StatusText = EditorTexts.Text.CopiedStatus;
            }
        }

        /// <summary>
        /// With elements selected they are copied: as elements for a paste in the editor, and as their picture for every
        /// other program. Without a selection the whole image is copied.
        /// </summary>
        private async Task CopyAsync()
        {
            var tab = SelectedTab;
            if (!tab.HasSelection)
            {
                await CopyImageAsync();
                return;
            }

            tab.EndTextEdit(null, true);
            var png = RenderSelection(tab);
            if (tab.CopySelection())
            {
                await CopyElementsAsync(png);
            }
        }

        private async Task CutAsync()
        {
            var tab = SelectedTab;
            tab.EndTextEdit(null, true);
            var png = RenderSelection(tab);
            if (tab.HasSelection)
            {
                tab.CutSelection();
                await CopyElementsAsync(png);
            }
        }

        private async Task CopyElementsAsync(byte[] png)
        {
            // The marker tells a later paste that the clipboard still holds these elements, not an image copied elsewhere since
            EditorSession.Current.CopyMarker = Guid.NewGuid().ToString("N");
            if (_view != null && png != null)
            {
                await _view.CopyImageAsync(png, EditorSession.Current.CopyMarker);
            }
        }

        private async Task PasteAsync()
        {
            var tab = SelectedTab;
            var (image, marker) = _view == null ? (null, null) : await _view.ReadClipboardAsync();
            bool ownElements = tab.CanPasteElements && (marker == EditorSession.Current.CopyMarker || image == null);
            if (ownElements)
            {
                tab.PasteElements();
            }
            else if (image != null)
            {
                var resource = DocumentFiles.ImageResource(image, out int width, out int height);
                tab.AddImage(resource, width, height, _view.VisibleCenter);
            }
        }

        private async Task InsertImageAsync()
        {
            if (_view == null || await _view.AskOpenPathAsync(true) is not string path)
            {
                return;
            }

            var tab = SelectedTab;
            var resource = DocumentFiles.ImageResource(await File.ReadAllBytesAsync(path), out int width, out int height);
            tab.AddImage(resource, width, height, _view.VisibleCenter);
        }

        private async Task PlaceStampAsync(StampViewModel stamp)
        {
            var tab = SelectedTab;
            var resource = DocumentFiles.ImageResource(await File.ReadAllBytesAsync(stamp.Stamp.FullPath), out int width, out int height);
            tab.AddImage(resource, width, height, _view?.VisibleCenter ?? tab.Document.CanvasArea().Center);
        }

        private async Task ImportStampAsync()
        {
            if (_view != null && await _view.AskOpenPathAsync(true) is string path)
            {
                Stamps.Library.Import(path);
                Stamps.Refresh();
            }
        }

        /// <summary>
        /// The selected elements become a stamp
        /// </summary>
        private void SelectionToStamp()
        {
            var tab = SelectedTab;
            tab.EndTextEdit(null, true);
            var elements = tab.SelectedElements;
            if (RenderSelection(tab) is not byte[] png)
            {
                return;
            }

            try
            {
                string name = elements.Select(Editing.ElementText.TextOf).FirstOrDefault(text => !string.IsNullOrWhiteSpace(text)) ?? EditorTexts.NameOf(PropertiesViewModel.KindOf(elements[0]));
                Stamps.Library.Add(png, name.Length > 40 ? name.Substring(0, 40) : name);
                Stamps.Refresh();
                StatusText = EditorTexts.Text.StampAddedStatus;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                EditorLog.Warn("The stamp could not be added", ex);
                Failed(ex);
            }
        }

        /// <summary>
        /// The selected elements drawn as PNG: without what lies below them, on a transparent background. Null without a selection.
        /// </summary>
        private static byte[] RenderSelection(DocumentTabViewModel tab)
        {
            var document = tab.Committed;
            var ids = DocumentEdits.WithSets(document, tab.Selection);
            var elements = document.Layers.SelectMany(layer => layer.Elements).Where(element => ids.Contains(element.Id)).ToList();
            if (elements.Count == 0)
            {
                return null;
            }

            var area = elements.Select(element => DocumentDiff.DrawnArea(document, element)).Aggregate(RectD.Union);
            area = RectD.FromPoints(new PointD(Math.Floor(area.X), Math.Floor(area.Y)), new PointD(Math.Ceiling(area.Right), Math.Ceiling(area.Bottom)));
            var transparent = document with { Canvas = document.Canvas with { Background = Rgba.Transparent } };
            using var image = DocumentRenderer.Render(transparent, area, null, element => ids.Contains(element.Id) || element is FrameElement { Target: Guid target } && ids.Contains(target));
            using var stream = new MemoryStream();
            image.SaveAsPng(stream);
            return stream.ToArray();
        }

        private async Task SendToAsync(EditorDestination destination)
        {
            var tab = SelectedTab;
            tab.EndTextEdit(null, true);
            StatusText = string.Format(EditorTexts.Text.SendingStatus, destination.Name);
            try
            {
                await EditorSession.Current.Greenshot.ExportAsync(destination, tab.Committed, tab.Title);
                StatusText = string.Format(EditorTexts.Text.SentStatus, destination.Name);
            }
            catch (Exception ex)
            {
                EditorLog.Warn($"Sending to {destination.Designation} failed", ex);
                StatusText = string.Empty;
                await ShowErrorAsync(string.Format(EditorTexts.Text.ExportFailed, destination.Name, ex.Message));
            }
        }

        private void GreenshotChanged()
        {
            OnPropertyChanged(nameof(Destinations));
            OnPropertyChanged(nameof(HasDestinations));
            OnPropertyChanged(nameof(GreenshotText));
            RelayCommand.RequeryAll();
        }

        private void Failed(Exception exception) => ShowErrorAsync(exception.Message).Forget("Showing an error");

        private Task ShowErrorAsync(string text) => _view?.ShowErrorAsync(text) ?? Task.CompletedTask;

        private void TabPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DocumentTabViewModel.ToolKind):
                    foreach (var tool in Tools)
                    {
                        tool.Refresh();
                    }

                    OnPropertyChanged(nameof(IsCropping));
                    break;
                case nameof(DocumentTabViewModel.Zoom):
                    OnPropertyChanged(nameof(ZoomText));
                    break;
                case nameof(DocumentTabViewModel.DisplayTitle):
                    OnPropertyChanged(nameof(WindowTitle));
                    break;
                case nameof(DocumentTabViewModel.UndoDescription):
                case nameof(DocumentTabViewModel.RedoDescription):
                    OnPropertyChanged(nameof(UndoText));
                    OnPropertyChanged(nameof(RedoText));
                    break;
            }

            RelayCommand.RequeryAll();
        }

        private void StatusChanged()
        {
            OnPropertyChanged(nameof(SelectionText));
            RelayCommand.RequeryAll();
        }

        private void DocumentChanged()
        {
            OnPropertyChanged(nameof(SizeText));
            OnPropertyChanged(nameof(ZoomText));
            OnPropertyChanged(nameof(UndoText));
            OnPropertyChanged(nameof(RedoText));
            StatusChanged();
            UpdateLayers();
            if (SelectedTab != null)
            {
                var area = SelectedTab.Document.CanvasArea();
                _resizeWidth = (int)Math.Round(area.Width);
                _resizeHeight = (int)Math.Round(area.Height);
                OnPropertyChanged(nameof(ResizeWidth));
                OnPropertyChanged(nameof(ResizeHeight));
            }
        }

        private void UpdateLayers()
        {
            var layers = SelectedTab?.Document.Layers ?? Array.Empty<Layer>();
            // Only rebuilt when a layer itself changed, not for every change of an element
            if (Layers.Select(layer => (layer.Layer.Id, layer.Layer.Name, layer.Layer.Visible, layer.Layer.Locked))
                .SequenceEqual(layers.Reverse().Select(layer => (layer.Id, layer.Name, layer.Visible, layer.Locked))))
            {
                return;
            }

            Layers.Clear();
            // The top layer first, like in other editors
            foreach (var layer in layers.Reverse())
            {
                Layers.Add(new LayerViewModel(SelectedTab, layer));
            }
        }
    }
}
