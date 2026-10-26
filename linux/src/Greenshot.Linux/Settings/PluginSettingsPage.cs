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

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Http;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Core.Plugins;
using Greenshot.Linux.Helpers;
using Greenshot.Linux.Plugins.Atlassian;
using Greenshot.Linux.Plugins.Cloud;
using Greenshot.Linux.Plugins.CustomUploader;
using Greenshot.Linux.Plugins.CustomUploader.Sxcu;
using Greenshot.Linux.Plugins.ExternalCommand;
using Greenshot.Linux.Plugins.Pdf;

namespace Greenshot.Linux.Settings;

/// <summary>
/// The Plugins tab: switching plugins on and off, and the settings of each plugin
/// </summary>
internal sealed class PluginSettingsPage(ISettingsHost host, Window owner, List<Action> apply)
{
    private static string T(string section, string key, params object?[] args) => Texts.Label(Texts.Get(section, key, args));

    public Control Create()
    {
        var page = Ui.Page(SwitchesSection());
        foreach (var plugin in host.Plugins)
        {
            var section = plugin switch
            {
                ExternalCommandPlugin external => ExternalCommandSection(external),
                PdfPlugin pdf => PdfSection(pdf),
                CustomUploaderPlugin uploader => CustomUploaderSection(uploader),
                BoxPlugin box => CloudSection(box.DisplayName, box.Settings, box.Destination),
                DropboxPlugin dropbox => CloudSection(dropbox.DisplayName, dropbox.Settings, dropbox.Destination),
                AtlassianPlugin atlassian => AtlassianSection(atlassian),
                _ => null,
            };
            if (section != null)
            {
                page.Children.Add(new Separator());
                page.Children.Add(section);
            }
        }

        return page;
    }

    private Control SwitchesSection()
    {
        var policy = host.Policy;
        var checks = new List<Control> { Ui.Note(Texts.Linux("plugins_restart")) };
        foreach (var plugin in host.Plugins)
        {
            var check = Ui.Check(plugin.DisplayName, policy.IsPluginEnabled(plugin.Name));
            if (policy.IsPluginBlocked(plugin.Name) || !policy.CanUserChangePlugins)
            {
                check.IsEnabled = false;
                ToolTip.SetTip(check, Texts.Linux("plugins_locked"));
            }

            apply.Add(() =>
            {
                if (check.IsEnabled)
                {
                    policy.SetPluginEnabled(plugin.Name, check.IsChecked == true);
                }
            });
            checks.Add(check);
        }

        return Ui.Section(Texts.Linux("plugins"), [.. checks]);
    }

    #region External commands

    private Control ExternalCommandSection(ExternalCommandPlugin plugin)
    {
        var commands = plugin.Commands;
        var list = new ListBox { Height = 140 };
        void Refresh() => list.ItemsSource = commands.Commands.ToList();
        Refresh();

        var add = new Button { Content = T("ExternalCommand", "settings_new") };
        var edit = new Button { Content = T("ExternalCommand", "settings_edit") };
        var delete = new Button { Content = T("ExternalCommand", "settings_delete") };
        add.Click += async (_, _) =>
        {
            if (await EditCommandAsync(commands, null) is { } command)
            {
                commands.Save(command);
                Refresh();
            }
        };
        edit.Click += async (_, _) =>
        {
            if (list.SelectedItem is string name && commands.Get(name) is { } existing && await EditCommandAsync(commands, existing) is { } changed)
            {
                commands.Save(changed, name);
                Refresh();
            }
        };
        delete.Click += (_, _) =>
        {
            if (list.SelectedItem is string name)
            {
                commands.Delete(name);
                Refresh();
            }
        };

        return Ui.Section(T("ExternalCommand", "settings_title"),
            Ui.Note(T("ExternalCommand", "settings_instructions")), list, Ui.Horizontal(add, edit, delete));
    }

