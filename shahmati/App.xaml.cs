using System.Windows;
using shahmati.Helpers;
using shahmati.Views;

namespace shahmati
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Проверяем подключение к базе данных и создаем таблицы, если нужно
            DatabaseHelper.InitializeDatabase();

            // Показываем окно входа
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}