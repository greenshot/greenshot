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

using System.Globalization;
using Greenshot.Linux.Core;
using Greenshot.Linux.Recipes.Approval;

namespace Greenshot.Linux.Recipes;

// The recipe engine is the one of Greenshot.Base and Greenshot (Windows), ported with few changes. These are the types
// of Windows it uses, made for Linux, so the ported code reads like the original.

/// <summary>
/// The log4net logger the ported code uses, written to the Greenshot log
/// </summary>
internal interface ILog
{
    bool IsDebugEnabled { get; }

    void Debug(object message, Exception exception = null);

    void DebugFormat(string format, params object[] args);

    void Info(object message, Exception exception = null);

    void InfoFormat(string format, params object[] args);

    void Warn(object message, Exception exception = null);

    void WarnFormat(string format, params object[] args);

    void Error(object message, Exception exception = null);

    void ErrorFormat(string format, params object[] args);
}

internal static class LogManager
{
    public static ILog GetLogger(Type type) => new Logger(type.Name);

    private sealed class Logger(string name) : ILog
    {
        public bool IsDebugEnabled => Log.DebugEnabled;

        private string Text(object message, Exception exception) =>
            exception == null || !IsDebugEnabled ? $"{name}: {message}" : $"{name}: {message} {exception}";

        public void Debug(object message, Exception exception = null) => Log.Debug(Text(message, exception));

        public void DebugFormat(string format, params object[] args) => Log.Debug($"{name}: {string.Format(CultureInfo.InvariantCulture, format, args)}");

        public void Info(object message, Exception exception = null) => Log.Info(Text(message, exception));

        public void InfoFormat(string format, params object[] args) => Log.Info($"{name}: {string.Format(CultureInfo.InvariantCulture, format, args)}");

        public void Warn(object message, Exception exception = null) => Log.Warn($"{name}: {message}", exception);

        public void WarnFormat(string format, params object[] args) => Log.Warn($"{name}: {string.Format(CultureInfo.InvariantCulture, format, args)}");

        public void Error(object message, Exception exception = null) => Log.Error($"{name}: {message}", exception);

        public void ErrorFormat(string format, params object[] args) => Log.Error($"{name}: {string.Format(CultureInfo.InvariantCulture, format, args)}");
    }
}

/// <summary>
/// How a screen capture is taken (Windows: one screen, all screens, the fixed screen), recipes may name it
/// </summary>
public enum ScreenCaptureMode
{
    Auto,
    FullScreen,
    Fixed,
}

/// <summary>
/// The clipboard formats of Windows, recipes may name them; on Linux the clipboard gets PNG
/// </summary>
public enum ClipboardFormat
{
    PNG,
    DIB,
    HTML,
    HTMLDATAURL,
    BITMAP,
    DIBV5,
}

/// <summary>
/// Which editor the Editor step uses
/// </summary>
public enum TargetEditor
{
    NewEditor,
    AvailableEditor,
    CurrentEditor,
}

/// <summary>
/// The capture modes of recipes (Greenshot.Base CaptureMode)
/// </summary>
public enum CaptureMode
{
    None,
    Region,
    FullScreen,
    ActiveWindow,
    Window,
    LastRegion,
    Clipboard,
    File,
    Import,
    Text,
}

/// <summary>
/// Runs code on the UI thread, set by the app
/// </summary>
public interface IUiDispatcher
{
    /// <summary>
    /// True on the UI thread
    /// </summary>
    bool CheckAccess();

    Task InvokeAsync(Action action, CancellationToken cancellationToken = default);

    Task<T> InvokeAsync<T>(Func<T> func, CancellationToken cancellationToken = default);

    Task<T> InvokeAsync<T>(Func<Task<T>> func, CancellationToken cancellationToken = default);
}

/// <summary>
/// Without a UI: runs the code right away (tests, the command line)
/// </summary>
public sealed class InlineUiDispatcher : IUiDispatcher
{
    public static InlineUiDispatcher Instance { get; } = new();

    public bool CheckAccess() => true;

    public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
    {
        action();
        return Task.CompletedTask;
    }

    public Task<T> InvokeAsync<T>(Func<T> func, CancellationToken cancellationToken = default) => Task.FromResult(func());

    public Task<T> InvokeAsync<T>(Func<Task<T>> func, CancellationToken cancellationToken = default) => func();
}

