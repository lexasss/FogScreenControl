using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FogControlWithKinect.Utils
{
    [ValueConversion(typeof(Services.CalibrationPoint), typeof(Visibility))]
    public class CalibrationPointToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Services.CalibrationPoint pt = (Services.CalibrationPoint)value;
            Services.CalibrationPoint id = (Services.CalibrationPoint)parameter;
            return pt == id ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException("ConvertBack is not implemented for CalibrationPointToVisibilityConverter.");
    }
}