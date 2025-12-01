using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using shahmati.Services;
using shahmati.Models;

namespace shahmati.Views
{
    public partial class RegistrationWindow : Window
    {
        private readonly ApiService _apiService;

        public RegistrationWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            // Валидация
            if (!ValidateInputs(username, email, password, confirmPassword))
            {
                return;
            }

            RegisterButton.IsEnabled = false;
            RegisterButton.Content = "Регистрация...";

            try
            {
                bool success = await _apiService.RegisterAsync(username, email, password);

                if (success)
                {
                    MessageBox.Show("Аккаунт успешно создан! Теперь войдите в систему.",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Возвращаемся к окну входа
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
                    this.Close();
                }
            }
            finally
            {
                RegisterButton.IsEnabled = true;
                RegisterButton.Content = "Создать аккаунт";
            }
        }

        private bool ValidateInputs(string username, string email, string password, string confirmPassword)
        {
            // Проверка логина
            if (string.IsNullOrEmpty(username) || username.Length < 3)
            {
                MessageBox.Show("Логин должен быть не менее 3 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
            {
                MessageBox.Show("Логин может содержать только латинские буквы, цифры и подчеркивание",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка email
            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                MessageBox.Show("Введите корректный email адрес", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка пароля
            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                MessageBox.Show("Пароль должен быть не менее 6 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка подтверждения пароля
            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void ValidateInputsInRealTime()
        {
            string username = UsernameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            bool isValid = !string.IsNullOrEmpty(username) && username.Length >= 3 &&
                          !string.IsNullOrEmpty(email) && IsValidEmail(email) &&
                          !string.IsNullOrEmpty(password) && password.Length >= 6 &&
                          password == confirmPassword;

            RegisterButton.IsEnabled = isValid;
        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInputsInRealTime();
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInputsInRealTime();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateInputsInRealTime();
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateInputsInRealTime();
        }
    }
}