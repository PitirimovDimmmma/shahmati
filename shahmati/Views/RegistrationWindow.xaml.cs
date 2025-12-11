using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

            // Валидация
            if (!ValidateInputs(username, email, password, confirmPassword, showMessage: true))
            {
                return;
            }

            // Блокируем интерфейс
            RegisterButton.IsEnabled = false;
            RegisterButton.Content = "Регистрирую...";
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                // Запускаем curl в фоне
                bool success = await ExecuteCurlInBackgroundAsync(username, email, password);

                if (success)
                {
                    MessageBox.Show($"✅ Молодец зарегистрировались!\nЛогин: {username}\nМожешь теперь войти!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Переходим на логин
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
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
                // Восстанавливаем интерфейс
                RegisterButton.IsEnabled = true;
                RegisterButton.Content = "Создать аккаунт";
                Mouse.OverrideCursor = null;
            }
        }

        private async Task<bool> ExecuteCurlInBackgroundAsync(string username, string email, string password)
        {
            try
            {
                // Создаем временный файл с JSON данными
                string jsonData = $"{{\"username\":\"{username}\",\"email\":\"{email}\",\"password\":\"{password}\"}}";
                string tempJsonFile = Path.GetTempFileName() + ".json";
                File.WriteAllText(tempJsonFile, jsonData, Encoding.UTF8);

                // Формируем команду curl
                string curlCommand = $"curl -X POST \"https://localhost:7259/api/auth/register\" " +
                                    $"-H \"Content-Type: application/json\" " +
                                    $"--data-binary \"@{tempJsonFile}\" " +
                                    $"--insecure";

                // Создаем процесс который СКРЫТ от пользователя
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {curlCommand}",
                    UseShellExecute = false,          // НЕ показывать окно
                    CreateNoWindow = true,            // Скрыть окно
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,    // Перенаправляем вывод
                    RedirectStandardError = true,     // Перенаправляем ошибки
                };

                Process process = new Process { StartInfo = psi };
                process.Start();

                // Ждем завершения и читаем результат
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Удаляем временный файл
                File.Delete(tempJsonFile);

                // Показываем результат в MessageBox
                if (process.ExitCode == 0 && output.Contains("\"id\":"))
                {
                    return true; // Успех!
                }
                else
                {
                    // Формируем сообщение об ошибке
                    string errorMessage = "Ошибка регистрации";

                    if (!string.IsNullOrEmpty(error))
                        errorMessage += $": {error}";
                    else if (!string.IsNullOrEmpty(output))
                        errorMessage += $": {output}";

                    MessageBox.Show(errorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении curl: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // Альтернатива: через PowerShell в фоне
        private async Task<bool> ExecutePowerShellInBackgroundAsync(string username, string email, string password)
        {
            try
            {
                string json = $@"{{""username"":""{username}"",""email"":""{email}"",""password"":""{password}""}}";

                string psCommand = $@"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$uri = 'https://localhost:7259/api/auth/register'
$body = '{json}'
$headers = @{{'Content-Type' = 'application/json'}}

try {{
    $response = Invoke-RestMethod -Uri $uri -Method Post -Body $body -Headers $headers -SkipCertificateCheck
    Write-Output 'SUCCESS'
    Write-Output ($response | ConvertTo-Json)
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

                if (process.ExitCode == 0 && output.Contains("SUCCESS"))
                {
                    return true;
                }
                else
                {
                    MessageBox.Show($"PowerShell error: {output}\n{error}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PowerShell error: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // Самый простой вариант - напрямую запускаем curl.exe
        private async Task<bool> ExecuteCurlDirectAsync(string username, string email, string password)
        {
            try
            {
                string json = $"{{\"username\":\"{username}\",\"email\":\"{email}\",\"password\":\"{password}\"}}";

                // Прямой вызов curl.exe
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "curl.exe",
                    Arguments = $"-X POST \"https://localhost:7259/api/auth/register\" " +
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
                    return true;
                }
                else
                {
                    string errorMsg = !string.IsNullOrEmpty(error) ? error : output;
                    MessageBox.Show($"CURL error: {errorMsg}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // curl не найден
                MessageBox.Show("curl не найден в системе!\n\n" +
                               "Установите curl одним из способов:\n" +
                               "1. Скачайте с https://curl.se/windows/\n" +
                               "2. Через Chocolatey: choco install curl\n" +
                               "3. Через Winget: winget install curl.curl",
                               "CURL not found", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool ValidateInputs(string username, string email, string password,
                                   string confirmPassword, bool showMessage = false)
        {
            // Простая валидация
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