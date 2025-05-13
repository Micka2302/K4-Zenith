
namespace ZenithAPI
{
    public static class ZenithString
    {
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
        /// Formats a number into a more readable string
        /// </summary>
        /// <param name="number">The number to format</param>
        /// <returns>A string representation of the number</returns>
        public static string FormatNumber(long number)
        {
            if (Math.Abs(number) < 1000)
            {
                return number.ToString();
            }
            else if (Math.Abs(number) < 1000000)
            {
                double value = number / 1000.0;
                return value.ToString("0.##") + "K";
            }
            else
            {
                double value = number / 1000000.0;
                return value.ToString("0.##") + "M";
            }
        }
    }
}