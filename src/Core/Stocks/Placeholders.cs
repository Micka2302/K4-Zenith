using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Logging;
using Zenith.Models;

namespace Zenith
{
	public sealed partial class Plugin : BasePlugin
	{
		public readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Func<CCSPlayerController, string>>> _pluginPlayerPlaceholders = new();
		public readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Func<string>>> _pluginServerPlaceholders = new();
		private readonly ConcurrentDictionary<string, string> _placeholderFormatCache = new();

		public void Initialize_Placeholders()
		{
			RegisterZenithPlayerPlaceholder("userid", p => p.UserId?.ToString() ?? "Unknown");
			RegisterZenithPlayerPlaceholder("name", p => p.PlayerName);
			RegisterZenithPlayerPlaceholder("steamid", p => p.SteamID.ToString());
			RegisterZenithPlayerPlaceholder("ip", p => p.IpAddress ?? "Unknown");
			RegisterZenithPlayerPlaceholder("country_short", p => GetCountryFromIP(p).ShortName);
			RegisterZenithPlayerPlaceholder("country_long", p => GetCountryFromIP(p).LongName);

			RegisterZenithServerPlaceholder("server_name", () => ConVar.Find("hostname")?.StringValue ?? "Unknown");
			RegisterZenithServerPlaceholder("map_name", () => Server.MapName);
			RegisterZenithServerPlaceholder("max_players", Server.MaxPlayers.ToString);
			RegisterZenithPlayerPlaceholder("arena", p => GetPlayerArenaName(p));
		}

		public string ReplacePlaceholders(CCSPlayerController? player, string text)
		{
			if (string.IsNullOrEmpty(text))
				return text;

			var serverReplaced = ReplacePlaceholdersInternal(text, isPlayerPlaceholder: false, player: null);
			return player == null || !player.IsValid ? serverReplaced : ReplacePlaceholdersInternal(serverReplaced, isPlayerPlaceholder: true, player);
		}

		public string ReplacePlaceholdersInternal(string text, bool isPlayerPlaceholder, CCSPlayerController? player)
		{
			if (string.IsNullOrEmpty(text) || (isPlayerPlaceholder && (player == null || !player.IsValid)))
				return text;

			if (!ContainsAnyPlaceholder(text))
				return text;

			var replacements = new Dictionary<string, string>(16);

			if (isPlayerPlaceholder)
			{
				if (!_pluginPlayerPlaceholders.IsEmpty)
				{
					foreach (var pluginPlaceholders in _pluginPlayerPlaceholders.Values)
					{
						if (pluginPlaceholders.IsEmpty)
							continue;

						foreach (var pair in pluginPlaceholders)
						{
							string placeholder = GetCachedPlaceholderFormat(pair.Key);

							if (text.Contains(placeholder, StringComparison.Ordinal))
								replacements[placeholder] = pair.Value(player!);
						}
					}
				}
			}
			else
			{
				if (!_pluginServerPlaceholders.IsEmpty)
				{
					foreach (var pluginPlaceholders in _pluginServerPlaceholders.Values)
					{
						if (pluginPlaceholders.IsEmpty)
							continue;

						foreach (var pair in pluginPlaceholders)
						{
							string placeholder = GetCachedPlaceholderFormat(pair.Key);

							if (text.Contains(placeholder, StringComparison.Ordinal))
								replacements[placeholder] = pair.Value();
						}
					}
				}
			}

			if (replacements.Count == 0)
				return text;

			string result = text;

			foreach (var replacement in replacements.OrderByDescending(x => x.Key.Length))
				result = result.Replace(replacement.Key, replacement.Value, StringComparison.Ordinal);

			return result;
		}

		private string GetCachedPlaceholderFormat(string key)
		{
			return _placeholderFormatCache.GetOrAdd(key, k => $"{{{k}}}");
		}

		private static bool ContainsAnyPlaceholder(string text)
		{
			return text.Contains('{') && text.Contains('}');
		}

		public void RegisterZenithPlayerPlaceholder(string key, Func<CCSPlayerController, string> valueFunc)
		{
			string callingPlugin = CallerIdentifier.GetCallingPluginName();
			var placeholders = _pluginPlayerPlaceholders.GetOrAdd(callingPlugin, _ => new ConcurrentDictionary<string, Func<CCSPlayerController, string>>());

			if (placeholders.ContainsKey(key))
				Logger.LogWarning($"Player placeholder '{key}' already exists for plugin '{callingPlugin}', overwriting.");

			placeholders[key] = valueFunc;
		}

		public void RegisterZenithServerPlaceholder(string key, Func<string> valueFunc)
		{
			string callingPlugin = CallerIdentifier.GetCallingPluginName();
			var placeholders = _pluginServerPlaceholders.GetOrAdd(callingPlugin, _ => new ConcurrentDictionary<string, Func<string>>());

			if (placeholders.ContainsKey(key))
				Logger.LogWarning($"Server placeholder '{key}' already exists for plugin '{callingPlugin}', overwriting.");

			placeholders[key] = valueFunc;
		}

		public void RemoveModulePlaceholders(string? callingPlugin = null)
		{
			if (callingPlugin != null)
			{
				_pluginPlayerPlaceholders.TryRemove(callingPlugin, out _);
				_pluginServerPlaceholders.TryRemove(callingPlugin, out _);
			}
			else
			{
				_pluginPlayerPlaceholders.Clear();
				_pluginServerPlaceholders.Clear();
			}
		}

		public void ListAllPlaceholders(string? pluginName = null, CCSPlayerController? player = null)
		{
			if (pluginName != null)
			{
				ListPlaceholdersForPlugin(pluginName, player);
			}
			else
			{
				foreach (var plugin in _pluginPlayerPlaceholders.Keys.Union(_pluginServerPlaceholders.Keys).Distinct())
					ListPlaceholdersForPlugin(plugin, player);
			}

			Player.Find(player)?.Print("Placeholder list has been printed to your console.");
		}

		private void ListPlaceholdersForPlugin(string pluginName, CCSPlayerController? player = null)
		{
			PrintToConsole($"Placeholders for plugin '{pluginName}':", player);

			if (_pluginPlayerPlaceholders.TryGetValue(pluginName, out var playerPlaceholders))
			{
				PrintToConsole("  Player placeholders:", player);
				foreach (var placeholder in playerPlaceholders.Keys)
					PrintToConsole($"    - {placeholder}", player);
			}

			if (_pluginServerPlaceholders.TryGetValue(pluginName, out var serverPlaceholders))
			{
				PrintToConsole("  Server placeholders:", player);
				foreach (var placeholder in serverPlaceholders.Keys)
					PrintToConsole($"    - {placeholder}", player);
			}
		}

		public static void PrintToConsole(string text, CCSPlayerController? player)
		{
			if (player == null)
				Server.PrintToConsole(text);
			else
				player.PrintToConsole(text);
		}
	}
}