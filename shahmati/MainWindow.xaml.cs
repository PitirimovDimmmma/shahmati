using shahmati.Helpers;
using shahmati.models;
using shahmati.Models;
using shahmati.Services;
using shahmati.ViewModels;
using shahmati.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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

        // Дополнительные поля для индикатора загрузки
        private Border _loadingPanel;
        private TextBlock _loadingText;
        private ProgressBar _loadingSpinner;

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
                _viewModel.GameManager.GameFinished += OnGameFinishedHandler;
                _viewModel.GameManager.MoveMade += OnMoveMade;
                _viewModel.GameManager.UpdateHistoryCallback = UpdateMoveHistoryInUI;
                _viewModel.GameManager.UserIsWhite = true; // Пользователь всегда белые
            }

            // Инициализируем таймеры
            InitializeChessTimers();
            InitializeGameTimer();

            // Инициализируем список ходов
            _gameMoves = new List<string>();

            // Создаем динамически индикатор загрузки
            CreateLoadingIndicator();

            Loaded += async (s, e) => await InitializeGame();
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            SetDefaultUserData();
            InitializeChessTimers();
            CreateLoadingIndicator();
        }

        private void CreateLoadingIndicator()
        {
            // Создаем индикатор загрузки динамически
            _loadingPanel = new Border
            {
                Name = "LoadingPanel",
                Visibility = Visibility.Collapsed,
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
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
                Name = "LoadingSpinner",
                Width = 50,
                Height = 50,
                IsIndeterminate = true,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _loadingText = new TextBlock
            {
                Name = "LoadingText",
                Text = "Загрузка...",
                Foreground = Brushes.White,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stackPanel.Children.Add(_loadingSpinner);
            stackPanel.Children.Add(_loadingText);
            _loadingPanel.Child = stackPanel;

            // Добавляем в главную сетку
            if (MainGrid != null)
            {
                MainGrid.Children.Add(_loadingPanel);
            }
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

                // Устанавливаем имя противника
                _opponentName = "Гость";

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

        private void ShowGameStartNotification()
        {
            // Формируем текст уведомления
            GameInfoText.Text = $"Режим: Человек vs Человек\n" +
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

        private async Task CreateNewOnlineGame()
        {
            try
            {
                string gameMode = "HumanVsHuman";
                string difficulty = "Medium";

                Console.WriteLine($"=== СОЗДАНИЕ НОВОЙ ИГРЫ ===");
                Console.WriteLine($"UserId: {_userId}");
                Console.WriteLine($"GameMode: {gameMode}");
                Console.WriteLine($"Difficulty: {difficulty}");

                var createDto = new CreateGameDto
                {
                    WhitePlayerId = _userId,
                    BlackPlayerId = null,
                    GameMode = gameMode,
                    Difficulty = difficulty
                };

                // ПРЯМОЙ CURL ЗАПРОС для отладки
                Console.WriteLine("=== ПРЯМОЙ CURL ЗАПРОС НА СОЗДАНИЕ ИГРЫ ===");
                string jsonData = JsonSerializer.Serialize(createDto);
                string tempJsonFile = Path.GetTempFileName() + ".json";
                await File.WriteAllTextAsync(tempJsonFile, jsonData, Encoding.UTF8);

                string curlCommand = $"curl -X POST \"https://localhost:7259/api/games\" " +
                                    $"-H \"Content-Type: application/json\" " +
                                    $"-v " +
                                    $"--data-binary \"@{tempJsonFile}\" " +
                                    $"--insecure";

                Console.WriteLine($"Curl command: {curlCommand}");

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {curlCommand}",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    File.Delete(tempJsonFile);

                    Console.WriteLine($"=== CURL OUTPUT ===");
                    Console.WriteLine(output);
                    Console.WriteLine($"=== CURL ERROR ===");
                    Console.WriteLine(error);

                    // Парсим ID из ответа
                    if (output.Contains("\"id\":"))
                    {
                        try
                        {
                            int startIndex = output.IndexOf("\"id\":") + 5;
                            int endIndex = output.IndexOf(",", startIndex);
                            string idStr = output.Substring(startIndex, endIndex - startIndex).Trim();

                            if (int.TryParse(idStr, out int gameId))
                            {
                                _currentGameId = gameId;
                                Console.WriteLine($"✅ Игра создана с ID: {gameId}");

                                // Сохраняем ID в файл
                                File.WriteAllText("current_game.txt",
                                    $"Game ID: {gameId}\n" +
                                    $"User ID: {_userId}\n" +
                                    $"Time: {DateTime.Now:HH:mm:ss}");

                                MessageBox.Show($"Игра #{gameId} создана!", "Успех",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Ошибка парсинга ID: {ex.Message}");
                        }
                    }

                    Console.WriteLine($"❌ Не удалось получить ID игры из ответа");
                    _currentGameId = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка создания игры: {ex.Message}");
                _currentGameId = 0;
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

        private async void OnGameFinishedHandler(string result)
        {
            await Dispatcher.Invoke(async () =>
            {
                try
                {
                    // Останавливаем таймеры
                    _gameTimer.Stop();
                    _whiteTimer.Stop();
                    _blackTimer.Stop();

                    // Определяем, кто победил
                    bool whiteWon = result.Contains("Победа белых") ||
                                   result.Contains("Черные сдались") ||
                                   result.Contains("White wins");
                    bool isDraw = result.Contains("Ничья") || result.Contains("Draw");

                    // Показываем уведомление
                    string message = "";
                    string title = "";

                    if (whiteWon)
                    {
                        message = $"🎉 ПОБЕДА БЕЛЫХ! 🎉\n\n" +
                                 $"Вы выиграли партию!\n" +
                                 $"Противник: {_opponentName}\n" +
                                 $"Результат будет сохранен в истории.";
                        title = "Поздравляем!";
                    }
                    else if (isDraw)
                    {
                        message = $"🤝 НИЧЬЯ! 🤝\n\n" +
                                 $"Партия закончилась вничью\n" +
                                 $"Противник: {_opponentName}\n" +
                                 $"Результат будет сохранен в истории.";
                        title = "Ничья";
                    }
                    else
                    {
                        message = $"😔 ПОРАЖЕНИЕ БЕЛЫХ\n\n" +
                                 $"Вы проиграли партию\n" +
                                 $"Противник: {_opponentName}\n" +
                                 $"Результат будет сохранен в истории.";
                        title = "Сожалеем";
                    }

                    MessageBox.Show(message, title, MessageBoxButton.OK,
                        whiteWon ? MessageBoxImage.Exclamation :
                        isDraw ? MessageBoxImage.Information : MessageBoxImage.Exclamation);

                    // Показываем панель с результатом
                    ShowGameResultButtonsInternal(whiteWon, isDraw);

                    // Обновляем статус
                    if (StatusText != null)
                        StatusText.Text = result;

                    if (StatusIcon != null)
                        StatusIcon.Text = "🏁";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка при завершении игры: {ex.Message}");
                }
            });
        }

        private void ShowGameResultButtonsInternal(bool whiteWon, bool isDraw)
        {
            GameOverPanel.Visibility = Visibility.Visible;

            if (whiteWon)
            {
                GameResultDescription.Text = $"Поздравляем с победой!\n" +
                                            $"Вы выиграли у {_opponentName}\n" +
                                            $"Игра сохранена в истории";
            }
            else if (isDraw)
            {
                GameResultDescription.Text = $"Партия закончилась вничью\n" +
                                            $"Противник: {_opponentName}\n" +
                                            $"Игра сохранена в истории";
            }
            else
            {
                GameResultDescription.Text = $"Вы проиграли {_opponentName}\n" +
                                            $"Игра сохранена в истории";
            }
        }

        private async Task<bool> UpdateUserRatingAfterGame(int ratingChange)
        {
            try
            {
                Console.WriteLine($"=== ОБНОВЛЕНИЕ РЕЙТИНГА ПОСЛЕ ИГРЫ ===");
                Console.WriteLine($"UserId: {_userId}");
                Console.WriteLine($"RatingChange: {ratingChange}");
                Console.WriteLine($"CurrentGameId: {_currentGameId}");

                // 1. Получить текущий профиль пользователя
                var user = await _apiService.GetUserAsync(_userId);
                if (user == null)
                {
                    Console.WriteLine("❌ Пользователь не найден");
                    return false;
                }

                // 2. Получить текущий рейтинг из профиля
                int currentRating = user.Profile?.Rating ?? 1200;
                Console.WriteLine($"Текущий рейтинг из профиля: {currentRating}");

                // 3. Рассчитать новый рейтинг
                int newRating = currentRating + ratingChange;
                Console.WriteLine($"Новый рейтинг: {newRating}");

                // 4. Обновить профиль напрямую
                bool profileUpdated = await UpdateProfileRatingDirectly(_userId, newRating);

                if (profileUpdated)
                {
                    Console.WriteLine($"✅ Профиль обновлен: {newRating}");

                    // 5. Добавить запись в историю рейтинга
                    if (_currentGameId > 0)
                    {
                        bool historyAdded = await AddRatingHistory(_userId, _currentGameId,
                            currentRating, newRating, ratingChange);

                        if (historyAdded)
                        {
                            Console.WriteLine($"✅ История рейтинга добавлена");
                        }
                    }

                    // 6. Обновить UI
                    await UpdateRatingUI();

                    return true;
                }
                else
                {
                    Console.WriteLine("❌ Не удалось обновить профиль");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления рейтинга: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // Метод для прямого обновления профиля
        private async Task<bool> UpdateProfileRatingDirectly(int userId, int newRating)
        {
            try
            {
                // Создаем запрос для обновления профиля
                var updateRequest = new UpdateProfileRequest
                {
                    Rating = newRating
                };

                // Вызываем API для обновления профиля
                bool result = await _apiService.UpdateProfileAsync(userId, updateRequest);

                if (result)
                {
                    Console.WriteLine($"✅ Профиль ID={userId} обновлен на рейтинг {newRating}");
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось обновить профиль ID={userId}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления профиля: {ex.Message}");
                return false;
            }
        }

        // Метод для добавления истории рейтинга
        private async Task<bool> AddRatingHistory(int userId, int gameId, int oldRating, int newRating, int ratingChange)
        {
            try
            {
                var historyDto = new AddRatingHistoryDto
                {
                    UserId = userId,
                    GameId = gameId,
                    OldRating = oldRating,
                    NewRating = newRating,
                    RatingChange = ratingChange,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _apiService.AddRatingHistoryAsync(historyDto);

                if (result)
                {
                    Console.WriteLine($"✅ История рейтинга добавлена для игры {gameId}");
                }
                else
                {
                    Console.WriteLine($"⚠️ Не удалось добавить историю рейтинга");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка добавления истории рейтинга: {ex.Message}");
                return false;
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
                else
                {
                    // Пытаемся получить пользователя напрямую
                    var user = await _apiService.GetUserAsync(_userId);
                    if (user?.Profile != null)
                    {
                        if (RatingText != null)
                            RatingText.Text = user.Profile.Rating.ToString();

                        if (UserRatingText != null)
                            UserRatingText.Text = $"Рейтинг: {user.Profile.Rating}";

                        Console.WriteLine($"✅ UI обновлен из профиля: рейтинг = {user.Profile.Rating}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления UI: {ex.Message}");
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
            OnGameFinishedHandler(resultMessage);
        }

        private void ShowLoadingIndicator(string message)
        {
            if (_loadingPanel != null)
            {
                _loadingPanel.Visibility = Visibility.Visible;
                _loadingText.Text = message;
            }
        }

        private void HideLoadingIndicator()
        {
            if (_loadingPanel != null)
            {
                _loadingPanel.Visibility = Visibility.Collapsed;
            }
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
                    StatusText.Text = $"Ход {_opponentName} (черные)";
                    StatusIcon.Text = "⚫";
                }
            }
        }

        // ===== ОБРАБОТЧИКИ СОБЫТИЙ UI =====

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
                    OnGameFinishedHandler("Игра прервана для начала новой");
                }

                // Начинаем новую игру
                _viewModel.StartNewGame();
                _currentGameId = 0;

                // Сбрасываем таймеры и счетчики
                _whiteTimeLeft = TimeSpan.FromMinutes(5);
                _blackTimeLeft = TimeSpan.FromMinutes(5);
                UpdateTimerDisplays();
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

                // Просто показываем сообщение
                MessageBox.Show("Игра прервана. Для сохранения результатов завершите игру нормально.",
                    "Игра прервана", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FinishCurrentGame error: {ex.Message}");
            }
            finally
            {
                // Останавливаем таймеры
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
                    OnGameFinishedHandler("Игра завершена при выходе из приложения");
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

        // ===== КНОПКА СДАЧИ =====

        private async void ResignButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите сдаться?\n\n" +
                                       "Игра будет завершена как поражение.",
                "Сдача",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Показываем индикатор загрузки
                    ShowLoadingIndicator("Завершение игры...");

                    // Определяем результат сдачи
                    // Пользователь всегда играет белыми, поэтому сдача = победа черных
                    string apiResult = "Black";

                    // Прямое завершение игры
                    bool success = await FinishGameThroughApi(_currentGameId, apiResult);

                    // Скрываем индикатор загрузки
                    HideLoadingIndicator();

                    if (success)
                    {
                        // Обновляем UI
                        StatusText.Text = "Вы сдались. Игра завершена.";
                        StatusIcon.Text = "🏳️";

                        // Вызываем обработчик завершения игры
                        OnGameFinishedHandler("Черные победили (сдача белых)");

                        MessageBox.Show("Игра завершена! Результат сохранен.",
                            "Сдача",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось завершить игру. Попробуйте снова.",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    HideLoadingIndicator();
                    MessageBox.Show($"Ошибка при сдаче: {ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private async Task<bool> FinishGameThroughApi(int gameId, string result)
        {
            try
            {
                Console.WriteLine($"=== ЗАВЕРШЕНИЕ ИГРЫ ЧЕРЕЗ API ===");
                Console.WriteLine($"GameId: {gameId}");
                Console.WriteLine($"Result: {result}");
                Console.WriteLine($"UserId: {_userId}");

                // Завершаем игру на сервере
                bool gameFinished = await _apiService.FinishGameAsync(gameId, result);

                if (gameFinished)
                {
                    Console.WriteLine($"✅ Игра #{gameId} завершена на сервере");

                    // Обновляем рейтинг пользователя
                    int ratingChange = CalculateRatingChange(result);
                    Console.WriteLine($"Рассчитанное изменение рейтинга: {ratingChange}");

                    if (ratingChange != 0)
                    {
                        // Создаем DTO для обновления рейтинга
                        var updateDto = new UpdateRatingDto
                        {
                            UserId = _userId,
                            GameId = gameId,
                            RatingChange = ratingChange
                        };

                        // Отправляем запрос на обновление рейтинга
                        bool ratingUpdated = await _apiService.UpdateUserRatingWithGameAsync(updateDto);

                        if (ratingUpdated)
                        {
                            Console.WriteLine($"✅ Рейтинг обновлен: {ratingChange}");

                            // Обновляем UI
                            await UpdateRatingUI();
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Рейтинг не обновлен автоматически");

                            // Пытаемся обновить рейтинг альтернативным способом
                            bool altSuccess = await _apiService.UpdateRatingWithCurlAsync(_userId, ratingChange);
                            if (altSuccess)
                            {
                                Console.WriteLine($"✅ Рейтинг обновлен через альтернативный метод");
                                await UpdateRatingUI();
                            }
                        }
                    }

                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось завершить игру #{gameId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка завершения игры: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        private int CalculateRatingChange(string result)
        {
            Console.WriteLine($"=== РАСЧЕТ ИЗМЕНЕНИЯ РЕЙТИНГА ===");
            Console.WriteLine($"Result: {result}");
            Console.WriteLine($"UserIsWhite: {_userIsWhite}");

            // Проверяем, кто победил
            bool isWinForUser = false;
            bool isDraw = false;

            if (_userIsWhite)
            {
                // Пользователь играет белыми
                isWinForUser = (result == "White");
                isDraw = (result == "Draw");
            }
            else
            {
                // Пользователь играет черными (на всякий случай)
                isWinForUser = (result == "Black");
                isDraw = (result == "Draw");
            }

            // При сдаче пользователь всегда проигрывает
            if (result == "Black" && _userIsWhite)
            {
                isWinForUser = false;
                isDraw = false;
            }

            Console.WriteLine($"isWinForUser: {isWinForUser}, isDraw: {isDraw}");

            // Расчет изменения рейтинга
            if (isDraw)
            {
                Console.WriteLine($"Изменение рейтинга: 0 (ничья)");
                return RATING_DRAW_CHANGE;
            }
            else if (isWinForUser)
            {
                Console.WriteLine($"Изменение рейтинга: +{RATING_WIN_CHANGE} (победа)");
                return RATING_WIN_CHANGE;
            }
            else
            {
                Console.WriteLine($"Изменение рейтинга: {RATING_LOSS_CHANGE} (поражение)");
                return RATING_LOSS_CHANGE;
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