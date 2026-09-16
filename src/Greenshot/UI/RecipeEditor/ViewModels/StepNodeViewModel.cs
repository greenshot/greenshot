using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Greenshot.Base.Drawing;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Base.Recipes;
using Greenshot.UI.RecipeEditor.Dialogs;
using Greenshot.UI.RecipeEditor.Helpers;
using Newtonsoft.Json.Linq;

namespace Greenshot.UI.RecipeEditor.ViewModels
{
    public class StepPortViewModel : ViewModelBase
    {
        private Point _anchor;
        private bool _isConnected;
        private string _title;

        public StepNodeViewModel Node { get; }
        public bool IsInput { get; }
        public bool IsConditional => Node?.IsConditional ?? false;
        public bool HasDynamicOutputPorts => Node?.HasDynamicOutputPorts ?? false;

        public Point Anchor
        {
            get => _anchor;
            set => SetField(ref _anchor, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetField(ref _isConnected, value);
        }

        public string Title
        {
            get => _title ?? (IsInput ? "In" : "Out");
            set => SetField(ref _title, value);
        }

        public StepPortViewModel(StepNodeViewModel node, bool isInput, string title = null)
        {
            Node = node;
            IsInput = isInput;
            _title = title;
        }
    }

    public class ConditionBranchViewModel : ViewModelBase
    {
        private string _key;
        private string _expression;
        private readonly Action _onChanged;
        private readonly Action<ConditionBranchViewModel> _onRemove;

        public StepPortViewModel Port { get; }

        public string Key
        {
            get => _key;
            set
            {
                if (SetField(ref _key, value))
                {
                    if (Port != null) Port.Title = value;
                    _onChanged?.Invoke();
                }
            }
        }

        public string Expression
        {
            get => _expression;
            set
            {
                if (SetField(ref _expression, value))
                {
                    _onChanged?.Invoke();
                }
            }
        }

        public ICommand RemoveCommand { get; }

        public ConditionBranchViewModel(string key, string expression, StepNodeViewModel node, Action onChanged = null, Action<ConditionBranchViewModel> onRemove = null)
        {
            _key = key ?? "A";
            _expression = expression ?? "${true}";
            _onChanged = onChanged;
            _onRemove = onRemove;
            Port = new StepPortViewModel(node, isInput: false, title: _key);
            RemoveCommand = new RelayCommand(() => _onRemove?.Invoke(this));
        }
    }

    public class PromptChoiceViewModel : ViewModelBase
    {
        private string _key;
        private string _label;
        private string _style = "Primary";
        private bool _isDefault;
        private bool _isCancel;
        private readonly Action _onChanged;
        private readonly Action<PromptChoiceViewModel> _onRemove;

        public StepPortViewModel Port { get; }

        public string Key
        {
            get => _key;
            set
            {
                if (SetField(ref _key, value))
                {
                    if (Port != null) Port.Title = value;
                    _onChanged?.Invoke();
                }
            }
        }

        public string Label
        {
            get => _label;
            set
            {
                if (SetField(ref _label, value))
                {
                    _onChanged?.Invoke();
                }
            }
        }

        public string Style
        {
            get => _style;
            set
            {
                if (SetField(ref _style, value))
                {
                    _onChanged?.Invoke();
                }
            }
        }

        public bool IsDefault
        {
            get => _isDefault;
            set
            {
                if (SetField(ref _isDefault, value))
                {
                    _onChanged?.Invoke();
                }
            }
        }

        public bool IsCancel
        {
            get => _isCancel;
            set
            {
                if (SetField(ref _isCancel, value))
                {
                    _onChanged?.Invoke();
                }
            }
        }

        public ICommand RemoveCommand { get; }

        public PromptChoiceViewModel(string key, string label, string style, bool isDefault, bool isCancel, StepNodeViewModel node, Action onChanged = null, Action<PromptChoiceViewModel> onRemove = null)
        {
            _key = key ?? "Yes";
            _label = label ?? key;
            _style = style ?? "Primary";
            _isDefault = isDefault;
            _isCancel = isCancel;
            _onChanged = onChanged;
            _onRemove = onRemove;
            Port = new StepPortViewModel(node, isInput: false, title: _key);
            RemoveCommand = new RelayCommand(() => _onRemove?.Invoke(this));
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["Key"] = Key,
                ["Label"] = Label,
                ["Style"] = Style,
                ["IsDefault"] = IsDefault,
                ["IsCancel"] = IsCancel
            };
        }
    }

    public class VariableItemViewModel : ViewModelBase
    {
        private string _key;
        private string _value;
        private readonly Action _onChanged;

        public string Key
        {
            get => _key;
            set { if (SetField(ref _key, value)) _onChanged?.Invoke(); }
        }

        public string Value
        {
            get => _value;
            set { if (SetField(ref _value, value)) _onChanged?.Invoke(); }
        }

        public ICommand RemoveCommand { get; }

        public VariableItemViewModel(string key, string value, Action onChanged = null, Action<VariableItemViewModel> onRemove = null)
        {
            _key = key;
            _value = value;
            _onChanged = onChanged;
            RemoveCommand = new RelayCommand(() => onRemove?.Invoke(this));
        }
    }

    public class DrawableItemViewModel : ViewModelBase
    {
        private string _type = "Rectangle";
        private string _text = "";
        private string _emoji = "🛡️";
        private string _horizontalAnchor = "None";
        private string _verticalAnchor = "None";
        private string _width = "200";
        private string _height = "40";
        private string _offsetX = "0";
        private string _offsetY = "0";
        private string _fillColor = "transparent";
        private string _lineColor = "#FF0000";
        private int _lineThickness = 2;
        private bool _shadow = true;
        private string _fontFamily = "Segoe UI";
        private double _fontSize = 12.0;
        private bool _fontBold = false;
        private bool _fontItalic = false;
        private string _textAlignment = "Center";
        private string _arrowHeads = "END_POINT";
        private int _stepNumber = 1;
        private int _blurRadius = 10;
        private int _pixelSize = 5;
        private int _magnificationFactor = 2;
        private string _foreColor = "#000000";
        private string _backColor = "#FFFFFF";
        private bool _roundedDots = false;
        private int _margin = 1;
        private string _tailDirection = "BottomLeft";
        private string _tailOffsetX = "0";
        private string _tailOffsetY = "0";

        // Image, SVG & Cursor fields
        private string _filePath = "";
        private string _svgXml = "";
        private string _imageData = "";
        private string _assetStorageMode = "Embed";
        private string _cursorName = "Arrow";

        // Aspect ratio tracking & parameters store
        private double _aspectRatio = 1.0;
        private bool? _lockAspectRatio;
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private readonly Action _onChanged;

        public static IReadOnlyList<string> AvailableFontFamilies { get; } = GetAvailableFonts();
        public static IReadOnlyList<string> AvailableTextAlignments { get; } = new[] { "Left", "Center", "Right" };
        public static IReadOnlyList<string> AvailableArrowHeads { get; } = new[] { "END_POINT", "START_POINT", "BOTH", "NONE" };
        public static IReadOnlyList<string> AvailableCursors { get; } = new[] { "AppStarting", "Arrow", "Cross", "Hand", "Help", "IBeam", "No", "SizeAll", "SizeNESW", "SizeNS", "SizeNWSE", "SizeWE", "UpArrow", "Wait" };

        private static List<string> GetAvailableFonts()
        {
            try
            {
                var fonts = System.Windows.Media.Fonts.SystemFontFamilies
                    .Select(f => f.Source)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .OrderBy(s => s)
                    .ToList();
                if (fonts.Count > 0) return fonts;
            }
            catch { }
            return new List<string> { "Arial", "Calibri", "Comic Sans MS", "Consolas", "Courier New", "Georgia", "Impact", "Lucida Console", "Segoe UI", "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana" };
        }

        public string Type
        {
            get => _type;
            set
            {
                if (SetField(ref _type, value))
                {
                    _lockAspectRatio = null;
                    bool isRational = (RecipeDrawableRegistry.Instance.GetScaleOptions(value) & ScaleOptions.Rational) == ScaleOptions.Rational;
                    if (isRational)
                    {
                        _aspectRatio = 1.0;
                        if (_width != _height || _height == "40")
                        {
                            if (string.Equals(value, "StepLabel", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "Counter", StringComparison.OrdinalIgnoreCase))
                            {
                                _width = "28";
                                _height = "28";
                            }
                            else if (string.Equals(value, "Emoji", StringComparison.OrdinalIgnoreCase))
                            {
                                _width = "32";
                                _height = "32";
                            }
                            else
                            {
                                _width = "150";
                                _height = "150";
                            }
                            OnPropertyChanged(nameof(Width));
                            OnPropertyChanged(nameof(Height));
                        }
                    }
                    else if ((IsImageType || IsSvgType) && (_width == "200" && _height == "40"))
                    {
                        _width = "120";
                        _height = "120";
                        _aspectRatio = 1.0;
                        OnPropertyChanged(nameof(Width));
                        OnPropertyChanged(nameof(Height));
                    }

                    _onChanged?.Invoke();
                    OnPropertyChanged(nameof(IsTextType));
                    OnPropertyChanged(nameof(IsEmojiType));
                    OnPropertyChanged(nameof(IsSpeechbubbleType));
                    OnPropertyChanged(nameof(IsQrCodeType));
                    OnPropertyChanged(nameof(IsImageType));
                    OnPropertyChanged(nameof(IsSvgType));
                    OnPropertyChanged(nameof(IsCursorType));
                    OnPropertyChanged(nameof(IsStepLabelType));
                    OnPropertyChanged(nameof(IsArrowType));
                    OnPropertyChanged(nameof(IsBlurType));
                    OnPropertyChanged(nameof(IsPixelizeType));
                    OnPropertyChanged(nameof(IsObfuscateType));
                    OnPropertyChanged(nameof(IsHighlightType));
                    OnPropertyChanged(nameof(IsMagnifyType));
                    OnPropertyChanged(nameof(HasFontSettings));
                    OnPropertyChanged(nameof(HasLineThickness));
                    OnPropertyChanged(nameof(HasShadow));
                    OnPropertyChanged(nameof(HasFillColor));
                    OnPropertyChanged(nameof(HasLineColor));
                    OnPropertyChanged(nameof(HasAnyStandardColor));
                    OnPropertyChanged(nameof(FillColorLabel));
                    OnPropertyChanged(nameof(LineColorLabel));
                    OnPropertyChanged(nameof(LockAspectRatio));
                    OnPropertyChanged(nameof(CanConfigure));
                    OnPropertyChanged(nameof(ConfigurationSummary));
                }
            }
        }

        private bool IsKnownNonColorType => IsQrCodeType || IsObfuscateType || IsMagnifyType || IsEmojiType || IsImageType || IsSvgType || IsCursorType;

