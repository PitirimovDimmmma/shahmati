using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace shahmati
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            string mode = parameter as string ?? "AI";

            if (mode == "AI")
            {
                // Для кнопки VS ИИ
                return isChecked ? new SolidColorBrush(Color.FromRgb(46, 204, 113)) : new SolidColorBrush(Color.FromRgb(52, 73, 94));
            }
            else
            {
                // Для кнопки VS ЧЕЛОВЕК
                return !isChecked ? new SolidColorBrush(Color.FromRgb(52, 152, 219)) : new SolidColorBrush(Color.FromRgb(52, 73, 94));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}