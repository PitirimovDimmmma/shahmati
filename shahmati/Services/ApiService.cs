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
        private readonly int _userId; // Добавляем поле
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

            Console.WriteLine($"=== API SERVICE INIT ===");
            Console.WriteLine($"Base URL: {BaseUrl}");
        }



        // ========== ТЕСТИРОВАНИЕ ПОДКЛЮЧЕНИЯ ==========
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("weatherforecast");
                Console.WriteLine($"=== CONNECTION TEST ===");
                Console.WriteLine($"Status: {response.StatusCode}");

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

        // ========== ПОЛЬЗОВАТЕЛИ ==========
        public async Task<List<UserWithProfileDto>> GetUsersAsync()
        {
            try
            {
                Console.WriteLine($"=== GET ALL USERS ===");
                return await _httpClient.GetFromJsonAsync<List<UserWithProfileDto>>("api/users");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения пользователей: {ex.Message}");
                MessageBox.Show($"Ошибка при получении пользователей: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new List<UserWithProfileDto>();
            }
            
        }

        public async Task<UserWithProfileDto> GetUserAsync(int id)
        {
            try
            {
                Console.WriteLine($"=== GET USER ID={id} ===");
                return await _httpClient.GetFromJsonAsync<UserWithProfileDto>($"api/users/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения пользователя: {ex.Message}");
                MessageBox.Show($"Ошибка получения пользователя: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        public async Task<List<UserDto>> SearchUsersAsync(string username)
        {
            try
            {
                Console.WriteLine($"=== SEARCH USERS: {username} ===");
                return await _httpClient.GetFromJsonAsync<List<UserDto>>($"api/users/search?username={username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка поиска пользователей: {ex.Message}");
                MessageBox.Show($"Ошибка поиска пользователей: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new List<UserDto>();
            }
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateUserRequest request)
        {
            try
            {
                Console.WriteLine($"=== UPDATE USER ID={id} ===");
                var response = await _httpClient.PutAsJsonAsync($"api/users/{id}", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления пользователя: {ex.Message}");
                MessageBox.Show($"Ошибка обновления пользователя: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // ========== РЕГИСТРАЦИЯ ==========
        public async Task<UserWithProfileDto> RegisterAsync(string username, string email, string password)
        {
            try
            {
                Console.WriteLine($"=== REGISTRATION START ===");
                Console.WriteLine($"Username: {username}");
                Console.WriteLine($"Email: {email}");
                Console.WriteLine($"Password length: {password?.Length}");

                var registerData = new
                {
                    username = username,
                    email = email,
                    password = password
                };

                var json = JsonSerializer.Serialize(registerData);
                Console.WriteLine($"JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/auth/register", content);

                var responseText = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseText}");

                if (response.IsSuccessStatusCode)
                {
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

                var loginData = new
                {
                    username = username,
                    password = password
                };

                var json = JsonSerializer.Serialize(loginData);
                Console.WriteLine($"Login JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/auth/login", content);

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

        // ===== НОВЫЕ МЕТОДЫ ДЛЯ СТАТИСТИКИ =====

        public async Task<ExtendedGameStatsDto> GetDetailedUserStatsAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/extended-stats/user/{userId}/detailed");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ExtendedGameStatsDto>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения детальной статистики: {ex.Message}");
                return null;
            }
        }

        public async Task<GameStatsDto> GetFilteredUserStatsAsync(int userId, FilterStatsRequest filter)
        {
            try
            {
                var jsonFilter = JsonSerializer.Serialize(filter);
                var content = new StringContent(jsonFilter, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"api/extended-stats/user/{userId}/filtered", content);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<GameStatsDto>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения фильтрованной статистики: {ex.Message}");
                return null;
            }
        }

        public async Task<List<PlayerRatingDto>> GetLeaderboardAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/extended-stats/leaderboard");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<PlayerRatingDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                return new List<PlayerRatingDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения таблицы лидеров: {ex.Message}");
                return new List<PlayerRatingDto>();
            }
        }

        public async Task<List<RatingHistoryDto>> GetUserRatingHistoryAsync(int userId, int limit = 20)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/extended-stats/user/{userId}/rating-history?limit={limit}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<RatingHistoryDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                return new List<RatingHistoryDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения истории рейтинга: {ex.Message}");
                return new List<RatingHistoryDto>();
            }
        }

        // ========== ПРОФИЛЬ ==========
        public async Task<UserProfileDto> GetProfileAsync(int userId)
        {
            try
            {
                Console.WriteLine($"=== GET PROFILE ID={userId} ===");
                return await _httpClient.GetFromJsonAsync<UserProfileDto>($"api/auth/profile/{userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения профиля: {ex.Message}");
                MessageBox.Show($"Ошибка получения профиля: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            try
            {
                Console.WriteLine($"=== UPDATE PROFILE ID={userId} ===");
                var response = await _httpClient.PutAsJsonAsync($"api/auth/profile/{userId}", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления профиля: {ex.Message}");
                MessageBox.Show($"Ошибка обновления профиля: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // ========== ИГРЫ ==========
        public async Task<List<GameDto>> GetActiveGamesAsync()
        {
            try
            {
                Console.WriteLine($"=== GET ACTIVE GAMES ===");
                return await _httpClient.GetFromJsonAsync<List<GameDto>>("api/games");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения активных игр: {ex.Message}");
                MessageBox.Show($"Ошибка при получении активных игр: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new List<GameDto>();
            }
        }

        public async Task<List<GameDto>> GetUserGamesAsync(int userId)
        {
            try
            {
                Console.WriteLine($"=== GET USER GAMES ID={userId} ===");
                return await _httpClient.GetFromJsonAsync<List<GameDto>>($"api/games/user/{userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения игр пользователя: {ex.Message}");
                MessageBox.Show($"Ошибка при получении игр пользователя: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new List<GameDto>();
            }
        }

        public async Task<GameDto> GetGameAsync(int id)
        {
            try
            {
                Console.WriteLine($"=== GET GAME ID={id} ===");
                return await _httpClient.GetFromJsonAsync<GameDto>($"api/games/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения игры: {ex.Message}");
                MessageBox.Show($"Ошибка при получении игры: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        public async Task<GameDto> CreateGameAsync(CreateGameDto createDto)
        {
            try
            {
                Console.WriteLine($"=== CREATE GAME ===");
                var response = await _httpClient.PostAsJsonAsync("api/games", createDto);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<GameDto>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Ошибка создания игры: {error}");
                    MessageBox.Show($"Ошибка создания игры: {error}",
                                   "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при создании игры: {ex.Message}");
                MessageBox.Show($"Ошибка при создании игры: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        public async Task<GameDto> MakeMoveAsync(int gameId, MakeMoveRequest moveRequest)
        {
            try
            {
                Console.WriteLine($"=== MAKE MOVE GAME ID={gameId} ===");
                var response = await _httpClient.PostAsJsonAsync($"api/games/{gameId}/moves", moveRequest);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<GameDto>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Ошибка хода: {error}");
                    MessageBox.Show($"Ошибка хода: {error}",
                                   "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при выполнении хода: {ex.Message}");
                MessageBox.Show($"Ошибка при выполнении хода: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        public async Task<bool> FinishGameAsync(int gameId, string result)
        {
            try
            {
                Console.WriteLine($"=== FINISH GAME ID={gameId} ===");
                var finishData = new { Result = result };
                var response = await _httpClient.PutAsJsonAsync($"api/games/{gameId}/finish", finishData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при завершении игры: {ex.Message}");
                MessageBox.Show($"Ошибка при завершении игры: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public async Task<bool> DeleteGameAsync(int gameId)
        {
            try
            {
                Console.WriteLine($"=== DELETE GAME ID={gameId} ===");
                var response = await _httpClient.DeleteAsync($"api/games/{gameId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при удалении игры: {ex.Message}");
                MessageBox.Show($"Ошибка при удалении игры: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // ========== СОХРАНЕННЫЕ ИГРЫ ==========
        public async Task<List<SavedGameDto>> GetSavedGamesAsync(int userId)
        {
            try
            {
                Console.WriteLine($"=== GET SAVED GAMES USER ID={userId} ===");
                return await _httpClient.GetFromJsonAsync<List<SavedGameDto>>($"api/savedgames/user/{userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при получении сохраненных игр: {ex.Message}");
                MessageBox.Show($"Ошибка при получении сохраненных игр: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new List<SavedGameDto>();
            }
        }

        public async Task<bool> SaveGameAsync(SaveGameRequest saveRequest)
        {
            try
            {
                Console.WriteLine($"=== SAVE GAME ===");
                var response = await _httpClient.PostAsJsonAsync("api/savedgames/save", saveRequest);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка сохранения игры: {ex.Message}");
                MessageBox.Show($"Ошибка сохранения игры: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public async Task<SavedGameDetailDto> GetSavedGameAsync(int id)
        {
            try
            {
                Console.WriteLine($"=== GET SAVED GAME ID={id} ===");
                return await _httpClient.GetFromJsonAsync<SavedGameDetailDto>($"api/savedgames/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки игры: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки игры: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        public async Task<bool> UpdateSavedGameAsync(int id, UpdateSavedGameRequest request)
        {
            try
            {
                Console.WriteLine($"=== UPDATE SAVED GAME ID={id} ===");
                var response = await _httpClient.PutAsJsonAsync($"api/savedgames/{id}", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления сохраненной игры: {ex.Message}");
                MessageBox.Show($"Ошибка обновления сохраненной игры: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public async Task<bool> DeleteSavedGameAsync(int id)
        {
            try
            {
                Console.WriteLine($"=== DELETE SAVED GAME ID={id} ===");
                var response = await _httpClient.DeleteAsync($"api/savedgames/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка удаления сохраненной игры: {ex.Message}");
                MessageBox.Show($"Ошибка удаления сохраненной игры: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // ========== ТРЕНИРОВКИ ==========
        public async Task<List<TrainingTypeDto>> GetTrainingTypesAsync()
        {
            try
            {
                Console.WriteLine($"=== GET TRAINING TYPES ===");
                return await _httpClient.GetFromJsonAsync<List<TrainingTypeDto>>("api/training/types");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при получении тренировок: {ex.Message}");
                MessageBox.Show($"Ошибка при получении тренировок: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new List<TrainingTypeDto>();
            }
        }

        public async Task<List<TrainingPositionDto>> GetTrainingPositionsAsync(int trainingTypeId)
        {
            try
            {
                Console.WriteLine($"=== GET TRAINING POSITIONS TYPE ID={trainingTypeId} ===");
                return await _httpClient.GetFromJsonAsync<List<TrainingPositionDto>>($"api/training/{trainingTypeId}/positions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при получении позиций: {ex.Message}");
                MessageBox.Show($"Ошибка при получении позиций: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new List<TrainingPositionDto>();
            }
        }

        public async Task<List<TrainingProgressDto>> GetUserTrainingProgressAsync(int userId)
        {
            try
            {
                Console.WriteLine($"=== GET TRAINING PROGRESS USER ID={userId} ===");
                return await _httpClient.GetFromJsonAsync<List<TrainingProgressDto>>($"api/training/progress/{userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при получении прогресса: {ex.Message}");
                MessageBox.Show($"Ошибка при получении прогресса: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new List<TrainingProgressDto>();
            }
        }

        public async Task<bool> StartTrainingAsync(StartTrainingRequest request)
        {
            try
            {
                Console.WriteLine($"=== START TRAINING USER ID={request.UserId} TYPE ID={request.TrainingTypeId} ===");
                var response = await _httpClient.PostAsJsonAsync("api/training/start", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка начала тренировки: {ex.Message}");
                MessageBox.Show($"Ошибка начала тренировки: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public async Task<bool> CompleteTrainingAsync(CompleteTrainingRequest request)
        {
            try
            {
                Console.WriteLine($"=== COMPLETE TRAINING USER ID={request.UserId} TYPE ID={request.TrainingTypeId} ===");
                var response = await _httpClient.PostAsJsonAsync("api/training/complete", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка завершения тренировки: {ex.Message}");
                MessageBox.Show($"Ошибка завершения тренировки: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public async Task<TrainingStatsDto> GetTrainingStatsAsync(int userId)
        {
            try
            {
                Console.WriteLine($"=== GET TRAINING STATS USER ID={userId} ===");
                return await _httpClient.GetFromJsonAsync<TrainingStatsDto>($"api/training/stats/{userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения статистики: {ex.Message}");
                MessageBox.Show($"Ошибка получения статистики: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new TrainingStatsDto();
            }
        }

        public async Task<List<TrainingTypeDto>> SearchTrainingsAsync(string query)
        {
            try
            {
                Console.WriteLine($"=== SEARCH TRAININGS: {query} ===");
                return await _httpClient.GetFromJsonAsync<List<TrainingTypeDto>>($"api/training/search?query={query}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка поиска тренировок: {ex.Message}");
                MessageBox.Show($"Ошибка поиска тренировок: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new List<TrainingTypeDto>();
            }
        }






        // ========== СТАТИСТИКА ==========
        public async Task<GameStatsDto> GetUserStatsAsync(int userId)
        {
            try
            {
                Console.WriteLine($"=== GET USER STATS ID={userId} ===");
                return await _httpClient.GetFromJsonAsync<GameStatsDto>($"api/stats/user/{userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения статистики: {ex.Message}");
                MessageBox.Show($"Ошибка получения статистики: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        public async Task<List<PlayerStatsDto>> GetLeaderboardOldAsync()
        {
            try
            {
                Console.WriteLine($"=== GET LEADERBOARD ===");
                return await _httpClient.GetFromJsonAsync<List<PlayerStatsDto>>("api/stats/leaderboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения таблицы лидеров: {ex.Message}");
                MessageBox.Show($"Ошибка получения таблицы лидеров: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new List<PlayerStatsDto>();
            }
        }

        // ========== ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ ДЛЯ WPF ==========
        public async Task<GameStatsDto> CalculateUserStatsAsync(int userId)
        {
            try
            {
                Console.WriteLine($"=== CALCULATE USER STATS ID={userId} ===");

                var games = await GetUserGamesAsync(userId);
                if (games == null || games.Count == 0)
                {
                    return new GameStatsDto
                    {
                        TotalGames = 0,
                        Wins = 0,
                        Losses = 0,
                        Draws = 0,
                        CurrentRating = 0,
                        HighestRating = 0,
                        WinPercentage = 0
                    };
                }

                var finishedGames = games.Where(g => g.IsFinished).ToList();
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

                var profile = await GetProfileAsync(userId);
                int currentRating = profile?.Rating ?? 0;

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
                    HighestRating = currentRating,
                    WinPercentage = winPercentage
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка расчета статистики: {ex.Message}");
                MessageBox.Show($"Ошибка расчета статистики: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new GameStatsDto
                {
                    TotalGames = 0,
                    Wins = 0,
                    Losses = 0,
                    Draws = 0,
                    CurrentRating = 0,
                    HighestRating = 0,
                    WinPercentage = 0
                };
            }
        }

        public async Task<List<PlayerStatsDto>> CalculateLeaderboardAsync()
        {
            try
            {
                Console.WriteLine($"=== CALCULATE LEADERBOARD ===");

                var users = await GetUsersAsync();
                if (users == null || users.Count == 0)
                    return new List<PlayerStatsDto>();

                var leaderboard = new List<PlayerStatsDto>();

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

                return leaderboard
                    .OrderByDescending(p => p.Rating)
                    .ThenByDescending(p => p.WinRate)
                    .Take(50)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка расчета лидерборда: {ex.Message}");
                MessageBox.Show($"Ошибка расчета лидерборда: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new List<PlayerStatsDto>();
            }
        }


        // В ApiService.cs добавьте:
        // ИСПРАВЛЕННЫЙ метод:
        public async Task<bool> UpdateUserRatingAsync(int userId, int ratingChange)
        {
            try
            {
                Console.WriteLine($"=== UPDATE USER RATING ===");
                Console.WriteLine($"UserId: {userId}");
                Console.WriteLine($"RatingChange: {ratingChange}");

                // Используем существующий эндпоинт из GamesController
                var request = new { RatingChange = ratingChange };
                var response = await _httpClient.PutAsJsonAsync($"api/users/{userId}/rating", request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Рейтинг пользователя {userId} обновлен на {ratingChange}");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Ошибка обновления рейтинга: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при обновлении рейтинга: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> UpdateUserRatingWithGameAsync(UpdateRatingDto updateDto)
        {
            try
            {
                Console.WriteLine($"=== UPDATE RATING WITH GAME ===");
                Console.WriteLine($"UserId: {updateDto.UserId}");
                Console.WriteLine($"RatingChange: {updateDto.RatingChange}");
                Console.WriteLine($"GameId: {updateDto.GameId}");

                var response = await _httpClient.PostAsJsonAsync("api/users/update-rating-with-game", updateDto);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Рейтинг обновлен для игры {updateDto.GameId}");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Ошибка: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при обновлении рейтинга: {ex.Message}");
                return false;
            }
        }

    }
}
