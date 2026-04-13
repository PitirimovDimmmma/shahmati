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
        private string _currentDifficulty = "Medium";
        private string _opponentName = "Stockfish AI";
        private bool _isExiting = false;

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

            _viewModel.PlayerTurnChanged += OnPlayerTurnChanged;
            _viewModel.AIMoveStarted += OnAIMoveStarted;
            _viewModel.AIMoveCompleted += OnAIMoveCompleted;
            _viewModel.GameFinished += OnGameFinishedHandler;
            _viewModel.GameManager.MoveMade += OnMoveMade;

            InitializeDifficultySlider();
            InitializeChessTimers();

            Loaded += async (s, e) =>
            {
                await InitializeGameAsync();
                await StartGameManually();
            };
        }

        // ========== НАЧИСЛЕНИЕ РЕЙТИНГА ==========

        private async Task UpdateRatingAsync(bool isWin, bool isDraw = false)
        {
            try
            {
                int ratingChange = 0;

                if (isDraw)
                {
                    ratingChange = 0;
                }
                else if (isWin)
                {
                    ratingChange = 15;
                }
                else
                {
                    ratingChange = -10;
                }

                if (ratingChange != 0)
                {
                    await _apiService.UpdateUserRatingAsync(_userId, ratingChange);
                    await UpdateRatingDisplayAsync();
                    Console.WriteLine($"Рейтинг изменён: {(ratingChange > 0 ? "+" : "")}{ratingChange}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления рейтинга: {ex.Message}");
            }
        }

        private async Task UpdateRatingDisplayAsync()
        {
            try
            {
                var stats = await _apiService.GetUserStatsAsync(_userId);
                if (stats != null)
                {
                    RatingText.Text = stats.CurrentRating.ToString();
                    UserRatingText.Text = $"Рейтинг: {stats.CurrentRating}";
                }
                else
                {
                    var user = await _apiService.GetUserAsync(_userId);
                    int rating = user?.Profile?.Rating ?? 1200;
                    RatingText.Text = rating.ToString();
                    UserRatingText.Text = $"Рейтинг: {rating}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления отображения рейтинга: {ex.Message}");
            }
        }

        // ========== ОБНОВЛЕНИЕ ИСТОРИИ ХОДОВ ==========

        private void UpdateMoveHistoryDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                var historyList = new List<string>();
                var moves = _viewModel.GameManager.MoveHistory;

                for (int i = 0; i < moves.Count; i += 2)
                {
                    int moveNumber = i / 2 + 1;
                    string whiteMove = i < moves.Count ? FormatMoveForDisplay(moves[i]) : "...";
                    string blackMove = (i + 1 < moves.Count) ? FormatMoveForDisplay(moves[i + 1]) : "...";
                    historyList.Add($"{moveNumber}. {whiteMove,-8} | {blackMove}");
                }

                MoveHistoryList.ItemsSource = historyList;

                if (MoveHistoryList.Items.Count > 0)
                {
                    MoveHistoryList.ScrollIntoView(MoveHistoryList.Items[MoveHistoryList.Items.Count - 1]);
                }

                MovesCountText.Text = moves.Count.ToString();
            });
        }

        private string FormatMoveForDisplay(string move)
        {
            if (string.IsNullOrEmpty(move) || move == "...") return move;

            // Если ход уже в шахматной нотации (содержит буквы N, B, R, Q, K или O)
            if (move.Length >= 2 && (move[0] == 'N' || move[0] == 'B' || move[0] == 'R' || move[0] == 'Q' || move[0] == 'K' || move[0] == 'O'))
            {
                return move;
            }

            // Если ход уже в формате e2-e4
            if (move.Contains("-"))
            {
                return move;
            }

            // Если ход в формате координат (4 символа, например e2e4)
            if (move.Length == 4)
            {
                string from = move.Substring(0, 2);
                string to = move.Substring(2, 2);

                // Рокировка
                if ((from == "e1" && to == "g1") || (from == "e8" && to == "g8")) return "O-O";
                if ((from == "e1" && to == "c1") || (from == "e8" && to == "c8")) return "O-O-O";

                // Пробуем определить фигуру
                try
                {
                    int fromRow = 8 - int.Parse(from[1].ToString());
                    int fromCol = from[0] - 'a';
                    var piece = _viewModel?.GameManager?.Board?.GetPieceAt(new Position(fromRow, fromCol));

                    if (piece == null) return $"{from}-{to}";

                    if (piece.Type == PieceType.Pawn)
                    {
                        // Взятие пешкой
                        if (from[0] != to[0])
                        {
                            return $"{from[0]}x{to}";
                        }
                        return to;
                    }

                    string pieceSymbol = piece.Type switch
                    {
                        PieceType.Knight => "N",
                        PieceType.Bishop => "B",
                        PieceType.Rook => "R",
                        PieceType.Queen => "Q",
                        PieceType.King => "K",
                        _ => ""
                    };

                    // Проверяем взятие
                    int toRow = 8 - int.Parse(to[1].ToString());
                    int toCol = to[0] - 'a';
                    var targetPiece = _viewModel?.GameManager?.Board?.GetPieceAt(new Position(toRow, toCol));

                    if (targetPiece != null)
                    {
                        return $"{pieceSymbol}x{to}";
                    }

                    return $"{pieceSymbol}{to}";
                }
                catch
                {
                    // Если ошибка при парсинге - возвращаем простой формат
                    return $"{from}-{to}";
                }
            }

            return move;
        }

        // ========== СОХРАНЕНИЕ И ЗАГРУЗКА ИГРЫ ==========

        private async void SaveGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.GameManager?.Board == null)
                {
                    MessageBox.Show("Нет активной игры для сохранения.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Сохранить игру",
                    Filter = "Chess save files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    FileName = $"chess_save_{DateTime.Now:yyyyMMdd_HHmmss}",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    string fen = GetFenFromBoard();
                    string moveHistory = string.Join("\n", _viewModel.GameManager.MoveHistory);
                    string currentPlayer = _viewModel.GameManager.CurrentPlayer == PieceColor.White ? "White" : "Black";

                    var savedGame = new SavedGameData
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = _userId,
                        Fen = fen,
                        MoveHistory = moveHistory,
                        CurrentPlayer = currentPlayer,
                        SavedAt = DateTime.Now,
                        GameMode = "HumanVsHuman",
                        GameName = System.IO.Path.GetFileNameWithoutExtension(filePath)
                    };

                    string json = JsonSerializer.Serialize(savedGame, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(filePath, json);

                    MessageBox.Show($"✅ Игра успешно сохранена!\n\n📍 Путь: {filePath}",
                        "Сохранение игры", MessageBoxButton.OK, MessageBoxImage.Information);

                    var dashboard = new DashboardWindow(_userId);
                    dashboard.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка сохранения игры: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Выберите сохранённую игру",
                    Filter = "Chess save files (*.json)|*.json|All files (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    string json = await File.ReadAllTextAsync(filePath);
                    var savedGame = JsonSerializer.Deserialize<SavedGameData>(json);

                    if (savedGame == null)
                    {
                        MessageBox.Show("Не удалось прочитать файл сохранения.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (savedGame.UserId != _userId)
                    {
                        MessageBox.Show("Этот файл сохранения принадлежит другому пользователю.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await LoadSavedGame(savedGame);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка загрузки игры: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadSavedGame(SavedGameData savedGame)
        {
            try
            {
                StopAllTimers();

                _viewModel.GameMode = "Человек vs Человек";
                _viewModel.SetUserIsWhite(true);
                await _viewModel.StartNewGameAsync();

                _viewModel.GameManager.Board.LoadFromFen(savedGame.Fen);
                _viewModel.GameManager.Board.ForceUpdate();

                var moveHistory = savedGame.MoveHistory.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
                _viewModel.GameManager.MoveHistory.Clear();
                foreach (var move in moveHistory)
                {
                    _viewModel.GameManager.MoveHistory.Add(move);
                }

                PieceColor currentPlayer = savedGame.CurrentPlayer == "White" ? PieceColor.White : PieceColor.Black;
                _viewModel.GameManager.SetCurrentPlayer(currentPlayer);

                if (currentPlayer == PieceColor.White)
                {
                    _whiteTimer.Start();
                    _blackTimer.Stop();
                    UpdateCurrentPlayer("Ваш ход (Белые)");
                }
                else
                {
                    _whiteTimer.Stop();
                    _blackTimer.Start();
                    UpdateCurrentPlayer("Ход черных");
                }

                _viewModel.ForceBoardUpdate();
                ForceRedrawBoard();

                MovesCountText.Text = _viewModel.GameManager.MoveHistory.Count.ToString();
                UpdateMoveHistoryDisplay();

                StatusText.Text = $"Игра загружена. {savedGame.SavedAt:dd.MM.yyyy HH:mm}";
                StatusIcon.Text = currentPlayer == PieceColor.White ? "♔" : "♚";
                HumanGameButtons.Visibility = Visibility.Visible;

                MessageBox.Show($"✅ Игра успешно загружена!\n\n📅 Сохранена: {savedGame.SavedAt:dd.MM.yyyy HH:mm}\n🎮 Ход: {(currentPlayer == PieceColor.White ? "Белых" : "Черных")}\n📝 Всего ходов: {_viewModel.GameManager.MoveHistory.Count}",
                    "Загрузка игры", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сохранённой игры: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetFenFromBoard()
        {
            var board = _viewModel.GameManager.Board;
            StringBuilder fen = new StringBuilder();

            for (int row = 0; row < 8; row++)
            {
                int emptyCount = 0;
                for (int col = 0; col < 8; col++)
                {
                    var piece = board.GetPieceAt(new Position(row, col));
                    if (piece == null)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            fen.Append(emptyCount);
                            emptyCount = 0;
                        }

                        char pieceChar = piece.Type switch
                        {
                            PieceType.King => 'k',
                            PieceType.Queen => 'q',
                            PieceType.Rook => 'r',
                            PieceType.Bishop => 'b',
                            PieceType.Knight => 'n',
                            PieceType.Pawn => 'p',
                            _ => ' '
                        };

                        if (piece.Color == PieceColor.White)
                            pieceChar = char.ToUpper(pieceChar);

                        fen.Append(pieceChar);
                    }
                }

                if (emptyCount > 0)
                    fen.Append(emptyCount);

                if (row < 7)
                    fen.Append('/');
            }

            fen.Append(_viewModel.GameManager.CurrentPlayer == PieceColor.White ? " w " : " b ");
            fen.Append("KQkq - 0 1");

            return fen.ToString();
        }

        // ========== ОСТАЛЬНЫЕ МЕТОДЫ ==========

        private void InitializeDifficultySlider()
        {
            if (DifficultySlider != null)
            {
                DifficultySlider.ValueChanged += DifficultySlider_ValueChanged;
                UpdateDifficultyText();
            }
        }

        private void DifficultySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int value = (int)DifficultySlider.Value;
            _currentDifficulty = value switch
            {
                0 => "Beginner",
                1 => "Easy",
                2 => "Medium",
                3 => "Hard",
                4 => "Expert",
                _ => "Medium"
            };
            UpdateDifficultyText();

            if (_viewModel != null)
            {
                _viewModel.Difficulty = _currentDifficulty;
                if (_viewModel.GameManager?.IsGameInProgress == true)
                {
                    var result = MessageBox.Show($"Сложность изменена на {GetDifficultyName(_currentDifficulty)}.\nНачать новую игру с новыми настройками?",
                        "Изменение сложности", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        NewGameButton_Click(sender, e);
                    }
                }
            }
        }

        private void UpdateDifficultyText()
        {
            if (DifficultyText != null)
            {
                string displayName = GetDifficultyName(_currentDifficulty);
                DifficultyText.Text = displayName;

                switch (_currentDifficulty)
                {
                    case "Beginner": DifficultyText.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113)); break;
                    case "Easy": DifficultyText.Foreground = new SolidColorBrush(Color.FromRgb(52, 152, 219)); break;
                    case "Medium": DifficultyText.Foreground = new SolidColorBrush(Color.FromRgb(241, 196, 15)); break;
                    case "Hard": DifficultyText.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34)); break;
                    case "Expert": DifficultyText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60)); break;
                }
            }
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

        private async Task InitializeGameAsync()
        {
            try
            {
                ShowLoadingIndicator("Загрузка данных пользователя...");
                await LoadUserDataAsync();

                bool isConnected = await _apiService.TestConnectionAsync();
                if (!isConnected)
                {
                    MessageBox.Show("⚠️ Сервер недоступен. Игра будет работать в локальном режиме.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                HideLoadingIndicator();
            }
            catch (Exception ex)
            {
                HideLoadingIndicator();
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                if (!string.IsNullOrEmpty(photoPath) && File.Exists(photoPath))
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
                await UpdateRatingDisplayAsync();
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
                UserAvatar.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/default_avatar.png"));
            }
            catch
            {
                UserAvatar.Source = null;
            }
        }

        private void OnMoveMade(string move)
        {
            Dispatcher.Invoke(() =>
            {
                ForceRedrawBoard();
                UpdateMoveHistoryDisplay();
            });
        }

        public async Task StartGameManually()
        {
            await StartNewGameWithAIAsync();
        }

        private async Task StartNewGameWithAIAsync()
        {
            try
            {
                ShowLoadingIndicator("Создание игры с Stockfish AI...");

                _viewModel.GameManager.InitializeApiService(_apiService);
                _viewModel.SetUserIsWhite(true);
                _viewModel.Difficulty = _currentDifficulty;

                _currentGameId = await _viewModel.GameManager.CreateAIGameOnServerAsync(
                    _userId,
                    _currentDifficulty,
                    "White"
                );

                await _viewModel.StartNewGameAsync();

                HideLoadingIndicator();
                ShowGameStartNotification();
                StartGameTimers();
                UpdateUIForNewGame();

                MoveHistoryList.ItemsSource = null;
                UpdateMoveHistoryDisplay();
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

        private async Task StartNewGameWithHumanAsync()
        {
            try
            {
                ShowLoadingIndicator("Создание игры для двух игроков...");

                _viewModel.GameMode = "Человек vs Человек";
                _viewModel.SetUserIsWhite(true);
                await _viewModel.StartNewGameAsync();

                HideLoadingIndicator();
                ShowGameStartNotification();
                StartGameTimers();

                MovesCountText.Text = "0";
                StatusText.Text = "Игра начата. Белые ходят первыми.";
                StatusIcon.Text = "♔";
                OpponentColorText.Text = "ЧЕРНЫЕ (игрок)";
                GameInfoText.Text = "Режим: два игрока";

                MoveHistoryList.ItemsSource = null;
                UpdateMoveHistoryDisplay();

                HumanGameButtons.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                HideLoadingIndicator();
                MessageBox.Show($"Ошибка создания игры: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowGameStartNotification()
        {
            GameStartNotification.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.5) };
            GameStartNotification.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                var fadeOut = new DoubleAnimation { From = 1, To = 0, Duration = TimeSpan.FromSeconds(0.5) };
                fadeOut.Completed += (s2, e2) => GameStartNotification.Visibility = Visibility.Collapsed;
                GameStartNotification.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            };
            timer.Start();
        }

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
            _blackTimer.Stop();
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

        public void OnPlayerTurnChanged(string playerColor)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateCurrentPlayer(playerColor);
                if (playerColor.Contains("Белые") || playerColor.Contains("Ваш ход"))
                {
                    _blackTimer.Stop();
                    _whiteTimer.Start();
                }
                else
                {
                    _whiteTimer.Stop();
                    _blackTimer.Start();
                }
            });
        }

        public void OnAIMoveStarted(string message)
        {
            Dispatcher.Invoke(() =>
            {
                AITurnIndicator.Visibility = Visibility.Visible;
                StatusText.Text = "🤖 Stockfish AI анализирует позицию...";
                StatusIcon.Text = "🤖";
                _whiteTimer.Stop();
                _blackTimer.Start();

                if (CurrentPlayerText != null)
                {
                    CurrentPlayerText.Text = "ХОД ИИ (ЧЕРНЫЕ)";
                    CurrentPlayerText.Foreground = Brushes.White;
                    if (CurrentPlayerText.Parent is Border border)
                        border.Background = new SolidColorBrush(Color.FromRgb(139, 0, 0));
                }

                ForceRedrawBoard();
            });
        }

        public void OnAIMoveCompleted(string move)
        {
            Dispatcher.Invoke(() =>
            {
                AITurnIndicator.Visibility = Visibility.Collapsed;

                if (_viewModel?.GameManager?.UserIsWhite == true)
                {
                    StatusText.Text = "Ваш ход (Белые)";
                    StatusIcon.Text = "♔";
                }
                else
                {
                    StatusText.Text = "Ваш ход (Черные)";
                    StatusIcon.Text = "♚";
                }

                if (_viewModel?.GameManager?.MoveHistory != null)
                {
                    MovesCountText.Text = _viewModel.GameManager.MoveHistory.Count.ToString();
                    UpdateMoveHistoryDisplay();
                }

                ForceRedrawBoard();

                if (CurrentPlayerText != null)
                {
                    CurrentPlayerText.Text = "ВАШ ХОД (БЕЛЫЕ)";
                    CurrentPlayerText.Foreground = Brushes.White;
                    if (CurrentPlayerText.Parent is Border border)
                        border.Background = new SolidColorBrush(Color.FromRgb(0, 100, 0));
                }
            });
        }

        public void OnGameFinishedHandler(string result)
        {
            Dispatcher.Invoke(async () =>
            {
                StopAllTimers();

                bool whiteWon = result.Contains("Победа белых") || result.Contains("White wins") || result.Contains("Победа белых!");
                bool isDraw = result.Contains("Ничья") || result.Contains("Draw");

                bool userWon = whiteWon;
                bool userLose = !whiteWon && !isDraw;

                if (userWon)
                {
                    await UpdateRatingAsync(true, false);
                }
                else if (userLose)
                {
                    await UpdateRatingAsync(false, false);
                }
                else if (isDraw)
                {
                    await UpdateRatingAsync(false, true);
                }

                await UpdateRatingDisplayAsync();

                string message = userWon
                    ? $"🎉 ПОБЕДА! +15 рейтинга\n\n{result}"
                    : isDraw
                        ? $"🤝 НИЧЬЯ! Рейтинг не изменился\n\n{result}"
                        : $"😔 ПОРАЖЕНИЕ -10 рейтинга\n\n{result}";

                var msgBoxResult = MessageBox.Show(message, "Игра окончена",
                    MessageBoxButton.OK,
                    userWon ? MessageBoxImage.Exclamation :
                    isDraw ? MessageBoxImage.Information : MessageBoxImage.Exclamation);

                if (msgBoxResult == MessageBoxResult.OK)
                {
                    var dashboard = new DashboardWindow(_userId);
                    dashboard.Show();
                    this.Close();
                }
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
                    _blackTimer.Stop();
                    _whiteTimer.Start();
                }
                else
                {
                    CurrentPlayerText.Text = $"{_opponentName.ToUpper()} (ЧЕРНЫЕ)";
                    CurrentPlayerText.Foreground = Brushes.White;
                    if (CurrentPlayerText.Parent is Border border)
                        border.Background = new SolidColorBrush(Color.FromRgb(139, 0, 0));
                    _whiteTimer.Stop();
                    _blackTimer.Start();
                }
            });
        }

        private void ForceRedrawBoard()
        {
            Dispatcher.Invoke(() =>
            {
                if (_viewModel?.GameManager?.Board != null)
                {
                    var boardItemsControl = FindName("BoardItemsControl") as ItemsControl;
                    if (boardItemsControl != null)
                    {
                        boardItemsControl.ItemsSource = null;
                        boardItemsControl.ItemsSource = _viewModel.GameManager.Board.CellsFlat;
                    }

                    for (int row = 0; row < 8; row++)
                    {
                        for (int col = 0; col < 8; col++)
                        {
                            var cell = _viewModel.GameManager.Board.Cells[row, col];
                            cell.OnPropertyChanged(nameof(BoardCell.Piece));
                            cell.OnPropertyChanged(nameof(BoardCell.PieceImagePath));
                        }
                    }

                    _viewModel.GameManager.Board.ForceUpdate();
                    _viewModel.ForceBoardUpdate();

                    var temp = DataContext;
                    DataContext = null;
                    DataContext = temp;
                }
            });
        }

        private async void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsGameStarting)
            {
                Console.WriteLine("Игра уже создается, подождите...");
                return;
            }

            string mode = _viewModel?.GameMode == "Человек vs Компьютер" ? "против Stockfish AI" : "с другом";
            var result = MessageBox.Show($"Начать новую игру {mode}?",
                "Новая игра", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                GameOverPanel.Visibility = Visibility.Collapsed;
                _whiteTimeLeft = TimeSpan.FromMinutes(10);
                _blackTimeLeft = TimeSpan.FromMinutes(10);
                UpdateTimerDisplays();

                _currentGameId = 0;

                if (_viewModel?.GameMode == "Человек vs Компьютер")
                {
                    await StartNewGameWithAIAsync();
                }
                else
                {
                    await StartNewGameWithHumanAsync();
                }
            }
        }

        private async void ResignButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Сдаться? Это -10 рейтинга.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes && _viewModel?.GameManager != null)
            {
                await _viewModel.GameManager.ResignAsync(PieceColor.White);
                await UpdateRatingAsync(false, false);
                await UpdateRatingDisplayAsync();

                var dashboard = new DashboardWindow(_userId);
                dashboard.Show();
                this.Close();
            }
        }

        private async void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isExiting) return;
            _isExiting = true;

            HomeButton.IsEnabled = false;

            try
            {
                if (_viewModel?.GameManager?.IsGameInProgress == true)
                {
                    var result = MessageBox.Show("Выйти в главное меню? Это поражение.",
                        "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (_currentGameId > 0)
                            await _apiService.FinishGameAsync(_currentGameId, "Black");

                        await UpdateRatingAsync(false, false);
                        await UpdateRatingDisplayAsync();

                        var dashboard = new DashboardWindow(_userId);
                        dashboard.Show();
                        this.Close();
                    }
                    else
                    {
                        _isExiting = false;
                        HomeButton.IsEnabled = true;
                    }
                }
                else
                {
                    var dashboard = new DashboardWindow(_userId);
                    dashboard.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выходе: {ex.Message}");
                _isExiting = false;
                HomeButton.IsEnabled = true;
            }
        }

        private void ViewStatsButton_Click(object sender, RoutedEventArgs e)
        {
            new StatisticsWindow(_userId).Show();
        }

        private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            new HistoryWindow(_userId).Show();
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

        private void VsAIToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.GameMode = "Человек vs Компьютер";
                VsAIText.Foreground = new SolidColorBrush(Colors.White);
                VsHumanText.Foreground = new SolidColorBrush(Colors.LightGray);

                if (DifficultyPanel != null)
                    DifficultyPanel.Visibility = Visibility.Visible;

                if (HumanGameButtons != null)
                    HumanGameButtons.Visibility = Visibility.Collapsed;

                OpponentColorText.Text = "ЧЕРНЫЕ (Stockfish AI)";
                StatusText.Text = "Режим: Игра против ИИ";

                if (_viewModel.GameManager?.IsGameInProgress == true)
                {
                    var result = MessageBox.Show("Сменить режим игры? Текущая игра будет завершена.",
                        "Смена режима", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        NewGameButton_Click(sender, e);
                    }
                }
            }
        }

        private void VsAIToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.GameMode = "Человек vs Человек";
                VsHumanText.Foreground = new SolidColorBrush(Colors.White);
                VsAIText.Foreground = new SolidColorBrush(Colors.LightGray);

                if (DifficultyPanel != null)
                    DifficultyPanel.Visibility = Visibility.Collapsed;

                if (HumanGameButtons != null)
                    HumanGameButtons.Visibility = Visibility.Visible;

                OpponentColorText.Text = "ЧЕРНЫЕ (игрок)";
                StatusText.Text = "Режим: Игра против человека";

                if (_viewModel.GameManager?.IsGameInProgress == true)
                {
                    var result = MessageBox.Show("Сменить режим игры? Текущая игра будет завершена.",
                        "Смена режима", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        NewGameButton_Click(sender, e);
                    }
                }
            }
        }

        private void VsAI_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VsAIToggle.IsChecked = true;
        }

        private void VsHuman_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VsAIToggle.IsChecked = false;
        }

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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopAllTimers();
        }

        private Border _loadingPanel;
        private ProgressBar _loadingSpinner;
        private TextBlock _loadingText;
    }

    public class SavedGameData
    {
        public string Id { get; set; }
        public int UserId { get; set; }
        public string Fen { get; set; }
        public string MoveHistory { get; set; }
        public string CurrentPlayer { get; set; }
        public DateTime SavedAt { get; set; }
        public string GameMode { get; set; }
        public string GameName { get; set; }
    }
}