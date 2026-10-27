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

using System.Text;
using Greenshot.Linux.Core.Ocr;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Recipes.Steps
{
    [StepInfo(WellKnownStepTypes.Ocr, "OCR", "Reads the text of the capture with OCR (Windows OCR). The text lines are kept with the capture, the text goes into the variable OcrText.", "Analysis")]
    [StepPayload(RawCapture = PayloadRequirement.Required, ExtractedText = PayloadRequirement.Created)]
    [StepOutputVariable("OcrText", ContractDataType.String, "The text of the capture, one line per text line (also in Payload.ExtractedText)")]
    public class OcrStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OcrStep));

        /// <summary>
        /// The text lines of the capture in the metadata of the payload, recognized once for all steps
        /// </summary>
        public const string OcrLinesKey = "OcrLines";

        public string Name { get; }

        public RecipeNodeConfig Config { get; }

        public OcrStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "OcrStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context.Payload?.RawCapture == null)
            {
                context.LogStep("OCR skipped: there is no capture.");
                return;
            }

            context.State = CaptureFlowState.Processing;
            context.Properties["OcrText"] = await ExtractTextAsync(context, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// The text lines of the current image of the payload (tesseract), kept in the payload
        /// </summary>
        internal static async Task<IReadOnlyList<OcrLine>> GetLinesAsync(CaptureFlowContext context, CancellationToken cancellationToken)
        {
            var payload = context.Payload;
            if (payload?.Image == null)
            {
                return Array.Empty<OcrLine>();
            }

            if (payload.Metadata.TryGetValue(OcrLinesKey, out var known) && known is IReadOnlyList<OcrLine> knownLines)
            {
                return knownLines;
            }

            IReadOnlyList<OcrLine> lines = Array.Empty<OcrLine>();
            var host = RecipeServices.Host;
            if (host != null)
            {
                try
                {
                    lines = await host.RecognizeTextAsync(payload.Image, cancellationToken).ConfigureAwait(false) ?? Array.Empty<OcrLine>();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // OCR is optional (tesseract not installed): without it there is simply no text
                    Log.Warn("Failed to run OCR", ex);
                }
            }

            payload.Metadata[OcrLinesKey] = lines;
            return lines;
        }

        internal static async Task<string> ExtractTextAsync(CaptureFlowContext context, CancellationToken cancellationToken)
        {
            var lines = await GetLinesAsync(context, cancellationToken).ConfigureAwait(false);
            var text = new StringBuilder();
            foreach (var line in lines)
            {
                if (line.Words.Count > 0)
                {
                    text.AppendLine(string.Join(" ", line.Words.Select(word => word.Text).Where(word => word.Length > 0)));
                }
                else if (!string.IsNullOrEmpty(line.Text))
                {
                    text.AppendLine(line.Text);
                }
            }

            string extracted = text.ToString().TrimEnd();
            if (context.Payload != null)
            {
                context.Payload.ExtractedText = extracted;
            }

            return extracted;
        }
    }
}
