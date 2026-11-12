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
using System.Linq;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.WpfEditor.Editing;
using Greenshot.WpfEditor.Rendering;
using Greenshot.WpfEditor.Tools;

namespace Greenshot.WpfEditor.ViewModels
{
    /// <summary>
    /// One document in a tab of the editor: the document with its undo steps, the selection, the tool which works on it,
    /// and the bitmap the canvas shows. Every change is a new document (the document model is immutable), so an undo step
    /// is only the document before the change.
    /// </summary>
    public sealed partial class DocumentTabViewModel : ObservableObject, IToolHost, IDisposable
    {
        private sealed record Step(GreenshotDocument Document, IReadOnlyCollection<Guid> Selection, string Description);

        private readonly Stack<Step> _undo = new();
        private readonly Stack<Step> _redo = new();
        private GreenshotDocument _committed;
        private GreenshotDocument _shown;
        private HashSet<Guid> _selection = new();
        private IEditorTool _tool;
        private string _title;
        private string _filePath;
        private bool _isModified;
        private double _zoom = 1;
        private ISet<Guid> _lastAdded = new HashSet<Guid>();
        private Guid? _editingText;
        private string _textBefore;
        private bool _textIsNew;
        private string _mergeKey;

        public DocumentTabViewModel(GreenshotDocument document, string title, string filePath = null)
        {
            _committed = _shown = Anchors.Apply(document ?? throw new ArgumentNullException(nameof(document)));
            _title = title;
            _filePath = filePath;
            Composition.Update(_shown);
            _tool = new SelectTool(this);
        }

        public EditorSession Session => EditorSession.Current;

        public DocumentComposition Composition { get; } = new();

        /// <summary>
        /// The document as it is shown, with the change being made
        /// </summary>
        public GreenshotDocument Document => _shown;

        /// <summary>
        /// The document of the last undo step, without a change which is still being made
        /// </summary>
        public GreenshotDocument Committed => _committed;

        public IReadOnlyCollection<Guid> Selection => _selection;

        public IReadOnlyList<Element> SelectedElements => _selection.Select(id => DocumentEdits.Find(_shown, id)).Where(element => element != null).ToList();

        public bool HasSelection => _selection.Count > 0;

        public string Title
        {
            get => _title;
            set
            {
                if (Set(ref _title, value))
                {
                    OnPropertyChanged(nameof(DisplayTitle));
                }
            }
        }

        public string FilePath
        {
            get => _filePath;
            set => Set(ref _filePath, value);
        }

        public bool IsModified
        {
            get => _isModified;
            set
            {
                if (Set(ref _isModified, value))
                {
                    OnPropertyChanged(nameof(DisplayTitle));
                }
            }
        }

        /// <summary>
        /// The title with a mark when the document has changes which aren't saved
        /// </summary>
        public string DisplayTitle => IsModified ? $"{Title} •" : Title;

        /// <summary>
        /// How big the canvas is shown, 1 is one canvas pixel per screen pixel (device independent)
        /// </summary>
        public double Zoom
        {
            get => _zoom;
            set => Set(ref _zoom, Math.Max(MinZoom, Math.Min(MaxZoom, value)));
        }

        public const double MinZoom = 0.05, MaxZoom = 32;

        /// <summary>
        /// Where the view of the tab was scrolled to, kept while another tab is shown
        /// </summary>
        public System.Windows.Vector ScrollOffset { get; set; }

        /// <summary>
        /// False until the canvas showed the tab: the first time it fits the document into the window
        /// </summary>
        public bool HasBeenShown { get; set; }

        public IEditorTool Tool => _tool;

        public ToolKind ToolKind
        {
            get => _tool.Kind;
            set
            {
                if (value == _tool.Kind)
                {
                    return;
                }

                _tool.Cancel();
                _tool = Create(value);
                if (value != ToolKind.Select)
                {
                    Select(Array.Empty<Guid>());
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(Tool));
                ToolChanged();
            }
        }

        private IEditorTool Create(ToolKind kind) => kind switch
        {
            ToolKind.Select => new SelectTool(this),
            ToolKind.Freehand => new FreehandTool(this),
            ToolKind.Crop => new CropTool(this),
            _ => new CreateTool(this, kind)
        };

        public string UndoDescription => _undo.Count > 0 ? _undo.Peek().Description : null;

        public string RedoDescription => _redo.Count > 0 ? _redo.Peek().Description : null;

        public bool CanUndo => _undo.Count > 0;

        public bool CanRedo => _redo.Count > 0;

        /// <summary>
        /// The committed document changed (not a preview while the mouse drags), the panels show the new values
        /// </summary>
        public event Action DocumentChanged;

        public event Action SelectionChanged;

        /// <summary>
        /// The adorners or the cursor of the tool changed
        /// </summary>
        public event Action ToolStateChanged;

        /// <summary>
        /// The canvas shows a text box over the element to type its text, see <see cref="EndTextEdit"/>
        /// </summary>
        public event Action<Guid> TextEditRequested;

        public Guid? EditingText => _editingText;

        public void Select(IEnumerable<Guid> ids)
        {
            var selection = new HashSet<Guid>(ids ?? Array.Empty<Guid>());
            if (selection.SetEquals(_selection))
            {
                return;
            }

            _selection = selection;
            OnPropertyChanged(nameof(Selection));
            OnPropertyChanged(nameof(HasSelection));
            SelectionChanged?.Invoke();
            ToolChanged();
        }

        public void Preview(GreenshotDocument document)
        {
            Show(Anchors.Apply(document));
        }

