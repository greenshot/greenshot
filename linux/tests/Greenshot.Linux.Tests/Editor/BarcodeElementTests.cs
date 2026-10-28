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

using Greenshot.Linux.Core.Barcodes;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Serialization;
using SkiaSharp;

namespace Greenshot.Linux.Tests.Editor;

public class BarcodeElementTests
{
    [Fact]
    public void A_wifi_qr_code_has_the_payload_and_can_be_read_back()
    {
        var element = new BarcodeElement
        {
            QrType = "Wifi",
            Values = new() { ["WifiSsid"] = "Office", ["WifiPassword"] = "secret", ["WifiEncryption"] = "WPA" },
        };
        Assert.Equal("WIFI:S:Office;T:WPA;P:secret;;", element.Payload);

        using var bitmap = element.Render(300, 300);
        Assert.NotNull(bitmap);
        Assert.Equal("WIFI:S:Office;T:WPA;P:secret;;", Assert.Single(Barcodes.Detect(bitmap!)).Text);
    }

    [Fact]
    public void A_barcode_uses_its_text_and_impossible_content_renders_nothing()
    {
        var ean = new BarcodeElement { Format = "EAN_13", Values = new() { ["Text"] = "5901234123457" } };
        Assert.False(ean.Is2D);
        using (var bitmap = ean.Render(300, 100))
        {
            Assert.NotNull(bitmap);
        }

        ean.Values["Text"] = "not digits";
        Assert.Null(ean.Render(300, 100));
        Assert.Null(new BarcodeElement().Render(200, 200));
    }

    [Fact]
    public void The_editor_inserts_and_changes_codes_undoably()
    {
        using var surface = new EditorSurface(new SKBitmap(400, 300));
        var controller = new EditorController(surface);
        var element = new BarcodeElement { Values = new() { ["Text"] = "https://getgreenshot.org" } };
        controller.InsertBarcode(element);
        Assert.Equal(SKRect.Create(100, 50, 200, 200), element.Bounds);

        // Dragging a corner keeps a QR code square
        element.DragHandle(HandlePosition.BottomRight, new SKPoint(400, 260), keepAspect: false);
        Assert.Equal(element.Bounds.Width, element.Bounds.Height);

        controller.ChangeBarcode(element, new BarcodeElement { Format = "CODE_128", Values = new() { ["Text"] = "ABC-123" }, ForeColor = SKColors.Navy });
        Assert.Equal(("CODE_128", SKColors.Navy), (element.Format, element.ForeColor));
        surface.Undo();
        var restored = Assert.IsType<BarcodeElement>(Assert.Single(surface.Elements));
        Assert.Equal(("QR_CODE", "https://getgreenshot.org"), (restored.Format, restored.Payload));
    }

    [Fact]
    public void Codes_are_kept_in_greenshot_files()
    {
        var element = new BarcodeElement { QrType = "Geo", Values = new() { ["Latitude"] = "59.33", ["Longitude"] = "18.06" }, RoundedDots = true, BackColor = SKColors.Yellow };
        var copy = Assert.IsType<BarcodeElement>(Assert.Single(ElementSerializer.Clone([element])));
        Assert.Equal(("geo:59.33,18.06", true, SKColors.Yellow), (copy.Payload, copy.RoundedDots, copy.BackColor));
    }
}
