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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Expressions;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Recipes.Pipeline;
using log4net;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Pipeline step that shows the completion notification (toast or tray balloon) for the exports before it.
    /// Dynamically evaluates whether notifications are enabled based on CoreConfig if not pre-defined.
    /// </summary>
    [StepInfo(WellKnownStepTypes.Notification, "Notification", "Shows the notification for the exports before it, and the message when it has one.", "Feedback")]
    [StepParameter(MessageParameter, ContractDataType.String, Purpose = TemplatePurpose.Text, Translatable = true, Description = "A message for the user, e.g. Customer number ${var.CustomerNumber} copied: a line after the exports, or a notification of its own when there were none")]
    [StepParameter("ShowNotification", ContractDataType.Boolean, Description = "Show the completion notification (default: settings)")]
    [StepParameter(NoHdrNoteParameter, ContractDataType.Boolean, DefaultValue = false, Description = "Say in the notification when the capture has no HDR content (payload.hdr is None: not on an HDR display, or nothing brighter than SDR white or outside sRGB), e.g. after a SaveFile with HdrFormat")]
    [StepInputVariable("ShowNotification", ContractDataType.Boolean, Description = "Overrides the ShowNotification parameter")]
    [StepOutputVariable("EnableCompletionNotification", ContractDataType.Boolean, "Whether the completion notification is shown")]
    public class NotificationStep : ICaptureStep
    {
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
        private static readonly ILog Log = LogManager.GetLogger(typeof(NotificationStep));

        public string Name { get; }
        public RecipeNodeConfig Config { get; }

        public NotificationStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "NotificationStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            bool showNotification = ResolveShowNotification(context);
            context.Properties["EnableCompletionNotification"] = showNotification;

            if (!showNotification)
            {
                // The exports before this step are done with, a later Notification step doesn't show them
                context.GetExportLog().TakeUnnotified();
                return;
            }
            // The exports since the previous Notification step (of all destination and plugin steps, also of parallel branches),
            // one notification shows them
            string message = Config.GetParameter<string>(MessageParameter);
            bool shown = await ExportNotification.ShowAsync(context, null, cancellationToken, GetNote(context, message)).ConfigureAwait(false);
            if (!shown && !string.IsNullOrWhiteSpace(message) && CoreConfig.NotificationLevel.Shows(NotificationKind.Info))
            {
                // No exports to show (e.g. a recipe which only copies a text): the message on its own
                var title = context.Payload?.RawCapture?.CaptureDetails?.Title;
                var notification = new Notification(NotificationKind.Info, message, TimeSpan.FromHours(1), title: string.IsNullOrWhiteSpace(title) ? "Greenshot" : title);
                await context.Ui.InvokeAsync(() => UserInteraction.Current.NotifyAsync(notification).FireAndLog("Recipe notification", Log), cancellationToken).ConfigureAwait(false);
            }
        }

        public const string NoHdrNoteParameter = "NoHdrNote";

        public const string MessageParameter = "Message";

        /// <summary>
        /// The lines after the exports: the one about the capture and the message, null for none
        /// </summary>
        internal string GetNote(CaptureFlowContext context, string message = null)
        {
            bool noHdrNote = Config.GetParameter(NoHdrNoteParameter, false);
            string hdrNote = noHdrNote && HdrOutput.StateOf(context.Payload) == HdrOutput.StateNone ? Texts.Core.NotificationNoHdr : null;
            var lines = new[] { hdrNote, string.IsNullOrWhiteSpace(message) ? null : message.Trim() }.Where(line => line != null).ToList();
            return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
        }

        private bool ResolveShowNotification(CaptureFlowContext context)
        {
            // Priority 1: Context override
            if (context.Properties.TryGetValue("ShowNotification", out var ctxVal) && ctxVal is bool ctxBool)
            {
                return ctxBool;
            }

            // Priority 2: Step parameter pre-definition
            if (Config.Parameters.TryGetValue("ShowNotification", out var stepVal) && stepVal != null)
            {
                if (stepVal is bool stepBool) return stepBool;
                if (bool.TryParse(stepVal.ToString(), out bool parsedBool)) return parsedBool;
            }

            // Priority 3: Dynamic user configuration evaluation
            return CoreConfig.ShowTrayNotification && !CoreConfig.HideTrayicon;
        }
    }
}
