using shahmati.Models;
using shahmati.Services;
using shahmati.ViewModels;
using shahmati.Views;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;
using System.Threading.Tasks;
using shahmati.models;
using System.Linq;
using System.Windows.Media;
using shahmati.Helpers;

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

        // Список ходов для истории
        private List<string> _gameMoves;

        // Переменные для отслеживания цветов игроков
        private bool _userIsWhite = true; // Пользователь всегда белые
        private string _opponentName = "Гость"; // Имя противника
        private bool _isPlayingVsAI = false;

        // Константы изменения рейтинга
        private const int RATING_WIN_CHANGE = 15;
        private const int RATING_LOSS_CHANGE = -10;
        private const int RATING_DRAW_CHANGE = 0;

        public MainWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _apiService = new ApiService();
            _viewModel = new MainViewModel(userId);
            DataContext = _viewModel;

            // Устанавливаем, что пользователь всегда белые
            _viewModel.SetUserIsWhite(true);
            _viewModel.PlayerTurnChanged += OnPlayerTurnChanged;

            // Подписываемся на события GameManager
            if (_viewModel.GameManager != null)
            {
                _viewModel.GameManager.GameFinished += OnGameFinished;
                _viewModel.GameManager.MoveMade += OnMoveMade;
                _viewModel.GameManager.UpdateHistoryCallback = UpdateMoveHistoryInUI;
                _viewModel.GameManager.UserIsWhite = true; // Пользователь всегда белые
            }

            // Инициализируем таймеры
            InitializeChessTimers();
            InitializeGameTimer();

            // Инициализируем список ходов
            _gameMoves = new List<string>();

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
            // Настройка таймера белых (пользователя)
            _whiteTimer = new DispatcherTimer();
            _whiteTimer.Interval = TimeSpan.FromSeconds(1);
            _whiteTimer.Tick += WhiteTimer_Tick;

            // Настройка таймера черных (противника)
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

                // Устанавливаем имя противника в зависимости от режима игры
                SetOpponentName();

                // Показываем уведомление о начале игры
                ShowGameStartNotification();

                // Запускаем игру
                _viewModel.StartNewGame();
                StartGameTimers();

                // Создаем новую игру в базе данных
                await CreateNewOnlineGame();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}");
            }
        }

        private void SetOpponentName()
        {
            _isPlayingVsAI = GameModeComboBox.SelectedIndex == 1;

            if (_isPlayingVsAI)
            {
                string difficulty = DifficultyComboBox?.SelectedItem?.ToString() ?? "Средний";
                _opponentName = $"ИИ ({difficulty})";
            }
            else
            {
                _opponentName = "Гость";
            }

            // Обновляем текст для противника в уведомлении
            OpponentColorText.Text = _isPlayingVsAI ? "ИИ" : "ГОСТЬ";
        }

        private void ShowGameStartNotification()
        {
            // Формируем текст уведомления
            string gameMode = _isPlayingVsAI ? "против ИИ" : "против Гостя";
            string difficultyText = "";

            if (_isPlayingVsAI)
            {
                string difficulty = DifficultyComboBox?.SelectedItem?.ToString() ?? "Средний";
                difficultyText = $"\nУровень сложности: {difficulty}";
            }

            GameInfoText.Text = $"Режим: {gameMode}{difficultyText}\n" +
                               $"Противник играет ЧЕРНЫМИ\n" +
                               $"Вы играете БЕЛЫМИ";

            // Анимация появления
            GameStartNotification.Visibility = Visibility.Visible;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            var scaleIn = new DoubleAnimation
            {
                From = 0.8,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            var translateIn = new DoubleAnimation
            {
                From = -20,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform());
            transformGroup.Children.Add(new TranslateTransform());
            GameStartNotification.RenderTransform = transformGroup;

            GameStartNotification.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            GameStartNotification.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleIn);
            GameStartNotification.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleIn);
            GameStartNotification.RenderTransform.BeginAnimation(TranslateTransform.YProperty, translateIn);
        }

        private void CloseNotification_Click(object sender, RoutedEventArgs e)
        {
            // Анимация исчезновения
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            fadeOut.Completed += (s, args) =>
            {
                GameStartNotification.Visibility = Visibility.Collapsed;

                // После закрытия уведомления показываем статус игры
                if (StatusText != null)
                {
                    StatusText.Text = "Игра начата! Вы играете БЕЛЫМИ.";
                    StatusIcon.Text = "♔";
                }
            };

            GameStartNotification.BeginAnimation(UIElement.OpacityProperty, fadeOut);
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

            _isPlayingVsAI = GameModeComboBox.SelectedIndex == 1;

            if (_isPlayingVsAI)
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

        private async Task CreateNewOnlineGame()
        {
            try
            {
                string gameMode = _isPlayingVsAI ? "HumanVsAI" : "HumanVsHuman";
                string difficulty = DifficultyComboBox?.SelectedItem?.ToString() ?? "Medium";

                Console.WriteLine($"=== СОЗДАНИЕ ИГРЫ ===");
                Console.WriteLine($"Режим: {gameMode}");
                Console.WriteLine($"Сложность: {difficulty}");

                // Для игры против ИИ или против человека (за одним компьютером)
                // В обоих случаях BlackPlayerId = null
                var createDto = new CreateGameDto
                {
                    WhitePlayerId = _userId,      // Текущий пользователь - белые
                    BlackPlayerId = null,         // null для гостя или ИИ
                    GameMode = gameMode,
                    Difficulty = difficulty
                };

                var game = await _apiService.CreateGameAsync(createDto);
                if (game != null)
                {
                    _currentGameId = game.Id;

                    // Записываем первый ход (начало игры)
                    _gameMoves.Clear();
                    _gameMoves.Add("Game Started");

                    UpdateGameHistory();

                    if (StatusText != null)
                        StatusText.Text = $"Игра #{game.Id} начата! Вы играете белыми.";

                    Console.WriteLine($"✅ Игра создана: ID={game.Id}");
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось создать игру в API");
                    if (StatusText != null)
                        StatusText.Text = "Игра создана локально (офлайн режим)";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания онлайн игры: {ex.Message}");
                // Игра продолжается в локальном режиме
                if (StatusText != null)
                    StatusText.Text = "Офлайн игра (без сохранения)";
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

        private void OnPlayerTurnChanged(string playerColor)
        {
            Dispatcher.Invoke(() =>
            {
                // Преобразуем цвета для пользователя
                string displayColor = playerColor;
                if (playerColor.Contains("Белые") || playerColor.Contains("White"))
                {
                    displayColor = "Ваш ход (Белые)";
                }
                else if (playerColor.Contains("Черные") || playerColor.Contains("Black"))
                {
                    displayColor = $"{_opponentName} (Черные)";
                }

                // Обновляем UI
                UpdateCurrentPlayer(displayColor);
                SwitchTurn(displayColor);
            });
        }

        // ===== ОБРАБОТЧИКИ ИГРОВЫХ СОБЫТИЙ =====

        private async void OnGameFinished(string result)
        {
            await Dispatcher.Invoke(async () =>
            {
                try
                {
                    // Останавливаем все таймеры
                    _gameTimer.Stop();
                    _whiteTimer.Stop();
                    _blackTimer.Stop();

                    // Обновляем статус в UI
                    if (StatusText != null)
                        StatusText.Text = result;

                    if (StatusIcon != null)
                        StatusIcon.Text = "🏁";

                    // Определяем, выиграли ли белые (пользователь)
                    bool whiteWon = result.Contains("Победа белых") || result.Contains("Черные сдались");
                    bool isDraw = result.Contains("Ничья");
                    string apiResult = isDraw ? "Draw" : (whiteWon ? "White" : "Black");

                    // Рассчитываем изменение рейтинга для белых (пользователя)
                    int ratingChange = 0;
                    if (whiteWon)
                        ratingChange = RATING_WIN_CHANGE;
                    else if (!isDraw)
                        ratingChange = RATING_LOSS_CHANGE;

                    // Показываем сообщение о результате
                    string message = "";
                    string title = "";

                    if (whiteWon)
                    {
                        message = $"🎉 ПОБЕДА БЕЛЫХ! 🎉\n\n" +
                                 $"Вы выиграли партию!\n" +
                                 $"Противник: {_opponentName}\n" +
                                 $"Ваш рейтинг увеличен на {RATING_WIN_CHANGE} очков";
                        title = "Поздравляем!";
                    }
                    else if (isDraw)
                    {
                        message = $"🤝 НИЧЬЯ! 🤝\n\n" +
                                 $"Партия закончилась вничью\n" +
                                 $"Противник: {_opponentName}\n" +
                                 $"Ваш рейтинг не изменился";
                        title = "Ничья";
                    }
                    else
                    {
                        message = $"😔 ПОРАЖЕНИЕ БЕЛЫХ\n\n" +
                                 $"Вы проиграли партию\n" +
                                 $"Противник: {_opponentName}\n" +
                                 $"Ваш рейтинг уменьшен на {Math.Abs(RATING_LOSS_CHANGE)} очков";
                        title = "Сожалеем";
                    }

                    MessageBox.Show(message, title, MessageBoxButton.OK,
                        whiteWon ? MessageBoxImage.Exclamation :
                        isDraw ? MessageBoxImage.Information : MessageBoxImage.Exclamation);

                    // Сохраняем игру в базу данных (только для белых)
                    await SaveGameToDatabase(apiResult, result, ratingChange);

                    // Обновляем рейтинг пользователя
                    if (ratingChange != 0)
                    {
                        await UpdateUserRating(ratingChange);
                    }

                    // Показываем панель с результатом
                    ShowGameResultButtons(whiteWon, isDraw);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при завершении игры: {ex.Message}");
                }
            });
        }

        private void ShowGameResultButtons(bool whiteWon, bool isDraw)
        {
            GameOverPanel.Visibility = Visibility.Visible;

            if (whiteWon)
            {
                GameResultDescription.Text = $"Поздравляем с победой!\n" +
                                            $"Вы выиграли у {_opponentName}\n" +
                                            $"Ваш рейтинг увеличен на {RATING_WIN_CHANGE} очков";
            }
            else if (isDraw)
            {
                GameResultDescription.Text = $"Партия закончилась вничью\n" +
                                            $"Противник: {_opponentName}\n" +
                                            $"Рейтинг не изменился";
            }
            else
            {
                GameResultDescription.Text = $"Вы проиграли {_opponentName}\n" +
                                            $"Ваш рейтинг уменьшен на {Math.Abs(RATING_LOSS_CHANGE)} очков";
            }
        }

        private void OnMoveMade(string moveNotation)
        {
            Dispatcher.Invoke(() =>
            {
                // Добавляем ход в список
                _gameMoves.Add(moveNotation);

                // Обновляем историю в UI
                UpdateGameHistory();

                // Обновляем счетчик ходов
                if (MovesCountText != null)
                    MovesCountText.Text = _gameMoves.Count.ToString();
            });
        }

        private void UpdateMoveHistoryInUI(string moves)
        {
            Dispatcher.Invoke(() =>
            {
                if (MoveHistoryList != null)
                {
                    MoveHistoryList.Items.Clear();

                    var moveList = moves.Split('\n');
                    foreach (var move in moveList)
                    {
                        if (!string.IsNullOrEmpty(move))
                            MoveHistoryList.Items.Add(move);
                    }

                    if (MoveHistoryList.Items.Count > 0)
                        MoveHistoryList.ScrollIntoView(MoveHistoryList.Items[MoveHistoryList.Items.Count - 1]);
                }
            });
        }

        private void UpdateGameHistory()
        {
            if (MoveHistoryList != null)
            {
                MoveHistoryList.Items.Clear();
                foreach (var move in _gameMoves)
                {
                    MoveHistoryList.Items.Add(move);
                }

                if (MoveHistoryList.Items.Count > 0)
                    MoveHistoryList.ScrollIntoView(MoveHistoryList.Items[MoveHistoryList.Items.Count - 1]);
            }
        }

        private async Task SaveGameToDatabase(string result, string description, int ratingChange)
        {
            try
            {
                if (_currentGameId > 0)
                {
                    Console.WriteLine($"=== СОХРАНЕНИЕ ИГРЫ ===");
                    Console.WriteLine($"GameId: {_currentGameId}");
                    Console.WriteLine($"Result: {result}");
                    Console.WriteLine($"Description: {description}");
                    Console.WriteLine($"RatingChange: {ratingChange}");

                    // 1. Завершаем игру на сервере
                    bool gameFinished = await _apiService.FinishGameAsync(_currentGameId, result);

                    if (gameFinished)
                    {
                        Console.WriteLine($"✅ Игра #{_currentGameId} завершена на сервере");

                        // 2. Обновляем рейтинг пользователя (если нужно)
                        if (ratingChange != 0)
                        {
                            // Дождитесь немного, чтобы сервер успел обработать игру
                            await Task.Delay(500);

                            // Обновляем рейтинг через отдельный эндпоинт
                            bool ratingUpdated = await _apiService.UpdateUserRatingAsync(_userId, ratingChange);

                            if (ratingUpdated)
                            {
                                Console.WriteLine($"✅ Рейтинг обновлен на {ratingChange}");
                            }
                            else
                            {
                                Console.WriteLine($"⚠️ Рейтинг не обновлен на сервере");
                            }
                        }

                        // 3. Обновляем статистику пользователя
                        await UpdateRatingUI();
                    }
                    else
                    {
                        Console.WriteLine($"❌ Не удалось завершить игру #{_currentGameId} на сервере");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Локальная игра, не сохраняем в БД");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка сохранения игры: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        private async Task UpdateUserRating(int ratingChange)
        {
            try
            {
                if (ratingChange != 0)
                {
                    Console.WriteLine($"=== ОБНОВЛЕНИЕ РЕЙТИНГА ===");
                    Console.WriteLine($"Изменение: {ratingChange}");

                    // 1. Получаем текущий рейтинг из UI
                    int currentRating = 0;
                    if (RatingText != null && int.TryParse(RatingText.Text, out int parsedRating))
                    {
                        currentRating = parsedRating;
                    }

                    int newRating = currentRating + ratingChange;

                    // 2. Обновляем рейтинг на сервере
                    bool success = await _apiService.UpdateUserRatingAsync(_userId, ratingChange);

                    if (success)
                    {
                        Console.WriteLine($"✅ Рейтинг обновлен на сервере");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Не удалось обновить рейтинг на сервере");
                    }

                    // 3. Обновляем локальный UI
                    await UpdateRatingUI();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления рейтинга: {ex.Message}");
            }
        }

        private async Task UpdateRatingUI()
        {
            try
            {
                // Обновляем статистику с сервера
                var stats = await _apiService.GetUserStatsAsync(_userId);
                if (stats != null)
                {
                    if (RatingText != null)
                        RatingText.Text = stats.CurrentRating.ToString();

                    if (UserRatingText != null)
                        UserRatingText.Text = $"Рейтинг: {stats.CurrentRating}";

                    Console.WriteLine($"✅ UI обновлен: рейтинг = {stats.CurrentRating}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления UI: {ex.Message}");
            }
        }

 

        private void ShowGameResultButtons(string result, bool userWon, bool isDraw)
        {
            // Показываем GameOverPanel вместо динамического создания
            GameOverPanel.Visibility = Visibility.Visible;

            if (userWon)
            {
                GameResultDescription.Text = $"Поздравляем с победой!\nВы выиграли у {_opponentName}\nВаш рейтинг увеличен на {RATING_WIN_CHANGE} очков";
            }
            else if (isDraw)
            {
                GameResultDescription.Text = $"Партия закончилась вничью\nПротивник: {_opponentName}\nРейтинг не изменился";
            }
            else
            {
                GameResultDescription.Text = $"Вы проиграли {_opponentName}\nВаш рейтинг уменьшен на {Math.Abs(RATING_LOSS_CHANGE)} очков";
            }
        }

        private void ShowStatistics()
        {
            StatisticsWindow statsWindow = new StatisticsWindow(_userId);
            statsWindow.Show();
        }

        private void GameOverByTimeout(string winner, string reason)
        {
            _gameTimer.Stop();
            _whiteTimer.Stop();
            _blackTimer.Stop();

            string resultMessage = $"Игра окончена! {winner} победили ({reason})";

            if (StatusText != null)
                StatusText.Text = resultMessage;

            if (StatusIcon != null)
                StatusIcon.Text = "⏰";

            // Определяем, выиграл ли пользователь (всегда белые)
            bool userWon = winner.Contains("Белые");
            OnGameFinished(resultMessage);
        }

        public void SwitchTurn(string newPlayer)
        {
            // Останавливаем все таймеры
            _whiteTimer.Stop();
            _blackTimer.Stop();

            if (newPlayer.Contains("Ваш ход"))
            {
                // Запускаем таймер белых (пользователя)
                _whiteTimer.Start();

                if (CurrentPlayerText != null)
                {
                    CurrentPlayerText.Text = "ВАШ ХОД (БЕЛЫЕ)";
                    CurrentPlayerText.Foreground = Brushes.White;

                    // Устанавливаем фон через родительский Border
                    if (CurrentPlayerText.Parent is Border border)
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(0, 100, 0));
                    }
                }
            }
            else
            {
                // Запускаем таймер черных (противника)
                _blackTimer.Start();

                if (CurrentPlayerText != null)
                {
                    CurrentPlayerText.Text = $"{_opponentName.ToUpper()} (ЧЕРНЫЕ)";
                    CurrentPlayerText.Foreground = Brushes.White;

                    // Устанавливаем фон через родительский Border
                    if (CurrentPlayerText.Parent is Border border)
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(139, 0, 0));
                    }
                }
            }

            // Обновляем статус
            if (StatusText != null)
            {
                if (newPlayer.Contains("Ваш ход"))
                {
                    StatusText.Text = "Ваш ход. Вы играете белыми.";
                    StatusIcon.Text = "⚪";
                }
                else
                {
                    if (_isPlayingVsAI)
                        StatusText.Text = $"{_opponentName} обдумывает ход...";
                    else
                        StatusText.Text = $"Ход {_opponentName} (черные)";
                    StatusIcon.Text = "⚫";
                }
            }
        }

        // ===== ОБРАБОТЧИКИ СОБЫТИЙ UI =====

        private void GameModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGameModeUI();
            SetOpponentName(); // Обновляем имя противника

            if (_isPlayingVsAI)
            {
                if (AITurnIndicator != null)
                    AITurnIndicator.Visibility = Visibility.Visible;

                if (StatusText != null)
                    StatusText.Text = "Игра против ИИ. Вы играете белыми.";

                if (StatusIcon != null)
                    StatusIcon.Text = "🤖";
            }
            else
            {
                if (AITurnIndicator != null)
                    AITurnIndicator.Visibility = Visibility.Collapsed;

                if (StatusText != null)
                    StatusText.Text = "Игра против гостя. Вы играете белыми.";

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
            SetOpponentName(); // Обновляем имя противника с новым уровнем сложности
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
            var result = MessageBox.Show("Завершить текущую игру?\n\n" +
                                       "Это засчитается как поражение (-10 рейтинга)",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await FinishCurrentGame("Игрок завершил игру досрочно");
            }
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Начать новую игру?\n\nТекущая игра будет завершена.",
                "Новая игра",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Скрываем GameOverPanel если он виден
                GameOverPanel.Visibility = Visibility.Collapsed;

                // Завершаем текущую игру
                if (_viewModel.GameManager != null && _viewModel.GameManager.IsGameInProgress)
                {
                    OnGameFinished("Игра прервана для начала новой");
                }

                // Начинаем новую игру
                _viewModel.StartNewGame();
                _currentGameId = 0;

                // Сбрасываем таймеры и счетчики
                SetTimersBasedOnDifficulty();
                _gameStartTime = DateTime.Now;
                _whiteTimer.Start();

                if (MovesCountText != null)
                    MovesCountText.Text = "0";

                // Показываем уведомление о начале новой игры
                ShowGameStartNotification();

                // Создаем новую онлайн игру
                _ = CreateNewOnlineGame();
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Начать игру заново?\n\nВсе ходы будут сброшены.",
                "Начать заново",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Скрываем GameOverPanel если он виден
                GameOverPanel.Visibility = Visibility.Collapsed;

                // Завершаем текущую игру
                if (_viewModel.GameManager != null && _viewModel.GameManager.IsGameInProgress)
                {
                    OnGameFinished("Игра перезапущена");
                }

                // Начинаем новую игру
                _viewModel.StartNewGame();

                SetTimersBasedOnDifficulty();
                _gameStartTime = DateTime.Now;
                _whiteTimer.Start();

                if (MovesCountText != null)
                    MovesCountText.Text = "0";

                // Показываем уведомление о начале новой игры
                ShowGameStartNotification();

                // Создаем новую онлайн игру
                _ = CreateNewOnlineGame();
            }
        }

        private async void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGameId != 0 && _viewModel?.GameManager?.IsGameInProgress == true)
            {
                var result = MessageBox.Show("Завершить текущую игру и вернуться на главную?\n\n" +
                                           "Это засчитается как поражение (-10 рейтинга)",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await FinishCurrentGame("Игрок вышел из игры");
                }
            }

            DashboardWindow dashboardWindow = new DashboardWindow(_userId);
            dashboardWindow.Show();
            this.Close();
        }

        private async Task FinishCurrentGame(string result)
        {
            try
            {
                Console.WriteLine($"=== ЗАВЕРШЕНИЕ ИГРЫ ПРИ ВЫХОДЕ ===");

                if (_currentGameId != 0 && _viewModel?.GameManager?.IsGameInProgress == true)
                {
                    // 1. Сохраняем игру как поражение
                    await SaveGameToDatabase("Black", "Игра завершена досрочно", RATING_LOSS_CHANGE);

                    // 2. Обновляем рейтинг
                    await UpdateUserRating(RATING_LOSS_CHANGE);

                    _currentGameId = 0;

                    Console.WriteLine($"✅ Игра завершена, рейтинг обновлен");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка завершения игры: {ex.Message}");
                MessageBox.Show($"Ошибка завершения игры: {ex.Message}");
            }
            finally
            {
                // Всегда останавливаем таймеры
                _gameTimer.Stop();
                _whiteTimer.Stop();
                _blackTimer.Stop();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Вызываем асинхронный метод завершения игры
            if (_viewModel.GameManager != null && _viewModel.GameManager.IsGameInProgress)
            {
                var result = MessageBox.Show("Завершить текущую игру перед выходом?\n\n" +
                                           "Это засчитается как поражение (-10 рейтинга)",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    OnGameFinished("Игра завершена при выходе из приложения");
                }
                else
                {
                    e.Cancel = true; // Отменяем закрытие окна
                }
            }
        }



        // Метод для обновления текущего игрока
        public void UpdateCurrentPlayer(string player)
        {
            if (CurrentPlayerText == null)
                return;

            Dispatcher.Invoke(() =>
            {
                if (player.Contains("Ваш ход"))
                {
                    CurrentPlayerText.Text = "ВАШ ХОД (БЕЛЫЕ)";
                    CurrentPlayerText.Foreground = Brushes.White;
                }
                else if (player.Contains("Черные"))
                {
                    CurrentPlayerText.Text = $"{_opponentName.ToUpper()} (ЧЕРНЫЕ)";
                    CurrentPlayerText.Foreground = Brushes.White;
                }
                else
                {
                    CurrentPlayerText.Text = player.ToUpper();
                }
            });
        }

        // Метод для обновления статуса хода ИИ
        public void UpdateAIThinking(bool isThinking)
        {
            Dispatcher.Invoke(() =>
            {
                if (isThinking)
                {
                    if (AITurnIndicator != null)
                        AITurnIndicator.Visibility = Visibility.Visible;
                    if (StatusText != null)
                        StatusText.Text = "ИИ обдумывает ход...";
                }
                else
                {
                    if (AITurnIndicator != null)
                        AITurnIndicator.Visibility = Visibility.Collapsed;
                    if (StatusText != null)
                        StatusText.Text = "Ваш ход";
                }
            });
        }

        // ===== КНОПКА СДАЧИ =====

        private async void ResignButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите сдаться?\n\n" +
                                       "Это засчитается как поражение (-10 рейтинга)",
                "Сдача",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes && _viewModel?.GameManager != null)
            {
                // Пользователь всегда белые, поэтому сдача белых
                _viewModel.GameManager.Resign(PieceColor.White);

                // ДОБАВЬТЕ: Вызов метода завершения игры
                await FinishGameManually("Black", "Игрок сдался");
            }
        }

        private async Task FinishGameManually(string result, string reason)
        {
            if (_currentGameId > 0)
            {
                Console.WriteLine($"=== РУЧНОЕ ЗАВЕРШЕНИЕ ИГРЫ ===");
                Console.WriteLine($"GameId: {_currentGameId}");
                Console.WriteLine($"Result: {result}");
                Console.WriteLine($"Reason: {reason}");

                // Завершаем игру на сервере
                bool gameFinished = await _apiService.FinishGameAsync(_currentGameId, result);

                if (gameFinished)
                {
                    Console.WriteLine($"✅ Игра #{_currentGameId} завершена на сервере");

                    // Обновляем рейтинг
                    int ratingChange = result == "White" ? RATING_WIN_CHANGE : RATING_LOSS_CHANGE;
                    await UpdateUserRating(ratingChange);
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось завершить игру #{_currentGameId}");
                }
            }
            else
            {
                Console.WriteLine("⚠️ Нет ID игры для завершения");
            }
        }


        private async void OnGameFinished(string result)
        {
            Console.WriteLine($"=== ON GAME FINISHED CALLED ===");
            Console.WriteLine($"Result from GameManager: {result}");

            await Dispatcher.Invoke(async () =>
            {
                try
                {
                    Console.WriteLine($"Dispatcher invoked, processing game finish...");

                    // Останавливаем все таймеры
                    _gameTimer.Stop();
                    _whiteTimer.Stop();
                    _blackTimer.Stop();

                    // Определяем результат для API
                    string apiResult = "Draw"; // по умолчанию
                    int ratingChange = 0;

                    if (result.Contains("Победа белых") || result.Contains("белые сдались"))
                    {
                        apiResult = "White";
                        ratingChange = RATING_WIN_CHANGE; // +15 за победу белых (пользователь)
                    }
                    else if (result.Contains("Победа черных") || result.Contains("черные сдались"))
                    {
                        apiResult = "Black";
                        ratingChange = RATING_LOSS_CHANGE; // -10 за поражение белых (пользователь)
                    }

                    Console.WriteLine($"API Result: {apiResult}");
                    Console.WriteLine($"Rating Change: {ratingChange}");
                    Console.WriteLine($"Current Game ID: {_currentGameId}");

                    // Сохраняем игру
                    await SaveGameToDatabase(apiResult, result, ratingChange);

                    // Обновляем статус в UI
                    if (StatusText != null)
                        StatusText.Text = result;

                    if (StatusIcon != null)
                        StatusIcon.Text = "🏁";

                    // Показываем сообщение о результате
                    string message = "";
                    string title = "";

                    if (apiResult == "White")
                    {
                        message = $"🎉 ПОБЕДА БЕЛЫХ! 🎉\n\n" +
                                 $"Вы выиграли партию!\n" +
                                 $"Противник: {_opponentName}\n" +
                                 $"Ваш рейтинг увеличен на {RATING_WIN_CHANGE} очков";
                        title = "Поздравляем!";
                    }
                    else if (apiResult == "Draw")
                    {
                        message = $"🤝 НИЧЬЯ! 🤝\n\n" +
                                 $"Партия закончилась вничью\n" +
                                 $"Противник: {_opponentName}\n" +
                                 $"Ваш рейтинг не изменился";
                        title = "Ничья";
                    }
                    else // Black
                    {
                        message = $"😔 ПОРАЖЕНИЕ БЕЛЫХ\n\n" +
                                 $"Вы проиграли партию\n" +
                                 $"Противник: {_opponentName}\n" +
                                 $"Ваш рейтинг уменьшен на {Math.Abs(RATING_LOSS_CHANGE)} очков";
                        title = "Сожалеем";
                    }

                    MessageBox.Show(message, title, MessageBoxButton.OK,
                        apiResult == "White" ? MessageBoxImage.Exclamation :
                        apiResult == "Draw" ? MessageBoxImage.Information : MessageBoxImage.Exclamation);

                    // Показываем панель с результатом
                    ShowGameResultButtons(apiResult == "White", apiResult == "Draw");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка в OnGameFinished: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                }
            });
        }

        private void ShowGameResultButtons(bool userWon, bool isDraw)
        {
            GameOverPanel.Visibility = Visibility.Visible;

            if (userWon)
            {
                GameResultDescription.Text = $"Поздравляем с победой!\n" +
                                            $"Вы выиграли у {_opponentName}\n" +
                                            $"Ваш рейтинг увеличен на {RATING_WIN_CHANGE} очков";
            }
            else if (isDraw)
            {
                GameResultDescription.Text = $"Партия закончилась вничью\n" +
                                            $"Противник: {_opponentName}\n" +
                                            $"Рейтинг не изменился";
            }
            else
            {
                GameResultDescription.Text = $"Вы проиграли {_opponentName}\n" +
                                            $"Ваш рейтинг уменьшен на {Math.Abs(RATING_LOSS_CHANGE)} очков";
            }
        }

        // Методы для кнопок GameOverPanel
        private void ViewStatsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowStatistics();
        }

        private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Открыть окно истории игр
            HistoryWindow historyWindow = new HistoryWindow(_userId);
            historyWindow.Show();
        }

        private void NewGameAfterButton_Click(object sender, RoutedEventArgs e)
        {
            NewGameButton_Click(sender, e);
        }
    }
}