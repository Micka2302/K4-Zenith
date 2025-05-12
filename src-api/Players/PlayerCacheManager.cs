using System.Collections.Concurrent;

namespace ZenithAPI
{
    /// <summary>
    /// Centralized player cache manager for Zenith modules
    /// </summary>
    public static class PlayerCacheManager
    {
        // Global player cache that modules can use
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<ulong, object>> _modulePlayerCaches = new();

        // Track module-specific expiration times
        private static readonly ConcurrentDictionary<string, TimeSpan> _moduleExpirationTimes = new();

        // Track last access times for players
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<ulong, DateTime>> _lastAccessTimes = new();

        // Default expiration time
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Registers a module with the player cache manager
        /// </summary>
        /// <param name="moduleName">The name of the module</param>
        /// <param name="expirationTime">Optional custom expiration time</param>
        public static void RegisterModule(string moduleName, TimeSpan? expirationTime = null)
        {
            _modulePlayerCaches.TryAdd(moduleName, new ConcurrentDictionary<ulong, object>());
            _lastAccessTimes.TryAdd(moduleName, new ConcurrentDictionary<ulong, DateTime>());
            _moduleExpirationTimes[moduleName] = expirationTime ?? DefaultExpiration;
        }

        /// <summary>
        /// Attempts to get a player's data from the cache
        /// </summary>
        /// <typeparam name="T">The type of the cached value</typeparam>
        /// <param name="moduleName">The module name</param>
        /// <param name="steamId">The player's Steam ID</param>
        /// <param name="value">The output value if found</param>
        /// <returns>True if the value exists in cache and is not expired</returns>
        public static bool TryGetPlayer<T>(string moduleName, ulong steamId, out T value) where T : class
        {
            value = default!;

            // Check if module is registered
            if (!_modulePlayerCaches.TryGetValue(moduleName, out var moduleCache) ||
                !_lastAccessTimes.TryGetValue(moduleName, out var accessTimes))
            {
                return false;
            }

            // Check if player is in cache and not expired
            if (moduleCache.TryGetValue(steamId, out var cachedValue) &&
                accessTimes.TryGetValue(steamId, out var lastAccess))
            {
                // Get expiration time for this module
                var expiration = _moduleExpirationTimes.GetValueOrDefault(moduleName, DefaultExpiration);

                // Check if the cache is still valid
                if (DateTime.UtcNow - lastAccess < expiration)
                {
                    // Return the cached value if it's of the correct type
                    if (cachedValue is T typedValue)
                    {
                        value = typedValue;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets or adds a player's data to the cache
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="moduleName">The module name</param>
        /// <param name="steamId">The player's Steam ID</param>
        /// <param name="valueFactory">Function to create the value if not in cache</param>
        /// <returns>The cached or created value</returns>
        public static T GetOrAddPlayer<T>(string moduleName, ulong steamId, Func<ulong, T> valueFactory) where T : class
        {
            // Try to get from cache first
            if (TryGetPlayer<T>(moduleName, steamId, out var cachedValue))
            {
                return cachedValue;
            }

            // Register module if not already registered
            if (!_modulePlayerCaches.TryGetValue(moduleName, out var moduleCache))
            {
                RegisterModule(moduleName);
                moduleCache = _modulePlayerCaches[moduleName];
            }

            if (!_lastAccessTimes.TryGetValue(moduleName, out var accessTimes))
            {
                _lastAccessTimes[moduleName] = new ConcurrentDictionary<ulong, DateTime>();
                accessTimes = _lastAccessTimes[moduleName];
            }

            // Create and cache the value
            var value = valueFactory(steamId);
            if (value != null)
            {
                moduleCache[steamId] = value;
                accessTimes[steamId] = DateTime.UtcNow;
                return value;
            }

            // Return null-safe default when valueFactory returns null
            return default!;
        }

        /// <summary>
        /// Set or update a player's data in the cache
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="moduleName">The module name</param>
        /// <param name="steamId">The player's Steam ID</param>
        /// <param name="value">The value to cache</param>
        public static void SetPlayer<T>(string moduleName, ulong steamId, T value) where T : class
        {
            // Register module if not already registered
            if (!_modulePlayerCaches.TryGetValue(moduleName, out var moduleCache))
            {
                RegisterModule(moduleName);
                moduleCache = _modulePlayerCaches[moduleName];
            }

            if (!_lastAccessTimes.TryGetValue(moduleName, out var accessTimes))
            {
                _lastAccessTimes[moduleName] = new ConcurrentDictionary<ulong, DateTime>();
                accessTimes = _lastAccessTimes[moduleName];
            }

            // Update cache
            if (value != null)
            {
                moduleCache[steamId] = value;
                accessTimes[steamId] = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Remove a player from the cache for a specific module
        /// </summary>
        /// <param name="moduleName">The module name</param>
        /// <param name="steamId">The player's Steam ID</param>
        public static void RemovePlayer(string moduleName, ulong steamId)
        {
            if (_modulePlayerCaches.TryGetValue(moduleName, out var moduleCache))
            {
                moduleCache.TryRemove(steamId, out _);
            }

            if (_lastAccessTimes.TryGetValue(moduleName, out var accessTimes))
            {
                accessTimes.TryRemove(steamId, out _);
            }
        }

        /// <summary>
        /// Remove a player from all module caches
        /// </summary>
        /// <param name="steamId">The player's Steam ID</param>
        public static void RemovePlayerFromAllModules(ulong steamId)
        {
            foreach (var moduleName in _modulePlayerCaches.Keys)
            {
                RemovePlayer(moduleName, steamId);
            }
        }

        /// <summary>
        /// Clean up expired entries for a specific module
        /// </summary>
        /// <param name="moduleName">The module name</param>
        public static void CleanupExpiredPlayers(string moduleName)
        {
            if (!_modulePlayerCaches.TryGetValue(moduleName, out var moduleCache) ||
                !_lastAccessTimes.TryGetValue(moduleName, out var accessTimes))
            {
                return;
            }

            var now = DateTime.UtcNow;
            var expiration = _moduleExpirationTimes.GetValueOrDefault(moduleName, DefaultExpiration);

            // Find all expired entries
            var expiredKeys = accessTimes
                .Where(kvp => now - kvp.Value > expiration)
                .Select(kvp => kvp.Key)
                .ToList();

            // Remove expired entries
            foreach (var key in expiredKeys)
            {
                moduleCache.TryRemove(key, out _);
                accessTimes.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Clean up all module player caches when module is unloaded
        /// </summary>
        /// <param name="moduleName">The module name</param>
        public static void CleanupModule(string moduleName)
        {
            _modulePlayerCaches.TryRemove(moduleName, out _);
            _lastAccessTimes.TryRemove(moduleName, out _);
            _moduleExpirationTimes.TryRemove(moduleName, out _);
        }

        /// <summary>
        /// Get all cached players for a module
        /// </summary>
        /// <param name="moduleName">The module name</param>
        /// <returns>Collection of steam IDs for cached players</returns>
        public static IEnumerable<ulong> GetAllCachedPlayers(string moduleName)
        {
            if (_modulePlayerCaches.TryGetValue(moduleName, out var moduleCache))
            {
                return moduleCache.Keys;
            }

            return Enumerable.Empty<ulong>();
        }
    }
}