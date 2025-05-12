using System.Reflection;

namespace ZenithAPI
{
    /// <summary>
    /// Extension methods to simplify config cache usage for modules
    /// </summary>
    public static class ConfigCacheExtensions
    {
        // Store event handlers for each module to allow unsubscribing
        private static readonly Dictionary<string, Action<string, string, string, object>> _configChangeHandlers = new();
        /// <summary>
        /// Initializes configuration caching for a module
        /// </summary>
        /// <param name="moduleServices">The module services</param>
        public static void InitConfigCache(this IModuleServices moduleServices)
        {
            // Get the calling module's name
            var moduleName = Assembly.GetCallingAssembly().GetName().Name;

            // Register the module with the config cache manager
            ConfigCacheManager.RegisterModule(moduleName!);

            // Create and store the event handler
            Action<string, string, string, object> handler = (changedModule, section, key, value) =>
            {
                // Invalidate the cache for this config
                ConfigCacheManager.InvalidateConfig(changedModule, section, key);
            };

            // Store the handler for later unsubscription
            _configChangeHandlers[moduleName!] = handler;

            // Subscribe to config changes to handle cache invalidation
            moduleServices.GetEventHandler().OnZenithConfigChanged += handler;
        }

        /// <summary>
        /// Cleans up the config cache when a module is unloaded
        /// </summary>
        /// <param name="moduleServices">The module services</param>
        public static void CleanupConfigCache(this IModuleServices moduleServices)
        {
            // Get the calling module's name
            var moduleName = Assembly.GetCallingAssembly().GetName().Name;

            // If we have a stored handler, unsubscribe it
            if (_configChangeHandlers.TryGetValue(moduleName!, out var handler))
            {
                try
                {
                    moduleServices.GetEventHandler().OnZenithConfigChanged -= handler;
                }
                catch (Exception)
                {
                    // Ignore any errors during unsubscription
                }

                _configChangeHandlers.Remove(moduleName!);
            }

            // Clean up the cache entries
            ConfigCacheManager.InvalidateAllForModule(moduleName!);
        }

        /// <summary>
        /// Gets a cached configuration value, or loads it if not cached
        /// </summary>
        /// <typeparam name="T">The type of the configuration value</typeparam>
        /// <param name="configAccessor">The module's configuration accessor</param>
        /// <param name="section">The configuration section</param>
        /// <param name="key">The configuration key</param>
        /// <returns>The configuration value</returns>
        public static T GetCachedValue<T>(
            this IModuleConfigAccessor configAccessor,
            string section,
            string key) where T : notnull
        {
            // Get the calling module's name
            var moduleName = Assembly.GetCallingAssembly().GetName().Name;

            // Use the config cache manager to get or add the value
            return ConfigCacheManager.GetOrAddValue<T>(
                moduleName!,
                section,
                key,
                () => configAccessor.GetValue<T>(section, key));
        }

        /// <summary>
        /// Invalidates all cached configuration values for a module
        /// </summary>
        /// <param name="moduleServices">The module services</param>
        public static void InvalidateConfigCache(this IModuleServices moduleServices)
        {
            // Get the calling module's name
            var moduleName = Assembly.GetCallingAssembly().GetName().Name;

            // Invalidate all cached values for this module
            ConfigCacheManager.InvalidateAllForModule(moduleName!);
        }
    }
}