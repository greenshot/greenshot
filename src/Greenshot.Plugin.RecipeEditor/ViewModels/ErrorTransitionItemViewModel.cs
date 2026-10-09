/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.Windows.Input;
using Greenshot.Base.Recipes;
using Greenshot.Base.Wpf;

namespace Greenshot.Plugin.RecipeEditor.ViewModels
{
    public class ErrorTransitionItemViewModel : ViewModelBase
    {
        private string _fromNodeId;
        private string _targetType;
        private string _toNodeId;
        private string _targetRecipeId;
        private string _errorType;
        private readonly Action<ErrorTransitionItemViewModel> _onDelete;
        private readonly Func<IEnumerable<StepNodeViewModel>> _nodesProvider;
        private readonly Func<IEnumerable<CaptureRecipe>> _recipesProvider;

        public ErrorTransitionItemViewModel(
            RecipeErrorTransitionConfig config,
            Action<ErrorTransitionItemViewModel> onDelete,
            Func<IEnumerable<StepNodeViewModel>> nodesProvider,
            Func<IEnumerable<CaptureRecipe>> recipesProvider)
        {
            _onDelete = onDelete;
            _nodesProvider = nodesProvider;
            _recipesProvider = recipesProvider;

            _fromNodeId = string.IsNullOrEmpty(config?.From) ? "*" : config.From;
            _errorType = string.IsNullOrEmpty(config?.ErrorType) ? "*" : config.ErrorType;
            _toNodeId = config?.To;
            _targetRecipeId = config?.TargetRecipeId;
            _targetType = !string.IsNullOrEmpty(_targetRecipeId) ? "Recipe" : "Step";

            DeleteCommand = new RelayCommand(() => _onDelete?.Invoke(this));
        }

        public string FromNodeId
        {
            get => _fromNodeId;
            set => SetField(ref _fromNodeId, value);
        }

        public string TargetType
        {
            get => _targetType;
            set
            {
                if (SetField(ref _targetType, value))
                {
                    OnPropertyChanged(nameof(IsTargetStep));
                    OnPropertyChanged(nameof(IsTargetRecipe));
                }
            }
        }

        public bool IsTargetStep => string.Equals(TargetType, "Step", StringComparison.OrdinalIgnoreCase);
        public bool IsTargetRecipe => string.Equals(TargetType, "Recipe", StringComparison.OrdinalIgnoreCase);

        public string ToNodeId
        {
            get => _toNodeId;
            set => SetField(ref _toNodeId, value);
        }

        public string TargetRecipeId
        {
            get => _targetRecipeId;
            set => SetField(ref _targetRecipeId, value);
        }

        public string ErrorType
        {
            get => _errorType;
            set => SetField(ref _errorType, value);
        }

        public ICommand DeleteCommand { get; }

        public IEnumerable<StepNodeViewModel> AvailableNodes => _nodesProvider?.Invoke() ?? Enumerable.Empty<StepNodeViewModel>();
        public IEnumerable<CaptureRecipe> AvailableRecipes => _recipesProvider?.Invoke() ?? Enumerable.Empty<CaptureRecipe>();

        public RecipeErrorTransitionConfig ToConfig()
        {
            return new RecipeErrorTransitionConfig(
                _fromNodeId,
                IsTargetStep ? _toNodeId : null,
                IsTargetRecipe ? _targetRecipeId : null,
                _errorType);
        }
    }
}
