using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Dapplo.Windows.Input.Enums;
using Greenshot.Base.Core;
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
            set { _ctrl = value; OnPropertyChanged(); Validate(); }
        }

        public ModifierLocation CtrlLocation
        {
            get => _ctrlLocation;
            set { _ctrlLocation = value; OnPropertyChanged(); Validate(); }
        }

        public bool Alt
        {
            get => _alt;
            set { _alt = value; OnPropertyChanged(); Validate(); }
        }

        public ModifierLocation AltLocation
        {
            get => _altLocation;
            set { _altLocation = value; OnPropertyChanged(); Validate(); }
        }

        public bool Shift
        {
            get => _shift;
            set { _shift = value; OnPropertyChanged(); Validate(); }
        }

        public ModifierLocation ShiftLocation
        {
            get => _shiftLocation;
            set { _shiftLocation = value; OnPropertyChanged(); Validate(); }
        }

        public bool Win
        {
            get => _win;
            set { _win = value; OnPropertyChanged(); Validate(); }
        }

        public ModifierLocation WinLocation
        {
            get => _winLocation;
            set { _winLocation = value; OnPropertyChanged(); Validate(); }
        }

        public VirtualKeyCode TriggerKey
        {
            get => _triggerKey;
            set { _triggerKey = value; OnPropertyChanged(); OnPropertyChanged(nameof(TriggerKeyDisplay)); Validate(); }
        }

        public string TriggerKeyDisplay => _triggerKey == VirtualKeyCode.None ? "Click here and press key" : KeyChord.FormatKeyName(_triggerKey);

        public ObservableCollection<KeyChord> Chords { get; } = new ObservableCollection<KeyChord>();

        public bool HasMultipleChords => Chords.Count > 1;

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
        public ICommand AddChordCommand { get; }
        public ICommand ResetToChord1Command { get; }

        public HotkeyEditorViewModel()
        {
            SaveCommand = new RelayCommand(Save, () => IsValid);
            CancelCommand = new RelayCommand(Cancel);
            ClearCommand = new RelayCommand(Clear);
            AddChordCommand = new RelayCommand(AddCurrentAsChord, () => IsCurrentChordValid());
            ResetToChord1Command = new RelayCommand(ResetToSingleChord);
        }

        public void LoadFromSequenceString(string actionName, string hotkeyString)
        {
            ActionTitle = actionName;
            Chords.Clear();

            var seq = HotkeySequence.Parse(hotkeyString);
            if (!seq.IsEmpty)
            {
                if (seq.Chords.Count > 1)
                {
                    for (int i = 0; i < seq.Chords.Count - 1; i++)
                    {
                        Chords.Add(seq.Chords[i]);
                    }
                    var last = seq.Chords[seq.Chords.Count - 1];
                    LoadChordIntoCurrent(last);
                }
                else
                {
                    LoadChordIntoCurrent(seq.Chords[0]);
                }
            }
            else
            {
                ClearCurrentFields();
            }

            OnPropertyChanged(nameof(HasMultipleChords));
            Validate();
        }

        private void LoadChordIntoCurrent(KeyChord chord)
        {
            Ctrl = chord.Ctrl;
            CtrlLocation = chord.CtrlLocation;
            Alt = chord.Alt;
            AltLocation = chord.AltLocation;
            Shift = chord.Shift;
            ShiftLocation = chord.ShiftLocation;
            Win = chord.Win;
            WinLocation = chord.WinLocation;
            TriggerKey = chord.Key;
        }

        private void ClearCurrentFields()
        {
            Ctrl = false;
            CtrlLocation = ModifierLocation.Any;
            Alt = false;
            AltLocation = ModifierLocation.Any;
            Shift = false;
            ShiftLocation = ModifierLocation.Any;
            Win = false;
            WinLocation = ModifierLocation.Any;
            TriggerKey = VirtualKeyCode.None;
        }

        public HotkeySequence BuildSequence()
        {
            var currentChord = GetCurrentChord();
            if (Chords.Count > 0)
            {
                var allChords = new List<KeyChord>(Chords);
                if (currentChord.Key != VirtualKeyCode.None)
                {
                    allChords.Add(currentChord);
                }
                return new HotkeySequence(allChords);
            }

            return HotkeySequence.FromChord(currentChord);
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

        private bool IsCurrentChordValid()
        {
            var chord = GetCurrentChord();
            var seq = HotkeySequence.FromChord(chord);
            return seq.Validate(out _);
        }

        public void AddCurrentAsChord()
        {
            if (!IsCurrentChordValid()) return;

            var current = GetCurrentChord();
            Chords.Add(current);

            // Clear current for next chord
            ClearCurrentFields();

            OnPropertyChanged(nameof(HasMultipleChords));
            Validate();
        }

        public void ResetToSingleChord()
        {
            Chords.Clear();
            OnPropertyChanged(nameof(HasMultipleChords));
            Validate();
        }

        public void SetCapturedKey(VirtualKeyCode vk, bool ctrl, bool alt, bool shift, bool win,
            bool isLeftCtrl, bool isRightCtrl, bool isLeftAlt, bool isRightAlt, bool isLeftShift, bool isRightShift, bool isLeftWin, bool isRightWin)
        {
            TriggerKey = vk;

            if (ctrl)
            {
                Ctrl = true;
                if (isLeftCtrl && !isRightCtrl && CtrlLocation != ModifierLocation.Any) CtrlLocation = ModifierLocation.Left;
                else if (isRightCtrl && !isLeftCtrl && CtrlLocation != ModifierLocation.Any) CtrlLocation = ModifierLocation.Right;
            }

            if (alt)
            {
                Alt = true;
                if (isLeftAlt && !isRightAlt && AltLocation != ModifierLocation.Any) AltLocation = ModifierLocation.Left;
                else if (isRightAlt && !isLeftAlt && AltLocation != ModifierLocation.Any) AltLocation = ModifierLocation.Right;
            }

            if (shift)
            {
                Shift = true;
                if (isLeftShift && !isRightShift && ShiftLocation != ModifierLocation.Any) ShiftLocation = ModifierLocation.Left;
                else if (isRightShift && !isLeftShift && ShiftLocation != ModifierLocation.Any) ShiftLocation = ModifierLocation.Right;
            }

            if (win)
            {
                Win = true;
                if (isLeftWin && !isRightWin && WinLocation != ModifierLocation.Any) WinLocation = ModifierLocation.Left;
                else if (isRightWin && !isLeftWin && WinLocation != ModifierLocation.Any) WinLocation = ModifierLocation.Right;
            }

            Validate();
        }

        public void UpdateModifiersFromKeyboard(bool ctrl, bool alt, bool shift, bool win,
            bool isLeftCtrl, bool isRightCtrl, bool isLeftAlt, bool isRightAlt, bool isLeftShift, bool isRightShift, bool isLeftWin, bool isRightWin)
        {
            if (ctrl)
            {
                Ctrl = true;
                if (isLeftCtrl && !isRightCtrl && CtrlLocation != ModifierLocation.Any) CtrlLocation = ModifierLocation.Left;
                else if (isRightCtrl && !isLeftCtrl && CtrlLocation != ModifierLocation.Any) CtrlLocation = ModifierLocation.Right;
            }

            if (alt)
            {
                Alt = true;
                if (isLeftAlt && !isRightAlt && AltLocation != ModifierLocation.Any) AltLocation = ModifierLocation.Left;
                else if (isRightAlt && !isLeftAlt && AltLocation != ModifierLocation.Any) AltLocation = ModifierLocation.Right;
            }

            if (shift)
            {
                Shift = true;
                if (isLeftShift && !isRightShift && ShiftLocation != ModifierLocation.Any) ShiftLocation = ModifierLocation.Left;
                else if (isRightShift && !isLeftShift && ShiftLocation != ModifierLocation.Any) ShiftLocation = ModifierLocation.Right;
            }

            if (win)
            {
                Win = true;
                if (isLeftWin && !isRightWin && WinLocation != ModifierLocation.Any) WinLocation = ModifierLocation.Left;
                else if (isRightWin && !isLeftWin && WinLocation != ModifierLocation.Any) WinLocation = ModifierLocation.Right;
            }

            Validate();
        }

        public void Validate()
        {
            var seq = BuildSequence();
            if (seq.IsEmpty)
            {
                ValidationError = "Please press a key or combination.";
                IsValid = false;
                return;
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
            Chords.Clear();
            Ctrl = false;
            CtrlLocation = ModifierLocation.Any;
            Alt = false;
            AltLocation = ModifierLocation.Any;
            Shift = false;
            ShiftLocation = ModifierLocation.Any;
            Win = false;
            WinLocation = ModifierLocation.Any;
            TriggerKey = VirtualKeyCode.None;
            OnPropertyChanged(nameof(HasMultipleChords));
            Validate();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
