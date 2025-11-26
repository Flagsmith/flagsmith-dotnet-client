using System;
using System.Globalization;

namespace FlagsmithEngine.Utils
{
    /// <summary>
    /// Provides culture-invariant conversion methods for numeric types.
    /// </summary>
    /// <remarks>
    /// Ensures consistent numeric parsing across all system locales by using InvariantCulture.
    /// Prevents locale-specific decimal separator issues (e.g., "1.23" vs "1,23").
    /// </remarks>
    internal static class InvariantConvert
    {
        /// <summary>
        /// Converts the string representation of a number to its 32-bit signed integer equivalent
        /// using culture-invariant formatting.
        /// </summary>
        /// <param name="value">A string containing a number to convert.</param>
        /// <returns>A 32-bit signed integer equivalent to the number in value.</returns>
        public static int ToInt32(string value) =>
            Convert.ToInt32(value, CultureInfo.InvariantCulture);

        /// <summary>
        /// Converts the string representation of a number to its 64-bit signed integer equivalent
        /// using culture-invariant formatting.
        /// </summary>
        /// <param name="value">A string containing a number to convert.</param>
        /// <returns>A 64-bit signed integer equivalent to the number in value.</returns>
        public static long ToInt64(string value) =>
            Convert.ToInt64(value, CultureInfo.InvariantCulture);

        /// <summary>
        /// Converts the string representation of a number to its double-precision floating-point equivalent
        /// using culture-invariant formatting.
        /// </summary>
        /// <param name="value">A string containing a number to convert.</param>
        /// <returns>A double-precision floating-point number equivalent to the numeric value in value.</returns>
        public static double ToDouble(string value) =>
            Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }
}
