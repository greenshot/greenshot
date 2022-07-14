/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;

namespace Greenshot.Plugin.Jira
{
    /// <summary>
    ///     This abstract class builds a base for a simple async memory cache.
    /// </summary>
    /// <typeparam name="TKey">Type for the key</typeparam>
    /// <typeparam name="TResult">Type for the stored value</typeparam>
    public abstract class AsyncMemoryCache<TKey, TResult> where TResult : class
    {
        private static readonly Task<TResult> EmptyValueTask = Task.FromResult<TResult>(null);
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
        private readonly MemoryCache _cache = new(Guid.NewGuid().ToString());
        private readonly LogSource _log = new();

        /// <summary>
        ///     Set the timespan for items to expire.
        /// </summary>
        public TimeSpan? ExpireTimeSpan { get; set; }

        /// <summary>
        ///     Set the timespan for items to slide.
        /// </summary>
        public TimeSpan? SlidingTimeSpan { get; set; }

        /// <summary>
        ///     Specifies if the RemovedCallback needs to be called
        ///     If this is active, ActivateUpdateCallback should be false
        /// </summary>
        protected bool ActivateRemovedCallback { get; set; } = true;

        /// <summary>
        ///     Specifies if the UpdateCallback needs to be called.
        ///     If this is active, ActivateRemovedCallback should be false
        /// </summary>
        protected bool ActivateUpdateCallback { get; set; } = false;

        /// <summary>
        /// Implement this method, it should create an instance of TResult via the supplied TKey.
        /// </summary>
        /// <param name="key">TKey</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>TResult</returns>
        protected abstract Task<TResult> CreateAsync(TKey key, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Creates a key under which the object is stored or retrieved, default is a toString on the object.
        /// </summary>
        /// <param name="keyObject">TKey</param>
        /// <returns>string</returns>
        protected virtual string CreateKey(TKey keyObject) => keyObject.ToString();

        /// <summary>
        ///     Get a task element from the cache, if this is not available call the create function.
        /// </summary>
        /// <param name="keyObject">object for the key</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task with TResult</returns>
        public Task<TResult> GetOrCreateAsync(TKey keyObject, CancellationToken cancellationToken = default)
        {
            var key = CreateKey(keyObject);
            return _cache.Get(key) as Task<TResult> ?? GetOrCreateInternalAsync(keyObject, null, cancellationToken);
        }

        /// <summary>
        ///     This takes care of the real async part of the code.
        /// </summary>
        /// <param name="keyObject"></param>
        /// <param name="cacheItemPolicy">CacheItemPolicy for when you want more control over the item</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>TResult</returns>
        private async Task<TResult> GetOrCreateInternalAsync(TKey keyObject, CacheItemPolicy cacheItemPolicy = null, CancellationToken cancellationToken = default)
        {
            var key = CreateKey(keyObject);
            var completionSource = new TaskCompletionSource<TResult>();

            if (cacheItemPolicy == null)
            {
                cacheItemPolicy = new CacheItemPolicy
                {
                    AbsoluteExpiration = ExpireTimeSpan.HasValue ? DateTimeOffset.Now.Add(ExpireTimeSpan.Value) : ObjectCache.InfiniteAbsoluteExpiration,
                    SlidingExpiration = SlidingTimeSpan ?? ObjectCache.NoSlidingExpiration
                };
                if (ActivateUpdateCallback)
                {
                    cacheItemPolicy.UpdateCallback = UpdateCallback;
                }

                if (ActivateRemovedCallback)
                {
                    cacheItemPolicy.RemovedCallback = RemovedCallback;
                }
            }

            // Test if we got an existing object or our own
            if (_cache.AddOrGetExisting(key, completionSource.Task, cacheItemPolicy) is Task<TResult> result && !completionSource.Task.Equals(result))
            {
                return await result.ConfigureAwait(false);
            }

            await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                result = _cache.AddOrGetExisting(key, completionSource.Task, cacheItemPolicy) as Task<TResult>;
                if (result != null && !completionSource.Task.Equals(result))
                {
                    return await result.ConfigureAwait(false);
                }

                // Now, start the background task, which will set the completionSource with the correct response
                // ReSharper disable once MethodSupportsCancellation
                // ReSharper disable once UnusedVariable
                var ignoreBackgroundTask = Task.Run(async () =>
                {
                    try
                    {
                        var backgroundResult = await CreateAsync(keyObject, cancellationToken).ConfigureAwait(false);
                        completionSource.TrySetResult(backgroundResult);
                    }
                    catch (TaskCanceledException)
                    {
                        completionSource.TrySetCanceled();
                    }
                    catch (Exception ex)
                    {
                        completionSource.TrySetException(ex);
                    }
                });
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return await completionSource.Task.ConfigureAwait(false);
        }

        /// <summary>
        ///     Override to know when an item is removed, make sure to configure ActivateUpdateCallback / ActivateRemovedCallback
        /// </summary>
        /// <param name="cacheEntryRemovedArguments">CacheEntryRemovedArguments</param>
        protected void RemovedCallback(CacheEntryRemovedArguments cacheEntryRemovedArguments)
        {
            _log.Verbose().WriteLine("Item {0} removed due to {1}.", cacheEntryRemovedArguments.CacheItem.Key, cacheEntryRemovedArguments.RemovedReason);
            if (cacheEntryRemovedArguments.CacheItem.Value is IDisposable disposable)
            {
                _log.Debug().WriteLine("Disposed cached item.");
                disposable.Dispose();
            }
        }

        /// <summary>
        ///     Override to modify the cache behaviour when an item is about to be removed, make sure to configure
        ///     ActivateUpdateCallback / ActivateRemovedCallback
        /// </summary>
        /// <param name="cacheEntryUpdateArguments">CacheEntryUpdateArguments</param>
        protected void UpdateCallback(CacheEntryUpdateArguments cacheEntryUpdateArguments) => _log.Verbose().WriteLine("Update request for {0} due to {1}.", cacheEntryUpdateArguments.Key, cacheEntryUpdateArguments.RemovedReason);
    }
}