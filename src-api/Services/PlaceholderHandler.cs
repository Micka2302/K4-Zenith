using System.Collections.Concurrent;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace ZenithAPI
{
	public sealed partial class PlaceholderHandler(BasePlugin plugin)
	{
		public static readonly ConcurrentDictionary<BasePlugin, List<ZenithPlayerPlaceholder>> _playerPlaceholders = new();
		public static readonly ConcurrentDictionary<BasePlugin, List<ZenithServerPlaceholder>> _serverPlaceholders = new();

		private readonly BasePlugin _plugin = plugin;
		private readonly ILogger _logger = plugin.Logger;

		public void RegisterPlayerPlaceholder(string placeholder, Func<CCSPlayerController, string> callback)
		{
			var existingPlaceholder = FindPlaceholder<ZenithPlayerPlaceholder>(placeholder);

			if (existingPlaceholder != null)
			{
				if (existingPlaceholder.Module != _plugin)
				{
					_logger.LogError($"Player placeholder '{placeholder}' is already registered by plugin '{existingPlaceholder.Module.ModuleName}'. The placeholder cannot be registered by '{_plugin.ModuleName}'.");
					return;
				}

				RemoveExistingPlaceholder(existingPlaceholder);
				_logger.LogWarning($"Player placeholder '{placeholder}' already exists for plugin '{_plugin.ModuleName}', overwriting.");
			}

			CreatePlayerPlaceholder(placeholder, callback);
		}

		public void RegisterServerPlaceholder(string placeholder, Func<string> callback)
		{
			var existingPlaceholder = FindPlaceholder<ZenithServerPlaceholder>(placeholder);

			if (existingPlaceholder != null)
			{
				if (existingPlaceholder.Module != _plugin)
				{
					_logger.LogError($"Server placeholder '{placeholder}' is already registered by plugin '{existingPlaceholder.Module.ModuleName}'. The placeholder cannot be registered by '{_plugin.ModuleName}'.");
					return;
				}

				RemoveExistingPlaceholder(existingPlaceholder);
				_logger.LogWarning($"Server placeholder '{placeholder}' already exists for plugin '{_plugin.ModuleName}', overwriting.");
			}

			CreateServerPlaceholder(placeholder, callback);
		}

		public static T? FindPlaceholder<T>(string placeholder) where T : class
		{
			if (typeof(T) == typeof(ZenithPlayerPlaceholder))
			{
				return _playerPlaceholders.Values.SelectMany(ph => ph).Where(ph => ph.Placeholder == placeholder).FirstOrDefault() as T;
			}
			else if (typeof(T) == typeof(ZenithServerPlaceholder))
			{
				return _serverPlaceholders.Values.SelectMany(ph => ph).Where(ph => ph.Placeholder == placeholder).FirstOrDefault() as T;
			}

			return null;
		}

		public static void RemoveExistingPlaceholder<T>(T existingPlaceholder) where T : class
		{
			if (existingPlaceholder != null)
			{
				if (existingPlaceholder is ZenithPlayerPlaceholder playerPlaceholder)
				{
					_playerPlaceholders.TryRemove(playerPlaceholder.Module, out _);
				}
				else if (existingPlaceholder is ZenithServerPlaceholder serverPlaceholder)
				{
					_serverPlaceholders.TryRemove(serverPlaceholder.Module, out _);
				}
			}
		}

		internal void CreatePlayerPlaceholder(string placeholder, Func<CCSPlayerController, string> callback)
		{
			var newPlaceholder = new ZenithPlayerPlaceholder
			{
				Module = _plugin,
				Placeholder = placeholder,
				Callback = callback
			};

			_playerPlaceholders.GetOrAdd(_plugin, _ => []).Add(newPlaceholder);
		}

		internal void CreateServerPlaceholder(string placeholder, Func<string> callback)
		{
			var newPlaceholder = new ZenithServerPlaceholder
			{
				Module = _plugin,
				Placeholder = placeholder,
				Callback = callback
			};

			_serverPlaceholders.GetOrAdd(_plugin, _ => []).Add(newPlaceholder);
		}

		public void UnregisterPlayerPlaceholder(string placeholder)
		{
			if (_playerPlaceholders.TryGetValue(_plugin, out var existingPlaceholders))
			{
				var placeholderToRemove = existingPlaceholders.FirstOrDefault(ph => ph.Placeholder == placeholder);
				if (placeholderToRemove != null)
				{
					existingPlaceholders.Remove(placeholderToRemove);
				}
			}
			else
			{
				_logger.LogWarning($"Failed to unregister player placeholder '{placeholder}' for plugin '{_plugin.ModuleName}'. Placeholder not found.");
			}
		}

		public void UnregisterServerPlaceholder(string placeholder)
		{
			if (_serverPlaceholders.TryGetValue(_plugin, out var existingPlaceholders))
			{
				var placeholderToRemove = existingPlaceholders.FirstOrDefault(ph => ph.Placeholder == placeholder);
				if (placeholderToRemove != null)
				{
					existingPlaceholders.Remove(placeholderToRemove);
				}
			}
			else
			{
				_logger.LogWarning($"Failed to unregister server placeholder '{placeholder}' for plugin '{_plugin.ModuleName}'. Placeholder not found.");
			}
		}

		public IReadOnlyList<ZenithPlayerPlaceholder> GetPlayerPlaceholders()
		{
			if (_playerPlaceholders.TryGetValue(_plugin, out var placeholders))
			{
				return placeholders.AsReadOnly();
			}

			return [];
		}

		public IReadOnlyList<ZenithServerPlaceholder> GetServerPlaceholders()
		{
			if (_serverPlaceholders.TryGetValue(_plugin, out var placeholders))
			{
				return placeholders.AsReadOnly();
			}

			return [];
		}

		public static IReadOnlyList<ZenithPlayerPlaceholder> GetAllPlayerPlaceholders()
		{
			return [.. _playerPlaceholders.Values.SelectMany(ph => ph)];
		}

		public static IReadOnlyList<ZenithServerPlaceholder> GetAllServerPlaceholders()
		{
			return [.. _serverPlaceholders.Values.SelectMany(ph => ph)];
		}

		public void RemovePlaceholders()
		{
			_playerPlaceholders.TryRemove(_plugin, out _);
			_serverPlaceholders.TryRemove(_plugin, out _);
		}

		public static string ReplacePlayerPlaceholders(string text, CCSPlayerController player)
		{
			foreach (var placeholder in GetAllPlayerPlaceholders())
			{
				text = text.Replace(placeholder.Placeholder, placeholder.Callback(player));
			}

			return text;
		}

		public static string ReplaceServerPlaceholders(string text)
		{
			foreach (var placeholder in GetAllServerPlaceholders())
			{
				text = text.Replace(placeholder.Placeholder, placeholder.Callback());
			}

			return text;
		}

		public static string ReplacePlaceholders(string text, CCSPlayerController? player = null)
		{
			if (player?.IsValid == true)
				text = ReplacePlayerPlaceholders(text, player);

			text = ReplaceServerPlaceholders(text);
			return text;
		}
	}
}