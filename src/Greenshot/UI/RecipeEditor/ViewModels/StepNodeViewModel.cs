using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Greenshot.Base.Recipes;
using Newtonsoft.Json.Linq;

namespace Greenshot.UI.RecipeEditor.ViewModels
{
    public class StepPortViewModel : ViewModelBase
    {
        private Point _anchor;
        private bool _isConnected;

        public StepNodeViewModel Node { get; }
        public bool IsInput { get; }

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

        public string Title => IsInput ? "In" : "Out";

        public StepPortViewModel(StepNodeViewModel node, bool isInput)
        {
            Node = node;
            IsInput = isInput;
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
        private string _tailDirection = "BottomLeft";
        private string _tailOffsetX = "0";
        private string _tailOffsetY = "0";
        private readonly Action _onChanged;

        public string Type
        {
            get => _type;
            set
            {
                if (SetField(ref _type, value))
                {
                    _onChanged?.Invoke();
                    OnPropertyChanged(nameof(IsTextType));
                    OnPropertyChanged(nameof(IsEmojiType));
                    OnPropertyChanged(nameof(IsSpeechbubbleType));
                }
            }
        }

        public bool IsTextType => string.Equals(_type, "Text", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(_type, "Speechbubble", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(_type, "StepLabel", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(_type, "Counter", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(_type, "Watermark", StringComparison.OrdinalIgnoreCase);

        public bool IsEmojiType => string.Equals(_type, "Emoji", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(_type, "Icon", StringComparison.OrdinalIgnoreCase);

        public bool IsSpeechbubbleType => string.Equals(_type, "Speechbubble", StringComparison.OrdinalIgnoreCase);

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
            set { if (SetField(ref _text, value)) _onChanged?.Invoke(); }
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

        public string Width
        {
            get => _width;
            set { if (SetField(ref _width, value)) _onChanged?.Invoke(); }
        }

        public string Height
        {
            get => _height;
            set { if (SetField(ref _height, value)) _onChanged?.Invoke(); }
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

        public ICommand RemoveCommand { get; }
        public ICommand PickFillColorCommand { get; }
        public ICommand PickLineColorCommand { get; }

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
        }

        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Type"] = Type,
                ["Width"] = Width,
                ["Height"] = Height,
                ["OffsetX"] = OffsetX,
                ["OffsetY"] = OffsetY,
                ["FillColor"] = FillColor,
                ["LineColor"] = LineColor,
                ["LineThickness"] = LineThickness
            };
            if (!string.IsNullOrEmpty(HorizontalAnchor) && HorizontalAnchor != "None") dict["HorizontalAnchor"] = HorizontalAnchor;
            if (!string.IsNullOrEmpty(VerticalAnchor) && VerticalAnchor != "None") dict["VerticalAnchor"] = VerticalAnchor;
            if (IsTextType) dict["Text"] = Text;
            if (IsEmojiType) dict["Emoji"] = Emoji;
            if (IsSpeechbubbleType)
            {
                dict["TailDirection"] = TailDirection;
                if (!string.IsNullOrEmpty(TailOffsetX) && TailOffsetX != "0") dict["TailOffsetX"] = TailOffsetX;
                if (!string.IsNullOrEmpty(TailOffsetY) && TailOffsetY != "0") dict["TailOffsetY"] = TailOffsetY;
            }
            return dict;
        }

        public static DrawableItemViewModel FromDictionary(IDictionary dict, Action onChanged, Action<DrawableItemViewModel> onRemove)
        {
            var item = new DrawableItemViewModel(onChanged, onRemove);
            if (dict == null) return item;

            string GetKey(params string[] possibleKeys)
            {
                foreach (var pk in possibleKeys)
                {
                    foreach (var k in dict.Keys)
                    {
                        if (string.Equals(k?.ToString(), pk, StringComparison.OrdinalIgnoreCase))
                        {
                            var v = dict[k];
                            if (v != null) return v.ToString();
                        }
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
                else if (string.Equals(typeVal, "Arrow", StringComparison.OrdinalIgnoreCase)) item._type = "Arrow";
                else if (string.Equals(typeVal, "Speechbubble", StringComparison.OrdinalIgnoreCase)) item._type = "Speechbubble";
                else if (string.Equals(typeVal, "Blur", StringComparison.OrdinalIgnoreCase)) item._type = "Blur";
                else if (string.Equals(typeVal, "Highlight", StringComparison.OrdinalIgnoreCase)) item._type = "Highlight";
                else item._type = typeVal;
            }
            else
            {
                item._type = "Rectangle";
            }

            string txt = GetKey("Text", "Content", "Label", "Number", "Watermark_Text", "WatermarkText");
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

            string lt = GetKey("LineThickness", "Thickness", "StrokeThickness", "BorderWidth");
            if (lt != null && int.TryParse(lt, out int it)) item._lineThickness = it;

            string td = GetKey("TailDirection", "TailPosition", "Tail", "TailDir");
            if (td != null) item._tailDirection = td;

            string tox = GetKey("TailOffsetX", "TailOffset_X", "Tail_OffsetX");
            if (tox != null) item._tailOffsetX = tox;

            string toy = GetKey("TailOffsetY", "TailOffset_Y", "Tail_OffsetY");
            if (toy != null) item._tailOffsetY = toy;

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

        public string Id => Config.Id;
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
            set => SetField(ref _isStartNode, value);
        }

        public bool IsInCycle
        {
            get => _isInCycle;
            set => SetField(ref _isInCycle, value);
        }

        public StepPortViewModel InputPort { get; }
        public StepPortViewModel OutputPort { get; }

        public ObservableCollection<StepPortViewModel> Input { get; } = new ObservableCollection<StepPortViewModel>();
        public ObservableCollection<StepPortViewModel> Output { get; } = new ObservableCollection<StepPortViewModel>();

        public ObservableCollection<VariableItemViewModel> Variables { get; } = new ObservableCollection<VariableItemViewModel>();
        public ObservableCollection<DrawableItemViewModel> Drawables { get; } = new ObservableCollection<DrawableItemViewModel>();

        public ICommand AddVariableCommand { get; }
        public ICommand AddDrawableCommand { get; }
        public ICommand BrowseSaveDirectoryCommand { get; }
        public ICommand PickBorderColorCommand { get; }
        public ICommand PickTextEffectColorCommand { get; }
        public ICommand PickCanvasBackgroundColorCommand { get; }
        public ICommand PickTransparencyBackgroundColorCommand { get; }

        public Action<StepNodeViewModel> OnSetStartNode { get; set; }
        public ICommand SetAsStartNodeCommand { get; }
        public Action<StepNodeViewModel> OnDeleteNode { get; set; }
        public ICommand DeleteNodeCommand { get; }

        public StepNodeViewModel(RecipeNodeConfig config, Point initialLocation, Action<StepNodeViewModel> onSetStartNode = null, Action<StepNodeViewModel> onDeleteNode = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _name = config.Name;
            _location = initialLocation;
            OnSetStartNode = onSetStartNode;
            OnDeleteNode = onDeleteNode;
            SetAsStartNodeCommand = new RelayCommand(() => OnSetStartNode?.Invoke(this));
            DeleteNodeCommand = new RelayCommand(() => OnDeleteNode?.Invoke(this));

            InputPort = new StepPortViewModel(this, isInput: true);
            OutputPort = new StepPortViewModel(this, isInput: false);
            Input.Add(InputPort);
            Output.Add(OutputPort);

            AddVariableCommand = new RelayCommand(() => AddVariable("new_var", "${user.username}"));
            AddDrawableCommand = new RelayCommand(p => AddDrawable((p as string) ?? "Rectangle"));
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
            LoadDrawablesFromConfig();
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

        public void LoadDrawablesFromConfig()
        {
            Drawables.Clear();
            if (Config.Parameters != null)
            {
                object dObj = null;
                foreach (var kvp in Config.Parameters)
                {
                    if (string.Equals(kvp.Key, "Drawables", StringComparison.OrdinalIgnoreCase))
                    {
                        dObj = kvp.Value;
                        break;
                    }
                }

                if (dObj != null)
                {
                    if (dObj is IEnumerable enumerable && !(dObj is string))
                    {
                        foreach (var elem in enumerable)
                        {
                            if (elem is IDictionary d)
                            {
                                Drawables.Add(DrawableItemViewModel.FromDictionary(d, SyncDrawablesToConfig, RemoveDrawable));
                            }
                            else if (elem is JObject jObj)
                            {
                                var jDict = jObj.ToObject<Dictionary<string, object>>();
                                Drawables.Add(DrawableItemViewModel.FromDictionary(jDict, SyncDrawablesToConfig, RemoveDrawable));
                            }
                        }
                    }
                }
                else if (Config.Parameters.Keys.Any(k => string.Equals(k, "DrawableType", StringComparison.OrdinalIgnoreCase) ||
                                                         string.Equals(k, "Shape", StringComparison.OrdinalIgnoreCase) ||
                                                         string.Equals(k, "Type", StringComparison.OrdinalIgnoreCase)))
                {
                    Drawables.Add(DrawableItemViewModel.FromDictionary(Config.Parameters, SyncDrawablesToConfig, RemoveDrawable));
                }
            }
        }

        public void SyncDrawablesToConfig()
        {
            if (Config.Parameters == null)
            {
                Config.Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            Config.Parameters["Drawables"] = Drawables.Select(d => d.ToDictionary()).ToList();
            NotifyConfigUpdated();
        }

        public void AddDrawable(string type = "Rectangle")
        {
            var item = new DrawableItemViewModel(SyncDrawablesToConfig, RemoveDrawable)
            {
                Type = type
            };
            if (string.Equals(type, "Text", StringComparison.OrdinalIgnoreCase))
            {
                item.Text = "Captured: ${now:yyyy-MM-dd}";
                item.Width = "220";
                item.Height = "32";
                item.FillColor = "rgba(0,0,0,160)";
                item.LineColor = "#0078D7";
            }
            else if (string.Equals(type, "StepLabel", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "Counter", StringComparison.OrdinalIgnoreCase))
            {
                item.Text = "1";
                item.Width = "28";
                item.Height = "28";
                item.FillColor = "#E81123";
                item.LineColor = "#FFFFFF";
            }
            else if (string.Equals(type, "Emoji", StringComparison.OrdinalIgnoreCase))
            {
                item.Emoji = "🛡️";
                item.Width = "32";
                item.Height = "32";
            }
            Drawables.Add(item);
            SyncDrawablesToConfig();
        }

        public void RemoveDrawable(DrawableItemViewModel item)
        {
            if (item != null && Drawables.Contains(item))
            {
                Drawables.Remove(item);
                SyncDrawablesToConfig();
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
        public string ConditionExpression
        {
            get => GetParam("Condition", "${payload.width > 800}");
            set => SetParam("Condition", value);
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
                    case WellKnownStepTypes.Drawable:
                        return Drawables.Count > 0 ? $"{Drawables.Count} item(s): {string.Join(", ", Drawables.Select(d => d.Type).Distinct())}" : "Drawables (0)";
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
                        return $"If: {ConditionExpression}";
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
                    default:
                        return StepType;
                }
            }
        }

        public void NotifyConfigUpdated()
        {
            OnPropertyChanged(nameof(Summary));
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(Name));
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
            OnPropertyChanged(nameof(ConditionExpression));
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
    }
}
