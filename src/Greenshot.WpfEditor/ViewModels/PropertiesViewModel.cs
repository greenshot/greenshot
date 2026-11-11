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
using System.Windows.Media;
using Greenshot.Base.Wpf.Emoji;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.WpfEditor.Editing;
using Greenshot.WpfEditor.Tools;
using TextAlignment = Greenshot.Document.TextAlignment;

namespace Greenshot.WpfEditor.ViewModels
{
    /// <summary>
    /// The properties panel: the style of the selected elements, or with nothing selected the style new elements of the
    /// tool get. A change of a selected element also becomes the default of its tool, like in the old editor.
    /// </summary>
    public sealed class PropertiesViewModel : ObservableObject
    {
        private DocumentTabViewModel _tab;
        private bool _updating;

        public DocumentTabViewModel Tab
        {
            get => _tab;
            set
            {
                if (_tab == value)
                {
                    return;
                }

                if (_tab != null)
                {
                    _tab.DocumentChanged -= Refresh;
                    _tab.SelectionChanged -= Refresh;
                    _tab.PropertyChanged -= TabPropertyChanged;
                }

                _tab = value;
                if (_tab != null)
                {
                    _tab.DocumentChanged += Refresh;
                    _tab.SelectionChanged += Refresh;
                    _tab.PropertyChanged += TabPropertyChanged;
                }

                Refresh();
            }
        }

