using System.Reflection;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core;

namespace Zenith
{
    public sealed partial class Plugin : BasePlugin
    {
        private static readonly HashSet<char> _chatColorChars = [.. typeof(ChatColors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(char))
            .Select(f => (char)f.GetValue(null)!)];

        public static string RemoveColorChars(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            int resultLength = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (!_chatColorChars.Contains(input[i]))
                    resultLength++;
            }

            if (resultLength == input.Length)
                return input;

            return string.Create(resultLength, input, (chars, state) =>
            {
                int pos = 0;
                for (int i = 0; i < state.Length; i++)
                {
                    char c = state[i];
                    if (!_chatColorChars.Contains(c))
                        chars[pos++] = c;
                }
            });
        }

        public static string RemoveColorCharsSpan(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            Span<char> buffer = stackalloc char[input.Length];
            int pos = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (!_chatColorChars.Contains(c))
                    buffer[pos++] = c;
            }

            return pos == input.Length ? input : new string(buffer[..pos]);
        }
    }
}