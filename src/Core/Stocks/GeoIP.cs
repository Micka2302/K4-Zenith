using System.Collections.Concurrent;
using CounterStrikeSharp.API.Core;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Logging;
using Zenith.Models;

namespace Zenith
{
	public sealed partial class Plugin : BasePlugin
	{
		private DatabaseReader? _geoIpDatabaseReader;
		private readonly ConcurrentDictionary<string, (string ShortName, string LongName)> _ipCountryCache = new();
		private static readonly (string ShortName, string LongName) _defaultCountry = ("??", "Unknown");

		public void Initialize_GeoIP()
		{
			if (_geoIpDatabaseReader != null)
				return;

			string databasePath = Path.Combine(ModuleDirectory, "GeoLite2-Country.mmdb");
			if (File.Exists(databasePath))
			{
				try
				{
					_geoIpDatabaseReader = new DatabaseReader(databasePath);
				}
				catch (Exception ex)
				{
					Logger.LogError($"Failed to load GeoIP database: {ex.Message}");
				}
			}
			else
			{
				Logger.LogWarning($"GeoIP database not found at {databasePath}");
			}
		}

		public (string ShortName, string LongName) GetCountryFromIP(CCSPlayerController? player)
		{
			if (player is null || !Player.List.TryGetValue(player.SteamID, out var playerData))
				return _defaultCountry;

			if (playerData._country != _defaultCountry)
				return playerData._country;

			playerData._country = player == null
				? _defaultCountry
				: GetCountryFromIP(player.IpAddress?.Split(':')[0]);

			return playerData._country;
		}

		public (string ShortName, string LongName) GetCountryFromIP(string? ipAddress)
		{
			if (string.IsNullOrEmpty(ipAddress))
				return _defaultCountry;

			if (_ipCountryCache.TryGetValue(ipAddress, out var cachedResult))
				return cachedResult;

			if (_geoIpDatabaseReader == null)
			{
				Initialize_GeoIP();
				if (_geoIpDatabaseReader == null)
				{
					_ipCountryCache[ipAddress] = _defaultCountry;
					return _defaultCountry;
				}
			}

			try
			{
				var response = _geoIpDatabaseReader.Country(ipAddress);
				var result = (
					response.Country.IsoCode ?? "??",
					response.Country.Name ?? "Unknown"
				);

				_ipCountryCache[ipAddress] = result;
				return result;
			}
			catch (Exception ex)
			{
				if (ex is not MaxMind.GeoIP2.Exceptions.AddressNotFoundException)
					Logger.LogError($"Error getting country for IP {ipAddress}: {ex.Message}");

				_ipCountryCache[ipAddress] = _defaultCountry;
				return _defaultCountry;
			}
		}
	}
}