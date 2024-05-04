using Avalonia.Data.Converters;
using Avalonia;
using System;
using System.Globalization;


namespace model
{
    public class DotDecimalSeparatorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue = value as string;
            if (!string.IsNullOrEmpty(stringValue))
            {
                var customCulture = (CultureInfo)culture.Clone();
                customCulture.NumberFormat.NumberDecimalSeparator = ".";

                if (double.TryParse(stringValue, NumberStyles.Number, customCulture, out double result))
                {
                    return result;
                }
            }
            return AvaloniaProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                var customCulture = (CultureInfo)culture.Clone();
                customCulture.NumberFormat.NumberDecimalSeparator = ".";
                return doubleValue.ToString("N2", customCulture);
            }
            return value;
        }
    }

}
