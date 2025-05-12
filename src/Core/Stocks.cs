using System.Collections.Concurrent;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Logging;
using Zenith.Models;
using System.Reflection;

namespace Zenith
{
	public sealed partial class Plugin : BasePlugin
	{
		public readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Func<CCSPlayerController, string>>> _pluginPlayerPlaceholders = new();
		public readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Func<string>>> _pluginServerPlaceholders = new();

		private readonly ConcurrentDictionary<string, string> _placeholderFormatCache = new();

		private static readonly HashSet<char> _chatColorChars = [.. typeof(ChatColors)
			.GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(f => f.FieldType == typeof(char))
			.Select(f => (char)f.GetValue(null)!)];

		private void Initialize_Placeholders()
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

			// ? Arena Support
			RegisterZenithPlayerPlaceholder("arena", GetPlayerArenaName);
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
			if (string.IsNullOrEmpty(text))
				return text;

			if (isPlayerPlaceholder && (player == null || !player.IsValid))
				return text;

			if (!ContainsAnyPlaceholder(text))
				return text;

			Dictionary<string, string> replacements = [];

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
							string key = pair.Key;
							string placeholder = GetCachedPlaceholderFormat(key);

							if (text.Contains(placeholder, StringComparison.Ordinal))
							{
								string value = pair.Value(player!);
								replacements[placeholder] = value;
							}
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
							string key = pair.Key;
							string placeholder = GetCachedPlaceholderFormat(key);

							if (text.Contains(placeholder, StringComparison.Ordinal))
							{
								string value = pair.Value();
								replacements[placeholder] = value;
							}
						}
					}
				}
			}

			if (replacements.Count == 0)
				return text;

			string result = text;

			foreach (var replacement in replacements.OrderByDescending(x => x.Key.Length))
			{
				result = result.Replace(replacement.Key, replacement.Value, StringComparison.Ordinal);
			}

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
			{
				Logger.LogWarning($"Player placeholder '{key}' already exists for plugin '{callingPlugin}', overwriting.");
			}

			placeholders[key] = valueFunc;
		}

		public void RegisterZenithServerPlaceholder(string key, Func<string> valueFunc)
		{
			string callingPlugin = CallerIdentifier.GetCallingPluginName();

			var placeholders = _pluginServerPlaceholders.GetOrAdd(callingPlugin, _ => new ConcurrentDictionary<string, Func<string>>());

			if (placeholders.ContainsKey(key))
			{
				Logger.LogWarning($"Server placeholder '{key}' already exists for plugin '{callingPlugin}', overwriting.");
			}

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

		public void DisposeModule(string callingPlugin)
		{
			Logger.LogInformation($"Disposing module '{callingPlugin}' and freeing resources.");

			RemoveModuleCommands(callingPlugin);
			RemoveModulePlaceholders(callingPlugin);
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
				{
					ListPlaceholdersForPlugin(plugin, player);
				}
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
				{
					PrintToConsole($"    - {placeholder}", player);
				}
			}

			if (_pluginServerPlaceholders.TryGetValue(pluginName, out var serverPlaceholders))
			{
				PrintToConsole("  Server placeholders:", player);
				foreach (var placeholder in serverPlaceholders.Keys)
				{
					PrintToConsole($"    - {placeholder}", player);
				}
			}
		}

		public static void PrintToConsole(string text, CCSPlayerController? player)
		{
			if (player == null)
			{
				Server.PrintToConsole(text);
			}
			else
			{
				player.PrintToConsole(text);
			}
		}

		private (string ShortName, string LongName) GetCountryFromIP(CCSPlayerController? player)
		{
			if (player is null || !Player.List.TryGetValue(player.SteamID, out var playerData))
				return ("??", "Unknown");

			if (playerData._country != ("??", "Unknown"))
				return playerData._country;

			playerData._country = player == null
				? ("??", "Unknown")
				: GetCountryFromIP(player.IpAddress?.Split(':')[0]);

			return playerData._country;
		}

		private (string ShortName, string LongName) GetCountryFromIP(string? ipAddress)
		{
			if (string.IsNullOrEmpty(ipAddress))
				return ("??", "Unknown");

			string databasePath = Path.Combine(ModuleDirectory, "GeoLite2-Country.mmdb");
			if (!File.Exists(databasePath))
				return ("??", "Unknown");

			try
			{
				using var reader = new DatabaseReader(databasePath);
				var response = reader.Country(ipAddress);

				return (
					response.Country.IsoCode ?? "??",
					response.Country.Name ?? "Unknown"
				);
			}
			catch
			{
				return ("??", "Unknown");
			}
		}

		public static string RemoveColorChars(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			var result = new StringBuilder(input.Length);

			foreach (char c in input)
			{
				if (!_chatColorChars.Contains(c))
				{
					result.Append(c);
				}
			}

			return result.ToString();
		}
	}
}