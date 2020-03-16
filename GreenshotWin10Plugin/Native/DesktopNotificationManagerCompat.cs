// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.UI.Notifications;

namespace GreenshotWin10Plugin.Native
{
    public static class DesktopNotificationManagerCompat
    {
        public const string TOAST_ACTIVATED_LAUNCH_ARG = "-ToastActivated";

        private static bool _registeredAumidAndComServer;
        private static string _applicationUserModelId;
        private static bool _registeredActivator;

        /// <summary>
        /// If not running under the Desktop Bridge, you must call this method to register your applicationUserModelId (AUMID) with the Compat library and to
        /// register your COM CLSID and EXE in LocalServer32 registry. Feel free to call this regardless, and we will no-op if running
        /// under Desktop Bridge. Call this upon application startup, before calling any other APIs.
        /// </summary>
        /// <param name="applicationUserModelId">An applicationUserModelId (AUMID) that uniquely identifies your application.</param>
        public static void RegisterAumidAndComServer<T>(string applicationUserModelId) where T : NotificationActivator
        {
            if (string.IsNullOrWhiteSpace(applicationUserModelId))
            {
                throw new ArgumentException("You must provide an Application User Model Id (AUMID).", nameof(applicationUserModelId));
            }

            // If running as Desktop Bridge
            if (DesktopBridgeHelpers.IsRunningAsUwp())
            {
                // Clear the AUMID since Desktop Bridge doesn't use it, and then we're done.
                // Desktop Bridge apps are registered with platform through their manifest.
                // Their LocalServer32 key is also registered through their manifest.
                _applicationUserModelId = null;
                _registeredAumidAndComServer = true;
                return;
            }

            _applicationUserModelId = applicationUserModelId;

            string exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath != null)
            {
                RegisterComServer<T>(exePath);
            }

            _registeredAumidAndComServer = true;
        }

        /// <summary>
        /// Register the application an a com server
        /// </summary>
        /// <typeparam name="T">type to register for</typeparam>
        /// <param name="exePath">string</param>
        private static void RegisterComServer<T>(string exePath) where T : NotificationActivator
        {
            // We register the EXE to start up when the notification is activated
            var guid = typeof(T).GUID;
            if (guid == null)
            {
                throw new ArgumentException("You must provide an Guid on your NotificationActivator.");
            }
            string regString = $"SOFTWARE\\Classes\\CLSID\\{{{guid}}}\\LocalServer32";
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regString);

            // Include a flag so we know this was a toast activation and should wait for COM to process
            // We also wrap EXE path in quotes for extra security
            key?.SetValue(null, '"' + exePath + '"' + " " + TOAST_ACTIVATED_LAUNCH_ARG);
        }

        /// <summary>
        /// Registers the activator type as a COM server client so that Windows can launch your activator.
        /// </summary>
        /// <typeparam name="T">Your implementation of NotificationActivator. Must have GUID and ComVisible attributes on class.</typeparam>
        public static void RegisterActivator<T>() where T : NotificationActivator
        {
            // Register type
            var regService = new RegistrationServices();

            regService.RegisterTypeForComClients(typeof(T), RegistrationClassContext.LocalServer, RegistrationConnectionType.MultipleUse);

            _registeredActivator = true;
        }

        /// <summary>
        /// Creates a toast notifier. You must have called <see cref="RegisterActivator{T}"/> first (and also <see cref="RegisterAumidAndComServer{T}"/> if you're a classic Win32 app), or this will throw an exception.
        /// </summary>
        /// <returns>ToastNotifier</returns>
        public static ToastNotifier CreateToastNotifier()
        {
            EnsureRegistered();

            if (_applicationUserModelId != null)
            {
                // Non-Desktop Bridge
                return ToastNotificationManager.CreateToastNotifier(_applicationUserModelId);
            }

            // Desktop Bridge
            return ToastNotificationManager.CreateToastNotifier();
        }

        /// <summary>
        /// Gets the <see cref="DesktopNotificationHistoryCompat"/> object. You must have called <see cref="RegisterActivator{T}"/> first (and also <see cref="RegisterAumidAndComServer{T}"/> if you're a classic Win32 app), or this will throw an exception.
        /// </summary>
        public static DesktopNotificationHistoryCompat History
        {
            get
            {
                EnsureRegistered();

                return new DesktopNotificationHistoryCompat(_applicationUserModelId);
            }
        }

        /// <summary>
        /// Checks if the AUMID is correctly registered, if not this throws an exception
        /// </summary>
        private static void EnsureRegistered()
        {
            // If not registered AUMID yet
            if (!_registeredAumidAndComServer)
            {
                // Check if Desktop Bridge
                if (DesktopBridgeHelpers.IsRunningAsUwp())
                {
                    // Implicitly registered, all good!
                    _registeredAumidAndComServer = true;
                }

                else
                {
                    // Otherwise, incorrect usage
                    throw new Exception("You must call RegisterAumidAndComServer first.");
                }
            }

            // If not registered activator yet
            if (!_registeredActivator)
            {
                // Incorrect usage
                throw new Exception("You must call RegisterActivator first.");
            }
        }

        /// <summary>
        /// Gets a boolean representing whether http images can be used within toasts. This is true if running under Desktop Bridge.
        /// </summary>
        public static bool CanUseHttpImages => DesktopBridgeHelpers.IsRunningAsUwp();
    }
}