using shahmati.Helpers;
using shahmati.models;
using shahmati.Models;
using shahmati.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;

namespace shahmati.ViewModels
{
    using ApiCreateGameDto = shahmati.Models.CreateGameDto;
    using ApiGameDto = shahmati.Models.GameDto;
    using ApiGameStatsDto = shahmati.Models.GameStatsDto;
    using ApiMoveDto = shahmati.Models.MoveDto;
    using ApiPlayerStatsDto = shahmati.Models.PlayerStatsDto;
    using ApiSavedGameDto = shahmati.Models.SavedGameDto;
    using ApiUserDto = shahmati.Models.UserDto;
    using ApiUserProfileDto = shahmati.Models.UserProfileDto;
    using ApiUserWithProfileDto = shahmati.Models.UserWithProfileDto;

    public class MainViewModel : INotifyPropertyChanged
    {
        private Board _board;
        private GameManager _gameManager;
        private Position _selectedPosition;
        private DispatcherTimer _animationTimer;
        private double _animationProgress;
        private Position _animationFrom;
        private Position _animationTo;
        private ChessPiece _animatingPiece;
        private string _gameMode = "Человек vs Человек";
        private string _difficulty = "Средний";
        private bool _isAITurn;
        private readonly ApiService _apiService;
        private int _currentUserId;
        private List<ApiGameDto> _activeGames;
        private string _currentPlayerColor = "Белые";
        private bool _enableMoveHighlighting = true;
        private bool _isHumanVsHuman = true;
        private bool _isGameActive = false;

        // Событие для уведомления MainWindow о смене хода
        public event Action<string> PlayerTurnChanged;

        public MainViewModel(int? userId = null)
        {
            _gameManager = new GameManager();
            _board = _gameManager.Board;
            _selectedPosition = Position.Invalid;
            _apiService = new ApiService();

            InitializeAnimationTimer();
            StartNewGameCommand = new RelayCommand(StartNewGame);
            CellClickCommand = new RelayCommand<Position>(HandleCellClick);

            _gameManager.PropertyChanged += GameManager_PropertyChanged;

            if (userId.HasValue)
            {
                _currentUserId = userId.Value;
            }
            else
            {
                _currentUserId = 0;
            }

            // Инициализируем начальное состояние
            _currentPlayerColor = "Белые";
        }

        public MainViewModel() : this(null)
        {
        }

        public Board Board
        {
            get => _board;
            private set
            {
                _board = value;
                OnPropertyChanged(nameof(Board));
            }
        }

        public List<ApiGameDto> ActiveGames
        {
            get => _activeGames;
            set
            {
                _activeGames = value;
                OnPropertyChanged(nameof(ActiveGames));
            }
        }

        // ИСПРАВЛЕНО: Добавлено явное свойство для отслеживания текущего игрока
        public string CurrentPlayerColor
        {
            get => _currentPlayerColor;
            private set
            {
                if (_currentPlayerColor != value)
                {
                    _currentPlayerColor = value;
                    OnPropertyChanged(nameof(CurrentPlayerColor));

                    // Уведомляем MainWindow о смене хода
                    PlayerTurnChanged?.Invoke(value);

                    // Обновляем текст для отображения
                    OnPropertyChanged(nameof(CurrentPlayerText));
                }
            }
        }

        public string CurrentPlayerText
        {
            get
            {
                if (_gameManager == null)
                    return "⚪ БЕЛЫЕ";

                // Определяем, чей сейчас ход по цвету фигур
                if (_gameManager.CurrentPlayer == PieceColor.White)
                {
                    return "⚪ БЕЛЫЕ";
                }
                else
                {
                    return "⚫ ЧЁРНЫЕ";
                }
            }
        }

        public bool IsAITurn
        {
            get => _isAITurn;
            private set
            {
                _isAITurn = value;
                OnPropertyChanged(nameof(IsAITurn));

                // Обновляем статус хода ИИ в MainWindow
                UpdateAITurnInMainWindow(value);
            }
        }

        public string GameMode
        {
            get => _gameMode;
            set
            {
                if (_gameMode != value)
                {
                    _gameMode = value;
                    _isHumanVsHuman = (value == "Человек vs Человек");
                    OnPropertyChanged(nameof(GameMode));

                    // Если переключаемся в режим ИИ, проверяем чей сейчас ход
                    if (!_isHumanVsHuman && _isGameActive)
                    {
                        CheckAITurn();
                    }
                }
            }
        }

        public string Difficulty
        {
            get => _difficulty;
            set
            {
                _difficulty = value;
                OnPropertyChanged(nameof(Difficulty));
            }
        }

        public Position SelectedPosition
        {
            get => _selectedPosition;
            set
            {
                _selectedPosition = value;
                OnPropertyChanged(nameof(SelectedPosition));
            }
        }

