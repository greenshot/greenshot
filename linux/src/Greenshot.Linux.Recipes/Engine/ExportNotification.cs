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

using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Recipes.Pipeline
{
    /// <summary>
    /// One notification for the exports since the last one (of all destination and plugin steps, also of parallel branches),
    /// with the error of a failed flow
    /// </summary>
    public static class ExportNotification
    {
        public const string FailureShownProperty = "Notification.FailureShown";

        public static async Task<bool> ShowAsync(CaptureFlowContext context, string flowError, CancellationToken cancellationToken)
        {
            var exports = context.GetExportLog().TakeUnnotified();
            var host = RecipeServices.Host;
            if (host == null || exports.Count == 0 && string.IsNullOrEmpty(flowError))
            {
                return false;
            }

            return await host.NotifyExportsAsync(exports, flowError, cancellationToken).ConfigureAwait(false);
        }
    }
}
