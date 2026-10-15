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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using log4net;
#if !GREENSHOT_LIGHT
using Greenshot.Notifications;
#endif

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Pipeline step that shows the completion notification (toast or tray balloon) for the exports before it.
    /// Dynamically evaluates whether notifications are enabled based on CoreConfig if not pre-defined.
    /// </summary>
    [StepInfo(WellKnownStepTypes.Notification, "Notification", "Shows the notification for the exports before it.", "Feedback")]
    [StepParameter("ShowNotification", ContractDataType.Boolean, Description = "Show the completion notification (default: settings)")]
    [StepInputVariable("ShowNotification", ContractDataType.Boolean, Description = "Overrides the ShowNotification parameter")]
    [StepOutputVariable("EnableCompletionNotification", ContractDataType.Boolean, "Whether the completion notification is shown")]
    public class NotificationStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NotificationStep));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

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

            // The exports since the previous Notification step (of all destination and plugin steps, also of parallel branches),
            // one notification shows them
            var exports = context.GetExportLog().TakeUnnotified();
            var shown = showNotification ? GetShownExports(exports) : Array.Empty<ExportOutcome>();
            if (shown.Count == 0)
            {
                return;
            }

            IReadOnlyDictionary<string, string> iconPaths = null;
            string captureId = null;
            string thumbnailPath = null;
            var last = shown[shown.Count - 1];
#if !GREENSHOT_LIGHT
            // The icons of the destinations as files: the toast shows one row per destination with its icon
            iconPaths = await NotificationIcons.GetIconPathsAsync(shown.Select(e => e.Destination), cancellationToken).ConfigureAwait(false);
            // What went to the (last) destination is kept, a click on the toast offers it in the destination picker, also after a
            // restart. Before the toast only its thumbnail is written, the capture itself after it.
            if (last.Source != null && NotificationCaptures.KeepsCaptures)
            {
                captureId = NotificationCaptures.NewId();
                if (CoreConfig.ShowNotificationThumbnail)
                {
                    thumbnailPath = await NotificationCaptures.WriteThumbnailAsync(captureId, last.Source, cancellationToken).ConfigureAwait(false);
                }
            }
#endif
            var notification = CreateNotification(shown, iconPaths, captureId, thumbnailPath);
            await context.Ui.InvokeAsync(() => UserInteraction.Current.NotifyAsync(notification).FireAndLog("Export notification", Log), cancellationToken).ConfigureAwait(false);
#if !GREENSHOT_LIGHT
            if (captureId != null)
            {
                await NotificationCaptures.WriteCaptureAsync(captureId, last.Source, cancellationToken).ConfigureAwait(false);
            }
#endif
        }

        /// <summary>
        /// The exports a notification shows: those with something to tell (see <see cref="ExportResultHandler.GetMessage"/>), none when
        /// the notification level doesn't show their kind (then nothing is kept for them either)
        /// </summary>
        internal static IReadOnlyList<ExportOutcome> GetShownExports(IEnumerable<ExportOutcome> exports)
        {
            var shown = exports.Where(e => GetMessage(e) != null).ToList();
            if (shown.Count == 0 || !CoreConfig.ShowTrayNotification || !CoreConfig.NotificationLevel.Shows(GetKind(shown)))
            {
                return Array.Empty<ExportOutcome>();
            }
            return shown;
        }

        private static string GetMessage(ExportOutcome export) => ExportResultHandler.GetMessage(export.Result, export.Target);

        internal static NotificationKind GetKind(IEnumerable<ExportOutcome> shown) =>
            shown.Any(e => e.Result.Status == ExportStatus.Failed) ? NotificationKind.Error : NotificationKind.Info;

        /// <summary>
        /// One notification for the exports: a line per export, with the icon of its destination
        /// </summary>
        /// <param name="shown">The exports, see <see cref="GetShownExports"/></param>
        /// <param name="iconPaths">The icon files of the destinations by their icon key, null or empty for no icons</param>
        /// <param name="captureId">The capture kept for the notification, which a click offers in the destination picker, or null</param>
        /// <param name="thumbnailPath">The thumbnail of the capture, or null</param>
        internal static Notification CreateNotification(IReadOnlyList<ExportOutcome> shown, IReadOnlyDictionary<string, string> iconPaths, string captureId, string thumbnailPath)
        {
            var last = shown[shown.Count - 1];
            var lines = shown.Select(e =>
            {
                string iconPath = null;
                var iconKey = e.Destination.Descriptor?.IconKey;
                if (iconKey != null)
                {
                    iconPaths?.TryGetValue(iconKey, out iconPath);
                }
                return new NotificationLine(GetMessage(e), iconPath);
            }).ToList();

            // Each export has its own file or link
            var savedFile = shown.LastOrDefault(e => e.Result.IsSucceeded && e.Result.Uri == null && !string.IsNullOrEmpty(e.Result.FilePath))?.Result.FilePath;
            var uploadUrl = shown.LastOrDefault(e => e.Result.IsSucceeded && e.Result.Uri != null)?.Result.Uri.AbsoluteUri;
            // Without a toast (tray balloon, Light) a click opens the saved file or the upload, the one which was exported last
            var opens = shown.LastOrDefault(e => e.Result.IsSucceeded && (e.Result.Uri != null || !string.IsNullOrEmpty(e.Result.FilePath)))?.Result;
            IReadOnlyList<NotificationButton> buttons = null;
#if !GREENSHOT_LIGHT
            // Only the toasts show them, the balloons of the tray icon (Light) don't
            buttons = NotificationButtons.Create(captureId, savedFile, uploadUrl);
#endif
            // Above the lines: what was captured (e.g. the title of the window), else Greenshot
            var title = last.CaptureDetails?.Title;
            return new Notification(GetKind(shown),
                string.Join(Environment.NewLine, lines.Select(l => l.Text)), TimeSpan.FromHours(1),
                opens == null ? (Action)null : () => Open(opens), captureId, thumbnailPath,
                string.IsNullOrWhiteSpace(title) ? "Greenshot" : title, lines, buttons);
        }

        private static void Open(ExportResult result)
        {
            if (result.Uri != null)
            {
                Process.Start(result.Uri.AbsoluteUri);
            }
            else
            {
                ExplorerHelper.OpenInExplorer(result.FilePath);
            }
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
