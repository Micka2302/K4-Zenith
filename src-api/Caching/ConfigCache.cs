using System.Collections.Concurrent;

namespace ZenithAPI
{
	/// <summary>
	/// A thread-safe, high-performance config cache for modules using the ZenithAPI.
	/// </summary>
	/// <typeparam name="TKey">Type used for cache key, typically string</typeparam>
	/// <typeparam name="TValue">Type used for cache value</typeparam>
	public sealed class ConfigCache<TKey, TValue> where TKey : notnull
	{
		private readonly ConcurrentDictionary<TKey, CacheEntry<TValue>> _cache = new();
		private readonly TimeSpan _defaultExpiration;
		private readonly string _moduleName;
		private readonly string _debugLabel;
		private readonly IModuleServices _moduleServices;
		private readonly int _maxEntries;
		private readonly bool _autoCleanup;

		/// <summary>
		/// Creates a new instance of the ConfigCache class.
		/// </summary>
		/// <param name="moduleName">The name of the module that owns this cache</param>
		/// <param name="moduleServices">The module services to use for config events</param>
		/// <param name="debugLabel">A label for debugging purposes</param>
		/// <param name="defaultExpiration">The default expiration time for entries</param>
		/// <param name="maxEntries">The maximum number of entries allowed in the cache</param>
		/// <param name="autoCleanup">Whether to automatically clean up expired entries</param>
		public ConfigCache(string moduleName, IModuleServices moduleServices, string debugLabel = "cache",
			TimeSpan? defaultExpiration = null, int maxEntries = 1000, bool autoCleanup = true)
		{
			_moduleName = moduleName;
			_debugLabel = debugLabel;
			_moduleServices = moduleServices;
			_defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(5);
			_maxEntries = maxEntries;
			_autoCleanup = autoCleanup;

			// Set up cleanup timer if auto cleanup is enabled
			if (_autoCleanup)
			{
				var cleanupInterval = TimeSpan.FromMinutes(Math.Max(1, _defaultExpiration.TotalMinutes / 2));
				var timer = new Timer(CleanupExpiredEntries, null, cleanupInterval, cleanupInterval);
			}

			var events = moduleServices.GetEventHandler();
			events.OnZenithConfigChanged += OnZenithConfigChanged;
		}

		/// <summary>
		/// Gets a value from the cache or adds it using the factory if it doesn't exist.
		/// </summary>
		/// <param name="key">The key to use</param>
		/// <param name="valueFactory">A factory to create the value if it doesn't exist</param>
		/// <param name="expiration">Optional specific expiration time for this entry</param>
		/// <returns>The cached value</returns>
		public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory, TimeSpan? expiration = null)
		{
			// Check if the key exists and is not expired
			if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
			{
				return entry.Value;
			}

			// Add or update the entry
			var newEntry = new CacheEntry<TValue>(valueFactory(key), expiration ?? _defaultExpiration);
			_cache[key] = newEntry;

			// Check if we need to clean up the cache
			if (_autoCleanup && _cache.Count > _maxEntries)
			{
				CleanupExpiredEntries(null);
			}

			return newEntry.Value;
		}

		/// <summary>
		/// Tries to get a value from the cache.
		/// </summary>
		/// <param name="key">The key to look up</param>
		/// <param name="value">The output value if found</param>
		/// <returns>True if the value was found and not expired, false otherwise</returns>
		public bool TryGetValue(TKey key, out TValue value)
		{
			if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
			{
				value = entry.Value;
				return true;
			}

			value = default!;
			return false;
		}

		/// <summary>
		/// Sets a value in the cache.
		/// </summary>
		/// <param name="key">The key to set</param>
		/// <param name="value">The value to set</param>
		/// <param name="expiration">Optional specific expiration time for this entry</param>
		public void Set(TKey key, TValue value, TimeSpan? expiration = null)
		{
			_cache[key] = new CacheEntry<TValue>(value, expiration ?? _defaultExpiration);
		}

		/// <summary>
		/// Invalidates a specific cache entry.
		/// </summary>
		/// <param name="key">The key to invalidate</param>
		public void Invalidate(TKey key)
		{
			_cache.TryRemove(key, out _);
		}

		/// <summary>
		/// Invalidates all cache entries.
		/// </summary>
		public void InvalidateAll()
		{
			_cache.Clear();
		}

		/// <summary>
		/// Invalidates all entries that match a predicate.
		/// </summary>
		/// <param name="predicate">A function that returns true for entries to invalidate</param>
		public void InvalidateWhere(Func<TKey, bool> predicate)
		{
			foreach (var key in _cache.Keys.Where(predicate))
			{
				_cache.TryRemove(key, out _);
			}
		}

		/// <summary>
		/// Handle Zenith config changes.
		/// </summary>
		private void OnZenithConfigChanged(string moduleName, string groupName, string configName, object newValue)
		{
			// Check if the config change is relevant to this module
			if (moduleName == _moduleName)
			{
				// For a TKey of string, we can try to invalidate entries that match the config name
				if (typeof(TKey) == typeof(string))
				{
					InvalidateWhere(k => k.ToString()!.Contains(configName) || k.ToString()!.Contains(groupName));
				}

				// For a tuple or other compound key, we need a more specific approach
				if (typeof(TKey) == typeof((string, string)) || typeof(TKey) == typeof(ValueTuple<string, string>))
				{
					InvalidateWhere(k =>
					{
						var (Section, Key) = ((string Section, string Key))Convert.ChangeType(k, typeof((string, string)))!;
						return Section == groupName || Key == configName;
					});
				}
			}
		}

		/// <summary>
		/// Cleans up expired entries.
		/// </summary>
		private void CleanupExpiredEntries(object? state)
		{
			var expiredKeys = _cache.Where(kvp => kvp.Value.IsExpired)
				.Select(kvp => kvp.Key)
				.ToList();

			foreach (var key in expiredKeys)
			{
				_cache.TryRemove(key, out _);
			}

			// If the cache is still too large, remove oldest entries
			if (_cache.Count > _maxEntries)
			{
				var keysToRemove = _cache.OrderBy(kvp => kvp.Value.CreatedAt)
					.Take(_cache.Count - _maxEntries / 2)
					.Select(kvp => kvp.Key)
					.ToList();

				foreach (var key in keysToRemove)
				{
					_cache.TryRemove(key, out _);
				}
			}
		}
	}
}