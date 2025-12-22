using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace shahmati.Converters
{
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string imagePath && !string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    // Сначала пробуем как в MainWindow - просто как относительный путь
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    // Проверяем, загрузилось ли изображение
                    if (bitmap.Width > 0 && bitmap.Height > 0)
                        return bitmap;

                    // Если не загрузилось, пробуем другие варианты
                    return TryAlternativePaths(imagePath);
                }
                catch
                {
                    // Если первый способ не работает, пробуем альтернативные пути
                    return TryAlternativePaths(imagePath);
                }
            }
            return null;
        }

        private BitmapImage TryAlternativePaths(string imagePath)
        {
            try
            {
                // Пробуем как абсолютный путь
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
                if (File.Exists(fullPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    return bitmap;
                }

                // Пробуем pack URI
                string packUri = $"pack://application:,,,/{imagePath}";
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(packUri, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    return bitmap;
                }
                catch { }

                // Если ничего не работает, возвращаем null
                return null;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}