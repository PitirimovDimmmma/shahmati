using shahmati.Helpers;
using shahmati.models;
using shahmati.Models;
using shahmati.Services;
using shahmati.ViewModels;
using shahmati.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace shahmati
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly MainViewModel _viewModel;
        private readonly int _userId;
        private int _currentGameId;
        private string _currentDifficulty = "Medium";
        private string _opponentName = "Stockfish AI";

        // Таймеры
        private DispatcherTimer _whiteTimer;
        private DispatcherTimer _blackTimer;
        private TimeSpan _whiteTimeLeft;
        private TimeSpan _blackTimeLeft;
        private DispatcherTimer _gameTimer;
        private DateTime _gameStartTime;

        public MainWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _apiService = new ApiService();
            _viewModel = new MainViewModel(userId);
            DataContext = _viewModel;

            // ПОДПИСКА НА СОБЫТИЯ VIEWMODEL
            _viewModel.PlayerTurnChanged += OnPlayerTurnChanged;
            _viewModel.AIMoveStarted += OnAIMoveStarted;
            _viewModel.AIMoveCompleted += OnAIMoveCompleted;
            _viewModel.GameFinished += OnGameFinishedHandler;

            // Инициализация таймеров
            InitializeChessTimers();

            Loaded += async (s, e) => await InitializeGameAsync();
        }

        private async Task InitializeGameAsync()
        {
            try
            {
                ShowLoadingIndicator("Загрузка данных пользователя...");

                // Загружаем пользователя
                await LoadUserDataAsync();

                // Проверяем API
                bool isConnected = await _apiService.TestConnectionAsync();
                if (!isConnected)
                {
                    MessageBox.Show("⚠️ Сервер недоступен. Игра будет работать в локальном режиме.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                HideLoadingIndicator();

                // Автоматически начинаем игру
                await StartNewGameWithAIAsync();
            }
            catch (Exception ex)
            {
                HideLoadingIndicator();
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task StartNewGameWithAIAsync()
        {
            try
            {
                ShowLoadingIndicator("Создание игры с Stockfish AI...");

                // Инициализируем GameManager с API
                _viewModel.GameManager.InitializeApiService(_apiService);

                // Устанавливаем цвет игрока
                _viewModel.SetUserIsWhite(true); // Пользователь играет белыми
                _viewModel.Difficulty = _currentDifficulty; // Устанавливаем сложность

                // Запускаем новую игру - БЕЗ ПАРАМЕТРОВ
                await _viewModel.StartNewGameAsync();

                // Создаем игру на сервере
                _currentGameId = await _viewModel.GameManager.CreateAIGameOnServerAsync(
                    _userId,
                    _currentDifficulty,
                    "White" // Пользователь играет белыми
                );

                Console.WriteLine($"✅ Игра создана с ID: {_currentGameId}");

                HideLoadingIndicator();

                // Показываем уведомление
                ShowGameStartNotification();

                // Запускаем таймеры
                StartGameTimers();

                // Обновляем UI
                UpdateUIForNewGame();
            }
            catch (Exception ex)
            {
                HideLoadingIndicator();
                MessageBox.Show($"Ошибка создания игры: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateUIForNewGame()
        {
            MovesCountText.Text = "0";
            StatusText.Text = "Ваш ход. Вы играете белыми против Stockfish AI.";
            StatusIcon.Text = "♔";
            OpponentColorText.Text = "ЧЕРНЫЕ (Stockfish AI)";
            GameInfoText.Text = $"Противник: Stockfish AI\nУровень: {GetDifficultyName(_currentDifficulty)}";
        }

        private void ShowGameStartNotification()
        {
            GameStartNotification.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };
            GameStartNotification.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private string GetDifficultyName(string difficulty)
        {
            return difficulty switch
            {
                "Beginner" => "Новичок",
                "Easy" => "Легкий",
                "Medium" => "Средний",
                "Hard" => "Сложный",
                "Expert" => "Эксперт",
                _ => "Средний"
            };
        }

        // ===== ЗАГРУЗКА ДАННЫХ ПОЛЬЗОВАТЕЛЯ =====
        private async Task LoadUserDataAsync()
        {
            try
            {
                var user = await _apiService.GetUserAsync(_userId);
                if (user != null)
                {
                    UserNameText.Text = user.Profile?.Nickname ?? user.Username;
                    await LoadUserAvatarAsync(user);
                    await LoadUserRatingAsync(user);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки пользователя: {ex.Message}");
                SetDefaultUserData();
            }
        }

        private async Task LoadUserAvatarAsync(UserWithProfileDto user)
        {
            try
            {
                string photoPath = user.Profile?.PhotoPath;
                if (!string.IsNullOrEmpty(photoPath))
                {
                    UserAvatar.Source = new BitmapImage(new Uri(photoPath));
                }
                else
                {
                    SetDefaultAvatar();
                }
            }
            catch
            {
                SetDefaultAvatar();
            }
        }

        private async Task LoadUserRatingAsync(UserWithProfileDto user)
        {
            try
            {
                var stats = await _apiService.GetUserStatsAsync(_userId);
                int rating = stats?.CurrentRating ?? user.Profile?.Rating ?? 1200;
                UserRatingText.Text = $"Рейтинг: {rating}";
                RatingText.Text = rating.ToString();
            }
            catch
            {
                int rating = user.Profile?.Rating ?? 1200;
                UserRatingText.Text = $"Рейтинг: {rating}";
                RatingText.Text = rating.ToString();
            }
        }

        private void SetDefaultUserData()
        {
            UserNameText.Text = "Гость";
            UserRatingText.Text = "Рейтинг: 1200";
            RatingText.Text = "1200";
            SetDefaultAvatar();
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
                UserAvatar.Source = null;
            }
        }

        // ===== ТАЙМЕРЫ =====
        private void InitializeChessTimers()
        {
            _whiteTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _blackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            _whiteTimer.Tick += WhiteTimer_Tick;
            _blackTimer.Tick += BlackTimer_Tick;
            _gameTimer.Tick += GameTimer_Tick;

            _whiteTimeLeft = TimeSpan.FromMinutes(10);
            _blackTimeLeft = TimeSpan.FromMinutes(10);
            UpdateTimerDisplays();
        }

        private void StartGameTimers()
        {
            _gameStartTime = DateTime.Now;
            _gameTimer.Start();
            _whiteTimer.Start();
        }

        private void StopAllTimers()
        {
            _gameTimer.Stop();
            _whiteTimer.Stop();
            _blackTimer.Stop();
        }

        private void WhiteTimer_Tick(object sender, EventArgs e)
        {
            if (_whiteTimeLeft > TimeSpan.Zero)
            {
                _whiteTimeLeft = _whiteTimeLeft.Subtract(TimeSpan.FromSeconds(1));
                UpdateTimerDisplays();
                if (_whiteTimeLeft <= TimeSpan.Zero)
                {
                    _whiteTimer.Stop();
                    _ = _viewModel.GameManager?.ResignAsync(PieceColor.White);
                }
            }
        }

        private void BlackTimer_Tick(object sender, EventArgs e)
        {
            if (_blackTimeLeft > TimeSpan.Zero)
            {
                _blackTimeLeft = _blackTimeLeft.Subtract(TimeSpan.FromSeconds(1));
                UpdateTimerDisplays();
                if (_blackTimeLeft <= TimeSpan.Zero)
                {
                    _blackTimer.Stop();
                    OnGameFinishedHandler("Победа белых! Время вышло у черных.");
                }
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _gameStartTime;
            GameTimeText.Text = $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }

        private void UpdateTimerDisplays()
        {
            WhiteTimerText.Text = $"{(int)_whiteTimeLeft.TotalMinutes:00}:{_whiteTimeLeft.Seconds:00}";
            BlackTimerText.Text = $"{(int)_blackTimeLeft.TotalMinutes:00}:{_blackTimeLeft.Seconds:00}";
        }

        // ===== ОБРАБОТЧИКИ СОБЫТИЙ ИЗ VIEWMODEL =====
        public void OnPlayerTurnChanged(string playerColor)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateCurrentPlayer(playerColor);
            });
        }

        public void OnAIMoveStarted(string message)
        {
            Dispatcher.Invoke(() =>
            {
                AITurnIndicator.Visibility = Visibility.Visible;
                StatusText.Text = "Stockfish AI думает...";
                StatusIcon.Text = "🤖";
            });
        }

        public void OnAIMoveCompleted(string move)
        {
            Dispatcher.Invoke(() =>
            {
                AITurnIndicator.Visibility = Visibility.Collapsed;
                StatusText.Text = _viewModel?.GameManager?.UserIsWhite == true
                    ? "Ваш ход (Белые)"
                    : "Ход ИИ";
                StatusIcon.Text = _viewModel?.GameManager?.CurrentPlayer == PieceColor.White ? "♔" : "♚";

                // Обновляем счетчик ходов
                if (_viewModel?.GameManager?.MoveHistory != null)
                {
                    MovesCountText.Text = _viewModel.GameManager.MoveHistory.Count.ToString();
                }
            });
        }

        public void OnGameFinishedHandler(string result)
        {
            Dispatcher.Invoke(() =>
            {
                StopAllTimers();

                bool whiteWon = result.Contains("Победа белых") || result.Contains("White wins");
                bool isDraw = result.Contains("Ничья") || result.Contains("Draw");

                string message = whiteWon
                    ? $"🎉 ПОБЕДА! +15 рейтинга"
                    : isDraw
                        ? $"🤝 НИЧЬЯ! Рейтинг не изменился"
                        : $"😔 ПОРАЖЕНИЕ -10 рейтинга";

                MessageBox.Show(message, "Игра окончена", MessageBoxButton.OK,
                    whiteWon ? MessageBoxImage.Exclamation :
                    isDraw ? MessageBoxImage.Information : MessageBoxImage.Exclamation);

                GameOverPanel.Visibility = Visibility.Visible;
                GameResultDescription.Text = result;
                StatusText.Text = result;
                StatusIcon.Text = "🏁";

                // Обновляем рейтинг в UI
                _ = UpdateRatingUIAsync();
            });
        }

        public void UpdateCurrentPlayer(string player)
        {
            if (CurrentPlayerText == null) return;

            Dispatcher.Invoke(() =>
            {
                if (player.Contains("Ваш ход") || player.Contains("Белые"))
                {
                    CurrentPlayerText.Text = "ВАШ ХОД (БЕЛЫЕ)";
                    CurrentPlayerText.Foreground = Brushes.White;
                    if (CurrentPlayerText.Parent is Border border)
                        border.Background = new SolidColorBrush(Color.FromRgb(0, 100, 0));

                    _whiteTimer.Start();
                    _blackTimer.Stop();
                }
                else
                {
                    CurrentPlayerText.Text = $"{_opponentName.ToUpper()} (ЧЕРНЫЕ)";
                    CurrentPlayerText.Foreground = Brushes.White;
                    if (CurrentPlayerText.Parent is Border border)
                        border.Background = new SolidColorBrush(Color.FromRgb(139, 0, 0));

                    _blackTimer.Start();
                    _whiteTimer.Stop();
                }
            });
        }

        public void SwitchTurn(string newPlayer) => UpdateCurrentPlayer(newPlayer);

        private async Task UpdateRatingUIAsync()
        {
            try
            {
                var stats = await _apiService.GetUserStatsAsync(_userId);
                if (stats != null)
                {
                    RatingText.Text = stats.CurrentRating.ToString();
                    UserRatingText.Text = $"Рейтинг: {stats.CurrentRating}";
                }
            }
            catch { }
        }

        // ===== ПОКАЗ/СКРЫТИЕ ИНДИКАТОРА =====
        private void ShowLoadingIndicator(string message)
        {
            if (_loadingPanel == null) CreateLoadingIndicator();
            _loadingPanel.Visibility = Visibility.Visible;
            _loadingText.Text = message;
        }

        private void HideLoadingIndicator()
        {
            if (_loadingPanel != null)
                _loadingPanel.Visibility = Visibility.Collapsed;
        }

        private void CreateLoadingIndicator()
        {
            _loadingPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var stackPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _loadingSpinner = new ProgressBar
            {
                Width = 50,
                Height = 50,
                IsIndeterminate = true,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _loadingText = new TextBlock
            {
                Text = "Загрузка...",
                Foreground = Brushes.White,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stackPanel.Children.Add(_loadingSpinner);
            stackPanel.Children.Add(_loadingText);
            _loadingPanel.Child = stackPanel;
            MainGrid.Children.Add(_loadingPanel);
        }

        // ===== КНОПКИ =====
        private async void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Начать новую игру против Stockfish AI?",
                "Новая игра", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                GameOverPanel.Visibility = Visibility.Collapsed;
                _whiteTimeLeft = TimeSpan.FromMinutes(10);
                _blackTimeLeft = TimeSpan.FromMinutes(10);
                UpdateTimerDisplays();
                await StartNewGameWithAIAsync();
            }
        }

        private async void ResignButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Сдаться? Это -10 рейтинга.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes && _viewModel?.GameManager != null)
            {
                await _viewModel.GameManager.ResignAsync(PieceColor.White);
            }
        }

        private async void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel?.GameManager?.IsGameInProgress == true)
            {
                var result = MessageBox.Show("Выйти в главное меню? Это поражение.",
                    "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_currentGameId > 0)
                        await _apiService.FinishGameAsync(_currentGameId, "Black");

                    new DashboardWindow(_userId).Show();
                    Close();
                }
            }
            else
            {
                new DashboardWindow(_userId).Show();
                Close();
            }
        }

        private void CloseNotification_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            fadeOut.Completed += (s, args) => GameStartNotification.Visibility = Visibility.Collapsed;
            GameStartNotification.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void ViewStatsButton_Click(object sender, RoutedEventArgs e)
        {
            new StatisticsWindow(_userId).Show();
        }

        private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            new HistoryWindow(_userId).Show();
        }

        private void NewGameAfterButton_Click(object sender, RoutedEventArgs e)
        {
            NewGameButton_Click(sender, e);
        }

        private void HighlightMovesToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
                _viewModel.EnableMoveHighlighting = true;
        }

        private void HighlightMovesToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
                _viewModel.EnableMoveHighlighting = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Очистка ресурсов
        }

        // Элементы UI - объявляем их здесь для доступа
        private Border _loadingPanel;
        private ProgressBar _loadingSpinner;
        private TextBlock _loadingText;
    }
}