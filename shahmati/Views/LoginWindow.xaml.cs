using shahmati.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace shahmati.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            // Тестируем подключение при запуске
            Loaded += async (s, e) => await TestApiConnection();
        }

        private async Task TestApiConnection()
        {
            try
            {
                // Проверяем через curl
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c curl https://localhost:7259/weatherforecast --insecure --silent",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Process process = new Process { StartInfo = psi };
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                if (process.ExitCode != 0 || output.Contains("404") || output.Contains("error"))
                {
                    MessageBox.Show("⚠️ API недоступен!\n\n" +
                                   "Убедитесь, что:\n" +
                                   "1. API проект запущен\n" +
                                   "2. Адрес: https://localhost:7259\n" +
                                   "3. В браузере работает: https://localhost:7259/swagger",
                                   "Ошибка подключения",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                }
            }
            catch
            {
                MessageBox.Show("⚠️ Не удалось проверить API\n\nУбедитесь, что curl установлен!",
                               "Ошибка",
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
                MessageBox.Show("Введите имя пользователя и пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Блокируем интерфейс
            LoginButton.IsEnabled = false;
            LoginButton.Content = "Вход через curl...";
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                // Выполняем логин через curl в фоне
                var user = await LoginWithCurlAsync(username, password);

                if (user != null)
                {
                    // Сохраняем данные пользователя
                    Application.Current.Properties["UserId"] = user.Id;
                    Application.Current.Properties["Username"] = user.Username;
                    Application.Current.Properties["UserProfile"] = user.Profile;

                    MessageBox.Show($"✅ Вход выполнен!\nДобро пожаловать, {user.Username}!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Восстанавливаем интерфейс
                LoginButton.IsEnabled = true;
                LoginButton.Content = "Войти";
                Mouse.OverrideCursor = null;
            }
        }

        private async Task<UserWithProfileDto> LoginWithCurlAsync(string username, string password)
        {
            try
            {
                // Создаем временный файл с JSON данными
                string jsonData = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
                string tempJsonFile = Path.GetTempFileName() + ".json";
                File.WriteAllText(tempJsonFile, jsonData, Encoding.UTF8);

                // Формируем команду curl
                string curlCommand = $"curl -X POST \"https://localhost:7259/api/auth/login\" " +
                                    $"-H \"Content-Type: application/json\" " +
                                    $"--data-binary \"@{tempJsonFile}\" " +
                                    $"--insecure --silent";

                // Создаем процесс который скрыт от пользователя
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {curlCommand}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Process process = new Process { StartInfo = psi };
                process.Start();

                // Читаем вывод
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Удаляем временный файл
                File.Delete(tempJsonFile);

                // Проверяем результат
                if (process.ExitCode == 0 && output.Contains("\"id\":"))
                {
                    try
                    {
                        // Парсим JSON ответ
                        var user = JsonSerializer.Deserialize<UserWithProfileDto>(output);
                        return user;
                    }
                    catch (JsonException)
                    {
                        MessageBox.Show($"Ошибка парсинга ответа: {output}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }
                }
                else
                {
                    string errorMessage = "Ошибка входа";
                    if (!string.IsNullOrEmpty(error))
                        errorMessage += $": {error}";
                    else if (!string.IsNullOrEmpty(output))
                        errorMessage += $": {output}";

                    MessageBox.Show(errorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении curl: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // Альтернатива: через PowerShell
        private async Task<UserWithProfileDto> LoginWithPowerShellAsync(string username, string password)
        {
            try
            {
                string json = $@"{{""username"":""{username}"",""password"":""{password}""}}";

                string psCommand = $@"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$uri = 'https://localhost:7259/api/auth/login'
$body = '{json}'
$headers = @{{'Content-Type' = 'application/json'}}

try {{
    $response = Invoke-RestMethod -Uri $uri -Method Post -Body $body -Headers $headers -SkipCertificateCheck
    $response | ConvertTo-Json -Compress
    exit 0
}} catch {{
    Write-Error $_.Exception.Message
    exit 1
}}";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psCommand}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Process process = new Process { StartInfo = psi };
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && output.Contains("\"id\":"))
                {
                    return JsonSerializer.Deserialize<UserWithProfileDto>(output);
                }
                else
                {
                    MessageBox.Show($"PowerShell error: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PowerShell error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // Быстрый вход через прямой вызов curl.exe
        private async Task<UserWithProfileDto> LoginDirectCurlAsync(string username, string password)
        {
            try
            {
                string json = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "curl.exe",
                    Arguments = $"-X POST \"https://localhost:7259/api/auth/login\" " +
                               $"-H \"Content-Type: application/json\" " +
                               $"-d \"{json.Replace("\"", "\\\"")}\" " +
                               $"--insecure --silent",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Process process = new Process { StartInfo = psi };
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && output.Contains("\"id\":"))
                {
                    return JsonSerializer.Deserialize<UserWithProfileDto>(output);
                }
                else
                {
                    string errorMsg = !string.IsNullOrEmpty(error) ? error : output;
                    MessageBox.Show($"CURL error: {errorMsg}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("curl не найден в системе!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

    }
}