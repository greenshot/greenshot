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

using System.Collections.Generic;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;

namespace Greenshot.Recipes
{
    /// <summary>
    /// The extensions Greenshot brings (also Greenshot Light): border, drop shadow and caption on every capture, all switched
    /// off until the user switches them on (Settings > Recipes or the quick settings). They run per destination
    /// (BeforeDestination), so the user can pick the destinations they are for, e.g. a border only for email.
    /// Built only from existing steps; the texts are language keys.
    /// </summary>
    public static class BuiltInExtensions
    {
        public const string BorderId = "ext_border";
        public const string DropShadowId = "ext_dropshadow";
        public const string CaptionId = "ext_caption";

        /// <summary>
        /// Order on the slot: the caption bar is part of the image, the border goes around it, the shadow falls outside the border
        /// </summary>
        public const int CaptionOrder = 100;
        public const int BorderOrder = 200;
        public const int DropShadowOrder = 300;

        public static IReadOnlyList<RecipeExtension> Create() => new[] { CreateBorder(), CreateDropShadow(), CreateCaption() };

        private static RecipeExtension CreateExtension(string id, string name, string description, int order)
        {
            return new RecipeExtension(id, name, description)
            {
                IsBuiltIn = true,
                Extends = new ExtensionTarget
                {
                    Recipes = new List<string> { RecipeExtension.TargetCaptures },
                    Slot = RecipeSlots.BeforeDestination,
                    Order = order
                }
            };
        }

        private static RecipeOption Switch(string label) => new RecipeOption
        {
            Key = RecipeExtension.EnabledOptionKey,
            Type = ContractDataType.Boolean,
            DefaultValue = false,
            Label = label,
            QuickSettings = true
        };

        private static RecipeOption Number(string key, string label, int defaultValue, int min, int max) => new RecipeOption
        {
            Key = key,
            Type = ContractDataType.Integer,
            DefaultValue = defaultValue,
            Min = min,
            Max = max,
            Label = label,
            EnabledWhen = RecipeExtension.EnabledOptionKey
        };

        private static RecipeOption Color(string key, string label, string defaultValue) => new RecipeOption
        {
            Key = key,
            Type = ContractDataType.Color,
            DefaultValue = defaultValue,
            Label = label,
            EnabledWhen = RecipeExtension.EnabledOptionKey
        };

        /// <summary>
        /// A border around the capture (#1239, #1016, #696, #591): width and color
        /// </summary>
        public static RecipeExtension CreateBorder()
        {
            var extension = CreateExtension(BorderId, "recipe_extension_border", "recipe_extension_border_description", BorderOrder)
                .AddOption(Switch("recipe_extension_border_enabled"))
                .AddOption(Number("width", "recipe_extension_border_width", 2, 1, 50))
                .AddOption(Color("color", "recipe_extension_border_color", "#000000"))
                .AddNode(new RecipeNodeConfig("border", WellKnownStepTypes.Effect, "Border")
                    .Set("Effect", "Border")
                    .Set("Width", "${option.width}")
                    .Set("Color", "${option.color}"));
            extension.Flow = new RecipeFlowConfig("border");
            return extension;
        }

        /// <summary>
        /// A drop shadow (#591): size, darkness and offset
        /// </summary>
        public static RecipeExtension CreateDropShadow()
        {
            var darkness = new RecipeOption
            {
                Key = "darkness",
                Type = ContractDataType.Decimal,
                DefaultValue = 0.6,
                Min = 0.1m,
                Max = 1m,
                Label = "recipe_extension_dropshadow_darkness",
                EnabledWhen = RecipeExtension.EnabledOptionKey
            };
            var extension = CreateExtension(DropShadowId, "recipe_extension_dropshadow", "recipe_extension_dropshadow_description", DropShadowOrder)
                .AddOption(Switch("recipe_extension_dropshadow_enabled"))
                .AddOption(Number("size", "recipe_extension_dropshadow_size", 7, 1, 50))
                .AddOption(darkness)
                .AddOption(Number("offset", "recipe_extension_dropshadow_offset", -1, -20, 20))
                .AddNode(new RecipeNodeConfig("shadow", WellKnownStepTypes.Effect, "Drop shadow")
                    .Set("Effect", "DropShadow")
                    .Set("ShadowSize", "${option.size}")
                    .Set("Darkness", "${option.darkness}")
                    .Set("ShadowOffsetX", "${option.offset}")
                    .Set("ShadowOffsetY", "${option.offset}"));
            extension.Flow = new RecipeFlowConfig("shadow");
            return extension;
        }