        public bool IsTextType => string.Equals(_type, "Text", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(_type, "Speechbubble", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(_type, "Watermark", StringComparison.OrdinalIgnoreCase);

        public bool IsQrCodeType => string.Equals(_type, "QRCode", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Barcode", StringComparison.OrdinalIgnoreCase);

        public bool IsEmojiType => string.Equals(_type, "Emoji", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(_type, "Icon", StringComparison.OrdinalIgnoreCase);

        public bool IsSpeechbubbleType => string.Equals(_type, "Speechbubble", StringComparison.OrdinalIgnoreCase);

        public bool IsImageType => string.Equals(_type, "Image", StringComparison.OrdinalIgnoreCase);

        public bool IsSvgType => string.Equals(_type, "Svg", StringComparison.OrdinalIgnoreCase);

        public bool IsCursorType => string.Equals(_type, "Cursor", StringComparison.OrdinalIgnoreCase);

        public bool IsStepLabelType => string.Equals(_type, "StepLabel", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(_type, "Counter", StringComparison.OrdinalIgnoreCase);

        public bool IsArrowType => string.Equals(_type, "Arrow", StringComparison.OrdinalIgnoreCase);

        public bool IsBlurType => string.Equals(_type, "Blur", StringComparison.OrdinalIgnoreCase);

        public bool IsPixelizeType => string.Equals(_type, "Pixelize", StringComparison.OrdinalIgnoreCase);

        public bool IsObfuscateType => IsBlurType || IsPixelizeType;

        public bool IsHighlightType => string.Equals(_type, "Highlight", StringComparison.OrdinalIgnoreCase);

        public bool IsMagnifyType => string.Equals(_type, "Magnify", StringComparison.OrdinalIgnoreCase);

        public bool HasFontSettings => string.Equals(_type, "Text", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(_type, "Speechbubble", StringComparison.OrdinalIgnoreCase);

        public bool HasLineThickness => string.Equals(_type, "Rectangle", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(_type, "Ellipse", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(_type, "Line", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(_type, "Arrow", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(_type, "Freehand", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(_type, "Text", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(_type, "Speechbubble", StringComparison.OrdinalIgnoreCase);

        public bool HasShadow => string.Equals(_type, "Rectangle", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(_type, "Ellipse", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(_type, "Line", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(_type, "Arrow", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(_type, "Freehand", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(_type, "Text", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(_type, "Speechbubble", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(_type, "StepLabel", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(_type, "Counter", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(_type, "Image", StringComparison.OrdinalIgnoreCase);

        public bool HasFillColor => string.Equals(_type, "Rectangle", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Ellipse", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Text", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Speechbubble", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "StepLabel", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Counter", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Highlight", StringComparison.OrdinalIgnoreCase) ||
                                    (!IsKnownNonColorType && !string.Equals(_type, "Line", StringComparison.OrdinalIgnoreCase) && !string.Equals(_type, "Arrow", StringComparison.OrdinalIgnoreCase) && !string.Equals(_type, "Freehand", StringComparison.OrdinalIgnoreCase));

        public bool HasLineColor => string.Equals(_type, "Rectangle", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Ellipse", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Line", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Arrow", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Freehand", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Text", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Speechbubble", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "StepLabel", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Counter", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(_type, "Highlight", StringComparison.OrdinalIgnoreCase) ||
                                    !IsKnownNonColorType;

        public bool HasAnyStandardColor => !IsQrCodeType && (HasFillColor || HasLineColor);

        public string FillColorLabel => IsStepLabelType ? "Circle Color" :
                                        IsHighlightType ? "Highlight Color" :
                                        IsTextType ? "Background Color" : "Fill Color";

        public string LineColorLabel => IsStepLabelType ? "Number Color" :
                                        IsTextType ? "Text Color" : "Line Color";

        public bool CanConfigure => RecipeDrawableRegistry.Instance.CanConfigureDrawable(_type);

        public string ConfigurationSummary
        {
            get
            {
                if (string.Equals(_type, "QRCode", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(_type, "Barcode", StringComparison.OrdinalIgnoreCase))
                {
                    string cat = _parameters.TryGetValue("QrType", out var q) && q != null ? q.ToString() : "Link / Text";
                    string txt = _parameters.TryGetValue("Text", out var t) && t != null ? t.ToString() : _text;
                    string dots = _parameters.TryGetValue("RoundedDots", out var rd) && rd is bool b && b ? " • Rounded Dots" : "";
                    if (!string.IsNullOrWhiteSpace(txt))
                    {
                        if (txt.Length > 35) txt = txt.Substring(0, 32) + "...";
                        return $"{cat}: {txt}{dots}";
                    }
                    return $"{cat}{dots}";
                }

                if (_parameters.Count > 0)
                {
                    return $"{_parameters.Count} custom parameter(s) configured";
                }

                return "Default configuration";
            }
        }

        public IDictionary<string, object> Parameters => _parameters;

        public bool LockAspectRatio
        {
            get => _lockAspectRatio ?? ((RecipeDrawableRegistry.Instance.GetScaleOptions(_type) & ScaleOptions.Rational) == ScaleOptions.Rational);
            set
            {
                if (SetField(ref _lockAspectRatio, value))
                {
                    if (value)
                    {
                        if (double.TryParse(_width, out double w) && double.TryParse(_height, out double h) && w > 0 && h > 0)
                        {
                            _aspectRatio = w / h;
                        }
                        else
                        {
                            _aspectRatio = 1.0;
                        }
                    }
                    _onChanged?.Invoke();
                }
            }
        }

        public string Width
        {
            get => _width;
            set
            {
                if (SetField(ref _width, value))
                {
                    if (LockAspectRatio && double.TryParse(value, out double w) && w > 0)
                    {
                        if (_aspectRatio <= 0.0001) _aspectRatio = 1.0;
                        int newH = Math.Max(1, (int)Math.Round(w / _aspectRatio));
                        if (_height != newH.ToString())
                        {
                            SetField(ref _height, newH.ToString(), nameof(Height));
                        }
                    }
                    _onChanged?.Invoke();
                }
            }
        }

        public string Height
        {
            get => _height;
            set
            {
                if (SetField(ref _height, value))
                {
                    if (LockAspectRatio && double.TryParse(value, out double h) && h > 0)
                    {
                        if (_aspectRatio <= 0.0001) _aspectRatio = 1.0;
                        int newW = Math.Max(1, (int)Math.Round(h * _aspectRatio));
                        if (_width != newW.ToString())
                        {
                            SetField(ref _width, newW.ToString(), nameof(Width));
                        }
                    }
                    _onChanged?.Invoke();
                }
            }
        }

        public string TailDirection
        {
            get => _tailDirection;
            set { if (SetField(ref _tailDirection, value)) _onChanged?.Invoke(); }
        }

        public string TailOffsetX
        {
            get => _tailOffsetX;
            set { if (SetField(ref _tailOffsetX, value)) _onChanged?.Invoke(); }
        }

        public string TailOffsetY
        {
            get => _tailOffsetY;
            set { if (SetField(ref _tailOffsetY, value)) _onChanged?.Invoke(); }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (SetField(ref _text, value))
                {
                    _parameters["Text"] = value;
                    if (IsStepLabelType && int.TryParse(value, out int parsedNum))
                    {
                        _stepNumber = parsedNum;
                        OnPropertyChanged(nameof(StepNumber));
                    }
                    _onChanged?.Invoke();
                    OnPropertyChanged(nameof(ConfigurationSummary));
                }
            }
        }

        public string Emoji
        {
            get => _emoji;
            set { if (SetField(ref _emoji, value)) _onChanged?.Invoke(); }
        }

        public string HorizontalAnchor
        {
            get => _horizontalAnchor;
            set { if (SetField(ref _horizontalAnchor, value)) _onChanged?.Invoke(); }
        }

        public string VerticalAnchor
        {
            get => _verticalAnchor;
            set { if (SetField(ref _verticalAnchor, value)) _onChanged?.Invoke(); }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (SetField(ref _filePath, value))
                {
                    OnPropertyChanged(nameof(HasLinkedFile));
                    OnPropertyChanged(nameof(SourceSummaryBadge));
                    _onChanged?.Invoke();
                }
            }
        }

        public string SvgXml
        {
            get => _svgXml;
            set
            {
                if (SetField(ref _svgXml, value))
                {
                    OnPropertyChanged(nameof(HasEmbeddedData));
                    OnPropertyChanged(nameof(SourceSummaryBadge));
                    _onChanged?.Invoke();
                }
            }
        }

        public string ImageData
        {
            get => _imageData;
            set
            {
                if (SetField(ref _imageData, value))
                {
                    OnPropertyChanged(nameof(HasEmbeddedData));
                    OnPropertyChanged(nameof(SourceSummaryBadge));
                    _onChanged?.Invoke();
                }
            }
        }

        public bool HasEmbeddedData => !string.IsNullOrEmpty(_imageData) || (!string.IsNullOrEmpty(_svgXml) && IsSvgType);
        public bool HasLinkedFile => !string.IsNullOrEmpty(_filePath);

        public string AssetStorageMode
        {
            get => _assetStorageMode;
            set
            {
                if (SetField(ref _assetStorageMode, value))
                {
                    OnPropertyChanged(nameof(IsEmbedMode));
                    OnPropertyChanged(nameof(IsLinkMode));
                    OnPropertyChanged(nameof(IsCursorPresetMode));
                    OnPropertyChanged(nameof(IsCursorEmbedMode));
                    OnPropertyChanged(nameof(IsCursorLinkMode));
                    OnPropertyChanged(nameof(SourceSummaryBadge));
                    _onChanged?.Invoke();
                }
            }
        }

        public bool IsEmbedMode
        {
            get => string.Equals(_assetStorageMode, "Embed", StringComparison.OrdinalIgnoreCase) || (HasEmbeddedData && !HasLinkedFile);
            set { if (value) AssetStorageMode = "Embed"; }
        }

        public bool IsLinkMode
        {
            get => string.Equals(_assetStorageMode, "Link", StringComparison.OrdinalIgnoreCase) || (HasLinkedFile && !HasEmbeddedData);
            set { if (value) AssetStorageMode = "Link"; }
        }

        public bool IsCursorPresetMode
        {
            get => IsCursorType && !HasEmbeddedData && !HasLinkedFile;
            set
            {
                if (value && IsCursorType)
                {
                    _imageData = null;
                    _filePath = null;
                    _assetStorageMode = "Preset";
                    if (string.IsNullOrEmpty(_cursorName)) _cursorName = "Arrow";
                    OnPropertyChanged(nameof(ImageData));
                    OnPropertyChanged(nameof(FilePath));
                    OnPropertyChanged(nameof(CursorName));
                    OnPropertyChanged(nameof(HasEmbeddedData));
                    OnPropertyChanged(nameof(HasLinkedFile));
                    OnPropertyChanged(nameof(IsCursorPresetMode));
                    OnPropertyChanged(nameof(IsCursorEmbedMode));
                    OnPropertyChanged(nameof(IsCursorLinkMode));
                    OnPropertyChanged(nameof(SourceSummaryBadge));
                    _onChanged?.Invoke();
                }
            }
        }

        public bool IsCursorEmbedMode
        {
            get => IsCursorType && HasEmbeddedData;
            set
            {
                if (value && IsCursorType)
                {
                    _filePath = null;
                    _assetStorageMode = "Embed";
                    OnPropertyChanged(nameof(FilePath));
                    OnPropertyChanged(nameof(HasEmbeddedData));
                    OnPropertyChanged(nameof(HasLinkedFile));
                    OnPropertyChanged(nameof(IsCursorPresetMode));
                    OnPropertyChanged(nameof(IsCursorEmbedMode));
                    OnPropertyChanged(nameof(IsCursorLinkMode));
                    OnPropertyChanged(nameof(SourceSummaryBadge));
                    _onChanged?.Invoke();
                }
            }
        }

        public bool IsCursorLinkMode
        {
            get => IsCursorType && HasLinkedFile;
            set
            {
                if (value && IsCursorType)
                {
                    _imageData = null;
                    _assetStorageMode = "Link";
                    OnPropertyChanged(nameof(ImageData));
                    OnPropertyChanged(nameof(HasEmbeddedData));
                    OnPropertyChanged(nameof(HasLinkedFile));
                    OnPropertyChanged(nameof(IsCursorPresetMode));
                    OnPropertyChanged(nameof(IsCursorEmbedMode));
                    OnPropertyChanged(nameof(IsCursorLinkMode));
                    OnPropertyChanged(nameof(SourceSummaryBadge));
                    _onChanged?.Invoke();
                }
            }
        }

        public string SourceSummaryBadge
        {
            get
            {
                if (IsCursorType)
                {
                    if (HasEmbeddedData) return "📦 Embedded Cursor (Bitmap)";
                    if (HasLinkedFile) return $"🔗 {Path.GetFileName(_filePath)}";
                    return $"Preset: {_cursorName ?? "Arrow"}";
                }
                if (IsImageType)
                {
                    if (HasEmbeddedData) return "📦 Embedded Image";
                    if (HasLinkedFile) return $"🔗 {Path.GetFileName(_filePath)}";
                    return "";
                }
                if (IsSvgType)
                {
                    if (HasEmbeddedData) return "📦 Embedded SVG";
                    if (HasLinkedFile) return $"🔗 {Path.GetFileName(_filePath)}";
                    return "";
                }
                return "";
            }
        }

        public string CursorName
        {
            get => _cursorName;
            set
            {
                if (SetField(ref _cursorName, value))
                {
                    _parameters["CursorName"] = value;
                    OnPropertyChanged(nameof(SourceSummaryBadge));
                    _onChanged?.Invoke();
                }
            }
        }

        public string OffsetX
        {
            get => _offsetX;
            set { if (SetField(ref _offsetX, value)) _onChanged?.Invoke(); }
        }

        public string OffsetY
        {
            get => _offsetY;
            set { if (SetField(ref _offsetY, value)) _onChanged?.Invoke(); }
        }

        public string FillColor
        {
            get => _fillColor;
            set { if (SetField(ref _fillColor, value)) _onChanged?.Invoke(); }
        }

        public string LineColor
        {
            get => _lineColor;
            set { if (SetField(ref _lineColor, value)) _onChanged?.Invoke(); }
        }

        public int LineThickness
        {
            get => _lineThickness;
            set { if (SetField(ref _lineThickness, value)) _onChanged?.Invoke(); }
        }

        public bool Shadow
        {
            get => _shadow;
            set { if (SetField(ref _shadow, value)) _onChanged?.Invoke(); }
        }

        public string FontFamily
        {
            get => _fontFamily;
            set { if (SetField(ref _fontFamily, value)) _onChanged?.Invoke(); }
        }

        public double FontSize
        {
            get => _fontSize;
            set { if (SetField(ref _fontSize, value)) _onChanged?.Invoke(); }
        }

        public bool FontBold
        {
            get => _fontBold;
            set { if (SetField(ref _fontBold, value)) _onChanged?.Invoke(); }
        }

        public bool FontItalic
        {
            get => _fontItalic;
            set { if (SetField(ref _fontItalic, value)) _onChanged?.Invoke(); }
        }

        public string TextAlignment
        {
            get => _textAlignment;
            set { if (SetField(ref _textAlignment, value)) _onChanged?.Invoke(); }
        }

        public string ArrowHeads
        {
            get => _arrowHeads;
            set { if (SetField(ref _arrowHeads, value)) _onChanged?.Invoke(); }
        }

        public int StepNumber
        {
            get => _stepNumber;
            set
            {
                if (SetField(ref _stepNumber, value))
                {
                    if (IsStepLabelType)
                    {
                        _text = value.ToString();
                        OnPropertyChanged(nameof(Text));
                    }
                    _onChanged?.Invoke();
                }
            }
        }

        public int BlurRadius
        {
            get => _blurRadius;
            set { if (SetField(ref _blurRadius, value)) _onChanged?.Invoke(); }
        }

        public int PixelSize
        {
            get => _pixelSize;
            set { if (SetField(ref _pixelSize, value)) _onChanged?.Invoke(); }
        }

        public int MagnificationFactor
        {
            get => _magnificationFactor;
            set { if (SetField(ref _magnificationFactor, value)) _onChanged?.Invoke(); }
        }

        public string ForeColor
        {
            get => _foreColor;
            set { if (SetField(ref _foreColor, value)) { _parameters["ForeColor"] = value; _onChanged?.Invoke(); } }
        }

        public string BackColor
        {
            get => _backColor;
            set { if (SetField(ref _backColor, value)) { _parameters["BackColor"] = value; _onChanged?.Invoke(); } }
        }

        public bool RoundedDots
        {
            get => _roundedDots;
            set { if (SetField(ref _roundedDots, value)) { _parameters["RoundedDots"] = value; OnPropertyChanged(nameof(ConfigurationSummary)); _onChanged?.Invoke(); } }
        }

        public int Margin
        {
            get => _margin;
            set { if (SetField(ref _margin, value)) { _parameters["Margin"] = value; _onChanged?.Invoke(); } }
        }

        public ICommand RemoveCommand { get; }
        public ICommand PickFillColorCommand { get; }
        public ICommand PickLineColorCommand { get; }
        public ICommand PickForeColorCommand { get; }
        public ICommand PickBackColorCommand { get; }
        public ICommand ConfigureCommand { get; }
        public ICommand ConfigureQrInDialogCommand => ConfigureCommand;
        public ICommand BrowseFileCommand { get; }

        public DrawableItemViewModel(Action onChanged = null, Action<DrawableItemViewModel> onRemove = null)
        {
            _onChanged = onChanged;
            RemoveCommand = new RelayCommand(() => onRemove?.Invoke(this));
            PickFillColorCommand = new RelayCommand(() =>
            {
                var picked = StepNodeViewModel.PromptColorHelper(FillColor);
                if (picked != null) FillColor = picked;
            });
            PickLineColorCommand = new RelayCommand(() =>
            {
                var picked = StepNodeViewModel.PromptColorHelper(LineColor);
                if (picked != null) LineColor = picked;
            });
            PickForeColorCommand = new RelayCommand(() =>
            {
                var picked = StepNodeViewModel.PromptColorHelper(ForeColor);
                if (picked != null) ForeColor = picked;
            });
            PickBackColorCommand = new RelayCommand(() =>
            {
                var picked = StepNodeViewModel.PromptColorHelper(BackColor);
                if (picked != null) BackColor = picked;
            });
            ConfigureCommand = new RelayCommand(ConfigureDrawableItem);
            BrowseFileCommand = new RelayCommand(BrowseFile);
        }

        private void ConfigureDrawableItem()
        {
            var dict = ToDictionary();
            var window = System.Windows.Application.Current?.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive);
            if (RecipeDrawableRegistry.Instance.ConfigureDrawable(Type, dict, window))
            {
                foreach (var kv in dict)
                {
                    _parameters[kv.Key] = kv.Value;
                }
                if (dict.TryGetValue("Text", out var t) && t != null)
                {
                    _text = t.ToString();
                    OnPropertyChanged(nameof(Text));
                }
                if (dict.TryGetValue("ForeColor", out var fc) && fc != null)
                {
                    _foreColor = fc.ToString();
                    OnPropertyChanged(nameof(ForeColor));
                }
                if (dict.TryGetValue("BackColor", out var bc) && bc != null)
                {
                    _backColor = bc.ToString();
                    OnPropertyChanged(nameof(BackColor));
                }
                if (dict.TryGetValue("RoundedDots", out var rd) && rd != null && bool.TryParse(rd.ToString(), out bool bRd))
                {
                    _roundedDots = bRd;
                    OnPropertyChanged(nameof(RoundedDots));
                }
                if (dict.TryGetValue("Margin", out var mg) && mg != null && int.TryParse(mg.ToString(), out int iMg))
                {
                    _margin = iMg;
                    OnPropertyChanged(nameof(Margin));
                }
                OnPropertyChanged(nameof(ConfigurationSummary));
                _onChanged?.Invoke();
            }
        }

        private void BrowseFile()
        {
            var ofd = new Microsoft.Win32.OpenFileDialog();
            if (IsSvgType)
            {
                ofd.Title = "Select SVG Vector Graphic";
                ofd.Filter = "Scalable Vector Graphics (*.svg)|*.svg|All Files (*.*)|*.*";
            }
            else if (IsCursorType)
            {
                ofd.Title = "Select Cursor or Image File";
                ofd.Filter = "Cursor and Image Files (*.cur;*.ico;*.png)|*.cur;*.ico;*.png|All Files (*.*)|*.*";
            }
            else
            {
                ofd.Title = "Select Image File";
                ofd.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico|All Files (*.*)|*.*";
            }

            if (ofd.ShowDialog() == true)
            {
                FilePath = ofd.FileName;
                try
                {
                    if (IsImageType && System.IO.File.Exists(ofd.FileName))
                    {
                        using var img = System.Drawing.Image.FromFile(ofd.FileName);
                        if (img.Width > 0 && img.Height > 0)
                        {
                            _aspectRatio = (double)img.Width / img.Height;
                        }
                        Width = img.Width.ToString();
                        Height = img.Height.ToString();
                        LockAspectRatio = true;

                        // Default embed mode automatically loads ImageData
                        if (IsEmbedMode)
                        {
                            using var ms = new MemoryStream();
                            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            ImageData = Convert.ToBase64String(ms.ToArray());
                        }
                    }
                    else if (IsSvgType && System.IO.File.Exists(ofd.FileName))
                    {
                        if (IsEmbedMode)
                        {
                            SvgXml = File.ReadAllText(ofd.FileName);
                        }
                    }
                    else if (IsCursorType && System.IO.File.Exists(ofd.FileName))
                    {
                        if (IsCursorEmbedMode || IsEmbedMode)
                        {
                            byte[] bytes = File.ReadAllBytes(ofd.FileName);
                            ImageData = Convert.ToBase64String(bytes);
                        }
                    }
                }
                catch
                {
                    // Ignore dimension read failures
                }
            }
        }

        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>(_parameters, StringComparer.OrdinalIgnoreCase)
            {
                ["Type"] = Type,
                ["Width"] = Width,
                ["Height"] = Height,
                ["OffsetX"] = OffsetX,
                ["OffsetY"] = OffsetY
            };
            if (!string.IsNullOrEmpty(HorizontalAnchor) && HorizontalAnchor != "None") dict["HorizontalAnchor"] = HorizontalAnchor;
            if (!string.IsNullOrEmpty(VerticalAnchor) && VerticalAnchor != "None") dict["VerticalAnchor"] = VerticalAnchor;

            if (HasLineThickness) dict["LineThickness"] = LineThickness;
            if (HasShadow) dict["Shadow"] = Shadow;
            if (HasFillColor) dict["FillColor"] = FillColor;
            if (HasLineColor) dict["LineColor"] = LineColor;

            if (HasFontSettings)
            {
                dict["FontFamily"] = FontFamily;
                dict["FontSize"] = FontSize;
                dict["Bold"] = FontBold;
                dict["Italic"] = FontItalic;
                dict["TextAlign"] = TextAlignment;
            }

            if (IsTextType) dict["Text"] = Text;
            if (IsEmojiType) dict["Emoji"] = Emoji;
            if (IsArrowType) dict["ArrowHeads"] = ArrowHeads;
            if (IsStepLabelType)
            {
                dict["Number"] = StepNumber;
                dict["Text"] = StepNumber.ToString();
            }
            if (IsBlurType) dict["BlurRadius"] = BlurRadius;
            if (IsPixelizeType) dict["PixelSize"] = PixelSize;
            if (IsMagnifyType) dict["MagnificationFactor"] = MagnificationFactor;
            if (IsQrCodeType)
            {
                dict["ForeColor"] = ForeColor;
                dict["BackColor"] = BackColor;
                dict["RoundedDots"] = RoundedDots;
                dict["Margin"] = Margin;
                if (!string.IsNullOrEmpty(Text)) dict["Text"] = Text;
            }
            if (IsImageType)
            {
                if (IsEmbedMode && !string.IsNullOrEmpty(ImageData))
                {
                    dict["ImageData"] = ImageData;
                    dict.Remove("FilePath");
                }
                else if (!string.IsNullOrEmpty(FilePath))
                {
                    dict["FilePath"] = FilePath;
                    dict.Remove("ImageData");
                }
            }
            if (IsSvgType)
            {
                if (IsEmbedMode && !string.IsNullOrEmpty(SvgXml))
                {
                    dict["Content"] = SvgXml;
                    dict.Remove("FilePath");
                }
                else if (!string.IsNullOrEmpty(FilePath))
                {
                    dict["FilePath"] = FilePath;
                }
                else if (!string.IsNullOrEmpty(SvgXml))
                {
                    dict["Content"] = SvgXml;
                }
            }
            if (IsCursorType)
            {
                if (HasEmbeddedData && !string.IsNullOrEmpty(ImageData))
                {
                    dict["ImageData"] = ImageData;
                    dict.Remove("FilePath");
                    dict.Remove("CursorName");
                }
                else if (HasLinkedFile && !string.IsNullOrEmpty(FilePath))
                {
                    dict["FilePath"] = FilePath;
                    dict.Remove("ImageData");
                    dict.Remove("CursorName");
                }
                else
                {
                    dict["CursorName"] = CursorName ?? "Arrow";
                    dict.Remove("ImageData");
                    dict.Remove("FilePath");
                }
            }
            if (IsSpeechbubbleType)
            {
                dict["TailDirection"] = TailDirection;
                if (!string.IsNullOrEmpty(TailOffsetX) && TailOffsetX != "0") dict["TailOffsetX"] = TailOffsetX;
                if (!string.IsNullOrEmpty(TailOffsetY) && TailOffsetY != "0") dict["TailOffsetY"] = TailOffsetY;
            }
            if (_lockAspectRatio.HasValue)
            {
                dict["LockAspectRatio"] = _lockAspectRatio.Value;
            }

            return dict;
        }

        public static DrawableItemViewModel FromDictionary(IDictionary dict, Action onChanged = null, Action<DrawableItemViewModel> onRemove = null)
        {
            var item = new DrawableItemViewModel(onChanged, onRemove);
            if (dict == null) return item;

            foreach (DictionaryEntry de in dict)
            {
                if (de.Key != null) item._parameters[de.Key.ToString()] = de.Value;
            }

            string GetKey(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (item._parameters.TryGetValue(k, out var val) && val != null)
                    {
                        return val.ToString();
                    }
                }
                return null;
            }

            bool? GetBool(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (item._parameters.TryGetValue(k, out var val) && val != null)
                    {
                        if (val is bool b) return b;
                        if (bool.TryParse(val.ToString(), out bool pb)) return pb;
                    }
                }
                return null;
            }

            int? GetInt(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (item._parameters.TryGetValue(k, out var val) && val != null)
                    {
                        if (val is int i) return i;
                        if (val is long l) return (int)l;
                        if (int.TryParse(val.ToString(), out int pi)) return pi;
                    }
                }
                return null;
            }

            double? GetDouble(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (item._parameters.TryGetValue(k, out var val) && val != null)
                    {
                        if (val is double d) return d;
                        if (val is float f) return f;
                        if (val is int i) return i;
                        if (double.TryParse(val.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double pd)) return pd;
                    }
                }
                return null;
            }

            string typeVal = GetKey("Type", "DrawableType", "Shape", "Element", "Kind");
            if (!string.IsNullOrEmpty(typeVal))
            {
                if (string.Equals(typeVal, "StepLabel", StringComparison.OrdinalIgnoreCase)) item._type = "StepLabel";
                else if (string.Equals(typeVal, "Emoji", StringComparison.OrdinalIgnoreCase)) item._type = "Emoji";
                else if (string.Equals(typeVal, "Text", StringComparison.OrdinalIgnoreCase)) item._type = "Text";
                else if (string.Equals(typeVal, "Rectangle", StringComparison.OrdinalIgnoreCase)) item._type = "Rectangle";
                else if (string.Equals(typeVal, "Ellipse", StringComparison.OrdinalIgnoreCase)) item._type = "Ellipse";
                else if (string.Equals(typeVal, "Line", StringComparison.OrdinalIgnoreCase)) item._type = "Line";
                else if (string.Equals(typeVal, "Arrow", StringComparison.OrdinalIgnoreCase)) item._type = "Arrow";
                else if (string.Equals(typeVal, "Speechbubble", StringComparison.OrdinalIgnoreCase)) item._type = "Speechbubble";
                else if (string.Equals(typeVal, "Blur", StringComparison.OrdinalIgnoreCase)) item._type = "Blur";
                else if (string.Equals(typeVal, "Pixelize", StringComparison.OrdinalIgnoreCase)) item._type = "Pixelize";
                else if (string.Equals(typeVal, "Highlight", StringComparison.OrdinalIgnoreCase)) item._type = "Highlight";
                else if (string.Equals(typeVal, "Magnify", StringComparison.OrdinalIgnoreCase)) item._type = "Magnify";
                else if (string.Equals(typeVal, "QRCode", StringComparison.OrdinalIgnoreCase)) item._type = "QRCode";
                else if (string.Equals(typeVal, "Barcode", StringComparison.OrdinalIgnoreCase)) item._type = "Barcode";
                else if (string.Equals(typeVal, "Image", StringComparison.OrdinalIgnoreCase)) item._type = "Image";
                else if (string.Equals(typeVal, "Svg", StringComparison.OrdinalIgnoreCase)) item._type = "Svg";
                else if (string.Equals(typeVal, "Cursor", StringComparison.OrdinalIgnoreCase)) item._type = "Cursor";
                else item._type = typeVal;
            }
            else
            {
                item._type = "Rectangle";
            }

            string txt = GetKey("Text", "Content", "Label", "Number", "Watermark_Text", "WatermarkText", "Payload", "Url", "Value", "Data");
            if (txt != null) item._text = txt;

            string emo = GetKey("Emoji", "Icon", "Glyph");
            if (emo != null) item._emoji = emo;

            string ha = GetKey("HorizontalAnchor", "HAnchor", "AnchorH", "AlignH");
            if (ha != null) item._horizontalAnchor = ha;

            string va = GetKey("VerticalAnchor", "VAnchor", "AnchorV", "AlignV");
            if (va != null) item._verticalAnchor = va;

            string w = GetKey("Width", "W", "Size", "Radius");
            if (w != null) item._width = w;

            string h = GetKey("Height", "H", "Size");
            if (h != null) item._height = h;

            string ox = GetKey("OffsetX", "Left", "X", "Offset_X");
            if (ox != null) item._offsetX = ox;

            string oy = GetKey("OffsetY", "Top", "Y", "Offset_Y");
            if (oy != null) item._offsetY = oy;

            string fc = GetKey("FillColor", "Fill", "Background", "BgColor", "Color");
            if (fc != null) item._fillColor = fc;

            string lc = GetKey("LineColor", "Line", "Stroke", "Border", "BorderColor", "TextColor");
            if (lc != null) item._lineColor = lc;

            int? lt = GetInt("LineThickness", "Thickness", "StrokeThickness", "BorderWidth");
            if (lt.HasValue) item._lineThickness = lt.Value;

            bool? sh = GetBool("Shadow", "DropShadow");
            if (sh.HasValue) item._shadow = sh.Value;
            else item._shadow = item._type switch
            {
                "Rectangle" or "Ellipse" or "Line" or "Arrow" or "Text" => true,
                _ => false
            };

            string ff = GetKey("FontFamily", "Font");
            if (ff != null) item._fontFamily = ff;

            double? fs = GetDouble("FontSize");
            if (fs.HasValue) item._fontSize = fs.Value;
            else if (item.IsSpeechbubbleType) item._fontSize = 14.0;
            else item._fontSize = 12.0;

            bool? fb = GetBool("Bold", "FontBold");
            if (fb.HasValue) item._fontBold = fb.Value;
            else if (item.IsSpeechbubbleType) item._fontBold = true;

            bool? fi = GetBool("Italic", "FontItalic");
            if (fi.HasValue) item._fontItalic = fi.Value;

            string ta = GetKey("TextAlign", "TextHorizontalAlignment", "Alignment", "Align");
            if (ta != null) item._textAlignment = ta;

            string ah = GetKey("ArrowHeads", "Heads");
            if (ah != null) item._arrowHeads = ah;

            int? sn = GetInt("Number", "Counter", "StepNumber");
            if (sn.HasValue) item._stepNumber = sn.Value;
            else if (int.TryParse(item._text, out int parsedNum)) item._stepNumber = parsedNum;

            int? br = GetInt("BlurRadius", "Radius");
            if (br.HasValue) item._blurRadius = br.Value;

            int? ps = GetInt("PixelSize", "PixelRadius", "BlockSize");
            if (ps.HasValue) item._pixelSize = ps.Value;

            int? mf = GetInt("MagnificationFactor", "Factor", "Zoom");
            if (mf.HasValue) item._magnificationFactor = mf.Value;

            string foreC = GetKey("ForeColor", "Foreground");
            if (foreC != null) item._foreColor = foreC;

            string backC = GetKey("BackColor", "Background", "BackgroundColor");
            if (backC != null) item._backColor = backC;

            bool? rd = GetBool("RoundedDots", "Rounded");
            if (rd.HasValue) item._roundedDots = rd.Value;

            int? mg = GetInt("Margin");
            if (mg.HasValue) item._margin = mg.Value;

            string td = GetKey("TailDirection", "TailPosition", "Tail", "TailDir");
            if (td != null) item._tailDirection = td;

            string tox = GetKey("TailOffsetX", "TailOffset_X", "Tail_OffsetX");
            if (tox != null) item._tailOffsetX = tox;

            string toy = GetKey("TailOffsetY", "TailOffset_Y", "Tail_OffsetY");
            if (toy != null) item._tailOffsetY = toy;

            string fp = GetKey("FilePath", "Path", "File", "ImageFile", "SvgFile");
            if (fp != null) item._filePath = fp;

            string imgData = GetKey("ImageData", "Base64", "CursorData");
            if (imgData != null)
            {
                item._imageData = imgData;
                item._assetStorageMode = "Embed";
            }
            else if (!string.IsNullOrEmpty(fp))
            {
                item._assetStorageMode = "Link";
            }

            string curName = GetKey("CursorName", "Cursor", "CursorType");
            if (curName != null)
            {
                item._cursorName = curName;
            }
            else if (item.IsCursorType && !string.IsNullOrEmpty(imgData))
            {
                item._cursorName = null;
                item._assetStorageMode = "Embed";
            }
            else if (item.IsCursorType && !string.IsNullOrEmpty(fp))
            {
                item._cursorName = null;
                item._assetStorageMode = "Link";
            }
            else if (item.IsCursorType)
            {
                item._cursorName = "Arrow";
                item._assetStorageMode = "Preset";
            }

            string svg = GetKey("SvgXml", "Svg", "Content", "Xml");
            if (svg != null && (item.IsSvgType || svg.Contains("<svg"))) item._svgXml = svg;

            string lockAsp = GetKey("LockAspectRatio", "LockRatio", "MaintainAspectRatio");
            if (lockAsp != null && bool.TryParse(lockAsp, out bool bLock)) item._lockAspectRatio = bLock;

            if (double.TryParse(item._width, out double dw) && double.TryParse(item._height, out double dh) && dw > 0 && dh > 0)
            {
                item._aspectRatio = dw / dh;
            }

            return item;
        }
    }

    public class StepNodeViewModel : ViewModelBase
    {
        private Point _location;
        private Size _size;
        private bool _isSelected;
        private bool _isStartNode;
        private bool _isInCycle;
        private string _name;

        public RecipeNodeConfig Config { get; }

        public Action<StepNodeViewModel, string, string> OnIdChanged { get; set; }
        public Func<string> RecipeNameProvider { get; set; }

        public string Id
        {
            get => Config.Id;
            set
            {
                if (string.IsNullOrWhiteSpace(value) || value == Config.Id) return;
                string oldId = Config.Id;
                Config.Id = value.Trim();
                OnPropertyChanged(nameof(Id));
                OnPropertyChanged(nameof(DisplayName));
                OnIdChanged?.Invoke(this, oldId, Config.Id);
            }
        }

        public void ResetId(string id)
        {
            Config.Id = id;
            OnPropertyChanged(nameof(Id));
            OnPropertyChanged(nameof(DisplayName));
        }

        public string StepType => Config.StepType;

        public string Name
        {
            get => _name;
            set
            {
                if (SetField(ref _name, value))
                {
                    Config.Name = value;
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? StepType : Name;

        public Point Location
        {
            get => _location;
            set => SetField(ref _location, value);
        }

        public Size Size
        {
            get => _size;
            set => SetField(ref _size, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }

        public bool IsStartNode
        {
            get => _isStartNode;
            set
            {
                if (SetField(ref _isStartNode, value))
                {
                    OnToggleStartNode?.Invoke(this);
                }
            }
        }

        public bool IsInCycle
        {
            get => _isInCycle;
            set => SetField(ref _isInCycle, value);
        }

        public bool IsConditional => string.Equals(StepType, WellKnownStepTypes.Conditional, StringComparison.OrdinalIgnoreCase);
        public bool IsUserPrompt => string.Equals(StepType, WellKnownStepTypes.UserPrompt, StringComparison.OrdinalIgnoreCase) || string.Equals(StepType, "PromptChoice", StringComparison.OrdinalIgnoreCase);
        public bool HasDynamicOutputPorts => IsConditional || IsUserPrompt;

        public bool IsExternalCommand => (StepType != null && StepType.StartsWith("ExternalCommand", StringComparison.OrdinalIgnoreCase)) ||
                                         string.Equals(StepType, "ExecuteCommand", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(StepType, "RunCommand", StringComparison.OrdinalIgnoreCase);

        public bool IsImgur => (StepType != null && StepType.StartsWith("Imgur", StringComparison.OrdinalIgnoreCase)) ||
                               string.Equals(StepType, "UploadToImgur", StringComparison.OrdinalIgnoreCase);

        public bool IsJira => (StepType != null && StepType.StartsWith("Jira", StringComparison.OrdinalIgnoreCase)) ||
                              string.Equals(StepType, "UploadToJira", StringComparison.OrdinalIgnoreCase);

        public bool IsConfluence => (StepType != null && StepType.StartsWith("Confluence", StringComparison.OrdinalIgnoreCase)) ||
                                    string.Equals(StepType, "UploadToConfluence", StringComparison.OrdinalIgnoreCase);

        public bool IsOffice => (StepType != null && StepType.StartsWith("Office", StringComparison.OrdinalIgnoreCase)) ||
                                string.Equals(StepType, "Excel", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(StepType, "PowerPoint", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(StepType, "Powerpoint", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(StepType, "Word", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(StepType, "OneNote", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(StepType, "Outlook", StringComparison.OrdinalIgnoreCase);

        public bool IsZxing => (StepType != null && StepType.StartsWith("Zxing", StringComparison.OrdinalIgnoreCase)) ||
                               string.Equals(StepType, "BarcodeScan", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(StepType, "DecodeBarcode", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(StepType, "QrCode", StringComparison.OrdinalIgnoreCase);

        public bool IsBox => (StepType != null && StepType.StartsWith("Box", StringComparison.OrdinalIgnoreCase)) ||
                             string.Equals(StepType, "UploadToBox", StringComparison.OrdinalIgnoreCase);

        public bool IsDropbox => (StepType != null && StepType.StartsWith("Dropbox", StringComparison.OrdinalIgnoreCase)) ||
                                 string.Equals(StepType, "UploadToDropbox", StringComparison.OrdinalIgnoreCase);

        public bool IsCloudStorage => IsBox || IsDropbox;

        public StepPortViewModel InputPort { get; }
        public StepPortViewModel OutputPort { get; }

        public ObservableCollection<StepPortViewModel> Input { get; } = new ObservableCollection<StepPortViewModel>();
        public ObservableCollection<StepPortViewModel> Output { get; } = new ObservableCollection<StepPortViewModel>();

        public ObservableCollection<ConditionBranchViewModel> ConditionBranches { get; } = new ObservableCollection<ConditionBranchViewModel>();
        public ObservableCollection<PromptChoiceViewModel> PromptChoices { get; } = new ObservableCollection<PromptChoiceViewModel>();
        public ObservableCollection<VariableItemViewModel> Variables { get; } = new ObservableCollection<VariableItemViewModel>();
        public ObservableCollection<DrawableItemViewModel> Annotations { get; } = new ObservableCollection<DrawableItemViewModel>();

        public ICommand AddConditionBranchCommand { get; }
        public ICommand AddPromptChoiceCommand { get; }
        public ICommand AddVariableCommand { get; }
        public ICommand AddAnnotationCommand { get; }
        public ICommand ImportFromEditorCommand { get; }
        public ICommand ImportFromGreenshotFileCommand { get; }
        public ICommand BrowseSaveDirectoryCommand { get; }
        public ICommand BrowseExternalExecutableCommand { get; }
        public ICommand BrowseSoundFileCommand { get; }
        public ICommand PlaySoundPreviewCommand { get; }
        public ICommand PickBorderColorCommand { get; }
        public ICommand PickTextEffectColorCommand { get; }
        public ICommand PickCanvasBackgroundColorCommand { get; }
        public ICommand PickTransparencyBackgroundColorCommand { get; }

        public Action<StepNodeViewModel> OnSetStartNode { get; set; }
        public Action<StepNodeViewModel> OnToggleStartNode { get; set; }
        public ICommand SetAsStartNodeCommand { get; }
        public ICommand ToggleStartNodeCommand { get; }
        public Action<StepNodeViewModel> OnDeleteNode { get; set; }
        public ICommand DeleteNodeCommand { get; }

        public StepNodeViewModel(RecipeNodeConfig config, Point initialLocation, Action<StepNodeViewModel> onSetStartNode = null, Action<StepNodeViewModel> onDeleteNode = null, Action<StepNodeViewModel, string, string> onIdChanged = null, Action<StepNodeViewModel> onToggleStartNode = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _name = config.Name;
            _location = initialLocation;
            OnSetStartNode = onSetStartNode;
            OnToggleStartNode = onToggleStartNode;
            OnDeleteNode = onDeleteNode;
            OnIdChanged = onIdChanged;
            SetAsStartNodeCommand = new RelayCommand(() => OnSetStartNode?.Invoke(this));
            ToggleStartNodeCommand = new RelayCommand(() =>
            {
                IsStartNode = !IsStartNode;
            });
            DeleteNodeCommand = new RelayCommand(() => OnDeleteNode?.Invoke(this));

            InputPort = new StepPortViewModel(this, isInput: true);
            OutputPort = new StepPortViewModel(this, isInput: false);
            Input.Add(InputPort);
            Output.Add(OutputPort);

            AddConditionBranchCommand = new RelayCommand(() =>
            {
                char nextChar = (char)('A' + ConditionBranches.Count);
                string nextKey = nextChar <= 'Z' ? nextChar.ToString() : $"C{ConditionBranches.Count + 1}";
                AddConditionBranch(nextKey, "${true}");
            });

            AddPromptChoiceCommand = new RelayCommand(() =>
            {
                string nextKey = $"Choice{PromptChoices.Count + 1}";
                string nextLabel = $"Option {PromptChoices.Count + 1}";
                AddPromptChoice(nextKey, nextLabel, "Primary", false, false);
            });

            if (IsConditional)
            {
                LoadConditionBranches();
            }
            else if (IsUserPrompt)
            {
                LoadPromptChoices();
            }

            AddVariableCommand = new RelayCommand(() => AddVariable("new_var", "${user.username}"));
            AddAnnotationCommand = new RelayCommand(p => AddAnnotation((p as string) ?? "Rectangle"));
            ImportFromEditorCommand = new RelayCommand(ExecuteImportFromEditor);
            ImportFromGreenshotFileCommand = new RelayCommand(ExecuteImportFromGreenshotFile);
            BrowseSaveDirectoryCommand = new RelayCommand(() =>
            {
                using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dlg.Description = "Select Destination Folder for Captures";
                    dlg.ShowNewFolderButton = true;
                    if (!string.IsNullOrEmpty(SaveDirectory))
                    {
                        try
                        {
                            dlg.SelectedPath = Greenshot.Base.Core.FilenameHelper.FillVariables(SaveDirectory, false);
                        }
                        catch { }
                    }
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        SaveDirectory = dlg.SelectedPath;
                    }
                }
            });

            BrowseExternalExecutableCommand = new RelayCommand(() =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select Executable or Script for External Command",
                    Filter = "Executable & Script Files (*.exe;*.cmd;*.bat;*.ps1;*.vbs)|*.exe;*.cmd;*.bat;*.ps1;*.vbs|All Files (*.*)|*.*",
                    CheckFileExists = true
                };
                if (dlg.ShowDialog() == true)
                {
                    ExternalCommandLine = dlg.FileName;
                }
            });

            BrowseSoundFileCommand = new RelayCommand(() =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select Feedback / Shutter Sound File (.wav)",
                    Filter = "Wave Audio Files (*.wav)|*.wav|All Audio Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*",
                    CheckFileExists = true
                };
                if (dlg.ShowDialog() == true)
                {
                    SoundFilePath = dlg.FileName;
                }
            });

            PlaySoundPreviewCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrEmpty(SoundFilePath))
                {
                    string expanded = SoundFilePath;
                    try
                    {
                        expanded = Greenshot.Base.Core.FilenameHelper.FillVariables(SoundFilePath, false);
                    }
                    catch { }
                    Greenshot.Helpers.SoundHelper.PlayFile(expanded);
                }
                else
                {
                    Greenshot.Helpers.SoundHelper.Play();
                }
            });

            PickBorderColorCommand = new RelayCommand(() =>
            {
                if (string.Equals(StepType, WellKnownStepTypes.Border, StringComparison.OrdinalIgnoreCase))
                {
                    var picked = PromptColorHelper(BorderColor);
                    if (picked != null) BorderColor = picked;
                }
                else
                {
                    var picked = PromptColorHelper(EffectBorderColor);
                    if (picked != null) EffectBorderColor = picked;
                }
            });
            PickTextEffectColorCommand = new RelayCommand(() =>
            {
                var picked = PromptColorHelper(TextEffectFillColor);
                if (picked != null) TextEffectFillColor = picked;
            });
            PickCanvasBackgroundColorCommand = new RelayCommand(() =>
            {
                var picked = PromptColorHelper(CanvasBackgroundColor);
                if (picked != null) CanvasBackgroundColor = picked;
            });
            PickTransparencyBackgroundColorCommand = new RelayCommand(() =>
            {
                var picked = PromptColorHelper(TransparencyBackgroundColor);
                if (picked != null) TransparencyBackgroundColor = picked;
            });

            LoadVariablesFromConfig();
            LoadAnnotationsFromConfig();
        }

        public static string PromptColorHelper(string currentColor)
        {
            var initialColor = ParseColorHelper(currentColor);
            using (var cd = new Greenshot.Editor.Forms.ColorDialog { Color = initialColor })
            {
                if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (cd.Color.A == 0) return "transparent";
                    if (cd.Color.A == 255) return $"#{cd.Color.R:X2}{cd.Color.G:X2}{cd.Color.B:X2}";
                    return $"#{cd.Color.A:X2}{cd.Color.R:X2}{cd.Color.G:X2}{cd.Color.B:X2}";
                }
            }
            return null;
        }

        public static System.Drawing.Color ParseColorHelper(string colorStr)
        {
            if (string.IsNullOrWhiteSpace(colorStr) || string.Equals(colorStr, "transparent", StringComparison.OrdinalIgnoreCase))
            {
                return System.Drawing.Color.Transparent;
            }
            try
            {
                if (colorStr.StartsWith("#", StringComparison.Ordinal))
                {
                    return System.Drawing.ColorTranslator.FromHtml(colorStr);
                }
                var named = System.Drawing.Color.FromName(colorStr);
                if (named.IsKnownColor) return named;
                return System.Drawing.ColorTranslator.FromHtml(colorStr);
            }
            catch
            {
                return System.Drawing.Color.Black;
            }
        }

        public void LoadVariablesFromConfig()
        {
            Variables.Clear();
            if (Config.Parameters != null)
            {
                object varsObj = null;
                foreach (var kvp in Config.Parameters)
                {
                    if (string.Equals(kvp.Key, "Variables", StringComparison.OrdinalIgnoreCase))
                    {
                        varsObj = kvp.Value;
                        break;
                    }
                }

                if (varsObj != null)
                {
                    if (varsObj is IDictionary dict)
                    {
                        foreach (var key in dict.Keys)
                        {
                            Variables.Add(new VariableItemViewModel(key?.ToString() ?? "", dict[key]?.ToString() ?? "", SyncVariablesToConfig, RemoveVariable));
                        }
                    }
                    else if (varsObj is JObject jObj)
                    {
                        foreach (var prop in jObj.Properties())
                        {
                            Variables.Add(new VariableItemViewModel(prop.Name, prop.Value?.ToString() ?? "", SyncVariablesToConfig, RemoveVariable));
                        }
                    }
                }
                else
                {
                    string singleVar = GetParam("Variable", "");
                    if (!string.IsNullOrEmpty(singleVar))
                    {
                        string val = GetParam("Value", "");
                        Variables.Add(new VariableItemViewModel(singleVar, val, SyncVariablesToConfig, RemoveVariable));
                    }
                }
            }
        }

        public void SyncVariablesToConfig()
        {
            if (Config.Parameters == null)
            {
                Config.Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var v in Variables)
            {
                if (!string.IsNullOrWhiteSpace(v.Key))
                {
                    dict[v.Key] = v.Value ?? "";
                }
            }
            Config.Parameters["Variables"] = dict;
            NotifyConfigUpdated();
        }

        public void AddVariable(string key = "var_name", string value = "${user.username}")
        {
            var item = new VariableItemViewModel(key, value, SyncVariablesToConfig, RemoveVariable);
            Variables.Add(item);
            SyncVariablesToConfig();
        }

        public void RemoveVariable(VariableItemViewModel item)
        {
            if (item != null && Variables.Contains(item))
            {
                Variables.Remove(item);
                SyncVariablesToConfig();
            }
        }

        public void LoadAnnotationsFromConfig()
        {
            Annotations.Clear();
            if (Config.Parameters != null)
            {
                object aObj = null;
                foreach (var kvp in Config.Parameters)
                {
                    if (string.Equals(kvp.Key, "Annotations", StringComparison.OrdinalIgnoreCase))
                    {
                        aObj = kvp.Value;
                        break;
                    }
                }

                if (aObj != null)
                {
                    if (aObj is IEnumerable enumerable && !(aObj is string))
                    {
                        foreach (var elem in enumerable)
                        {
                            if (elem is IDictionary d)
                            {
                                Annotations.Add(DrawableItemViewModel.FromDictionary(d, SyncAnnotationsToConfig, RemoveAnnotation));
                            }
                            else if (elem is JObject jObj)
                            {
                                var jDict = jObj.ToObject<Dictionary<string, object>>();
                                Annotations.Add(DrawableItemViewModel.FromDictionary(jDict, SyncAnnotationsToConfig, RemoveAnnotation));
                            }
                        }
                    }
                }
                else if (Config.Parameters.Keys.Any(k => string.Equals(k, "AnnotationType", StringComparison.OrdinalIgnoreCase) ||
                                                         string.Equals(k, "Shape", StringComparison.OrdinalIgnoreCase) ||
                                                         string.Equals(k, "Type", StringComparison.OrdinalIgnoreCase)))
                {
                    Annotations.Add(DrawableItemViewModel.FromDictionary(Config.Parameters, SyncAnnotationsToConfig, RemoveAnnotation));
                }
            }
        }

        public void SyncAnnotationsToConfig()
        {
            if (Config.Parameters == null)
            {
                Config.Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            Config.Parameters["Annotations"] = Annotations.Select(d => d.ToDictionary()).ToList();
            NotifyConfigUpdated();
        }

        public void AddAnnotation(string type = "Rectangle")
        {
            var item = new DrawableItemViewModel(SyncAnnotationsToConfig, RemoveAnnotation)
            {
                Type = type
            };
            if (string.Equals(type, "Text", StringComparison.OrdinalIgnoreCase))
            {
                item.Text = "Captured: ${now:yyyy-MM-dd}";
                item.Width = "220";
                item.Height = "32";
                item.FillColor = "transparent";
                item.LineColor = "#FF0000";
                item.FontSize = 12.0;
                item.FontBold = false;
                item.FontItalic = false;
                item.Shadow = true;
                item.LineThickness = 0;
            }
            else if (string.Equals(type, "Speechbubble", StringComparison.OrdinalIgnoreCase))
            {
                item.Text = "Note";
                item.Width = "160";
                item.Height = "60";
                item.FontSize = 14.0;
                item.FontBold = true;
                item.LineColor = "#0000FF";
                item.FillColor = "#FFFFFF";
                item.LineThickness = 2;
                item.Shadow = false;
            }
            else if (string.Equals(type, "StepLabel", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "Counter", StringComparison.OrdinalIgnoreCase))
            {
                item.StepNumber = 1;
                item.Text = "1";
                item.Width = "28";
                item.Height = "28";
                item.FillColor = "#E81123";
                item.LineColor = "#FFFFFF";
                item.Shadow = false;
            }
            else if (string.Equals(type, "Emoji", StringComparison.OrdinalIgnoreCase))
            {
                item.Emoji = "🛡️";
                item.Width = "32";
                item.Height = "32";
            }
            else if (string.Equals(type, "Arrow", StringComparison.OrdinalIgnoreCase))
            {
                item.ArrowHeads = "END_POINT";
                item.LineThickness = 2;
                item.LineColor = "#FF0000";
                item.Shadow = true;
                item.Width = "100";
                item.Height = "50";
            }
            else if (string.Equals(type, "Line", StringComparison.OrdinalIgnoreCase))
            {
                item.LineThickness = 2;
                item.LineColor = "#FF0000";
                item.Shadow = true;
                item.Width = "100";
                item.Height = "50";
            }
            else if (string.Equals(type, "Blur", StringComparison.OrdinalIgnoreCase))
            {
                item.BlurRadius = 10;
                item.Width = "150";
                item.Height = "80";
            }
            else if (string.Equals(type, "Pixelize", StringComparison.OrdinalIgnoreCase))
            {
                item.PixelSize = 5;
                item.Width = "150";
                item.Height = "80";
            }
            else if (string.Equals(type, "Magnify", StringComparison.OrdinalIgnoreCase))
            {
                item.MagnificationFactor = 2;
                item.Width = "120";
                item.Height = "120";
            }
            else if (string.Equals(type, "Highlight", StringComparison.OrdinalIgnoreCase))
            {
                item.FillColor = "#FFFF00";
                item.LineColor = "#FF0000";
                item.Width = "150";
                item.Height = "40";
            }
            else if (string.Equals(type, "QRCode", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "Barcode", StringComparison.OrdinalIgnoreCase))
            {
                item.Text = "https://getgreenshot.org";
                item.ForeColor = "#000000";
                item.BackColor = "#FFFFFF";
                item.Margin = 1;
                item.RoundedDots = false;
                if (string.Equals(type, "QRCode", StringComparison.OrdinalIgnoreCase))
                {
                    item.Width = "150";
                    item.Height = "150";
                }
                else
                {
                    item.Width = "250";
                    item.Height = "80";
                }
            }
            else if (string.Equals(type, "Image", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "Svg", StringComparison.OrdinalIgnoreCase))
            {
                item.Width = "120";
                item.Height = "120";
            }
            else if (string.Equals(type, "Cursor", StringComparison.OrdinalIgnoreCase))
            {
                item.CursorName = "Arrow";
                item.Width = "32";
                item.Height = "32";
                item.LockAspectRatio = true;
            }
            else if (item.LockAspectRatio)
            {
                item.Width = "150";
                item.Height = "150";
            }
            Annotations.Add(item);
            SyncAnnotationsToConfig();
        }

        public void RemoveAnnotation(DrawableItemViewModel item)
        {
            if (item != null && Annotations.Contains(item))
            {
                Annotations.Remove(item);
                SyncAnnotationsToConfig();
            }
        }

        public void ImportAnnotations(IEnumerable<Dictionary<string, object>> annotations, bool replaceExisting = false)
        {
            if (annotations == null) return;
            if (replaceExisting)
            {
                Annotations.Clear();
            }
            foreach (var dict in annotations)
            {
                Annotations.Add(DrawableItemViewModel.FromDictionary(dict, SyncAnnotationsToConfig, RemoveAnnotation));
            }
            SyncAnnotationsToConfig();
        }

        private static Window GetActiveWindow()
        {
            return Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                ?? Application.Current?.Windows.OfType<Window>().FirstOrDefault()
                ?? Application.Current?.MainWindow;
        }

        private static MessageBoxResult ShowMessageBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            var owner = GetActiveWindow();
            if (owner != null)
            {
                return MessageBox.Show(owner, messageBoxText, caption, button, icon);
            }
            return MessageBox.Show(messageBoxText, caption, button, icon);
        }

        private static void ShowInfoMessage(string messageBoxText, string caption, MessageBoxImage icon = MessageBoxImage.Information)
        {
            var owner = GetActiveWindow();
            if (owner != null)
            {
                MessageBox.Show(owner, messageBoxText, caption, MessageBoxButton.OK, icon);
            }
            else
            {
                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, icon);
            }
        }

        private void ExecuteImportFromEditor()
        {
            var editors = Greenshot.Editor.Forms.ImageEditorForm.Editors;
            if (editors == null || editors.Count == 0)
            {
                var result = ShowMessageBox(
                    "No open image editors were found in Greenshot.\n\nWould you like to import annotations from a saved .greenshot file instead?",
                    "No Open Editors Found",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    ExecuteImportFromGreenshotFile();
                }
                return;
            }

            IImageEditor chosenEditor = null;
            if (editors.Count == 1)
            {
                chosenEditor = editors[0];
            }
            else
            {
                var owner = GetActiveWindow();
                var dlg = new SelectEditorDialog(editors);
                if (owner != null)
                {
                    dlg.Owner = owner;
                }
                else
                {
                    dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                if (dlg.ShowDialog() == true && dlg.SelectedEditor != null)
                {
                    chosenEditor = dlg.SelectedEditor;
                }
                else
                {
                    return;
                }
            }

            if (chosenEditor?.Surface == null)
            {
                ShowInfoMessage("Could not access the surface of the selected editor.", "Import Failed", MessageBoxImage.Warning);
                return;
            }

            var items = EditorAnnotationImporter.AnalyzeSurfaceElements(chosenEditor.Surface);
            if (items.Count == 0)
            {
                ShowInfoMessage("The selected image editor does not contain any annotations.", "No Annotations Found", MessageBoxImage.Information);
                return;
            }

            string sourceTitle = chosenEditor.CaptureDetails?.Title ?? chosenEditor.Surface?.CaptureDetails?.Title ?? "Image Editor";
            string recipeName = RecipeNameProvider?.Invoke() ?? DisplayName;

            var ownerWin = GetActiveWindow();
            var importDlg = new ImportAnnotationsDialog(items, sourceTitle, recipeName, Annotations.Count > 0);
            if (ownerWin != null)
            {
                importDlg.Owner = ownerWin;
            }
            else
            {
                importDlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            if (importDlg.ShowDialog() == true && importDlg.ResultAnnotations != null && importDlg.ResultAnnotations.Count > 0)
            {
                ImportAnnotations(importDlg.ResultAnnotations, importDlg.ShouldReplaceExisting);
            }
        }

        private void ExecuteImportFromGreenshotFile()
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Greenshot File to Import Annotations",
                Filter = "Greenshot Files (*.greenshot)|*.greenshot|All Files (*.*)|*.*",
                CheckFileExists = true
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    var surface = EditorAnnotationImporter.LoadSurfaceFromGreenshotFile(ofd.FileName);
                    var items = EditorAnnotationImporter.AnalyzeSurfaceElements(surface);
                    if (items.Count == 0)
                    {
                        ShowInfoMessage("No annotations were found in the selected .greenshot file.", "No Annotations Found", MessageBoxImage.Information);
                        return;
                    }

                    string sourceTitle = System.IO.Path.GetFileName(ofd.FileName);
                    string recipeName = RecipeNameProvider?.Invoke() ?? DisplayName;

                    var ownerWin = GetActiveWindow();
                    var importDlg = new ImportAnnotationsDialog(items, sourceTitle, recipeName, Annotations.Count > 0);
                    if (ownerWin != null)
                    {
                        importDlg.Owner = ownerWin;
                    }
                    else
                    {
                        importDlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    }

                    if (importDlg.ShowDialog() == true && importDlg.ResultAnnotations != null && importDlg.ResultAnnotations.Count > 0)
                    {
                        ImportAnnotations(importDlg.ResultAnnotations, importDlg.ShouldReplaceExisting);
                    }
                }
                catch (Exception ex)
                {
                    ShowInfoMessage($"Failed to load .greenshot file:\n{ex.Message}", "Import Error", MessageBoxImage.Error);
                }
            }
        }

        #region Typed Step Properties for Two-Way WPF Binding

        // --- 1. Source Step ---
        public string SourceType
        {
            get => GetParam("SourceType", "Region");
            set { SetParam("SourceType", value); OnPropertyChanged(nameof(IsWindowSource)); }
        }

        public bool IsWindowSource => SourceType == "Window" || SourceType == "ActiveWindow";

        public string WindowTitlePattern
        {
            get => GetParam("WindowTitlePattern", "");
            set => SetParam("WindowTitlePattern", value);
        }

        public string ProcessName
        {
            get => GetParam("ProcessName", "");
            set => SetParam("ProcessName", value);
        }

        public string CaptureMouseCursorMode
        {
            get
            {
                if (Config.Parameters != null && Config.Parameters.TryGetValue("CaptureMouseCursor", out var val) && val != null)
                {
                    if (val is bool b) return b ? "true" : "false";
                    if (bool.TryParse(val.ToString(), out bool pb)) return pb ? "true" : "false";
                }
                return "";
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Config.Parameters?.Remove("CaptureMouseCursor");
                }
                else
                {
                    SetParam("CaptureMouseCursor", value == "true");
                }
                OnPropertyChanged();
                NotifyConfigUpdated();
            }
        }

        public int DelayMs
        {
            get
            {
                if (int.TryParse(GetParam("DelayMs", "0"), out int d)) return d;
                return 0;
            }
            set => SetParam("DelayMs", value);
        }

        // --- 2. InteractiveSelection Step ---
        public string SelectionMode
        {
            get => GetParam("SelectionMode", "Region");
            set => SetParam("SelectionMode", value);
        }

        public bool AllowWindowSnapping
        {
            get => GetParamBool("AllowWindowSnapping", true);
            set => SetParam("AllowWindowSnapping", value);
        }

        // --- 3. Border Step ---
        public int BorderWidth
        {
            get
            {
                if (int.TryParse(GetParam("Width", "2"), out int w)) return w;
                return 2;
            }
            set => SetParam("Width", value);
        }

        public string BorderColor
        {
            get => GetParam("Color", "#0078D7");
            set => SetParam("Color", value);
        }

        // --- 4. Effect Step ---
        public string EffectType
        {
            get => GetParam("Effect", "DropShadow");
            set
            {
                SetParam("Effect", value);
                NotifyEffectProperties();
            }
        }

        public bool IsBorderEffect => string.Equals(EffectType, "Border", StringComparison.OrdinalIgnoreCase);
        public bool IsDropShadowEffect => string.Equals(EffectType, "DropShadow", StringComparison.OrdinalIgnoreCase);
        public bool IsTornEdgeEffect => string.Equals(EffectType, "TornEdge", StringComparison.OrdinalIgnoreCase);
        public bool IsGrayscaleEffect => string.Equals(EffectType, "Grayscale", StringComparison.OrdinalIgnoreCase);
        public bool IsInvertEffect => string.Equals(EffectType, "Invert", StringComparison.OrdinalIgnoreCase);
        public bool IsMonochromeEffect => string.Equals(EffectType, "Monochrome", StringComparison.OrdinalIgnoreCase);
        public bool IsAdjustEffect => string.Equals(EffectType, "Adjust", StringComparison.OrdinalIgnoreCase);
        public bool IsRotateEffect => string.Equals(EffectType, "Rotate", StringComparison.OrdinalIgnoreCase);
        public bool IsResizeEffect => string.Equals(EffectType, "Resize", StringComparison.OrdinalIgnoreCase);
        public bool IsResizeCanvasEffect => string.Equals(EffectType, "ResizeCanvas", StringComparison.OrdinalIgnoreCase);
        public bool IsReduceColorsEffect => string.Equals(EffectType, "ReduceColors", StringComparison.OrdinalIgnoreCase);
        public bool IsRemoveTransparencyEffect => string.Equals(EffectType, "RemoveTransparency", StringComparison.OrdinalIgnoreCase);

        public void NotifyEffectProperties()
        {
            OnPropertyChanged(nameof(EffectType));
            OnPropertyChanged(nameof(IsBorderEffect));
            OnPropertyChanged(nameof(IsDropShadowEffect));
            OnPropertyChanged(nameof(IsTornEdgeEffect));
            OnPropertyChanged(nameof(IsGrayscaleEffect));
            OnPropertyChanged(nameof(IsInvertEffect));
            OnPropertyChanged(nameof(IsMonochromeEffect));
            OnPropertyChanged(nameof(IsAdjustEffect));
            OnPropertyChanged(nameof(IsRotateEffect));
            OnPropertyChanged(nameof(IsResizeEffect));
            OnPropertyChanged(nameof(IsResizeCanvasEffect));
            OnPropertyChanged(nameof(IsReduceColorsEffect));
            OnPropertyChanged(nameof(IsRemoveTransparencyEffect));
            OnPropertyChanged(nameof(EffectBorderWidth));
            OnPropertyChanged(nameof(EffectBorderColor));
            OnPropertyChanged(nameof(ShadowSize));
            OnPropertyChanged(nameof(ShadowDarkness));
            OnPropertyChanged(nameof(ShadowOffsetX));
            OnPropertyChanged(nameof(ShadowOffsetY));
            OnPropertyChanged(nameof(ToothHeight));
            OnPropertyChanged(nameof(HorizontalToothRange));
            OnPropertyChanged(nameof(VerticalToothRange));
            OnPropertyChanged(nameof(TornEdgeShadow));
            OnPropertyChanged(nameof(TornEdges));
            OnPropertyChanged(nameof(MonochromeThreshold));
            OnPropertyChanged(nameof(AdjustBrightness));
            OnPropertyChanged(nameof(AdjustContrast));
            OnPropertyChanged(nameof(AdjustGamma));
            OnPropertyChanged(nameof(RotateAngle));
            OnPropertyChanged(nameof(ResizeWidth));
            OnPropertyChanged(nameof(ResizeHeight));
            OnPropertyChanged(nameof(ResizePercentage));
            OnPropertyChanged(nameof(ResizeMaintainAspectRatio));
            OnPropertyChanged(nameof(CanvasMargin));
            OnPropertyChanged(nameof(CanvasBackgroundColor));
            OnPropertyChanged(nameof(ReduceColorsCount));
            OnPropertyChanged(nameof(TransparencyBackgroundColor));
            OnPropertyChanged(nameof(Summary));
        }

        // Border Effect Parameters
        public int EffectBorderWidth
        {
            get => int.TryParse(GetParam("Width", "2"), out int w) ? w : 2;
            set { SetParam("Width", value); OnPropertyChanged(nameof(EffectBorderWidth)); OnPropertyChanged(nameof(Summary)); }
        }

        public string EffectBorderColor
        {
            get => GetParam("Color", "#0000FF");
            set { SetParam("Color", value); OnPropertyChanged(nameof(EffectBorderColor)); OnPropertyChanged(nameof(Summary)); }
        }

        // Drop Shadow Parameters
        public int ShadowSize
        {
            get => int.TryParse(GetParam("ShadowSize", "10"), out int s) ? s : 10;
            set { SetParam("ShadowSize", value); OnPropertyChanged(nameof(ShadowSize)); OnPropertyChanged(nameof(Summary)); }
        }

        public double ShadowDarkness
        {
            get => double.TryParse(GetParam("Darkness", "0.6"), NumberStyles.Any, CultureInfo.InvariantCulture, out double d) ? d : 0.6;
            set { SetParam("Darkness", value); OnPropertyChanged(nameof(ShadowDarkness)); OnPropertyChanged(nameof(Summary)); }
        }

        public int ShadowOffsetX
        {
            get => int.TryParse(GetParam("ShadowOffsetX", "0"), out int s) ? s : 0;
            set { SetParam("ShadowOffsetX", value); OnPropertyChanged(nameof(ShadowOffsetX)); }
        }

        public int ShadowOffsetY
        {
            get => int.TryParse(GetParam("ShadowOffsetY", "0"), out int s) ? s : 0;
            set { SetParam("ShadowOffsetY", value); OnPropertyChanged(nameof(ShadowOffsetY)); }
        }

        // Torn Edge Parameters
        public int ToothHeight
        {
            get => int.TryParse(GetParam("ToothHeight", "12"), out int t) ? t : 12;
            set { SetParam("ToothHeight", value); OnPropertyChanged(nameof(ToothHeight)); }
        }

        public int HorizontalToothRange
        {
            get => int.TryParse(GetParam("HorizontalToothRange", "20"), out int h) ? h : 20;
            set { SetParam("HorizontalToothRange", value); OnPropertyChanged(nameof(HorizontalToothRange)); }
        }

        public int VerticalToothRange
        {
            get => int.TryParse(GetParam("VerticalToothRange", "20"), out int v) ? v : 20;
            set { SetParam("VerticalToothRange", value); OnPropertyChanged(nameof(VerticalToothRange)); }
        }

        public bool TornEdgeShadow
        {
            get => bool.TryParse(GetParam("GenerateShadow", "true"), out bool b) ? b : true;
            set { SetParam("GenerateShadow", value); OnPropertyChanged(nameof(TornEdgeShadow)); }
        }

        public string TornEdges
        {
            get => GetParam("Edges", "top,right,bottom,left");
            set { SetParam("Edges", value); OnPropertyChanged(nameof(TornEdges)); OnPropertyChanged(nameof(Summary)); }
        }

        // Monochrome Parameters
        public byte MonochromeThreshold
        {
            get => byte.TryParse(GetParam("Threshold", "128"), out byte t) ? t : (byte)128;
            set { SetParam("Threshold", (int)value); OnPropertyChanged(nameof(MonochromeThreshold)); OnPropertyChanged(nameof(Summary)); }
        }

        // Adjust Parameters
        public float AdjustBrightness
        {
            get => float.TryParse(GetParam("Brightness", "1.0"), NumberStyles.Any, CultureInfo.InvariantCulture, out float f) ? f : 1.0f;
            set { SetParam("Brightness", value.ToString("0.0#", CultureInfo.InvariantCulture)); OnPropertyChanged(nameof(AdjustBrightness)); OnPropertyChanged(nameof(Summary)); }
        }

        public float AdjustContrast
        {
            get => float.TryParse(GetParam("Contrast", "1.0"), NumberStyles.Any, CultureInfo.InvariantCulture, out float f) ? f : 1.0f;
            set { SetParam("Contrast", value.ToString("0.0#", CultureInfo.InvariantCulture)); OnPropertyChanged(nameof(AdjustContrast)); OnPropertyChanged(nameof(Summary)); }
        }

        public float AdjustGamma
        {
            get => float.TryParse(GetParam("Gamma", "1.0"), NumberStyles.Any, CultureInfo.InvariantCulture, out float f) ? f : 1.0f;
            set { SetParam("Gamma", value.ToString("0.0#", CultureInfo.InvariantCulture)); OnPropertyChanged(nameof(AdjustGamma)); OnPropertyChanged(nameof(Summary)); }
        }

        // Rotate Parameters
        public int RotateAngle
        {
            get => int.TryParse(GetParam("Angle", "90"), out int a) ? a : 90;
            set { SetParam("Angle", value); OnPropertyChanged(nameof(RotateAngle)); OnPropertyChanged(nameof(Summary)); }
        }

        // Resize Parameters
        public string ResizeWidth
        {
            get => GetParam("Width", "0");
            set { SetParam("Width", value); OnPropertyChanged(nameof(ResizeWidth)); OnPropertyChanged(nameof(Summary)); }
        }

        public string ResizeHeight
        {
            get => GetParam("Height", "0");
            set { SetParam("Height", value); OnPropertyChanged(nameof(ResizeHeight)); OnPropertyChanged(nameof(Summary)); }
        }

        public string ResizePercentage
        {
            get => GetParam("Percentage", "0");
            set { SetParam("Percentage", value); OnPropertyChanged(nameof(ResizePercentage)); OnPropertyChanged(nameof(Summary)); }
        }

        public bool ResizeMaintainAspectRatio
        {
            get => bool.TryParse(GetParam("MaintainAspectRatio", "true"), out bool b) ? b : true;
            set { SetParam("MaintainAspectRatio", value); OnPropertyChanged(nameof(ResizeMaintainAspectRatio)); }
        }

        // Resize Canvas Parameters
        public int CanvasMargin
        {
            get => int.TryParse(GetParam("Margin", "20"), out int m) ? m : 20;
            set { SetParam("Margin", value); OnPropertyChanged(nameof(CanvasMargin)); OnPropertyChanged(nameof(Summary)); }
        }

        public string CanvasBackgroundColor
        {
            get => GetParam("BackgroundColor", "#FFFFFF");
            set { SetParam("BackgroundColor", value); OnPropertyChanged(nameof(CanvasBackgroundColor)); }
        }

        // Reduce Colors Parameters
        public int ReduceColorsCount
        {
            get => int.TryParse(GetParam("Colors", "256"), out int c) ? c : 256;
            set { SetParam("Colors", value); OnPropertyChanged(nameof(ReduceColorsCount)); OnPropertyChanged(nameof(Summary)); }
        }

        // Remove Transparency Parameters
        public string TransparencyBackgroundColor
        {
            get => GetParam("Color", "#FFFFFF");
            set { SetParam("Color", value); OnPropertyChanged(nameof(TransparencyBackgroundColor)); }
        }

        // --- 5. TextEffect / ObfuscateText Step ---
        public string TextEffectAction
        {
            get => GetParam("Effect", "Redact");
            set => SetParam("Effect", value);
        }

        public string TextEffectFillColor
        {
            get => GetParam("FillColor", "#000000");
            set => SetParam("FillColor", value);
        }

        public string TextEffectPattern
        {
            get => GetParam("Pattern", @"\b\d{4}-\d{4}\b");
            set => SetParam("Pattern", value);
        }

        // --- 6. Conditional Step ---
        public void AddConditionBranch(string key = null, string expression = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                char nextChar = (char)('A' + ConditionBranches.Count);
                key = nextChar <= 'Z' ? nextChar.ToString() : $"C{ConditionBranches.Count + 1}";
            }
            if (string.IsNullOrEmpty(expression))
            {
                expression = "${true}";
            }

            var branch = new ConditionBranchViewModel(key, expression, this, SyncConditionBranchesToConfig, RemoveConditionBranch);
            ConditionBranches.Add(branch);
            Output.Add(branch.Port);
            SyncConditionBranchesToConfig();
            OnPropertyChanged(nameof(Summary));
        }

        public void RemoveConditionBranch(ConditionBranchViewModel branch)
        {
            if (branch == null) return;
            if (ConditionBranches.Count <= 1) return; // Keep at least 1 branch

            ConditionBranches.Remove(branch);
            Output.Remove(branch.Port);
            SyncConditionBranchesToConfig();
            OnPropertyChanged(nameof(Summary));
        }

        private void LoadConditionBranches()
        {
            ConditionBranches.Clear();
            Output.Remove(OutputPort);

            var branchesParam = Config.GetParameter<object>("Branches") ?? Config.GetParameter<object>("branches");
            if (branchesParam is IEnumerable enumerable && !(branchesParam is string))
            {
                foreach (var item in enumerable)
                {
                    if (item is IDictionary dict)
                    {
                        string k = dict.Contains("Key") ? dict["Key"]?.ToString() : (dict.Contains("key") ? dict["key"]?.ToString() : null);
                        string exp = dict.Contains("Expression") ? dict["Expression"]?.ToString() : (dict.Contains("expression") ? dict["expression"]?.ToString() : null);
                        if (!string.IsNullOrEmpty(k) || !string.IsNullOrEmpty(exp))
                        {
                            var b = new ConditionBranchViewModel(k ?? "A", exp ?? "${true}", this, SyncConditionBranchesToConfig, RemoveConditionBranch);
                            ConditionBranches.Add(b);
                            Output.Add(b.Port);
                        }
                    }
                    else if (item is Newtonsoft.Json.Linq.JObject jobj)
                    {
                        string k = jobj.Value<string>("Key") ?? jobj.Value<string>("key");
                        string exp = jobj.Value<string>("Expression") ?? jobj.Value<string>("expression");
                        var b = new ConditionBranchViewModel(k ?? "A", exp ?? "${true}", this, SyncConditionBranchesToConfig, RemoveConditionBranch);
                        ConditionBranches.Add(b);
                        Output.Add(b.Port);
                    }
                }
            }

            if (ConditionBranches.Count == 0)
            {
                var b1 = new ConditionBranchViewModel("A", "${payload.width > 800}", this, SyncConditionBranchesToConfig, RemoveConditionBranch);
                var b2 = new ConditionBranchViewModel("B", "else", this, SyncConditionBranchesToConfig, RemoveConditionBranch);
                ConditionBranches.Add(b1);
                ConditionBranches.Add(b2);
                Output.Add(b1.Port);
                Output.Add(b2.Port);
                SyncConditionBranchesToConfig();
            }
        }

        private void SyncConditionBranchesToConfig()
        {
            var list = new List<Dictionary<string, string>>();
            foreach (var b in ConditionBranches)
            {
                list.Add(new Dictionary<string, string>
                {
                    ["Key"] = b.Key,
                    ["Expression"] = b.Expression
                });
            }
            Config.Set("Branches", list);
            Config.Parameters?.Remove("Condition");
            Config.Parameters?.Remove("condition");
            OnPropertyChanged(nameof(Summary));
        }

        public void AddPromptChoice(string key, string label, string style = "Primary", bool isDefault = false, bool isCancel = false)
        {
            if (string.IsNullOrEmpty(key))
            {
                key = $"Choice{PromptChoices.Count + 1}";
            }
            if (string.IsNullOrEmpty(label))
            {
                label = key;
            }

            var choice = new PromptChoiceViewModel(key, label, style, isDefault, isCancel, this, SyncPromptChoicesToConfig, RemovePromptChoice);
            PromptChoices.Add(choice);
            Output.Remove(OutputPort);
            Output.Add(choice.Port);
            SyncPromptChoicesToConfig();
            OnPropertyChanged(nameof(Summary));
        }

        public void RemovePromptChoice(PromptChoiceViewModel choice)
        {
            if (choice == null) return;
            if (PromptChoices.Count <= 1) return; // Keep at least 1 choice

            PromptChoices.Remove(choice);
            Output.Remove(choice.Port);
            SyncPromptChoicesToConfig();
            OnPropertyChanged(nameof(Summary));
        }

        private void LoadPromptChoices()
        {
            PromptChoices.Clear();
            Output.Remove(OutputPort);

            var choicesParam = Config.GetParameter<object>("Choices") ?? Config.GetParameter<object>("choices");
            if (choicesParam is IEnumerable enumerable && !(choicesParam is string))
            {
                foreach (var item in enumerable)
                {
                    if (item is IDictionary dict)
                    {
                        string k = dict.Contains("Key") ? dict["Key"]?.ToString() : (dict.Contains("key") ? dict["key"]?.ToString() : null);
                        string l = dict.Contains("Label") ? dict["Label"]?.ToString() : (dict.Contains("label") ? dict["label"]?.ToString() : null);
                        string s = dict.Contains("Style") ? dict["Style"]?.ToString() : (dict.Contains("style") ? dict["style"]?.ToString() : "Primary");
                        bool isDef = dict.Contains("IsDefault") && Convert.ToBoolean(dict["IsDefault"]);
                        bool isCanc = dict.Contains("IsCancel") && Convert.ToBoolean(dict["IsCancel"]);
                        if (!string.IsNullOrEmpty(k))
                        {
                            var c = new PromptChoiceViewModel(k, l ?? k, s, isDef, isCanc, this, SyncPromptChoicesToConfig, RemovePromptChoice);
                            PromptChoices.Add(c);
                            Output.Add(c.Port);
                        }
                    }
                    else if (item is Newtonsoft.Json.Linq.JObject jobj)
                    {
                        string k = jobj.Value<string>("Key") ?? jobj.Value<string>("key");
                        string l = jobj.Value<string>("Label") ?? jobj.Value<string>("label");
                        string s = jobj.Value<string>("Style") ?? jobj.Value<string>("style") ?? "Primary";
                        bool isDef = jobj.Value<bool?>("IsDefault") ?? jobj.Value<bool?>("isDefault") ?? false;
                        bool isCanc = jobj.Value<bool?>("IsCancel") ?? jobj.Value<bool?>("isCancel") ?? false;
                        if (!string.IsNullOrEmpty(k))
                        {
                            var c = new PromptChoiceViewModel(k, l ?? k, s, isDef, isCanc, this, SyncPromptChoicesToConfig, RemovePromptChoice);
                            PromptChoices.Add(c);
                            Output.Add(c.Port);
                        }
                    }
                }
            }

            if (PromptChoices.Count == 0)
            {
                var c1 = new PromptChoiceViewModel("Yes", "Yes, Proceed", "Primary", true, false, this, SyncPromptChoicesToConfig, RemovePromptChoice);
                var c2 = new PromptChoiceViewModel("No", "No, Cancel", "Secondary", false, true, this, SyncPromptChoicesToConfig, RemovePromptChoice);
                PromptChoices.Add(c1);
                PromptChoices.Add(c2);
                Output.Add(c1.Port);
                Output.Add(c2.Port);
                SyncPromptChoicesToConfig();
            }
        }

        private void SyncPromptChoicesToConfig()
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var c in PromptChoices)
            {
                list.Add(c.ToDictionary());
            }
            Config.Set("Choices", list);
            OnPropertyChanged(nameof(Summary));
        }

        // --- 7. Notification Step ---
        public string NotificationTitle
        {
            get => GetParam("Title", "Greenshot");
            set => SetParam("Title", value);
        }

        public string NotificationMessage
        {
            get => GetParam("Message", "Capture completed");
            set => SetParam("Message", value);
        }

        // --- 8. ImmediateFeedback Step ---
        public bool PlaySound
        {
            get => GetParamBool("PlaySound", true);
            set => SetParam("PlaySound", value);
        }

        public string SoundFilePath
        {
            get => GetParam("SoundFilePath", "");
            set
            {
                SetParamOrRemoveIfEmpty("SoundFilePath", value);
                OnPropertyChanged(nameof(SoundFilePath));
                OnPropertyChanged(nameof(Summary));
            }
        }

        // --- 9. Destinations Step ---
        public bool DestinationEditor
        {
            get => HasDestination("Editor");
            set => ToggleDestination("Editor", value);
        }

        public bool DestinationClipboard
        {
            get => HasDestination("Clipboard");
            set => ToggleDestination("Clipboard", value);
        }

        public bool DestinationFile
        {
            get => HasDestination("File");
            set => ToggleDestination("File", value);
        }

        public bool DestinationPrinter
        {
            get => HasDestination("Printer");
            set => ToggleDestination("Printer", value);
        }

        public bool DestinationEmail
        {
            get => HasDestination("EMail");
            set => ToggleDestination("EMail", value);
        }

        public bool DestinationOcr
        {
            get => HasDestination("OCR");
            set => ToggleDestination("OCR", value);
        }

        public string SaveDirectory
        {
            get => GetParam("SaveDirectory", "");
            set { SetParam("SaveDirectory", value); OnPropertyChanged(nameof(SaveDirectory)); OnPropertyChanged(nameof(Summary)); }
        }

        public string FilenamePattern
        {
            get => GetParam("FilenamePattern", "greenshot ${capturetime}");
            set { SetParam("FilenamePattern", value); OnPropertyChanged(nameof(FilenamePattern)); OnPropertyChanged(nameof(Summary)); }
        }

        public string ImageFormat
        {
            get => GetParam("Format", "png");
            set { SetParam("Format", value); OnPropertyChanged(nameof(ImageFormat)); OnPropertyChanged(nameof(Summary)); }
        }

        public bool AllowOverwrite
        {
            get => GetParamBool("AllowOverwrite", false);
            set { SetParam("AllowOverwrite", value); OnPropertyChanged(nameof(AllowOverwrite)); OnPropertyChanged(nameof(Summary)); }
        }

        public string CustomDestinationsText
        {
            get
            {
                var list = GetDestinationList();
                var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Editor", "Clipboard", "File", "Printer", "EMail", "OCR" };
                var customs = list.Where(d => !known.Contains(d)).ToList();
                return string.Join(", ", customs);
            }
            set
            {
                var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Editor", "Clipboard", "File", "Printer", "EMail", "OCR" };
                var current = GetDestinationList().Where(d => known.Contains(d)).ToList();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var customItems = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                    foreach (var item in customItems)
                    {
                        if (!current.Contains(item, StringComparer.OrdinalIgnoreCase))
                        {
                            current.Add(item);
                        }
                    }
                }
                SetParam("DestinationDesignations", current);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Summary));
            }
        }

        public string CustomDestinationId
        {
            get => GetParam("CustomDestinationId", GetParam("Destination", ""));
            set { SetParam("CustomDestinationId", value); SetParam("Destination", value); OnPropertyChanged(nameof(Summary)); }
        }

        public bool ShowPrintDialog
        {
            get => GetParamBool("ShowPrintDialog", true);
            set => SetParam("ShowPrintDialog", value);
        }

        public string EmailSubject
        {
            get => GetParam("EmailSubject", "Screenshot");
            set => SetParam("EmailSubject", value);
        }

        public string EmailRecipient
        {
            get => GetParam("EmailRecipient", "");
            set => SetParam("EmailRecipient", value);
        }

        // --- 10. Dedicated Clipboard Step ---
        public string ClipboardMode
        {
            get => GetParam("ClipboardMode", "ImageOnly");
            set
            {
                SetParam("ClipboardMode", value);
                OnPropertyChanged(nameof(ClipboardMode));
                OnPropertyChanged(nameof(IsClipboardTextEnabled));
                OnPropertyChanged(nameof(Summary));
            }
        }

        public bool IsClipboardTextEnabled => string.Equals(ClipboardMode, "TextOnly", StringComparison.OrdinalIgnoreCase) ||
                                              string.Equals(ClipboardMode, "ImageAndText", StringComparison.OrdinalIgnoreCase) ||
                                              ClipboardFormatText;

        public bool ClipboardFormatPNG
        {
            get => GetParamBool("ClipboardFormatPNG", true);
            set { SetParam("ClipboardFormatPNG", value); OnPropertyChanged(nameof(ClipboardFormatPNG)); OnPropertyChanged(nameof(Summary)); }
        }

        public bool ClipboardFormatDIB
        {
            get => GetParamBool("ClipboardFormatDIB", true);
            set { SetParam("ClipboardFormatDIB", value); OnPropertyChanged(nameof(ClipboardFormatDIB)); OnPropertyChanged(nameof(Summary)); }
        }

        public bool ClipboardFormatDIBV5
        {
            get => GetParamBool("ClipboardFormatDIBV5", false);
            set { SetParam("ClipboardFormatDIBV5", value); OnPropertyChanged(nameof(ClipboardFormatDIBV5)); OnPropertyChanged(nameof(Summary)); }
        }

        public bool ClipboardFormatBitmap
        {
            get => GetParamBool("ClipboardFormatBitmap", false);
            set { SetParam("ClipboardFormatBitmap", value); OnPropertyChanged(nameof(ClipboardFormatBitmap)); OnPropertyChanged(nameof(Summary)); }
        }

        public bool ClipboardFormatHTML
        {
            get => GetParamBool("ClipboardFormatHTML", true);
            set { SetParam("ClipboardFormatHTML", value); OnPropertyChanged(nameof(ClipboardFormatHTML)); OnPropertyChanged(nameof(Summary)); }
        }

        public bool ClipboardFormatHTMLDataUrl
        {
            get => GetParamBool("ClipboardFormatHTMLDataUrl", false);
            set { SetParam("ClipboardFormatHTMLDataUrl", value); OnPropertyChanged(nameof(ClipboardFormatHTMLDataUrl)); OnPropertyChanged(nameof(Summary)); }
        }

        public bool ClipboardFormatText
        {
            get => GetParamBool("ClipboardFormatText", false);
            set
            {
                SetParam("ClipboardFormatText", value);
                OnPropertyChanged(nameof(ClipboardFormatText));
                OnPropertyChanged(nameof(IsClipboardTextEnabled));
                OnPropertyChanged(nameof(Summary));
            }
        }

        public string ClipboardCustomText
        {
            get => GetParam("ClipboardCustomText", "${ocr_text}");
            set { SetParam("ClipboardCustomText", value); OnPropertyChanged(nameof(ClipboardCustomText)); }
        }

        // --- 11. Processors / OCR Step ---
        public string ProcessorMode
        {
            get => GetParam("ProcessorMode", "All");
            set
            {
                SetParam("ProcessorMode", value);
                OnPropertyChanged(nameof(ProcessorMode));
                OnPropertyChanged(nameof(IsCustomProcessorsEnabled));
                OnPropertyChanged(nameof(Summary));
            }
        }

        public bool IsCustomProcessorsEnabled => string.Equals(ProcessorMode, "Selected", StringComparison.OrdinalIgnoreCase);

        public string ProcessorTiming
        {
            get => GetParam("Timing", "Any");
            set
            {
                SetParam("Timing", value);
                OnPropertyChanged(nameof(ProcessorTiming));
                OnPropertyChanged(nameof(Summary));
            }
        }

        public bool ProcessorRunOcr
        {
            get => GetParamBool("RunOcr", true);
            set { SetParam("RunOcr", value); OnPropertyChanged(nameof(ProcessorRunOcr)); OnPropertyChanged(nameof(Summary)); }
        }

        public bool ProcessorRunTitleFix
        {
            get => GetParamBool("RunTitleFix", true);
            set { SetParam("RunTitleFix", value); OnPropertyChanged(nameof(ProcessorRunTitleFix)); OnPropertyChanged(nameof(Summary)); }
        }

        public bool ProcessorRunPlugins
        {
            get => GetParamBool("RunPlugins", true);
            set { SetParam("RunPlugins", value); OnPropertyChanged(nameof(ProcessorRunPlugins)); OnPropertyChanged(nameof(Summary)); }
        }

        public string CustomProcessorIdsText
        {
            get
            {
                var list = Config.GetParameter<List<string>>("ProcessorIds");
                return list != null ? string.Join(", ", list) : GetParam("CustomProcessors", "");
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var items = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    SetParam("ProcessorIds", items);
                }
                else
                {
                    Config.Parameters?.Remove("ProcessorIds");
                }
                SetParam("CustomProcessors", value ?? "");
                OnPropertyChanged(nameof(CustomProcessorIdsText));
                OnPropertyChanged(nameof(Summary));
            }
        }

        // --- 12. UserPrompt Step ---
        public string UserPromptTitle
        {
            get => GetParam("Title", "Decision Required");
            set { SetParam("Title", value); OnPropertyChanged(nameof(UserPromptTitle)); OnPropertyChanged(nameof(Summary)); }
        }

        public string UserPromptMessage
        {
            get => GetParam("Message", "Please choose how to proceed:");
            set { SetParam("Message", value); OnPropertyChanged(nameof(UserPromptMessage)); }
        }

        public bool UserPromptShowPreview
        {
            get => GetParamBool("ShowPreview", true);
            set { SetParam("ShowPreview", value); OnPropertyChanged(nameof(UserPromptShowPreview)); }
        }

        public int UserPromptTimeoutSeconds
        {
            get => int.TryParse(GetParam("TimeoutSeconds", "0"), out int t) ? t : 0;
            set { SetParam("TimeoutSeconds", value); OnPropertyChanged(nameof(UserPromptTimeoutSeconds)); }
        }

        public string UserPromptDefaultChoice
        {
            get => GetParam("DefaultChoice", "");
            set { SetParamOrRemoveIfEmpty("DefaultChoice", value); OnPropertyChanged(nameof(UserPromptDefaultChoice)); }
        }

        // --- Configuration Fallback Properties across Steps ---
        public string ScreenCaptureMode
        {
            get => GetParam("ScreenCaptureMode", "");
            set { SetParamOrRemoveIfEmpty("ScreenCaptureMode", value); OnPropertyChanged(nameof(ScreenCaptureMode)); }
        }

        public string WindowCaptureMode
        {
            get => GetParam("WindowCaptureMode", "");
            set { SetParamOrRemoveIfEmpty("WindowCaptureMode", value); OnPropertyChanged(nameof(WindowCaptureMode)); }
        }

        public string AlignDpiMode
        {
            get => GetTriStateParam("AlignDpi");
            set { SetTriStateParam("AlignDpi", value); OnPropertyChanged(nameof(AlignDpiMode)); }
        }

        public string AllowWindowSnappingMode
        {
            get => GetTriStateParam("AllowWindowSnapping");
            set { SetTriStateParam("AllowWindowSnapping", value); OnPropertyChanged(nameof(AllowWindowSnappingMode)); }
        }

        public string OutputFileFormat
        {
            get => GetParam("Format", "");
            set { SetParamOrRemoveIfEmpty("Format", value); OnPropertyChanged(nameof(OutputFileFormat)); OnPropertyChanged(nameof(Summary)); }
        }

        public string OutputFilePromptQuality
        {
            get => GetTriStateParam("PromptQuality");
            set { SetTriStateParam("PromptQuality", value); OnPropertyChanged(nameof(OutputFilePromptQuality)); }
        }

        public string OutputFileAllowOverwrite
        {
            get => GetTriStateParam("AllowOverwrite");
            set { SetTriStateParam("AllowOverwrite", value); OnPropertyChanged(nameof(OutputFileAllowOverwrite)); }
        }

        public string OutputFileCopyPath
        {
            get => GetTriStateParam("CopyPathToClipboard");
            set { SetTriStateParam("CopyPathToClipboard", value); OnPropertyChanged(nameof(OutputFileCopyPath)); }
        }

        public string OutputFileJpegQuality
        {
            get => GetParam("JpegQuality", "");
            set { SetParamOrRemoveIfEmpty("JpegQuality", value); OnPropertyChanged(nameof(OutputFileJpegQuality)); }
        }

        public string EditorMatchSizeToCapture
        {
            get => GetTriStateParam("MatchSizeToCapture");
            set { SetTriStateParam("MatchSizeToCapture", value); OnPropertyChanged(nameof(EditorMatchSizeToCapture)); }
        }

        public string EditorReuseEditor
        {
            get => GetTriStateParam("ReuseEditor");
            set { SetTriStateParam("ReuseEditor", value); OnPropertyChanged(nameof(EditorReuseEditor)); }
        }

        public string EditorSuppressSaveDialog
        {
            get => GetTriStateParam("SuppressSaveDialog");
            set { SetTriStateParam("SuppressSaveDialog", value); OnPropertyChanged(nameof(EditorSuppressSaveDialog)); }
        }

        public string PrinterName
        {
            get => GetParam("PrinterName", "");
            set { SetParamOrRemoveIfEmpty("PrinterName", value); OnPropertyChanged(nameof(PrinterName)); }
        }

        public string PrinterShowPrintDialog
        {
            get => GetTriStateParam("ShowPrintDialog");
            set { SetTriStateParam("ShowPrintDialog", value); OnPropertyChanged(nameof(PrinterShowPrintDialog)); }
        }

        public string PrinterAllowRotate
        {
            get => GetTriStateParam("AllowRotate");
            set { SetTriStateParam("AllowRotate", value); OnPropertyChanged(nameof(PrinterAllowRotate)); }
        }

        public string PrinterAllowEnlarge
        {
            get => GetTriStateParam("AllowEnlarge");
            set { SetTriStateParam("AllowEnlarge", value); OnPropertyChanged(nameof(PrinterAllowEnlarge)); }
        }

        public string PrinterAllowShrink
        {
            get => GetTriStateParam("AllowShrink");
            set { SetTriStateParam("AllowShrink", value); OnPropertyChanged(nameof(PrinterAllowShrink)); }
        }

        public string PrinterCenter
        {
            get => GetTriStateParam("Center");
            set { SetTriStateParam("Center", value); OnPropertyChanged(nameof(PrinterCenter)); }
        }

        public string PrinterColorMode
        {
            get => GetParam("ColorMode", "");
            set { SetParamOrRemoveIfEmpty("ColorMode", value); OnPropertyChanged(nameof(PrinterColorMode)); }
        }

        public string PrinterPrintFooter
        {
            get => GetTriStateParam("PrintFooter");
            set { SetTriStateParam("PrintFooter", value); OnPropertyChanged(nameof(PrinterPrintFooter)); }
        }

        public string PrinterFooterPattern
        {
            get => GetParam("FooterPattern", "");
            set { SetParamOrRemoveIfEmpty("FooterPattern", value); OnPropertyChanged(nameof(PrinterFooterPattern)); }
        }

        public string OcrLanguage
        {
            get => GetParam("OcrLanguage", GetParam("Language", ""));
            set { SetParamOrRemoveIfEmpty("OcrLanguage", value); OnPropertyChanged(nameof(OcrLanguage)); }
        }

        // --- 13. ExternalCommand Step ---
        public string ExternalCommandLine
        {
            get => GetParam("CommandLine", GetParam("Executable", GetParam("Path", "")));
            set { SetParam("CommandLine", value); OnPropertyChanged(nameof(ExternalCommandLine)); OnPropertyChanged(nameof(Summary)); }
        }

        public string ExternalCommandArguments
        {
            get => GetParam("Arguments", GetParam("Argument", GetParam("Args", "{0}")));
            set { SetParam("Arguments", value); OnPropertyChanged(nameof(ExternalCommandArguments)); OnPropertyChanged(nameof(Summary)); }
        }

        public string ExternalCommandFormat
        {
            get => GetParam("Format", GetParam("OutputFormat", "png"));
            set { SetParam("Format", value); OnPropertyChanged(nameof(ExternalCommandFormat)); }
        }

        public bool ExternalCommandRunInBackground
        {
            get => GetParamBool("RunInBackground", GetParamBool("Async", false));
            set { SetParam("RunInBackground", value); OnPropertyChanged(nameof(ExternalCommandRunInBackground)); }
        }

        public bool ExternalCommandOutputToClipboard
        {
            get => GetParamBool("OutputToClipboard", false);
            set { SetParam("OutputToClipboard", value); OnPropertyChanged(nameof(ExternalCommandOutputToClipboard)); }
        }

        public bool ExternalCommandUriToClipboard
        {
            get => GetParamBool("UriToClipboard", false);
            set { SetParam("UriToClipboard", value); OnPropertyChanged(nameof(ExternalCommandUriToClipboard)); }
        }

        public bool ExternalCommandReloadAfterExecution
        {
            get => GetParamBool("ReloadAfterExecution", GetParamBool("UpdatePayload", false));
            set { SetParam("ReloadAfterExecution", value); OnPropertyChanged(nameof(ExternalCommandReloadAfterExecution)); }
        }

        public string ExternalCommandWorkingDirectory
        {
            get => GetParam("WorkingDirectory", GetParam("WorkingDir", ""));
            set { SetParamOrRemoveIfEmpty("WorkingDirectory", value); OnPropertyChanged(nameof(ExternalCommandWorkingDirectory)); }
        }

        public string ExternalCommandSetOutputVariable
        {
            get => GetParam("SetOutputVariable", "");
            set { SetParamOrRemoveIfEmpty("SetOutputVariable", value); OnPropertyChanged(nameof(ExternalCommandSetOutputVariable)); }
        }

        public string ExternalCommandSetExitCodeVariable
        {
            get => GetParam("SetExitCodeVariable", "");
            set { SetParamOrRemoveIfEmpty("SetExitCodeVariable", value); OnPropertyChanged(nameof(ExternalCommandSetExitCodeVariable)); }
        }

        // --- 14. Imgur Step ---
        public string ImgurFormat
        {
            get => GetParam("Format", GetParam("UploadFormat", "png"));
            set { SetParam("Format", value); OnPropertyChanged(nameof(ImgurFormat)); }
        }

        public bool ImgurCopyLinkToClipboard
        {
            get => GetParamBool("CopyLinkToClipboard", true);
            set { SetParam("CopyLinkToClipboard", value); OnPropertyChanged(nameof(ImgurCopyLinkToClipboard)); }
        }

        public bool ImgurOpenInBrowser
        {
            get => GetParamBool("OpenInBrowser", false);
            set { SetParam("OpenInBrowser", value); OnPropertyChanged(nameof(ImgurOpenInBrowser)); }
        }

        // --- 15. Jira Step ---
        public string JiraIssueKey
        {
            get => GetParam("IssueKey", GetParam("Issue", ""));
            set { SetParam("IssueKey", value); OnPropertyChanged(nameof(JiraIssueKey)); OnPropertyChanged(nameof(Summary)); }
        }

        public string JiraFormat
        {
            get => GetParam("Format", GetParam("UploadFormat", "png"));
            set { SetParam("Format", value); OnPropertyChanged(nameof(JiraFormat)); }
        }

        public int JiraJpegQuality
        {
            get => int.TryParse(GetParam("JpegQuality", "80"), out int q) ? q : 80;
            set { SetParam("JpegQuality", value); OnPropertyChanged(nameof(JiraJpegQuality)); }
        }

        // --- 16. Confluence Step ---
        public string ConfluencePageId
        {
            get => GetParam("PageId", GetParam("Page", ""));
            set { SetParam("PageId", value); OnPropertyChanged(nameof(ConfluencePageId)); OnPropertyChanged(nameof(Summary)); }
        }

        public string ConfluenceFormat
        {
            get => GetParam("Format", GetParam("UploadFormat", "png"));
            set { SetParam("Format", value); OnPropertyChanged(nameof(ConfluenceFormat)); }
        }

        public int ConfluenceJpegQuality
        {
            get => int.TryParse(GetParam("JpegQuality", "80"), out int q) ? q : 80;
            set { SetParam("JpegQuality", value); OnPropertyChanged(nameof(ConfluenceJpegQuality)); }
        }

        // --- 17. Office Step ---
        public string OfficeApplication
        {
            get
            {
                string app = GetParam("Application", GetParam("Target", ""));
                if (string.IsNullOrEmpty(app))
                {
                    if (string.Equals(StepType, "Excel", StringComparison.OrdinalIgnoreCase)) return "Excel";
                    if (string.Equals(StepType, "PowerPoint", StringComparison.OrdinalIgnoreCase) || string.Equals(StepType, "Powerpoint", StringComparison.OrdinalIgnoreCase)) return "PowerPoint";
                    if (string.Equals(StepType, "Word", StringComparison.OrdinalIgnoreCase)) return "Word";
                    if (string.Equals(StepType, "OneNote", StringComparison.OrdinalIgnoreCase)) return "OneNote";
                    if (string.Equals(StepType, "Outlook", StringComparison.OrdinalIgnoreCase)) return "Outlook";
                    return "Word";
                }
                return app;
            }
            set { SetParam("Application", value); OnPropertyChanged(nameof(OfficeApplication)); OnPropertyChanged(nameof(Summary)); }
        }

        // --- 18. Zxing Step ---
        public string ZxingSetVariable
        {
            get => GetParam("SetVariable", GetParam("Variable", "barcode_text"));
            set { SetParam("SetVariable", value); OnPropertyChanged(nameof(ZxingSetVariable)); OnPropertyChanged(nameof(Summary)); }
        }

        public bool ZxingCopyToClipboard
        {
            get => GetParamBool("CopyToClipboard", true);
            set { SetParam("CopyToClipboard", value); OnPropertyChanged(nameof(ZxingCopyToClipboard)); }
        }

        private bool HasDestination(string dest)
        {
            if (Config.Parameters == null) return false;
            var list = GetDestinationList();
            return list.Contains(dest, StringComparer.OrdinalIgnoreCase);
        }

        private void ToggleDestination(string dest, bool enabled)
        {
            var list = GetDestinationList();
            if (enabled)
            {
                if (!list.Contains(dest, StringComparer.OrdinalIgnoreCase)) list.Add(dest);
            }
            else
            {
                list.RemoveAll(d => string.Equals(d, dest, StringComparison.OrdinalIgnoreCase));
            }
            SetParam("DestinationDesignations", list);
            NotifyConfigUpdated();
        }

        private List<string> GetDestinationList()
        {
            var list = new List<string>();
            if (Config.Parameters != null)
            {
                if (Config.Parameters.TryGetValue("DestinationDesignations", out var val) && val != null)
                {
                    if (val is IEnumerable<string> strEnum) list.AddRange(strEnum);
                    else if (val is IEnumerable objEnum)
                    {
                        foreach (var item in objEnum) if (item != null) list.Add(item.ToString());
                    }
                }
            }
            return list;
        }

        #endregion

        public string Summary
        {
            get
            {
                switch (StepType)
                {
                    case WellKnownStepTypes.Source:
                        return $"Source: {SourceType}";
                    case WellKnownStepTypes.InteractiveSelection:
                        return $"Mode: {SelectionMode}";
                    case WellKnownStepTypes.Border:
                        return $"Border: {BorderWidth}px {BorderColor}";
                    case WellKnownStepTypes.Effect:
                        if (IsBorderEffect) return $"Border: {EffectBorderWidth}px {EffectBorderColor}";
                        if (IsDropShadowEffect) return $"Shadow: {ShadowSize}px ({ShadowDarkness:0.#})";
                        if (IsTornEdgeEffect) return $"TornEdge ({TornEdges})";
                        if (IsRotateEffect) return $"Rotate: {RotateAngle}°";
                        if (IsResizeEffect) return ResizePercentage != "0" && !string.IsNullOrEmpty(ResizePercentage) ? $"Resize: {ResizePercentage}%" : $"Resize: {ResizeWidth}x{ResizeHeight}";
                        if (IsResizeCanvasEffect) return $"Canvas Padding: {CanvasMargin}px";
                        if (IsMonochromeEffect) return $"Monochrome (Thresh: {MonochromeThreshold})";
                        if (IsAdjustEffect) return $"Adjust (B:{AdjustBrightness} C:{AdjustContrast} G:{AdjustGamma})";
                        if (IsReduceColorsEffect) return $"Reduce Colors ({ReduceColorsCount})";
                        if (IsRemoveTransparencyEffect) return "Remove Transparency";
                        if (IsGrayscaleEffect) return "Grayscale";
                        if (IsInvertEffect) return "Invert Colors";
                        return $"Effect: {EffectType}";
                    case WellKnownStepTypes.TextEffect:
                    case "ObfuscateText":
                        return $"DLP: {TextEffectAction}";
                    case WellKnownStepTypes.Annotation:
                        return Annotations.Count > 0 ? $"{Annotations.Count} item(s): {string.Join(", ", Annotations.Select(d => d.Type).Distinct())}" : "Annotations (0)";
                    case WellKnownStepTypes.SetVariable:
                        return Variables.Count > 0 ? string.Join(", ", Variables.Select(v => $"{v.Key}={v.Value}")) : "Variables (0)";
                    case WellKnownStepTypes.ImmediateFeedback:
                        return PlaySound ? "Sound: Enabled" : "Silent";
                    case WellKnownStepTypes.SaveFile:
                    case "SaveToFile":
                        return !string.IsNullOrWhiteSpace(SaveDirectory) ? $"Save to: {SaveDirectory}" : "Save to File (Default)";
                    case WellKnownStepTypes.Clipboard:
                        if (string.Equals(ClipboardMode, "TextOnly", StringComparison.OrdinalIgnoreCase)) return "Clipboard: OCR Text only";
                        var fmtList = new List<string>();
                        if (ClipboardFormatPNG) fmtList.Add("PNG");
                        if (ClipboardFormatDIB) fmtList.Add("DIB");
                        if (ClipboardFormatDIBV5) fmtList.Add("DIBv5");
                        if (ClipboardFormatBitmap) fmtList.Add("Bitmap");
                        if (ClipboardFormatHTML) fmtList.Add("HTML");
                        if (ClipboardFormatHTMLDataUrl) fmtList.Add("DataURL");
                        if (ClipboardFormatText || string.Equals(ClipboardMode, "ImageAndText", StringComparison.OrdinalIgnoreCase)) fmtList.Add("Text");
                        return fmtList.Count > 0 ? $"Clipboard: {string.Join(", ", fmtList)}" : "Copy to Clipboard";
                    case WellKnownStepTypes.Editor:
                        return "Open in Image Editor";
                    case WellKnownStepTypes.Printer:
                        return "Send to Printer";
                    case WellKnownStepTypes.Email:
                        return "Send via E-Mail";
                    case WellKnownStepTypes.CustomDestination:
                        return !string.IsNullOrWhiteSpace(CustomDestinationId) ? $"Custom: {CustomDestinationId}" : "Custom Destination";
                    case WellKnownStepTypes.Destinations:
                        var dests = GetDestinationList();
                        return dests.Count > 0 ? $"To: {string.Join(", ", dests)}" : "Export targets";
                    case WellKnownStepTypes.Notification:
                        return $"Toast: {NotificationTitle}";
                    case WellKnownStepTypes.Conditional:
                        return $"{ConditionBranches.Count} decision branch(es)";
                    case WellKnownStepTypes.UserPrompt:
                    case "PromptChoice":
                        return PromptChoices.Count > 0 ? $"Prompt: {string.Join(", ", PromptChoices.Select(c => c.Label))}" : "User Decision Prompt";
                    case WellKnownStepTypes.Processors:
                        if (string.Equals(ProcessorMode, "OCR", StringComparison.OrdinalIgnoreCase)) return "Processors: Windows OCR";
                        if (string.Equals(ProcessorMode, "Selected", StringComparison.OrdinalIgnoreCase))
                        {
                            var active = new List<string>();
                            if (ProcessorRunOcr) active.Add("OCR");
                            if (ProcessorRunTitleFix) active.Add("TitleFix");
                            if (ProcessorRunPlugins) active.Add("Plugins");
                            return active.Count > 0 ? $"Processors: {string.Join(", ", active)}" : "Processors (None)";
                        }
                        return ProcessorTiming != "Any" && !string.IsNullOrEmpty(ProcessorTiming) ? $"Processors: All ({ProcessorTiming})" : "Processors: All Active";
                    case var _ when IsExternalCommand:
                        return !string.IsNullOrWhiteSpace(ExternalCommandLine) ? $"Run: {System.IO.Path.GetFileName(ExternalCommandLine)} {ExternalCommandArguments}".Trim() : "Execute Command";
                    case var _ when IsImgur:
                        return "Upload to Imgur";
                    case var _ when IsJira:
                        return !string.IsNullOrWhiteSpace(JiraIssueKey) ? $"Attach to Jira: {JiraIssueKey}" : "Attach to Jira Issue";
                    case var _ when IsConfluence:
                        return !string.IsNullOrWhiteSpace(ConfluencePageId) ? $"Upload to Confluence: {ConfluencePageId}" : "Upload to Confluence Page";
                    case var _ when IsOffice:
                        return $"Export to Microsoft {OfficeApplication}";
                    case var _ when IsZxing:
                        return "Scan Barcode / QR Code";
                    case var _ when IsBox:
                        return "Upload to Box";
                    case var _ when IsDropbox:
                        return "Upload to Dropbox";
                    default:
                        return StepType;
                }
            }
        }

        public void NotifyConfigUpdated()
        {
            if (IsConditional)
            {
                LoadConditionBranches();
            }
            else if (IsUserPrompt)
            {
                LoadPromptChoices();
            }
            OnPropertyChanged(nameof(Summary));
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(IsExternalCommand));
            OnPropertyChanged(nameof(IsImgur));
            OnPropertyChanged(nameof(IsJira));
            OnPropertyChanged(nameof(IsConfluence));
            OnPropertyChanged(nameof(IsOffice));
            OnPropertyChanged(nameof(IsZxing));
            OnPropertyChanged(nameof(IsBox));
            OnPropertyChanged(nameof(IsDropbox));
            OnPropertyChanged(nameof(IsCloudStorage));
            OnPropertyChanged(nameof(SourceType));
            OnPropertyChanged(nameof(IsWindowSource));
            OnPropertyChanged(nameof(WindowTitlePattern));
            OnPropertyChanged(nameof(ProcessName));
            OnPropertyChanged(nameof(CaptureMouseCursorMode));
            OnPropertyChanged(nameof(DelayMs));
            OnPropertyChanged(nameof(SelectionMode));
            OnPropertyChanged(nameof(AllowWindowSnapping));
            OnPropertyChanged(nameof(BorderWidth));
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(EffectType));
            OnPropertyChanged(nameof(IsDropShadowEffect));
            OnPropertyChanged(nameof(IsBorderEffect));
            OnPropertyChanged(nameof(ShadowSize));
            OnPropertyChanged(nameof(ShadowDarkness));
            OnPropertyChanged(nameof(TextEffectAction));
            OnPropertyChanged(nameof(TextEffectFillColor));
            OnPropertyChanged(nameof(TextEffectPattern));
            OnPropertyChanged(nameof(ConditionBranches));
            OnPropertyChanged(nameof(PromptChoices));
            OnPropertyChanged(nameof(UserPromptTitle));
            OnPropertyChanged(nameof(UserPromptMessage));
            OnPropertyChanged(nameof(UserPromptShowPreview));
            OnPropertyChanged(nameof(UserPromptTimeoutSeconds));
            OnPropertyChanged(nameof(UserPromptDefaultChoice));
            OnPropertyChanged(nameof(PlaySound));
            OnPropertyChanged(nameof(SoundFilePath));
            OnPropertyChanged(nameof(ScreenCaptureMode));
            OnPropertyChanged(nameof(WindowCaptureMode));
            OnPropertyChanged(nameof(AlignDpiMode));
            OnPropertyChanged(nameof(AllowWindowSnappingMode));
            OnPropertyChanged(nameof(OutputFileFormat));
            OnPropertyChanged(nameof(OutputFilePromptQuality));
            OnPropertyChanged(nameof(OutputFileAllowOverwrite));
            OnPropertyChanged(nameof(OutputFileCopyPath));
            OnPropertyChanged(nameof(OutputFileJpegQuality));
            OnPropertyChanged(nameof(EditorMatchSizeToCapture));
            OnPropertyChanged(nameof(EditorReuseEditor));
            OnPropertyChanged(nameof(EditorSuppressSaveDialog));
            OnPropertyChanged(nameof(PrinterName));
            OnPropertyChanged(nameof(PrinterShowPrintDialog));
            OnPropertyChanged(nameof(PrinterAllowRotate));
            OnPropertyChanged(nameof(PrinterAllowEnlarge));
            OnPropertyChanged(nameof(PrinterAllowShrink));
            OnPropertyChanged(nameof(PrinterCenter));
            OnPropertyChanged(nameof(PrinterColorMode));
            OnPropertyChanged(nameof(PrinterPrintFooter));
            OnPropertyChanged(nameof(PrinterFooterPattern));
            OnPropertyChanged(nameof(OcrLanguage));
            OnPropertyChanged(nameof(ExternalCommandLine));
            OnPropertyChanged(nameof(ExternalCommandArguments));
            OnPropertyChanged(nameof(ExternalCommandFormat));
            OnPropertyChanged(nameof(ExternalCommandRunInBackground));
            OnPropertyChanged(nameof(ExternalCommandOutputToClipboard));
            OnPropertyChanged(nameof(ExternalCommandUriToClipboard));
            OnPropertyChanged(nameof(ExternalCommandReloadAfterExecution));
            OnPropertyChanged(nameof(ExternalCommandWorkingDirectory));
            OnPropertyChanged(nameof(ExternalCommandSetOutputVariable));
            OnPropertyChanged(nameof(ExternalCommandSetExitCodeVariable));
            OnPropertyChanged(nameof(ImgurFormat));
            OnPropertyChanged(nameof(ImgurCopyLinkToClipboard));
            OnPropertyChanged(nameof(ImgurOpenInBrowser));
            OnPropertyChanged(nameof(JiraIssueKey));
            OnPropertyChanged(nameof(JiraFormat));
            OnPropertyChanged(nameof(JiraJpegQuality));
            OnPropertyChanged(nameof(ConfluencePageId));
            OnPropertyChanged(nameof(ConfluenceFormat));
            OnPropertyChanged(nameof(ConfluenceJpegQuality));
            OnPropertyChanged(nameof(OfficeApplication));
            OnPropertyChanged(nameof(ZxingSetVariable));
            OnPropertyChanged(nameof(ZxingCopyToClipboard));
            OnPropertyChanged(nameof(NotificationTitle));
            OnPropertyChanged(nameof(NotificationMessage));
            OnPropertyChanged(nameof(PlaySound));
            OnPropertyChanged(nameof(DestinationEditor));
            OnPropertyChanged(nameof(DestinationClipboard));
            OnPropertyChanged(nameof(DestinationFile));
            OnPropertyChanged(nameof(DestinationPrinter));
            OnPropertyChanged(nameof(DestinationEmail));
            OnPropertyChanged(nameof(DestinationOcr));
            OnPropertyChanged(nameof(SaveDirectory));
            OnPropertyChanged(nameof(FilenamePattern));
            OnPropertyChanged(nameof(ImageFormat));
            OnPropertyChanged(nameof(AllowOverwrite));
            OnPropertyChanged(nameof(CustomDestinationsText));
            OnPropertyChanged(nameof(CustomDestinationId));
            OnPropertyChanged(nameof(ShowPrintDialog));
            OnPropertyChanged(nameof(EmailSubject));
            OnPropertyChanged(nameof(EmailRecipient));
            OnPropertyChanged(nameof(ClipboardMode));
            OnPropertyChanged(nameof(IsClipboardTextEnabled));
            OnPropertyChanged(nameof(ClipboardFormatPNG));
            OnPropertyChanged(nameof(ClipboardFormatDIB));
            OnPropertyChanged(nameof(ClipboardFormatDIBV5));
            OnPropertyChanged(nameof(ClipboardFormatBitmap));
            OnPropertyChanged(nameof(ClipboardFormatHTML));
            OnPropertyChanged(nameof(ClipboardFormatHTMLDataUrl));
            OnPropertyChanged(nameof(ClipboardFormatText));
            OnPropertyChanged(nameof(ClipboardCustomText));
            OnPropertyChanged(nameof(ProcessorMode));
            OnPropertyChanged(nameof(IsCustomProcessorsEnabled));
            OnPropertyChanged(nameof(ProcessorTiming));
            OnPropertyChanged(nameof(ProcessorRunOcr));
            OnPropertyChanged(nameof(ProcessorRunTitleFix));
            OnPropertyChanged(nameof(ProcessorRunPlugins));
            OnPropertyChanged(nameof(CustomProcessorIdsText));
        }

        public string GetParam(string key, string fallback = "")
        {
            if (Config?.Parameters != null)
            {
                foreach (var kvp in Config.Parameters)
                {
                    if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase) && kvp.Value != null)
                    {
                        return kvp.Value.ToString();
                    }
                }
            }
            return fallback;
        }

        public bool GetParamBool(string key, bool fallback = false)
        {
            if (Config?.Parameters != null)
            {
                foreach (var kvp in Config.Parameters)
                {
                    if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase) && kvp.Value != null)
                    {
                        if (kvp.Value is bool b) return b;
                        if (bool.TryParse(kvp.Value.ToString(), out bool pb)) return pb;
                    }
                }
            }
            return fallback;
        }

        public void SetParam(string key, object value)
        {
            if (Config.Parameters == null)
            {
                Config.Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            Config.Parameters[key] = value;
            NotifyConfigUpdated();
        }

        public string GetTriStateParam(string key)
        {
            if (Config.Parameters != null && Config.Parameters.TryGetValue(key, out var val) && val != null)
            {
                if (val is bool b) return b ? "true" : "false";
                if (bool.TryParse(val.ToString(), out bool pb)) return pb ? "true" : "false";
            }
            return "";
        }

        public void SetTriStateParam(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Config.Parameters?.Remove(key);
            }
            else
            {
                SetParam(key, value == "true");
            }
            NotifyConfigUpdated();
        }

        public void SetParamOrRemoveIfEmpty(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Config.Parameters?.Remove(key);
            }
            else
            {
                SetParam(key, value.Trim());
            }
            NotifyConfigUpdated();
        }
    }
}
