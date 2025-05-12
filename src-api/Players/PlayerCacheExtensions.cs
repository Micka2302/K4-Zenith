using System.Reflection;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;

namespace ZenithAPI
{
    /// <summary>
    /// Extension methods for the player cache system
    /// </summary>
    public static class PlayerCacheExtensions
    {
        /// <summary>
        /// Initializes the player cache for a module
        /// </summary>
        /// <param name="moduleServices">The module services</param>
        /// <param name="expirationTime">Optional custom expiration time</param>
        public static void InitPlayerCache(this IModuleServices moduleServices, TimeSpan? expirationTime = null)
        {
            var moduleName = Assembly.GetCallingAssembly().GetName().Name;
            PlayerCacheManager.RegisterModule(moduleName!, expirationTime);
            
            // Set up automatic cleanup on player disconnect
            var events = moduleServices.GetEventHandler();
            events.OnZenithPlayerUnloaded += (player) => 
            {
                if (player != null)
                {
                    PlayerCacheManager.RemovePlayer(moduleName!, player.SteamID);
                }
            };
        }
        
        /// <summary>
        /// Gets a player from the cache or adds it if not already cached
        /// </summary>
        /// <typeparam name="T">The type of player data to cache</typeparam>
        /// <param name="capability">The player capability</param>
        /// <param name="player">The player controller</param>
        /// <param name="valueFactory">Function to create the player data if not in cache</param>
        /// <returns>The cached player data</returns>
        public static T GetOrAddCachedPlayer<T>(
            this PlayerCapability<IPlayerServices> capability,
            CCSPlayerController player,
            Func<CCSPlayerController, T> valueFactory) where T : class
        {
            if (player == null) 
                return null!;
                
            var moduleName = Assembly.GetCallingAssembly().GetName().Name;
            
            return PlayerCacheManager.GetOrAddPlayer(
                moduleName!,
                player.SteamID,
                steamId => valueFactory(player));
        }
        
        /// <summary>
        /// Try to get a player from the cache
        /// </summary>
        /// <typeparam name="T">The type of player data</typeparam>
        /// <param name="capability">The player capability</param>
        /// <param name="player">The player controller</param>
        /// <param name="value">The output value if found</param>
        /// <returns>True if the player was found in cache</returns>
        public static bool TryGetCachedPlayer<T>(
            this PlayerCapability<IPlayerServices> capability,
            CCSPlayerController player,
            out T value) where T : class
        {
            value = default!;
            
            if (player == null)
                return false;
                
            var moduleName = Assembly.GetCallingAssembly().GetName().Name;
            
            return PlayerCacheManager.TryGetPlayer(moduleName!, player.SteamID, out value);
        }
        
        /// <summary>
        /// Set a player in the cache
        /// </summary>
        /// <typeparam name="T">The type of player data</typeparam>
        /// <param name="capability">The player capability</param>
        /// <param name="player">The player controller</param>
        /// <param name="value">The value to cache</param>
        public static void SetCachedPlayer<T>(
            this PlayerCapability<IPlayerServices> capability,
            CCSPlayerController player,
            T value) where T : class
        {
            if (player == null)
                return;
                
            var moduleName = Assembly.GetCallingAssembly().GetName().Name;
            
            PlayerCacheManager.SetPlayer(moduleName!, player.SteamID, value);
        }
        
        /// <summary>
        /// Remove a player from the cache
        /// </summary>
        /// <param name="capability">The player capability</param>
        /// <param name="player">The player controller</param>
        public static void RemoveCachedPlayer(
            this PlayerCapability<IPlayerServices> capability,
            CCSPlayerController player)
        {
            if (player == null)
                return;
                
            var moduleName = Assembly.GetCallingAssembly().GetName().Name;
            
            PlayerCacheManager.RemovePlayer(moduleName!, player.SteamID);
        }
        
        /// <summary>
        /// Cleanup all cached players for the module
        /// </summary>
        /// <param name="moduleServices">The module services</param>
        public static void CleanupPlayerCache(this IModuleServices moduleServices)
        {
            var moduleName = Assembly.GetCallingAssembly().GetName().Name;
            
            PlayerCacheManager.CleanupModule(moduleName!);
        }
        
        /// <summary>
        /// Cleanup expired players from the cache
        /// </summary>
        /// <param name="moduleServices">The module services</param>
        public static void CleanupExpiredPlayers(this IModuleServices moduleServices)
        {
            var moduleName = Assembly.GetCallingAssembly().GetName().Name;
            
            PlayerCacheManager.CleanupExpiredPlayers(moduleName!);
        }
        
        /// <summary>
        /// Get a specific type of player cache manager for a module
        /// </summary>
        /// <typeparam name="T">The type of player data</typeparam>
        /// <param name="moduleServices">The module services</param>
        /// <returns>A type-specific player cache manager</returns>
        public static TypedPlayerCache<T> GetTypedPlayerCache<T>(this IModuleServices moduleServices) where T : class
        {
            var moduleName = Assembly.GetCallingAssembly().GetName().Name;
            
            return new TypedPlayerCache<T>(moduleName!);
        }
    }
    
    // TypedPlayerCache has been moved to TypedPlayerCache.cs
}