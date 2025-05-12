# Zenith Configuration Caching System

This document describes the configuration caching system implemented in the ZenithAPI to improve performance and reduce redundant configuration accesses.

## Overview

The configuration caching system provides a centralized way for modules to cache frequently accessed configuration values. It automatically handles invalidation when configuration values change, ensuring that modules always have access to the most up-to-date values without having to implement their own caching logic.

## Components

The system consists of the following components:

1. **ConfigCacheManager**: A static class that manages a centralized cache of configuration values.
2. **ConfigCacheExtensions**: Extension methods that make it easy for modules to use the caching system.
3. **IZenithEvents.OnZenithConfigChanged**: An event that notifies when configuration values change.

## How to Use

### 1. Initialize the Cache in Your Module

When your module loads, initialize the configuration cache:

```csharp
// In your OnAllPluginsLoaded or similar initialization method
_moduleServices.InitConfigCache();
```

### 2. Use the Cache to Retrieve Configuration Values

Instead of directly calling `GetValue<T>()` on your config accessor, use the extension method:

```csharp
// Old way - uncached
var value = _configAccessor.GetValue<int>("Section", "Key");

// New way - cached with automatic invalidation
var value = _configAccessor.GetCachedValue<int>("Section", "Key");
```

### 3. Invalidate Cache When Needed

In most cases, you don't need to manually invalidate the cache, as it happens automatically when configuration values change. However, if you need to invalidate all cached values for your module:

```csharp
_moduleServices.InvalidateConfigCache();
```

## Performance Considerations

### When to Use Caching

Configuration caching is most beneficial for:

1. Values that are accessed frequently (e.g., in gameplay loops)
2. Values that are expensive to retrieve (e.g., complex objects, lists)
3. Values that don't change often during gameplay

### When Not to Use Caching

Consider not using caching for:

1. Values that change frequently (although our system handles this automatically)
2. Values that are accessed rarely
3. Simple values that are cheap to retrieve

## Implementation Notes

- The cache stores values indefinitely until invalidated by configuration changes
- The cache is automatically invalidated when configuration values change via events
- Each module has its own logical cache namespace to prevent conflicts
- The cache uses a thread-safe `ConcurrentDictionary` to support concurrent access

## Example Usage

Here's a complete example of using the centralized configuration cache in a module:

```csharp
public override void OnAllPluginsLoaded(bool hotReload)
{
    // Initialize the module
    _moduleServices = _moduleServicesCapability.Get();
    _configAccessor = _moduleServices.GetModuleConfigAccessor();

    // Initialize the centralized config cache
    _moduleServices.InitConfigCache();

    // Use cached config values
    int interval = _configAccessor.GetCachedValue<int>("Settings", "Interval");
    string message = _configAccessor.GetCachedValue<string>("Messages", "Welcome");
    List<string> commands = _configAccessor.GetCachedValue<List<string>>("Commands", "Allowed");
}
```

## Migrating from LocalConfigCache

If you're using a local configuration cache in your module, follow these steps to migrate:

1. Remove your local `ConfigCache` or `LocalConfigCache` implementation
2. Add a call to `_moduleServices.InitConfigCache()` in your initialization code
3. Replace calls to your local cache with `_configAccessor.GetCachedValue<T>()`
4. Remove any manual cache invalidation logic, as it's now handled automatically

## Troubleshooting

If you encounter issues with the cache:

1. Ensure you've initialized the cache with `_moduleServices.InitConfigCache()`
2. Check that you're using the correct configuration section and key names
3. Try invalidating the cache with `_moduleServices.InvalidateConfigCache()`
4. Make sure that the OnZenithConfigChanged event is being triggered when configs change