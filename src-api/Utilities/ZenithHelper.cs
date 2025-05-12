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
        public static IPlayerServices? GetZenithPlayer(this PlayerCapability<IPlayerServices>? capability, CCSPlayerController? player)
        {
            if (player == null) return null;
            try { return capability?.Get(player); }
            catch { return null; }
        }

        /// <summary>
        /// Generic player cache implementation to reduce code duplication across modules
        /// </summary>
        /// <typeparam name="TValue">The type of cache value</typeparam>
        public class PlayerCache<TValue>(TimeSpan expiration)
        {
            private readonly ConcurrentDictionary<ulong, TValue> _cache = new();
            private readonly TimeSpan _expiration = expiration;
            private readonly ConcurrentDictionary<ulong, DateTime> _lastUpdateTimes = new();

            public bool TryGetValue(ulong steamId, out TValue value)
            {
                value = default!;

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
        /// Formats a number to a shorter string format (e.g. 1.25k, 3.45M)
        /// </summary>
        public static string FormatNumber(long number)
        {
            // Handle negative numbers
            bool isNegative = number < 0;
            long absNumber = Math.Abs(number);

            string result;
            if (absNumber >= 1_000_000)
            {
                double millions = absNumber / 1_000_000.0;
                result = $"{millions:F2}M";
            }
            else if (absNumber >= 1_000)
            {
                double thousands = absNumber / 1_000.0;
                result = $"{thousands:F2}k";
            }
            else
            {
                result = absNumber.ToString();
            }

            return isNegative ? "-" + result : result;
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
    }
}