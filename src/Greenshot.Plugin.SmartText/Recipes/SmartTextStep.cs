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
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.SmartText.Detection;
using Greenshot.Plugin.SmartText.Views;
using log4net;

namespace Greenshot.Plugin.SmartText.Recipes
{
    /// <summary>
    /// The types of the SmartText step parameters which hold more than one value, read strictly like the rest of a recipe. The
    /// patterns are written as in the .gssmarttext.json files (camelCase, the names are fixed on the properties), so a pattern can be copied from a file into a recipe.
    /// </summary>
    [JsonSerializable(typeof(List<SmartTextPattern>))]
    [JsonSerializable(typeof(SmartTextArea))]
    internal partial class SmartTextJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// Finds data in the OCR text of the capture (web addresses, e-mail addresses, phone numbers, own patterns) and gives the
    /// picked value to the following steps as var.&lt;ResultName&gt;. See docs/vNext/smart-text.md.
    /// </summary>
    [StepInfo(StepType, "Smart text", "Finds data in the OCR text: web addresses, e-mail addresses, phone numbers and your own patterns. Runs OCR when the capture has no text yet.", "Analysis")]
    [StepPayload(RawCapture = PayloadRequirement.Required)]
    [StepParameter(DetectorsParameter, ContractDataType.Object, ValueType = typeof(List<string>), SupportsExpressions = false,
        Description = "Which detectors: ids of the built-in ones (Url, Email, Phone, Iban, Date, IssueKey, ...) and names of own patterns; default all which are on in the settings (none when Patterns is set)")]
    [StepParameter(PatternsParameter, ContractDataType.Object, ValueType = typeof(List<SmartTextPattern>), SupportsExpressions = false,
        Description = "Own patterns of this recipe, the fields of a .gssmarttext.json file (name, pattern, label, labelPosition, area, digitLookAlikes, normalize, validate, ...) without openUrl")]
    [StepParameter(PickParameter, ContractDataType.Enum, DefaultValue = PickAskIfMore, AllowedValues = new[] { PickFirst, PickAll, PickAsk, PickAskIfMore }, SupportsExpressions = false,
        Description = "First: the first item; All: all items; Ask: the user picks in a window; AskIfMore: the window only when there is more than one. Without a user Ask and AskIfMore take the first item")]
    [StepParameter(AreaParameter, ContractDataType.Object, ValueType = typeof(SmartTextArea), SupportsExpressions = false,
        Description = "Look only here, as fractions of the capture: {\"left\": 0.5, \"top\": 0, \"width\": 0.5, \"height\": 0.3}; it replaces the area of the patterns")]
    [StepParameter(LanguageParameter, ContractDataType.String, Description = "The OCR language when OCR runs here, e.g. de-DE (default: the OCR language of the settings)")]
    [StepParameter(ResultNameParameter, ContractDataType.String, DefaultValue = DefaultResultName, SupportsExpressions = false,
        Description = "The results are var.<ResultName>, .text, .kind, .count, .all and .picked; default Found")]
    [StepOutputVariable("{Parameter:ResultName|Found}", ContractDataType.String, "The value of the (first) picked item, e.g. a phone number without spaces")]
    [StepOutputVariable("{Parameter:ResultName|Found}.text", ContractDataType.String, "The picked item as OCR read it")]
    [StepOutputVariable("{Parameter:ResultName|Found}.kind", ContractDataType.String, "The detector of the picked item: a built-in id (e.g. Phone) or the name of the own pattern")]
    [StepOutputVariable("{Parameter:ResultName|Found}.count", ContractDataType.Integer, "How many items were found, 0: nothing")]
    [StepOutputVariable("{Parameter:ResultName|Found}.picked", ContractDataType.Integer, "How many items were picked, 0: none (nothing found, or the window was closed)")]
    [StepOutputVariable("{Parameter:ResultName|Found}.all", ContractDataType.String, "All picked values, one per line")]
    public class SmartTextStep : ICaptureStep
    {
        public const string StepType = "SmartText";
        public const string DetectorsParameter = "Detectors";
        public const string PatternsParameter = "Patterns";
        public const string PickParameter = "Pick";
        public const string AreaParameter = "Area";
        public const string LanguageParameter = "Language";
        public const string ResultNameParameter = "ResultName";
        public const string DefaultResultName = "Found";
        public const string PickFirst = "First";
        public const string PickAll = "All";
        public const string PickAsk = "Ask";
        public const string PickAskIfMore = "AskIfMore";

        private static readonly ILog Log = LogManager.GetLogger(typeof(SmartTextStep));

        /// <summary>
        /// The JSON types of the parameters, registered with the step
        /// </summary>
        internal static SmartTextJsonContext JsonTypes => SmartTextJsonContext.Default;

