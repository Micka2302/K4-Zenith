using System.Collections.Concurrent;
using CounterStrikeSharp.API.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;
using System.Reflection;
using ZenithAPI;
using System.Globalization;

namespace Zenith
{
	public sealed partial class Plugin : BasePlugin
	{
		private ConfigManager _configManager = null!;

		public void Initialize_Config()
		{
			try
			{
				string configDirectory = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "configs", "zenith");
				_configManager = new ConfigManager(configDirectory, Logger);
				RegisterCoreConfigs();
			}
			catch (Exception ex)
			{
				Logger.LogError($"Failed to initialize config: {ex.Message}");
				throw;
			}
		}

		public ModuleConfigAccessor GetModuleConfigAccessor()
		{
			string callerPlugin = CallerIdentifier.GetCallingPluginName();
			Logger.LogInformation($"Module {callerPlugin} requested config accessor.");
			return ConfigManager.GetModuleAccessor(callerPlugin);
		}

		public static void RegisterModuleConfig<T>(string groupName, string configName, string description, T defaultValue, ConfigFlag flags = ConfigFlag.None) where T : notnull
		{
			string callerPlugin = CallerIdentifier.GetCallingPluginName();
			ConfigManager.RegisterConfig(callerPlugin, groupName, configName, description, defaultValue, flags);
		}

		public static bool HasModuleConfigValue(string groupName, string configName)
		{
			string callerPlugin = CallerIdentifier.GetCallingPluginName();
			return ConfigManager.HasConfigValue(callerPlugin, groupName, configName);
		}

		public static T GetModuleConfigValue<T>(string groupName, string configName) where T : notnull
		{
			string callerPlugin = CallerIdentifier.GetCallingPluginName();
			return ConfigManager.GetConfigValue<T>(callerPlugin, groupName, configName);
		}

