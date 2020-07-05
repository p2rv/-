using System;
using System.Windows.Data;


namespace Battleships
{
    public class BooleanToClientStatusMessageConverter : IValueConverter
    {
        private const string strTrue = "Сlient connected";
        private const string strFalse = "Client is't connected";

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool)
                if ((bool)value)
                    return strTrue;
            return strFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString() == strTrue;
        }
    }
}
