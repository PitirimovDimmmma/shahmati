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
                var leaderboard = await _apiService.GetLeaderboardAsync();
                if (leaderboard != null && leaderboard.Count > 0)
                {
                    // Назначаем ранги
                    for (int i = 0; i < leaderboard.Count; i++)
                    {
                        leaderboard[i].Rank = i + 1;
                    }

                    LeaderboardGrid.ItemsSource = leaderboard;

                    // Находим позицию текущего пользователя
                    var userRank = leaderboard.FirstOrDefault(p => p.UserId == _userId);
                    if (userRank != null)
                    {
                        UserRankText.Text = $"🏆 Ваш ранг: {userRank.Rank}";
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