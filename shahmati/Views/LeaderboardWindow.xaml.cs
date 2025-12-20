using shahmati.Models;
using shahmati.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace shahmati.Views
{
    public partial class LeaderboardWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly int _userId;

        public LeaderboardWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _apiService = new ApiService();

            Loaded += async (s, e) => await LoadLeaderboard();
        }

        private async Task LoadLeaderboard()
        {
            try
            {
                // Используем правильный метод
                var leaderboard = await _apiService.GetLeaderboardAsync();
                if (leaderboard != null && leaderboard.Count > 0)
                {
                    // Для PlayerStatsDto нет свойства Rank, добавляем его
                    for (int i = 0; i < leaderboard.Count; i++)
                    {
                        // Создаем новый объект или используем динамический
                        var player = leaderboard[i];
                        // Можно добавить свойство через dynamic или создать новый тип
                    }

                    // Вместо привязки к PlayerRatingDto привязываемся к PlayerStatsDto
                    // Создаем новый список с добавленным Rank
                    var rankedList = leaderboard.Select((p, index) => new
                    {
                        Rank = index + 1,
                        p.UserId,
                        p.Username,
                        p.Rating,
                        p.GamesPlayed,
                        p.Wins,
                        p.WinRate
                    }).ToList();

                    LeaderboardGrid.ItemsSource = rankedList;

                    // Находим позицию текущего пользователя
                    var userRank = leaderboard.FirstOrDefault(p => p.UserId == _userId);
                    if (userRank != null)
                    {
                        int rank = leaderboard.IndexOf(userRank) + 1;
                        UserRankText.Text = $"🏆 Ваш ранг: {rank}";
                    }
                    else
                    {
                        UserRankText.Text = "🤔 Вы не в таблице лидеров";
                    }

                    LastUpdateText.Text = $"🕒 Обновлено: {DateTime.Now:HH:mm:ss}";
                }
                else
                {
                    UserRankText.Text = "📭 Нет данных";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки таблицы лидеров: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                UserRankText.Text = "⚠️ Ошибка загрузки";
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadLeaderboard();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow dashboardWindow = new DashboardWindow(_userId);
            dashboardWindow.Show();
            this.Close();
        }
    }
}