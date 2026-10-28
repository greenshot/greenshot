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

using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Rendering;
using Greenshot.Linux.Editor.Serialization;
using SkiaSharp;

namespace Greenshot.Linux.Editor;

/// <summary>
/// The capture being edited: the image and the elements on it, the selection and the undo history.
/// The Surface of the Windows editor without the UI.
/// The image is never changed in place, effects replace it; so undo can keep the old image.
/// </summary>
public sealed partial class EditorSurface : IDisposable
{
    public const int MaxUndoSteps = 50;

    private readonly List<DrawableElement> _elements = new();
    private readonly List<DrawableElement> _selection = new();
    private readonly Stack<Snapshot> _undo = new();
    private readonly Stack<Snapshot> _redo = new();

    private sealed record Snapshot(SKBitmap Image, string Elements, int CounterStart);

    public EditorSurface(SKBitmap image)
    {
        Image = image;
    }

    /// <summary>
    /// The redraw is needed: elements, the image or the selection changed
    /// </summary>
    public event Action? Changed;

    public event Action? SelectionChanged;

    public SKBitmap Image { get; private set; }

    public int Width => Image.Width;

    public int Height => Image.Height;

    /// <summary>
    /// The number of the first step label
    /// </summary>
    public int CounterStart { get; set; } = 1;

    /// <summary>
    /// The bottom element first
    /// </summary>
    public IReadOnlyList<DrawableElement> Elements => _elements;

    public IReadOnlyList<DrawableElement> Selection => _selection;

    /// <summary>
    /// Changed since it was saved or exported
    /// </summary>
    public bool IsModified { get; set; }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    /// <summary>
    /// Remember the state before a change, so it can be undone. Call it before every change.
    /// </summary>
    public void Checkpoint()
    {
        _undo.Push(TakeSnapshot());
        ClearRedo();
        TrimUndo();
        IsModified = true;
    }

    /// <summary>
    /// Forget the last checkpoint without restoring it, when the change didn't happen after all
    /// (e.g. a click which created no element)
    /// </summary>
    public void DiscardCheckpoint()
    {
        if (_undo.Count > 0)
        {
            ReleaseImage(_undo.Pop().Image);
        }
    }

    public void Undo()
    {
        if (_undo.Count == 0)
        {
            return;
        }

        _redo.Push(TakeSnapshot());
        Restore(_undo.Pop());
    }

    public void Redo()
    {
        if (_redo.Count == 0)
        {
            return;
        }

        _undo.Push(TakeSnapshot());
        Restore(_redo.Pop());
    }

    /// <summary>
    /// Tell the views to redraw, after an element was changed directly
    /// </summary>
    public void Invalidate() => Changed?.Invoke();

    public void Add(DrawableElement element, bool select = true)
    {
        element.FitToImage(Width, Height);
        _elements.Add(element);
        if (select)
        {
            Select(element);
        }

        Changed?.Invoke();
    }

    public void Remove(IEnumerable<DrawableElement> elements)
    {
        var removed = elements.ToList();
        foreach (var element in removed)
        {
            _elements.Remove(element);
            _selection.Remove(element);
            (element as IDisposable)?.Dispose();
        }

        SelectionChanged?.Invoke();
        Changed?.Invoke();
    }

    public void Select(DrawableElement element, bool addToSelection = false)
    {
        if (!addToSelection)
        {
            _selection.Clear();
        }

        if (!_selection.Contains(element))
        {
            _selection.Add(element);
        }

        SelectionChanged?.Invoke();
        Changed?.Invoke();
    }

    public void SelectMany(IEnumerable<DrawableElement> elements)
    {
        _selection.Clear();
        _selection.AddRange(elements.Where(_elements.Contains));
        SelectionChanged?.Invoke();
        Changed?.Invoke();
    }

    public void ToggleSelection(DrawableElement element)
    {
        if (!_selection.Remove(element))
        {
            _selection.Add(element);
        }

        SelectionChanged?.Invoke();
        Changed?.Invoke();
    }

    public void ClearSelection()
    {
        if (_selection.Count == 0)
        {
            return;
        }

        _selection.Clear();
        SelectionChanged?.Invoke();
        Changed?.Invoke();
    }

    /// <summary>
    /// The top most element at the point, null when there is none
    /// </summary>
    public DrawableElement? ElementAt(SKPoint point, float tolerance)
    {
        for (int i = _elements.Count - 1; i >= 0; i--)
        {
            if (_elements[i].HitTest(point, tolerance))
            {
                return _elements[i];
            }
        }

        return null;
    }

    /// <summary>
    /// The number shown on the step label: its place among the labels plus the counter start
    /// </summary>
    public int StepNumber(StepLabelElement label)
    {
        int number = CounterStart;
        foreach (var element in _elements)
        {
            if (element == label)
            {
                return number;
            }

            if (element is StepLabelElement)
            {
                number++;
            }
        }

        return number;
    }

