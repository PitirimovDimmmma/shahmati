using System;
using System.Windows;
using System.Threading.Tasks;
using shahmati.Views;

namespace shahmati
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Просто открываем окно входа БЕЗ тестирования
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}