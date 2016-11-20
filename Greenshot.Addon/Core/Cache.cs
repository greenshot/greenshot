//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections.Generic;
using System.Timers;
using Dapplo.Log;

#endregion

namespace Greenshot.Addon.Core
{
	/// <summary>
	///     Cache class
	/// </summary>
	/// <typeparam name="TK">Type of key</typeparam>
	/// <typeparam name="TV">Type of value</typeparam>
	public class Cache<TK, TV>
	{
		private static readonly LogSource Log = new LogSource();

		private readonly Action<TK,TV> _expiredCallback;
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
		public Cache(Action<TK, TV> expiredCallback) : this()
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
		public Cache(int secondsToExpire, Action<TK, TV> expiredCallback) : this(expiredCallback)
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
				List<TV> elements = new List<TV>();

				foreach (TV element in _internalCache.Values)
				{
					elements.Add(element);
				}
				foreach (TV element in elements)
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
				TV result = default(TV);
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
				cachedItem.Expired += (cacheKey, cacheValue) =>
				{
					if (_internalCache.ContainsKey(cacheKey))
					{
						Log.Debug().WriteLine("Expiring object with Key: {0}", cacheKey);
						_expiredCallback?.Invoke(cacheKey, cacheValue);
						Remove(cacheKey);
					}
					else
					{
						Log.Debug().WriteLine("Expired old object with Key: {0}", cacheKey);
					}
				};

				if (_internalCache.ContainsKey(key))
				{
					_internalCache[key] = value;
					Log.Debug().WriteLine("Updated item with Key: {0}", key);
				}
				else
				{
					_internalCache.Add(key, cachedItem);
					Log.Debug().WriteLine("Added item with Key: {0}", key);
				}
			}
		}

		/// <summary>
		///     Contains
		/// </summary>
		/// <param name="key"></param>
		/// <returns>true if the cache contains the key</returns>
		public bool Contains(TK key)
		{
			return _internalCache.ContainsKey(key);
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
				Log.Debug().WriteLine("Removed item with Key: {0}", key);
			}
		}

		/// <summary>
		///     A cache item
		/// </summary>
		private class CachedItem
		{
			private readonly Timer _timerEvent;
			private readonly int secondsToExpire;

			public CachedItem(TK key, TV item, int secondsToExpire)
			{
				if (key == null)
				{
					throw new ArgumentNullException(nameof(key));
				}
				Key = key;
				Item = item;
				this.secondsToExpire = secondsToExpire;
				if (secondsToExpire > 0)
				{
					_timerEvent = new Timer(secondsToExpire*1000)
					{
						AutoReset = false
					};
					_timerEvent.Elapsed += timerEvent_Elapsed;
					_timerEvent.Start();
				}
			}

			public TV Item { get; }

			public TK Key { get; }

			public event Action<TK, TV> Expired;

			private void ExpireNow()
			{
				_timerEvent.Stop();
				if ((secondsToExpire > 0))
				{
					Expired?.Invoke(Key, Item);
				}
			}

			public static implicit operator TV(CachedItem a)
			{
				return a.Item;
			}

			private void timerEvent_Elapsed(object sender, ElapsedEventArgs e)
			{
				ExpireNow();
			}
		}
	}
}