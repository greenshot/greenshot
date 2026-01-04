/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using log4net;
using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Greenshot.Helpers
{
    /// <summary>
    ///     This protects your resources or application from running more than once
    ///     Simplifies the usage of the Mutex class, as described here:
    ///     https://msdn.microsoft.com/en-us/library/System.Threading.Mutex.aspx
    /// </summary>
    public class ResourceMutex : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ResourceMutex));
        private readonly string _mutexId;
        private readonly string _resourceName;
        private Mutex _applicationMutex;

        /// <summary>
        ///     Private constructor
        /// </summary>
        /// <param name="mutexId"></param>
        /// <param name="resourceName"></param>
        private ResourceMutex(string mutexId, string resourceName = null)
        {
            _mutexId = mutexId;
            _resourceName = resourceName ?? "some resource";
        }

        /// <summary>
        ///     Test if the Mutex was created and locked.
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        ///     Create a ResourceMutex for the specified mutex id and resource-name
        /// </summary>
        /// <param name="mutexId">ID of the mutex, preferably a Guid as string</param>
        /// <param name="resourceName">Name of the resource to lock, e.g your application name, usefull for logs</param>
        /// <param name="global">true to have a global mutex see: https://msdn.microsoft.com/en-us/library/bwe34f1k.aspx </param>
        public static ResourceMutex Create(string mutexId, string resourceName = null, bool global = false)
        {
            var applicationMutex = new ResourceMutex((global ? @"Global\" : @"Local\") + mutexId, resourceName);
            applicationMutex.Lock();
            return applicationMutex;
        }

        /// <summary>
        ///     This tries to get the Mutex, which takes care of having multiple instances running
        /// </summary>
        /// <returns>true if it worked, false if another instance is already running or something went wrong</returns>
        public bool Lock()
        {
            Log.DebugFormat("{0} is trying to get Mutex {1}", _resourceName, _mutexId);

            IsLocked = true;
            // check whether there's an local instance running already, but use local so this works in a multi-user environment
            try
            {
                // Added Mutex Security, hopefully this prevents the UnauthorizedAccessException more gracefully
                var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                var mutexSecurity = new MutexSecurity();
                mutexSecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.FullControl, AccessControlType.Allow));
                mutexSecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.ChangePermissions, AccessControlType.Deny));
                mutexSecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.Delete, AccessControlType.Deny));

                // 1) Create Mutex
                _applicationMutex = new Mutex(true, _mutexId, out var createdNew, mutexSecurity);
                // 2) if the mutex wasn't created new get the right to it, this returns false if it's already locked
                if (!createdNew && !_applicationMutex.WaitOne(100, false))
                {
                    Log.InfoFormat("{0} is already in use, mutex {1} is NOT locked for the caller", _resourceName, _mutexId);
                    IsLocked = false;
                    // Clean up
                    _applicationMutex.Close();
                    _applicationMutex = null;
                }
                else
                {
                    Log.InfoFormat(createdNew ? "{0} has created & claimed the mutex {1}" : "{0} has claimed the mutex {1}", _resourceName, _mutexId);
                }
            }
            catch (AbandonedMutexException e)
            {
                // Another instance didn't cleanup correctly!
                // we can ignore the exception, it happened on the "waitOne" but still the mutex belongs to us
                Log.WarnFormat("{0} didn't cleanup correctly, but we got the mutex {1}.", _resourceName, _mutexId);
                Log.Warn(e);
            }
            catch (UnauthorizedAccessException e)
            {
                Log.ErrorFormat("{0} is most likely already running for a different user in the same session, can't create/get mutex {1} due to error.", _resourceName, _mutexId);
                Log.Error(e);
                IsLocked = false;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Problem obtaining the Mutex {1} for {0}, assuming it was already taken!", _resourceName, _mutexId);
                Log.Error(ex);
                IsLocked = false;
            }

            return IsLocked;
        }

        //  To detect redundant Dispose calls
        private bool _disposedValue;

        /// <summary>
        ///     The real disposing code
        /// </summary>
        /// <param name="disposing">true if dispose is called, false when the finalizer is called</param>
        protected void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (_applicationMutex != null)
            {
                try
                {
                    _applicationMutex.ReleaseMutex();
                    _applicationMutex = null;
                    Log.InfoFormat("Released Mutex {0} for {1}", _mutexId, _resourceName);
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Error releasing Mutex {0} for {1}", _mutexId, _resourceName);
                    Log.Error(ex);
                }
            }

            _disposedValue = true;
        }

        /// <summary>
        ///     Make sure the ApplicationMutex is disposed when the finalizer is called
        /// </summary>
        ~ResourceMutex()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        /// <summary>
        ///     The dispose interface, which calls Dispose(true) to signal that dispose is called.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}