using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace shahmati.Converters
{
    public class DifficultyToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string difficulty)
            {
                return difficulty switch
                {
                    "Beginner" => new SolidColorBrush(Color.FromRgb(46, 204, 113)), // Зеленый
                    "Easy" => new SolidColorBrush(Color.FromRgb(52, 152, 219)), // Синий
                    "Medium" => new SolidColorBrush(Color.FromRgb(241, 196, 15)), // Желтый
                    "Hard" => new SolidColorBrush(Color.FromRgb(230, 126, 34)), // Оранжевый
                    "Expert" => new SolidColorBrush(Color.FromRgb(231, 76, 60)), // Красный
                    _ => new SolidColorBrush(Color.FromRgb(149, 165, 166)) // Серый
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}