        private void TabPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DocumentTabViewModel.ToolKind))
            {
                Refresh();
            }
        }

        /// <summary>
        /// The elements the panel changes; empty when it changes the defaults of the tool
        /// </summary>
        private IReadOnlyList<Element> Targets => _tab?.SelectedElements ?? Array.Empty<Element>();

        /// <summary>
        /// The element the panel shows the values of: the first selected one, else an example of the tool
        /// </summary>
        private Element Sample => Targets.FirstOrDefault() ?? ExampleOf(_tab?.ToolKind ?? ToolKind.Select);

        private Style SampleStyle
        {
            get
            {
                var sample = Sample;
                if (sample == null)
                {
                    return Style.Empty;
                }

                return Targets.Count > 0 ? _tab.Document.StyleOf(sample) : EditorSession.Current.StyleFor(KindOf(sample)).Over(StyleDefaults.For(sample));
            }
        }

        public bool IsVisible => Sample != null;

        public bool ShowsSelection => Targets.Count > 0;

        public bool ShowsCanvas => _tab != null && !ShowsSelection && _tab.ToolKind == ToolKind.Select;

        public string Heading => Sample is Element sample ? EditorTexts.NameOf(KindOf(sample)) : EditorTexts.Wpf.Canvas;

        public bool HasStroke => Sample is not (null or TextElement or StepLabelElement or HighlightElement or ObfuscateElement or ImageElement or EmojiElement or CutMarkElement or FrameElement);

        public bool HasFill => Sample is RectangleElement or EllipseElement or TextElement or SpeechBubbleElement or StepLabelElement or ArrowElement;

        public bool HasShadow => Sample is not (null or HighlightElement or ObfuscateElement or StepLabelElement or FrameElement or CutMarkElement);

        public bool HasText => Sample is TextElement or SpeechBubbleElement or RectangleElement or EllipseElement or LineElement or ArrowElement or StepLabelElement;

        public bool HasArrowHeads => Sample is ArrowElement;

        public bool IsHighlight => Sample is HighlightElement;

        public bool IsObfuscate => Sample is ObfuscateElement;

        public bool IsCutMark => Sample is CutMarkElement;

        public bool IsEmoji => Sample is EmojiElement;

        private EmojiPickerViewModel _emojiPicker;

        /// <summary>
        /// The emoji picker of the other editor (history, categories, search), made when the emoji section is first shown
        /// </summary>
        public EmojiPickerViewModel EmojiPicker
        {
            get
            {
                if (_emojiPicker == null)
                {
                    _emojiPicker = new EmojiPickerViewModel(EditorSession.Current.Host?.EmojiHistory ?? new SessionEmojiHistory()) { CurrentEmoji = Emoji };
                    _emojiPicker.Picked += (_, args) => Emoji = args.Emoji;
                }

                return _emojiPicker;
            }
        }

        public string Emoji
        {
            get => Sample is EmojiElement emoji && Targets.Count > 0 ? emoji.Emoji : EditorSession.Current.Options.Emoji;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    ChangeOption(options => options.Emoji = value, element => element is EmojiElement emoji ? emoji with { Emoji = value } : element, "emoji");
                }
            }
        }

        public bool HasArea => Targets.Count == 1 && ElementShapes.AreaOf(Targets[0]) != null;

        public bool CanTurn => Targets.Count == 1 && ElementShapes.CanTurn(Targets[0]);

        public Color LineColor
        {
            get => ToColor(SampleStyle.Stroke?.Color ?? Rgba.Transparent);
            set => ChangeStyle(style => style with { Stroke = (style.Stroke ?? new StrokeStyle()) with { Color = ToRgba(value), Enabled = value.A > 0 } }, "stroke.color");
        }

        public double LineWidth
        {
            get => SampleStyle.Stroke is { Enabled: not false } stroke ? stroke.Width ?? 0 : 0;
            set => ChangeStyle(style => style with { Stroke = (style.Stroke ?? new StrokeStyle()) with { Width = Math.Round(value), Enabled = value > 0 } }, "stroke.width");
        }

        public Color FillColor
        {
            get => SampleStyle.Fill is { Enabled: not false } fill ? ToColor(fill.Color ?? Rgba.Transparent) : Colors.Transparent;
            set => ChangeStyle(style => style with { Fill = new FillStyle { Color = ToRgba(value), Enabled = value.A > 0 } }, "fill.color");
        }

        public bool Shadow
        {
            get => SampleStyle.Shadow?.Enabled == true;
            set => ChangeStyle(style => style with { Shadow = (style.Shadow ?? new ShadowStyle()) with { Enabled = value } }, null);
        }

        public IReadOnlyList<string> FontFamilies { get; } = Fonts.SystemFontFamilies.Select(family => family.Source).OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase).ToList();

        public string FontFamily
        {
            get => TextStyle.Families(SampleStyle.Text?.Family).FirstOrDefault() ?? string.Empty;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    ChangeText(text => text with { Family = value }, "text.family");
                }
            }
        }

        public double FontSize
        {
            get => SampleStyle.Text?.Size ?? 11;
            set => ChangeText(text => text with { Size = Math.Max(4, Math.Min(400, value)) }, "text.size");
        }

        public bool Bold
        {
            get => (SampleStyle.Text?.Weight ?? 400) >= 600;
            set => ChangeText(text => text with { Weight = value ? 700 : 400 }, null);
        }

        public bool Italic
        {
            get => SampleStyle.Text?.Italic == true;
            set => ChangeText(text => text with { Italic = value }, null);
        }

        public Color TextColor
        {
            get => ToColor(SampleStyle.Text?.Color ?? SampleStyle.Stroke?.Color ?? new Rgba(0, 0, 0, 255));
            set => ChangeText(text => text with { Color = ToRgba(value) }, "text.color");
        }

        public TextAlignment HorizontalAlignment
        {
            get => SampleStyle.Text?.Horizontal ?? TextAlignment.Center;
            set => ChangeText(text => text with { Horizontal = value }, null);
        }

        public TextAlignment VerticalAlignment
        {
            get => SampleStyle.Text?.Vertical ?? TextAlignment.Center;
            set => ChangeText(text => text with { Vertical = value }, null);
        }

        public ArrowHeads ArrowHeads
        {
            get => Sample is ArrowElement arrow ? (Targets.Count > 0 ? arrow.Heads : EditorSession.Current.Options.ArrowHeads) : ArrowHeads.End;
            set => ChangeOption(options => options.ArrowHeads = value, element => element is ArrowElement arrow ? arrow with { Heads = value } : element);
        }

        public HighlightMode HighlightMode
        {
            get => Sample is HighlightElement highlight && Targets.Count > 0 ? highlight.Mode : EditorSession.Current.Options.HighlightMode;
            set => ChangeOption(options => options.HighlightMode = value, element => element is HighlightElement highlight ? highlight with { Mode = value } : element);
        }

        public Color HighlightColor
        {
            get => ToColor(Sample is HighlightElement highlight ? highlight.Color : new Rgba(255, 255, 0, 255));
            set => ChangeOption(_ => { }, element => element is HighlightElement highlight ? highlight with { Color = ToRgba(value) } : element, "highlight.color");
        }

        public int Magnification
        {
            get => Sample is HighlightElement highlight && Targets.Count > 0 ? highlight.Magnification : EditorSession.Current.Options.Magnification;
            set => ChangeOption(options => options.Magnification = value, element => element is HighlightElement highlight ? highlight with { Magnification = Math.Max(1, value) } : element, "highlight.magnification");
        }

        public ObfuscateMode ObfuscateMode
        {
            get => Sample is ObfuscateElement obfuscate && Targets.Count > 0 ? obfuscate.Mode : EditorSession.Current.Options.ObfuscateMode;
            set => ChangeOption(options => options.ObfuscateMode = value, element => element is ObfuscateElement obfuscate ? obfuscate with { Mode = value } : element);
        }

        public int ObfuscateSize
        {
            get => Sample is ObfuscateElement obfuscate && Targets.Count > 0
                ? obfuscate.Size
                : EditorSession.Current.Options.ObfuscateMode == ObfuscateMode.Blur ? EditorSession.Current.Options.BlurRadius : EditorSession.Current.Options.PixelSize;
            set => ChangeOption(options =>
            {
                if (options.ObfuscateMode == ObfuscateMode.Blur)
                {
                    options.BlurRadius = value;
                }
                else
                {
                    options.PixelSize = value;
                }
            }, element => element is ObfuscateElement obfuscate ? obfuscate with { Size = Math.Max(1, value) } : element, "obfuscate.size");
        }

        public EdgeStyle CutMarkShape
        {
            get => EditorSession.Current.Options.CutMarkShape;
            set => ChangeOption(options => options.CutMarkShape = value, element => element);
        }

        public IReadOnlyList<ArrowHeads> AllArrowHeads { get; } = (ArrowHeads[])Enum.GetValues(typeof(ArrowHeads));

        public IReadOnlyList<HighlightMode> AllHighlightModes { get; } = (HighlightMode[])Enum.GetValues(typeof(HighlightMode));

        public IReadOnlyList<ObfuscateMode> AllObfuscateModes { get; } = (ObfuscateMode[])Enum.GetValues(typeof(ObfuscateMode));

        public IReadOnlyList<EdgeStyle> AllEdgeStyles { get; } = (EdgeStyle[])Enum.GetValues(typeof(EdgeStyle));

        // Position and size of one selected element, in canvas pixels
        public double AreaX
        {
            get => AreaOfTarget()?.X ?? 0;
            set => ChangeArea(area => area with { X = value });
        }

        public double AreaY
        {
            get => AreaOfTarget()?.Y ?? 0;
            set => ChangeArea(area => area with { Y = value });
        }

        public double AreaWidth
        {
            get => AreaOfTarget()?.Width ?? 0;
            set => ChangeArea(area => area with { Width = Math.Max(1, value) });
        }

        public double AreaHeight
        {
            get => AreaOfTarget()?.Height ?? 0;
            set => ChangeArea(area => area with { Height = Math.Max(1, value) });
        }

        public double Rotation
        {
            get => Targets.Count == 1 ? Targets[0].Rotation : 0;
            set
            {
                if (!_updating && CanTurn)
                {
                    _tab.ChangeSelected(element => ElementShapes.Unanchored(ElementShapes.WithRotation(element, value)), EditorTexts.Wpf.UndoTurn, "rotation");
                }
            }
        }

        // The canvas, shown when nothing is selected
        public int CanvasWidth => (int)Math.Round(_tab?.Document.CanvasArea().Width ?? 0);

        public int CanvasHeight => (int)Math.Round(_tab?.Document.CanvasArea().Height ?? 0);

        public Color Background
        {
            get => ToColor(_tab?.Document.Canvas.Background ?? Rgba.Transparent);
            set
            {
                if (!_updating && _tab != null)
                {
                    _tab.ChangeDocument(document => document with { Canvas = document.Canvas with { Background = ToRgba(value) } }, EditorTexts.Wpf.UndoStyle, "canvas.background");
                }
            }
        }

        // The frame of the capture: border, shadow and torn edges
        public bool HasFrame => _tab != null && CaptureFrames.FrameOf(_tab.Document) != null;

        public EdgeStyle FrameShape
        {
            get => _tab == null ? EdgeStyle.None : CaptureFrames.FrameOf(_tab.Document)?.Shape?.Style ?? EdgeStyle.None;
            set => ChangeFrame(frame => frame with { Shape = (frame.Shape ?? new EdgeShape(value)) with { Style = value } }, null);
        }

        public double FrameWidth
        {
            get => _tab == null ? 0 : CaptureFrames.FrameOf(_tab.Document)?.Band?.Width ?? 0;
            set => ChangeFrame(frame => frame with { Band = value <= 0 ? null : new FrameBand(Math.Round(value), frame.Band?.Color ?? new Rgba(105, 105, 105, 255)) }, "frame.width");
        }

        public Color FrameColor
        {
            get => ToColor(_tab == null ? Rgba.Transparent : CaptureFrames.FrameOf(_tab.Document)?.Band?.Color ?? new Rgba(105, 105, 105, 255));
            set => ChangeFrame(frame => frame with { Band = new FrameBand(frame.Band?.Width ?? 4, ToRgba(value)) }, "frame.color");
        }

        public bool FrameShadow
        {
            get => _tab != null && CaptureFrames.FrameOf(_tab.Document) is FrameElement frame && _tab.Document.StyleOf(frame).Shadow?.Enabled == true;
            set => ChangeFrame(frame => frame with { Style = frame.Style with { Shadow = (frame.Style.Shadow ?? new ShadowStyle()) with { Enabled = value } } }, null);
        }

        public void AddBorder() => ChangeFrame(frame => frame with { Band = new FrameBand(4, new Rgba(105, 105, 105, 255)) }, null);

        public void AddDropShadow() => FrameShadow = true;

        public void AddTornEdges() => ChangeFrame(frame => frame with { Shape = new EdgeShape(EdgeStyle.Torn), Style = frame.Style with { Shadow = (frame.Style.Shadow ?? new ShadowStyle()) with { Enabled = true } } }, null);

        public void RemoveFrame()
        {
            if (_tab != null && HasFrame)
            {
                _tab.ChangeDocument(CaptureFrames.WithoutFrame, EditorTexts.Wpf.UndoEffect);
            }
        }

        private void ChangeFrame(Func<FrameElement, FrameElement> change, string mergeKey)
        {
            if (!_updating && _tab != null)
            {
                _tab.ChangeDocument(document => CaptureFrames.WithFrame(document, change), EditorTexts.Wpf.UndoEffect, mergeKey);
            }
        }

        private RectD? AreaOfTarget() => Targets.Count == 1 ? ElementShapes.AreaOf(Targets[0]) : null;

        private void ChangeArea(Func<RectD, RectD> change)
        {
            if (_updating || !HasArea)
            {
                return;
            }

            _tab.ChangeSelected(element => ElementShapes.AreaOf(element) is RectD area ? ElementShapes.Unanchored(ElementShapes.WithArea(element, change(area))) : element, EditorTexts.Wpf.UndoResize, "area");
        }

        private void ChangeText(Func<TextStyle, TextStyle> change, string mergeKey) =>
            ChangeStyle(style => style with { Text = change(style.Text ?? new TextStyle()) }, mergeKey);

        /// <summary>
        /// Changes the style of the selected elements and the default of their tools, or with nothing selected the default of the tool
        /// </summary>
        private void ChangeStyle(Func<Style, Style> change, string mergeKey)
        {
            if (_updating || _tab == null)
            {
                return;
            }

            var targets = Targets;
            foreach (var kind in (targets.Count > 0 ? targets.Select(KindOf) : new[] { _tab.ToolKind }).Distinct())
            {
                EditorSession.Current.SetStyle(kind, change(EditorSession.Current.StyleFor(kind)));
            }

            if (targets.Count > 0)
            {
                _tab.ChangeSelected(element => element with { Style = change(element.Style ?? Style.Empty) }, EditorTexts.Wpf.UndoStyle, mergeKey);
            }
            else
            {
                Refresh();
            }
        }

        private void ChangeOption(Action<ToolOptions> option, Func<Element, Element> change, string mergeKey = null)
        {
            if (_updating || _tab == null)
            {
                return;
            }

            option(EditorSession.Current.Options);
            if (Targets.Count > 0)
            {
                _tab.ChangeSelected(change, EditorTexts.Wpf.UndoStyle, mergeKey);
            }
            else
            {
                Refresh();
            }
        }

        /// <summary>
        /// Shows the values of the selection or tool again
        /// </summary>
        public void Refresh()
        {
            _updating = true;
            try
            {
                OnPropertyChanged(string.Empty);
                if (_emojiPicker != null)
                {
                    _emojiPicker.CurrentEmoji = Emoji;
                }
            }
            finally
            {
                _updating = false;
            }
        }

        public static ToolKind KindOf(Element element) => element switch
        {
            RectangleElement => ToolKind.Rectangle,
            EllipseElement => ToolKind.Ellipse,
            ArrowElement => ToolKind.Arrow,
            LineElement => ToolKind.Line,
            FreehandElement => ToolKind.Freehand,
            TextElement => ToolKind.Text,
            SpeechBubbleElement => ToolKind.SpeechBubble,
            StepLabelElement => ToolKind.StepLabel,
            EmojiElement => ToolKind.Emoji,
            HighlightElement => ToolKind.Highlight,
            ObfuscateElement => ToolKind.Obfuscate,
            CutMarkElement => ToolKind.CutMark,
            _ => ToolKind.Select
        };

        /// <summary>
        /// An element of the tool, to show the style new elements get
        /// </summary>
        private static Element ExampleOf(ToolKind kind) => kind switch
        {
            ToolKind.Rectangle => new RectangleElement(Guid.Empty, default),
            ToolKind.Ellipse => new EllipseElement(Guid.Empty, default),
            ToolKind.Line => new LineElement(Guid.Empty, default, default),
            ToolKind.Arrow => new ArrowElement(Guid.Empty, default, default),
            ToolKind.Freehand => new FreehandElement(Guid.Empty, Array.Empty<PointD>()),
            ToolKind.Text => new TextElement(Guid.Empty, default, string.Empty),
            ToolKind.SpeechBubble => new SpeechBubbleElement(Guid.Empty, default, string.Empty, default),
            ToolKind.StepLabel => new StepLabelElement(Guid.Empty, default),
            ToolKind.Highlight => new HighlightElement(Guid.Empty, default, EditorSession.Current.Options.HighlightMode),
            ToolKind.Obfuscate => new ObfuscateElement(Guid.Empty, default, EditorSession.Current.Options.ObfuscateMode, EditorSession.Current.Options.PixelSize),
            ToolKind.CutMark => new CutMarkElement(Guid.Empty, default, true, 0),
            ToolKind.Emoji => new EmojiElement(Guid.Empty, default, EditorSession.Current.Options.Emoji),
            _ => null
        };

        public static Color ToColor(Rgba rgba) => Color.FromArgb(rgba.A, rgba.R, rgba.G, rgba.B);

        public static Rgba ToRgba(Color color) => new(color.R, color.G, color.B, color.A);
    }
}
