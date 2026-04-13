using shahmati.Models;
using shahmati.Services;
using System;
using System.Windows;
using System.Windows.Input;

namespace shahmati.Views
{
    public partial class DashboardWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly int _userId;

        public DashboardWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _apiService = new ApiService();

            Loaded += async (s, e) => await LoadUserData();
        }

        private async System.Threading.Tasks.Task LoadUserData()
        {
            try
            {
                var user = await _apiService.GetUserAsync(_userId);
                if (user != null)
                {
                    UserNameText.Text = user.Profile?.Nickname ?? user.Username;
                    await LoadUserRating();
                }
                else
                {
                    SetDefaultData();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки данных: {ex.Message}");
                SetDefaultData();
            }
        }

        private async System.Threading.Tasks.Task LoadUserRating()
        {
            try
            {
                var stats = await _apiService.GetUserStatsAsync(_userId);
                if (stats != null)
                {
                    UserRatingText.Text = $"Рейтинг: {stats.CurrentRating}";
                }
                else
                {
                    var user = await _apiService.GetUserAsync(_userId);
                    int rating = user?.Profile?.Rating ?? 0;
                    UserRatingText.Text = $"Рейтинг: {rating}";
                }
            }
            catch (Exception ex)
            {
                UserRatingText.Text = "Рейтинг: 0";
            }
        }

        private void SetDefaultData()
        {
            UserNameText.Text = "Гость";
            UserRatingText.Text = "Рейтинг: 0";
        }

        private void UserProfileArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
            {
                OpenUserProfile();
            }
        }

        private void OpenUserProfile()
        {
            try
            {
                UserProfileWindow profileWindow = new UserProfileWindow(_userId);
                profileWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия профиля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GameCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow gameWindow = new MainWindow(_userId);
            gameWindow.Show();
            Close();
        }

        private void TrainingCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TrainingSelectionWindow trainingWindow = new TrainingSelectionWindow(_userId);
            trainingWindow.Show();
            Close();
        }

        private void RulesCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RulesWindow rulesWindow = new RulesWindow(_userId);
            rulesWindow.Show();
            Close();
        }

        private void HistoryCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HistoryWindow historyWindow = new HistoryWindow(_userId);
            historyWindow.Show();
            Close();
        }

        private void StatisticsCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StatisticsWindow statisticsWindow = new StatisticsWindow(_userId);
            statisticsWindow.Show();
            Close();
        }

        private void LeaderboardCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LeaderboardWindow leaderboardWindow = new LeaderboardWindow(_userId);
            leaderboardWindow.Show();
            Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Properties.Clear();
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}