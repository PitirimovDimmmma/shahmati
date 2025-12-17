using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
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
                // Пробуем оба метода регистрации
                var user = await RegisterWithCurlAsync(username, email, password);

                // Если curl не сработал, пробуем HttpClient
                if (user == null || user.Id <= 0)
                {
                    Console.WriteLine("🔄 CURL не сработал, пробуем HttpClient...");
                    user = await RegisterWithHttpClientAsync(username, email, password);
                }

                if (user != null && user.Id > 0)
                {
                    Console.WriteLine($"✅ Регистрация успешна! ID пользователя: {user.Id}");

                    MessageBox.Show($"✅ Регистрация успешна!\nЛогин: {user.Username}\nЗавершите настройку профиля",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // ПЕРЕХОДИМ НА ОКНО НАСТРОЙКИ ПРОФИЛЯ
                    ProfileSetupWindow profileSetupWindow = new ProfileSetupWindow(user.Id);
                    profileSetupWindow.Show();
                    this.Close();
                }
                else
                {
                    string errorMessage = "❌ Регистрация не удалась.\n\nВозможные причины:\n";
                    errorMessage += "1. Логин или email уже используются\n";
                    errorMessage += "2. Проблема с подключением к серверу\n";
                    errorMessage += "3. Неверные данные\n\n";
                    errorMessage += "Попробуйте другие данные или проверьте подключение.";

                    MessageBox.Show(errorMessage,
                        "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка при регистрации:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"❌ Exception details: {ex}");
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

                Console.WriteLine($"📤 Отправляемые данные: {jsonData}");

                string tempJsonFile = Path.GetTempFileName() + ".json";
                await File.WriteAllTextAsync(tempJsonFile, jsonData, Encoding.UTF8);

                // Команда curl для регистрации
                string curlCommand = $"curl -X POST \"https://localhost:7259/api/auth/register\" " +
                                    $"-H \"Content-Type: application/json\" " +
                                    $"--data-binary \"@{tempJsonFile}\" " +
                                    $"--insecure --silent --show-error";

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
                    Console.WriteLine($"🚀 Запуск curl команды...");
                    process.Start();

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    File.Delete(tempJsonFile);

                    Console.WriteLine($"=== CURL RESPONSE ===");
                    Console.WriteLine($"Output: {output}");
                    Console.WriteLine($"Error: {error}");
                    Console.WriteLine($"Exit Code: {process.ExitCode}");
                    Console.WriteLine($"=====================");

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        try
                        {
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
                                Console.WriteLine($"❌ Не удалось десериализовать пользователя");
                                return null;
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            Console.WriteLine($"❌ Ошибка парсинга JSON: {jsonEx.Message}");
                            Console.WriteLine($"Raw response: {output}");
                            return null;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Ошибка curl. ExitCode={process.ExitCode}");
                        if (!string.IsNullOrEmpty(error))
                        {
                            Console.WriteLine($"Curl error: {error}");
                        }
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка RegisterWithCurlAsync: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        private async Task<UserWithProfileDto> RegisterWithHttpClientAsync(string username, string email, string password)
        {
            try
            {
                using var handler = new System.Net.Http.HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;

                using var httpClient = new System.Net.Http.HttpClient(handler);
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var requestData = new { username, email, password };
                var json = JsonSerializer.Serialize(requestData);
                var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"📤 HTTP отправка на https://localhost:7259/api/auth/register");
                var response = await httpClient.PostAsync("https://localhost:7259/api/auth/register", content);

                Console.WriteLine($"📥 HTTP статус: {response.StatusCode}");
                var responseText = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"📥 HTTP ответ: {responseText}");

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
                else
                {
                    Console.WriteLine($"❌ HTTP ошибка: {response.StatusCode}");
                    Console.WriteLine($"Подробности: {responseText}");

                    // Пытаемся получить более детальную информацию об ошибке
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<JsonElement>(responseText);
                        if (errorObj.TryGetProperty("errors", out var errors))
                        {
                            Console.WriteLine($"Ошибки валидации: {errors}");
                        }
                        if (errorObj.TryGetProperty("title", out var title))
                        {
                            Console.WriteLine($"Заголовок ошибки: {title}");
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки парсинга
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HTTP ошибка: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        private bool ValidateInputs(string username, string email, string password,
                                   string confirmPassword, bool showMessage = false)
        {
            if (string.IsNullOrEmpty(username) || username.Length < 3)
            {
                if (showMessage)
                    MessageBox.Show("Логин должен быть не менее 3 символов", "Ошибка");
                return false;
            }

            if (string.IsNullOrEmpty(email) || !email.Contains("@") || !email.Contains("."))
            {
                if (showMessage)
                    MessageBox.Show("Введите корректный email", "Ошибка");
                return false;
            }

            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                if (showMessage)
                    MessageBox.Show("Пароль должен быть не менее 6 символов", "Ошибка");
                return false;
            }

            if (password != confirmPassword)
            {
                if (showMessage)
                    MessageBox.Show("Пароли не совпадают", "Ошибка");
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