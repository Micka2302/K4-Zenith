using System.Reflection;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace ZenithAPI
{
	/// <summary>
	/// Utility class for handling chat colors.
	/// </summary>
	public static partial class ChatColor
	{
		private static readonly Dictionary<string, char> _chatColors;
		private static readonly Regex? _colorPattern = null;

		static ChatColor()
		{
			if (_chatColors?.Count > 0)
				return;

			_chatColors = new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase);
			var chatColorType = typeof(ChatColors);
			var fields = chatColorType.GetFields(BindingFlags.Public | BindingFlags.Static);

			foreach (var field in fields)
			{
				if (field.FieldType == typeof(char) && !field.IsObsolete())
				{
					string colorName = field.Name.ToLowerInvariant();
					char colorValue = (char)field.GetValue(null)!;
					_chatColors[colorName] = colorValue;
				}
			}

			// Build regex pattern once during initialization
			string pattern = string.Join("|", _chatColors.Keys.Select(k => $@"\{{{k}\}}|{k}"));

			// Using compiled regex for better performance
			_colorPattern = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
		}

		/// <summary>
		/// Applies color prefixes in a string, replacing {colorname} with the actual color character.
		/// </summary>
		public static string ReplaceColors(string msg, CCSPlayerController? player = null)
		{
			if (_colorPattern == null || string.IsNullOrEmpty(msg))
				return msg;

			// Replace color tags with actual color characters
			msg = _colorPattern.Replace(msg, match =>
			{
				string key = match.Value.Trim('{', '}').ToLowerInvariant();
				return _chatColors.TryGetValue(key, out char color) ? color.ToString() : string.Empty;
			});

			// Replace team color if applicable
			if (player != null && msg.Contains("{team}", StringComparison.OrdinalIgnoreCase))
			{
				msg = msg.Replace("{team}", ChatColors.ForPlayer(player).ToString());
			}

			// Replace random color if applicable
			if (msg.Contains("{random}", StringComparison.OrdinalIgnoreCase))
			{
				msg = msg.Replace("{random}", _chatColors.Values.ElementAt(Random.Shared.Next(0, _chatColors.Count)).ToString());
			}

			return msg;
		}

		/// <summary>
		/// Gets the character value for a named chat color.
		/// </summary>
		public static char GetValue(string colorName, CCSPlayerController? player = null)
		{
			// Special case for team colors which depend on the player
			if (colorName.Equals("team", StringComparison.OrdinalIgnoreCase))
			{
				return player != null ? ChatColors.ForPlayer(player) : ChatColors.Default;
			}

			// Special case for random color
			if (colorName.Equals("random", StringComparison.OrdinalIgnoreCase))
			{
				return _chatColors.Values.ElementAt(Random.Shared.Next(0, _chatColors.Count));
			}

			// Check if the color name exists in the dictionary
			return _chatColors.TryGetValue(colorName, out char color) ? color : ChatColors.Default;
		}

		/// <summary>
		/// Removes color characters from a string.
		/// </summary>
		/// <param name="text">The text to clean.</param>
		/// <returns>The cleaned text without color characters.</returns>
		public static string RemoveColorKeys(string msg)
		{
			if (string.IsNullOrEmpty(msg))
				return msg;

			// Remove color characters
			foreach (var color in _chatColors.Keys)
			{
				msg = msg.Replace(color.ToString(), string.Empty);
			}

			// Remove any remaining color tags
			msg = msg.Replace("{team}", string.Empty);
			msg = msg.Replace("{random}", string.Empty);

			return msg;
		}

		/// <summary>
		/// Removes color characters from a string, including the prefix.
		/// </summary>
		/// <param name="text">The text to clean.</param>
		/// <returns>The cleaned text without color characters and prefix.</returns>
		public static string RemoveColorValues(string msg)
		{
			if (string.IsNullOrEmpty(msg))
				return msg;

			// Remove color characters
			foreach (var color in _chatColors.Values)
			{
				msg = msg.Replace(color.ToString(), string.Empty);
			}

			return msg;
		}

		private static bool IsObsolete(this FieldInfo field)
		{
			return field.GetCustomAttribute<ObsoleteAttribute>() != null;
		}
	}
}