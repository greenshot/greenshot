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
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.WpfEditor.Files;
using Greenshot.WpfEditor.Tools;
using log4net;

namespace Greenshot.WpfEditor.ViewModels
{
    /// <summary>
    /// What the window of the editor needs from its view: dialogs and the zoom of the canvas
    /// </summary>
    public interface IEditorView
    {
        string AskOpenPath();

        string AskSavePath(string fileName, string directory);

        /// <summary>
        /// 0 save, 1 don't save, 2 cancel
        /// </summary>
        int AskSaveChanges(string title);

        void ShowError(string text);

        void Zoom(ZoomCommand command);

        PointD VisibleCenter { get; }

        void Print(GreenshotDocument document, string title);
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
        private static readonly ILog Log = LogManager.GetLogger(typeof(EditorWindowViewModel));
        private DocumentTabViewModel _selectedTab;
        private IEditorView _view;

        public EditorWindowViewModel()
        {
            Tools = Enum.GetValues(typeof(ToolKind)).Cast<ToolKind>().Select(kind => new ToolViewModel(this, kind)).ToList();
            OpenCommand = new RelayCommand(Open);
            SaveCommand = new RelayCommand(() => Save(SelectedTab, false), () => SelectedTab != null);
            SaveAsCommand = new RelayCommand(() => Save(SelectedTab, true), () => SelectedTab != null);
            CopyImageCommand = new RelayCommand(() => Run(() => EditorClipboard.CopyImage(SelectedTab.Document)), () => SelectedTab != null);
            PrintCommand = new RelayCommand(() => _view?.Print(SelectedTab.Document, SelectedTab.Title), () => SelectedTab != null);
            CloseTabCommand = new RelayCommand(tab => Close(tab as DocumentTabViewModel ?? SelectedTab));
            UndoCommand = new RelayCommand(() => SelectedTab.Undo(), () => SelectedTab?.CanUndo == true);
            RedoCommand = new RelayCommand(() => SelectedTab.Redo(), () => SelectedTab?.CanRedo == true);
            CutCommand = new RelayCommand(() => SelectedTab.CutSelection(), () => SelectedTab?.HasSelection == true);
            CopyCommand = new RelayCommand(Copy, () => SelectedTab != null);
            PasteCommand = new RelayCommand(Paste, () => SelectedTab != null);
            DeleteCommand = new RelayCommand(() => SelectedTab.DeleteSelection(), () => SelectedTab?.HasSelection == true);
            DuplicateCommand = new RelayCommand(() => SelectedTab.DuplicateSelection(), () => SelectedTab?.HasSelection == true);
            SelectAllCommand = new RelayCommand(() => { SelectedTab.ToolKind = ToolKind.Select; SelectedTab.SelectAll(); }, () => SelectedTab != null);
            ArrangeCommand = new RelayCommand(parameter => SelectedTab.Arrange((Arrangement)parameter), _ => SelectedTab?.HasSelection == true);
            TurnClockwiseCommand = new RelayCommand(() => SelectedTab.Turn(90), () => SelectedTab != null);
            TurnCounterClockwiseCommand = new RelayCommand(() => SelectedTab.Turn(270), () => SelectedTab != null);
            ZoomCommand = new RelayCommand(parameter => _view?.Zoom((ZoomCommand)parameter), _ => SelectedTab != null);
            InsertImageCommand = new RelayCommand(InsertImage, () => SelectedTab != null);
            AutoCropCommand = new RelayCommand(() => SelectedTab.AutoCrop(), () => SelectedTab != null);
            ApplyCropCommand = new RelayCommand(() => (SelectedTab?.Tool as CropTool)?.Apply(), () => SelectedTab?.Tool is CropTool);
            CancelCropCommand = new RelayCommand(() => SelectedTab.ToolKind = ToolKind.Select, () => SelectedTab?.Tool is CropTool);
            SelectToolCommand = new RelayCommand(parameter => SelectTool((ToolKind)parameter), _ => SelectedTab != null);
            SendToCommand = new RelayCommand(parameter => SendTo(parameter as EditorDestination), parameter => SelectedTab != null && parameter is EditorDestination);
            AddBorderCommand = new RelayCommand(() => Properties.AddBorder(), () => SelectedTab != null);
            AddDropShadowCommand = new RelayCommand(() => Properties.AddDropShadow(), () => SelectedTab != null);
            AddTornEdgesCommand = new RelayCommand(() => Properties.AddTornEdges(), () => SelectedTab != null);
            RemoveFrameCommand = new RelayCommand(() => Properties.RemoveFrame(), () => Properties.HasFrame);
            ResizeCommand = new RelayCommand(() => SelectedTab.Resize(ResizeWidth, ResizeHeight), () => SelectedTab != null && ResizeWidth > 0 && ResizeHeight > 0);
        }

