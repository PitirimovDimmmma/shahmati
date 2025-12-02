using shahmati.Models;
using shahmati.Services;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace shahmati.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ApiService _apiService;

        public LoginWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();

            // Тестируем подключение при запуске
            Loaded += async (s, e) => await TestApiConnection();
        }

        private async Task TestApiConnection()
        {
            bool isConnected = await _apiService.TestConnectionAsync();
            if (!isConnected)
            {
                // ИЗМЕНИ ЭТО СООБЩЕНИЕ!
                MessageBox.Show("⚠️ Не удалось подключиться к серверу\n\n" +
                               "Убедитесь, что:\n" +
                               "1. API проект запущен\n" +
                               "2. Адрес: https://localhost:7259\n" +
                               "3. В браузере работает: https://localhost:7259/swagger",
                               "Ошибка подключения",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
            }
        }

        

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите имя пользователя и пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Content = "Вход...";

            try
            {
                var user = await _apiService.LoginAsync(username, password);

                if (user != null)
                {
                    // Сохраняем данные пользователя
                    Application.Current.Properties["UserId"] = user.Id;
                    Application.Current.Properties["Username"] = user.Username;
                    Application.Current.Properties["UserProfile"] = user.Profile;

                    // Открываем главное окно
                    DashboardWindow dashboard = new DashboardWindow(user.Id);
                    dashboard.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Неверные учетные данные. Проверьте логин и пароль.",
                        "Ошибка авторизации",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "Войти";
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow registrationWindow = new RegistrationWindow();
            registrationWindow.Show();
            this.Close();
        }

        private void UsernameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }
    }
}