        public void Commit(GreenshotDocument document, string description) => Commit(document, description, null);

        /// <summary>
        /// Commits the change; changes with the same merge key one after the other (e.g. while a slider moves) are one undo step
        /// </summary>
        public void Commit(GreenshotDocument document, string description, string mergeKey)
        {
            var before = _committed;
            document = Anchors.Apply(document);
            if (ReferenceEquals(document, before))
            {
                Show(before);
                return;
            }

            bool merge = mergeKey != null && mergeKey == _mergeKey && _undo.Count > 0 && _redo.Count == 0;
            if (!merge)
            {
                _undo.Push(new Step(before, _selection.ToList(), description));
            }

            _mergeKey = mergeKey;
            _redo.Clear();
            _lastAdded = AddedIds(before, document);
            _committed = document;
            IsModified = true;
            Show(document);
            UndoStateChanged();
        }

        public void CancelPreview() => Show(_committed);

        public Style StyleFor(ToolKind kind) => Session.StyleFor(kind);

        public ToolOptions Options => Session.Options;

        public void EditText(Guid elementId)
        {
            EndTextEdit(null, true);
            if (DocumentEdits.Find(_committed, elementId) is not Element element || !ElementText.HasText(element))
            {
                return;
            }

            _editingText = elementId;
            _textBefore = ElementText.TextOf(element) ?? string.Empty;
            _textIsNew = _lastAdded.Contains(elementId) && _undo.Count > 0;
            // The text is typed in a text box over the element, the element itself doesn't show it meanwhile
            Show(DocumentEdits.Replace(_committed, ElementText.WithText(element, string.Empty)));
            OnPropertyChanged(nameof(EditingText));
            TextEditRequested?.Invoke(elementId);
        }

        /// <summary>
        /// Typing ended: keep the text, or (keep false, or text null) leave the element as it was. A new text element which stays
        /// empty is removed again, together with the undo step which added it.
        /// </summary>
        public void EndTextEdit(string text, bool keep)
        {
            if (_editingText is not Guid id)
            {
                return;
            }

            _editingText = null;
            OnPropertyChanged(nameof(EditingText));
            var element = DocumentEdits.Find(_committed, id);
            bool removable = element is TextElement or SpeechBubbleElement;
            if (element == null)
            {
                Show(_committed);
                return;
            }

            if (_textIsNew && removable && (!keep || string.IsNullOrEmpty(text)))
            {
                // The element was only just added: as if it was never there
                var step = _undo.Pop();
                _committed = step.Document;
                Select(Array.Empty<Guid>());
                Show(_committed);
                UndoStateChanged();
                return;
            }

            if (!keep || text == null || text == _textBefore)
            {
                Show(_committed);
                return;
            }

            var changed = DocumentEdits.Replace(_committed, ElementText.WithText(element, text));
            if (_textIsNew)
            {
                // One undo step for adding the element with its text
                _committed = Anchors.Apply(changed);
                IsModified = true;
                Show(_committed);
                return;
            }

            Commit(changed, EditorTexts.Wpf.UndoText);
        }

        public void ToolChanged() => ToolStateChanged?.Invoke();

        public void Undo()
        {
            EndTextEdit(null, true);
            _tool.Cancel();
            if (_undo.Count == 0)
            {
                return;
            }

            var step = _undo.Pop();
            _redo.Push(new Step(_committed, _selection.ToList(), step.Description));
            Restore(step);
        }

        public void Redo()
        {
            EndTextEdit(null, true);
            _tool.Cancel();
            if (_redo.Count == 0)
            {
                return;
            }

            var step = _redo.Pop();
            _undo.Push(new Step(_committed, _selection.ToList(), step.Description));
            Restore(step);
        }

        private void Restore(Step step)
        {
            _committed = step.Document;
            _mergeKey = null;
            _lastAdded = new HashSet<Guid>();
            IsModified = true;
            Show(_committed);
            Select(step.Selection.Where(id => DocumentEdits.Find(_committed, id) != null));
            UndoStateChanged();
        }

        private void UndoStateChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoDescription));
            OnPropertyChanged(nameof(RedoDescription));
        }

        private void Show(GreenshotDocument document)
        {
            var before = _shown;
            _shown = document;
            if (ReferenceEquals(before, document))
            {
                return;
            }

            Composition.Update(document);
            if (_selection.Any(id => DocumentEdits.Find(document, id) == null))
            {
                _selection = new HashSet<Guid>(_selection.Where(id => DocumentEdits.Find(document, id) != null));
                OnPropertyChanged(nameof(HasSelection));
                SelectionChanged?.Invoke();
            }

            // The panels and the status show the committed document: a preview (each mouse move of a drag) only draws
            if (ReferenceEquals(document, _committed))
            {
                DocumentChanged?.Invoke();
            }

            ToolChanged();
        }

        private static ISet<Guid> AddedIds(GreenshotDocument before, GreenshotDocument after)
        {
            var old = new HashSet<Guid>(before.Layers.SelectMany(layer => layer.Elements).Select(element => element.Id));
            return new HashSet<Guid>(after.Layers.SelectMany(layer => layer.Elements).Select(element => element.Id).Where(id => !old.Contains(id)));
        }

        /// <summary>
        /// The document was saved: the undo steps stay, but it has no changes to save any more
        /// </summary>
        public void MarkSaved(string filePath, string title)
        {
            FilePath = filePath;
            Title = title;
            IsModified = false;
        }

        public void Dispose()
        {
            _tool.Cancel();
            Composition.Dispose();
        }
    }
}
