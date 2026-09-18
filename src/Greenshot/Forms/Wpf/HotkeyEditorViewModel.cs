using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Dapplo.Windows.Input.Enums;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using Greenshot.UI.RecipeEditor.ViewModels;

namespace Greenshot.Forms.Wpf
{
    public class HotkeyEditorViewModel : INotifyPropertyChanged
    {
        private string _actionTitle;
        private bool _ctrl;
        private ModifierLocation _ctrlLocation = ModifierLocation.Any;
        private bool _alt;
        private ModifierLocation _altLocation = ModifierLocation.Any;
        private bool _shift;
        private ModifierLocation _shiftLocation = ModifierLocation.Any;
        private bool _win;
        private ModifierLocation _winLocation = ModifierLocation.Any;

        private VirtualKeyCode _triggerKey = VirtualKeyCode.None;
        private string _validationError;
        private bool _isValid;
        private bool _isCapturing;
        private int _currentStepIndex;
        private bool _isSyncing;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Saved;
        public event EventHandler Cancelled;

        public string ActionTitle
        {
            get => _actionTitle;
            set { _actionTitle = value; OnPropertyChanged(); }
        }

        public bool Ctrl
        {
            get => _ctrl;
            set
            {
                if (_ctrl != value)
                {
                    _ctrl = value;
                    OnPropertyChanged();
                    SyncCurrentToStep();
                    Validate();
                }
            }
        }

        public ModifierLocation CtrlLocation
        {
            get => _ctrlLocation;
            set
            {
                if (_ctrlLocation != value)
                {
                    _ctrlLocation = value;
                    OnPropertyChanged();
                    SyncCurrentToStep();
                    Validate();
                }
            }
        }

        public bool Alt
        {
            get => _alt;
            set
            {
                if (_alt != value)
                {
                    _alt = value;
                    OnPropertyChanged();
                    SyncCurrentToStep();
                    Validate();
                }
            }
        }

        public ModifierLocation AltLocation
        {
            get => _altLocation;
            set
            {
                if (_altLocation != value)
                {
                    _altLocation = value;
                    OnPropertyChanged();
                    SyncCurrentToStep();
                    Validate();
                }
            }
        }

        public bool Shift
        {
            get => _shift;
            set
            {
                if (_shift != value)
                {
                    _shift = value;
                    OnPropertyChanged();
                    SyncCurrentToStep();
                    Validate();
                }
            }
        }

        public ModifierLocation ShiftLocation
        {
            get => _shiftLocation;
            set
            {
                if (_shiftLocation != value)
                {
                    _shiftLocation = value;
                    OnPropertyChanged();
                    SyncCurrentToStep();
                    Validate();
                }
            }
        }

        public bool Win
        {
            get => _win;
            set
            {
                if (_win != value)
                {
                    _win = value;
                    OnPropertyChanged();
                    SyncCurrentToStep();
                    Validate();
                }
            }
        }

        public ModifierLocation WinLocation
        {
            get => _winLocation;
            set
            {
                if (_winLocation != value)
                {
                    _winLocation = value;
                    OnPropertyChanged();
                    SyncCurrentToStep();
                    Validate();
                }
            }
        }