        /// <summary>
        /// A bar above or below the capture with a text, by default the date and time (#906): text, position, font size and colors
        /// </summary>
        public static RecipeExtension CreateCaption()
        {
            const string barHeight = "${option.fontSize * 2}";
            var text = new RecipeOption
            {
                Key = "text",
                Type = ContractDataType.String,
                Format = RecipeOption.FormatTemplate,
                DefaultValue = "${now:yyyy-MM-dd HH:mm:ss}",
                Label = "recipe_extension_caption_text",
                Description = "recipe_extension_caption_text_description",
                EnabledWhen = RecipeExtension.EnabledOptionKey
            };
            var position = new RecipeOption
            {
                Key = "position",
                Type = ContractDataType.Enum,
                DefaultValue = "Bottom",
                Label = "recipe_extension_caption_position",
                EnabledWhen = RecipeExtension.EnabledOptionKey,
                Choices = new List<RecipeOptionChoice>
                {
                    new RecipeOptionChoice { Value = "Top", Label = "recipe_extension_caption_position_top" },
                    new RecipeOptionChoice { Value = "Bottom", Label = "recipe_extension_caption_position_bottom" }
                }
            };

            RecipeNodeConfig Bar(string id, string side) => new RecipeNodeConfig(id, WellKnownStepTypes.Effect, "Caption bar")
                .Set("Effect", "ResizeCanvas")
                .Set(side, barHeight)
                .Set("BackgroundColor", "${option.barColor}");

            RecipeNodeConfig Text(string id, string anchor) => new RecipeNodeConfig(id, WellKnownStepTypes.Annotation, "Caption text")
                .Set("Type", "Text")
                .Set("Text", "${option.text}")
                .Set("FontSize", "${option.fontSize}")
                .Set("LineColor", "${option.textColor}")
                .Set("Shadow", false)
                .Set("TextAlign", "Center")
                .Set("Left", 0)
                .Set("Width", "${payload.width}")
                .Set("Height", barHeight)
                .Set("VerticalAnchor", anchor)
                .Set(anchor, 0);

            var extension = CreateExtension(CaptionId, "recipe_extension_caption", "recipe_extension_caption_description", CaptionOrder)
                .AddOption(Switch("recipe_extension_caption_enabled"))
                .AddOption(text)
                .AddOption(position)
                .AddOption(Number("fontSize", "recipe_extension_caption_fontsize", 12, 6, 72))
                .AddOption(Color("textColor", "recipe_extension_caption_textcolor", "#000000"))
                .AddOption(Color("barColor", "recipe_extension_caption_barcolor", "#FFFFFF"))
                .AddNode(RecipeStepConfig.CreateConditional("position", new[]
                {
                    new KeyValuePair<string, string>("Top", "${option.position == 'Top'}"),
                    new KeyValuePair<string, string>("Bottom", "else")
                }).WithName("Caption position"))
                .AddNode(Bar("bar_top", "Top"))
                .AddNode(Text("text_top", "Top"))
                .AddNode(Bar("bar_bottom", "Bottom"))
                .AddNode(Text("text_bottom", "Bottom"));
            extension.Flow = new RecipeFlowConfig("position")
                .AddConditionalTransition("position", "Top", "bar_top")
                .AddConditionalTransition("position", "Bottom", "bar_bottom")
                .AddTransition("bar_top", "text_top")
                .AddTransition("bar_bottom", "text_bottom");
            return extension;
        }
    }
}
