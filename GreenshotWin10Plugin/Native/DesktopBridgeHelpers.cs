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

using System.Runtime.InteropServices;
using System.Text;
using GreenshotPlugin.Core;

namespace GreenshotWin10Plugin.Native
{
    /// <summary>
    /// Code from https://github.com/qmatteoq/DesktopBridgeHelpers/edit/master/DesktopBridge.Helpers/Helpers.cs
    /// </summary>
    public static class DesktopBridgeHelpers
    {
        const long AppModelErrorNoPackage = 15700L;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder packageFullName);

        private static bool? _isRunningAsUwp;
        public static bool IsRunningAsUwp()
        {
            if (_isRunningAsUwp != null) return _isRunningAsUwp.Value;

            if (WindowsVersion.IsWindows7OrLower)
            {
                _isRunningAsUwp = false;
            }
            else
            {
                int length = 0;
                StringBuilder sb = new StringBuilder(0);
                GetCurrentPackageFullName(ref length, sb);

                sb = new StringBuilder(length);
                int result = GetCurrentPackageFullName(ref length, sb);

                _isRunningAsUwp = result != AppModelErrorNoPackage;
            }

            return _isRunningAsUwp.Value;
        }
    }
}
