using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace shahmati.Views
{
    public partial class RegistrationWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5436;Database=kursovoi;Username=postgres;Password=2005";

        public RegistrationWindow()
        {
            InitializeComponent();
        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInputs();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateInputs();
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateInputs();
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInputs();
        }

        private void ValidateInputs()
        {
            bool isValid = true;

            // Проверка логина
            string username = UsernameTextBox.Text;
            if (string.IsNullOrEmpty(username) || username.Length < 3)
            {
                UsernameError.Text = "Логин должен быть не менее 3 символов";
                UsernameError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else if (!Regex.IsMatch(username, @"^[a-zA-Z0-9]+$"))
            {
                UsernameError.Text = "Только латинские буквы и цифры";
                UsernameError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else if (IsUsernameExists(username))
            {
                UsernameError.Text = "Логин уже занят";
                UsernameError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                UsernameError.Visibility = Visibility.Collapsed;
            }

            // Проверка пароля
            string password = PasswordBox.Password;
            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                PasswordError.Text = "Пароль должен быть не менее 6 символов";
                PasswordError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                PasswordError.Visibility = Visibility.Collapsed;
            }

            // Проверка подтверждения пароля
            string confirmPassword = ConfirmPasswordBox.Password;
            if (password != confirmPassword)
            {
                ConfirmPasswordError.Text = "Пароли не совпадают";
                ConfirmPasswordError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                ConfirmPasswordError.Visibility = Visibility.Collapsed;
            }

            // Проверка email
            string email = EmailTextBox.Text;
            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                EmailError.Text = "Введите корректный email";
                EmailError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else if (IsEmailExists(email))
            {
                EmailError.Text = "Email уже используется";
                EmailError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                EmailError.Visibility = Visibility.Collapsed;
            }

            RegisterButton.IsEnabled = isValid;
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameTextBox.Text;
                string password = PasswordBox.Password;
                string email = EmailTextBox.Text;

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // Вставляем пользователя
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO users (username, email, password_hash) VALUES (@username, @email, @password_hash) RETURNING id",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("username", username);
                        cmd.Parameters.AddWithValue("email", email);
                        cmd.Parameters.AddWithValue("password_hash", HashPassword(password));

                        int userId = (int)await cmd.ExecuteScalarAsync();

                        MessageBox.Show("Аккаунт успешно создан!");

                        // Переходим к созданию профиля
                        ProfileSetupWindow profileWindow = new ProfileSetupWindow(userId);
                        profileWindow.Show();
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private bool IsUsernameExists(string username)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM users WHERE username = @username", conn))
                    {
                        cmd.Parameters.AddWithValue("username", username);
                        var result = cmd.ExecuteScalar();
                        return Convert.ToInt64(result) > 0;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool IsEmailExists(string email)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM users WHERE email = @email", conn))
                    {
                        cmd.Parameters.AddWithValue("email", email);
                        var result = cmd.ExecuteScalar();
                        return Convert.ToInt64(result) > 0;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}