using darts_hub.model;
using System;
using System.Globalization;

namespace darts_hub.control
{
    /// <summary>
    /// Helper class for extracting and working with argument type ranges
    /// </summary>
    public static class ArgumentTypeHelper
    {
        /// <summary>
        /// Extracts the minimum and maximum values from an argument type
        /// </summary>
        /// <param name="argument">The argument to extract range from</param>
        /// <param name="minimum">Output minimum value</param>
        /// <param name="maximum">Output maximum value</param>
        /// <returns>True if range was successfully extracted</returns>
        public static bool TryGetNumericRange(Argument argument, out decimal minimum, out decimal maximum)
        {
            minimum = decimal.MinValue;
            maximum = decimal.MaxValue;

            if (argument == null || string.IsNullOrEmpty(argument.Type))
                return false;

            var typeClear = argument.GetTypeClear();
            
            // Only process numeric types
            if (typeClear != Argument.TypeInt && typeClear != Argument.TypeFloat)
                return false;

            // Check if type contains range definition
            if (!argument.Type.Contains("[") || !argument.Type.Contains("]"))
                return false;

            try
            {
                // Extract range part: "int[0..10]" -> "0..10"
                var rangeStart = argument.Type.IndexOf('[') + 1;
                var rangeEnd = argument.Type.IndexOf(']');
                var rangeString = argument.Type.Substring(rangeStart, rangeEnd - rangeStart);

                // Split by ".."
                var parts = rangeString.Split(new[] { ".." }, StringSplitOptions.None);
                
                if (parts.Length != 2)
                    return false;

                // Parse minimum
                if (typeClear == Argument.TypeInt)
                {
                    if (!int.TryParse(parts[0], out var minInt))
                        return false;
                    minimum = minInt;
                }
                else if (typeClear == Argument.TypeFloat)
                {
                    if (!decimal.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out minimum))
                        return false;
                }

                // Parse maximum
                if (typeClear == Argument.TypeInt)
                {
                    if (!int.TryParse(parts[1], out var maxInt))
                        return false;
                    maximum = maxInt;
                }
                else if (typeClear == Argument.TypeFloat)
                {
                    if (!decimal.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out maximum))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the increment step for a numeric argument based on its type
        /// </summary>
        public static decimal GetIncrementStep(Argument argument)
        {
            if (argument == null)
                return 1;

            var typeClear = argument.GetTypeClear();
            
            return typeClear switch
            {
                Argument.TypeInt => 1,
                Argument.TypeFloat => 0.1m,
                _ => 1
            };
        }

        /// <summary>
        /// Gets the decimal places for display based on argument type
        /// </summary>
        public static int GetDecimalPlaces(Argument argument)
        {
            if (argument == null)
                return 0;

            var typeClear = argument.GetTypeClear();
            
            return typeClear switch
            {
                Argument.TypeFloat => 2,
                _ => 0
            };
        }

        /// <summary>
        /// Validates if a value is within the argument's range
        /// </summary>
        public static bool IsValueInRange(Argument argument, decimal value)
        {
            if (!TryGetNumericRange(argument, out var min, out var max))
                return true; // No range restriction

            return value >= min && value <= max;
        }

        /// <summary>
        /// Gets a format string for displaying numeric values
        /// </summary>
        public static string GetFormatString(Argument argument)
        {
            if (argument == null)
                return "F0";

            var typeClear = argument.GetTypeClear();
            
            return typeClear switch
            {
                Argument.TypeFloat => $"F{GetDecimalPlaces(argument)}",
                _ => "F0"
            };
        }
    }
}
