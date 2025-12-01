using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using shahmati.Models;

namespace shahmati.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:7001/api";

        public ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // Аутентификация
        public async Task<UserWithProfileDto> LoginAsync(string username, string password)
        {
            try
            {
                // Используем анонимный объект
                var loginData = new 
                { 
                    Username = username, 
                    Password = password 
                };
                
                var response = await _httpClient.PostAsJsonAsync("auth/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserWithProfileDto>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка авторизации: {error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при авторизации: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> RegisterAsync(string username, string email, string password)
        {
            try
            {
                // Используем анонимный объект
                var registerData = new 
                { 
                    Username = username, 
                    Email = email, 
                    Password = password 
                };
                
                var response = await _httpClient.PostAsJsonAsync("auth/register", registerData);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка регистрации: {error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}");
            }
            return false;
        }

        public async Task<UserProfileDto> GetProfileAsync(int userId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<UserProfileDto>($"auth/profile/{userId}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения профиля: {ex.Message}");
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
                MessageBox.Show($"Ошибка обновления профиля: {ex.Message}");
                return false;
            }
        }

        // Игры
        public async Task<List<GameDto>> GetActiveGamesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<GameDto>>("games");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении активных игр: {ex.Message}");
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
                MessageBox.Show($"Ошибка при получении игр пользователя: {ex.Message}");
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
                MessageBox.Show($"Ошибка при получении игры: {ex.Message}");
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
                    MessageBox.Show($"Ошибка создания игры: {error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании игры: {ex.Message}");
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
                    MessageBox.Show($"Ошибка хода: {error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении хода: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> FinishGameAsync(int gameId, string result)
        {
            try
            {
                // Используем анонимный объект
                var finishData = new { Result = result };
                var response = await _httpClient.PutAsJsonAsync($"games/{gameId}/finish", finishData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при завершении игры: {ex.Message}");
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
                MessageBox.Show($"Ошибка при удалении игры: {ex.Message}");
                return false;
            }
        }

        // Сохраненные игры
        public async Task<List<SavedGameDto>> GetSavedGamesAsync(int userId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<SavedGameDto>>($"savedgames/user/{userId}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении сохраненных игр: {ex.Message}");
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
                MessageBox.Show($"Ошибка сохранения игры: {ex.Message}");
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
                MessageBox.Show($"Ошибка загрузки игры: {ex.Message}");
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
                MessageBox.Show($"Ошибка обновления сохраненной игры: {ex.Message}");
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
                MessageBox.Show($"Ошибка удаления сохраненной игры: {ex.Message}");
                return false;
            }
        }

        // Пользователи
        public async Task<List<UserWithProfileDto>> GetUsersAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<UserWithProfileDto>>("users");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении пользователей: {ex.Message}");
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
                MessageBox.Show($"Ошибка при получении пользователя: {ex.Message}");
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
                MessageBox.Show($"Ошибка поиска пользователей: {ex.Message}");
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
                MessageBox.Show($"Ошибка обновления пользователя: {ex.Message}");
                return false;
            }
        }

        // Статистика
        public async Task<GameStatsDto> GetUserStatsAsync(int userId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<GameStatsDto>($"stats/user/{userId}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения статистики: {ex.Message}");
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
                MessageBox.Show($"Ошибка получения таблицы лидеров: {ex.Message}");
                return new List<PlayerStatsDto>();
            }
        }

        // Тестирование подключения
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("games");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к API: {ex.Message}\n\nУбедитесь, что API запущен на localhost:7001");
                return false;
            }
        }
    }
}