        public bool EnableMoveHighlighting
        {
            get => _enableMoveHighlighting;
            set
            {
                if (_enableMoveHighlighting != value)
                {
                    _enableMoveHighlighting = value;
                    OnPropertyChanged(nameof(EnableMoveHighlighting));

                    if (_board != null && _selectedPosition.IsValid())
                    {
                        UpdateMoveHighlighting();
                    }
                }
            }
        }

        public ICommand StartNewGameCommand { get; }
        public ICommand CellClickCommand { get; }

        public double AnimationProgress => _animationProgress;
        public bool IsAnimating => _animationTimer?.IsEnabled ?? false;

        // ДОБАВЛЕНО: Метод для обновления статуса ИИ в MainWindow
        private void UpdateAITurnInMainWindow(bool isThinking)
        {
            if (Application.Current.Windows.OfType<MainWindow>().FirstOrDefault() is MainWindow window)
            {
                window.UpdateAIThinking(isThinking);
            }
        }

        // ИСПРАВЛЕНО: Явно устанавливаем текущего игрока при старте новой игры
        public void StartNewGame()
        {
            _gameManager?.StartNewGame(GameMode, Difficulty);
            Board = _gameManager?.Board;
            SelectedPosition = Position.Invalid;

            // Сбрасываем состояние игры
            _isGameActive = true;

            // Явно устанавливаем первого игрока
            if (_gameManager != null)
            {
                // Определяем первого игрока
                if (_gameManager.CurrentPlayer == PieceColor.White)
                {
                    CurrentPlayerColor = "Белые";
                }
                else
                {
                    CurrentPlayerColor = "Черные";
                }
            }
            else
            {
                CurrentPlayerColor = "Белые";
            }

            // Проверяем, нужно ли ИИ делать ход
            CheckAITurn();

            // Уведомляем MainWindow о начале игры
            NotifyMainWindowGameStarted();
        }

        private void NotifyMainWindowGameStarted()
        {
            if (Application.Current.Windows.OfType<MainWindow>().FirstOrDefault() is MainWindow window)
            {
                // Устанавливаем текущего игрока в UI
                window.UpdateCurrentPlayer(CurrentPlayerColor);

                // Запускаем таймеры
                if (CurrentPlayerColor == "Белые")
                {
                    window.SwitchTurn("Белые");
                }
                else
                {
                    window.SwitchTurn("Черные");
                }
            }
        }

        private void UpdateMoveHighlighting()
        {
            if (!_enableMoveHighlighting)
            {
                foreach (var cell in Board.CellsFlat)
                {
                    cell.IsPossibleMove = false;
                }
            }
            else if (_selectedPosition.IsValid())
            {
                var piece = Board.GetPieceAt(_selectedPosition);
                if (piece != null && piece.Color == _gameManager?.CurrentPlayer)
                {
                    var possibleMoves = piece.GetPossibleMoves(_selectedPosition, Board);
                    foreach (var move in possibleMoves)
                    {
                        var cell = GetCellAt(move);
                        if (cell != null)
                        {
                            cell.IsPossibleMove = true;
                        }
                    }
                }
            }
            OnPropertyChanged(nameof(Board));
        }

        public async Task<bool> CheckApiConnection()
        {
            return await _apiService.TestConnectionAsync();
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var user = await _apiService.LoginAsync(username, password);
            if (user != null)
            {
                _currentUserId = user.Id;
                return true;
            }
            return false;
        }

        public async Task LoadActiveGamesAsync()
        {
            var games = await _apiService.GetActiveGamesAsync();
            ActiveGames = games ?? new List<ApiGameDto>();
        }

        public async Task<ApiGameDto> CreateNewGame(string gameMode, string difficulty)
        {
            var createDto = new ApiCreateGameDto
            {
                WhitePlayerId = _currentUserId,
                GameMode = gameMode,
                Difficulty = difficulty
            };
            return await _apiService.CreateGameAsync(createDto);
        }

        public void SelectGame(int gameId)
        {
            Console.WriteLine($"Выбрана игра: {gameId}");
        }

        private void InitializeAnimationTimer()
        {
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(50);
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        // ИСПРАВЛЕНО: Явно обновляем текущего игрока при изменении в GameManager
        private void GameManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_gameManager == null) return;

            if (e.PropertyName == nameof(GameManager.CurrentPlayer))
            {
                // Обновляем CurrentPlayerColor на основе GameManager
                if (_gameManager.CurrentPlayer == PieceColor.White)
                {
                    CurrentPlayerColor = "Белые";
                }
                else
                {
                    CurrentPlayerColor = "Черные";
                }

                OnPropertyChanged(nameof(CurrentPlayerText));
                CheckAITurn();
            }
            else if (e.PropertyName == nameof(GameManager.Board))
            {
                Board = _gameManager.Board;
            }
        }

