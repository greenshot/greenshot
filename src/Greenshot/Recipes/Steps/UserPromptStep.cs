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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Expressions;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Recipes.Views;
using log4net;
using Greenshot.Base.Threading;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Interactive conditional pipeline step presenting a decision modal to the user.
    /// The user's selection determines which branch is activated in the DAG execution engine.
    /// </summary>
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

            // Capture preview image if enabled
            Image previewImg = null;
            bool disposePreview = false;
            if (showPreview)
            {
                if (context.Payload?.RawCapture?.Image != null)
                {
                    previewImg = context.Payload.RawCapture.Image;
                }
                else
                {
                    var surf = context.Payload?.EnsureSurface();
                    if (surf != null)
                    {
                        previewImg = surf.GetImageForExport();
                        disposePreview = true;
                    }
                }
            }

            string chosenKey;
            try
            {
                // The prompt is UI: shown on the UI thread, the flow waits without blocking
                chosenKey = await context.Ui.InvokeAsync(() =>
                {
                    var promptWindow = new RecipeUserPromptWindow(title, message, choices, previewImg, timeoutSeconds);
                    // A cancelled flow closes the prompt, the close is posted to the UI thread
                    using (cancellationToken.Register(() => context.Ui.InvokeAsync(() =>
                           {
                               if (promptWindow.IsVisible)
                               {
                                   promptWindow.Close();
                               }
                           }, CancellationToken.None).FireAndLog("Close the user prompt", Log)))
                    {
                        promptWindow.ShowDialog();
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    return promptWindow.SelectedChoiceKey ?? (choices.FirstOrDefault(c => c.IsDefault) ?? choices.FirstOrDefault())?.Key ?? "Yes";
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (disposePreview)
                {
                    previewImg?.Dispose();
                }
            }

            context.Properties["UserChoice." + Config.Id] = chosenKey;

            context.LogStep($"User Prompt '{title}' -> Selected choice: '{chosenKey}'");
            Log.InfoFormat("UserPromptStep [{0}] user selected choice '{1}'", Config.Id, chosenKey);
        }

        /// <summary>
        /// The choices, a label can be an expression (the choices are read as their type, so they are not evaluated with the other parameters)
        /// </summary>
        private List<PromptChoice> ReadChoices(CaptureFlowContext context)
        {
            var choices = new List<PromptChoice>();
            foreach (var choice in Config.GetParameter<List<PromptChoice>>("Choices") ?? new List<PromptChoice>())
            {
                if (string.IsNullOrEmpty(choice?.Key)) continue;
                string label = choice.Label;
                if (label != null && label.IndexOf("${", StringComparison.Ordinal) >= 0)
                {
                    label = ExpressionEvaluator.Instance.Evaluate<string>(label, context, label);
                }
                choices.Add(new PromptChoice(choice.Key, label ?? choice.Key, choice.Style, choice.IsDefault, choice.IsCancel));
            }
            return choices;
        }
    }
}
