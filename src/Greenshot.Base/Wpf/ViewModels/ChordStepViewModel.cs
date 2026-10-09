using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using Dapplo.Windows.Input.Enums;
using Greenshot.Base.Core;

namespace Greenshot.Base.Wpf.ViewModels
{
    public class ChordStepViewModel : INotifyPropertyChanged
    {
        private readonly HotkeyEditorViewModel _parent;
        private readonly int _index;

        public event PropertyChangedEventHandler PropertyChanged;

        public ChordStepViewModel(HotkeyEditorViewModel parent, int index, KeyChord chord, bool isSelected)
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
