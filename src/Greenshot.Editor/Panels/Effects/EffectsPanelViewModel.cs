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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Effects;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Drawing;
using log4net;

namespace Greenshot.Editor.Panels.Effects
{
    /// <summary>
    /// The Effects panel of one editor window: the effect, its presets and settings, a preview of the capture with the effect,
    /// and Apply, which applies it as one undo step. No UI toolkit here, the preview is a System.Drawing bitmap like the capture.
    /// </summary>
    public sealed class EffectsPanelViewModel : INotifyPropertyChanged, IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EffectsPanelViewModel));

        /// <summary>
        /// The largest width or height of a resized capture (GDI+ fails a bit above 25000)
        /// </summary>
        public const int MaximumDimension = 30_000;

        /// <summary>
        /// The size of the overview, the panel scales it down further
        /// </summary>
        private const int OverviewMaximumWidth = 640;
        private const int OverviewMaximumHeight = 480;

        /// <summary>
        /// The size of the corner which is shown at 100 %
        /// </summary>
        private const int DetailWidth = 280;
        private const int DetailHeight = 160;

        // A border has no settings in greenshot.ini, the last one is kept until Greenshot ends (like the copied style)
        private static int _lastBorderWidth = 2;
        private static Color _lastBorderColor = Color.Black;
        private static Color _lastTransparencyColor = Color.White;

        private readonly IEditorPanelContext _context;
        private readonly IEditorConfiguration _configuration;
        private readonly Action _applied;
        private Surface _surface;
        private EffectKind _effect = EffectKind.Border;
        private bool _isShown;
        private bool _previewRunning;
        private bool _previewDirty;
        private bool _applying;
        private bool _disposed;
        private Bitmap _preview;
        private Bitmap _previewDetail;

        private int _borderWidth = _lastBorderWidth;
        private Color _borderColor = _lastBorderColor;
        private readonly DropShadowEffect _dropShadow;
        private TornEdgeEffect _tornEdges;
        private Color _transparencyColor = _lastTransparencyColor;
        private string _resizeWidth = string.Empty;
        private string _resizeHeight = string.Empty;
        private bool _keepAspectRatio = true;
        private int _resizedWidth;
        private int _resizedHeight;
        private bool _updatingSize;

        /// <param name="context">IEditorPanelContext of the editor window</param>
        /// <param name="configuration">IEditorConfiguration with the last settings of the drop shadow and the torn edges</param>
        /// <param name="applied">called after an effect was applied, e.g. to update the undo button of the window</param>
        public EffectsPanelViewModel(IEditorPanelContext context, IEditorConfiguration configuration, Action applied = null)
        {
            _context = context;
            _configuration = configuration;
            _applied = applied;
            _dropShadow = Copy(configuration?.DropShadowEffectSettings ?? new DropShadowEffect());
            _tornEdges = Copy(configuration?.TornEdgeEffectSettings ?? new TornEdgeEffect());
            _tornEdges.Reseed();
            Effects = Enum.GetValues(typeof(EffectKind)).Cast<EffectKind>().Select(kind => new EffectEntry(kind, GetTitle(kind))).ToList();
            _context.CurrentSurfaceChanged += OnCurrentSurfaceChanged;
            _context.ElementsChanged += OnElementsChanged;
            AttachSurface(_context.CurrentSurface as Surface);
            ResetResize();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The preview changed, the view shows the new bitmaps
        /// </summary>
        public event EventHandler PreviewChanged;

        /// <summary>
        /// The effects to choose from
        /// </summary>
        public IReadOnlyList<EffectEntry> Effects { get; }

        public int EffectIndex
        {
            get => (int)_effect;
            set => Effect = (EffectKind)Math.Max(0, Math.Min(Effects.Count - 1, value));
        }

        /// <summary>
        /// The effect which is shown and applied
        /// </summary>
        public EffectKind Effect
        {
            get => _effect;
            set
            {
                if (_effect == value)
                {
                    return;
                }

                _effect = value;
                if (value == EffectKind.TornEdges)
                {
                    TakeTornEdgesOfSurface();
                }
                else if (value == EffectKind.Resize)
                {
                    ResetResize();
                }

                OnPropertyChanged(nameof(EffectIndex));
                OnEffectChanged();
            }
        }

        private void OnEffectChanged()
        {
            OnPropertyChanged(nameof(Effect));
            OnPropertyChanged(nameof(Presets));
            OnPropertyChanged(nameof(HasPresets));
            OnPropertyChanged(nameof(ShowsBorder));
            OnPropertyChanged(nameof(ShowsShadow));
            OnPropertyChanged(nameof(ShowsTornEdges));
            OnPropertyChanged(nameof(ShowsTransparency));
            OnPropertyChanged(nameof(ShowsResize));
            OnPropertyChanged(nameof(HasNoSettings));
            OnPropertyChanged(nameof(ShowsDetail));
            OnPropertyChanged(nameof(BorderWidth));
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(TransparencyColor));
            OnShadowChanged();
            OnTornEdgesChanged();
            OnStateChanged();
        }

        public bool ShowsBorder => _effect == EffectKind.Border;

        public bool ShowsShadow => _effect == EffectKind.DropShadow || _effect == EffectKind.TornEdges && _tornEdges.GenerateShadow;

        public bool ShowsTornEdges => _effect == EffectKind.TornEdges;

        public bool ShowsTransparency => _effect == EffectKind.RemoveTransparency;

        public bool ShowsResize => _effect == EffectKind.Resize;

        public bool HasNoSettings => _effect is EffectKind.Grayscale or EffectKind.Invert;

        /// <summary>
        /// The effect changes the edges: the bottom right corner is shown at 100 %, the overview is too small for them
        /// </summary>
        public bool ShowsDetail => _effect is EffectKind.Border or EffectKind.DropShadow or EffectKind.TornEdges;

        /// <summary>
        /// The presets of the effect
        /// </summary>
        public IReadOnlyList<EffectPreset> Presets => CreatePresets(_effect);

        public bool HasPresets => Presets.Count > 0;

        /// <summary>
        /// Set the fields to the preset, they can be changed afterwards
        /// </summary>
        public void ApplyPreset(EffectPreset preset)
        {
            if (preset == null)
            {
                return;
            }

            preset.ApplyTo(this);
            OnEffectChanged();
            RequestPreview();
        }

        #region Border

        public int BorderWidth
        {
            get => _borderWidth;
            set => SetSetting(ref _borderWidth, Math.Max(1, Math.Min(100, value)));
        }

        public Color BorderColor
        {
            get => _borderColor;
            set => SetSetting(ref _borderColor, value);
        }

        #endregion

        #region Shadow, of the drop shadow or of the torn edges

        private DropShadowEffect CurrentShadow => _effect == EffectKind.TornEdges ? _tornEdges : _dropShadow;

        /// <summary>
        /// 1 to 40, like the slider: 40 is black
        /// </summary>
        public int ShadowDarkness
        {
            get => Math.Max(1, Math.Min(40, (int)Math.Round(CurrentShadow.Darkness * 40)));
            set
            {
                CurrentShadow.Darkness = Math.Max(1, Math.Min(40, value)) / 40f;
                OnSettingChanged();
            }
        }

        public int ShadowSize
        {
            get => Math.Max(1, Math.Min(20, CurrentShadow.ShadowSize));
            set
            {
                CurrentShadow.ShadowSize = Math.Max(1, Math.Min(20, value));
                OnSettingChanged();
            }
        }

        public int ShadowOffsetX
        {
            get => Math.Max(-20, Math.Min(20, CurrentShadow.ShadowOffset.X));
            set
            {
                CurrentShadow.ShadowOffset = new NativePoint(Math.Max(-20, Math.Min(20, value)), CurrentShadow.ShadowOffset.Y);
                OnSettingChanged();
            }
        }

        public int ShadowOffsetY
        {
            get => Math.Max(-20, Math.Min(20, CurrentShadow.ShadowOffset.Y));
            set
            {
                CurrentShadow.ShadowOffset = new NativePoint(CurrentShadow.ShadowOffset.X, Math.Max(-20, Math.Min(20, value)));
                OnSettingChanged();
            }
        }

        private void OnShadowChanged()
        {
            OnPropertyChanged(nameof(ShadowDarkness));
            OnPropertyChanged(nameof(ShadowSize));
            OnPropertyChanged(nameof(ShadowOffsetX));
            OnPropertyChanged(nameof(ShadowOffsetY));
        }

        #endregion

        #region Torn edges

        /// <summary>
        /// The capture has torn edges already: Apply changes them, there is only one torn edge element
        /// </summary>
        public bool ChangesTornEdges => _surface?.TornEdges != null;

        public bool TornTop
        {
            get => GetEdge(0);
            set => SetEdge(0, value);
        }

        public bool TornRight
        {
            get => GetEdge(1);
            set => SetEdge(1, value);
        }

        public bool TornBottom
        {
            get => GetEdge(2);
            set => SetEdge(2, value);
        }

        public bool TornLeft
        {
            get => GetEdge(3);
            set => SetEdge(3, value);
        }

        public int ToothHeight
        {
            get => Math.Max(1, Math.Min(40, _tornEdges.ToothHeight));
            set
            {
                _tornEdges.ToothHeight = Math.Max(1, Math.Min(40, value));
                OnSettingChanged();
            }
        }

        /// <summary>
        /// How wide a tooth is, the torn edges element uses one range for all sides
        /// </summary>
        public int ToothRange
        {
            get => Math.Max(2, Math.Min(40, _tornEdges.HorizontalToothRange));
            set
            {
                int range = Math.Max(2, Math.Min(40, value));
                _tornEdges.HorizontalToothRange = range;
                _tornEdges.VerticalToothRange = range;
                OnSettingChanged();
            }
        }

        public bool TornShadow
        {
            get => _tornEdges.GenerateShadow;
            set
            {
                _tornEdges.GenerateShadow = value;
                OnPropertyChanged(nameof(ShowsShadow));
                OnSettingChanged();
            }
        }

        /// <summary>
        /// The color of the torn off parts, transparent by default
        /// </summary>
        public Color TornBackgroundColor
        {
            get => _tornEdges.BackgroundColor;
            set
            {
                _tornEdges.BackgroundColor = value;
                OnSettingChanged();
            }
        }

        /// <summary>
        /// Other random edges, with the same settings
        /// </summary>
        public void NewEdges()
        {
            _tornEdges.Reseed();
            OnSettingChanged();
        }

        private bool GetEdge(int index) => _tornEdges.Edges is { Length: 4 } edges && edges[index];

        private void SetEdge(int index, bool value)
        {
            var edges = _tornEdges.Edges is { Length: 4 } current ? (bool[])current.Clone() : new[] { true, true, true, true };
            edges[index] = value;
            _tornEdges.Edges = edges;
            OnSettingChanged();
        }

        private void OnTornEdgesChanged()
        {
            OnPropertyChanged(nameof(ChangesTornEdges));
            OnPropertyChanged(nameof(ApplyText));
            OnPropertyChanged(nameof(TornTop));
            OnPropertyChanged(nameof(TornRight));
            OnPropertyChanged(nameof(TornBottom));
            OnPropertyChanged(nameof(TornLeft));
            OnPropertyChanged(nameof(ToothHeight));
            OnPropertyChanged(nameof(ToothRange));
            OnPropertyChanged(nameof(TornShadow));
            OnPropertyChanged(nameof(TornBackgroundColor));
            OnPropertyChanged(nameof(ShowsShadow));
        }

        /// <summary>
        /// Torn edges which are there are shown with their settings, a change replaces them
        /// </summary>
        private void TakeTornEdgesOfSurface()
        {
            if (_surface?.TornEdges is { } tornEdges)
            {
                _tornEdges = tornEdges.GetSettings();
            }
        }

        #endregion

        #region Remove transparency

        public Color TransparencyColor
        {
            get => _transparencyColor;
            set => SetSetting(ref _transparencyColor, value);
        }

        private bool HasTransparency => _surface?.Image is { } image && Image.IsAlphaPixelFormat(image.PixelFormat);

        #endregion

        #region Resize

        public string ResizeWidth
        {
            get => _resizeWidth;
            set => SetResizeText(value, true);
        }

        public string ResizeHeight
        {
            get => _resizeHeight;
            set => SetResizeText(value, false);
        }

        public bool KeepAspectRatio
        {
            get => _keepAspectRatio;
            set
            {
                if (_keepAspectRatio == value)
                {
                    return;
                }

                _keepAspectRatio = value;
                OnPropertyChanged();
                if (value)
                {
                    SetResizeText(_resizeWidth, true);
                }
                else
                {
                    OnStateChanged();
                }
            }
        }

        /// <summary>
        /// Set the size to a percentage of the capture
        /// </summary>
        public void ResizeToPercent(int percent)
        {
            var size = ImageSize;
            SetResizedSize((int)Math.Round(size.Width * percent / 100.0), (int)Math.Round(size.Height * percent / 100.0));
        }

        private NativeSize ImageSize => _surface?.Image is { } image ? new NativeSize(image.Width, image.Height) : new NativeSize(0, 0);

        private bool IsResizeValid => IsValidDimension(_resizedWidth) && IsValidDimension(_resizedHeight);

        private static bool IsValidDimension(int dimension) => dimension >= 1 && dimension <= MaximumDimension;

        private void ResetResize()
        {
            var size = ImageSize;
            SetResizedSize(size.Width, size.Height);
        }

        private void SetResizedSize(int width, int height)
        {
            _updatingSize = true;
            try
            {
                _resizedWidth = width;
                _resizedHeight = height;
                _resizeWidth = width.ToString(CultureInfo.CurrentCulture);
                _resizeHeight = height.ToString(CultureInfo.CurrentCulture);
            }
            finally
            {
                _updatingSize = false;
            }

            OnPropertyChanged(nameof(ResizeWidth));
            OnPropertyChanged(nameof(ResizeHeight));
            OnStateChanged();
        }

        private void SetResizeText(string text, bool isWidth)
        {
            if (_updatingSize)
            {
                return;
            }

            if (isWidth)
            {
                _resizeWidth = text ?? string.Empty;
            }
            else
            {
                _resizeHeight = text ?? string.Empty;
            }

            int value = int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed) ? parsed : 0;
            var size = ImageSize;
            if (isWidth)
            {
                _resizedWidth = value;
                if (_keepAspectRatio && size.Width > 0 && value > 0)
                {
                    _resizedHeight = Math.Max(1, (int)Math.Round(size.Height * (double)value / size.Width));
                    _resizeHeight = _resizedHeight.ToString(CultureInfo.CurrentCulture);
                    OnPropertyChanged(nameof(ResizeHeight));
                }
            }
            else
            {
                _resizedHeight = value;
                if (_keepAspectRatio && size.Height > 0 && value > 0)
                {
                    _resizedWidth = Math.Max(1, (int)Math.Round(size.Width * (double)value / size.Height));
                    _resizeWidth = _resizedWidth.ToString(CultureInfo.CurrentCulture);
                    OnPropertyChanged(nameof(ResizeWidth));
                }
            }

            OnPropertyChanged(isWidth ? nameof(ResizeWidth) : nameof(ResizeHeight));
            OnStateChanged();
        }

        #endregion

        #region State, apply

        /// <summary>
        /// The text of the button: Apply, or the change of the torn edges which are there
        /// </summary>
        public string ApplyText => _effect == EffectKind.TornEdges && ChangesTornEdges ? Texts.Editor.EffectsChangeTornEdges : Texts.Editor.EffectsApply;

        public bool CanApply => _surface != null && !_applying && _effect switch
        {
            EffectKind.RemoveTransparency => HasTransparency,
            EffectKind.Resize => IsResizeValid && (_resizedWidth != ImageSize.Width || _resizedHeight != ImageSize.Height),
            EffectKind.TornEdges => _tornEdges.Edges?.Any(edge => edge) == true,
            _ => true
        };

        /// <summary>
        /// Why Apply can't be used, or what the effect makes; empty when there is nothing to say
        /// </summary>
        public string StatusText => _effect switch
        {
            EffectKind.RemoveTransparency when _surface != null && !HasTransparency => Texts.Editor.EffectsNoTransparency,
            EffectKind.Resize when !IsResizeValid => string.Format(Texts.Editor.EffectsResizeInvalid, MaximumDimension),
            EffectKind.Resize => string.Format(Texts.Editor.EffectsNewSize, _resizedWidth, _resizedHeight),
            EffectKind.Grayscale or EffectKind.Invert => Texts.Editor.EffectsNoSettings,
            _ => string.Empty
        };

        public bool StatusIsError => _effect == EffectKind.Resize && !IsResizeValid || _effect == EffectKind.RemoveTransparency && _surface != null && !HasTransparency;

        private void OnStateChanged()
        {
            OnPropertyChanged(nameof(CanApply));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusIsError));
            OnPropertyChanged(nameof(ApplyText));
        }

        /// <summary>
        /// The effect as it is applied, a new instance
        /// </summary>
        public IEffect CreateEffect() => _effect switch
        {
            EffectKind.Border => new BorderEffect { Width = _borderWidth, Color = _borderColor },
            EffectKind.DropShadow => Copy(_dropShadow),
            EffectKind.TornEdges => Copy(_tornEdges),
            EffectKind.Grayscale => new GrayscaleEffect(),
            EffectKind.Invert => new InvertEffect(),
            EffectKind.RemoveTransparency => new RemoveTransparencyEffect { Color = _transparencyColor },
            EffectKind.Resize => new ResizeEffect(_resizedWidth, _resizedHeight, false),
            _ => null
        };

        /// <summary>
        /// Apply the effect to the shown capture as one undo step. The torn edges are an element which can be changed later.
        /// The settings are kept for the next time, the drop shadow and the torn edges in greenshot.ini.
        /// </summary>
        public async Task ApplyAsync()
        {
            var surface = _surface;
            if (!CanApply || surface == null)
            {
                return;
            }

            _applying = true;
            OnStateChanged();
            try
            {
                Remember();
                switch (_effect)
                {
                    case EffectKind.TornEdges:
                        var settings = Copy(_tornEdges);
                        if (surface.TornEdges is { } tornEdges)
                        {
                            surface.ChangeTornEdges(tornEdges, settings);
                        }
                        else
                        {
                            var added = surface.AddTornEdges(settings);
                            // The edges of the preview
                            added.SetSeed(settings.Seed);
                        }

                        break;
                    default:
                        await surface.ApplyBitmapEffectAsync(CreateEffect());
                        break;
                }
            }
            finally
            {
                _applying = false;
            }

            _applied?.Invoke();
            if (_effect == EffectKind.Resize)
            {
                ResetResize();
            }

            OnTornEdgesChanged();
            OnStateChanged();
            RequestPreview();
        }

        private void Remember()
        {
            switch (_effect)
            {
                case EffectKind.Border:
                    _lastBorderWidth = _borderWidth;
                    _lastBorderColor = _borderColor;
                    break;
                case EffectKind.RemoveTransparency:
                    _lastTransparencyColor = _transparencyColor;
                    break;
                case EffectKind.DropShadow when _configuration != null:
                    _configuration.DropShadowEffectSettings = Copy(_dropShadow);
                    break;
                case EffectKind.TornEdges when _configuration != null:
                    _configuration.TornEdgeEffectSettings = Copy(_tornEdges);
                    break;
            }
        }

        #endregion

        #region Preview

        /// <summary>
        /// The whole capture with the effect, scaled down; null while there is none
        /// </summary>
        public Bitmap Preview => _preview;

        /// <summary>
        /// The bottom right corner of the capture with the effect at 100 %, only for the effects on the edges
        /// </summary>
        public Bitmap PreviewDetail => _previewDetail;

        /// <summary>
        /// The panel is shown: only then the preview is made
        /// </summary>
        public bool IsShown
        {
            get => _isShown;
            set
            {
                if (_isShown == value)
                {
                    return;
                }

                _isShown = value;
                if (value)
                {
                    RequestPreview();
                }
            }
        }

        /// <summary>
        /// Show this effect, e.g. picked in the Effects menu of the editor
        /// </summary>
        public void Show(EffectKind effect)
        {
            Effect = effect;
            if (effect == EffectKind.TornEdges)
            {
                // Torn edges which were added since are shown with their settings
                TakeTornEdgesOfSurface();
                OnTornEdgesChanged();
                OnStateChanged();
            }

            RequestPreview();
        }

        /// <summary>
        /// Make a new preview; when one is being made, one more follows it with the newest settings
        /// </summary>
        private void RequestPreview()
        {
            _previewDirty = true;
            if (_previewRunning || !_isShown || _surface == null)
            {
                return;
            }

            _previewRunning = true;
            AsyncCommand.Run(RunPreviewsAsync, "Effect preview");
        }

        private async Task RunPreviewsAsync()
        {
            try
            {
                while (_previewDirty && _isShown && _surface != null)
                {
                    _previewDirty = false;
                    var surface = _surface;
                    bool detail = ShowsDetail;
                    Bitmap full = null;
                    try
                    {
                        full = await CreatePreviewAsync(surface);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Couldn't make the preview of the effect", ex);
                    }

                    if (full == null || _disposed)
                    {
                        full?.Dispose();
                        if (!_disposed)
                        {
                            SetPreview(null, null);
                        }

                        continue;
                    }

                    var (overview, corner) = await ScalePreviewAsync(full, detail);
                    if (_disposed || _previewDirty || !ReferenceEquals(surface, _surface))
                    {
                        // Already old, the next one follows
                        overview.Dispose();
                        corner?.Dispose();
                        continue;
                    }

                    SetPreview(overview, corner);
                }
            }
            finally
            {
                _previewRunning = false;
            }
        }

        private Task<Bitmap> CreatePreviewAsync(Surface surface)
        {
            switch (_effect)
            {
                case EffectKind.TornEdges:
                    return surface.CreateTornEdgesPreviewAsync(Copy(_tornEdges));
                case EffectKind.Resize:
                    // The capture as it is: the preview is scaled anyway, the new size is in the status text
                    return surface.CreateEffectPreviewAsync(new ResizeEffect(ImageSize.Width, ImageSize.Height, false), false);
                case EffectKind.Grayscale:
                case EffectKind.Invert:
                case EffectKind.RemoveTransparency:
                    return surface.CreateEffectPreviewAsync(CreateEffect(), true);
                default:
                    return surface.CreateEffectPreviewAsync(CreateEffect(), false);
            }
        }

        /// <summary>
        /// The overview and the corner, made on the thread pool; the full image is disposed
        /// </summary>
        internal static async Task<(Bitmap Overview, Bitmap Detail)> ScalePreviewAsync(Bitmap full, bool withDetail)
        {
            await ThreadPoolSwitch.SwitchToThreadPoolAsync();
            using (full)
            {
                double scale = Math.Min(1.0, Math.Min((double)OverviewMaximumWidth / full.Width, (double)OverviewMaximumHeight / full.Height));
                int width = Math.Max(1, (int)Math.Round(full.Width * scale));
                int height = Math.Max(1, (int)Math.Round(full.Height * scale));
                var overview = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                overview.SetResolution(full.HorizontalResolution, full.VerticalResolution);
                using (var graphics = Graphics.FromImage(overview))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.DrawImage(full, new Rectangle(0, 0, width, height), 0, 0, full.Width, full.Height, GraphicsUnit.Pixel);
                }

                Bitmap detail = null;
                if (withDetail)
                {
                    var corner = new Rectangle(Math.Max(0, full.Width - DetailWidth), Math.Max(0, full.Height - DetailHeight),
                        Math.Min(DetailWidth, full.Width), Math.Min(DetailHeight, full.Height));
                    detail = full.Clone(corner, PixelFormat.Format32bppArgb);
                    // One pixel of the capture is one pixel of the panel at 100 % scaling
                    detail.SetResolution(96, 96);
                }

                return (overview, detail);
            }
        }

        private void SetPreview(Bitmap overview, Bitmap detail)
        {
            var oldPreview = _preview;
            var oldDetail = _previewDetail;
            _preview = overview;
            _previewDetail = detail;
            PreviewChanged?.Invoke(this, EventArgs.Empty);
            oldPreview?.Dispose();
            oldDetail?.Dispose();
        }

        #endregion

        #region Presets

        private static IReadOnlyList<EffectPreset> CreatePresets(EffectKind effect)
        {
            var texts = Texts.Editor;
            switch (effect)
            {
                case EffectKind.Border:
                    return new[]
                    {
                        new EffectPreset(texts.EffectsPresetDefault, vm => vm.SetBorder(2, Color.Black)),
                        new EffectPreset(texts.EffectsPresetThin, vm => vm.SetBorder(1, Color.DimGray)),
                        new EffectPreset(texts.EffectsPresetWide, vm => vm.SetBorder(6, Color.Black)),
                        new EffectPreset(texts.EffectsPresetWhiteFrame, vm => vm.SetBorder(16, Color.White))
                    };
                case EffectKind.DropShadow:
                    return new[]
                    {
                        new EffectPreset(texts.EffectsPresetDefault, vm => vm.SetShadow(vm._dropShadow, 0.6f, 7, -1, -1)),
                        new EffectPreset(texts.EffectsPresetSoft, vm => vm.SetShadow(vm._dropShadow, 0.3f, 12, 0, 0)),
                        new EffectPreset(texts.EffectsPresetStrong, vm => vm.SetShadow(vm._dropShadow, 0.9f, 5, 3, 3)),
                        new EffectPreset(texts.EffectsPresetFloating, vm => vm.SetShadow(vm._dropShadow, 0.5f, 18, 6, 8))
                    };
                case EffectKind.TornEdges:
                    return new[]
                    {
                        new EffectPreset(texts.EffectsPresetAllSides, vm => vm.SetTornEdges(true, true, true, true)),
                        new EffectPreset(texts.EffectsPresetBottom, vm => vm.SetTornEdges(false, false, true, false)),
                        new EffectPreset(texts.EffectsPresetBottomRight, vm => vm.SetTornEdges(false, true, true, false)),
                        new EffectPreset(texts.EffectsPresetTopBottom, vm => vm.SetTornEdges(true, false, true, false))
                    };
                case EffectKind.RemoveTransparency:
                    return new[]
                    {
                        new EffectPreset(texts.EffectsPresetWhite, vm => vm._transparencyColor = Color.White),
                        new EffectPreset(texts.EffectsPresetBlack, vm => vm._transparencyColor = Color.Black)
                    };
                case EffectKind.Resize:
                    return new[] { 25, 50, 75, 150, 200 }
                        .Select(percent => new EffectPreset(string.Format(CultureInfo.CurrentCulture, "{0} %", percent), vm => vm.ResizeToPercent(percent)))
                        .ToArray();
                default:
                    return Array.Empty<EffectPreset>();
            }
        }

        private void SetBorder(int width, Color color)
        {
            _borderWidth = width;
            _borderColor = color;
            OnPropertyChanged(nameof(BorderWidth));
            OnPropertyChanged(nameof(BorderColor));
        }

        private void SetShadow(DropShadowEffect shadow, float darkness, int size, int offsetX, int offsetY)
        {
            shadow.Darkness = darkness;
            shadow.ShadowSize = size;
            shadow.ShadowOffset = new NativePoint(offsetX, offsetY);
        }

        /// <summary>
        /// The sides to tear, the other settings stay: a preset for the sides is combined with the teeth and shadow chosen
        /// </summary>
        private void SetTornEdges(bool top, bool right, bool bottom, bool left)
        {
            _tornEdges.Edges = new[] { top, right, bottom, left };
        }

        #endregion

        #region Helpers

        public static string GetTitle(EffectKind effect) => effect switch
        {
            EffectKind.Border => Texts.Editor.Border,
            EffectKind.DropShadow => Texts.Editor.ImageShadow,
            EffectKind.TornEdges => Texts.Editor.TornEdge,
            EffectKind.Grayscale => Texts.Editor.Grayscale,
            EffectKind.Invert => Texts.Editor.Invert,
            EffectKind.RemoveTransparency => Texts.Editor.RemoveTransparency,
            EffectKind.Resize => Texts.Editor.EffectsResize,
            _ => effect.ToString()
        };

        internal static DropShadowEffect Copy(DropShadowEffect effect) => new()
        {
            Darkness = effect.Darkness,
            ShadowSize = effect.ShadowSize,
            ShadowOffset = effect.ShadowOffset
        };

        internal static TornEdgeEffect Copy(TornEdgeEffect effect) => new()
        {
            Edges = effect.Edges is { Length: 4 } edges ? (bool[])edges.Clone() : new[] { true, true, true, true },
            ToothHeight = effect.ToothHeight,
            HorizontalToothRange = effect.HorizontalToothRange,
            VerticalToothRange = effect.VerticalToothRange,
            Darkness = effect.Darkness,
            ShadowSize = effect.ShadowSize,
            ShadowOffset = effect.ShadowOffset,
            GenerateShadow = effect.GenerateShadow,
            BackgroundColor = effect.BackgroundColor,
            Seed = effect.Seed
        };

        private void SetSetting<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged(propertyName);
            OnSettingChanged();
        }

        private void OnSettingChanged()
        {
            OnStateChanged();
            RequestPreview();
        }

        private void AttachSurface(Surface surface)
        {
            if (_surface != null)
            {
                _surface.ImageChanged -= OnImageChanged;
            }

            _surface = surface;
            if (_surface != null)
            {
                _surface.ImageChanged += OnImageChanged;
            }
        }

        private void OnCurrentSurfaceChanged(object sender, EventArgs e)
        {
            AttachSurface(_context.CurrentSurface as Surface);
            OnSurfaceContentChanged();
        }

        private void OnImageChanged(object sender, EventArgs e) => OnSurfaceContentChanged();

        private void OnElementsChanged(object sender, EventArgs e) => OnSurfaceContentChanged();

        private void OnSurfaceContentChanged()
        {
            if (_effect == EffectKind.Resize)
            {
                ResetResize();
            }

            OnTornEdgesChanged();
            OnStateChanged();
            RequestPreview();
        }

        public void Dispose()
        {
            _disposed = true;
            _context.CurrentSurfaceChanged -= OnCurrentSurfaceChanged;
            _context.ElementsChanged -= OnElementsChanged;
            AttachSurface(null);
            _isShown = false;
            SetPreview(null, null);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }

    /// <summary>
    /// An effect in the list of the panel
    /// </summary>
    public sealed class EffectEntry
    {
        public EffectEntry(EffectKind kind, string title)
        {
            Kind = kind;
            Title = title;
        }

        public EffectKind Kind { get; }

        public string Title { get; }

        public override string ToString() => Title;
    }
}
