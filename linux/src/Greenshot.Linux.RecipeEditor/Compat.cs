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

#nullable disable

using System.Windows.Input;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Recipes;

namespace Greenshot.Linux.RecipeEditor
{
    /// <summary>
    /// A position on the canvas of the recipe editor, the System.Windows.Point of the Windows version
    /// </summary>
    public readonly record struct Point(double X, double Y)
    {
        public static Vector operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);

        public static Point operator +(Point a, Vector b) => new(a.X + b.X, a.Y + b.Y);
    }

    public readonly record struct Vector(double X, double Y)
    {
        public double Length => Math.Sqrt(X * X + Y * Y);
    }

    public readonly record struct Size(double Width, double Height);

    /// <summary>
    /// A file format of the SaveFile step
    /// </summary>
    public sealed class FileFormatOption
    {
        public string Id { get; set; }

        public string DisplayName { get; set; }

        public string DisplayNameWithPreferredExtension { get; set; }

        public override string ToString() => DisplayNameWithPreferredExtension ?? DisplayName ?? Id;
    }

    /// <summary>
    /// An ICommand for the view models (Greenshot.Base.Wpf.RelayCommand)
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
            : this(_ => execute(), canExecute == null ? null : _ => canExecute())
        {
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Runs an async command from a click, errors are logged
    /// </summary>
    public static class AsyncCommand
    {
        public static void Run(Func<Task> action, string what) => action().FireAndLog(what);
    }

    public enum MessageBoxButton
    {
        OK,
        OKCancel,
        YesNoCancel,
        YesNo,
    }

    public enum MessageBoxImage
    {
        None,
        Error,
        Question,
        Warning,
        Information,
    }

    public enum MessageBoxResult
    {
        None,
        OK,
        Cancel,
        Yes,
        No,
    }

    /// <summary>
    /// A file of the file dialogs: the filter of Windows ("Name|*.png;*.jpg|..."), the chosen file
    /// </summary>
    public sealed record FileDialogRequest(string Title, string Filter, string FileName, string InitialDirectory, bool Save);

    /// <summary>
    /// What the recipe editor asks the app for: modal dialogs (they return when the user answered, the UI keeps running),
    /// the open image editors and the theme. Without an app (tests) nothing is shown.
    /// </summary>
    public interface IRecipeEditorUi
    {
        MessageBoxResult ShowMessage(string text, string caption, MessageBoxButton buttons, MessageBoxImage image);

        /// <summary>
        /// The index of the chosen button, <paramref name="cancelIndex"/> when the dialog was closed
        /// </summary>
        int ShowChoice(string caption, string text, MessageBoxImage image, IReadOnlyList<string> buttons, int defaultIndex, int cancelIndex);

        string PickFile(FileDialogRequest request);

        string PickFolder(string title, string initialDirectory);

        /// <summary>
        /// "#RRGGBB", "#AARRGGBB" or "transparent", null when cancelled
        /// </summary>
        string PickColor(string current);

        void ShowRecipeManager(ViewModels.RecipeManagerViewModel viewModel);

        IReadOnlyList<IImageEditor> OpenEditors { get; }

        IImageEditor SelectEditor(IReadOnlyList<IImageEditor> editors);

        /// <summary>
        /// The annotations chosen in the import dialog and whether they replace the existing ones, null when cancelled
        /// </summary>
        (List<Dictionary<string, object>> Annotations, bool Replace)? ReviewImport(List<Helpers.ImportableElementItem> items, string sourceTitle, string recipeName, bool hasExisting);

        void PlaySound(string file);

        void SetClipboardText(string text);

        bool IsDarkMode { get; }

        void ToggleTheme();

        event Action ThemeChanged;

        /// <summary>
        /// Runs the action later on the UI thread, after the pending layout
        /// </summary>
        void Post(Action action);
    }

    /// <summary>
    /// The app's <see cref="IRecipeEditorUi"/>, set at the start
    /// </summary>
    public static class RecipeEditorUi
    {
        private static readonly IRecipeEditorUi Unattended = new UnattendedRecipeEditorUi();

        public static IRecipeEditorUi Current { get; set; }

        internal static IRecipeEditorUi Get() => Current ?? Unattended;

        private sealed class UnattendedRecipeEditorUi : IRecipeEditorUi
        {
            public MessageBoxResult ShowMessage(string text, string caption, MessageBoxButton buttons, MessageBoxImage image) =>
                buttons is MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel ? MessageBoxResult.No : MessageBoxResult.OK;

            public int ShowChoice(string caption, string text, MessageBoxImage image, IReadOnlyList<string> buttons, int defaultIndex, int cancelIndex) => cancelIndex;

            public string PickFile(FileDialogRequest request) => null;

            public string PickFolder(string title, string initialDirectory) => null;

            public string PickColor(string current) => null;

            public void ShowRecipeManager(ViewModels.RecipeManagerViewModel viewModel)
            {
            }

            public IReadOnlyList<IImageEditor> OpenEditors => [];

            public IImageEditor SelectEditor(IReadOnlyList<IImageEditor> editors) => null;

            public (List<Dictionary<string, object>> Annotations, bool Replace)? ReviewImport(List<Helpers.ImportableElementItem> items, string sourceTitle, string recipeName, bool hasExisting) => null;

            public void PlaySound(string file)
            {
            }

            public void SetClipboardText(string text)
            {
            }

            public bool IsDarkMode => false;

            public void ToggleTheme()
            {
            }

            public event Action ThemeChanged
            {
                add { }
                remove { }
            }

            public void Post(Action action) => action();
        }
    }

    /// <summary>
    /// The message box of Greenshot.Base.Wpf, shown by the app
    /// </summary>
    public static class ThemedMessageBox
    {
        public static MessageBoxResult Show(string text, string caption, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None) =>
            RecipeEditorUi.Get().ShowMessage(text, caption, buttons, image);

        public static int ShowChoice(object owner, string caption, string text, MessageBoxImage image, string[] buttons, int defaultIndex = 0, int cancelIndex = -1) =>
            RecipeEditorUi.Get().ShowChoice(caption, text, image, buttons, defaultIndex, cancelIndex < 0 ? buttons.Length - 1 : cancelIndex);
    }

    /// <summary>
    /// The file dialogs of Microsoft.Win32, shown by the app
    /// </summary>
    public class OpenFileDialog
    {
        public string Title { get; set; }

        public string Filter { get; set; }

        public string FileName { get; set; }

        public string InitialDirectory { get; set; }

        public string DefaultExt { get; set; }

        public bool CheckFileExists { get; set; }

        protected virtual bool IsSave => false;

        public bool? ShowDialog()
        {
            var file = RecipeEditorUi.Get().PickFile(new FileDialogRequest(Title, Filter, FileName, InitialDirectory, IsSave));
            if (string.IsNullOrEmpty(file))
            {
                return false;
            }

            FileName = file;
            return true;
        }
    }

    public sealed class SaveFileDialog : OpenFileDialog
    {
        protected override bool IsSave => true;
    }

    /// <summary>
    /// The paths of recipes can have placeholders (${capturetime}, ${YYYY}, environment variables)
    /// </summary>
    public static class RecipeEditorPaths
    {
        public static string Expand(string path)
        {
            if (string.IsNullOrEmpty(path) || RecipeServices.Settings == null)
            {
                return path;
            }

            return Greenshot.Linux.Core.Naming.FilenamePattern.ForSettings(RecipeServices.Settings).Fill(path, null, false);
        }
    }

    /// <summary>
    /// The theme of the app, light or dark
    /// </summary>
    public static class WpfThemeHelper
    {
        public static bool IsDarkMode => RecipeEditorUi.Get().IsDarkMode;

        public static void ToggleTheme() => RecipeEditorUi.Get().ToggleTheme();

        public static event Action ThemeChanged
        {
            add => RecipeEditorUi.Get().ThemeChanged += value;
            remove => RecipeEditorUi.Get().ThemeChanged -= value;
        }
    }
}