    public enum ZOrder
    {
        Top,
        Up,
        Down,
        Bottom,
    }

    /// <summary>
    /// Bring the selection to the front or the back, or one step up or down
    /// </summary>
    public void Arrange(ZOrder order)
    {
        if (_selection.Count == 0)
        {
            return;
        }

        Checkpoint();
        var selected = _elements.Where(_selection.Contains).ToList();
        switch (order)
        {
            case ZOrder.Top:
                _elements.RemoveAll(selected.Contains);
                _elements.AddRange(selected);
                break;
            case ZOrder.Bottom:
                _elements.RemoveAll(selected.Contains);
                _elements.InsertRange(0, selected);
                break;
            case ZOrder.Up:
                for (int i = _elements.Count - 2; i >= 0; i--)
                {
                    if (_selection.Contains(_elements[i]) && !_selection.Contains(_elements[i + 1]))
                    {
                        (_elements[i], _elements[i + 1]) = (_elements[i + 1], _elements[i]);
                    }
                }

                break;
            case ZOrder.Down:
                for (int i = 1; i < _elements.Count; i++)
                {
                    if (_selection.Contains(_elements[i]) && !_selection.Contains(_elements[i - 1]))
                    {
                        (_elements[i], _elements[i - 1]) = (_elements[i - 1], _elements[i]);
                    }
                }

                break;
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Replace the image, e.g. after an effect; the elements are mapped with the matrix (rotation, crop, border).
    /// Call <see cref="Checkpoint"/> first, the old image stays in the undo history.
    /// </summary>
    public void ReplaceImage(SKBitmap image, SKMatrix elementTransform)
    {
        Image = image;
        if (!elementTransform.IsIdentity)
        {
            foreach (var element in _elements)
            {
                element.Transform(elementTransform);
            }
        }

        foreach (var element in _elements)
        {
            element.FitToImage(Width, Height);
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Draw the image and the elements; for export or the editor view
    /// </summary>
    public void Render(SKBitmap target, RenderMode mode)
    {
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(Image, 0, 0);
        var context = new RenderContext(canvas, target, mode, StepNumber, _elements);
        foreach (var element in _elements)
        {
            element.Draw(context);
        }

        canvas.Flush();
    }

    /// <summary>
    /// The finished image with all elements, the caller owns it
    /// </summary>
    public SKBitmap Flatten()
    {
        var result = new SKBitmap(new SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        Render(result, RenderMode.Export);
        return result;
    }

    public string SerializeElements() => ElementSerializer.Serialize(_elements, CounterStart);

    /// <summary>
    /// Replace the elements with the ones of a saved surface
    /// </summary>
    public void LoadElements(string json)
    {
        var (elements, counterStart) = ElementSerializer.Deserialize(json);
        DisposeElements(_elements);
        _elements.Clear();
        _elements.AddRange(elements);
        foreach (var element in _elements)
        {
            element.FitToImage(Width, Height);
        }

        _selection.Clear();
        CounterStart = counterStart;
        SelectionChanged?.Invoke();
        Changed?.Invoke();
    }

    private Snapshot TakeSnapshot() => new(Image, SerializeElements(), CounterStart);

    private void Restore(Snapshot snapshot)
    {
        Image = snapshot.Image;
        LoadElements(snapshot.Elements);
        CounterStart = snapshot.CounterStart;
        IsModified = true;
    }

    private void ClearRedo()
    {
        while (_redo.Count > 0)
        {
            ReleaseImage(_redo.Pop().Image);
        }
    }

    private void TrimUndo()
    {
        if (_undo.Count <= MaxUndoSteps)
        {
            return;
        }

        var kept = _undo.Reverse().Skip(_undo.Count - MaxUndoSteps).ToList();
        var dropped = _undo.Reverse().Take(_undo.Count - MaxUndoSteps).ToList();
        _undo.Clear();
        foreach (var snapshot in kept)
        {
            _undo.Push(snapshot);
        }

        foreach (var snapshot in dropped)
        {
            ReleaseImage(snapshot.Image);
        }
    }

    /// <summary>
    /// Dispose an image which is no longer in the history and not the current one
    /// </summary>
    private void ReleaseImage(SKBitmap image)
    {
        if (image != Image && _undo.All(s => s.Image != image) && _redo.All(s => s.Image != image))
        {
            image.Dispose();
        }
    }

    private static void DisposeElements(IEnumerable<DrawableElement> elements)
    {
        foreach (var element in elements)
        {
            (element as IDisposable)?.Dispose();
        }
    }

    public void Dispose()
    {
        DisposeElements(_elements);
        var images = new HashSet<SKBitmap>(_undo.Select(s => s.Image).Concat(_redo.Select(s => s.Image))) { Image };
        foreach (var image in images)
        {
            image.Dispose();
        }

        _undo.Clear();
        _redo.Clear();
    }
}
