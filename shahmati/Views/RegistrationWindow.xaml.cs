using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using shahmati.Models;

namespace shahmati.Views
{
    public partial class RegistrationWindow : Window
    {
        public RegistrationWindow()
        {
            InitializeComponent();
            RegisterButton.IsEnabled = false;
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (!ValidateInputs(username, email, password, confirmPassword, showMessage: true))
            {
                return;
            }

            RegisterButton.IsEnabled = false;
            RegisterButton.Content = "Регистрирую...";
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                // ЗАПУСКАЕМ CURL ДЛЯ РЕГИСТРАЦИИ
                var user = await RegisterWithCurlAsync(username, email, password);

                if (user != null && user.Id > 0)
                {
                    Console.WriteLine($"✅ Регистрация успешна! ID пользователя: {user.Id}");

                    MessageBox.Show($"✅ Регистрация успешна!\nЛогин: {user.Username}\nID: {user.Id}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // ВАЖНО: передаём РЕАЛЬНЫЙ ID пользователя!
                    DashboardWindow dashboardWindow = new DashboardWindow(user.Id); // user.Id = 18
                    dashboardWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("❌ Регистрация не удалась. Попробуйте другие данные.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RegisterButton.IsEnabled = true;
                RegisterButton.Content = "Создать аккаунт";
                Mouse.OverrideCursor = null;
            }
        }

        private async Task<UserWithProfileDto> RegisterWithCurlAsync(string username, string email, string password)
        {
            try
            {
                // Создаем JSON для регистрации
                string jsonData = JsonSerializer.Serialize(new
                {
                    username = username,
                    email = email,
                    password = password
                });

                string tempJsonFile = Path.GetTempFileName() + ".json";
                await File.WriteAllTextAsync(tempJsonFile, jsonData, Encoding.UTF8);

                // Команда curl для регистрации
                string curlCommand = $"curl -X POST \"https://localhost:7259/api/auth/register\" " +
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

                    Console.WriteLine($"CURL Response: {output}");

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        // Парсим ответ API
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        var user = JsonSerializer.Deserialize<UserWithProfileDto>(output, options);

                        if (user != null && user.Id > 0)
                        {
                            Console.WriteLine($"✅ Успешно получен пользователь ID={user.Id}");
                            return user;
                        }
                        else
                        {
                            Console.WriteLine($"❌ Не удалось получить ID пользователя из ответа");
                            return null;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Ошибка curl: {error}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка RegisterWithCurlAsync: {ex.Message}");
                return null;
            }
        }

        // Альтернатива - через HttpClient напрямую
        private async Task<UserWithProfileDto> RegisterWithHttpClientAsync(string username, string email, string password)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                // Отключаем проверку SSL для разработки
                var handler = new System.Net.Http.HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;

                using var httpClient = new System.Net.Http.HttpClient(handler);

                var requestData = new { username, email, password };
                var json = JsonSerializer.Serialize(requestData);
                var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("https://localhost:7259/api/auth/register", content);
                var responseText = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"HTTP Response: {responseText}");

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var user = JsonSerializer.Deserialize<UserWithProfileDto>(responseText, options);

                    if (user != null && user.Id > 0)
                    {
                        Console.WriteLine($"✅ HTTP: Получен пользователь ID={user.Id}");
                        return user;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HTTP ошибка: {ex.Message}");
                return null;
            }
        }

        private bool ValidateInputs(string username, string email, string password,
                                   string confirmPassword, bool showMessage = false)
        {
            if (string.IsNullOrEmpty(username) || username.Length < 3)
            {
                if (showMessage) MessageBox.Show("Логин должен быть не менее 3 символов", "Ошибка");
                return false;
            }

            if (string.IsNullOrEmpty(email) || !email.Contains("@") || !email.Contains("."))
            {
                if (showMessage) MessageBox.Show("Введите корректный email", "Ошибка");
                return false;
            }

            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                if (showMessage) MessageBox.Show("Пароль должен быть не менее 6 символов", "Ошибка");
                return false;
            }

            if (password != confirmPassword)
            {
                if (showMessage) MessageBox.Show("Пароли не совпадают", "Ошибка");
                return false;
            }

            return true;
        }

        private void ValidateInputsInRealTime()
        {
            string username = UsernameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            bool usernameValid = !string.IsNullOrEmpty(username) && username.Length >= 3;
            bool emailValid = !string.IsNullOrEmpty(email) && email.Contains("@") && email.Contains(".");
            bool passwordValid = !string.IsNullOrEmpty(password) && password.Length >= 6;
            bool confirmValid = password == confirmPassword && !string.IsNullOrEmpty(confirmPassword);

            RegisterButton.IsEnabled = usernameValid && emailValid && passwordValid && confirmValid;
        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e) => ValidateInputsInRealTime();
        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e) => ValidateInputsInRealTime();
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) => ValidateInputsInRealTime();
        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) => ValidateInputsInRealTime();

        private void UsernameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && RegisterButton.IsEnabled)
            {
                RegisterButton_Click(sender, e);
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && RegisterButton.IsEnabled)
            {
                RegisterButton_Click(sender, e);
            }
        }

        private void ConfirmPasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && RegisterButton.IsEnabled)
            {
                RegisterButton_Click(sender, e);
            }
        }

        private void EmailTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && RegisterButton.IsEnabled)
            {
                RegisterButton_Click(sender, e);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}