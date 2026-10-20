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
using System.IO;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Recipes.Approval;
using log4net;

namespace Greenshot.Plugins
{
    /// <summary>
    /// What happens with a plugin file after the signature check
    /// </summary>
    public enum PluginGateResult
    {
        Load,
        Skip,
        HoldBack
    }

    /// <summary>
    /// A plugin which isn't signed like Greenshot.exe, held back until the user answers the question
    /// </summary>
    public sealed class HeldBackPlugin
    {
        public string FilePath { get; set; }
        public string Name { get; set; }
        public string Sha256 { get; set; }

        /// <summary>
        /// The name of the signer, null when the file isn't signed
        /// </summary>
        public string Signer { get; set; }
    }

    /// <summary>
    /// Loads a plugin only when it is signed like Greenshot.exe, or when its SHA-256 is approved. A release build is signed,
    /// a build from Visual Studio isn't: then the check is off. Hashes which passed the check are kept, so a plugin is only
    /// verified again when it changed (about 6 ms for the signature check, 0.1-0.3 ms for the hash).
    /// See docs/vNext/plugin-loading.md.
    /// </summary>
    public sealed class PluginSignatureGate
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PluginSignatureGate));

        private readonly ICoreConfiguration _configuration;
        private readonly string _greenshotExe;
        private readonly bool _isActive;
        private readonly HashSet<string> _verified;
        private readonly HashSet<string> _verifiedNow = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _greenshotThumbprint;

        public PluginSignatureGate(ICoreConfiguration configuration, string greenshotExe)
        {
            _configuration = configuration;
            _greenshotExe = greenshotExe;
            _verified = new HashSet<string>(ConfigurationRestrictions.Names(configuration.PluginVerifiedHashes), StringComparer.OrdinalIgnoreCase);
            _isActive = IsVerified(greenshotExe);
            if (!_isActive)
            {
                Log.Info("Greenshot.exe isn't signed, plugins are loaded without checking their signature");
            }
        }

        /// <summary>
        /// The plugins held back for the question
        /// </summary>
        public List<HeldBackPlugin> HeldBack { get; } = new List<HeldBackPlugin>();

        /// <summary>
        /// Decide about one plugin file, which the include / exclude settings allow
        /// </summary>
        public PluginGateResult Check(string pluginFile)
        {
            if (!_isActive)
            {
                return PluginGateResult.Load;
            }

            var sha256 = RecipeTrustStore.ComputeSha256(pluginFile);
            if (sha256 == null)
            {
                return PluginGateResult.Skip;
            }

            if (_verified.Contains(sha256))
            {
                _verifiedNow.Add(sha256);
                return PluginGateResult.Load;
            }

            if (IsListed(_configuration.PluginApprovedHashes, sha256))
            {
                return PluginGateResult.Load;
            }

            if (IsListed(_configuration.PluginRejectedHashes, sha256))
            {
                Log.WarnFormat("Not loading the plugin {0}, it was rejected (SHA-256 {1})", pluginFile, sha256);
                return PluginGateResult.Skip;
            }

            _greenshotThumbprint ??= AuthenticodeHelper.GetSignerThumbprint(_greenshotExe);
            if (AuthenticodeHelper.VerifyAuthenticode(pluginFile)
                && string.Equals(AuthenticodeHelper.GetSignerThumbprint(pluginFile), _greenshotThumbprint, StringComparison.OrdinalIgnoreCase))
            {
                _verifiedNow.Add(sha256);
                return PluginGateResult.Load;
            }

            if (!_configuration.AskForUnsignedPlugins)
            {
                Log.WarnFormat("Not loading the plugin {0}, it isn't signed like Greenshot.exe (SHA-256 {1})", pluginFile, sha256);
                return PluginGateResult.Skip;
            }

            Log.WarnFormat("Holding back the plugin {0}, it isn't signed like Greenshot.exe (SHA-256 {1})", pluginFile, sha256);
            HeldBack.Add(new HeldBackPlugin
            {
                FilePath = pluginFile,
                Name = ConfigurationRestrictions.PluginNameFromFile(pluginFile),
                Sha256 = sha256,
                Signer = AuthenticodeHelper.GetSignerName(pluginFile)
            });
            return PluginGateResult.HoldBack;
        }

        /// <summary>
        /// Keep the hashes which passed the check this start: those of plugins which were removed or changed are dropped
        /// </summary>
        public void SaveVerified()
        {
            if (!_isActive || _configuration.IsConstant(nameof(ICoreConfiguration.PluginVerifiedHashes)) || _verified.SetEquals(_verifiedNow))
            {
                return;
            }

            _configuration.PluginVerifiedHashes = _verifiedNow.OrderBy(hash => hash, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Greenshot.exe has a valid signature: a hash in the verified list, or checked now
        /// </summary>
        private bool IsVerified(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                return false;
            }

            var sha256 = RecipeTrustStore.ComputeSha256(file);
            if (_verified.Contains(sha256))
            {
                _verifiedNow.Add(sha256);
                return true;
            }

            if (!AuthenticodeHelper.VerifyAuthenticode(file))
            {
                return false;
            }

            _verifiedNow.Add(sha256);
            return true;
        }

        private static bool IsListed(IEnumerable<string> hashes, string sha256) =>
            ConfigurationRestrictions.Names(hashes).Contains(sha256, StringComparer.OrdinalIgnoreCase);
    }
}