/// <summary>
/// The step needs the user, but the recipe runs unattended (command line, AI tools)
/// </summary>
public class InteractionRequiredException(string what) : InvalidOperationException($"'{what}' needs the user, but this runs unattended.");

/// <summary>
/// The editor a recipe of the editor's Recipes menu runs for
/// </summary>
public interface IImageEditor
{
    Greenshot.Linux.Editor.EditorSurface Surface { get; }

    Greenshot.Linux.Core.Capturing.CaptureDetails CaptureDetails { get; }
}

/// <summary>
/// A hotkey of a recipe trigger ("Ctrl + Shift + B", chords separated by a comma: "Ctrl + K, C"), checked like the
/// HotkeySequence of Windows. Linux binds keys in the compositor or through the GlobalShortcuts portal, which take the
/// same notation.
/// </summary>
public sealed class HotkeySequence
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase) { "Escape", "Esc", "Return", "Enter", "Tab", "Capital", "CapsLock" };
    private static readonly HashSet<string> Standalone = new(StringComparer.OrdinalIgnoreCase) { "PrintScreen", "Print", "Snapshot", "Scroll", "ScrollLock", "Pause" };

    public sealed record Chord(bool Ctrl, bool Alt, bool Shift, bool Win, string Key)
    {
        public bool HasModifiers => Ctrl || Alt || Shift || Win;
    }

    public List<Chord> Chords { get; } = new();

    public bool IsEmpty => Chords.Count == 0 || Chords.All(c => string.IsNullOrEmpty(c.Key));

    public static HotkeySequence Parse(string sequence)
    {
        var result = new HotkeySequence();
        if (string.IsNullOrWhiteSpace(sequence) || string.Equals(sequence.Trim(), "None", StringComparison.OrdinalIgnoreCase))
        {
            return result;
        }

        foreach (var part in sequence.Replace(" then ", ",").Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            bool ctrl = false, alt = false, shift = false, win = false;
            string key = null;
            foreach (var raw in part.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string lower = raw.ToLowerInvariant();
                if (lower.StartsWith("ctrl") || lower.StartsWith("control")) ctrl = true;
                else if (lower.StartsWith("alt")) alt = true;
                else if (lower.StartsWith("shift")) shift = true;
                else if (lower.StartsWith("win") || lower is "super" or "meta" or "logo") win = true;
                else key = raw;
            }

            if (key != null || ctrl || alt || shift || win)
            {
                result.Chords.Add(new Chord(ctrl, alt, shift, win, key));
            }
        }

        return result;
    }

    private static bool IsFunctionKey(string key) =>
        key.Length is 2 or 3 && (key[0] is 'F' or 'f') && int.TryParse(key.AsSpan(1), out int number) && number is >= 1 and <= 24;

    private static bool IsAlphanumeric(string key) =>
        key.Length == 1 && char.IsLetterOrDigit(key[0])
        || key.StartsWith("Numpad", StringComparison.OrdinalIgnoreCase)
        || key.Length == 2 && (key[0] is 'D' or 'd') && char.IsDigit(key[1]);

    public bool Validate(out string errorMessage)
    {
        if (IsEmpty)
        {
            errorMessage = "No hotkey set.";
            return false;
        }

        for (int i = 0; i < Chords.Count; i++)
        {
            var chord = Chords[i];
            string label = Chords.Count > 1 ? $"Chord {i + 1}: " : "";
            if (string.IsNullOrEmpty(chord.Key))
            {
                errorMessage = $"{label}Please choose a trigger key.";
                return false;
            }

            if (Reserved.Contains(chord.Key))
            {
                errorMessage = $"{label}{chord.Key} is a reserved system key and cannot be used as a trigger.";
                return false;
            }

            // The first chord needs a modifier, unless it's F1-F24, PrintScreen, ScrollLock or Pause
            if (i == 0)
            {
                if (!chord.HasModifiers && !IsFunctionKey(chord.Key) && !Standalone.Contains(chord.Key))
                {
                    errorMessage = $"{label}Common keys require at least one modifier key (Ctrl, Alt, Shift, or Win).";
                    return false;
                }

                if (chord.Shift && !chord.Ctrl && !chord.Alt && !chord.Win && IsAlphanumeric(chord.Key))
                {
                    errorMessage = $"{label}Shift cannot be the only modifier for letters or numbers.";
                    return false;
                }
            }
        }

        errorMessage = null;
        return true;
    }
}

