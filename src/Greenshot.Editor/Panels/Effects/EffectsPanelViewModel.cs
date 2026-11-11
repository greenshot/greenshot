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
    /// and Apply, which applies it as one undo step. No UI toolkit here, the preview is a System.Drawing bitmap like the capture,
    /// in its full size: the view zooms and pans it.
    /// </summary>
    public sealed class EffectsPanelViewModel : INotifyPropertyChanged, IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EffectsPanelViewModel));

        /// <summary>
        /// The largest width or height of a resized capture (GDI+ fails a bit above 25000)
        /// </summary>
        public const int MaximumDimension = 30_000;

        /// <summary>
        /// A resize to more pixels isn't previewed, the preview would need too much memory (it is in the full size)
        /// </summary>
        private const long MaximumResizePreviewPixels = 40_000_000;

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

        private int _borderWidth = _lastBorderWidth;
        private Color _borderColor = _lastBorderColor;
        private readonly DropShadowEffect _dropShadow;
        private Color _transparencyColor = _lastTransparencyColor;
        private string _resizeWidth = string.Empty;
        private string _resizeHeight = string.Empty;
        private bool _keepAspectRatio = true;
        private int _resizedWidth;
        private int _resizedHeight;
        private bool _updatingSize;

        /// <param name="context">IEditorPanelContext of the editor window</param>
        /// <param name="configuration">IEditorConfiguration with the last settings of the drop shadow</param>
        /// <param name="applied">called after an effect was applied, e.g. to update the undo button of the window</param>
        public EffectsPanelViewModel(IEditorPanelContext context, IEditorConfiguration configuration, Action applied = null)
        {
            _context = context;
            _configuration = configuration;
            _applied = applied;
            _dropShadow = Copy(configuration?.DropShadowEffectSettings ?? new DropShadowEffect());
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
                if (value == EffectKind.Resize)
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
            OnPropertyChanged(nameof(ShowsTransparency));
            OnPropertyChanged(nameof(ShowsResize));
            OnPropertyChanged(nameof(BorderWidth));
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(TransparencyColor));
            OnShadowChanged();
            OnStateChanged();
        }

        public bool ShowsBorder => _effect == EffectKind.Border;

        public bool ShowsShadow => _effect == EffectKind.DropShadow;

        public bool ShowsTransparency => _effect == EffectKind.RemoveTransparency;

        public bool ShowsResize => _effect == EffectKind.Resize;

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

        #region Drop shadow

        private DropShadowEffect CurrentShadow => _dropShadow;

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
                    OnSettingChanged();
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
            OnSettingChanged();
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
            OnSettingChanged();
        }

        #endregion

        #region State, apply

        public bool CanApply => _surface != null && !_applying && _effect switch
        {
            EffectKind.RemoveTransparency => HasTransparency,
            EffectKind.Resize => IsResizeValid && (_resizedWidth != ImageSize.Width || _resizedHeight != ImageSize.Height),
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
            _ => string.Empty
        };

        public bool StatusIsError => _effect == EffectKind.Resize && !IsResizeValid || _effect == EffectKind.RemoveTransparency && _surface != null && !HasTransparency;

        private void OnStateChanged()
        {
            OnPropertyChanged(nameof(CanApply));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusIsError));
        }

        /// <summary>
        /// The effect as it is applied, a new instance
        /// </summary>
        public IEffect CreateEffect() => _effect switch
        {
            EffectKind.Border => new BorderEffect { Width = _borderWidth, Color = _borderColor },
            EffectKind.DropShadow => Copy(_dropShadow),
            EffectKind.RemoveTransparency => new RemoveTransparencyEffect { Color = _transparencyColor },
            EffectKind.Resize => new ResizeEffect(_resizedWidth, _resizedHeight, false),
            _ => null
        };

        /// <summary>
        /// Apply the effect to the shown capture as one undo step.
        /// The settings are kept for the next time, the drop shadow in greenshot.ini.
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
                await surface.ApplyBitmapEffectAsync(CreateEffect());
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
            }
        }

        #endregion

        #region Preview

        /// <summary>
        /// The whole capture with the effect in its full size, the view zooms and pans it; null while there is none
        /// </summary>
        public Bitmap Preview => _preview;

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
                    Bitmap preview = null;
                    try
                    {
                        preview = await CreatePreviewAsync(surface);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Couldn't make the preview of the effect", ex);
                    }

                    if (_disposed || _previewDirty || !ReferenceEquals(surface, _surface))
                    {
                        // Already old, the next one follows
                        preview?.Dispose();
                        continue;
                    }

                    SetPreview(preview);
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
                case EffectKind.Resize when !IsResizeValid || (long)_resizedWidth * _resizedHeight > MaximumResizePreviewPixels:
                    // The capture as it is, the new size is in the status text
                    return surface.CreateEffectPreviewAsync(new ResizeEffect(ImageSize.Width, ImageSize.Height, false), false);
                case EffectKind.RemoveTransparency:
                    return surface.CreateEffectPreviewAsync(CreateEffect(), true);
                default:
                    return surface.CreateEffectPreviewAsync(CreateEffect(), false);
            }
        }

        private void SetPreview(Bitmap preview)
        {
            var oldPreview = _preview;
            _preview = preview;
            PreviewChanged?.Invoke(this, EventArgs.Empty);
            oldPreview?.Dispose();
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

        #endregion

        #region Helpers

        public static string GetTitle(EffectKind effect) => effect switch
        {
            EffectKind.Border => Texts.Editor.Border,
            EffectKind.DropShadow => Texts.Editor.ImageShadow,
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
            SetPreview(null);
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
