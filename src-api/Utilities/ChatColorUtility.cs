using System.Reflection;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace ZenithAPI
{
	/// <summary>
	/// Utility class for handling chat colors.
	/// </summary>
	public static class ChatColorUtility
	{
		private static readonly Dictionary<string, char> _chatColors;
		private static readonly Regex _colorPattern;

		static ChatColorUtility()
		{
			// Initialize the dictionary once with all available chat colors
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
		public static string ApplyPrefixColors(string msg)
		{
			if (string.IsNullOrEmpty(msg))
				return msg;

			// Process the string with regex replacement
			return _colorPattern.Replace(msg, match =>
			{
				string key = match.Value.Trim('{', '}');
				return _chatColors.TryGetValue(key, out char color) ? color.ToString() : match.Value;
			});
		}

		/// <summary>
		/// Gets the character value for a named chat color.
		/// </summary>
		public static char GetChatColorValue(string colorName, CCSPlayerController? player = null)
		{
			// Special case for team colors which depend on the player
			if (colorName.Equals("team", StringComparison.OrdinalIgnoreCase) && player != null)
			{
				return ChatColors.ForPlayer(player);
			}

			// Direct lookup in the color dictionary
			if (_chatColors.TryGetValue(colorName, out char color))
			{
				return color;
			}

			return ChatColors.Default;
		}
	}
}