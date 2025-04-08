using System;
using System.Globalization;
using System.Windows.Data;

namespace MemoryGameWPF.Converters 
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return Binding.DoNothing; // Or return false/true depending on desired default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return Binding.DoNothing; // Or throw exception
        }
    }
}