/// <summary>
/// [Recipe] of greenshot.ini like IRecipeConfiguration of Windows (EnableRecipeFeature, RecipeFiles, DisabledRecipeIds)
/// </summary>
public sealed class RecipeConfiguration
{
    private const string Section = RecipeConfigHelper.SectionName;

    public bool Enabled
    {
        get => RecipeConfigHelper.IsRecipeFeatureEnabled();
        set => RecipeServices.Settings?.SetValue(Section, "EnableRecipeFeature", value ? "True" : "False");
    }

    public string RecipeFiles
    {
        get => RecipeServices.Settings?.GetValue(Section, nameof(RecipeFiles));
        set => SetOrRemove(nameof(RecipeFiles), value);
    }

    public string DisabledRecipeIds
    {
        get => RecipeServices.Settings?.GetValue(Section, nameof(DisabledRecipeIds));
        set => SetOrRemove(nameof(DisabledRecipeIds), value);
    }

    private static void SetOrRemove(string key, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            RecipeServices.Settings?.RemoveValue(Section, key);
        }
        else
        {
            RecipeServices.Settings?.SetValue(Section, key, value);
        }
    }
}

/// <summary>
/// Saving greenshot.ini, the IniConfigRegistry of Windows
/// </summary>
internal static class IniConfigRegistry
{
    public static Greenshot.Linux.Core.Configuration.CoreSettings Get() => RecipeServices.Settings;
}

/// <summary>
/// The UI thread of the app, UiDispatcher.Current of Windows
/// </summary>
internal static class UiDispatcher
{
    public static IUiDispatcher Current => RecipeServices.Ui ?? InlineUiDispatcher.Instance;
}

internal static class TaskExtensions
{
    /// <summary>
    /// A task nobody waits for: its failure goes into the log
    /// </summary>
    public static void FireAndLog(this Task task, string what, ILog log = null) =>
        task.ContinueWith(t => Log.Warn($"{what} failed", t.Exception?.GetBaseException()), CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
}

/// <summary>
/// The approval window of recipe files, made by the app; it is modal like the WPF window of Windows
/// </summary>
public interface IRecipeApprovalUi
{
    /// <summary>
    /// What the user approved, null when the recipe was rejected; read-only requests only show the details
    /// </summary>
    ApprovalResult ShowApproval(Approval.RecipeApprovalRequest request);

    void ShowValidationError(string filePath, RecipeValidationResult validation, CaptureRecipe recipe, string rawErrorMessage);
}

/// <summary>
/// The approval window as the ported RecipeManager uses it (properties and ShowDialog of the WPF window)
/// </summary>
internal sealed class RecipeApprovalWindow(Approval.RecipeApprovalRequest request)
{
    public bool Topmost { get; set; }

    public bool ShowActivated { get; set; }

    public object WindowStartupLocation { get; set; }

    public RecipeApproval Approval { get; private set; }

    public bool OpenInEditor { get; private set; }

    public bool IsRevoked { get; private set; }

    public bool? ShowDialog()
    {
        var ui = RecipeServices.ApprovalUi;
        if (ui == null)
        {
            Log.Warn($"No approval window, the recipe file {request.FilePath} is not approved");
            return false;
        }

        var result = ui.ShowApproval(request);
        if (result == null)
        {
            return false;
        }

        IsRevoked = ReferenceEquals(result, ApprovalResult.Revoked);
        Approval = result.Approval;
        OpenInEditor = result.OpenInEditor;
        return Approval != null;
    }

    public static void ShowValidationError(string filePath, RecipeValidationResult validation = null, CaptureRecipe recipe = null, string rawErrorMessage = null) =>
        RecipeServices.ApprovalUi?.ShowValidationError(filePath, validation, recipe, rawErrorMessage);
}

/// <summary>
/// Copying to the clipboard failed; error routes of recipes can name it (onError "ClipboardException")
/// </summary>
public class ClipboardException(string message, string owner = null, Exception inner = null) : InvalidOperationException(message, inner)
{
    /// <summary>
    /// The program which holds the clipboard, when known
    /// </summary>
    public string Owner { get; } = owner;
}

/// <summary>
/// An export to a destination failed; error routes of recipes can name it (onError "DestinationExportException")
/// </summary>
public class DestinationExportException(string message, string failedDestination, Exception inner = null) : InvalidOperationException(message, inner)
{
    public string FailedDestination { get; } = failedDestination;
}