        public ObservableCollection<DocumentTabViewModel> Tabs { get; } = new();

        public IReadOnlyList<ToolViewModel> Tools { get; }

        public PropertiesViewModel Properties { get; } = new();

        public ObservableCollection<LayerViewModel> Layers { get; } = new();

        public IReadOnlyList<EditorDestination> Destinations => EditorSession.Current.Host?.Destinations ?? Array.Empty<EditorDestination>();

        public bool HasDestinations => Destinations.Count > 0;

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

        public string WindowTitle => SelectedTab == null ? EditorTexts.Wpf.Title : $"{SelectedTab.DisplayTitle} - {EditorTexts.Wpf.Title}";

        public bool IsCropping => SelectedTab?.Tool is CropTool;

        public string ZoomText => SelectedTab == null ? string.Empty : string.Format(EditorTexts.Wpf.ZoomStatus, Math.Round(SelectedTab.Zoom * 100));

        public string SizeText
        {
            get
            {
                if (SelectedTab == null)
                {
                    return string.Empty;
                }

                var area = SelectedTab.Document.CanvasArea();
                return string.Format(EditorTexts.Wpf.SizeStatus, Math.Round(area.Width), Math.Round(area.Height));
            }
        }

        public string SelectionText => SelectedTab?.HasSelection == true ? string.Format(EditorTexts.Wpf.SelectionStatus, SelectedTab.Selection.Count) : string.Empty;

        public string UndoText => SelectedTab?.UndoDescription is string step ? string.Format(EditorTexts.Wpf.UndoStep, step) : EditorTexts.Wpf.Undo;

        public string RedoText => SelectedTab?.RedoDescription is string step ? string.Format(EditorTexts.Wpf.RedoStep, step) : EditorTexts.Wpf.Redo;

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
        public ICommand PrintCommand { get; }
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

        public void Attach(IEditorView view) => _view = view;

        /// <summary>
        /// Shows the document in a new tab
        /// </summary>
        public DocumentTabViewModel Add(GreenshotDocument document, string title, string filePath = null)
        {
            var tab = new DocumentTabViewModel(document, string.IsNullOrWhiteSpace(title) ? EditorTexts.Wpf.Untitled : title, filePath);
            Tabs.Add(tab);
            SelectedTab = tab;
            return tab;
        }

        public void OpenFile(string path)
        {
            try
            {
                var document = DocumentFiles.Open(path);
                // An image becomes a new document, saving it again asks where
                Add(document, Path.GetFileNameWithoutExtension(path), DocumentFiles.IsDocument(path) ? path : null);
            }
            catch (Exception ex)
            {
                Log.Warn($"Opening {path} failed", ex);
                _view?.ShowError(string.Format(EditorTexts.Wpf.OpenFailed, path, ex.Message));
            }
        }

        private void Open()
        {
            if (_view?.AskOpenPath() is string path)
            {
                OpenFile(path);
            }
        }

