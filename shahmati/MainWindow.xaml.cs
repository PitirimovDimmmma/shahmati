using shahmati.Models;
using shahmati.Services;
using shahmati.ViewModels;
using shahmati.Views;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;
using System.Threading.Tasks;

namespace shahmati
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly MainViewModel _viewModel;
        private readonly int _userId;
        private int _currentGameId;

        // Таймеры для шахмат
        private DispatcherTimer _whiteTimer;
        private DispatcherTimer _blackTimer;
        private TimeSpan _whiteTimeLeft;
        private TimeSpan _blackTimeLeft;

        // Таймер общей игры
        private DispatcherTimer _gameTimer;
        private DateTime _gameStartTime;

        public MainWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _apiService = new ApiService();
            _viewModel = new MainViewModel(userId);
            DataContext = _viewModel;

            // Инициализируем таймеры
            InitializeChessTimers();
            InitializeGameTimer();

            Loaded += async (s, e) => await InitializeGame();
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            SetDefaultUserData();
            InitializeChessTimers();
        }

        private void InitializeChessTimers()
        {
            // Настройка таймера белых
            _whiteTimer = new DispatcherTimer();
            _whiteTimer.Interval = TimeSpan.FromSeconds(1);
            _whiteTimer.Tick += WhiteTimer_Tick;

            // Настройка таймера черных
            _blackTimer = new DispatcherTimer();
            _blackTimer.Interval = TimeSpan.FromSeconds(1);
            _blackTimer.Tick += BlackTimer_Tick;

            // Установка времени по умолчанию (5 минут)
            _whiteTimeLeft = TimeSpan.FromMinutes(5);
            _blackTimeLeft = TimeSpan.FromMinutes(5);
            UpdateTimerDisplays();
        }

        private void InitializeGameTimer()
        {
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromSeconds(1);
            _gameTimer.Tick += GameTimer_Tick;
        }

        private async Task InitializeGame()
        {
            try
            {
                // Загружаем данные пользователя из API
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

                // Обновляем интерфейс в зависимости от режима игры
                UpdateGameModeUI();

                // Запускаем игру
                _viewModel.StartNewGame();
                StartGameTimers();
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
                Console.WriteLine($"=== Загрузка данных пользователя ID={_userId} ===");

                var user = await _apiService.GetUserAsync(_userId);
                if (user != null)
                {
                    Console.WriteLine($"✅ Пользователь найден: {user.Username}");

                    if (UserNameText != null)
                        UserNameText.Text = user.Profile?.Nickname ?? user.Username;

                    await LoadUserAvatar(user);
                    await LoadUserRating(user);
                }
                else
                {
                    Console.WriteLine("❌ Пользователь не найден");
                    SetDefaultUserData();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки данных: {ex.Message}");
                SetDefaultUserData();
            }
        }

        private async Task LoadUserAvatar(UserWithProfileDto user)
        {
            try
            {
                string photoPath = user.Profile?.PhotoPath;

                if (!string.IsNullOrEmpty(photoPath))
                {
                    if (File.Exists(photoPath))
                    {
                        UserAvatar.Source = new BitmapImage(new Uri(photoPath));
                    }
                    else if (photoPath.StartsWith("http"))
                    {
                        UserAvatar.Source = new BitmapImage(new Uri(photoPath));
                    }
                    else if (photoPath.StartsWith("/"))
                    {
                        try
                        {
                            UserAvatar.Source = new BitmapImage(
                                new Uri($"pack://application:,,,{photoPath}"));
                        }
                        catch
                        {
                            SetDefaultAvatar();
                        }
                    }
                    else
                    {
                        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, photoPath);
                        if (File.Exists(fullPath))
                        {
                            UserAvatar.Source = new BitmapImage(new Uri(fullPath));
                        }
                        else
                        {
                            SetDefaultAvatar();
                        }
                    }
                }
                else
                {
                    SetDefaultAvatar();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки аватара: {ex.Message}");
                SetDefaultAvatar();
            }
        }

        private async Task LoadUserRating(UserWithProfileDto user)
        {
            try
            {
                var stats = await _apiService.GetUserStatsAsync(_userId);

                if (stats != null)
                {
                    int rating = stats.CurrentRating;
                    Console.WriteLine($"Рейтинг из статистики: {rating}");

                    if (UserRatingText != null)
                        UserRatingText.Text = $"Рейтинг: {rating}";

                    if (RatingText != null)
                        RatingText.Text = rating.ToString();
                }
                else
                {
                    int rating = user?.Profile?.Rating ?? 1200;
                    Console.WriteLine($"Рейтинг из профиля: {rating}");

                    if (UserRatingText != null)
                        UserRatingText.Text = $"Рейтинг: {rating}";

                    if (RatingText != null)
                        RatingText.Text = rating.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки рейтинга: {ex.Message}");

                int defaultRating = user?.Profile?.Rating ?? 1200;
                if (UserRatingText != null)
                    UserRatingText.Text = $"Рейтинг: {defaultRating}";

                if (RatingText != null)
                    RatingText.Text = defaultRating.ToString();
            }
        }

        private void SetDefaultAvatar()
        {
            try
            {
                string[] possiblePaths = {
                    "pack://application:,,,/Resources/default_avatar.png",
                    "pack://application:,,,/shahmati;component/Resources/default_avatar.png",
                    "/Resources/default_avatar.png",
                    "Resources/default_avatar.png"
                };

                foreach (var path in possiblePaths)
                {
                    try
                    {
                        UserAvatar.Source = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
                        return;
                    }
                    catch { }
                }

                UserAvatar.Source = null;
            }
            catch
            {
                UserAvatar.Source = null;
            }
        }

        private void SetDefaultUserData()
        {
            if (UserNameText != null)
                UserNameText.Text = "Гость";

            if (UserRatingText != null)
                UserRatingText.Text = "Рейтинг: 1200";

            if (RatingText != null)
                RatingText.Text = "1200";

            SetDefaultAvatar();
        }

        private void UpdateGameModeUI()
        {
            if (GameModeComboBox == null || AIDifficultyPanel == null ||
                DifficultyComboBox == null || HighlightMovesToggle == null)
                return;

            if (GameModeComboBox.SelectedIndex == 1)
            {
                AIDifficultyPanel.Visibility = Visibility.Visible;

                if (DifficultyComboBox.SelectedIndex > 0)
                {
                    HighlightMovesToggle.Visibility = Visibility.Collapsed;
                }
                else
                {
                    HighlightMovesToggle.Visibility = Visibility.Visible;
                }
            }
            else
            {
                AIDifficultyPanel.Visibility = Visibility.Collapsed;
                HighlightMovesToggle.Visibility = Visibility.Visible;
            }
        }

        private void StartGameTimers()
        {
            _gameStartTime = DateTime.Now;
            _gameTimer.Start();
            _whiteTimer.Start();
        }

        // ===== ТАЙМЕРЫ =====

        private void WhiteTimer_Tick(object sender, EventArgs e)
        {
            if (_whiteTimeLeft > TimeSpan.Zero && _whiteTimeLeft < TimeSpan.MaxValue)
            {
                _whiteTimeLeft = _whiteTimeLeft.Subtract(TimeSpan.FromSeconds(1));
                UpdateTimerDisplays();

                if (_whiteTimeLeft <= TimeSpan.Zero)
                {
                    _whiteTimer.Stop();
                    GameOverByTimeout("Черные", "время вышло у белых");
                }
            }
        }

        private void BlackTimer_Tick(object sender, EventArgs e)
        {
            if (_blackTimeLeft > TimeSpan.Zero && _blackTimeLeft < TimeSpan.MaxValue)
            {
                _blackTimeLeft = _blackTimeLeft.Subtract(TimeSpan.FromSeconds(1));
                UpdateTimerDisplays();

                if (_blackTimeLeft <= TimeSpan.Zero)
                {
                    _blackTimer.Stop();
                    GameOverByTimeout("Белые", "время вышло у черных");
                }
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _gameStartTime;
            if (GameTimeText != null)
                GameTimeText.Text = $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }

        private void UpdateTimerDisplays()
        {
            // Исправляем отображение бесконечности
            if (_whiteTimeLeft == TimeSpan.MaxValue || _whiteTimeLeft.TotalMinutes > 10000)
            {
                if (WhiteTimerText != null)
                    WhiteTimerText.Text = "∞";
            }
            else if (_whiteTimeLeft >= TimeSpan.Zero)
            {
                if (WhiteTimerText != null)
                    WhiteTimerText.Text = $"{(int)_whiteTimeLeft.TotalMinutes:00}:{_whiteTimeLeft.Seconds:00}";
            }

            if (_blackTimeLeft == TimeSpan.MaxValue || _blackTimeLeft.TotalMinutes > 10000)
            {
                if (BlackTimerText != null)
                    BlackTimerText.Text = "∞";
            }
            else if (_blackTimeLeft >= TimeSpan.Zero)
            {
                if (BlackTimerText != null)
                    BlackTimerText.Text = $"{(int)_blackTimeLeft.TotalMinutes:00}:{_blackTimeLeft.Seconds:00}";
            }
        }

        private void SetTimersBasedOnDifficulty()
        {
            if (DifficultyComboBox == null)
                return;

            // Для "без времени" используем специальное значение
            if (DifficultyComboBox.SelectedIndex == 0) // Новичок (без времени)
            {
                _whiteTimeLeft = TimeSpan.MaxValue;
                _blackTimeLeft = TimeSpan.MaxValue;
            }
            else
            {
                switch (DifficultyComboBox.SelectedIndex)
                {
                    case 1: // Легкий (10 минут)
                        _whiteTimeLeft = TimeSpan.FromMinutes(10);
                        _blackTimeLeft = TimeSpan.FromMinutes(10);
                        break;
                    case 2: // Средний (5 минут)
                        _whiteTimeLeft = TimeSpan.FromMinutes(5);
                        _blackTimeLeft = TimeSpan.FromMinutes(5);
                        break;
                    case 3: // Сложный (3 минуты)
                        _whiteTimeLeft = TimeSpan.FromMinutes(3);
                        _blackTimeLeft = TimeSpan.FromMinutes(3);
                        break;
                    case 4: // Эксперт (1 минута)
                        _whiteTimeLeft = TimeSpan.FromMinutes(1);
                        _blackTimeLeft = TimeSpan.FromMinutes(1);
                        break;
                    default:
                        _whiteTimeLeft = TimeSpan.FromMinutes(5);
                        _blackTimeLeft = TimeSpan.FromMinutes(5);
                        break;
                }
            }

            UpdateTimerDisplays();
        }

        public void SwitchTurn(string newPlayer)
        {
            if (newPlayer.Contains("Белые") || newPlayer.Contains("White"))
            {
                _blackTimer.Stop();
                _whiteTimer.Start();
            }
            else
            {
                _whiteTimer.Stop();
                _blackTimer.Start();
            }

            UpdateCurrentPlayer(newPlayer);
        }

        private void GameOverByTimeout(string winner, string reason)
        {
            _gameTimer.Stop();
            _whiteTimer.Stop();
            _blackTimer.Stop();

            if (StatusText != null)
                StatusText.Text = $"Игра окончена! {winner} победили ({reason})";

            if (StatusIcon != null)
                StatusIcon.Text = "⏰";

            MessageBox.Show($"Время вышло! Победили {winner}",
                "Игра окончена",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // ===== ОБРАБОТЧИКИ СОБЫТИЙ =====

        private void GameModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGameModeUI();

            if (GameModeComboBox.SelectedIndex == 1)
            {
                if (AITurnIndicator != null)
                    AITurnIndicator.Visibility = Visibility.Visible;

                if (StatusText != null)
                    StatusText.Text = "Игра против ИИ. Ваш ход.";

                if (StatusIcon != null)
                    StatusIcon.Text = "🤖";
            }
            else
            {
                if (AITurnIndicator != null)
                    AITurnIndicator.Visibility = Visibility.Collapsed;

                if (StatusText != null)
                    StatusText.Text = "Игра человек против человека";

                if (StatusIcon != null)
                    StatusIcon.Text = "👥";
            }
        }

        private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DifficultyComboBox == null || HighlightMovesToggle == null)
                return;

            if (DifficultyComboBox.SelectedIndex == 0)
            {
                HighlightMovesToggle.Visibility = Visibility.Visible;
                HighlightMovesToggle.IsChecked = true;

                if (_viewModel != null)
                    _viewModel.EnableMoveHighlighting = true;
            }
            else
            {
                HighlightMovesToggle.Visibility = Visibility.Collapsed;
                HighlightMovesToggle.IsChecked = false;

                if (_viewModel != null)
                    _viewModel.EnableMoveHighlighting = false;
            }

            SetTimersBasedOnDifficulty();
        }

        private void HighlightMovesToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.EnableMoveHighlighting = true;
                if (StatusText != null)
                    StatusText.Text = "Подсветка возможных ходов ВКЛЮЧЕНА";
            }
        }

        private void HighlightMovesToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.EnableMoveHighlighting = false;
                if (StatusText != null)
                    StatusText.Text = "Подсветка возможных ходов ВЫКЛЮЧЕНА";
            }
        }

        private async void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Завершить текущую игру?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await FinishCurrentGame();
            }
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Начать новую игру? Текущая игра будет завершена.",
                "Новая игра",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _viewModel.StartNewGame();
                _currentGameId = 0;

                SetTimersBasedOnDifficulty();

                _gameStartTime = DateTime.Now;
                _whiteTimer.Start();

                if (MovesCountText != null)
                    MovesCountText.Text = "0";

                if (StatusText != null)
                    StatusText.Text = "Новая игра начата!";

                if (StatusIcon != null)
                    StatusIcon.Text = "♛";
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Начать игру заново? Все ходы будут сброшены.",
                "Начать заново",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _viewModel.StartNewGame();

                SetTimersBasedOnDifficulty();

                _gameStartTime = DateTime.Now;
                _whiteTimer.Start();

                if (MovesCountText != null)
                    MovesCountText.Text = "0";

                if (StatusText != null)
                    StatusText.Text = "Игра начата заново";
            }
        }

        private async void HomeButton_Click(object sender, RoutedEventArgs e)
        {
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

                    if (StatusText != null)
                        StatusText.Text = "Игра завершена";

                    if (StatusIcon != null)
                        StatusIcon.Text = "🏁";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка завершения игры: {ex.Message}");
                }
            }

            _gameTimer.Stop();
            _whiteTimer.Stop();
            _blackTimer.Stop();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Вызываем асинхронный метод завершения игры
            var task = Task.Run(async () => await FinishCurrentGame());
            task.Wait(); // Ждем завершения
        }

        // Метод для обновления истории ходов
        public void UpdateMoveHistory(List<string> moves)
        {
            if (MoveHistoryList == null || MovesCountText == null)
                return;

            Dispatcher.Invoke(() =>
            {
                MoveHistoryList.Items.Clear();
                foreach (var move in moves)
                {
                    MoveHistoryList.Items.Add(move);
                }

                if (moves.Count > 0)
                {
                    MoveHistoryList.ScrollIntoView(MoveHistoryList.Items[moves.Count - 1]);
                }

                MovesCountText.Text = moves.Count.ToString();
            });
        }

        // Метод для обновления текущего игрока
        public void UpdateCurrentPlayer(string player)
        {
            if (CurrentPlayerText == null)
                return;

            Dispatcher.Invoke(() =>
            {
                CurrentPlayerText.Text = player.ToUpper();

                if (player.Contains("Белые") || player.Contains("White"))
                {
                    CurrentPlayerText.Foreground = System.Windows.Media.Brushes.White;
                }
                else
                {
                    CurrentPlayerText.Foreground = System.Windows.Media.Brushes.Black;
                }
            });
        }

        // Метод для обновления статуса хода ИИ
        public void UpdateAIThinking(bool isThinking)
        {
            if (AITurnIndicator == null || StatusText == null)
                return;

            Dispatcher.Invoke(() =>
            {
                if (isThinking)
                {
                    AITurnIndicator.Visibility = Visibility.Visible;
                    StatusText.Text = "ИИ обдумывает ход...";
                }
                else
                {
                    AITurnIndicator.Visibility = Visibility.Collapsed;
                    StatusText.Text = "Ваш ход";
                }
            });
        }

        // Метод для создания онлайн игры
        private async void CreateOnlineGame()
        {
            try
            {
                var createDto = new CreateGameDto
                {
                    WhitePlayerId = _userId,
                    GameMode = GameModeComboBox.SelectedIndex == 1 ? "HumanVsAI" : "HumanVsHuman",
                    Difficulty = DifficultyComboBox.Text
                };

                var game = await _apiService.CreateGameAsync(createDto);
                if (game != null)
                {
                    _currentGameId = game.Id;

                    if (StatusText != null)
                        StatusText.Text = $"Онлайн игра создана! ID: {game.Id}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания игры: {ex.Message}");
            }
        }
    }
}