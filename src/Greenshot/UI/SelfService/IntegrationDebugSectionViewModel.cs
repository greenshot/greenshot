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

#if DEBUG
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Greenshot.Base.Wpf;

namespace Greenshot.UI.SelfService
{
    /// <summary>
    /// Self-service section available exclusively in DEBUG builds to register and test
    /// Native Messaging hosts (Chrome, Edge, Firefox) and the greenshot:// URL protocol
    /// for the current user (HKCU) without administrative elevation.
    /// </summary>
    public class IntegrationDebugSectionViewModel : SelfServiceSectionViewModel
    {
        public override string Id => "debug_integration";
        public override string Title => "Integration (Debug)";
        public override string Subtitle => "Register Native Messaging host & URL scheme for current user (HKCU)";
        public override string Icon => "🛠️";

        private string _extensionId;
        private string _chromeStatus;
        private string _edgeStatus;
        private string _firefoxStatus;
        private string _urlSchemeStatus;
        private string _proxyStatus;
        private string _actionFeedback;
        private Brush _actionFeedbackBrush;

        public string ExtensionId
        {
            get => _extensionId;
            set
            {
                if (_extensionId != value)
                {
                    _extensionId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ChromeStatus { get => _chromeStatus; set { _chromeStatus = value; OnPropertyChanged(); } }
        public string EdgeStatus { get => _edgeStatus; set { _edgeStatus = value; OnPropertyChanged(); } }
        public string FirefoxStatus { get => _firefoxStatus; set { _firefoxStatus = value; OnPropertyChanged(); } }
        public string UrlSchemeStatus { get => _urlSchemeStatus; set { _urlSchemeStatus = value; OnPropertyChanged(); } }
        public string ProxyStatus { get => _proxyStatus; set { _proxyStatus = value; OnPropertyChanged(); } }
        public string ActionFeedback 
        { 
            get => _actionFeedback; 
            set 
            { 
                _actionFeedback = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(HasActionFeedback)); 
            } 
        }
        public bool HasActionFeedback => !string.IsNullOrEmpty(_actionFeedback);
        public Brush ActionFeedbackBrush { get => _actionFeedbackBrush; set { _actionFeedbackBrush = value; OnPropertyChanged(); } }

        public string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;
        public string ProxyPath => Path.Combine(BaseDirectory, "greenshot-proxy.exe");
        public string ChromeManifestPath => Path.Combine(BaseDirectory, "org.greenshot.proxy.json");
        public string FirefoxManifestPath => Path.Combine(BaseDirectory, "org.greenshot.proxy-firefox.json");
        public string ExtensionDirectory => Path.GetFullPath(Path.Combine(BaseDirectory, @"..\..\..\..\Greenshot.BrowserExtension"));

        public IntegrationDebugSectionViewModel()
        {
            EnsureManifestsExist();
            LoadCurrentExtensionId();
            Refresh();
        }

        public override void Refresh()
        {
            CheckStatus();
        }

        public void CheckStatus()
        {
            // Proxy binary
            ProxyStatus = File.Exists(ProxyPath) ? "Found: " + ProxyPath : "Missing in " + BaseDirectory;

            // Chrome Native Messaging Host
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Google\Chrome\NativeMessagingHosts\org.greenshot.proxy"))
            {
                var val = key?.GetValue(null)?.ToString();
                ChromeStatus = !string.IsNullOrEmpty(val) ? "Registered (HKCU): " + val : "Not registered in HKCU";
            }

            // Edge Native Messaging Host
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Edge\NativeMessagingHosts\org.greenshot.proxy"))
            {
                var val = key?.GetValue(null)?.ToString();
                EdgeStatus = !string.IsNullOrEmpty(val) ? "Registered (HKCU): " + val : "Not registered in HKCU";
            }

            // Firefox Native Messaging Host
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Mozilla\NativeMessagingHosts\org.greenshot.proxy"))
            {
                var val = key?.GetValue(null)?.ToString();
                FirefoxStatus = !string.IsNullOrEmpty(val) ? "Registered (HKCU): " + val : "Not registered in HKCU";
            }

            // Custom URL Scheme: greenshot://
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\greenshot\shell\open\command"))
            {
                var val = key?.GetValue(null)?.ToString();
                UrlSchemeStatus = !string.IsNullOrEmpty(val) ? "Registered (HKCU): " + val : "Not registered in HKCU";
            }
        }

        public void RegisterAll()
        {
            try
            {
                EnsureManifestsExist();
                SaveExtensionIdInternal();

                // Chrome
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Google\Chrome\NativeMessagingHosts\org.greenshot.proxy"))
                {
                    key?.SetValue(null, ChromeManifestPath);
                }

                // Edge
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Edge\NativeMessagingHosts\org.greenshot.proxy"))
                {
                    key?.SetValue(null, ChromeManifestPath);
                }

                // Firefox
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Mozilla\NativeMessagingHosts\org.greenshot.proxy"))
                {
                    key?.SetValue(null, FirefoxManifestPath);
                }

                // Custom URL Scheme: greenshot://
                string exePath = Path.Combine(BaseDirectory, "Greenshot.exe");
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\greenshot"))
                {
                    key?.SetValue(null, "URL:Greenshot Protocol");
                    key?.SetValue("URL Protocol", "");
                    using (var iconKey = key.CreateSubKey("DefaultIcon"))
                    {
                        iconKey?.SetValue(null, $"\"{exePath}\",0");
                    }
                    using (var cmdKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        cmdKey?.SetValue(null, $"\"{ProxyPath}\" \"%1\"");
                    }
                }

                CheckStatus();
                ActionFeedback = "Successfully registered Chrome, Edge, Firefox Native Hosts and greenshot:// URL Scheme in HKCU!";
                ActionFeedbackBrush = Brushes.ForestGreen;
            }
            catch (Exception ex)
            {
                ActionFeedback = "Registration failed: " + ex.Message;
                ActionFeedbackBrush = Brushes.Red;
            }
        }

        public void UnregisterAll()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Google\Chrome\NativeMessagingHosts\org.greenshot.proxy", false);
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Edge\NativeMessagingHosts\org.greenshot.proxy", false);
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Mozilla\NativeMessagingHosts\org.greenshot.proxy", false);
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\greenshot", false);

                CheckStatus();
                ActionFeedback = "Removed debug native messaging hosts and greenshot:// protocol from HKCU.";
                ActionFeedbackBrush = Brushes.DodgerBlue;
            }
            catch (Exception ex)
            {
                ActionFeedback = "Unregister failed: " + ex.Message;
                ActionFeedbackBrush = Brushes.Red;
            }
        }

