using shahmati.Models;
using shahmati.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace shahmati.Views
{
    public partial class StatisticsWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly int _userId;

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

                // Загружаем информацию о пользователе
                await LoadUserInfo();

                // Загружаем статистику игр
                await LoadGameStats();

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

        private async Task LoadUserInfo()
        {
            try
            {
                var user = await _apiService.GetUserAsync(_userId);
                if (user != null)
                {
                    UserNameText.Text = user.Profile?.Nickname ?? user.Username;
                    UserRatingText.Text = $"Рейтинг: {user.Profile?.Rating ?? 0}";

                    // Пытаемся загрузить серии, если они есть в профиле
                    if (user.Profile != null)
                    {
                        // Используем рефлексию или проверяем наличие свойств
                        var currentStreak = user.Profile.GetType().GetProperty("CurrentStreak")?.GetValue(user.Profile, null);
                        var bestStreak = user.Profile.GetType().GetProperty("BestStreak")?.GetValue(user.Profile, null);

                        CurrentStreakText.Text = currentStreak?.ToString() ?? "0";
                        BestStreakText.Text = bestStreak?.ToString() ?? "0";
                    }
                    else
                    {
                        CurrentStreakText.Text = "0";
                        BestStreakText.Text = "0";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки пользователя: {ex.Message}");
                UserNameText.Text = "Игрок";
                UserRatingText.Text = "Рейтинг: 1200";
                CurrentStreakText.Text = "0";
                BestStreakText.Text = "0";
            }
        }

        private async Task LoadGameStats()
        {
            try
            {
                var stats = await _apiService.GetUserStatsAsync(_userId);
                if (stats != null)
                {
                    TotalGamesText.Text = stats.TotalGames.ToString();
                    WinsText.Text = stats.Wins.ToString();
                    LossesText.Text = stats.Losses.ToString();
                    DrawsText.Text = stats.Draws.ToString();

                    double winPercentage = stats.TotalGames > 0
                        ? (stats.Wins * 100.0 / stats.TotalGames)
                        : 0;
                    WinRateText.Text = $"{winPercentage:F1}%";

                    CurrentRatingText.Text = stats.CurrentRating.ToString();
                    BestRatingText.Text = stats.HighestRating.ToString();

                    VsHumanGamesText.Text = stats.TotalGames.ToString();
                    VsHumanWinsText.Text = stats.Wins.ToString();
                }
                else
                {
                    SetEmptyStats();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки статистики игр: {ex.Message}");
                SetEmptyStats();
            }
        }

        private void SetEmptyStats()
        {
            TotalGamesText.Text = "0";
            WinsText.Text = "0";
            LossesText.Text = "0";
            DrawsText.Text = "0";
            WinRateText.Text = "0%";
            CurrentRatingText.Text = "1200";
            BestRatingText.Text = "1200";
            VsHumanGamesText.Text = "0";
            VsHumanWinsText.Text = "0";
            CurrentStreakText.Text = "0";
            BestStreakText.Text = "0";
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
                    RatingHistoryGrid.ItemsSource = new List<RatingHistoryDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки истории рейтинга: {ex.Message}");
                RatingHistoryGrid.ItemsSource = new List<RatingHistoryDto>();
            }
        }

        private void ShowLoading(bool show)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow dashboardWindow = new DashboardWindow(_userId);
            dashboardWindow.Show();
            Close();
        }
    }
}