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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.UI;
using log4net;
using Newtonsoft.Json.Linq;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Interactive conditional pipeline step presenting a decision modal to the user.
    /// The user's selection determines which branch is activated in the DAG execution engine.
    /// </summary>
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
            string defaultChoice = Config.GetParameter<string>("DefaultChoice");

            var choices = ParseChoices(Config.GetParameter<object>("Choices") ?? Config.GetParameter<object>("choices"));

            // Capture preview image if enabled
            Image previewImg = null;
            bool disposePreview = false;
            if (showPreview)
            {
                if (context.Payload?.SharedRenderedBitmap != null)
                {
                    previewImg = context.Payload.SharedRenderedBitmap;
                }
                else if (context.Payload?.RawCapture?.Image != null)
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

            var tcs = new TaskCompletionSource<string>();
            var uiContext = SimpleServiceProvider.Current.GetInstance<SynchronizationContext>(isOptional: true) ?? SynchronizationContext.Current;

            void ShowDialogOnUi()
            {
                try
                {
                    var promptWindow = new RecipeUserPromptWindow(title, message, choices, previewImg, timeoutSeconds, defaultChoice);
                    promptWindow.ShowDialog();
                    string selected = promptWindow.SelectedChoiceKey ?? defaultChoice ?? (choices.FirstOrDefault()?.Key ?? "Yes");
                    tcs.SetResult(selected);
                }
                catch (Exception ex)
                {
                    Log.Error("Error displaying RecipeUserPromptWindow", ex);
                    tcs.SetException(ex);
                }
                finally
                {
                    if (disposePreview)
                    {
                        previewImg?.Dispose();
                    }
                }
            }

            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(ShowDialogOnUi);
            }
            else if (uiContext != null && SynchronizationContext.Current != uiContext)
            {
                uiContext.Send(_ => ShowDialogOnUi(), null);
            }
            else
            {
                ShowDialogOnUi();
            }

            string chosenKey = await tcs.Task.ConfigureAwait(false);

            context.Properties["UserPrompt.Choice." + Config.Id] = chosenKey;
            context.Properties["UserChoice." + Config.Id] = chosenKey;
            context.Properties["LastUserChoice"] = chosenKey;

            context.LogStep($"User Prompt '{title}' -> Selected choice: '{chosenKey}'");
            Log.InfoFormat("UserPromptStep [{0}] user selected choice '{1}'", Config.Id, chosenKey);
        }

        private static List<PromptChoice> ParseChoices(object choicesObj)
        {
            var list = new List<PromptChoice>();
            if (choicesObj == null) return list;

            if (choicesObj is IEnumerable<PromptChoice> typedList)
            {
                list.AddRange(typedList);
                return list;
            }

            if (choicesObj is IEnumerable enumerable && !(choicesObj is string))
            {
                foreach (var item in enumerable)
                {
                    if (item is PromptChoice pc)
                    {
                        list.Add(pc);
                    }
                    else if (item is IDictionary dict)
                    {
                        string k = dict.Contains("Key") ? dict["Key"]?.ToString() : (dict.Contains("key") ? dict["key"]?.ToString() : null);
                        string l = dict.Contains("Label") ? dict["Label"]?.ToString() : (dict.Contains("label") ? dict["label"]?.ToString() : null);
                        string s = dict.Contains("Style") ? dict["Style"]?.ToString() : (dict.Contains("style") ? dict["style"]?.ToString() : "Primary");
                        bool isDef = dict.Contains("IsDefault") && Convert.ToBoolean(dict["IsDefault"]);
                        bool isCanc = dict.Contains("IsCancel") && Convert.ToBoolean(dict["IsCancel"]);
                        if (!string.IsNullOrEmpty(k))
                        {
                            list.Add(new PromptChoice(k, l ?? k, s, isDef, isCanc));
                        }
                    }
                    else if (item is JObject jobj)
                    {
                        string k = jobj.Value<string>("Key") ?? jobj.Value<string>("key");
                        string l = jobj.Value<string>("Label") ?? jobj.Value<string>("label");
                        string s = jobj.Value<string>("Style") ?? jobj.Value<string>("style") ?? "Primary";
                        bool isDef = jobj.Value<bool?>("IsDefault") ?? jobj.Value<bool?>("isDefault") ?? false;
                        bool isCanc = jobj.Value<bool?>("IsCancel") ?? jobj.Value<bool?>("isCancel") ?? false;
                        if (!string.IsNullOrEmpty(k))
                        {
                            list.Add(new PromptChoice(k, l ?? k, s, isDef, isCanc));
                        }
                    }
                }
            }

            return list;
        }
    }
}
