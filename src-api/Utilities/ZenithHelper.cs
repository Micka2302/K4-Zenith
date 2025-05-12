using System.Collections.Concurrent;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;

namespace ZenithAPI
{
    /// <summary>
    /// Helper utilities for Zenith modules to reduce code duplication
    /// </summary>
    public static class ZenithHelper
    {
        /// <summary>
        /// Safely retrieves a player's Zenith service interface
        /// </summary>
        /// <param name="player">The player controller</param>
        /// <param name="capability">The player capability for Zenith services</param>
        /// <returns>The player services, or null if not available</returns>
        public static IPlayerServices? GetZenithPlayer(
            CCSPlayerController? player,
            PlayerCapability<IPlayerServices> capability)
        {
            if (player == null) return null;
            try { return capability.Get(player); }
            catch { return null; }
        }

        /// <summary>
        /// Generic player cache implementation to reduce code duplication across modules
        /// </summary>
        /// <typeparam name="TValue">The type of cache value</typeparam>
        public class PlayerCache<TValue>
        {
            private readonly ConcurrentDictionary<ulong, TValue> _cache = new();
            private readonly TimeSpan _expiration;
            private readonly ConcurrentDictionary<ulong, DateTime> _lastUpdateTimes = new();

            public PlayerCache(TimeSpan expiration)
            {
                _expiration = expiration;
            }

            public bool TryGetValue(ulong steamId, out TValue value)
            {
                // Initialize default value (will be overwritten if found)
                value = default!;

                // Try to get the value if it exists and is not expired
                if (_cache.TryGetValue(steamId, out var cachedValue) &&
                    _lastUpdateTimes.TryGetValue(steamId, out var lastUpdate) &&
                    DateTime.Now - lastUpdate < _expiration)
                {
                    // Only assign if not null to avoid warnings
                    if (cachedValue != null)
                    {
                        value = cachedValue;
                        return true;
                    }
                }

                return false;
            }

            public void Set(ulong steamId, TValue value)
            {
                _cache[steamId] = value;
                _lastUpdateTimes[steamId] = DateTime.Now;
            }

            public void Remove(ulong steamId)
            {
                _cache.TryRemove(steamId, out _);
                _lastUpdateTimes.TryRemove(steamId, out _);
            }

            public IEnumerable<KeyValuePair<ulong, TValue>> GetAll()
            {
                return _cache;
            }

            public void Clear()
            {
                _cache.Clear();
                _lastUpdateTimes.Clear();
            }

            public void CleanupExpired()
            {
                var now = DateTime.Now;
                var expiredKeys = _lastUpdateTimes
                    .Where(kvp => now - kvp.Value > _expiration)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    Remove(key);
                }
            }
        }

        /// <summary>
        /// Formats a number to a shorter string format (e.g. 1.2k, 3.5M)
        /// </summary>
        public static string FormatNumber(int number)
        {
            if (number >= 1000000)
            {
                double millions = number / 1000000.0;
                return $"{millions:F1}M";
            }
            else if (number >= 1000)
            {
                double thousands = number / 1000.0;
                return $"{thousands:F1}k";
            }

            return number.ToString();
        }

        /// <summary>
        /// Truncates a string to a maximum length, adding an ellipsis if needed
        /// </summary>
        public static string TruncateString(string input, int maxLength = 12)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
                return input;

            return string.Concat(input.AsSpan(0, maxLength), "...");
        }

        /// <summary>
        /// Strips comments starting with // from a JSON string
        /// </summary>
        public static string StripJsonComments(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            var result = new System.Text.StringBuilder(json.Length);
            using (var reader = new StringReader(json))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmedLine = line.TrimStart();
                    if (!trimmedLine.StartsWith("//"))
                    {
                        result.AppendLine(line);
                    }
                }
            }
            return result.ToString();
        }
    }
}