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

using System.Net;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Http;
using Greenshot.Linux.Core.Plugins;
using Greenshot.Linux.Plugins.CustomUploader;
using Greenshot.Linux.Plugins.CustomUploader.Sxcu;
using Greenshot.Linux.Plugins.ExternalCommand;
using Greenshot.Linux.Plugins.Pdf;

namespace Greenshot.Linux.Tests.Plugins;

/// <summary>
/// Shares the static Texts with the language tests
/// </summary>
[Collection(nameof(Languages.TextsTests))]
public sealed class PluginTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "gs-plugins-" + Guid.NewGuid().ToString("N"));

    private CoreSettings Settings() => CoreSettings.Load(Path.Combine(_folder, "greenshot.ini"));

    [Theory]
    [InlineData("\"{0}\"", new[] { "/tmp/my capture.png" })]
    [InlineData("--new-window {0}", new[] { "--new-window", "/tmp/my capture.png" })]
    [InlineData("-o 'out dir' \"{0}\" --x", new[] { "-o", "out dir", "/tmp/my capture.png", "--x" })]
    [InlineData("", new string[0])]
    public void Arguments_are_split_before_the_file_is_filled_in(string template, string[] expected)
    {
        Assert.Equal(expected, ExternalCommandDestination.BuildArguments(template, "/tmp/my capture.png"));
    }

    [Fact]
    public void A_file_name_with_quotes_stays_one_argument()
    {
        var path = "/tmp/a \"b\" 'c'.png";
        Assert.Equal([path], ExternalCommandDestination.BuildArguments("\"{0}\"", path));
    }

    [Theory]
    [InlineData("Uploaded: https://example.com/i/abc.png.", "https://example.com/i/abc.png")]
    [InlineData("nothing here", null)]
    public void The_first_link_of_the_output_is_found(string output, string? expected)
    {
        Assert.Equal(expected, ExternalCommandDestination.FindUri(output));
    }

    [Fact]
    public void Installed_editors_are_added_once_and_a_deleted_one_stays_away()
    {
        var settings = Settings();
        var commands = new ExternalCommandSettings(settings);
        commands.AddInstalledEditors(program => program is "gimp" or "krita");
        Assert.Equal(["GIMP", "Krita"], commands.Commands);
        Assert.Equal("gimp", commands.Get("GIMP")!.Commandline);
        Assert.Equal("\"{0}\"", commands.Get("GIMP")!.Argument);

        commands.Delete("GIMP");
        commands.AddInstalledEditors(program => program is "gimp" or "krita");
        Assert.Equal(["Krita"], commands.Commands);
        Assert.Null(settings.GetValue(ExternalCommandSettings.SectionName, "Commandline.GIMP"));

        commands.Save(new ExternalCommandDefinition("Shrink", "convert", "{0} -resize 50% {0}", "jpg", false), oldName: "Krita");
        Assert.Equal(["Shrink"], commands.Commands);
        Assert.False(commands.Get("Shrink")!.RunInBackground);
    }

    [Fact]
    public void The_policy_follows_the_user_and_the_administrator()
    {
        var settings = Settings();
        var userOnly = new PluginPolicy(settings, new IniFile());
        userOnly.SetPluginEnabled("Box", false);
        Assert.False(userOnly.IsPluginEnabled("box"));
        Assert.True(userOnly.IsPluginEnabled("Dropbox"));
        userOnly.SetPluginEnabled("Box", true);
        Assert.True(userOnly.IsPluginEnabled("Box"));

        var fixedIni = IniFile.Parse("[Core]\nBlockedPluginNames=Dropbox\nAllowedDestinations=Clipboard,FileNoDialog\nExcludePluginNames=\n");
        var admin = new PluginPolicy(settings, fixedIni);
        Assert.False(admin.IsPluginEnabled("Dropbox"));
        Assert.True(admin.IsPluginBlocked("Dropbox"));
        Assert.True(admin.IsDestinationEnabled("Clipboard"));
        Assert.False(admin.IsDestinationEnabled("Printer"));
        Assert.False(admin.CanUserChangePlugins);
    }

    [Fact]
    public void Pdf_pages_follow_the_settings()
    {
        var pdf = new PdfSettings(Settings());
        var (page, image) = PdfWriter.Layout(960, 480, pdf);
        Assert.Equal(720, page.Width, 2);
        Assert.Equal(new SkiaSharp.SKRect(0, 0, 720, 360), image);

        pdf.PageSize = "A4";
        (page, image) = PdfWriter.Layout(96, 96, pdf);
        Assert.Equal(595.3f, page.Width, 1);
        Assert.Equal(72, image.Width, 2);
        pdf.FitToPage = true;
        (_, image) = PdfWriter.Layout(96, 96, pdf);
        Assert.Equal(595.3f, image.Width, 1);
    }

    private const string Sxcu = """
        {
          // A comment like the ones in shared files
          "Version": "14.0.0",
          "Name": "Example host",
          "DestinationType": "ImageUploader",
          "RequestMethod": "POST",
          "RequestURL": "https://api.example.com/upload",
          "Headers": { "Authorization": "Bearer YOUR_TOKEN" },
          "Body": "MultipartFormData",
          "Arguments": { "expiry": 1.0 },
          "FileFormName": "file",
          "URL": "{json:data.link}",
          "DeletionURL": "https://api.example.com/delete/{json:data.id}",
        }
        """;

    [Fact]
    public void An_uploader_file_is_read_and_its_values_can_be_filled_in()
    {
        Core.Languages.Texts.Initialize("en-US",
            [Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../src/Greenshot.Plugin.CustomUploader/Languages"))]);
        var definition = UploaderFile.Read(Sxcu);
        Assert.Equal("Example host", definition.Name);
        Assert.Equal("api.example.com", UploaderFile.GetServer(definition).Host);

        var values = UploaderImport.GetValues(definition);
        var token = values.Single(v => v.Name == "Authorization");
        Assert.True(token.IsSecret);
        Assert.True(token.LooksLikePlaceholder);
        token.Value = "Bearer abc123";
        var filled = UploaderImport.WithValues(definition, values);
        Assert.Equal("Bearer abc123", filled.Headers["Authorization"]);
        Assert.Contains("api.example.com", UploaderImport.DescribeImport(filled, replaces: false));
        Assert.False(UploaderImport.HasNoDeletion(filled));
    }

    [Theory]
    [InlineData("\"RequestURL\": \"https://api.example.com/upload\"", "\"RequestURL\": \"http://api.example.com/upload\"")]
    [InlineData("\"RequestURL\": \"https://api.example.com/upload\"", "\"RequestURL\": \"https://{json:host}/upload\"")]
    [InlineData("\"RequestMethod\": \"POST\"", "\"RequestMethod\": \"GET\"")]
    public void Unsafe_uploader_files_are_refused(string original, string changed)
    {
        Assert.Throws<UploaderFileException>(() => UploaderFile.Read(Sxcu.Replace(original, changed)));
    }

    [Fact]
    public async Task The_request_carries_the_file_and_the_response_gives_the_links()
    {
        var definition = UploaderFile.Read(Sxcu);
        var image = new EncodedImage([1, 2, 3], "png");
        using var request = UploaderRequest.Create(definition, image, "capture.png", null);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("Bearer YOUR_TOKEN", request.Headers.Authorization!.ToString());
        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("name=\"file\"; filename=\"capture.png\"", body);
        Assert.Contains("name=\"expiry\"", body);

        var result = new HttpResult(HttpStatusCode.OK, "OK", new Uri("https://api.example.com/upload"), null,
            "{\"data\": {\"link\": \"https://i.example.com/x.png\", \"id\": \"42\"}}");
        var response = UploaderResponse.From(definition, result, "capture.png");
        Assert.True(response.IsSuccess);
        Assert.Equal("https://i.example.com/x.png", response.Url.AbsoluteUri);
        Assert.Equal("https://api.example.com/delete/42", response.DeletionUrl.AbsoluteUri);
    }

    [Fact]
    public async Task The_secrets_file_is_only_readable_by_the_user()
    {
        var store = new FileSecretStore(Path.Combine(_folder, "secrets.ini"));
        await store.SetAsync("a/b", "label", "s3cret=\n");
        Assert.Equal("s3cret=\n", await store.GetAsync("a/b"));
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(Path.Combine(_folder, "secrets.ini")));
        }
        await store.RemoveAsync("a/b");
        Assert.Null(await store.GetAsync("a/b"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