		public static void SetModuleConfigValue<T>(string groupName, string configName, T value) where T : notnull
		{
			string callerPlugin = CallerIdentifier.GetCallingPluginName();
			ConfigManager.SetConfigValue(callerPlugin, groupName, configName, value);
		}
	}

	public class ModuleConfigAccessor : IModuleConfigAccessor
	{
		private readonly string _moduleName;

		internal ModuleConfigAccessor(string moduleName)
		{
			_moduleName = moduleName;
		}

		public T GetValue<T>(string groupName, string configName) where T : notnull
		{
			return ConfigManager.GetConfigValue<T>(_moduleName, groupName, configName);
		}

		public void SetValue<T>(string groupName, string configName, T value) where T : notnull
		{
			ConfigManager.SetConfigValue(_moduleName, groupName, configName, value);
		}

		public bool HasValue(string groupName, string configName)
		{
			return ConfigManager.HasConfigValue(_moduleName, groupName, configName);
		}
	}

	public class ConfigItem
	{
		public required string Name { get; set; }
		public required string Description { get; set; }
		public required object DefaultValue { get; set; }
		public required object CurrentValue { get; set; }
		[YamlIgnore]
		public ConfigFlag Flags { get; set; }
	}

	public class ConfigGroup
	{
		public required string Name { get; set; }
		public ConcurrentDictionary<string, ConfigItem> Items { get; set; } = new ConcurrentDictionary<string, ConfigItem>();
	}

	public class ModuleConfig
	{
		public required string ModuleName { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.Now;
		public DateTime LastUpdated { get; set; } = DateTime.Now;
		public ConcurrentDictionary<string, ConfigGroup> Groups { get; set; } = new ConcurrentDictionary<string, ConfigGroup>();
	}

	public class ConfigManager
	{
		private static string CoreModuleName => Assembly.GetEntryAssembly()?.GetName().Name ?? "K4-Zenith";
		private static string _baseConfigDirectory = string.Empty;
		private static readonly ConcurrentDictionary<string, ModuleConfig> _moduleConfigs = new ConcurrentDictionary<string, ModuleConfig>();

		private static readonly ConcurrentDictionary<string, ConfigItem> _configLookupCache = new ConcurrentDictionary<string, ConfigItem>();

		private static readonly ConcurrentDictionary<(Type, Type), Func<object, object>> _typeConverterCache =
			new ConcurrentDictionary<(Type, Type), Func<object, object>>();

		private static readonly ConcurrentDictionary<string, Timer> _pendingSaveTimers = new ConcurrentDictionary<string, Timer>();
		private static readonly TimeSpan _saveDelay = TimeSpan.FromMilliseconds(500); // Batch saves with 500ms delay

		private static ILogger Logger = null!;
		public static bool GlobalChangeTracking { get; set; } = false;
		public static bool GlobalAutoReloadEnabled { get; set; } = false;
		private static FileSystemWatcher? _watcher;

		private static readonly Lazy<IDeserializer> _deserializer = new Lazy<IDeserializer>(() =>
			new DeserializerBuilder()
				.WithNamingConvention(CamelCaseNamingConvention.Instance)
				.IgnoreUnmatchedProperties()
				.Build());

		private static readonly Lazy<ISerializer> _serializer = new Lazy<ISerializer>(() =>
			new SerializerBuilder()
				.WithNamingConvention(CamelCaseNamingConvention.Instance)
				.DisableAliases()
				.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
				.Build());

		public ConfigManager(string baseConfigDirectory, ILogger logger)
		{
			_baseConfigDirectory = baseConfigDirectory;
			Logger = logger;
			Directory.CreateDirectory(_baseConfigDirectory);
			Directory.CreateDirectory(Path.Combine(_baseConfigDirectory, "modules"));

			SetupFileWatcher();
			InitializeTypeConverters();
		}

		private static void InitializeTypeConverters()
		{
			// String to common types
			_typeConverterCache[(typeof(string), typeof(int))] = value => Convert.ToInt32((string)value, CultureInfo.InvariantCulture);
			_typeConverterCache[(typeof(string), typeof(float))] = value => Convert.ToSingle((string)value, CultureInfo.InvariantCulture);
			_typeConverterCache[(typeof(string), typeof(double))] = value => Convert.ToDouble((string)value, CultureInfo.InvariantCulture);
			_typeConverterCache[(typeof(string), typeof(decimal))] = value => Convert.ToDecimal((string)value, CultureInfo.InvariantCulture);
			_typeConverterCache[(typeof(string), typeof(bool))] = value => Convert.ToBoolean((string)value, CultureInfo.InvariantCulture);

			// Object to common types
			_typeConverterCache[(typeof(object), typeof(int))] = value => Convert.ToInt32(value, CultureInfo.InvariantCulture);
			_typeConverterCache[(typeof(object), typeof(float))] = value => Convert.ToSingle(value, CultureInfo.InvariantCulture);
			_typeConverterCache[(typeof(object), typeof(double))] = value => Convert.ToDouble(value, CultureInfo.InvariantCulture);
			_typeConverterCache[(typeof(object), typeof(decimal))] = value => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
			_typeConverterCache[(typeof(object), typeof(bool))] = value => Convert.ToBoolean(value, CultureInfo.InvariantCulture);
			_typeConverterCache[(typeof(object), typeof(string))] = value => value.ToString() ?? string.Empty;
		}

		public static ModuleConfigAccessor GetModuleAccessor(string moduleName)
		{
			return new ModuleConfigAccessor(moduleName);
		}

		private static void SetupFileWatcher()
		{
			_watcher = new FileSystemWatcher(_baseConfigDirectory)
			{
				NotifyFilter = NotifyFilters.LastWrite,
				Filter = "*.yaml",
				IncludeSubdirectories = true,
				EnableRaisingEvents = GlobalAutoReloadEnabled
			};

			_watcher.Changed += OnConfigFileChanged;
			_watcher.Created += OnConfigFileChanged;
		}

		public static void Dispose()
		{
			_watcher?.Dispose();

			foreach (var timer in _pendingSaveTimers.Values)
			{
				timer.Dispose();
			}
			_pendingSaveTimers.Clear();
		}

		public static void SetGlobalAutoReload(bool enabled)
		{
			GlobalAutoReloadEnabled = enabled;
			if (_watcher != null)
			{
				_watcher.EnableRaisingEvents = enabled;
			}
		}

		private static readonly ConcurrentDictionary<string, Timer> _debounceTimers = new ConcurrentDictionary<string, Timer>();

		private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
		{
			if (!GlobalAutoReloadEnabled)
				return;

			if (_debounceTimers.TryRemove(e.FullPath, out var existingTimer))
			{
				existingTimer.Dispose();
			}

			var newTimer = new Timer(DebounceCallback, e.FullPath, 1000, Timeout.Infinite);
			_debounceTimers[e.FullPath] = newTimer;
		}

		private static void DebounceCallback(object? state)
		{
			var fullPath = (string)state!;

			_debounceTimers.TryRemove(fullPath, out _);

			var relativePath = Path.GetRelativePath(_baseConfigDirectory, fullPath);
			var pathParts = relativePath.Split(Path.DirectorySeparatorChar);
			if (pathParts.Length >= 2 && pathParts[0] == "modules")
			{
				var moduleName = Path.GetFileNameWithoutExtension(pathParts[1]);
				ReloadModuleConfig(moduleName);
			}
			else if (pathParts.Length == 1 && pathParts[0] == "core.yaml")
			{
				ReloadModuleConfig(CoreModuleName);
			}
		}

		private static void ReloadModuleConfig(string moduleName, bool force = false)
		{
			if (!GlobalAutoReloadEnabled && !force)
				return;

			var newConfig = LoadModuleConfig(moduleName);

			if (_moduleConfigs.TryGetValue(moduleName, out var existingConfig))
			{
				bool configChanged = false;

				foreach (var group in newConfig.Groups)
				{
					var existingGroup = existingConfig.Groups.GetOrAdd(group.Key, new ConfigGroup { Name = group.Value.Name });

					foreach (var item in group.Value.Items)
					{
						if (existingGroup.Items.TryGetValue(item.Key, out var existingItem))
						{
							if (item.Value.CurrentValue != null && !item.Value.CurrentValue.Equals(existingItem.CurrentValue))
							{
								existingItem.CurrentValue = item.Value.CurrentValue;
								configChanged = true;

								InvalidateConfigCache(moduleName, group.Key, item.Key);
							}
						}
						else
						{
							existingGroup.Items[item.Key] = item.Value;
							configChanged = true;

							InvalidateConfigCache(moduleName, group.Key, item.Key);
						}
					}
				}

				if (configChanged)
				{
					Logger.LogDebug($"Config values changed for module {moduleName}");
				}
			}
			else
			{
				_moduleConfigs[moduleName] = newConfig;

				// Invalidate all caches for this module
				InvalidateAllConfigCache(moduleName);
			}

			if (!force)
				Logger.LogInformation($"Config reloaded for module {moduleName}");
		}

		private static void InvalidateConfigCache(string moduleName, string groupName, string configName)
		{
			string cacheKey = $"{moduleName}:{groupName}:{configName}";
			_configLookupCache.TryRemove(cacheKey, out _);
		}

		private static void InvalidateAllConfigCache(string moduleName)
		{
			var keysToRemove = _configLookupCache.Keys
				.Where(k => k.StartsWith($"{moduleName}:"))
				.ToList();

			foreach (var key in keysToRemove)
			{
				_configLookupCache.TryRemove(key, out _);
			}
		}

		public static void ReloadAllConfigs()
		{
			_configLookupCache.Clear();

			foreach (var moduleName in _moduleConfigs.Keys)
			{
				ReloadModuleConfig(moduleName, true);
			}

			Logger.LogInformation("All Zenith configurations reloaded.");
		}

		public static void RegisterConfig<T>(string moduleName, string groupName, string configName, string description, T defaultValue, ConfigFlag flags) where T : notnull
		{
			var moduleConfig = _moduleConfigs.GetOrAdd(moduleName, k => LoadModuleConfig(moduleName));

			var group = moduleConfig.Groups.GetOrAdd(groupName, new ConfigGroup { Name = groupName });

			var existingConfig = group.Items.GetOrAdd(configName, new ConfigItem
			{
				Name = configName,
				Description = description,
				DefaultValue = defaultValue,
				CurrentValue = defaultValue,
				Flags = flags
			});

			existingConfig.Flags = flags;

			ScheduleModuleConfigSave(moduleName);
		}

		public static bool HasConfigValue(string callerModule, string groupName, string configName)
		{
			string cacheKey = $"{callerModule}:{groupName}:{configName}";
			if (_configLookupCache.TryGetValue(cacheKey, out _))
			{
				return true;
			}

			if (_moduleConfigs.TryGetValue(callerModule, out var moduleConfig))
			{
				if (moduleConfig.Groups.TryGetValue(groupName, out var group))
				{
					var result = group.Items.ContainsKey(configName);

					if (result && group.Items.TryGetValue(configName, out var configItem))
					{
						_configLookupCache[cacheKey] = configItem;
					}

					return result;
				}
			}
			return false;
		}

		public static T GetConfigValue<T>(string callerModule, string groupName, string configName) where T : notnull
		{
			string cacheKey = $"{callerModule}:{groupName}:{configName}";

			if (_configLookupCache.TryGetValue(cacheKey, out var cachedItem))
			{
				try
				{
					return ConvertValue<T>(cachedItem.CurrentValue);
				}
				catch (Exception)
				{
					_configLookupCache.TryRemove(cacheKey, out _);
				}
			}

			if (_moduleConfigs.TryGetValue(callerModule, out var moduleConfig))
			{
				var (found, value) = TryGetConfigValue<T>(moduleConfig, groupName, configName, callerModule);
				if (found)
					return value;
			}

			foreach (var config in _moduleConfigs.Values)
			{
				if (config.ModuleName != callerModule)
				{
					var (found, value) = TryGetConfigValue<T>(config, groupName, configName, callerModule, checkGlobalOnly: true);
					if (found)
						return value;
				}
			}

			throw new KeyNotFoundException($"Configuration '{groupName}.{configName}' not found for module '{callerModule}'");
		}

		private static (bool found, T value) TryGetConfigValue<T>(ModuleConfig config, string groupName, string configName, string callerModule, bool checkGlobalOnly = false) where T : notnull
		{
			if (!config.Groups.TryGetValue(groupName, out var group))
			{
				return (false, default!);
			}

			if (!group.Items.TryGetValue(configName, out var configItem))
			{
				return (false, default!);
			}

			if (configItem != null)
			{
				if (checkGlobalOnly && !configItem.Flags.HasFlag(ConfigFlag.Global))
				{
					Logger.LogWarning($"Config '{groupName}.{configName}' not allowed to be accessed globally");
					return (false, default!);
				}

				if (callerModule != CoreModuleName && callerModule != config.ModuleName && !configItem.Flags.HasFlag(ConfigFlag.Global))
				{
					Logger.LogWarning($"Attempt to access non-global config '{groupName}.{configName}' from module '{callerModule}'");
					return (false, default!);
				}

				if (configItem.CurrentValue == null)
				{
					Logger.LogWarning($"Config '{groupName}.{configName}' has a null value for module '{config.ModuleName}'");
					throw new InvalidOperationException($"Configuration '{groupName}.{configName}' has null value for module '{config.ModuleName}'");
				}

				try
				{
					if (!checkGlobalOnly)
					{
						var cacheKey = $"{callerModule}:{groupName}:{configName}";
						_configLookupCache.TryAdd(cacheKey, configItem);
					}

					return (true, ConvertValue<T>(configItem.CurrentValue));
				}
				catch (InvalidCastException ex)
				{
					Logger.LogError($"Failed to cast config value for '{groupName}.{configName}' to type {typeof(T)}. Stored type: {configItem.CurrentValue.GetType()}. Error: {ex.Message}");
					throw;
				}
				catch (FormatException ex)
				{
					Logger.LogError($"Failed to parse config value '{configItem.CurrentValue}' for '{groupName}.{configName}' to type {typeof(T)}. Error: {ex.Message}");
					throw;
				}
			}

			return (false, default!);
		}

		private static T ConvertValue<T>(object value) where T : notnull
		{
			Type targetType = typeof(T);
			Type sourceType = value.GetType();

			if (sourceType == targetType)
			{
				return (T)value;
			}

			if (targetType == typeof(string) && value is string currentString && string.IsNullOrEmpty(currentString))
			{
				return (T)(object)"";
			}

			if (targetType == typeof(List<string>) && value is List<object> objectList)
			{
				var stringList = objectList.Select(o => o.ToString() ?? string.Empty).ToList();
				return (T)(object)stringList;
			}

			if (targetType == typeof(List<int>) && value is List<object> intObjectList)
			{
				var intList = intObjectList.Select(o => Convert.ToInt32(o, CultureInfo.InvariantCulture)).ToList();
				return (T)(object)intList;
			}

			if (targetType == typeof(List<double>) && value is List<object> doubleObjectList)
			{
				var doubleList = doubleObjectList.Select(o => Convert.ToDouble(o, CultureInfo.InvariantCulture)).ToList();
				return (T)(object)doubleList;
			}

			if (targetType == typeof(List<float>) && value is List<object> floatObjectList)
			{
				var floatList = floatObjectList.Select(o => Convert.ToSingle(o, CultureInfo.InvariantCulture)).ToList();
				return (T)(object)floatList;
			}

			if (targetType == typeof(List<decimal>) && value is List<object> decimalObjectList)
			{
				var decimalList = decimalObjectList.Select(o => Convert.ToDecimal(o, CultureInfo.InvariantCulture)).ToList();
				return (T)(object)decimalList;
			}

			var key = (sourceType, targetType);
			if (_typeConverterCache.TryGetValue(key, out var converter))
			{
				return (T)converter(value);
			}

			if (targetType.IsPrimitive || targetType == typeof(decimal))
			{
				var result = (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);

				_typeConverterCache.TryAdd(key, v => Convert.ChangeType(v, targetType, CultureInfo.InvariantCulture));

				return result;
			}

			return (T)Convert.ChangeType(value, targetType);
		}

		public static void SetConfigValue<T>(string callerModule, string groupName, string configName, T value) where T : notnull
		{
			if (_moduleConfigs.TryGetValue(callerModule, out var moduleConfig))
			{
				if (TrySetConfigValue(moduleConfig, groupName, configName, value, callerModule))
				{
					return;
				}
			}

			foreach (var config in _moduleConfigs.Values)
			{
				if (config.ModuleName != callerModule)
				{
					if (TrySetConfigValue(config, groupName, configName, value, callerModule, checkGlobalOnly: true))
					{
						return;
					}
				}
			}

			throw new KeyNotFoundException($"Configuration '{groupName}.{configName}' not found for module '{callerModule}'");
		}

		private static bool TrySetConfigValue<T>(ModuleConfig moduleConfig, string groupName, string configName, T value, string callerModule, bool checkGlobalOnly = false) where T : notnull
		{
			if (!moduleConfig.Groups.TryGetValue(groupName, out var group))
			{
				return false;
			}

			if (!group.Items.TryGetValue(configName, out var config))
			{
				return false;
			}

			if (config != null)
			{
				if (checkGlobalOnly && !config.Flags.HasFlag(ConfigFlag.Global))
				{
					return false;
				}

				if (callerModule != CoreModuleName && callerModule != moduleConfig.ModuleName && !config.Flags.HasFlag(ConfigFlag.Global))
				{
					Logger.LogWarning($"Attempt to modify non-global config '{groupName}.{configName}' from module '{callerModule}'");
					return false;
				}

				if (callerModule != CoreModuleName && callerModule != moduleConfig.ModuleName)
				{
					if (config.Flags.HasFlag(ConfigFlag.Locked))
					{
						Logger.LogWarning($"Attempt to modify locked configuration '{groupName}.{configName}' for module '{callerModule}'");
						return false;
					}

					if (config.Flags.HasFlag(ConfigFlag.Protected))
					{
						throw new InvalidOperationException($"Cannot modify protected configuration '{groupName}.{configName}' for module '{callerModule}'");
					}
				}

				if (!value.Equals(config.CurrentValue))
				{
					config.CurrentValue = value;

					InvalidateConfigCache(moduleConfig.ModuleName, groupName, configName);

					if (GlobalChangeTracking || config.Flags.HasFlag(ConfigFlag.Global) || callerModule == CoreModuleName)
					{
						ScheduleModuleConfigSave(moduleConfig.ModuleName);
					}
				}

				return true;
			}

			return false;
		}

		private static void ScheduleModuleConfigSave(string moduleName)
		{
			if (_pendingSaveTimers.TryGetValue(moduleName, out var existingTimer))
			{
				existingTimer.Change(_saveDelay, Timeout.InfiniteTimeSpan);
			}
			else
			{
				var timer = new Timer(SaveModuleConfigCallback, moduleName, _saveDelay, Timeout.InfiniteTimeSpan);
				_pendingSaveTimers[moduleName] = timer;
			}
		}

		private static void SaveModuleConfigCallback(object? state)
		{
			var moduleName = (string)state!;

			_pendingSaveTimers.TryRemove(moduleName, out var timer);
			timer?.Dispose();

			SaveModuleConfig(moduleName);
		}

		private static ModuleConfig LoadModuleConfig(string moduleName)
		{
			string filePath = moduleName == CoreModuleName
				? Path.Combine(_baseConfigDirectory, "core.yaml")
				: Path.Combine(_baseConfigDirectory, "modules", $"{moduleName}.yaml");

			var newConfig = new ModuleConfig { ModuleName = moduleName };

			if (File.Exists(filePath))
			{
				try
				{
					var yaml = File.ReadAllText(filePath);
					var loadedConfig = _deserializer.Value.Deserialize<ModuleConfig>(yaml);

					if (loadedConfig != null)
					{
						var existingConfig = _moduleConfigs.GetOrAdd(moduleName, k => new ModuleConfig { ModuleName = moduleName });

						foreach (var group in loadedConfig.Groups)
						{
							var configGroup = existingConfig.Groups.GetOrAdd(group.Key, new ConfigGroup { Name = group.Value.Name });

							foreach (var item in group.Value.Items)
							{
								var existingItem = configGroup.Items.GetOrAdd(item.Key, new ConfigItem
								{
									Name = item.Value.Name,
									Description = item.Value.Description,
									DefaultValue = item.Value.DefaultValue,
									CurrentValue = item.Value.CurrentValue ?? item.Value.DefaultValue,
									Flags = item.Value.Flags
								});

								existingItem.Flags |= item.Value.Flags;
								existingItem.CurrentValue = item.Value.CurrentValue ?? existingItem.CurrentValue;
							}
						}
					}
				}
				catch (Exception ex)
				{
					Logger.LogError($"Error loading config for module {moduleName}: {ex.Message}");
				}
			}

			return newConfig;
		}

		private static void SaveModuleConfig(string moduleName)
		{
			if (_moduleConfigs.TryGetValue(moduleName, out var moduleConfig))
			{
				CleanupUnusedConfigs(moduleName);

				var header = $@"# This file was generated by Zenith Core.
#
# Developer: K4ryuu @ KitsuneLab
# Module: {moduleName}
#";

				var yaml = header + _serializer.Value.Serialize(moduleConfig);

				string filePath = moduleName == CoreModuleName
					? Path.Combine(_baseConfigDirectory, "core.yaml")
					: Path.Combine(_baseConfigDirectory, "modules", $"{moduleName}.yaml");

				File.WriteAllText(filePath, yaml);

				moduleConfig.LastUpdated = DateTime.Now;
			}
		}

		public static void CleanupUnusedConfigs(string moduleName)
		{
			if (_moduleConfigs.TryGetValue(moduleName, out var moduleConfig))
			{
				foreach (var key in moduleConfig.Groups.Keys.ToList())
				{
					if (moduleConfig.Groups[key].Items.IsEmpty)
					{
						moduleConfig.Groups.TryRemove(key, out _);
					}
				}
			}
		}
	}
}
