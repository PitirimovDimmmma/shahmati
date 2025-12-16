using shahmati.Models;
using shahmati.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace shahmati.ViewModels
{
    public class StatisticsViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly int _userId;

        private ExtendedGameStatsDto _stats;
        private ObservableCollection<RatingHistoryDto> _ratingHistory;
        private ObservableCollection<PlayerRatingDto> _leaderboard;

        public ExtendedGameStatsDto Stats
        {
            get => _stats;
            set => SetProperty(ref _stats, value);
        }

        public ObservableCollection<RatingHistoryDto> RatingHistory
        {
            get => _ratingHistory;
            set => SetProperty(ref _ratingHistory, value);
        }

        public ObservableCollection<PlayerRatingDto> Leaderboard
        {
            get => _leaderboard;
            set => SetProperty(ref _leaderboard, value);
        }

        public StatisticsViewModel(int userId)
        {
            _userId = userId;
            _apiService = new ApiService();
            RatingHistory = new ObservableCollection<RatingHistoryDto>();
            Leaderboard = new ObservableCollection<PlayerRatingDto>();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                // Загружаем статистику
                Stats = await _apiService.GetDetailedUserStatsAsync(_userId);

                // Загружаем историю рейтинга
                var history = await _apiService.GetUserRatingHistoryAsync(_userId);
                RatingHistory.Clear();
                foreach (var item in history)
                {
                    RatingHistory.Add(item);
                }

                // Загружаем таблицу лидеров
                var leaderboard = await _apiService.GetLeaderboardAsync();
                Leaderboard.Clear();
                foreach (var player in leaderboard)
                {
                    Leaderboard.Add(player);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<GameStatsDto> ApplyFilterAsync(FilterStatsRequest filter)
        {
            return await _apiService.GetFilteredUserStatsAsync(_userId, filter);
        }
    }
}