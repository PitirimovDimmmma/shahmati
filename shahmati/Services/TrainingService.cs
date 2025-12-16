using shahmati.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;

namespace shahmati.Services
{
    public class TrainingService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://localhost:7259/";

        public TrainingService()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (sender, cert, chain, sslPolicyErrors) => true;

            _httpClient = new HttpClient(handler);
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // Получение всех типов тренировок
        public async Task<List<TrainingTypeDto>> GetTrainingTypesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TrainingTypeDto>>("training/types");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении тренировок: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new List<TrainingTypeDto>();
            }
        }

        // Получение тренировок по категории
        public async Task<List<TrainingTypeDto>> GetTrainingsByCategoryAsync(string category)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TrainingTypeDto>>($"training/category/{category}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении тренировок: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new List<TrainingTypeDto>();
            }
        }

        // Получение тренировочных позиций
        public async Task<List<TrainingPositionDto>> GetTrainingPositionsAsync(int trainingTypeId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TrainingPositionDto>>($"training/{trainingTypeId}/positions");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении позиций: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new List<TrainingPositionDto>();
            }
        }

        // Получение прогресса пользователя
        public async Task<List<TrainingProgressDto>> GetUserTrainingProgressAsync(int userId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TrainingProgressDto>>($"training/progress/{userId}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении прогресса: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new List<TrainingProgressDto>();
            }
        }

        // Начало тренировки
        public async Task<bool> StartTrainingAsync(StartTrainingRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("training/start", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка начала тренировки: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                return false;
            }
        }

        // Завершение тренировки
        public async Task<bool> CompleteTrainingAsync(CompleteTrainingRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("training/complete", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка завершения тренировки: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                return false;
            }
        }

        // Получение статистики тренировок
        public async Task<TrainingStatsDto> GetTrainingStatsAsync(int userId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<TrainingStatsDto>($"training/stats/{userId}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения статистики: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new TrainingStatsDto();
            }
        }

        // Поиск тренировок
        public async Task<List<TrainingTypeDto>> SearchTrainingsAsync(string query)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TrainingTypeDto>>($"training/search?query={query}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска тренировок: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return new List<TrainingTypeDto>();
            }
        }
    }
}