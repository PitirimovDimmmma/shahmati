using shahmati.Models;
using shahmati.Services;
using shahmati.ViewModels;
using shahmati.Views;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace shahmati
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly MainViewModel _viewModel;
        private readonly int _userId;
        private int _currentGameId;

        public MainWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _apiService = new ApiService();
            _viewModel = new MainViewModel(userId);
            DataContext = _viewModel;

            Loaded += async (s, e) => await InitializeGame();
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            UserNameText.Text = "Тестовый пользователь";
            UserRatingText.Text = "Рейтинг: 1200";
        }

        private async Task InitializeGame()
        {
            try
            {
                // Загружаем данные пользователя
                await LoadUserData();

                // Проверяем подключение к API
                bool isConnected = await _apiService.TestConnectionAsync();
                if (!isConnected)
                {
                    MessageBox.Show("⚠️ Работаем в локальном режиме\n\nСервер недоступен",
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}");
            }
        }

        private async Task LoadUserData()
        {
            try
            {
                var user = await _apiService.GetUserAsync(_userId);
                if (user != null)
                {
                    UserNameText.Text = user.Profile?.Nickname ?? user.Username;

                    var stats = await _apiService.GetUserStatsAsync(_userId);
                    if (stats != null)
                    {
                        UserRatingText.Text = $"Рейтинг: {stats.CurrentRating}";
                    }

                    // Загружаем аватар
                    if (!string.IsNullOrEmpty(user.Profile?.PhotoPath))
                    {
                        try
                        {
                            UserAvatar.Source = new BitmapImage(new Uri(user.Profile.PhotoPath));
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
                UserAvatar.Source = new BitmapImage(
                    new Uri("pack://application:,,,/Resources/default_avatar.png"));
            }
            catch
            {
                // Если ресурс не найден
            }
        }

        private async void SaveGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGameId == 0)
            {
                MessageBox.Show("Сначала создайте игру", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Получаем текущую игру
                var game = await _apiService.GetGameAsync(_currentGameId);
                if (game != null)
                {
                    // Сохраняем игру
                    var saveRequest = new SaveGameRequest
                    {
                        UserId = _userId,
                        GameData = game.GameState,
                        GameName = $"Игра от {DateTime.Now:dd.MM.yyyy HH:mm}"
                    };

                    bool success = await _apiService.SaveGameAsync(saveRequest);

                    if (success)
                    {
                        MessageBox.Show("Игра сохранена успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private async void LoadGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем список сохраненных игр
                var savedGames = await _apiService.GetSavedGamesAsync(_userId);
                if (savedGames.Count == 0)
                {
                    MessageBox.Show("Нет сохраненных игр", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // TODO: Добавить диалог выбора игры
                MessageBox.Show("Функция загрузки игры будет реализована в следующем обновлении",
                    "В разработке",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void RestartGameButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Начать новую игру? Текущий прогресс будет потерян.",
                "Новая игра",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Сбрасываем ViewModel
                DataContext = new MainViewModel(_userId);
                _currentGameId = 0;
                MessageBox.Show("Новая игра начата!");
            }
        }

        private async void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Завершаем текущую игру если она есть
            if (_currentGameId != 0)
            {
                var result = MessageBox.Show("Завершить текущую игру и вернуться на главную?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await FinishCurrentGame();
                }
            }

            DashboardWindow dashboardWindow = new DashboardWindow(_userId);
            dashboardWindow.Show();
            this.Close();
        }

        private async Task FinishCurrentGame()
        {
            if (_currentGameId != 0)
            {
                try
                {
                    await _apiService.FinishGameAsync(_currentGameId, "Abandoned");
                    _currentGameId = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка завершения игры: {ex.Message}");
                }
            }
        }

        private async void CreateOnlineGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var createDto = new CreateGameDto
                {
                    WhitePlayerId = _userId,
                    GameMode = _viewModel.GameMode == "Человек vs Компьютер" ? "HumanVsAI" : "HumanVsHuman",
                    Difficulty = _viewModel.Difficulty
                };

                var game = await _apiService.CreateGameAsync(createDto);
                if (game != null)
                {
                    _currentGameId = game.Id;
                    MessageBox.Show($"Онлайн игра создана! ID: {game.Id}",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания игры: {ex.Message}");
            }
        }

        protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Завершаем текущую игру при закрытии окна
            await FinishCurrentGame();
            base.OnClosing(e);
        }
    }
}