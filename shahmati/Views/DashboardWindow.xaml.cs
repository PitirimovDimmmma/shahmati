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
                Console.WriteLine($"=== Загрузка данных пользователя ID={_userId} ===");

                // Загружаем данные пользователя
                var user = await _apiService.GetUserAsync(_userId);
                if (user != null)
                {
                    Console.WriteLine($"Пользователь найден: {user.Username}");

                    // Устанавливаем никнейм
                    UserNameText.Text = user.Profile?.Nickname ?? user.Username;

                    // Загружаем аватар
                    LoadUserAvatar(user.Profile?.PhotoPath);

                    // Загружаем рейтинг
                    await LoadUserRating();
                }
                else
                {
                    Console.WriteLine("❌ Пользователь не найден");
                    SetDefaultData();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки данных: {ex.Message}");
                SetDefaultData();
            }
        }

        private async Task LoadUserRating()
        {
            try
            {
                var stats = await _apiService.GetUserStatsAsync(_userId);
                if (stats != null)
                {
                    Console.WriteLine($"Рейтинг из статистики: {stats.CurrentRating}");
                    UserRatingText.Text = $"Рейтинг: {stats.CurrentRating}";
                }
                else
                {
                    // Если статистики нет, получаем рейтинг из профиля
                    var user = await _apiService.GetUserAsync(_userId);
                    int rating = user?.Profile?.Rating ?? 0;
                    Console.WriteLine($"Рейтинг из профиля: {rating}");
                    UserRatingText.Text = $"Рейтинг: {rating}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки рейтинга: {ex.Message}");
                UserRatingText.Text = "Рейтинг: 0";
            }
        }

        private void LoadUserAvatar(string photoPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(photoPath))
                {
                    Console.WriteLine($"Загрузка аватара: {photoPath}");

                    if (System.IO.File.Exists(photoPath))
                    {
                        UserAvatarImage.Source = new BitmapImage(new Uri(photoPath));
                        Console.WriteLine("✅ Аватар загружен с диска");
                    }
                    else if (photoPath.StartsWith("http"))
                    {
                        UserAvatarImage.Source = new BitmapImage(new Uri(photoPath));
                        Console.WriteLine("✅ Аватар загружен по URL");
                    }
                    else
                    {
                        SetDefaultAvatar();
                        Console.WriteLine("⚠️ Аватар не найден, используем стандартный");
                    }
                }
                else
                {
                    SetDefaultAvatar();
                    Console.WriteLine("⚠️ Путь к аватару не указан, используем стандартный");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки аватара: {ex.Message}");
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
                UserAvatarImage.Source = null;
            }
        }

        private void SetDefaultData()
        {
            UserNameText.Text = "Гость";
            UserRatingText.Text = "Рейтинг: 0";
            SetDefaultAvatar();
        }

        // Обработчик клика на область профиля
        private void UserProfileArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
            {
                Console.WriteLine("=== Открытие профиля пользователя ===");
                OpenUserProfile();
            }
        }

        private void OpenUserProfile()
        {
            try
            {
                // Открываем окно профиля с возможностью редактирования
                UserProfileWindow profileWindow = new UserProfileWindow(_userId);
                profileWindow.ShowDialog();

                // После закрытия окна профиля обновляем данные
                if (profileWindow.DataUpdated)
                {
                    Console.WriteLine("✅ Данные профиля обновлены, перезагружаем");
                    LoadUserData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия профиля: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void GameCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow gameWindow = new MainWindow(_userId);
            gameWindow.Show();
            this.Close();
        }

        private void TrainingCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
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
            HistoryWindow historyWindow = new HistoryWindow(_userId);
            historyWindow.Show();
            this.Close();
        }

        private void StatisticsCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StatisticsWindow statisticsWindow = new StatisticsWindow(_userId);
            statisticsWindow.Show();
            this.Close();
        }

        private void LeaderboardCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LeaderboardWindow leaderboardWindow = new LeaderboardWindow(_userId);
            leaderboardWindow.Show();
            this.Close();
        }

        /*private async void AdminPanel_MouseDown(object sender, MouseButtonEventArgs e)
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
        }*/

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