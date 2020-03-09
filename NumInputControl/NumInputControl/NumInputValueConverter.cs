using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NumInputControl
{
    [Obsolete("Use DragInput instead")]
    public class NumInputValueConverter : IMultiValueConverter
    {
        private int precision;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            precision = (int)values[1];
            return string.Format("{0:F" + precision + "}", values[0]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            double.TryParse(value.ToString(), out double val);
            return new object[] { val, precision };
        }
    }
}