        public void SaveExtensionId()
        {
            try
            {
                SaveExtensionIdInternal();
                ActionFeedback = "Saved extension ID to org.greenshot.proxy.json!";
                ActionFeedbackBrush = Brushes.ForestGreen;
            }
            catch (Exception ex)
            {
                ActionFeedback = "Failed to save extension ID: " + ex.Message;
                ActionFeedbackBrush = Brushes.Red;
            }
        }

        private void SaveExtensionIdInternal()
        {
            if (!File.Exists(ChromeManifestPath)) return;
            string id = ExtensionId?.Trim();
            if (string.IsNullOrEmpty(id)) return;

            string json = File.ReadAllText(ChromeManifestPath);
            var jobj = JObject.Parse(json);
            var origins = new JArray { $"chrome-extension://{id}/" };
            jobj["allowed_origins"] = origins;
            File.WriteAllText(ChromeManifestPath, jobj.ToString(Newtonsoft.Json.Formatting.Indented));
        }

        private void LoadCurrentExtensionId()
        {
            try
            {
                if (File.Exists(ChromeManifestPath))
                {
                    string json = File.ReadAllText(ChromeManifestPath);
                    var jobj = JObject.Parse(json);
                    if (jobj["allowed_origins"] is JArray arr && arr.Count > 0)
                    {
                        string first = arr[0]?.ToString() ?? "";
                        if (first.StartsWith("chrome-extension://"))
                        {
                            first = first.Substring("chrome-extension://".Length).TrimEnd('/');
                        }
                        ExtensionId = first;
                    }
                }
            }
            catch { }
        }

        private void EnsureManifestsExist()
        {
            try
            {
                if (!File.Exists(ChromeManifestPath) || !File.Exists(FirefoxManifestPath))
                {
                    string installerDir = Path.GetFullPath(Path.Combine(BaseDirectory, @"..\..\..\..\Greenshot-Installer\additional_files"));
                    string srcChrome = Path.Combine(installerDir, "org.greenshot.proxy.json");
                    string srcFirefox = Path.Combine(installerDir, "org.greenshot.proxy-firefox.json");

                    if (!File.Exists(ChromeManifestPath) && File.Exists(srcChrome))
                    {
                        File.Copy(srcChrome, ChromeManifestPath, true);
                    }
                    if (!File.Exists(FirefoxManifestPath) && File.Exists(srcFirefox))
                    {
                        File.Copy(srcFirefox, FirefoxManifestPath, true);
                    }
                }
            }
            catch { }
        }

        public void TestUrlScheme()
        {
            try
            {
                Process.Start(new ProcessStartInfo("greenshot://capture?target=screen") { UseShellExecute = true });
                ActionFeedback = "Launched 'greenshot://capture?target=screen'. Check Greenshot!";
                ActionFeedbackBrush = Brushes.DodgerBlue;
            }
            catch (Exception ex)
            {
                ActionFeedback = "Failed to launch URL scheme: " + ex.Message;
                ActionFeedbackBrush = Brushes.Red;
            }
        }

        public void OpenExtensionFolder()
        {
            try
            {
                if (Directory.Exists(ExtensionDirectory))
                {
                    Process.Start("explorer.exe", ExtensionDirectory);
                }
                else
                {
                    ActionFeedback = "Extension directory not found: " + ExtensionDirectory;
                    ActionFeedbackBrush = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                ActionFeedback = "Error opening folder: " + ex.Message;
                ActionFeedbackBrush = Brushes.Red;
            }
        }
    }
}
#endif
