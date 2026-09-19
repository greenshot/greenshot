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
using Greenshot.Base.Recipes;
using Greenshot.Pipeline.Steps;
using Greenshot.Plugin.RecipeEditor.ViewModels;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class DrawableItemViewModelTests
    {
        public DrawableItemViewModelTests()
        {
            TestEnvironment.EnsureInitialized();
            AnnotationStep.EnsureBuiltInDrawablesRegistered();
        }

        [Fact]
        public void TypeProperties_DetectCategoryFlagsCorrectly()
        {
            var item = new DrawableItemViewModel();

            // Default is Rectangle
            Assert.Equal("Rectangle", item.Type);
            Assert.True(item.HasLineThickness);
            Assert.True(item.HasShadow);
            Assert.True(item.HasFillColor);
            Assert.True(item.HasLineColor);
            Assert.True(item.HasAnyStandardColor);
            Assert.False(item.HasFontSettings);
            Assert.False(item.IsArrowType);
            Assert.False(item.IsQrCodeType);

            // Arrow
            item.Type = "Arrow";
            Assert.True(item.IsArrowType);
            Assert.True(item.HasLineThickness);
            Assert.True(item.HasShadow);
            Assert.True(item.HasLineColor);
            Assert.False(item.HasFillColor);
            Assert.True(item.HasAnyStandardColor);
            Assert.False(item.HasFontSettings);

            // Text
            item.Type = "Text";
            Assert.True(item.IsTextType);
            Assert.True(item.HasFontSettings);
            Assert.True(item.HasLineThickness);
            Assert.True(item.HasShadow);
            Assert.True(item.HasFillColor);
            Assert.True(item.HasLineColor);
            Assert.Equal("Background Color", item.FillColorLabel);
            Assert.Equal("Text Color", item.LineColorLabel);

            // Speechbubble
            item.Type = "Speechbubble";
            Assert.True(item.IsSpeechbubbleType);
            Assert.True(item.IsTextType);
            Assert.True(item.HasFontSettings);
            Assert.True(item.HasLineThickness);
            Assert.True(item.HasShadow);

            // StepLabel
            item.Type = "StepLabel";
            Assert.True(item.IsStepLabelType);
            Assert.True(item.HasShadow);
            Assert.True(item.HasFillColor);
            Assert.True(item.HasLineColor);
            Assert.False(item.HasLineThickness);
            Assert.Equal("Circle Color", item.FillColorLabel);
            Assert.Equal("Number Color", item.LineColorLabel);

            // QRCode
            item.Type = "QRCode";
            Assert.True(item.IsQrCodeType);
            Assert.False(item.HasFillColor);
            Assert.False(item.HasLineColor);
            Assert.False(item.HasAnyStandardColor);
            Assert.False(item.HasLineThickness);
            Assert.False(item.HasShadow);

            // Blur & Pixelize
            item.Type = "Blur";
            Assert.True(item.IsBlurType);
            Assert.True(item.IsObfuscateType);
            Assert.False(item.HasAnyStandardColor);

            item.Type = "Pixelize";
            Assert.True(item.IsPixelizeType);
            Assert.True(item.IsObfuscateType);

            // Magnify
            item.Type = "Magnify";
            Assert.True(item.IsMagnifyType);
            Assert.False(item.HasAnyStandardColor);

            // Emoji
            item.Type = "Emoji";
            Assert.True(item.IsEmojiType);
            Assert.False(item.HasAnyStandardColor);
        }

        [Fact]
        public void TextAndStepNumber_SynchronizeBidirectionally()
        {
            var item = new DrawableItemViewModel { Type = "StepLabel" };
            item.StepNumber = 5;
            Assert.Equal("5", item.Text);

            item.Text = "12";
            Assert.Equal(12, item.StepNumber);
        }

        [Fact]
        public void ToDictionary_And_FromDictionary_RoundTrip_Rectangle()
        {
            var original = new DrawableItemViewModel
            {
                Type = "Rectangle",
                Width = "350",
                Height = "120",
                OffsetX = "15",
                OffsetY = "25",
                HorizontalAnchor = "Right",
                VerticalAnchor = "Bottom",
                FillColor = "#112233",
                LineColor = "#445566",
                LineThickness = 4,
                Shadow = false
            };

            var dict = original.ToDictionary();
            Assert.Equal("Rectangle", dict["Type"]);
            Assert.Equal(4, dict["LineThickness"]);
            Assert.Equal(false, dict["Shadow"]);
            Assert.Equal("#112233", dict["FillColor"]);
            Assert.Equal("#445566", dict["LineColor"]);
            Assert.Equal("Right", dict["HorizontalAnchor"]);
            Assert.Equal("Bottom", dict["VerticalAnchor"]);

            var restored = DrawableItemViewModel.FromDictionary(dict);
            Assert.Equal("Rectangle", restored.Type);
            Assert.Equal("350", restored.Width);
            Assert.Equal("120", restored.Height);
            Assert.Equal("15", restored.OffsetX);
            Assert.Equal("25", restored.OffsetY);
            Assert.Equal("Right", restored.HorizontalAnchor);
            Assert.Equal("Bottom", restored.VerticalAnchor);
            Assert.Equal("#112233", restored.FillColor);
            Assert.Equal("#445566", restored.LineColor);
            Assert.Equal(4, restored.LineThickness);
            Assert.False(restored.Shadow);
        }

        [Fact]
        public void ToDictionary_And_FromDictionary_RoundTrip_Text()
        {
            var original = new DrawableItemViewModel
            {
                Type = "Text",
                Text = "Hello Greenshot!",
                FontFamily = "Consolas",
                FontSize = 18.5,
                FontBold = true,
                FontItalic = true,
                TextAlignment = "Right",
                LineColor = "#FFFFFF",
                FillColor = "#000000",
                LineThickness = 2,
                Shadow = true
            };

            var dict = original.ToDictionary();
            Assert.Equal("Text", dict["Type"]);
            Assert.Equal("Hello Greenshot!", dict["Text"]);
            Assert.Equal("Consolas", dict["FontFamily"]);
            Assert.Equal(18.5, dict["FontSize"]);
            Assert.Equal(true, dict["Bold"]);
            Assert.Equal(true, dict["Italic"]);
            Assert.Equal("Right", dict["TextAlign"]);
            Assert.Equal(2, dict["LineThickness"]);
            Assert.Equal(true, dict["Shadow"]);

            var restored = DrawableItemViewModel.FromDictionary(dict);
            Assert.Equal("Text", restored.Type);
            Assert.Equal("Hello Greenshot!", restored.Text);
            Assert.Equal("Consolas", restored.FontFamily);
            Assert.Equal(18.5, restored.FontSize);
            Assert.True(restored.FontBold);
            Assert.True(restored.FontItalic);
            Assert.Equal("Right", restored.TextAlignment);
            Assert.Equal(2, restored.LineThickness);
            Assert.True(restored.Shadow);
        }

        [Fact]
        public void ToDictionary_And_FromDictionary_RoundTrip_Arrow()
        {
            var original = new DrawableItemViewModel
            {
                Type = "Arrow",
                ArrowHeads = "BOTH",
                LineThickness = 5,
                LineColor = "#00FF00",
                Shadow = false
            };

            var dict = original.ToDictionary();
            Assert.Equal("Arrow", dict["Type"]);
            Assert.Equal("BOTH", dict["ArrowHeads"]);
            Assert.Equal(5, dict["LineThickness"]);
            Assert.Equal(false, dict["Shadow"]);
            Assert.False(dict.ContainsKey("FillColor"));

            var restored = DrawableItemViewModel.FromDictionary(dict);
            Assert.Equal("Arrow", restored.Type);
            Assert.Equal("BOTH", restored.ArrowHeads);
            Assert.Equal(5, restored.LineThickness);
            Assert.False(restored.Shadow);
        }

        [Fact]
        public void ToDictionary_And_FromDictionary_RoundTrip_StepLabel()
        {
            var original = new DrawableItemViewModel
            {
                Type = "StepLabel",
                StepNumber = 7,
                FillColor = "#AA0000",
                LineColor = "#FFFFFF",
                Shadow = true
            };

            var dict = original.ToDictionary();
            Assert.Equal("StepLabel", dict["Type"]);
            Assert.Equal(7, dict["Number"]);
            Assert.Equal("7", dict["Text"]);
            Assert.Equal(true, dict["Shadow"]);
            Assert.Equal("#AA0000", dict["FillColor"]);
            Assert.Equal("#FFFFFF", dict["LineColor"]);

            var restored = DrawableItemViewModel.FromDictionary(dict);
            Assert.Equal("StepLabel", restored.Type);
            Assert.Equal(7, restored.StepNumber);
            Assert.Equal("7", restored.Text);
            Assert.True(restored.Shadow);
            Assert.Equal("#AA0000", restored.FillColor);
            Assert.Equal("#FFFFFF", restored.LineColor);
        }

        [Fact]
        public void ToDictionary_And_FromDictionary_RoundTrip_QrCode()
        {
            var original = new DrawableItemViewModel
            {
                Type = "QRCode",
                Text = "https://github.com/greenshot",
                ForeColor = "#001122",
                BackColor = "#EEFFAA",
                RoundedDots = true,
                Margin = 3
            };

            var dict = original.ToDictionary();
            Assert.Equal("QRCode", dict["Type"]);
            Assert.Equal("https://github.com/greenshot", dict["Text"]);
            Assert.Equal("#001122", dict["ForeColor"]);
            Assert.Equal("#EEFFAA", dict["BackColor"]);
            Assert.Equal(true, dict["RoundedDots"]);
            Assert.Equal(3, dict["Margin"]);
            Assert.False(dict.ContainsKey("FillColor"));
            Assert.False(dict.ContainsKey("LineColor"));

            var restored = DrawableItemViewModel.FromDictionary(dict);
            Assert.Equal("QRCode", restored.Type);
            Assert.Equal("https://github.com/greenshot", restored.Text);
            Assert.Equal("#001122", restored.ForeColor);
            Assert.Equal("#EEFFAA", restored.BackColor);
            Assert.True(restored.RoundedDots);
            Assert.Equal(3, restored.Margin);
        }

        [Fact]
        public void ToDictionary_And_FromDictionary_RoundTrip_Blur_And_Pixelize()
        {
            var blurItem = new DrawableItemViewModel
            {
                Type = "Blur",
                BlurRadius = 25
            };
            var blurDict = blurItem.ToDictionary();
            Assert.Equal(25, blurDict["BlurRadius"]);

            var restoredBlur = DrawableItemViewModel.FromDictionary(blurDict);
            Assert.Equal("Blur", restoredBlur.Type);
            Assert.Equal(25, restoredBlur.BlurRadius);

            var pixelItem = new DrawableItemViewModel
            {
                Type = "Pixelize",
                PixelSize = 14
            };
            var pixelDict = pixelItem.ToDictionary();
            Assert.Equal(14, pixelDict["PixelSize"]);

            var restoredPixel = DrawableItemViewModel.FromDictionary(pixelDict);
            Assert.Equal("Pixelize", restoredPixel.Type);
            Assert.Equal(14, restoredPixel.PixelSize);
        }

        [Fact]
        public void ToDictionary_And_FromDictionary_RoundTrip_Magnify()
        {
            var magnifyItem = new DrawableItemViewModel
            {
                Type = "Magnify",
                MagnificationFactor = 4
            };
            var dict = magnifyItem.ToDictionary();
            Assert.Equal(4, dict["MagnificationFactor"]);

            var restored = DrawableItemViewModel.FromDictionary(dict);
            Assert.Equal("Magnify", restored.Type);
            Assert.Equal(4, restored.MagnificationFactor);
        }

        [Fact]
        public void AddAnnotation_InitializesTypeSpecificDefaults()
        {
            var nodeConfig = new RecipeNodeConfig { Id = "test_annotation", StepType = WellKnownStepTypes.Annotation };
            var stepVm = new StepNodeViewModel(nodeConfig, new System.Windows.Point(0, 0));

            stepVm.AddAnnotation("Text");
            var textItem = stepVm.Annotations[0];
            Assert.Equal("Text", textItem.Type);
            Assert.Equal(12.0, textItem.FontSize);
            Assert.False(textItem.FontBold);
            Assert.True(textItem.Shadow);

            stepVm.AddAnnotation("Speechbubble");
            var bubbleItem = stepVm.Annotations[1];
            Assert.Equal("Speechbubble", bubbleItem.Type);
            Assert.Equal(14.0, bubbleItem.FontSize);
            Assert.True(bubbleItem.FontBold);
            Assert.False(bubbleItem.Shadow);

            stepVm.AddAnnotation("Arrow");
            var arrowItem = stepVm.Annotations[2];
            Assert.Equal("Arrow", arrowItem.Type);
            Assert.Equal("END_POINT", arrowItem.ArrowHeads);

            stepVm.AddAnnotation("QRCode");
            var qrItem = stepVm.Annotations[3];
            Assert.Equal("QRCode", qrItem.Type);
            Assert.Equal("#000000", qrItem.ForeColor);
            Assert.Equal("#FFFFFF", qrItem.BackColor);
            Assert.Equal(1, qrItem.Margin);

            stepVm.AddAnnotation("Blur");
            var blurItem = stepVm.Annotations[4];
            Assert.Equal(10, blurItem.BlurRadius);

            stepVm.AddAnnotation("Pixelize");
            var pixelItem = stepVm.Annotations[5];
            Assert.Equal(5, pixelItem.PixelSize);

            stepVm.AddAnnotation("Magnify");
            var magItem = stepVm.Annotations[6];
            Assert.Equal(2, magItem.MagnificationFactor);
        }
    }
}
