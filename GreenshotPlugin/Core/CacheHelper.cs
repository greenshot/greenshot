/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Caching;

namespace GreenshotPlugin.Core {
	public delegate void CacheObjectExpired(string key, object cacheValue);

	/// <summary>
	/// Description of CacheHelper.
	/// </summary>
	public class CacheHelper<T> {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger("CacheHelper");
		private Cache cache = HttpRuntime.Cache;
		private string prefix;
		private double defaultExpiration = 10*60;	// 10 Minutes
		private CacheItemRemovedCallback defaultCallback = null;
		private CacheObjectExpired expiredCallback = null;
		
		public CacheHelper(string prefix) {
			defaultCallback = new CacheItemRemovedCallback(OnRemoved);
			this.prefix = prefix + ".";
		}

		public CacheHelper(string prefix, double defaultExpiration) : this(prefix) {
			this.defaultExpiration = defaultExpiration;
		}

		public CacheHelper(string prefix, double defaultExpiration, CacheObjectExpired expiredCallback) : this(prefix, defaultExpiration) {
			this.expiredCallback = expiredCallback;
		}
		
		private void OnRemoved(string key, object cacheValue, CacheItemRemovedReason reason) {
			LOG.DebugFormat("The item with key '{0}' is being removed from the cache with reason: {1}", key, reason);
			switch (reason) {
				case CacheItemRemovedReason.Expired:
					if (expiredCallback != null) {
						expiredCallback.Invoke(key, cacheValue);
					}
					break;
				case CacheItemRemovedReason.Underused:
					break;
			}
		}

		/// <summary>
		/// Insert value into the cache using default expiration & default callback
		/// </summary>
		/// <param name="o">Item to be cached</param>
		/// <param name="key">Name of item</param>
		public void Add(string key, T o) {
			if (defaultCallback != null) {
				cache.Insert(prefix + key, o, null, DateTime.Now.AddSeconds(defaultExpiration), Cache.NoSlidingExpiration, CacheItemPriority.Default, defaultCallback);
			} else {
				cache.Insert(prefix + key, o, null, DateTime.Now.AddSeconds(defaultExpiration), Cache.NoSlidingExpiration);
				
			}
		}
		
		/// <summary>
		/// Get all the methods for this cache
		/// </summary>
		/// <returns>IEnumerator of the type</returns>
		public IEnumerable<T> GetElements() {
			IDictionaryEnumerator cacheEnum = cache.GetEnumerator();
			while (cacheEnum.MoveNext()) {
				string key = cacheEnum.Key as string;
				if (!string.IsNullOrEmpty(key) && key.StartsWith(prefix)) {
					yield return (T)cacheEnum.Value;
				}
			}
		}
		
		/// <summary>
		/// Insert value into the cache using the supplied expiration time in seconds
		/// </summary>
		/// <param name="o">Item to be cached</param>
		/// <param name="key">Name of item</param>
		/// <param name="seconds">expiration time in "double" seconds</param>
		public void Add(string key, T o, double seconds) {
			cache.Insert(prefix + key, o, null, DateTime.Now.AddSeconds(seconds), Cache.NoSlidingExpiration);
		}

		/// <summary>
		/// Insert value into the cache using
		/// appropriate name/value pairs
		/// </summary>
		/// <param name="o">Item to be cached</param>
		/// <param name="key">Name of item</param>
		public void Add(string key, T o, CacheItemRemovedCallback callback) {
			cache.Insert(prefix + key, o, null, DateTime.Now.AddSeconds(defaultExpiration), Cache.NoSlidingExpiration, CacheItemPriority.Default, callback);
		}

		/// <summary>
		/// Remove item from cache
		/// </summary>
		/// <param name="key">Name of cached item</param>
		public void Remove(string key) {
			cache.Remove(prefix + key);
		}

		/// <summary>
		/// Check for item in cache
		/// </summary>
		/// <param name="key">Name of cached item</param>
		/// <returns></returns>
		public bool Exists(string key) {
			return cache[prefix + key] != null;
		}

		/// <summary>
		/// Retrieve cached item
		/// </summary>
		/// <param name="key">Name of cached item</param>
		/// <returns>Cached item as type</returns>
		public T Get(string key) {
			try {
				return (T) cache[prefix + key];
			} catch {
				return default(T);
			}
		}
	}
}
