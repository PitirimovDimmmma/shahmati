using shahmati.Models;
using shahmati.Services;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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

        private async Task LoadUserData()
        {
            try
            {
                // Загружаем данные пользователя
                var user = await _apiService.GetUserAsync(_userId);
                if (user != null)
                {
                    UserNameText.Text = user.Profile?.Nickname ?? user.Username;

                    // Загружаем статистику
                    var stats = await _apiService.GetUserStatsAsync(_userId);
                    if (stats != null)
                    {
                        UserRatingText.Text = $"Рейтинг: {stats.CurrentRating}";
                    }

                    // Загружаем аватар если есть
                    if (!string.IsNullOrEmpty(user.Profile?.PhotoPath))
                    {
                        try
                        {
                            UserAvatarImage.Source = new BitmapImage(new Uri(user.Profile.PhotoPath));
                        }
                        catch
                        {
                            SetDefaultAvatar();
                        }
                    }
                    else
                    {
                        SetDefaultAvatar();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
                SetDefaultAvatar();
            }
        }

        private void SetDefaultAvatar()
        {
            try
            {
                UserAvatarImage.Source = new BitmapImage(
                    new Uri("pack://application:,,,/Resources/default_avatar.png"));
            }
            catch
            {
                // Если ресурс не найден
            }
        }

        private void GameCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Открываем главное окно с игрой
            MainWindow gameWindow = new MainWindow(_userId);
            gameWindow.Show();
            this.Close();
        }

        private void TrainingCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Открываем окно выбора тренировок
            TrainingSelectionWindow trainingWindow = new TrainingSelectionWindow(_userId);
            trainingWindow.Show();
            this.Close();
        }

        private void RulesCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RulesWindow rulesWindow = new RulesWindow(_userId);
            rulesWindow.Show();
            this.Close();
        }

        private void HistoryCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Открываем окно истории игр
            HistoryWindow historyWindow = new HistoryWindow(_userId);
            historyWindow.Show();
            this.Close();
        }

        private void StatisticsCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Открываем окно статистики
            StatisticsWindow statisticsWindow = new StatisticsWindow(_userId);
            statisticsWindow.Show();
            this.Close();
        }

        private void LeaderboardCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Открываем окно таблицы лидеров
            LeaderboardWindow leaderboardWindow = new LeaderboardWindow(_userId);
            leaderboardWindow.Show();
            this.Close();
        }

        private async void AdminPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Проверяем роль пользователя
                var user = await _apiService.GetUserAsync(_userId);

                if (user?.Role == "Admin")
                {
                    AdminWindow adminWindow = new AdminWindow(_userId);
                    adminWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("У вас нет прав администратора",
                        "Доступ запрещен",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем данные пользователя
            Application.Current.Properties.Clear();

            // Возвращаемся к окну входа
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}