    private async Task<ExternalCommandDefinition?> EditCommandAsync(ExternalCommandSettings commands, ExternalCommandDefinition? command)
    {
        ExternalCommandDefinition? result = null;
        var name = new TextBox { Text = command?.Name ?? string.Empty, Width = 300 };
        var program = new TextBox { Text = command?.Commandline ?? string.Empty, Width = 300 };
        var arguments = new TextBox { Text = command?.Argument ?? "\"{0}\"", Width = 300 };
        var formats = ImageFormats.Supported.ToList();
        var format = new ComboBox { ItemsSource = formats, SelectedIndex = Math.Max(0, formats.IndexOf(ImageFormats.Normalize(command?.OutputFormat))) };
        var background = Ui.Check(T("ExternalCommand", "label_run_in_background"), command?.RunInBackground ?? true);
        var error = Ui.Note(string.Empty);
        var dialog = new Window
        {
            Title = T("ExternalCommand", "settings_detail_title"),
            Icon = AppIcons.Window,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var ok = new Button { Content = Texts.Linux("ok"), IsDefault = true };
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true };
        ok.Click += (_, _) =>
        {
            var newName = (name.Text ?? string.Empty).Trim().Replace(",", string.Empty);
            if (newName.Length == 0 || string.IsNullOrWhiteSpace(program.Text))
            {
                return;
            }

            if (newName != command?.Name && commands.Commands.Contains(newName))
            {
                error.Text = T("ExternalCommand", "tooltip_duplicate_name");
                return;
            }

            result = new ExternalCommandDefinition(newName, program.Text.Trim(), arguments.Text ?? "\"{0}\"", formats[Math.Max(0, format.SelectedIndex)],
                background.IsChecked == true);
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();
        var buttons = Ui.Horizontal(ok, cancel);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        dialog.Content = Ui.Page(
            Ui.Row(T("ExternalCommand", "label_name"), name),
            Ui.Row(T("ExternalCommand", "label_command"), program),
            Ui.Row(T("ExternalCommand", "label_argument"), arguments),
            Ui.Note(Texts.Get("ExternalCommand", "label_information", "{0}")),
            Ui.Row(T("ExternalCommand", "label_outputimageformat"), format),
            background,
            error,
            buttons);
        await dialog.ShowDialog(owner);
        return result;
    }

    #endregion

    #region PDF

    private Control PdfSection(PdfPlugin plugin)
    {
        var pdf = plugin.Settings;
        string[] keys = ["page_image", "page_custom", "page_a3", "page_a4", "page_a5", "page_a6", "page_letter"];
        var size = new ComboBox
        {
            ItemsSource = keys.Select(k => T("Pdf", k)).ToList(),
            SelectedIndex = Math.Max(0, PdfSettings.PageSizes.ToList().IndexOf(pdf.PageSize)),
        };
        NumericUpDown Mm(double value) => new() { Value = (decimal)value, Minimum = 0, Maximum = 2000, Increment = 1, FormatString = "0.#", Width = 140 };
        var width = Mm(pdf.PageWidthMm);
        var height = Mm(pdf.PageHeightMm);
        var top = Mm(pdf.MarginTopMm);
        var bottom = Mm(pdf.MarginBottomMm);
        var left = Mm(pdf.MarginLeftMm);
        var right = Mm(pdf.MarginRightMm);
        var scaling = new ComboBox
        {
            ItemsSource = new[] { T("Pdf", "scale_shrink"), T("Pdf", "scale_fit") },
            SelectedIndex = pdf.FitToPage ? 1 : 0,
        };
        void UpdateCustom() => width.IsEnabled = height.IsEnabled = size.SelectedIndex == 1;
        size.SelectionChanged += (_, _) => UpdateCustom();
        UpdateCustom();
        apply.Add(() =>
        {
            pdf.PageSize = PdfSettings.PageSizes[Math.Max(0, size.SelectedIndex)];
            pdf.PageWidthMm = (double)(width.Value ?? 210);
            pdf.PageHeightMm = (double)(height.Value ?? 297);
            pdf.MarginTopMm = (double)(top.Value ?? 0);
            pdf.MarginBottomMm = (double)(bottom.Value ?? 0);
            pdf.MarginLeftMm = (double)(left.Value ?? 0);
            pdf.MarginRightMm = (double)(right.Value ?? 0);
            pdf.FitToPage = scaling.SelectedIndex == 1;
        });

        return Ui.Section(T("Pdf", "settings_title"),
            Ui.Row(T("Pdf", "label_page_size"), size),
            Ui.Row(T("Pdf", "label_page_width") + " (mm)", width),
            Ui.Row(T("Pdf", "label_page_height") + " (mm)", height),
            Ui.Row(T("Pdf", "label_top") + " (mm)", top),
            Ui.Row(T("Pdf", "label_bottom") + " (mm)", bottom),
            Ui.Row(T("Pdf", "label_left") + " (mm)", left),
            Ui.Row(T("Pdf", "label_right") + " (mm)", right),
            Ui.Row(T("Pdf", "label_scaling"), scaling));
    }

    #endregion

    #region Custom uploader

    private Control CustomUploaderSection(CustomUploaderPlugin plugin)
    {
        var store = plugin.Store;
        var uploaders = new ListBox { Height = 110 };
        void RefreshUploaders()
        {
            var items = store.Names.Select(n => store.Uploaders.TryGetValue(n, out var d) && d != null
                ? $"{n} – {UploaderFile.DescribeServer(d)}"
                : $"{n} – {T("CustomUploader", "unreadable")}").ToList();
            uploaders.ItemsSource = items.Count > 0 ? items : [T("CustomUploader", "no_uploaders")];
        }

        RefreshUploaders();
        var import = new Button { Content = T("CustomUploader", "import") };
        import.Click += async (_, _) =>
        {
            await ImportAsync(store);
            RefreshUploaders();
        };
        var remove = new Button { Content = T("CustomUploader", "remove") };
        remove.Click += async (_, _) =>
        {
            int index = uploaders.SelectedIndex;
            if (index >= 0 && index < store.Names.Count)
            {
                var name = store.Names[index];
                if (await AskAsync(T("CustomUploader", "remove_question", name)))
                {
                    await store.RemoveAsync(name);
                    RefreshUploaders();
                }
            }
        };

        var uploads = new ListBox { Height = 140 };
        IReadOnlyList<UploadRecord> records = [];
        async Task RefreshUploadsAsync()
        {
            records = await store.GetUploadsAsync();
            uploads.ItemsSource = records.Count > 0
                ? records.Select(r => $"{r.Uploaded.LocalDateTime.ToString("g", Texts.Culture)}  {r.Uploader}  {r.Url}").ToList()
                : [T("CustomUploader", "no_uploads")];
        }

        _ = RefreshUploadsAsync();
        UploadRecord? Selected() => uploads.SelectedIndex >= 0 && uploads.SelectedIndex < records.Count ? records[uploads.SelectedIndex] : null;
        var open = new Button { Content = T("CustomUploader", "open") };
        open.Click += (_, _) =>
        {
            if (Selected() is { } upload)
            {
                ProcessRunner.StartDetached("xdg-open", upload.Url);
            }
        };
        var delete = new Button { Content = T("CustomUploader", "delete") };
        delete.Click += async (_, _) =>
        {
            if (Selected() is not { } upload)
            {
                return;
            }

            if (upload.DeletionUrl == null)
            {
                await AskAsync(T("CustomUploader", "no_deletion_link", upload.Uploader), onlyOk: true);
            }
            else if (await AskAsync(T("CustomUploader", "delete_question", upload.DeletionUrl)))
            {
                ProcessRunner.StartDetached("xdg-open", upload.DeletionUrl);
            }
        };
        var forget = new Button { Content = T("CustomUploader", "forget") };
        forget.Click += async (_, _) =>
        {
            if (Selected() is not { } upload)
            {
                return;
            }

            var question = T("CustomUploader", "forget_question") + (upload.DeletionUrl != null ? "\n\n" + T("CustomUploader", "forget_loses_deletion") : string.Empty);
            if (await AskAsync(question))
            {
                await store.ForgetUploadAsync(upload);
                await RefreshUploadsAsync();
            }
        };

        return Ui.Section(T("CustomUploader", "upload_menu_item"),
            Ui.Note(Texts.Get("CustomUploader", "settings_description")),
            new TextBlock { Text = T("CustomUploader", "uploaders_title"), FontWeight = FontWeight.SemiBold },
            uploaders,
            Ui.Horizontal(import, remove),
            new TextBlock { Text = T("CustomUploader", "uploads_title"), FontWeight = FontWeight.SemiBold },
            uploads,
            Ui.Horizontal(open, delete, forget));
    }

    /// <summary>
    /// Import an .sxcu file: check it, let the user fill in the values (the key), confirm the server, store it in the keyring
    /// </summary>
    private async Task ImportAsync(UploaderStore store)
    {
        if (host.FileChooser == null)
        {
            return;
        }

        var path = await host.FileChooser.OpenFileAsync(T("CustomUploader", "import_title"), [("ShareX custom uploader", "*.sxcu")]);
        if (path == null)
        {
            return;
        }

        UploaderDefinition definition;
        try
        {
            definition = UploaderImport.ReadFile(path);
        }
        catch (Exception ex) when (ex is UploaderFileException or IOException)
        {
            await AskAsync(T("CustomUploader", "import_failed", Path.GetFileName(path), ex.Message), onlyOk: true);
            return;
        }

        while (true)
        {
            var values = UploaderImport.GetValues(definition);
            if (!await EditValuesAsync(definition.Name, values))
            {
                return;
            }

            var placeholders = values.Where(v => v.LooksLikePlaceholder).Select(v => v.Label).ToList();
            if (placeholders.Count > 0 && !await AskAsync(Texts.Get("CustomUploader", "values_placeholder_question", string.Join(", ", placeholders))))
            {
                continue;
            }

            try
            {
                definition = UploaderImport.WithValues(definition, values);
                break;
            }
            catch (UploaderFileException ex)
            {
                await AskAsync(T("CustomUploader", "import_failed", definition.Name, ex.Message), onlyOk: true);
            }
        }

        var question = UploaderImport.DescribeImport(definition, store.Names.Contains(definition.Name, StringComparer.OrdinalIgnoreCase));
        var choices = new List<string> { T("CustomUploader", "import_button") };
        if (UploaderImport.HasNoDeletion(definition))
        {
            choices.Add(T("CustomUploader", "import_with_reminder"));
        }

        var choice = await ChooseAsync(T("CustomUploader", "import_title"), question, choices);
        if (choice < 0)
        {
            return;
        }

        await store.AddAsync(definition, remindBeforeUpload: choice == 1);
        Log.Info($"Imported the uploader {definition.Name} for {UploaderFile.DescribeServer(definition)}");
    }

    private async Task<bool> EditValuesAsync(string name, IReadOnlyList<UploaderValue> values)
    {
        bool next = false;
        var dialog = new Window
        {
            Title = string.Format(Texts.Get("CustomUploader", "values_title"), name),
            Icon = AppIcons.Window,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var rows = new List<Control> { Ui.Note(Texts.Get("CustomUploader", "values_intro")) };
        foreach (var value in values)
        {
            var box = new TextBox { Text = value.Value, Width = 360, PasswordChar = value.IsSecret ? '•' : default };
            box.TextChanged += (_, _) => value.Value = box.Text;
            Control field = box;
            if (value.IsSecret)
            {
                var show = new ToggleButton { Content = T("CustomUploader", "values_show") };
                show.IsCheckedChanged += (_, _) => box.PasswordChar = show.IsChecked == true ? default : '•';
                field = Ui.Horizontal(box, show);
            }

            rows.Add(Ui.Row(value.Label, field));
        }

        var ok = new Button { Content = T("CustomUploader", "values_next"), IsDefault = true };
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true };
        ok.Click += (_, _) =>
        {
            next = true;
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();
        var buttons = Ui.Horizontal(ok, cancel);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        rows.Add(buttons);
        dialog.Content = new ScrollViewer { Content = Ui.Page([.. rows]), MaxHeight = 640 };
        await dialog.ShowDialog(owner);
        return next;
    }

    #endregion

    #region Box and Dropbox

    private Control CloudSection(string title, CloudSettings settings, CloudDestination destination)
    {
        var id = new TextBox { Text = settings.ClientId, Width = 320 };
        var secret = new TextBox { Text = settings.ClientSecret, Width = 320, PasswordChar = '•' };
        var formats = ImageFormats.Supported.ToList();
        var format = new ComboBox { ItemsSource = formats, SelectedIndex = Math.Max(0, formats.IndexOf(settings.UploadFormat)) };
        var link = Ui.Check(T(settings.Section, "label_AfterUploadLinkToClipBoard"), settings.AfterUploadLinkToClipBoard);
        var status = Ui.Note(string.Empty);
        var signOut = new Button { Content = Texts.Linux("sign_out") };
        async Task RefreshAsync()
        {
            bool signedIn = await destination.Client.IsSignedInAsync();
            status.Text = Texts.Linux(signedIn ? "signed_in" : "not_signed_in");
            signOut.IsEnabled = signedIn;
        }

        signOut.Click += async (_, _) =>
        {
            await destination.Client.SignOutAsync();
            await RefreshAsync();
        };
        _ = RefreshAsync();
        apply.Add(() =>
        {
            settings.ClientId = id.Text?.Trim() ?? string.Empty;
            settings.ClientSecret = secret.Text?.Trim() ?? string.Empty;
            settings.UploadFormat = formats[Math.Max(0, format.SelectedIndex)];
            settings.AfterUploadLinkToClipBoard = link.IsChecked == true;
        });

        return Ui.Section(T(settings.Section, "settings_title"),
            Ui.Note(Texts.Linux("cloud_credentials_hint")),
            Ui.Row(Texts.Linux("client_id"), id),
            Ui.Row(Texts.Linux("client_secret"), secret),
            Ui.Row(T(settings.Section, "label_upload_format"), format),
            link,
            Ui.Horizontal(status, signOut));
    }

    #endregion

    #region Jira and Confluence

    private Control AtlassianSection(AtlassianPlugin plugin)
    {
        var jira = plugin.Jira.Settings;
        var confluence = plugin.Confluence.Settings;
        var formats = ImageFormats.Supported.ToList();
        ComboBox Format(string format) => new() { ItemsSource = formats, SelectedIndex = Math.Max(0, formats.IndexOf(format)) };

        var jiraUrl = new TextBox { Text = jira.Url, Width = 360, PlaceholderText = "https://example.atlassian.net" };
        var jiraFormat = Format(jira.UploadFormat);
        var confluenceUrl = new TextBox { Text = confluence.Url, Width = 360, PlaceholderText = "https://example.atlassian.net/wiki" };
        var confluenceFormat = Format(confluence.UploadFormat);
        var timeout = new NumericUpDown { Value = confluence.Timeout, Minimum = 1, Maximum = 1440, Increment = 5, FormatString = "0", Width = 140 };
        var openPage = Ui.Check(T("Confluence", "open_page_after_upload"), confluence.OpenPageAfterUpload);
        var wikiMarkup = Ui.Check(T("Confluence", "copy_wikimarkup"), confluence.CopyWikiMarkupForImageToClipboard);
        var personal = Ui.Check(T("Confluence", "include_person_spaces"), confluence.IncludePersonSpaces);
        apply.Add(() =>
        {
            jira.Url = jiraUrl.Text ?? string.Empty;
            jira.UploadFormat = formats[Math.Max(0, jiraFormat.SelectedIndex)];
            confluence.Url = confluenceUrl.Text ?? string.Empty;
            confluence.UploadFormat = formats[Math.Max(0, confluenceFormat.SelectedIndex)];
            confluence.Timeout = (int)(timeout.Value ?? 30);
            confluence.OpenPageAfterUpload = openPage.IsChecked == true;
            confluence.CopyWikiMarkupForImageToClipboard = wikiMarkup.IsChecked == true;
            confluence.IncludePersonSpaces = personal.IsChecked == true;
        });

        return Ui.Section(Texts.Linux("plugin_atlassian"),
            Ui.Note(Texts.Linux("atlassian_hint")),
            new TextBlock { Text = T("Jira", "settings_title"), FontWeight = FontWeight.SemiBold },
            Ui.Row(T("Jira", "label_url"), jiraUrl),
            Ui.Row(T("Jira", "label_upload_format"), jiraFormat),
            AccountRow(plugin.Jira.Connector.SignIn, () => jira.Url = jiraUrl.Text ?? string.Empty),
            new TextBlock { Text = T("Confluence", "plugin_settings"), FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 8, 0, 0) },
            Ui.Row(T("Confluence", "label_url"), confluenceUrl),
            Ui.Row(T("Confluence", "label_timeout"), timeout),
            Ui.Row(T("Confluence", "upload_format"), confluenceFormat),
            openPage,
            wikiMarkup,
            personal,
            AccountRow(plugin.Confluence.Connector.SignIn, () => confluence.Url = confluenceUrl.Text ?? string.Empty));
    }

    /// <summary>
    /// The account of a server: who is signed in, sign in, test and sign out like the account box of Windows.
    /// The url in the box is used right away, before the settings are saved.
    /// </summary>
    private static Control AccountRow(ServerSignIn signIn, Action applyUrl)
    {
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, MaxWidth = 520 };
        var signInButton = new Button { Content = Texts.Core("signin_account_sign_in") };
        var test = new Button { Content = Texts.Core("signin_account_test") };
        var signOut = new Button { Content = Texts.Core("signin_account_sign_out") };

        async Task RefreshAsync(string? error = null)
        {
            bool stored = await signIn.HasStoredCredentialsAsync();
            status.Text = error
                          ?? (signIn.IsSignedIn ? Texts.Core("signin_account_signed_in", signIn.Account)
                              : stored ? Texts.Core("signin_account_stored")
                              : Texts.Core("signin_account_not_signed_in"));
            test.IsEnabled = signIn.IsSignedIn || stored;
            signOut.IsEnabled = signIn.IsSignedIn || stored;
        }

        async Task RunAsync(Func<Task> action)
        {
            applyUrl();
            if (string.IsNullOrEmpty(signIn.Url))
            {
                return;
            }

            try
            {
                await action();
                await RefreshAsync();
            }
            catch (Exception ex) when (ex is HttpRequestException or UnauthorizedAccessException or System.Text.Json.JsonException
                                           or TaskCanceledException or UriFormatException or InvalidOperationException)
            {
                await RefreshAsync(ServerSignIn.DescribeError(ex));
            }
        }

        signInButton.Click += async (_, _) => await RunAsync(() => signIn.SignInAsync());
        test.Click += async (_, _) => await RunAsync(() => signIn.TestAsync());
        signOut.Click += async (_, _) => await RunAsync(signIn.SignOutAsync);
        _ = RefreshAsync();
        return Ui.Row(Texts.Core("signin_account"), new StackPanel { Spacing = 6, Children = { status, Ui.Horizontal(signInButton, test, signOut) } });
    }

    #endregion

    private async Task<bool> AskAsync(string message, bool onlyOk = false) =>
        await ChooseAsync("Greenshot", message, onlyOk ? [Texts.Linux("ok")] : [Texts.Linux("yes")]) == 0;

    /// <summary>
    /// A question with the choices and Cancel; the index of the choice, -1 for Cancel
    /// </summary>
    private async Task<int> ChooseAsync(string title, string message, IReadOnlyList<string> choices)
    {
        int result = -1;
        var dialog = new Window
        {
            Title = title,
            Icon = AppIcons.Window,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        for (int i = 0; i < choices.Count; i++)
        {
            int index = i;
            var button = new Button { Content = choices[i] };
            button.Click += (_, _) =>
            {
                result = index;
                dialog.Close();
            };
            buttons.Children.Add(button);
        }

        if (choices.Count > 1 || choices[0] != Texts.Linux("ok"))
        {
            var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true, IsDefault = true };
            cancel.Click += (_, _) => dialog.Close();
            buttons.Children.Add(cancel);
        }

        dialog.Content = Ui.Page(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 520 }, buttons);
        await dialog.ShowDialog(owner);
        return result;
    }
}
