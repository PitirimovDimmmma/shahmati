using System;
using System.Windows;
using System.Windows.Media.Imaging;
using shahmati.Services;
using shahmati.Models;

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

        private void GameCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Открываем главное окно с игрой
            MainWindow gameWindow = new MainWindow(_userId);
            gameWindow.Show();
            this.Close();
        }

        private void OnlineGamesCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Открываем окно онлайн игр
            OnlineGamesWindow onlineGamesWindow = new OnlineGamesWindow(_userId);
            onlineGamesWindow.Show();
            this.Close();
        }

        private void RulesCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RulesWindow rulesWindow = new RulesWindow(_userId);
            rulesWindow.Show();
            this.Close();
        }

        private void ProfileCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Открываем окно профиля
            ProfileSetupWindow profileWindow = new ProfileSetupWindow(_userId);
            profileWindow.Show();
            this.Close();
        }

        private void HistoryCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Показываем сообщение о том, что функция в разработке
            MessageBox.Show("История игр будет доступна в следующем обновлении",
                "В разработке",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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