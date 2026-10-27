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

using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Expressions;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Recipes.Steps
{
    [StepInfo(WellKnownStepTypes.UserPrompt, "User Prompt", "Asks the user to choose; conditional transitions of this node follow the chosen key.", "Interaction")]
    [StepParameter("Choices", ContractDataType.Object, ValueType = typeof(List<PromptChoice>), Required = true, Description = "The choices (key and text); the one with IsDefault is the answer to Enter and when the prompt times out")]
    [StepParameter("Title", ContractDataType.String, Description = "Title of the prompt")]
    [StepParameter("Message", ContractDataType.String, Description = "Message of the prompt")]
    [StepParameter("ShowPreview", ContractDataType.Boolean, DefaultValue = true, Description = "Show a preview of the capture")]
    [StepParameter("TimeoutSeconds", ContractDataType.Integer, DefaultValue = 0, Description = "Close the prompt after this many seconds (0: never)")]
    [StepOutputVariable("UserChoice.{NodeId}", ContractDataType.String, "The key of the chosen choice")]
    public class UserPromptStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UserPromptStep));

        public string Name { get; }

        public RecipeNodeConfig Config { get; }

        public UserPromptStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "UserPromptStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            string title = Config.GetParameter<string>("Title") ?? "Decision Required";
            string message = Config.GetParameter<string>("Message") ?? "Please select how you would like to proceed with this capture:";
            bool showPreview = Config.GetParameter("ShowPreview", true);
            int timeoutSeconds = Config.GetParameter("TimeoutSeconds", 0);
            var choices = ReadChoices(context);
            var fallback = (choices.FirstOrDefault(c => c.IsDefault) ?? choices.FirstOrDefault())?.Key ?? "Yes";

            string chosenKey = fallback;
            var host = RecipeServices.Host;
            if (host == null || !context.UserInteraction.IsInteractive)
            {
                throw new InteractionRequiredException(title);
            }

            var preview = showPreview ? context.Payload?.Image : null;
            chosenKey = await host.PromptAsync(title, message, choices, preview, timeoutSeconds, cancellationToken).ConfigureAwait(false) ?? fallback;
            context.Properties["UserChoice." + Config.Id] = chosenKey;
            context.LogStep($"User Prompt '{title}' -> Selected choice: '{chosenKey}'");
            Log.InfoFormat("UserPromptStep [{0}] user selected choice '{1}'", Config.Id, chosenKey);
        }

        private List<PromptChoice> ReadChoices(CaptureFlowContext context)
        {
            var choices = new List<PromptChoice>();
            foreach (var choice in Config.GetParameter<List<PromptChoice>>("Choices") ?? new List<PromptChoice>())
            {
                if (string.IsNullOrEmpty(choice?.Key)) continue;
                string label = choice.Label;
                if (label != null && label.Contains("${", StringComparison.Ordinal))
                {
                    label = ExpressionEvaluator.Instance.Evaluate<string>(label, context, label);
                }

                choices.Add(new PromptChoice(choice.Key, label ?? choice.Key, choice.Style, choice.IsDefault, choice.IsCancel));
            }

            return choices;
        }
    }
}
