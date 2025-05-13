using System.Collections.Concurrent;
using System.Reflection;
using CounterStrikeSharp.API.Core;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Logging;

namespace ZenithAPI
{
	public class GeoIP
	{
		private static DatabaseReader? _geoIpDatabaseReader;
		private static readonly (string ShortName, string LongName) _defaultCountry = ("??", "Unknown");

		private readonly BasePlugin _plugin;
		private readonly ILogger _logger;

		public GeoIP(BasePlugin plugin)
		{
			_plugin = plugin;
			_logger = plugin.Logger;

			if (_geoIpDatabaseReader != null)
				return;

			string databasePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "GeoLite2-Country.mmdb");
			if (File.Exists(databasePath))
			{
				try
				{
					_geoIpDatabaseReader = new DatabaseReader(databasePath);
				}
				catch (Exception ex)
				{
					_logger.LogError($"Failed to load GeoIP database: {ex.Message}");
				}
			}
			else
			{
				_logger.LogWarning($"GeoIP database not found at {databasePath}");
			}
		}

		public (string ShortName, string LongName) GetPlayerCountry(CCSPlayerController? player)
		{
			if (player?.IsValid == true)
			{
				return GetIPCountry(player.IpAddress?.Split(':')[0]);
			}

			return _defaultCountry;
		}

		public (string ShortName, string LongName) GetIPCountry(string? ipAddress)
		{
			if (string.IsNullOrEmpty(ipAddress))
				return _defaultCountry;

			if (_geoIpDatabaseReader == null)
				return _defaultCountry;

			try
			{
				var response = _geoIpDatabaseReader.Country(ipAddress);
				var result = (
					response.Country.IsoCode ?? _defaultCountry.ShortName,
					response.Country.Name ?? _defaultCountry.LongName
				);

				return result;
			}
			catch (Exception ex)
			{
				if (ex is not MaxMind.GeoIP2.Exceptions.AddressNotFoundException)
					_logger.LogError($"Error getting country for IP {ipAddress}: {ex.Message}");

				return _defaultCountry;
			}
		}
	}
}