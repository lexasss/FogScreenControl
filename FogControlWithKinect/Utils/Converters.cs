using System;
using System.Globalization;
using System.Windows.Data;

namespace FogControlWithKinect.Utils
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BoolInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            !(bool)value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            !(bool)value;
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class CalibrationPointToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Services.CalibrationPoint pt = (Services.CalibrationPoint)value;
            Services.CalibrationPoint id = (Services.CalibrationPoint)parameter;
            return pt == id ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException("ConvertBack is not implemented for CalibrationPointToVisibilityConverter.");
    }
}