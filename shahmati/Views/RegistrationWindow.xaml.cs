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

            // Инициализируем кнопку
            RegisterButton.IsEnabled = false;
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            // Валидация
            if (!ValidateInputs(username, email, password, confirmPassword, showMessage: true))
            {
                return;
            }

            RegisterButton.IsEnabled = false;
            RegisterButton.Content = "Регистрация...";
            RegisterButton.Cursor = System.Windows.Input.Cursors.Wait;

            try
            {
                bool success = await _apiService.RegisterAsync(username, email, password);

                if (success)
                {
                    MessageBox.Show("✅ Аккаунт успешно создан!\nТеперь войдите в систему.",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Возвращаемся к окну входа
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка при регистрации: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                RegisterButton.IsEnabled = ValidateInputs(
                    UsernameTextBox.Text.Trim(),
                    EmailTextBox.Text.Trim(),
                    PasswordBox.Password,
                    ConfirmPasswordBox.Password,
                    showMessage: false);

                RegisterButton.Content = "Создать аккаунт";
                RegisterButton.Cursor = System.Windows.Input.Cursors.Hand;
            }
        }

        private bool ValidateInputs(string username, string email, string password,
                                   string confirmPassword, bool showMessage = false)
        {
            bool isValid = true;

            // Сбрасываем ошибки
            ClearErrors();

            // Проверка логина
            if (string.IsNullOrEmpty(username))
            {
                if (showMessage) ShowError(UsernameError, "Введите логин");
                isValid = false;
            }
            else if (username.Length < 3)
            {
                if (showMessage) ShowError(UsernameError, "Логин должен быть не менее 3 символов");
                isValid = false;
            }
            else if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
            {
                if (showMessage) ShowError(UsernameError, "Только латинские буквы, цифры и подчеркивание");
                isValid = false;
            }

            // Проверка email
            if (string.IsNullOrEmpty(email))
            {
                if (showMessage) ShowError(EmailError, "Введите email");
                isValid = false;
            }
            else if (!IsValidEmail(email))
            {
                if (showMessage) ShowError(EmailError, "Некорректный email адрес");
                isValid = false;
            }

            // Проверка пароля
            if (string.IsNullOrEmpty(password))
            {
                if (showMessage) ShowError(PasswordError, "Введите пароль");
                isValid = false;
            }
            else if (password.Length < 6)
            {
                if (showMessage) ShowError(PasswordError, "Пароль должен быть не менее 6 символов");
                isValid = false;
            }

            // Проверка подтверждения пароля
            if (string.IsNullOrEmpty(confirmPassword))
            {
                if (showMessage) ShowError(ConfirmPasswordError, "Подтвердите пароль");
                isValid = false;
            }
            else if (password != confirmPassword)
            {
                if (showMessage) ShowError(ConfirmPasswordError, "Пароли не совпадают");
                isValid = false;
            }

            return isValid;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            }
            catch
            {
                return false;
            }
        }

        private void ClearErrors()
        {
            UsernameError.Visibility = Visibility.Collapsed;
            EmailError.Visibility = Visibility.Collapsed;
            PasswordError.Visibility = Visibility.Collapsed;
            ConfirmPasswordError.Visibility = Visibility.Collapsed;
        }

        private void ShowError(TextBlock errorControl, string message)
        {
            errorControl.Text = message;
            errorControl.Visibility = Visibility.Visible;
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

            // Проверяем каждое поле отдельно с отображением ошибок
            bool usernameValid = !string.IsNullOrEmpty(username) && username.Length >= 3
                && Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");

            bool emailValid = !string.IsNullOrEmpty(email) && IsValidEmail(email);

            bool passwordValid = !string.IsNullOrEmpty(password) && password.Length >= 6;

            bool confirmValid = !string.IsNullOrEmpty(confirmPassword) && password == confirmPassword;

            // Показываем/скрываем ошибки
            UsernameError.Visibility = !usernameValid && !string.IsNullOrEmpty(username) ?
                Visibility.Visible : Visibility.Collapsed;

            EmailError.Visibility = !emailValid && !string.IsNullOrEmpty(email) ?
                Visibility.Visible : Visibility.Collapsed;

            PasswordError.Visibility = !passwordValid && !string.IsNullOrEmpty(password) ?
                Visibility.Visible : Visibility.Collapsed;

            ConfirmPasswordError.Visibility = !confirmValid && !string.IsNullOrEmpty(confirmPassword) ?
                Visibility.Visible : Visibility.Collapsed;

            // Включаем кнопку только если все поля валидны
            RegisterButton.IsEnabled = usernameValid && emailValid && passwordValid && confirmValid;
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