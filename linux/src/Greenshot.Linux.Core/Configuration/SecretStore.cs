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

using System.Text;

namespace Greenshot.Linux.Core.Configuration;

/// <summary>
/// Where keys, tokens and uploader files are kept. The Windows version obfuscates them in greenshot.ini with a key that
/// is in its source; on Linux they go to the keyring of the desktop (Secret Service: GNOME Keyring, KWallet, KeePassXC)
/// with secret-tool. Without it they go to a file only the user can read.
/// </summary>
public interface ISecretStore
{
    Task<string?> GetAsync(string key);

    Task SetAsync(string key, string label, string secret);

    Task RemoveAsync(string key);
}

public static class SecretStores
{
    /// <summary>
    /// The keyring when secret-tool is installed, else the file
    /// </summary>
    public static ISecretStore Create() => ProcessRunner.IsOnPath("secret-tool") ? new KeyringSecretStore() : new FileSecretStore();
}

/// <summary>
/// The Secret Service through secret-tool (libsecret); the secret goes through stdin, never on the command line
/// </summary>
public sealed class KeyringSecretStore : ISecretStore
{
    private const string Application = "greenshot";

    public async Task<string?> GetAsync(string key)
    {
        var result = await ProcessRunner.RunAsync("secret-tool", ["lookup", "application", Application, "key", key], timeout: TimeSpan.FromSeconds(60))
            .ConfigureAwait(false);
        return result.ExitCode == 0 && result.Output.Length > 0 ? Encoding.UTF8.GetString(result.Output) : null;
    }

    public async Task SetAsync(string key, string label, string secret)
    {
        var result = await ProcessRunner.RunAsync("secret-tool", ["store", "--label", $"Greenshot: {label}", "application", Application, "key", key],
            Encoding.UTF8.GetBytes(secret), TimeSpan.FromSeconds(60)).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"The keyring didn't store the secret: {result.Error.Trim()}");
        }
    }

    public Task RemoveAsync(string key) =>
        ProcessRunner.RunAsync("secret-tool", ["clear", "application", Application, "key", key], timeout: TimeSpan.FromSeconds(60));
}

/// <summary>
/// secrets.ini next to greenshot.ini, readable only by the user (0600), for systems without a keyring
/// </summary>
public sealed class FileSecretStore(string? path = null) : ISecretStore
{
    private const string Section = "Secrets";
    private readonly string _path = path ?? Path.Combine(XdgDirectories.ConfigHome, "greenshot", "secrets.ini");
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<string?> GetAsync(string key)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var value = IniFile.Load(_path).Get(Section, key);
            return value == null ? null : Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SetAsync(string key, string label, string secret)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var ini = IniFile.Load(_path);
            ini.Set(Section, key, Convert.ToBase64String(Encoding.UTF8.GetBytes(secret)));
            Save(ini);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveAsync(string key)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var ini = IniFile.Load(_path);
            ini.Remove(Section, key);
            Save(ini);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Through a temporary file which is created readable only by the user, so the secrets are never readable by others
    /// </summary>
    private void Save(IniFile ini)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temp = _path + ".tmp";
        File.Delete(temp);
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        using (var stream = new FileStream(temp, options))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
        {
            writer.Write(ini.ToString());
        }

        File.Move(temp, _path, overwrite: true);
    }
}
