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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Pipeline step reading the text of the capture with OCR.
    /// </summary>
    [StepInfo(WellKnownStepTypes.Ocr, "OCR", "Reads the text of the capture with OCR (Windows OCR). The text lines are kept with the capture, the text goes into the variable OcrText.", "Analysis")]
    [StepPayload(RawCapture = PayloadRequirement.Required, ExtractedText = PayloadRequirement.Created)]
    [StepParameter(LanguageParameter, ContractDataType.String, Description = "The OCR language, e.g. de-DE (default: the OCR language of the settings); an installed Windows OCR language")]
    [StepOutputVariable("OcrText", ContractDataType.String, "The text of the capture, one line per text line (also in Payload.ExtractedText)")]
    public class OcrStep : ICaptureStep
    {
        /// <summary>
        /// The OCR language of the step, also used by the steps which run OCR themselves
        /// </summary>
        public const string LanguageParameter = "Language";

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
            context.Properties["OcrText"] = await ExtractTextAsync(context, cancellationToken, Config.GetParameter<string>(LanguageParameter)).ConfigureAwait(false);
        }

        /// <summary>
        /// Puts the OCR text of the (selected part of the) capture into Payload.ExtractedText, and returns it.
        /// The lines OCR found while selecting are used, otherwise OCR runs now.
        /// Where the text goes (clipboard, stdout, ...) is up to the following steps.
        /// </summary>
        /// <param name="context">the flow</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <param name="languageTag">the OCR language when OCR runs now, null: the setting</param>
        internal static async Task<string> ExtractTextAsync(CaptureFlowContext context, CancellationToken cancellationToken, string languageTag = null)
        {
            var rawCapture = context.Payload?.RawCapture;
            var captureDetails = rawCapture?.CaptureDetails;
            if (captureDetails == null) return string.Empty;

            var ocrLines = await OcrLines.GetAsync(captureDetails, rawCapture.Image, languageTag, context.UserInteraction, cancellationToken).ConfigureAwait(false);
            if (ocrLines == null || !ocrLines.Any()) return string.Empty;

            var bounds = rawCapture.Image != null
                ? new NativeRect(0, 0, rawCapture.Image.Width, rawCapture.Image.Height)
                : NativeRect.Empty;

            var textResult = new StringBuilder();

            foreach (var line in ocrLines)
            {
                if (!bounds.IsEmpty && !line.Box.IntersectsWith(bounds)) continue;

                if (line.Words != null && line.Words.Count > 0)
                {
                    bool lineHasWords = false;
                    for (var i = 0; i < line.Words.Count; i++)
                    {
                        var word = line.Words[i];
                        if (!bounds.IsEmpty && !word.Box.IntersectsWith(bounds)) continue;
                        if (lineHasWords && word.Text.Length > 0)
                        {
                            textResult.Append(' ');
                        }
                        textResult.Append(word.Text);
                        lineHasWords = true;
                    }
                    if (lineHasWords)
                    {
                        textResult.AppendLine();
                    }
                }
                else if (!string.IsNullOrEmpty(line.Text))
                {
                    textResult.AppendLine(line.Text);
                }
            }

            string extracted = textResult.ToString().TrimEnd();
            context.Payload.ExtractedText = extracted;
            return extracted;
        }
    }
}
