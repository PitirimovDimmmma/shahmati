using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using shahmati.Models;

namespace shahmati.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) => await TestApiConnection();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите имя пользователя и пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Content = "Вход";
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                var user = await LoginWithCurlAsync(username, password);

                if (user != null && user.Id > 0)
                {
                    Console.WriteLine($"✅ Успешный вход! UserId: {user.Id}");

                    // Сохраняем данные пользователя
                    Application.Current.Properties["UserId"] = user.Id;
                    Application.Current.Properties["Username"] = user.Username;
                    Application.Current.Properties["UserProfile"] = user.Profile;

                    MessageBox.Show($"✅ Вход выполнен!\nДобро пожаловать, {user.Username}!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // ПЕРЕДАЁМ РЕАЛЬНЫЙ ID пользователя!
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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "Войти";
                Mouse.OverrideCursor = null;
            }
        }

        private async Task<UserWithProfileDto> LoginWithCurlAsync(string username, string password)
        {
            try
            {
                string jsonData = JsonSerializer.Serialize(new { username, password });
                string tempJsonFile = Path.GetTempFileName() + ".json";
                await File.WriteAllTextAsync(tempJsonFile, jsonData, Encoding.UTF8);

                string curlCommand = $"curl -X POST \"https://localhost:7259/api/auth/login\" " +
                                    $"-H \"Content-Type: application/json\" " +
                                    $"--data-binary \"@{tempJsonFile}\" " +
                                    $"--insecure --silent";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {curlCommand}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    File.Delete(tempJsonFile);

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var user = JsonSerializer.Deserialize<UserWithProfileDto>(output, options);

                        if (user != null && user.Id > 0)
                        {
                            Console.WriteLine($"✅ CURL: Пользователь ID={user.Id} найден");
                            return user;
                        }
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка LoginWithCurlAsync: {ex.Message}");
                return null;
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

        private async Task TestApiConnection()
        {
            // ... код проверки подключения ...
        }
    }
}