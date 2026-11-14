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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Recipes.Views;
using log4net;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Waits before the next step, e.g. before the capture, so the user can open a menu or a popup. Shows a countdown which
    /// doesn't take the focus (a menu would close), and can let the user choose the delay first. Unattended (command line,
    /// tests) nothing is shown and it waits the Seconds of the step.
    /// </summary>
    [StepInfo(WellKnownStepTypes.Delay, "Delay", "Waits before the next step, e.g. before the capture, so you can open a menu or a popup. Shows a countdown and can let you choose the delay first. Unattended (command line) it waits the Seconds without a window.", "Interaction")]
    [StepParameter(SecondsParameter, ContractDataType.Integer, DefaultValue = DefaultSeconds, Description = "Seconds to wait, 0 to 600 (supports ${...}, e.g. a recipe option); with AskUser the choice selected first")]
    [StepParameter(AskUserParameter, ContractDataType.Boolean, DefaultValue = false, Description = "Let the user choose the delay first: 3, 5, 10, 30 seconds or Seconds. Unattended: Seconds, nothing is asked")]
    [StepParameter(ShowCountdownParameter, ContractDataType.Boolean, DefaultValue = true, Description = "Show a countdown, which doesn't take the focus, with a button to cancel")]
    [StepOutputVariable(OutputVariable, ContractDataType.Integer, "The seconds the step waited")]
    public class DelayStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DelayStep));

        public const string SecondsParameter = "Seconds";
        public const string AskUserParameter = "AskUser";
        public const string ShowCountdownParameter = "ShowCountdown";
        public const string OutputVariable = "DelaySeconds";
        public const int DefaultSeconds = 5;
        public const int MaxSeconds = 600;

        /// <summary>
        /// The delays offered when the user chooses, with the Seconds of the step
        /// </summary>
        private static readonly int[] OfferedSeconds = { 3, 5, 10, 30 };

        /// <summary>
        /// The countdown closes this long before the end, so it is gone from the screen before the next step captures it
        /// </summary>
        private static readonly TimeSpan CountdownGoneBeforeEnd = TimeSpan.FromMilliseconds(250);

        private const string CancelKey = "cancel";

        public string Name { get; }
        public RecipeNodeConfig Config { get; }

        public DelayStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.Id ?? WellKnownStepTypes.Delay;
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) return;

            int seconds = Clamp(Config.GetParameter(SecondsParameter, DefaultSeconds));
            bool interactive = context.UserInteraction?.IsInteractive == true;

            if (interactive && Config.GetParameter(AskUserParameter, false))
            {
                int? chosen = await AskAsync(context, seconds, cancellationToken).ConfigureAwait(false);
                if (chosen == null)
                {
                    context.Abort("The user cancelled the delay.");
                    return;
                }
                seconds = chosen.Value;
            }

            context.Properties[OutputVariable] = seconds;
            context.LogStep($"[Delay] {seconds} s");
            if (seconds <= 0)
            {
                return;
            }

            if (interactive && Config.GetParameter(ShowCountdownParameter, true))
            {
                if (!await CountdownAsync(context, seconds, cancellationToken).ConfigureAwait(false))
                {
                    context.Abort("The user cancelled the delay.");
                }
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// The seconds to wait: 0 to <see cref="MaxSeconds"/>
        /// </summary>
        internal static int Clamp(int seconds) => Math.Max(0, Math.Min(MaxSeconds, seconds));

        /// <summary>
        /// The delays the user can choose: <see cref="OfferedSeconds"/> and the Seconds of the step, sorted
        /// </summary>
        internal static IReadOnlyList<int> ChoicesFor(int seconds) =>
            OfferedSeconds.Concat(seconds > 0 ? new[] { seconds } : Array.Empty<int>()).Distinct().OrderBy(s => s).ToList();

        /// <summary>
        /// Ask with the prompt of the UserPrompt step: one button per delay, the Seconds of the step is the default (Enter)
        /// </summary>
        /// <returns>The chosen seconds, null when the user cancelled</returns>
        private static async Task<int?> AskAsync(CaptureFlowContext context, int seconds, CancellationToken cancellationToken)
        {
            int defaultSeconds = seconds > 0 ? seconds : DefaultSeconds;
            var choices = ChoicesFor(defaultSeconds)
                .Select(s => new PromptChoice(s.ToString(CultureInfo.InvariantCulture), string.Format(Texts.Recipe.DelayChoiceSeconds, s),
                    s == defaultSeconds ? "Primary" : null, s == defaultSeconds, false))
                .Append(new PromptChoice(CancelKey, Texts.Core.Cancel, null, false, true))
                .ToList();

            string chosenKey = await context.Ui.InvokeAsync(() =>
            {
                var promptWindow = new RecipeUserPromptWindow(Texts.Recipe.DelayPromptTitle, Texts.Recipe.DelayPromptMessage, choices);
                // A cancelled flow closes the prompt, the close is posted to the UI thread
                using (cancellationToken.Register(() => context.Ui.InvokeAsync(() =>
                       {
                           if (promptWindow.IsVisible)
                           {
                               promptWindow.Close();
                           }
                       }, CancellationToken.None).FireAndLog("Close the delay prompt", Log)))
                {
                    promptWindow.ShowDialog();
                }

                cancellationToken.ThrowIfCancellationRequested();
                return promptWindow.SelectedChoiceKey;
            }, cancellationToken).ConfigureAwait(false);

            return int.TryParse(chosenKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out int chosen) ? Clamp(chosen) : null;
        }

        /// <summary>
        /// Show the countdown and wait, it closes shortly before the end
        /// </summary>
        /// <returns>False when the user cancelled</returns>
        private static async Task<bool> CountdownAsync(CaptureFlowContext context, int seconds, CancellationToken cancellationToken)
        {
            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            DelayCountdownWindow window = null;
            await context.Ui.InvokeAsync(() =>
            {
                window = new DelayCountdownWindow(seconds, () => cancelled.TrySetResult(true));
                window.ShowWithoutFocus();
            }, cancellationToken).ConfigureAwait(false);

            try
            {
                for (int left = seconds; left > 0; left--)
                {
                    var tick = Task.Delay(left > 1 ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(1) - CountdownGoneBeforeEnd, cancellationToken);
                    if (await Task.WhenAny(tick, cancelled.Task).ConfigureAwait(false) == cancelled.Task)
                    {
                        return false;
                    }
                    // Throws when the flow was cancelled
                    await tick.ConfigureAwait(false);
                    if (left > 1)
                    {
                        int secondsLeft = left - 1;
                        await context.Ui.InvokeAsync(() => window.SecondsLeft = secondsLeft, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                await context.Ui.InvokeAsync(() => window?.Close(), CancellationToken.None).ConfigureAwait(false);
            }

            await Task.Delay(CountdownGoneBeforeEnd, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
}