        /// <summary>
        /// Saves the tab, true when it was saved
        /// </summary>
        public bool Save(DocumentTabViewModel tab, bool askPath)
        {
            if (tab == null)
            {
                return false;
            }

            tab.EndTextEdit(null, true);
            string path = tab.FilePath;
            if (askPath || path == null)
            {
                path = _view?.AskSavePath(DocumentFiles.FileNameOf(tab.Title) + DocumentFiles.DocumentExtension, path == null ? null : Path.GetDirectoryName(path));
                if (path == null)
                {
                    return false;
                }
            }

            try
            {
                DocumentFiles.Save(tab.Committed, path);
                if (DocumentFiles.IsDocument(path))
                {
                    tab.MarkSaved(path, Path.GetFileNameWithoutExtension(path));
                }
                else
                {
                    // An export as image doesn't keep the elements: the document itself isn't saved
                    tab.Title = Path.GetFileNameWithoutExtension(path);
                }

                OnPropertyChanged(nameof(WindowTitle));
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"Saving {path} failed", ex);
                _view?.ShowError(string.Format(EditorTexts.Wpf.SaveFailed, path, ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Closes the tab, asking to save its changes. False when the user cancelled.
        /// </summary>
        public bool Close(DocumentTabViewModel tab)
        {
            if (tab == null)
            {
                return true;
            }

            if (tab.IsModified && _view != null)
            {
                SelectedTab = tab;
                switch (_view.AskSaveChanges(tab.Title))
                {
                    case 0 when !Save(tab, false):
                    case 2:
                        return false;
                }
            }

            int index = Tabs.IndexOf(tab);
            Tabs.Remove(tab);
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
        public bool CloseAll()
        {
            foreach (var tab in Tabs.ToList())
            {
                if (!Close(tab))
                {
                    return false;
                }
            }

            return true;
        }

        private void SelectTool(ToolKind kind)
        {
            SelectedTab.ToolKind = kind;
        }

        private void Copy()
        {
            // With elements selected they are copied, else the image
            if (!SelectedTab.CopySelection())
            {
                Run(() => EditorClipboard.CopyImage(SelectedTab.Document));
            }
        }

        private void Paste()
        {
            var tab = SelectedTab;
            if (EditorClipboard.ContainsImage() && EditorClipboard.PasteImage() is byte[] bytes)
            {
                Run(() =>
                {
                    var resource = DocumentFiles.ImageResource(bytes, out int width, out int height);
                    tab.AddImage(resource, width, height, _view?.VisibleCenter ?? tab.Document.CanvasArea().Center);
                });
                return;
            }

            tab.PasteElements();
        }

        private void InsertImage()
        {
            if (_view?.AskOpenPath() is not string path)
            {
                return;
            }

            var tab = SelectedTab;
            Run(() =>
            {
                var resource = DocumentFiles.ImageResource(File.ReadAllBytes(path), out int width, out int height);
                tab.AddImage(resource, width, height, _view?.VisibleCenter ?? tab.Document.CanvasArea().Center);
            });
        }

        private void SendTo(EditorDestination destination)
        {
            var tab = SelectedTab;
            tab.EndTextEdit(null, true);
            AsyncCommand.Run(() => SendToAsync(destination, tab), $"Send to {destination.Name}");
        }

        private async Task SendToAsync(EditorDestination destination, DocumentTabViewModel tab)
        {
            try
            {
                await destination.ExportAsync(tab.Committed, tab.Title);
            }
            catch (Exception ex)
            {
                Log.Warn($"Sending to {destination.Name} failed", ex);
                _view?.ShowError(string.Format(EditorTexts.Wpf.ExportFailed, destination.Name, ex.Message));
            }
        }

        private void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.Warn("Editor command failed", ex);
                _view?.ShowError(ex.Message);
            }
        }

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

            CommandManager.InvalidateRequerySuggested();
        }

        private void StatusChanged()
        {
            OnPropertyChanged(nameof(SelectionText));
            CommandManager.InvalidateRequerySuggested();
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
