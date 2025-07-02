using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FogScreenControl.Utils
{
    [ValueConversion(typeof(Enums.CalibrationPoint), typeof(Visibility))]
    public class CalibrationPointToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Enums.CalibrationPoint pt = (Enums.CalibrationPoint)value;
            Enums.CalibrationPoint id = (Enums.CalibrationPoint)parameter;
            return pt == id ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException("ConvertBack is not implemented for CalibrationPointToVisibilityConverter.");
    }
}