        public VirtualKeyCode TriggerKey
        {
            get => _triggerKey;
            set
            {
                if (_triggerKey != value)
                {
                    _triggerKey = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TriggerKeyDisplay));
                    SyncCurrentToStep();
                    Validate();
                }
            }
        }

        public string TriggerKeyDisplay => _triggerKey == VirtualKeyCode.None ? "Click here and press key" : KeyChord.FormatKeyName(_triggerKey);

        public ObservableCollection<KeyChord> Steps { get; } = new ObservableCollection<KeyChord>();

        /// <summary>
        /// Backward compatibility alias for Steps.
        /// </summary>
        public ObservableCollection<KeyChord> Chords => Steps;

        public ObservableCollection<ChordStepItem> StepItems { get; } = new ObservableCollection<ChordStepItem>();

        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            set
            {
                if (_currentStepIndex != value)
                {
                    SelectStep(value);
                }
            }
        }

        public bool HasMultipleChords => Steps.Count > 1;

        public string ValidationError
        {
            get => _validationError;
            set { _validationError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasValidationError)); }
        }

        public bool HasValidationError => !string.IsNullOrEmpty(_validationError);

        public bool IsValid
        {
            get => _isValid;
            set { _isValid = value; OnPropertyChanged(); }
        }

        public bool IsCapturing
        {
            get => _isCapturing;
            set { _isCapturing = value; OnPropertyChanged(); }
        }

        public IReadOnlyList<ModifierLocation> AvailableLocations { get; } = new[]
        {
            ModifierLocation.Any,
            ModifierLocation.Left,
            ModifierLocation.Right
        };

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand AddStepCommand { get; }
        public ICommand AddChordCommand => AddStepCommand;
        public ICommand ResetToChord1Command { get; }

        public HotkeyEditorViewModel()
        {
            SaveCommand = new RelayCommand(Save, () => IsValid);
            CancelCommand = new RelayCommand(Cancel);
            ClearCommand = new RelayCommand(Clear);
            AddStepCommand = new RelayCommand(AddStep, CanAddStep);
            ResetToChord1Command = new RelayCommand(ResetToSingleChord);

            Steps.Add(new KeyChord());
            _currentStepIndex = 0;
            RefreshStepItems();
            Validate();
        }

        public void LoadFromSequenceString(string actionName, string hotkeyString)
        {
            ActionTitle = actionName;
            _isSyncing = true;
            try
            {
                Steps.Clear();
                var seq = HotkeySequence.Parse(hotkeyString);
                if (!seq.IsEmpty)
                {
                    foreach (var chord in seq.Chords)
                    {
                        Steps.Add(chord);
                    }
                    _currentStepIndex = 0;
                    LoadChordIntoCurrent(Steps[0]);
                }
                else
                {
                    Steps.Add(new KeyChord());
                    _currentStepIndex = 0;
                    ClearCurrentFields();
                }
            }
            finally
            {
                _isSyncing = false;
            }

            OnPropertyChanged(nameof(HasMultipleChords));
            RefreshStepItems();
            Validate();
        }

        public void SelectStep(int index)
        {
            if (index < 0 || index >= Steps.Count) return;

            _currentStepIndex = index;
            OnPropertyChanged(nameof(CurrentStepIndex));

            _isSyncing = true;
            try
            {
                LoadChordIntoCurrent(Steps[index]);
            }
            finally
            {
                _isSyncing = false;
            }

            RefreshStepItems();
            Validate();
        }

        public void AddStep()
        {
            if (!CanAddStep()) return;

            var newChord = new KeyChord();
            Steps.Add(newChord);
            _currentStepIndex = Steps.Count - 1;
            OnPropertyChanged(nameof(CurrentStepIndex));
            OnPropertyChanged(nameof(HasMultipleChords));

            _isSyncing = true;
            try
            {
                ClearCurrentFields();
            }
            finally
            {
                _isSyncing = false;
            }

            RefreshStepItems();
            Validate();
        }

        public void RemoveStep(int index)
        {
            if (index < 0 || index >= Steps.Count || Steps.Count <= 1) return;

            Steps.RemoveAt(index);
            if (_currentStepIndex >= Steps.Count)
            {
                _currentStepIndex = Steps.Count - 1;
            }
            OnPropertyChanged(nameof(CurrentStepIndex));
            OnPropertyChanged(nameof(HasMultipleChords));

            _isSyncing = true;
            try
            {
                LoadChordIntoCurrent(Steps[_currentStepIndex]);
            }
            finally
            {
                _isSyncing = false;
            }

            RefreshStepItems();
            Validate();
        }

        public void ResetToSingleChord()
        {
            if (Steps.Count > 1)
            {
                var first = Steps[0];
                Steps.Clear();
                Steps.Add(first);
                _currentStepIndex = 0;
                OnPropertyChanged(nameof(CurrentStepIndex));
                OnPropertyChanged(nameof(HasMultipleChords));
                LoadChordIntoCurrent(first);
                RefreshStepItems();
                Validate();
            }
        }

        private bool CanAddStep()
        {
            if (TriggerKey == VirtualKeyCode.None) return false;
            var seq = BuildSequence();
            return seq.Validate(out _);
        }

        private void SyncCurrentToStep()
        {
            if (_isSyncing) return;
            if (_currentStepIndex >= 0 && _currentStepIndex < Steps.Count)
            {
                Steps[_currentStepIndex] = GetCurrentChord();
                RefreshStepItems();
            }
        }

        private void LoadChordIntoCurrent(KeyChord chord)
        {
            _ctrl = chord.Ctrl;
            _ctrlLocation = chord.CtrlLocation;
            _alt = chord.Alt;
            _altLocation = chord.AltLocation;
            _shift = chord.Shift;
            _shiftLocation = chord.ShiftLocation;
            _win = chord.Win;
            _winLocation = chord.WinLocation;
            _triggerKey = chord.Key;

            OnPropertyChanged(nameof(Ctrl));
            OnPropertyChanged(nameof(CtrlLocation));
            OnPropertyChanged(nameof(Alt));
            OnPropertyChanged(nameof(AltLocation));
            OnPropertyChanged(nameof(Shift));
            OnPropertyChanged(nameof(ShiftLocation));
            OnPropertyChanged(nameof(Win));
            OnPropertyChanged(nameof(WinLocation));
            OnPropertyChanged(nameof(TriggerKey));
            OnPropertyChanged(nameof(TriggerKeyDisplay));
        }

        private void ClearCurrentFields()
        {
            _ctrl = false;
            _ctrlLocation = ModifierLocation.Any;
            _alt = false;
            _altLocation = ModifierLocation.Any;
            _shift = false;
            _shiftLocation = ModifierLocation.Any;
            _win = false;
            _winLocation = ModifierLocation.Any;
            _triggerKey = VirtualKeyCode.None;

            OnPropertyChanged(nameof(Ctrl));
            OnPropertyChanged(nameof(CtrlLocation));
            OnPropertyChanged(nameof(Alt));
            OnPropertyChanged(nameof(AltLocation));
            OnPropertyChanged(nameof(Shift));
            OnPropertyChanged(nameof(ShiftLocation));
            OnPropertyChanged(nameof(Win));
            OnPropertyChanged(nameof(WinLocation));
            OnPropertyChanged(nameof(TriggerKey));
            OnPropertyChanged(nameof(TriggerKeyDisplay));
        }

        public HotkeySequence BuildSequence()
        {
            var validChords = Steps.Where(c => c.Key != VirtualKeyCode.None).ToList();
            return new HotkeySequence(validChords);
        }

        public KeyChord GetCurrentChord()
        {
            return new KeyChord
            {
                Ctrl = Ctrl,
                CtrlLocation = CtrlLocation,
                Alt = Alt,
                AltLocation = AltLocation,
                Shift = Shift,
                ShiftLocation = ShiftLocation,
                Win = Win,
                WinLocation = WinLocation,
                Key = TriggerKey
            };
        }

        public void SetCapturedKey(VirtualKeyCode vk, bool ctrl, bool alt, bool shift, bool win,
            bool isLeftCtrl, bool isRightCtrl, bool isLeftAlt, bool isRightAlt, bool isLeftShift, bool isRightShift, bool isLeftWin, bool isRightWin)
        {
            _triggerKey = vk;
            OnPropertyChanged(nameof(TriggerKey));
            OnPropertyChanged(nameof(TriggerKeyDisplay));

            if (ctrl)
            {
                _ctrl = true;
                OnPropertyChanged(nameof(Ctrl));
                if (isLeftCtrl && !isRightCtrl && CtrlLocation != ModifierLocation.Any) CtrlLocation = ModifierLocation.Left;
                else if (isRightCtrl && !isLeftCtrl && CtrlLocation != ModifierLocation.Any) CtrlLocation = ModifierLocation.Right;
            }

            if (alt)
            {
                _alt = true;
                OnPropertyChanged(nameof(Alt));
                if (isLeftAlt && !isRightAlt && AltLocation != ModifierLocation.Any) AltLocation = ModifierLocation.Left;
                else if (isRightAlt && !isLeftAlt && AltLocation != ModifierLocation.Any) AltLocation = ModifierLocation.Right;
            }

            if (shift)
            {
                _shift = true;
                OnPropertyChanged(nameof(Shift));
                if (isLeftShift && !isRightShift && ShiftLocation != ModifierLocation.Any) ShiftLocation = ModifierLocation.Left;
                else if (isRightShift && !isLeftShift && ShiftLocation != ModifierLocation.Any) ShiftLocation = ModifierLocation.Right;
            }

            if (win)
            {
                _win = true;
                OnPropertyChanged(nameof(Win));
                if (isLeftWin && !isRightWin && WinLocation != ModifierLocation.Any) WinLocation = ModifierLocation.Left;
                else if (isRightWin && !isLeftWin && WinLocation != ModifierLocation.Any) WinLocation = ModifierLocation.Right;
            }

            SyncCurrentToStep();
            Validate();
        }

        public void UpdateModifiersFromKeyboard(bool ctrl, bool alt, bool shift, bool win,
            bool isLeftCtrl, bool isRightCtrl, bool isLeftAlt, bool isRightAlt, bool isLeftShift, bool isRightShift, bool isLeftWin, bool isRightWin)
        {
            if (ctrl)
            {
                _ctrl = true;
                OnPropertyChanged(nameof(Ctrl));
                if (isLeftCtrl && !isRightCtrl && CtrlLocation != ModifierLocation.Any) CtrlLocation = ModifierLocation.Left;
                else if (isRightCtrl && !isLeftCtrl && CtrlLocation != ModifierLocation.Any) CtrlLocation = ModifierLocation.Right;
            }

            if (alt)
            {
                _alt = true;
                OnPropertyChanged(nameof(Alt));
                if (isLeftAlt && !isRightAlt && AltLocation != ModifierLocation.Any) AltLocation = ModifierLocation.Left;
                else if (isRightAlt && !isLeftAlt && AltLocation != ModifierLocation.Any) AltLocation = ModifierLocation.Right;
            }

            if (shift)
            {
                _shift = true;
                OnPropertyChanged(nameof(Shift));
                if (isLeftShift && !isRightShift && ShiftLocation != ModifierLocation.Any) ShiftLocation = ModifierLocation.Left;
                else if (isRightShift && !isLeftShift && ShiftLocation != ModifierLocation.Any) ShiftLocation = ModifierLocation.Right;
            }

            if (win)
            {
                _win = true;
                OnPropertyChanged(nameof(Win));
                if (isLeftWin && !isRightWin && WinLocation != ModifierLocation.Any) WinLocation = ModifierLocation.Left;
                else if (isRightWin && !isLeftWin && WinLocation != ModifierLocation.Any) WinLocation = ModifierLocation.Right;
            }

            SyncCurrentToStep();
            Validate();
        }

        public void Validate()
        {
            var seq = BuildSequence();
            if (seq.IsEmpty)
            {
                var current = GetCurrentChord();
                if (current.HasModifiers && current.Key == VirtualKeyCode.None)
                {
                    ValidationError = "Please choose a trigger key for the selected modifiers.";
                    IsValid = false;
                    return;
                }

                // Completely empty sequence represents "None" (disabled hotkey), which is valid
                ValidationError = null;
                IsValid = true;
                return;
            }

            // Check if any intermediate steps are missing a trigger key
            for (int i = 0; i < Steps.Count; i++)
            {
                if (Steps[i].Key == VirtualKeyCode.None)
                {
                    ValidationError = $"Step {i + 1} requires a trigger key.";
                    IsValid = false;
                    return;
                }
            }

            if (seq.Validate(out string error))
            {
                ValidationError = null;
                IsValid = true;
            }
            else
            {
                ValidationError = error;
                IsValid = false;
            }
        }

        public void RefreshStepItems()
        {
            StepItems.Clear();
            for (int i = 0; i < Steps.Count; i++)
            {
                StepItems.Add(new ChordStepItem(this, i, Steps[i], i == _currentStepIndex));
            }
        }

        private void Save()
        {
            if (IsValid)
            {
                Saved?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void Clear()
        {
            _isSyncing = true;
            try
            {
                Steps.Clear();
                Steps.Add(new KeyChord());
                _currentStepIndex = 0;
                ClearCurrentFields();
            }
            finally
            {
                _isSyncing = false;
            }

            OnPropertyChanged(nameof(CurrentStepIndex));
            OnPropertyChanged(nameof(HasMultipleChords));
            RefreshStepItems();
            Validate();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ChordStepItem : INotifyPropertyChanged
    {
        private readonly HotkeyEditorViewModel _parent;
        private readonly int _index;

        public event PropertyChangedEventHandler PropertyChanged;

        public ChordStepItem(HotkeyEditorViewModel parent, int index, KeyChord chord, bool isSelected)
        {
            _parent = parent;
            _index = index;
            Chord = chord;
            IsSelected = isSelected;
            SelectCommand = new RelayCommand(() => _parent.SelectStep(_index));
            RemoveCommand = new RelayCommand(() => _parent.RemoveStep(_index));
        }

        public int Index => _index;
        public int StepNumber => _index + 1;
        public KeyChord Chord { get; }
        public bool IsSelected { get; }

        public string StepHeader => $"Step {StepNumber}:";
        public string StepText => Chord.Key == VirtualKeyCode.None && !Chord.HasModifiers ? "None" : Chord.ToString();

        public bool CanRemove => _parent.Steps.Count > 1;

        public Brush BorderBrush => IsSelected
            ? new SolidColorBrush(Color.FromRgb(0x0A, 0x84, 0xFF))
            : ThemeManager.Instance.BorderBrush;

        public Brush BackgroundBrush => IsSelected
            ? (ThemeManager.Instance.IsDarkTheme ? new SolidColorBrush(Color.FromArgb(0x40, 0x0A, 0x84, 0xFF)) : new SolidColorBrush(Color.FromArgb(0x25, 0x00, 0x78, 0xD7)))
            : ThemeManager.Instance.ControlBackgroundBrush;

        public Brush TextBrush => ThemeManager.Instance.ForegroundBrush;
        public Brush MutedBrush => ThemeManager.Instance.MutedBrush;

        public ICommand SelectCommand { get; }
        public ICommand RemoveCommand { get; }
    }
}
