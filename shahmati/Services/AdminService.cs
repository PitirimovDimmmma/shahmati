using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using shahmati.Models.Admin;

namespace shahmati.Services
{
    public class AdminService
    {
        private const string BaseUrl = "https://localhost:7259";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // ========== ПОЛУЧЕНИЕ ВСЕХ ПОЛЬЗОВАТЕЛЕЙ ==========
        public async Task<List<AdminUserDto>> GetAllUsersAsync(int adminId)
        {
            return await ExecuteCurlCommandAsync<List<AdminUserDto>>(
                $"{BaseUrl}/api/admin/users",
                "GET",
                adminId,
                "Получение списка пользователей"
            );
        }

        // ========== ПОЛУЧЕНИЕ СТАТИСТИКИ ==========
        public async Task<AdminStatsDto> GetAdminStatsAsync(int adminId)
        {
            return await ExecuteCurlCommandAsync<AdminStatsDto>(
                $"{BaseUrl}/api/admin/stats",
                "GET",
                adminId,
                "Получение статистики"
            );
        }

        // ========== ИЗМЕНЕНИЕ РОЛИ ПОЛЬЗОВАТЕЛЯ ==========
        public async Task<bool> UpdateUserRoleAsync(int adminId, int userId, UpdateUserRoleRequest request)
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            return await ExecuteCurlCommandAsync<bool>(
                $"{BaseUrl}/api/admin/users/{userId}/role",
                "PUT",
                adminId,
                "Изменение роли пользователя",
                json
            );
        }

        // ========== БЛОКИРОВКА/РАЗБЛОКИРОВКА ==========
        public async Task<bool> ToggleBlockUserAsync(int adminId, int userId)
        {
            return await ExecuteCurlCommandAsync<bool>(
                $"{BaseUrl}/api/admin/users/{userId}/block",
                "PUT",
                adminId,
                "Блокировка пользователя"
            );
        }

        // ========== УДАЛЕНИЕ ПОЛЬЗОВАТЕЛЯ ==========
        public async Task<bool> DeleteUserAsync(int adminId, int userId)
        {
            return await ExecuteCurlCommandAsync<bool>(
                $"{BaseUrl}/api/admin/users/{userId}",
                "DELETE",
                adminId,
                "Удаление пользователя"
            );
        }

        // ========== ОСНОВНОЙ МЕТОД ДЛЯ ВЫПОЛНЕНИЯ CURL ==========
        private async Task<T> ExecuteCurlCommandAsync<T>(string url, string method, int adminId,
                                                        string operationName, string jsonBody = null)
        {
            try
            {
                // Создаем временный файл для JSON (если есть тело)
                string tempJsonFile = null;
                if (!string.IsNullOrEmpty(jsonBody))
                {
                    tempJsonFile = Path.GetTempFileName() + ".json";
                    await File.WriteAllTextAsync(tempJsonFile, jsonBody, Encoding.UTF8);
                }

                // Формируем команду curl
                string curlCommand = $"curl -X {method.ToUpper()} \"{url}\" " +
                                    $"-H \"Content-Type: application/json\" " +
                                    $"-H \"X-User-Id: {adminId}\" " +
                                    (tempJsonFile != null ? $"--data-binary \"@{tempJsonFile}\" " : "") +
                                    $"--insecure --silent";

                // Создаем скрытый процесс
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {curlCommand}",
                    UseShellExecute = false,
                    CreateNoWindow = true,                     // СКРЫТЫЙ ОТ ПОЛЬЗОВАТЕЛЯ
                    WindowStyle = ProcessWindowStyle.Hidden,   // СКРЫТЫЙ ОТ ПОЛЬЗОВАТЕЛЯ
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                Console.WriteLine($"[AdminService] Выполняется: {operationName}");

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();

                    // Асинхронно читаем вывод
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    // Удаляем временный файл
                    if (tempJsonFile != null && File.Exists(tempJsonFile))
                        File.Delete(tempJsonFile);

                    // Проверяем результат
                    if (process.ExitCode == 0)
                    {
                        if (!string.IsNullOrEmpty(output))
                        {
                            try
                            {
                                // Парсим JSON ответ
                                var result = JsonSerializer.Deserialize<T>(output, JsonOptions);
                                Console.WriteLine($"[AdminService] {operationName}: УСПЕХ");
                                return result;
                            }
                            catch (JsonException)
                            {
                                // Если ответ не JSON, но успешен (например, просто сообщение)
                                Console.WriteLine($"[AdminService] {operationName}: Ответ не JSON: {output}");

                                // Для bool операций возвращаем true
                                if (typeof(T) == typeof(bool))
                                    return (T)(object)true;

                                // Для других типов пытаемся создать пустой объект
                                return default(T);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[AdminService] {operationName}: Пустой ответ");
                            return default(T);
                        }
                    }
                    else
                    {
                        string errorMsg = !string.IsNullOrEmpty(error) ? error : output;

                        // Проверяем тип ошибки
                        if (errorMsg.Contains("403") || errorMsg.Contains("Forbidden"))
                        {
                            ShowMessageInUI("❌ У вас нет прав администратора");
                        }
                        else if (errorMsg.Contains("404"))
                        {
                            ShowMessageInUI($"❌ Пользователь не найден");
                        }
                        else if (errorMsg.Contains("500"))
                        {
                            ShowMessageInUI("❌ Ошибка сервера");
                        }
                        else
                        {
                            ShowMessageInUI($"❌ {operationName}: {errorMsg}");
                        }

                        Console.WriteLine($"[AdminService] {operationName}: ОШИБКА - {errorMsg}");
                        return default(T);
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Ошибка при {operationName}: {ex.Message}";
                ShowMessageInUI($"❌ {errorMessage}");
                Console.WriteLine($"[AdminService] Исключение: {ex.Message}");
                return default(T);
            }
        }

        // ========== ПОМОЩНИК ДЛЯ ПОКАЗА СООБЩЕНИЙ В UI ==========
        private void ShowMessageInUI(string message)
        {
            // Вызываем в UI потоке
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                MessageBox.Show(message, "Админ-панель",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }
}