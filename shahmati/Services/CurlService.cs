using shahmati.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace shahmati.Services
{
    public class CurlService
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Универсальный метод для выполнения CURL запросов
        public static async Task<T> ExecuteCurlAsync<T>(string url, string method = "GET",
                                                        string jsonBody = null,
                                                        Dictionary<string, string> headers = null,
                                                        string operationName = "Операция")
        {
            try
            {
                // 1. Создаем временный файл для JSON
                string tempJsonFile = null;
                if (!string.IsNullOrEmpty(jsonBody))
                {
                    tempJsonFile = Path.GetTempFileName() + ".json";
                    await File.WriteAllTextAsync(tempJsonFile, jsonBody, Encoding.UTF8);
                }

                // 2. Формируем команду curl
                StringBuilder curlCmd = new StringBuilder();
                curlCmd.Append($"curl -X {method.ToUpper()} \"{url}\" ");

                // Добавляем заголовки
                curlCmd.Append("-H \"Content-Type: application/json\" ");
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        curlCmd.Append($"-H \"{header.Key}: {header.Value}\" ");
                    }
                }

                // Добавляем тело запроса
                if (tempJsonFile != null)
                {
                    curlCmd.Append($"--data-binary \"@{tempJsonFile}\" ");
                }

                // Опции безопасности и вывода
                curlCmd.Append("--insecure --silent --show-error");

                Console.WriteLine($"[CurlService] Выполняется: {operationName}");
                Console.WriteLine($"[CurlService] URL: {url}");
                Console.WriteLine($"[CurlService] Метод: {method}");

                // 3. Запускаем скрытый процесс
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {curlCmd}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    // Убираем временный файл
                    if (tempJsonFile != null)
                        File.Delete(tempJsonFile);

                    // 4. Обрабатываем результат
                    if (process.ExitCode == 0)
                    {
                        if (string.IsNullOrEmpty(output))
                        {
                            // Пустой ответ (например, для DELETE)
                            return default(T);
                        }

                        // Попробуем распарсить как JSON
                        try
                        {
                            var result = JsonSerializer.Deserialize<T>(output, JsonOptions);
                            Console.WriteLine($"[CurlService] {operationName}: УСПЕХ");
                            return result;
                        }
                        catch (JsonException)
                        {
                            // Если это не JSON, возвращаем сообщение
                            if (typeof(T) == typeof(string))
                                return (T)(object)output;

                            return default(T);
                        }
                    }
                    else
                    {
                        // Анализируем ошибку
                        string errorMsg = AnalyzeCurlError(error, output);

                        Console.WriteLine($"[CurlService] {operationName}: ОШИБКА - {errorMsg}");

                        // Показываем пользователю понятное сообщение
                        ShowErrorMessage(operationName, errorMsg);
                        return default(T);
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Исключение: {ex.Message}";
                Console.WriteLine($"[CurlService] {operationName}: {errorMsg}");
                ShowErrorMessage(operationName, errorMsg);
                return default(T);
            }
        }

        // Метод для анализа ошибок CURL
        private static string AnalyzeCurlError(string error, string output)
        {
            if (!string.IsNullOrEmpty(error))
            {
                if (error.Contains("Could not resolve host"))
                    return "Сервер недоступен. Проверьте подключение к интернету.";

                if (error.Contains("Connection refused"))
                    return "Сервер отказал в подключении. Убедитесь, что API запущен.";

                if (error.Contains("SSL certificate problem"))
                    return "Проблема с SSL сертификатом.";

                return error;
            }

            if (!string.IsNullOrEmpty(output))
            {
                if (output.Contains("\"status\":404") || output.Contains("404 Not Found"))
                    return "Ресурс не найден (404). Проверьте URL.";

                if (output.Contains("\"status\":403") || output.Contains("403 Forbidden"))
                    return "Доступ запрещен (403). Нет прав.";

                if (output.Contains("\"status\":401") || output.Contains("401 Unauthorized"))
                    return "Требуется авторизация (401).";

                if (output.Contains("\"status\":500") || output.Contains("500 Internal Server Error"))
                    return "Внутренняя ошибка сервера (500).";

                return output.Length > 200 ? output.Substring(0, 200) + "..." : output;
            }

            return "Неизвестная ошибка";
        }

        // Метод для отображения ошибок в UI
        private static void ShowErrorMessage(string operationName, string error)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                MessageBox.Show($"❌ {operationName}\n\nОшибка: {error}",
                    "Ошибка запроса",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
        }

        // ========== СПЕЦИАЛЬНЫЕ МЕТОДЫ ДЛЯ ПОЛЬЗОВАТЕЛЕЙ ==========

        // Исправленный метод получения пользователя
        public static async Task<UserWithProfileDto> GetUserByIdAsync(int userId)
        {
            // Пробуем разные варианты URL
            string[] possibleUrls = {
                $"https://localhost:7259/api/users/{userId}",
                $"https://localhost:7259/api/auth/profile/{userId}",
                $"https://localhost:7259/users/{userId}"
            };

            foreach (var url in possibleUrls)
            {
                try
                {
                    var result = await ExecuteCurlAsync<UserWithProfileDto>(
                        url,
                        "GET",
                        null,
                        null,
                        $"Получение пользователя ID={userId}"
                    );

                    if (result != null && result.Id > 0)
                    {
                        Console.WriteLine($"[CurlService] Пользователь найден по URL: {url}");
                        return result;
                    }
                }
                catch
                {
                    // Пробуем следующий URL
                    continue;
                }
            }

            // Если ни один URL не сработал
            Console.WriteLine($"[CurlService] Пользователь ID={userId} не найден ни по одному URL");
            return null;
        }

        // Тестовый метод проверки всех маршрутов
        public static async Task TestAllRoutesAsync()
        {
            Console.WriteLine("[CurlService] Тестирование маршрутов API...");

            string[] testRoutes = {
                "/api/users",
                "/api/users/1",
                "/api/auth/profile/1",
                "/api/admin/users",
                "/api/games",
                "/weatherforecast"
            };

            foreach (var route in testRoutes)
            {
                string url = $"https://localhost:7259{route}";
                Console.WriteLine($"\nТестируем: {url}");

                try
                {
                    var result = await ExecuteCurlAsync<string>(
                        url,
                        "GET",
                        null,
                        null,
                        $"Тест {route}"
                    );

                    if (string.IsNullOrEmpty(result) || result.Contains("error"))
                    {
                        Console.WriteLine($"❌ {route}: НЕ РАБОТАЕТ");
                    }
                    else
                    {
                        Console.WriteLine($"✅ {route}: РАБОТАЕТ");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ {route}: ОШИБКА - {ex.Message}");
                }
            }
        }
    }
}