using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SinkMyBattleship_2._0.Converters
{
    public class FireConverter : IValueConverter
    {
        //public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        //{
        //    if (value == null)
        //    {
        //        return Brushes.Yellow;
        //    }

        //    var text = (string)value;
        //    var control = (number >= 0 && number <= 255) ? true : false;

        //    return control == true ? Brushes.White : Brushes.Red;
        //}

        //public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        //{
        //    throw new NotImplementedException();
        //}
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var vm = (Dictionary<string, int>)value;

            switch (vm[parameter.ToString()])
            {
                case 0:
                    return Brushes.LightBlue;
                case 1:
                    return Brushes.Red;
                case 2:
                    return Brushes.White;
                default:
                    return Brushes.LightBlue;
            }

            //throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
