#region Greenshot GNU General License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General License for more details.
// 
// You should have received a copy of the GNU General License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Security;
using Greenshot.Addons.Core;

namespace Greenshot.Components
{
    /// <summary>
    ///     This exports a IAuthenticationProvider which manages the rights in the configuration
    ///     This is used to show or hide elements in the UI depending on the available rights
    /// </summary>
    [Export(typeof(IAuthenticationProvider))]
    public class AuthenticationProvider : PropertyChangedBase, IAuthenticationProvider
    {
        [Import]
        private ICoreConfiguration CoreConfiguration { get; set; }

        public bool HasPermissions(IEnumerable<string> neededPermissions, PermissionOperations permissionOperation = PermissionOperations.Or)
        {
            // Argument check
            if (neededPermissions == null)
            {
                throw new ArgumentNullException(nameof(neededPermissions));
            }

            if (CoreConfiguration.Permissions== null || CoreConfiguration.Permissions.Count == 0)
            {
                return false;
            }

            // Create a clean list of permissions needed
            var permissionsToCompare = neededPermissions.Where(s => !string.IsNullOrWhiteSpace(s)).Select(permission => permission.Trim().ToLowerInvariant()).ToList();

            if (permissionOperation == PermissionOperations.Or)
            {
                return permissionsToCompare.Any(permission => CoreConfiguration.Permissions.Contains(permission));
            }
            return permissionsToCompare.All(permission => CoreConfiguration.Permissions.Contains(permission));
        }

        /// <summary>
        ///     Add a permission and inform via INotifyPropertyChanged events of changes
        /// </summary>
        /// <param name="permission">string with the permission</param>
        public void AddPermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                throw new ArgumentNullException(nameof(permission));
            }
            var newPermission = permission.Trim().ToLowerInvariant();
            CoreConfiguration.Permissions.Add(newPermission);
            NotifyOfPropertyChange(nameof(HasPermissions));
        }

        /// <summary>
        ///     Remove a permission and inform via INotifyPropertyChanged events of changes
        /// </summary>
        /// <param name="permission">string with the permission</param>
        public void RemovePermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                throw new ArgumentNullException(nameof(permission));
            }
            var removingPermission = permission.Trim().ToLowerInvariant();
            CoreConfiguration.Permissions.Remove(removingPermission);
            NotifyOfPropertyChange(nameof(HasPermissions));
        }
    }
}