        // ИСПРАВЛЕНО: Четкая логика определения хода ИИ
        private void CheckAITurn()
        {
            if (_gameManager == null || !_isGameActive) return;

            bool shouldBeAITurn = false;

            if (_gameMode == "Человек vs Компьютер")
            {
                // В этом режиме ИИ играет за черных
                shouldBeAITurn = (_gameManager.CurrentPlayer == PieceColor.Black);
            }
            else if (_gameMode == "Компьютер vs Компьютер")
            {
                // В этом режиме ИИ играет за обоих
                shouldBeAITurn = true;
            }

            if (shouldBeAITurn && _gameManager.IsGameInProgress && !IsAITurn)
            {
                IsAITurn = true;
                _ = MakeAIMoveAsync();
            }
            else if (!shouldBeAITurn)
            {
                IsAITurn = false;
            }
        }

        private async Task MakeAIMoveAsync()
        {
            try
            {
                // Уведомляем UI, что ИИ думает
                UpdateAITurnInMainWindow(true);

                // Имитация размышлений ИИ
                int delay = _difficulty switch
                {
                    "Новичок" => 1500,
                    "Лёгкий" => 1000,
                    "Средний" => 700,
                    "Сложный" => 400,
                    "Эксперт" => 200,
                    _ => 500
                };

                await Task.Delay(delay);

                // Делаем ход ИИ
                await _gameManager?.MakeAIMoveAsync();

                // После хода ИИ снова проверяем, чей ход
                CheckAITurn();
            }
            finally
            {
                UpdateAITurnInMainWindow(false);
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            _animationProgress += 0.1;
            if (_animationProgress >= 1.0)
            {
                _animationProgress = 0;
                _animationTimer.Stop();
                CompleteAnimation();
            }
            OnPropertyChanged(nameof(AnimationProgress));
        }

        // ИСПРАВЛЕНО: Проверка возможности хода и обновление статуса
        public async void HandleCellClick(Position position)
        {
            if (IsAnimating || !position.IsValid() || !_isGameActive) return;

            // Если сейчас ход ИИ - игнорируем клики
            if (IsAITurn) return;

            var clickedPiece = Board.GetPieceAt(position);

            // Если кликаем на свою фигуру - выбираем ее
            if (clickedPiece != null &&
                clickedPiece.Color == _gameManager?.CurrentPlayer)
            {
                SelectPiece(position);
                return;
            }

            // Если фигура выбрана и кликаем на клетку для хода
            if (SelectedPosition.IsValid() &&
                clickedPiece?.Color != _gameManager?.CurrentPlayer)
            {
                await TryMakeMove(SelectedPosition, position);
            }
        }

        private void SelectPiece(Position position)
        {
            ResetSelection();

            SelectedPosition = position;
            var piece = Board.GetPieceAt(position);

            if (piece != null)
            {
                if (EnableMoveHighlighting)
                {
                    var possibleMoves = piece.GetPossibleMoves(position, Board);
                    foreach (var move in possibleMoves)
                    {
                        var cell = GetCellAt(move);
                        if (cell != null)
                        {
                            cell.IsPossibleMove = true;
                        }
                    }
                }

                var selectedCell = GetCellAt(position);
                if (selectedCell != null)
                {
                    selectedCell.IsSelected = true;
                }
            }

            OnPropertyChanged(nameof(Board));
        }

        private void ResetSelection()
        {
            foreach (var cell in Board.CellsFlat)
            {
                cell.IsSelected = false;
                cell.IsPossibleMove = false;
            }
            SelectedPosition = Position.Invalid;
        }

        // ИСПРАВЛЕНО: После успешного хода обновляем UI в MainWindow
        private async Task<bool> TryMakeMove(Position from, Position to)
        {
            bool moveMade = await _gameManager.MakeMove(from, to);

            if (moveMade)
            {
                StartAnimation(from, to);

                // Уведомляем MainWindow о смене хода
                if (Application.Current.Windows.OfType<MainWindow>().FirstOrDefault() is MainWindow window)
                {
                    window.UpdateCurrentPlayer(CurrentPlayerColor);
                    window.SwitchTurn(CurrentPlayerColor);
                }

                return true;
            }
            else
            {
                ResetSelection();
                return false;
            }
        }

        private void StartAnimation(Position from, Position to)
        {
            _animationFrom = from;
            _animationTo = to;
            _animatingPiece = Board.GetPieceAt(from);
            _animationProgress = 0;
            _animationTimer.Start();
            PlayMoveSound();
        }

        private void CompleteAnimation()
        {
            OnPropertyChanged(nameof(Board));
            OnPropertyChanged(nameof(CurrentPlayerText));
            ResetSelection();
        }

        private BoardCell GetCellAt(Position position)
        {
            if (!position.IsValid()) return null;
            return Board.Cells[position.Row, position.Column];
        }

        private void PlayMoveSound()
        {
            // System.Media.SystemSounds.Beep.Play();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}