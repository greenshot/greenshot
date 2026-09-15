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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Drawing;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Helpers;
using Greenshot.Pipeline.Steps;
using Greenshot.Plugin.Zxing;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class RecipeDrawableRegistryTests
    {
        public RecipeDrawableRegistryTests()
        {
            TestEnvironment.EnsureInitialized();
            DrawableStep.EnsureBuiltInDrawablesRegistered();
            var zxing = new ZxingPlugin();
            zxing.RegisterDrawables(RecipeDrawableRegistry.Instance);
        }

        [Fact]
        public void Registry_BuiltInDrawables_AreRegisteredAndInstantiated()
        {
            var registry = RecipeDrawableRegistry.Instance;
            Assert.True(registry.IsRegistered("Rectangle"));
            Assert.True(registry.IsRegistered("Text"));
            Assert.True(registry.IsRegistered("Emoji"));
            Assert.True(registry.IsRegistered("Speechbubble"));
            Assert.True(registry.IsRegistered("Arrow"));

            using var bmp = new Bitmap(500, 500);
            using var surface = new Surface(bmp);

            var rect = registry.CreateDrawable("Rectangle", surface, new Dictionary<string, object>(), null);
            Assert.NotNull(rect);
            Assert.IsType<RectangleContainer>(rect);

            var text = registry.CreateDrawable("Text", surface, new Dictionary<string, object> { ["Text"] = "Hello" }, null);
            Assert.NotNull(text);
            Assert.IsType<TextContainer>(text);
        }

        [Fact]
        public void Registry_ZxingDrawables_RegisteredAndCreateImageContainer()
        {
            var registry = RecipeDrawableRegistry.Instance;
            Assert.True(registry.IsRegistered("QRCode"));
            Assert.True(registry.IsRegistered("Barcode"));

            using var bmp = new Bitmap(500, 500);
            using var surface = new Surface(bmp);

            var p = new Dictionary<string, object>
            {
                ["Text"] = "https://getgreenshot.org",
                ["Size"] = 180,
                ["RoundedDots"] = true
            };

            var qrContainer = registry.CreateDrawable("QRCode", surface, p, null);
            Assert.NotNull(qrContainer);
            Assert.IsAssignableFrom<ImageContainer>(qrContainer);
            Assert.IsType<BarcodeContainer>(qrContainer);

            var img = (ImageContainer)qrContainer;
            Assert.NotNull(img.Image);
            Assert.True(img.Width > 0);
            Assert.True(img.Height > 0);

            // Double click handler model
            Assert.NotNull(img.Tag);
            Assert.IsType<ZxingModel>(img.Tag);
            var model = (ZxingModel)img.Tag;
            Assert.Equal("https://getgreenshot.org", model.RawText);
            Assert.True(model.RoundedDots);
        }

        [Fact]
        public async Task DrawableStep_WithQrCode_AppliesAnchorsAndPositionsCorrectly()
        {
            using var bmp = new Bitmap(1000, 800);
            using var surface = new Surface(bmp);

            var nodeConfig = new RecipeNodeConfig
            {
                Id = "qr_node",
                StepType = "Drawable",
                Parameters = new Dictionary<string, object>
                {
                    ["Type"] = "QRCode",
                    ["Text"] = "https://getgreenshot.org",
                    ["HorizontalAnchor"] = "Right",
                    ["VerticalAnchor"] = "Bottom",
                    ["Width"] = 150,
                    ["Height"] = 150,
                    ["Margin"] = 25
                }
            };

            var step = new DrawableStep(nodeConfig);
            var recipe = new CaptureRecipe("test_recipe", "Test");
            var capture = new Capture((Image)bmp.Clone());
            using var context = new CaptureFlowContext(recipe)
            {
                Payload = new CapturePayload(capture)
            };

            await step.ExecuteAsync(context);

            var s = context.Payload.EnsureSurface();
            Assert.Single(s.Elements);
            var element = s.Elements.First();

            Assert.Equal(150, element.Width);
            Assert.Equal(150, element.Height);

            // Right anchor: 1000 - 150 - 25 = 825
            Assert.Equal(825, element.Left);
            // Bottom anchor: 800 - 150 - 25 = 625
            Assert.Equal(625, element.Top);
        }

        [Fact]
        public async Task DrawableStep_WithCenterAnchor_PositionsInCenter()
        {
            using var bmp = new Bitmap(1000, 800);
            using var surface = new Surface(bmp);

            var nodeConfig = new RecipeNodeConfig
            {
                Id = "qr_center",
                StepType = "Drawable",
                Parameters = new Dictionary<string, object>
                {
                    ["Type"] = "QRCode",
                    ["Text"] = "GreenshotQR",
                    ["HorizontalAnchor"] = "Center",
                    ["VerticalAnchor"] = "Center",
                    ["Size"] = 200
                }
            };

            var step = new DrawableStep(nodeConfig);
            var recipe = new CaptureRecipe("center_recipe", "Test Center");
            var capture = new Capture((Image)bmp.Clone());
            using var context = new CaptureFlowContext(recipe)
            {
                Payload = new CapturePayload(capture)
            };

            await step.ExecuteAsync(context);

            var s = context.Payload.EnsureSurface();
            Assert.Single(s.Elements);
            var element = s.Elements.First();

            Assert.Equal(200, element.Width);
            Assert.Equal(200, element.Height);

            // Center X: (1000 - 200) / 2 = 400
            Assert.Equal(400, element.Left);
            // Center Y: (800 - 200) / 2 = 300
            Assert.Equal(300, element.Top);
        }

        [Fact]
        public void ZxingBarcodeGenerator_GeneratesValidBitmapsWithCustomColorsAndSizes()
        {
            var bmp = ZxingBarcodeGenerator.Generate(
                payload: "https://getgreenshot.org",
                format: ZXing.BarcodeFormat.QR_CODE,
                foreColor: Color.DarkBlue,
                backColor: Color.LightYellow,
                roundedDots: false,
                requestedWidth: 250,
                requestedHeight: 250
            );

            Assert.NotNull(bmp);
            Assert.Equal(250, bmp.Width);
            Assert.Equal(250, bmp.Height);

            // Check quiet zone corner has background color
            var cornerPixel = bmp.GetPixel(0, 0);
            Assert.Equal(Color.LightYellow.ToArgb(), cornerPixel.ToArgb());
        }

        [Fact]
        public void ZxingPlugin_RegisterSteps_RegistersBarcodeScan()
        {
            var plugin = new ZxingPlugin();
            plugin.RegisterSteps(StepRegistry.Instance);

            Assert.True(StepRegistry.Instance.IsRegistered("BarcodeScan"));

            var nodeConfig = new RecipeNodeConfig
            {
                Id = "scan_node",
                StepType = "BarcodeScan"
            };

            var step = StepRegistry.Instance.CreateStep(nodeConfig);
            Assert.NotNull(step);
            Assert.IsType<ZxingStep>(step);
        }

        [Fact]
        public void Registry_ZxingDrawables_StructuredTypes_CreatesPopulatedContainers()
        {
            var registry = RecipeDrawableRegistry.Instance;

            using var bmp = new Bitmap(500, 500);
            using var surface = new Surface(bmp);

            // 1. vCard Contact Card
            var vcardParams = new Dictionary<string, object>
            {
                ["QrType"] = "BusinessCard",
                ["VcardFirstName"] = "Jane",
                ["VcardLastName"] = "Doe",
                ["VcardCompany"] = "Acme Corp",
                ["VcardEmail"] = "jane.doe@example.com",
                ["VcardPhone"] = "+1-555-123456",
                ["VcardUrl"] = "https://example.com"
            };
            var vcardContainer = registry.CreateDrawable("QRCode", surface, vcardParams, null) as ImageContainer;
            Assert.NotNull(vcardContainer);
            Assert.NotNull(vcardContainer.Tag);
            var vcardModel = Assert.IsType<ZxingModel>(vcardContainer.Tag);
            Assert.Equal(2, vcardModel.QrCategoryIndex);
            Assert.Contains("BEGIN:VCARD", vcardModel.RawText);
            Assert.Contains("Jane", vcardModel.RawText);
            Assert.Contains("Doe", vcardModel.RawText);

            // 2. EPC SEPA Payment Transfer
            var epcParams = new Dictionary<string, object>
            {
                ["QrType"] = "Payment",
                ["EpcName"] = "Max Mustermann",
                ["EpcIban"] = "DE89370400440532013000",
                ["EpcBic"] = "COBADEFFXXX",
                ["EpcAmount"] = 49.99,
                ["EpcReference"] = "INV-2026-001",
                ["EpcMessage"] = "Design consultation invoice"
            };
            var epcContainer = registry.CreateDrawable("QRCode", surface, epcParams, null) as ImageContainer;
            Assert.NotNull(epcContainer);
            Assert.NotNull(epcContainer.Tag);
            var epcModel = Assert.IsType<ZxingModel>(epcContainer.Tag);
            Assert.Equal(3, epcModel.QrCategoryIndex);
            Assert.Contains("BCD\n002\n1\nSCT", epcModel.RawText);
            Assert.Contains("DE89370400440532013000", epcModel.RawText);
            Assert.Contains("EUR49.99", epcModel.RawText);

            // 3. WiFi Network
            var wifiParams = new Dictionary<string, object>
            {
                ["QrType"] = "WiFi",
                ["WifiSsid"] = "Office-Guest",
                ["WifiPassword"] = "SuperSecretKey!",
                ["WifiEncryption"] = "WPA"
            };
            var wifiContainer = registry.CreateDrawable("QRCode", surface, wifiParams, null) as ImageContainer;
            Assert.NotNull(wifiContainer);
            Assert.NotNull(wifiContainer.Tag);
            var wifiModel = Assert.IsType<ZxingModel>(wifiContainer.Tag);
            Assert.Equal(1, wifiModel.QrCategoryIndex);
            Assert.Equal("WIFI:S:Office-Guest;T:WPA;P:SuperSecretKey!;;", wifiModel.RawText);

            // 4. Email
            var emailParams = new Dictionary<string, object>
            {
                ["QrType"] = "Email",
                ["EmailTo"] = "support@getgreenshot.org",
                ["EmailSubject"] = "Question",
                ["EmailBody"] = "Hello Greenshot Team"
            };
            var emailContainer = registry.CreateDrawable("QRCode", surface, emailParams, null) as ImageContainer;
            Assert.NotNull(emailContainer);
            var emailModel = Assert.IsType<ZxingModel>(emailContainer.Tag);
            Assert.Equal(4, emailModel.QrCategoryIndex);
            Assert.StartsWith("mailto:support@getgreenshot.org", emailModel.RawText);
            Assert.Contains("subject=Question", emailModel.RawText);
            Assert.Contains("body=Hello%20Greenshot%20Team", emailModel.RawText);

            // 5. Calendar Event
            var calParams = new Dictionary<string, object>
            {
                ["QrType"] = "CalendarEvent",
                ["EventTitle"] = "Sprint Review",
                ["EventLocation"] = "Room 101",
                ["EventStart"] = "20261001T090000Z",
                ["EventEnd"] = "20261001T100000Z",
                ["EventDescription"] = "Q4 review meeting"
            };
            var calContainer = registry.CreateDrawable("QRCode", surface, calParams, null) as ImageContainer;
            Assert.NotNull(calContainer);
            var calModel = Assert.IsType<ZxingModel>(calContainer.Tag);
            Assert.Equal(5, calModel.QrCategoryIndex);
            Assert.Contains("BEGIN:VCALENDAR", calModel.RawText);
            Assert.Contains("SUMMARY:Sprint Review", calModel.RawText);
            Assert.Contains("DTSTART:20261001T090000Z", calModel.RawText);

            // 6. Phone
            var phoneParams = new Dictionary<string, object>
            {
                ["QrType"] = "Phone",
                ["PhoneNumber"] = "+49-123-456789"
            };
            var phoneContainer = registry.CreateDrawable("QRCode", surface, phoneParams, null) as ImageContainer;
            Assert.NotNull(phoneContainer);
            var phoneModel = Assert.IsType<ZxingModel>(phoneContainer.Tag);
            Assert.Equal(6, phoneModel.QrCategoryIndex);
            Assert.Equal("tel:+49-123-456789", phoneModel.RawText);

            // 7. SMS
            var smsParams = new Dictionary<string, object>
            {
                ["QrType"] = "Sms",
                ["SmsNumber"] = "+49-123-456789",
                ["SmsMessage"] = "Quick message"
            };
            var smsContainer = registry.CreateDrawable("QRCode", surface, smsParams, null) as ImageContainer;
            Assert.NotNull(smsContainer);
            var smsModel = Assert.IsType<ZxingModel>(smsContainer.Tag);
            Assert.Equal(7, smsModel.QrCategoryIndex);
            Assert.Equal("smsto:+49-123-456789:Quick message", smsModel.RawText);

            // 8. Geo Location
            var geoParams = new Dictionary<string, object>
            {
                ["QrType"] = "Geo",
                ["Latitude"] = "52.5200",
                ["Longitude"] = "13.4050"
            };
            var geoContainer = registry.CreateDrawable("QRCode", surface, geoParams, null) as ImageContainer;
            Assert.NotNull(geoContainer);
            var geoModel = Assert.IsType<ZxingModel>(geoContainer.Tag);
            Assert.Equal(8, geoModel.QrCategoryIndex);
            Assert.Equal("geo:52.5200,13.4050", geoModel.RawText);
        }

        [Fact]
        public void ZxingPlugin_SchemaProviders_ReturnValidJsonSchemas()
        {
            var plugin = new ZxingPlugin();

            string drawableSchema = plugin.GetDrawableSchemaJson();
            Assert.NotNull(drawableSchema);
            Assert.Contains("qrType", drawableSchema);
            Assert.Contains("epcIban", drawableSchema);
            Assert.Contains("vcardEmail", drawableSchema);
            Assert.Contains("wifiSsid", drawableSchema);
            Assert.Contains("emailTo", drawableSchema);
            string stepSchema = plugin.GetStepSchemaJson();
            Assert.NotNull(stepSchema);
            Assert.Contains("BarcodeScan", stepSchema);
        }

        [Fact]
        public void BarcodeContainer_ScaleOptions_ReturnsRationalFor2DBarcode()
        {
            using var bmp = new Bitmap(500, 500);
            using var surface = new Surface(bmp);

            var model = new ZxingModel { FormatIndex = 0, RawText = "test" };
            var container = new BarcodeContainer(surface, model, 0);

            Assert.IsAssignableFrom<IHaveScaleOptions>(container);
            var scaleOptions = ((IHaveScaleOptions)container).GetScaleOptions();
            Assert.Equal(ScaleOptions.Rational, scaleOptions & ScaleOptions.Rational);
        }

        [Fact]
        public void BarcodeContainer_DynamicRegeneration_RegeneratesSharpBitmapWhenResized()
        {
            using var bmp = new Bitmap(500, 500);
            using var surface = new Surface(bmp);

            var model = new ZxingModel { FormatIndex = 0, RawText = "https://greenshot.org" };
            var container = new BarcodeContainer(surface, model, 0);

            // Initial dimensions
            container.Width = 100;
            container.Height = 100;

            // Trigger Draw which will ensure bitmap matches container size
            using (var g = Graphics.FromImage(bmp))
            {
                container.Draw(g, RenderMode.EXPORT);
            }

            Assert.NotNull(container.Image);
            Assert.Equal(100, container.Image.Width);
            Assert.Equal(100, container.Image.Height);

            // Resize container to 240x240
            container.Width = 240;
            container.Height = 240;

            using (var g = Graphics.FromImage(bmp))
            {
                container.Draw(g, RenderMode.EXPORT);
            }

            // Container should have regenerated high-res bitmap matching 240x240!
            Assert.NotNull(container.Image);
            Assert.Equal(240, container.Image.Width);
            Assert.Equal(240, container.Image.Height);
        }

        [Fact]
        public async Task DrawableStep_AspectRatioLock_ForcesSquareForRationalDrawables()
        {
            using var bmp = new Bitmap(1000, 800);
            using var surface = new Surface(bmp);

            // Pass non-square Width (200) and Height (40)
            var nodeConfig = new RecipeNodeConfig
            {
                Id = "qr_ratio_node",
                StepType = "Drawable",
                Parameters = new Dictionary<string, object>
                {
                    ["Type"] = "QRCode",
                    ["Text"] = "GreenshotAspectLock",
                    ["Width"] = 200,
                    ["Height"] = 40
                }
            };

            var step = new DrawableStep(nodeConfig);
            var recipe = new CaptureRecipe("ratio_recipe", "Test Ratio");
            var capture = new Capture((Image)bmp.Clone());
            using var context = new CaptureFlowContext(recipe)
            {
                Payload = new CapturePayload(capture)
            };

            await step.ExecuteAsync(context);

            var s = context.Payload.EnsureSurface();
            Assert.Single(s.Elements);
            var element = s.Elements.First();

            // Should have locked aspect ratio to square fitting inside specified box (Min(200, 40) = 40)
            Assert.Equal(40, element.Width);
            Assert.Equal(40, element.Height);
        }

        [Fact]
        public void RecipeDrawableRegistry_GetScaleOptions_ReturnsExpectedScaleOptions()
        {
            var registry = RecipeDrawableRegistry.Instance;

            // Plugin drawables
            Assert.Equal(ScaleOptions.Rational, registry.GetScaleOptions("QRCode"));
            Assert.Equal(ScaleOptions.Default, registry.GetScaleOptions("Barcode"));

            // Built-in drawables
            Assert.Equal(ScaleOptions.Rational, registry.GetScaleOptions("StepLabel"));
            Assert.Equal(ScaleOptions.Rational, registry.GetScaleOptions("Emoji"));
            Assert.Equal(ScaleOptions.Rational, registry.GetScaleOptions("Svg"));
            Assert.Equal(ScaleOptions.Default, registry.GetScaleOptions("Rectangle"));
            Assert.Equal(ScaleOptions.Default, registry.GetScaleOptions("Text"));
            Assert.Equal(ScaleOptions.Default, registry.GetScaleOptions("UnknownDrawable"));
        }
    }
}
