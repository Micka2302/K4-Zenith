
namespace ZenithAPI
{
    /// <summary>
    /// Type-specific player cache helper
    /// </summary>
    /// <typeparam name="T">The type of player data</typeparam>
    public sealed class TypedPlayerCache<T> where T : class
    {
        private readonly string _moduleName;

        internal TypedPlayerCache(string moduleName)
        {
            _moduleName = moduleName;
        }

        /// <summary>
        /// Try to get a player from the cache
        /// </summary>
        public bool TryGetPlayer(ulong steamId, out T value)
        {
            return PlayerCacheManager.TryGetPlayer(_moduleName, steamId, out value);
        }

        /// <summary>
        /// Get or add a player to the cache
        /// </summary>
        public T GetOrAddPlayer(ulong steamId, Func<ulong, T> valueFactory)
        {
            return PlayerCacheManager.GetOrAddPlayer(_moduleName, steamId, valueFactory);
        }

        /// <summary>
        /// Set a player in the cache
        /// </summary>
        public void SetPlayer(ulong steamId, T value)
        {
            PlayerCacheManager.SetPlayer(_moduleName, steamId, value);
        }

        /// <summary>
        /// Remove a player from the cache
        /// </summary>
        public void RemovePlayer(ulong steamId)
        {
            PlayerCacheManager.RemovePlayer(_moduleName, steamId);
        }

        /// <summary>
        /// Get all cached players
        /// </summary>
        public IEnumerable<ulong> GetAllCachedPlayers()
        {
            return PlayerCacheManager.GetAllCachedPlayers(_moduleName);
        }

        /// <summary>
        /// Clean up expired players
        /// </summary>
        public void CleanupExpiredPlayers()
        {
            PlayerCacheManager.CleanupExpiredPlayers(_moduleName);
        }
    }
}