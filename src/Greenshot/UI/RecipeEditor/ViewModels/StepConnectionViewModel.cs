using System;
using System.Windows;
using System.Windows.Input;

namespace Greenshot.UI.RecipeEditor.ViewModels
{
    public class StepConnectionViewModel : ViewModelBase
    {
        private bool _isCycle;
        private bool _isActive;
        private readonly Action<StepConnectionViewModel> _onDisconnect;

        public StepPortViewModel Source { get; }
        public StepPortViewModel Target { get; }

        public StepNodeViewModel SourceNode => Source?.Node;
        public StepNodeViewModel TargetNode => Target?.Node;

        public ICommand DisconnectCommand { get; }

        public bool IsCycle
        {
            get => _isCycle;
            set => SetField(ref _isCycle, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetField(ref _isActive, value);
        }

        public StepConnectionViewModel(StepPortViewModel source, StepPortViewModel target, Action<StepConnectionViewModel> onDisconnect = null)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target ?? throw new ArgumentNullException(nameof(target));
            _onDisconnect = onDisconnect;
            DisconnectCommand = new RelayCommand(() => _onDisconnect?.Invoke(this));
            if (Source != null) Source.IsConnected = true;
            if (Target != null) Target.IsConnected = true;
        }
    }

    public class PendingConnectionViewModel : ViewModelBase
    {
        private Point _targetLocation;
        private bool _isVisible;
        private StepPortViewModel _source;
        private StepPortViewModel _target;

        public ICommand StartedCommand { get; set; }
        public ICommand CompletedCommand { get; set; }

        public StepPortViewModel Source
        {
            get => _source;
            set => SetField(ref _source, value);
        }

        public StepPortViewModel Target
        {
            get => _target;
            set => SetField(ref _target, value);
        }

        public Point TargetLocation
        {
            get => _targetLocation;
            set => SetField(ref _targetLocation, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetField(ref _isVisible, value);
        }
    }
}
