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

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Greenshot.Linux.RecipeEditor.ViewModels;
using Greenshot.Linux.Recipes;

namespace Greenshot.Linux.RecipeEditing;

public partial class RecipeEditorWindow : Window
{
    private readonly DispatcherTimer _unsavedStateTimer;
    private RecipeGraphCanvas? _canvas;
    private bool _confirmedClose;

    public RecipeEditorWindow()
        : this(null)
    {
    }

    public RecipeEditorWindow(IRecipeManager? recipeManager)
    {
        InitializeComponent();
        ViewModel = new RecipeEditorViewModel(recipeManager);
        DataContext = ViewModel;
        _canvas = this.FindControl<RecipeGraphCanvas>("EditorCanvas");

        // Steps and triggers change their configuration directly: compare with the saved recipe regularly
        _unsavedStateTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(700), DispatcherPriority.Background, (_, _) => ViewModel.RefreshUnsavedState());
        _unsavedStateTimer.Start();
        Closing += OnWindowClosing;
        Closed += (_, _) =>
        {
            _unsavedStateTimer.Stop();
            ViewModel.Detach();
        };
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _canvas?.ResetView(), DispatcherPriority.Background);
        ViewModel.PropertyChanged += (_, e) =>
        {
            // Another recipe: show its steps
            if (e.PropertyName == nameof(RecipeEditorViewModel.ActiveRecipe))
            {
                Dispatcher.UIThread.Post(() => _canvas?.ResetView(), DispatcherPriority.Background);
            }
        };
    }

    public RecipeEditorViewModel ViewModel { get; }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_confirmedClose)
        {
            return;
        }

        // Save, discard or keep editing the unsaved changes; the question is shown after this event
        if (ViewModel.IsDirty)
        {
            e.Cancel = true;
            Dispatcher.UIThread.Post(() =>
            {
                if (ViewModel.ConfirmDiscardChanges())
                {
                    _confirmedClose = true;
                    Close();
                }
            });
        }
    }
}
