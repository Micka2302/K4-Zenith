using System.Collections.Concurrent;

namespace ZenithAPI
{
    /// <summary>
    /// Centralized configuration cache manager for the Zenith API
    /// </summary>
    public static class ConfigCacheManager
    {
        // Global configuration cache that all modules can access
        private static readonly ConcurrentDictionary<string, object> _globalCache = new();

        // Tracks module-specific subscriptions to config changes for invalidation
        private static readonly ConcurrentDictionary<string, HashSet<string>> _moduleSubscriptions = new();

        /// <summary>
        /// Registers a module with the config cache manager
        /// </summary>
        /// <param name="moduleName">Name of the module</param>
        public static void RegisterModule(string moduleName)
        {
            _moduleSubscriptions.TryAdd(moduleName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets or adds a configuration value to the cache
        /// </summary>
        /// <typeparam name="T">The type of configuration value</typeparam>
        /// <param name="moduleName">The module requesting the value</param>
        /// <param name="section">The configuration section</param>
        /// <param name="key">The configuration key</param>
        /// <param name="valueFactory">A function to create the value if not in cache</param>
        /// <returns>The cached or newly created value</returns>
        public static T GetOrAddValue<T>(
            string moduleName,
            string section,
            string key,
            Func<T> valueFactory)
        {
            // Generate a unique cache key
            string cacheKey = GetCacheKey(moduleName, section, key);

            // Track this subscription for invalidation
            if (_moduleSubscriptions.TryGetValue(moduleName, out var subscriptions))
            {
                subscriptions.Add($"{section}:{key}");
            }

            // Try to get from cache or add new value
            if (_globalCache.TryGetValue(cacheKey, out var cachedValue))
            {
                return (T)cachedValue;
            }

            // Create new value and cache it
            T value = valueFactory();
            // Ensure we don't store null values to avoid warnings
            if (value != null)
            {
                _globalCache[cacheKey] = value;
            }

            return value;
        }

        /// <summary>
        /// Invalidates cached configurations when values change
        /// </summary>
        /// <param name="moduleName">The module whose config changed</param>
        /// <param name="section">The config section that changed</param>
        /// <param name="key">The config key that changed</param>
        public static void InvalidateConfig(string moduleName, string section, string key)
        {
            // Invalidate the specific config entry
            string specificKey = GetCacheKey(moduleName, section, key);
            _globalCache.TryRemove(specificKey, out _);

            // Also invalidate any module that has subscribed to this config
            foreach (var moduleEntry in _moduleSubscriptions)
            {
                if (moduleEntry.Value.Contains($"{section}:{key}"))
                {
                    string subscriptionKey = GetCacheKey(moduleEntry.Key, section, key);
                    _globalCache.TryRemove(subscriptionKey, out _);
                }
            }
        }

        /// <summary>
        /// Invalidates all cached values for a module
        /// </summary>
        /// <param name="moduleName">The module name</param>
        public static void InvalidateAllForModule(string moduleName)
        {
            // Get all keys for this module
            var keysToRemove = _globalCache.Keys
                .Where(k => k.StartsWith($"{moduleName}:"))
                .ToList();

            // Remove all keys
            foreach (var key in keysToRemove)
            {
                _globalCache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Clears the entire cache
        /// </summary>
        public static void ClearAll()
        {
            _globalCache.Clear();
        }

        /// <summary>
        /// Creates a unique cache key for a config value
        /// </summary>
        private static string GetCacheKey(string moduleName, string section, string key)
        {
            return $"{moduleName}:{section}:{key}";
        }
    }
}