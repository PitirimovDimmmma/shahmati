using System.Windows;
using shahmati.Helpers;
using shahmati.Views;

namespace shahmati
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Тестируем подключение к API
            bool isConnected = await DatabaseHelper.TestApiConnection();

            if (!isConnected)
            {
                MessageBox.Show("⚠️ Не удалось подключиться к API серверу\n\n" +
                               "Убедитесь, что:\n" +
                               "• API проект запущен\n" +
                               "• Адрес: http://localhost:7001\n" +
                               "• Сервер доступен",
                               "Ошибка подключения",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
            }

            // Показываем окно входа
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}