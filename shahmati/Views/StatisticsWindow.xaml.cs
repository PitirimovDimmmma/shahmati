using shahmati.Models;
using shahmati.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace shahmati.Views
{
    public partial class StatisticsWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly int _userId;
        private ExtendedGameStatsDto _currentStats;

        public StatisticsWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _apiService = new ApiService();

            Loaded += async (s, e) => await LoadStatistics();
        }

        private async Task LoadStatistics()
        {
            try
            {
                ShowLoading(true);

                // Загружаем пользователя
                var user = await _apiService.GetUserAsync(_userId);
                if (user != null)
                {
                    UserNameText.Text = user.Profile?.Nickname ?? user.Username;
                    UserRatingText.Text = $"Рейтинг: {user.Profile?.Rating ?? 0}";
                }

                // Загружаем детальную статистику
                await LoadDetailedStatistics();

                // Загружаем историю рейтинга
                await LoadRatingHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async Task LoadDetailedStatistics()
        {
            try
            {
                // Пробуем загрузить расширенную статистику
                _currentStats = await _apiService.GetDetailedUserStatsAsync(_userId);

                if (_currentStats == null)
                {
                    // Если расширенной статистики нет, пробуем загрузить базовую
                    var basicStats = await _apiService.GetUserStatsAsync(_userId);
                    if (basicStats != null)
                    {
                        _currentStats = new ExtendedGameStatsDto
                        {
                            Overall = basicStats,
                            VsAI = new GameStatsDto { TotalGames = 0, Wins = 0, Losses = 0, Draws = 0 },
                            VsHuman = new GameStatsDto { TotalGames = 0, Wins = 0, Losses = 0, Draws = 0 },
                            Performance = new PerformanceMetricsDto()
                        };
                    }
                    else
                    {
                        MessageBox.Show("Не удалось загрузить статистику. Возможно, у вас еще нет сыгранных игр.",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }

                // Общая статистика
                if (_currentStats.Overall != null)
                {
                    UpdateOverallStats(_currentStats.Overall);
                }

                // Против ИИ
                if (_currentStats.VsAI != null)
                {
                    UpdateVsAIStats(_currentStats.VsAI);
                }

                // Против людей
                if (_currentStats.VsHuman != null)
                {
                    UpdateVsHumanStats(_currentStats.VsHuman);
                }

                // Рейтинг и достижения
                if (_currentStats.Performance != null)
                {
                    UpdatePerformanceMetrics(_currentStats.Performance);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки детальной статистики: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateOverallStats(GameStatsDto stats)
        {
            TotalGamesText.Text = stats.TotalGames.ToString();
            WinsText.Text = stats.Wins.ToString();
            LossesText.Text = stats.Losses.ToString();
            DrawsText.Text = stats.Draws.ToString();

            // Рассчитываем процент побед
            double winPercentage = stats.TotalGames > 0
                ? (stats.Wins * 100.0 / stats.TotalGames)
                : 0;
            WinRateText.Text = $"{winPercentage:F1}%";

            CurrentRatingText.Text = stats.CurrentRating.ToString();
            BestRatingText.Text = stats.HighestRating.ToString();
        }

        private void UpdateVsAIStats(GameStatsDto stats)
        {
            VsAIGamesText.Text = stats.TotalGames.ToString();
            VsAIWinsText.Text = stats.Wins.ToString();

            double winPercentage = stats.TotalGames > 0
                ? (stats.Wins * 100.0 / stats.TotalGames)
                : 0;
            VsAIWinRateText.Text = $"{winPercentage:F1}%";
        }

        private void UpdateVsHumanStats(GameStatsDto stats)
        {
            VsHumanGamesText.Text = stats.TotalGames.ToString();
            VsHumanWinsText.Text = stats.Wins.ToString();

            double winPercentage = stats.TotalGames > 0
                ? (stats.Wins * 100.0 / stats.TotalGames)
                : 0;
            VsHumanWinRateText.Text = $"{winPercentage:F1}%";
        }

        private void UpdatePerformanceMetrics(PerformanceMetricsDto metrics)
        {
            CurrentStreakText.Text = metrics.CurrentStreak.ToString();
            BestStreakText.Text = metrics.BestStreak.ToString();

            // Игры по сложности
            if (metrics.GamesByDifficulty != null && metrics.GamesByDifficulty.Count > 0)
            {
                var difficultyStats = new List<KeyValuePair<string, int>>();
                foreach (var kvp in metrics.GamesByDifficulty)
                {
                    difficultyStats.Add(new KeyValuePair<string, int>(kvp.Key, kvp.Value));
                }
                DifficultyStatsList.ItemsSource = difficultyStats;
            }
            else
            {
                DifficultyStatsList.ItemsSource = new List<KeyValuePair<string, int>>
                {
                    new KeyValuePair<string, int>("Нет данных", 0)
                };
            }
        }

        private async Task LoadRatingHistory()
        {
            try
            {
                var history = await _apiService.GetUserRatingHistoryAsync(_userId, 20);
                if (history != null && history.Count > 0)
                {
                    RatingHistoryGrid.ItemsSource = history;
                }
                else
                {
                    RatingHistoryGrid.ItemsSource = new List<RatingHistoryDto>
                    {
                        new RatingHistoryDto
                        {
                            GameId = 0,
                            OpponentName = "Нет данных",
                            Result = "N/A",
                            OldRating = 0,
                            NewRating = 0,
                            RatingChange = 0,
                            CreatedAt = DateTime.Now
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории рейтинга: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ApplyFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowLoading(true);

                var filter = new FilterStatsRequest
                {
                    GameMode = GetSelectedGameMode(),
                    Difficulty = GetSelectedDifficulty(),
                    Color = "All"
                };

                var filteredStats = await _apiService.GetFilteredUserStatsAsync(_userId, filter);
                if (filteredStats != null)
                {
                    // Обновляем UI с отфильтрованными данными
                    UpdateOverallStats(filteredStats);

                    // Показываем какой фильтр активен
                    string filterInfo = GetFilterInfo();
                    MessageBox.Show($"Применен фильтр: {filterInfo}\n\n" +
                                  $"Показано: {filteredStats.TotalGames} игр",
                                  "Фильтр применен",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка применения фильтров: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private string GetSelectedGameMode()
        {
            if (VsAIRadio.IsChecked == true) return "AI";
            if (VsHumanRadio.IsChecked == true) return "Human";
            return "All";
        }

        private string GetSelectedDifficulty()
        {
            if (EasyRadio.IsChecked == true) return "Easy";
            if (MediumRadio.IsChecked == true) return "Medium";
            if (HardRadio.IsChecked == true) return "Hard";
            return "All";
        }

        private string GetFilterInfo()
        {
            string gameMode = GetSelectedGameMode();
            string difficulty = GetSelectedDifficulty();

            string gameModeText = gameMode switch
            {
                "AI" => "Против ИИ",
                "Human" => "Против людей",
                _ => "Все игры"
            };

            string difficultyText = difficulty switch
            {
                "Easy" => "Легкий",
                "Medium" => "Средний",
                "Hard" => "Сложный",
                _ => "Все уровни"
            };

            return $"{gameModeText}, {difficultyText}";
        }

        private void ShowLoading(bool show)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow dashboardWindow = new DashboardWindow(_userId);
            dashboardWindow.Show();
            this.Close();
        }
    }
}