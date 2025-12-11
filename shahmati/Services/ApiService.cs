using shahmati.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace shahmati.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        // ИСПРАВЬТЕ: добавьте api/
        private const string BaseUrl = "https://localhost:7259/";

        public ApiService()
        {
            // Для разработки: игнорируем SSL ошибки
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (sender, cert, chain, sslPolicyErrors) => true;

            _httpClient = new HttpClient(handler);
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Добавьте логирование URL
            Console.WriteLine($"=== API SERVICE INIT ===");
            Console.WriteLine($"Base URL: {BaseUrl}");
            Console.WriteLine($"Full Auth URL: {new Uri(_httpClient.BaseAddress, "auth/register")}");
        }

        // ========== ТЕСТИРОВАНИЕ ПОДКЛЮЧЕНИЯ ==========
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Тестируем через weatherforecast
                var response = await _httpClient.GetAsync("weatherforecast");

                Console.WriteLine($"=== CONNECTION TEST ===");
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"URL: weatherforecast");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ API доступен");
                    return true;
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ API ошибка: {content}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        // ========== РЕГИСТРАЦИЯ - ИСПРАВЛЕННАЯ ВЕРСИЯ ==========
        public async Task<UserWithProfileDto> RegisterAsync(string username, string email, string password)
        {
            try
            {
                Console.WriteLine($"=== REGISTRATION START ===");
                Console.WriteLine($"Username: {username}");
                Console.WriteLine($"Email: {email}");
                Console.WriteLine($"Password length: {password?.Length}");

                // Создаем объект точно такой же как в curl
                var registerData = new
                {
                    username = username,  // ВНИМАНИЕ: строчные буквы как в curl!
                    email = email,
                    password = password
                };

                var json = JsonSerializer.Serialize(registerData);
                Console.WriteLine($"JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Отправляем запрос
                var response = await _httpClient.PostAsync("auth/register", content);

                var responseText = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseText}");

                if (response.IsSuccessStatusCode)
                {
                    // Парсим ответ
                    var user = JsonSerializer.Deserialize<UserWithProfileDto>(responseText);
                    Console.WriteLine($"✅ Регистрация успешна! UserId: {user?.Id}");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"✅ Регистрация успешна!\nID пользователя: {user?.Id}",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    });

                    return user;
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка сервера: {response.StatusCode}");

                    // Пытаемся распарсить ошибку
                    try
                    {
                        var errorDoc = JsonDocument.Parse(responseText);
                        if (errorDoc.RootElement.TryGetProperty("message", out var message))
                        {
                            var errorMessage = message.GetString();

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"❌ Ошибка регистрации: {errorMessage}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                    }
                    catch
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"❌ Ошибка регистрации: {responseText}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                    return null;
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"❌ HTTP Request Error: {httpEx.Message}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"❌ Ошибка подключения к серверу:\n{httpEx.Message}\n\n" +
                                   $"Убедитесь, что API запущен по адресу: {BaseUrl}",
                        "Ошибка сети", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"❌ Неожиданная ошибка:\n{ex.Message}",
                        "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return null;
            }
        }

        // ========== ЛОГИН ==========
        public async Task<UserWithProfileDto> LoginAsync(string username, string password)
        {
            try
            {
                Console.WriteLine($"=== LOGIN ATTEMPT ===");
                Console.WriteLine($"Username: {username}");

                // Также используем строчные буквы
                var loginData = new
                {
                    username = username,
                    password = password
                };

                var json = JsonSerializer.Serialize(loginData);
                Console.WriteLine($"Login JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("auth/login", content);

                var responseText = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Login Response Status: {response.StatusCode}");
                Console.WriteLine($"Login Response Body: {responseText}");

                if (response.IsSuccessStatusCode)
                {
                    var user = JsonSerializer.Deserialize<UserWithProfileDto>(responseText);
                    Console.WriteLine($"✅ Логин успешен! UserId: {user?.Id}");
                    return user;
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка логина: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Login error: {ex.Message}");
                return null;
            }
        }

        // ========== ПРОФИЛЬ ==========
        public async Task<UserProfileDto> GetProfileAsync(int userId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<UserProfileDto>($"auth/profile/{userId}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения профиля: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return null;
            }
        }

        public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"auth/profile/{userId}", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления профиля: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                return false;
            }
        }

        // ========== ИГРЫ ==========
        public async Task<List<GameDto>> GetActiveGamesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<GameDto>>("games");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении активных игр: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new List<GameDto>();
            }
        }

        public async Task<List<GameDto>> GetUserGamesAsync(int userId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<GameDto>>($"games/user/{userId}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении игр пользователя: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new List<GameDto>();
            }
        }

        public async Task<GameDto> GetGameAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<GameDto>($"games/{id}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении игры: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return null;
            }
        }

        public async Task<GameDto> CreateGameAsync(CreateGameDto createDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("games", createDto);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<GameDto>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка создания игры: {error}",
                                   "Ошибка",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании игры: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
            return null;
        }

        public async Task<GameDto> MakeMoveAsync(int gameId, MakeMoveRequest moveRequest)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"games/{gameId}/moves", moveRequest);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<GameDto>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка хода: {error}",
                                   "Ошибка",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении хода: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
            return null;
        }

        public async Task<bool> FinishGameAsync(int gameId, string result)
        {
            try
            {
                var finishData = new { Result = result };
                var response = await _httpClient.PutAsJsonAsync($"games/{gameId}/finish", finishData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при завершении игры: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                return false;
            }
        }

        public async Task<bool> DeleteGameAsync(int gameId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"games/{gameId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении игры: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                return false;
            }
        }

        // ========== СОХРАНЕННЫЕ ИГРЫ ==========
        public async Task<List<SavedGameDto>> GetSavedGamesAsync(int userId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<SavedGameDto>>($"savedgames/user/{userId}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении сохраненных игр: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new List<SavedGameDto>();
            }
        }

        public async Task<bool> SaveGameAsync(SaveGameRequest saveRequest)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("savedgames/save", saveRequest);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения игры: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                return false;
            }
        }

        public async Task<SavedGameDetailDto> GetSavedGameAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<SavedGameDetailDto>($"savedgames/{id}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки игры: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return null;
            }
        }

        public async Task<bool> UpdateSavedGameAsync(int id, UpdateSavedGameRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"savedgames/{id}", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления сохраненной игры: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                return false;
            }
        }

        public async Task<bool> DeleteSavedGameAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"savedgames/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления сохраненной игры: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                return false;
            }
        }

        // ========== ПОЛЬЗОВАТЕЛИ ==========
        public async Task<List<UserWithProfileDto>> GetUsersAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<UserWithProfileDto>>("users");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении пользователей: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new List<UserWithProfileDto>();
            }
        }

        public async Task<UserWithProfileDto> GetUserAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<UserWithProfileDto>($"users/{id}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении пользователя: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return null;
            }
        }

        public async Task<List<UserDto>> SearchUsersAsync(string username)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<UserDto>>($"users/search?username={username}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска пользователей: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new List<UserDto>();
            }
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateUserRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"users/{id}", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления пользователя: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                return false;
            }
        }

        // ========== СТАТИСТИКА ==========
        public async Task<GameStatsDto> GetUserStatsAsync(int userId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<GameStatsDto>($"stats/user/{userId}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения статистики: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return null;
            }
        }

        public async Task<List<PlayerStatsDto>> GetLeaderboardAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<PlayerStatsDto>>("stats/leaderboard");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения таблицы лидеров: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new List<PlayerStatsDto>();
            }
        }

        // ========== ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ ДЛЯ WPF ==========
        public async Task<GameStatsDto> CalculateUserStatsAsync(int userId)
        {
            try
            {
                // Получаем все игры пользователя
                var games = await GetUserGamesAsync(userId);
                if (games == null || games.Count == 0)
                {
                    return new GameStatsDto
                    {
                        TotalGames = 0,
                        Wins = 0,
                        Losses = 0,
                        Draws = 0,
                        CurrentRating = 1200,
                        HighestRating = 1200,
                        WinPercentage = 0
                    };
                }

                // Фильтруем завершенные игры
                var finishedGames = games.Where(g => g.IsFinished).ToList();

                // Подсчитываем статистику
                int wins = 0, losses = 0, draws = 0;

                foreach (var game in finishedGames)
                {
                    if (game.Result == "Draw")
                    {
                        draws++;
                    }
                    else if ((game.Result == "White" && game.WhitePlayer.Id == userId) ||
                             (game.Result == "Black" && game.BlackPlayer?.Id == userId))
                    {
                        wins++;
                    }
                    else
                    {
                        losses++;
                    }
                }

                // Получаем текущий рейтинг
                var profile = await GetProfileAsync(userId);
                int currentRating = profile?.Rating ?? 1200;

                int totalGames = finishedGames.Count;
                double winPercentage = totalGames > 0 ?
                    Math.Round((double)wins / totalGames * 100, 1) : 0;

                return new GameStatsDto
                {
                    TotalGames = totalGames,
                    Wins = wins,
                    Losses = losses,
                    Draws = draws,
                    CurrentRating = currentRating,
                    HighestRating = currentRating, // упрощенно
                    WinPercentage = winPercentage
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчета статистики: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new GameStatsDto
                {
                    TotalGames = 0,
                    Wins = 0,
                    Losses = 0,
                    Draws = 0,
                    CurrentRating = 1200,
                    HighestRating = 1200,
                    WinPercentage = 0
                };
            }
        }

        public async Task<List<PlayerStatsDto>> CalculateLeaderboardAsync()
        {
            try
            {
                // Получаем всех пользователей
                var users = await GetUsersAsync();
                if (users == null || users.Count == 0)
                    return new List<PlayerStatsDto>();

                var leaderboard = new List<PlayerStatsDto>();

                // Для каждого пользователя рассчитываем статистику
                foreach (var user in users)
                {
                    if (user.Profile == null) continue;

                    var stats = await CalculateUserStatsAsync(user.Id);

                    leaderboard.Add(new PlayerStatsDto
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        Rating = user.Profile.Rating,
                        GamesPlayed = stats.TotalGames,
                        Wins = stats.Wins,
                        WinRate = stats.WinPercentage
                    });
                }

                // Сортируем по рейтингу
                return leaderboard
                    .OrderByDescending(p => p.Rating)
                    .ThenByDescending(p => p.WinRate)
                    .Take(50)
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчета лидерборда: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new List<PlayerStatsDto>();
            }
        }
    }
}