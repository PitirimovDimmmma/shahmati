using System;
using System.Windows;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace shahmati.Views
{
    public partial class LoginWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5436;Database=kursovoi;Username=postgres;Password=2005";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните все поля");
                return;
            }

            if (AuthenticateUser(username, password))
            {
                // Получаем ID пользователя
                int userId = GetUserId(username);

                // Проверяем, есть ли профиль
                if (!HasProfile(userId))
                {
                    // Переходим к созданию профиля
                    ProfileSetupWindow profileWindow = new ProfileSetupWindow(userId);
                    profileWindow.Show();
                }
                else
                {
                    // ИСПРАВЛЕНИЕ: Переходим к ГЛАВНОМУ ЭКРАНУ (Dashboard)
                    DashboardWindow dashboardWindow = new DashboardWindow(userId);
                    dashboardWindow.Show();
                }
                this.Close();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль");
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow registrationWindow = new RegistrationWindow();
            registrationWindow.Show();
            this.Close();
        }

        private bool AuthenticateUser(string username, string password)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT password_hash FROM users WHERE username = @username", conn))
                    {
                        cmd.Parameters.AddWithValue("username", username);
                        var result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            string storedHash = result.ToString();
                            string inputHash = HashPassword(password);
                            return storedHash == inputHash;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}");
            }
            return false;
        }

        private int GetUserId(string username)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT id FROM users WHERE username = @username", conn))
                    {
                        cmd.Parameters.AddWithValue("username", username);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private bool HasProfile(int userId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM profiles WHERE user_id = @user_id", conn))
                    {
                        cmd.Parameters.AddWithValue("user_id", userId);
                        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
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