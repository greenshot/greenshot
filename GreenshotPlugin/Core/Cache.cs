#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Timers;
using log4net;

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     Cache class
	/// </summary>
	/// <typeparam name="TK">Type of key</typeparam>
	/// <typeparam name="TV">Type of value</typeparam>
	public class Cache<TK, TV>
	{
		public delegate void CacheObjectExpired(TK key, TV cacheValue);

		private static readonly ILog Log = LogManager.GetLogger(typeof(Cache<TK, TV>));
		private readonly CacheObjectExpired _expiredCallback;
		private readonly IDictionary<TK, TV> _internalCache = new Dictionary<TK, TV>();
		private readonly object _lockObject = new object();
		private readonly int _secondsToExpire = 10;

		/// <summary>
		///     Initialize the cache
		/// </summary>
		public Cache()
		{
		}

		/// <summary>
		///     Initialize the cache
		/// </summary>
		/// <param name="expiredCallback"></param>
		public Cache(CacheObjectExpired expiredCallback) : this()
		{
			_expiredCallback = expiredCallback;
		}

		/// <summary>
		///     Initialize the cache with a expire setting
		/// </summary>
		/// <param name="secondsToExpire"></param>
		public Cache(int secondsToExpire) : this()
		{
			_secondsToExpire = secondsToExpire;
		}

		/// <summary>
		///     Initialize the cache with a expire setting
		/// </summary>
		/// <param name="secondsToExpire"></param>
		/// <param name="expiredCallback"></param>
		public Cache(int secondsToExpire, CacheObjectExpired expiredCallback) : this(expiredCallback)
		{
			_secondsToExpire = secondsToExpire;
		}

		/// <summary>
		///     Enumerable for the values in the cache
		/// </summary>
		public IEnumerable<TV> Elements
		{
			get
			{
				var elements = new List<TV>();

				lock (_lockObject)
				{
					foreach (var element in _internalCache.Values)
					{
						elements.Add(element);
					}
				}
				foreach (var element in elements)
				{
					yield return element;
				}
			}
		}

		/// <summary>
		///     Get the value by key from the cache
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public TV this[TK key]
		{
			get
			{
				var result = default(TV);
				lock (_lockObject)
				{
					if (_internalCache.ContainsKey(key))
					{
						result = _internalCache[key];
					}
				}
				return result;
			}
		}

		/// <summary>
		///     Contains
		/// </summary>
		/// <param name="key"></param>
		/// <returns>true if the cache contains the key</returns>
		public bool Contains(TK key)
		{
			lock (_lockObject)
			{
				return _internalCache.ContainsKey(key);
			}
		}

		/// <summary>
		///     Add a value to the cache
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Add(TK key, TV value)
		{
			Add(key, value, null);
		}

		/// <summary>
		///     Add a value to the cache
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="secondsToExpire">optional value for the seconds to expire</param>
		public void Add(TK key, TV value, int? secondsToExpire)
		{
			lock (_lockObject)
			{
				var cachedItem = new CachedItem(key, value, secondsToExpire ?? _secondsToExpire);
				cachedItem.Expired += delegate(TK cacheKey, TV cacheValue)
				{
					if (_internalCache.ContainsKey(cacheKey))
					{
						Log.DebugFormat("Expiring object with Key: {0}", cacheKey);
						_expiredCallback?.Invoke(cacheKey, cacheValue);
						Remove(cacheKey);
					}
					else
					{
						Log.DebugFormat("Expired old object with Key: {0}", cacheKey);
					}
				};

				if (_internalCache.ContainsKey(key))
				{
					_internalCache[key] = value;
					Log.DebugFormat("Updated item with Key: {0}", key);
				}
				else
				{
					_internalCache.Add(key, cachedItem);
					Log.DebugFormat("Added item with Key: {0}", key);
				}
			}
		}

		/// <summary>
		///     Remove item from cache
		/// </summary>
		/// <param name="key"></param>
		public void Remove(TK key)
		{
			lock (_lockObject)
			{
				if (!_internalCache.ContainsKey(key))
				{
					throw new ApplicationException($"An object with key ‘{key}’ does not exists in cache");
				}
				_internalCache.Remove(key);
				Log.DebugFormat("Removed item with Key: {0}", key);
			}
		}

		/// <summary>
		///     A cache item
		/// </summary>
		private class CachedItem
		{
			private readonly int _secondsToExpire;
			private readonly Timer _timerEvent;

			public CachedItem(TK key, TV item, int secondsToExpire)
			{
				if (key == null)
				{
					throw new ArgumentNullException(nameof(key));
				}
				Key = key;
				Item = item;
				_secondsToExpire = secondsToExpire;
				if (secondsToExpire <= 0)
				{
					return;
				}
				_timerEvent = new Timer(secondsToExpire * 1000) {AutoReset = false};
				_timerEvent.Elapsed += timerEvent_Elapsed;
				_timerEvent.Start();
			}

			public TK Key { get; }
			public TV Item { get; }
			public event CacheObjectExpired Expired;

			private void ExpireNow()
			{
				_timerEvent.Stop();
				if (_secondsToExpire > 0)
				{
					Expired?.Invoke(Key, Item);
				}
			}

			private void timerEvent_Elapsed(object sender, ElapsedEventArgs e)
			{
				ExpireNow();
			}

			public static implicit operator TV(CachedItem a)
			{
				return a.Item;
			}
		}
	}
}