        public string Name { get; }
        public RecipeNodeConfig Config { get; }

        public SmartTextStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? StepType;
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            string resultName = Parameter(ResultNameParameter) ?? DefaultResultName;
            SetResults(context, resultName, Array.Empty<SmartTextItem>(), 0);

            var rawCapture = context.Payload?.RawCapture;
            var captureDetails = rawCapture?.CaptureDetails;
            if (captureDetails == null || rawCapture.Image == null)
            {
                context.LogStep("Smart text skipped: there is no capture.");
                return;
            }

            var inline = Config.GetParameter<List<SmartTextPattern>>(PatternsParameter) ?? new List<SmartTextPattern>();
            foreach (var pattern in inline)
            {
                var problems = SmartTextPatternCheck.Problems(pattern, allowOpenUrl: false);
                if (problems.Count > 0)
                {
                    // A recipe mistake: the flow stops and says why, the value of the pattern isn't secret
                    context.Fail($"Smart text: the pattern '{pattern?.Name}' can't be used: {string.Join(" ", problems)}");
                    return;
                }
            }

            string pick = Choice(PickParameter, PickAskIfMore, new[] { PickFirst, PickAll, PickAsk, PickAskIfMore });
            var names = Config.GetParameter<List<string>>(DetectorsParameter);
            var area = Config.GetParameter<SmartTextArea>(AreaParameter);
            var detectors = SmartTextDefinitions.Detectors(names, inline, out var unknown, area: area);
            foreach (string name in unknown)
            {
                context.LogStep($"Smart text: there is no detector '{name}' (built-in id or own pattern of the settings).");
                Log.WarnFormat("SmartText step '{0}': there is no detector '{1}'", Config.Id, name);
            }

            context.State = CaptureFlowState.Processing;
            string language = Parameter(LanguageParameter);
            var lines = await OcrLines.GetAsync(captureDetails, rawCapture.Image, language, cancellationToken).ConfigureAwait(false);
            var layout = TextLayout.From(lines, new NativeRect(0, 0, rawCapture.Image.Width, rawCapture.Image.Height));
            var items = SmartTextScanner.Scan(layout, detectors);
            Remember(captureDetails, items);

            // Only how many: the values come from the capture and stay out of the log
            context.LogStep($"Smart text found {items.Count} item(s) with {detectors.Count} detector(s).");

            IReadOnlyList<SmartTextItem> picked;
            bool canAsk = context.UserInteraction?.IsInteractive == true;
            if (items.Count == 0)
            {
                picked = items;
            }
            else if (pick == PickAll)
            {
                picked = items;
            }
            else if (pick == PickFirst || !canAsk || (pick == PickAskIfMore && items.Count == 1))
            {
                picked = new[] { items[0] };
            }
            else
            {
                picked = await context.Ui.InvokeAsync(() => SmartTextWindow.Pick(rawCapture.Image, layout, items, cancellationToken), cancellationToken).ConfigureAwait(false)
                         ?? Array.Empty<SmartTextItem>();
            }

            SetResults(context, resultName, picked, items.Count);
            context.LogStep($"Smart text: {picked.Count} item(s) picked.");
        }

        /// <summary>
        /// The results of the step: the first picked item, how many there were and all picked values, one per line
        /// </summary>
        internal static void SetResults(CaptureFlowContext context, string resultName, IReadOnlyList<SmartTextItem> picked, int count)
        {
            var first = picked.FirstOrDefault();
            context.Properties[resultName] = first?.Value ?? string.Empty;
            context.Properties[resultName + ".text"] = first?.ReadText ?? string.Empty;
            context.Properties[resultName + ".kind"] = first?.DetectorId ?? string.Empty;
            context.Properties[resultName + ".count"] = count;
            context.Properties[resultName + ".picked"] = picked.Count;
            context.Properties[resultName + ".all"] = string.Join(Environment.NewLine, picked.Select(item => item.Value));
        }

        /// <summary>
        /// The items are kept with the capture like the barcodes, the ones of an earlier run are replaced
        /// </summary>
        internal static void Remember(ICaptureDetails captureDetails, IReadOnlyList<SmartTextItem> items)
        {
            var features = captureDetails?.Features;
            if (features == null) return;
            lock (features)
            {
                features.RemoveAll(feature => feature is SmartTextItem);
                features.AddRange(items);
            }
        }

        private string Parameter(string name)
        {
            string value = Config.GetParameter<object>(name)?.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private string Choice(string name, string defaultValue, IReadOnlyList<string> allowed)
        {
            string value = Parameter(name) ?? defaultValue;
            return allowed.FirstOrDefault(a => string.Equals(a, value, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"SmartText: '{value}' is not a value of {name}, use one of {string.Join(", ", allowed)}.");
        }
    }
}
