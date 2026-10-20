using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using Dapplo.Windows.Input.Enums;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;

namespace Greenshot.Base.Wpf.ViewModels
{
    public class ChordStepViewModel : INotifyPropertyChanged
    {
        private readonly HotkeyEditorViewModel _parent;
        private readonly int _index;

        // All properties are fixed, a changed step gets a new view model. The interface stays so WPF binding does not fall back to PropertyDescriptor (which leaks)
        public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }

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

        public string StepHeader => string.Format(Texts.Settings.HotkeyStep, StepNumber);
        public string StepText => Chord.Key == VirtualKeyCode.None && !Chord.HasModifiers ? Texts.Settings.HotkeyNone : Chord.ToString();

        /// <summary>
        /// The name of the step for a screen reader, the selection is otherwise only shown by color
        /// </summary>
        public string AccessibleName => string.Format(IsSelected ? Texts.Settings.HotkeyStepSelectedAccessible : Texts.Settings.HotkeyStepAccessible, StepNumber, StepText);

        public bool CanRemove => _parent.Steps.Count > 1;

        // The accent of the theme, which follows Windows and switches to the system colors in high contrast
        public Brush BorderBrush => IsSelected
            ? ThemeManager.Instance.AccentBrush
            : ThemeManager.Instance.BorderBrush;

        public Brush BackgroundBrush => IsSelected
            ? CreateAccentTint()
            : ThemeManager.Instance.ControlBackgroundBrush;

        private static Brush CreateAccentTint()
        {
            var accent = ThemeManager.Instance.CurrentPalette.AccentBrush.Color;
            return new SolidColorBrush(Color.FromArgb(ThemeManager.Instance.IsDarkTheme ? (byte)0x40 : (byte)0x25, accent.R, accent.G, accent.B));
        }

        public Brush TextBrush => ThemeManager.Instance.ForegroundBrush;
        public Brush MutedBrush => ThemeManager.Instance.MutedBrush;

        public ICommand SelectCommand { get; }
        public ICommand RemoveCommand { get; }
    }
}
