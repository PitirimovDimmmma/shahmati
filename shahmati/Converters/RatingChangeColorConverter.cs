using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace shahmati.Converters
{
    public class RatingChangeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int ratingChange)
            {
                return ratingChange > 0 ? Brushes.Green :
                       ratingChange < 0 ? Brushes.Red : Brushes.Gray;
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}