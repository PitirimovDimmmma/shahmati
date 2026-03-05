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
using System.Linq;

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
        private string _gameMode = "Человек vs Компьютер";
        private string _difficulty = "Medium";
        private bool _isAITurn;
        private readonly ApiService _apiService;
        private int _currentUserId;
        private List<ApiGameDto> _activeGames;
        private string _currentPlayerDisplayColor = "Белые";
        private bool _enableMoveHighlighting = true;
        private bool _isHumanVsHuman = false;
        private bool _isGameActive = false;
        private bool _userIsWhite = true;

        // Событие для уведомления MainWindow о смене хода
        public event Action<string> PlayerTurnChanged;
        public event Action<string> AIMoveStarted;
        public event Action<string> AIMoveCompleted;
        public event Action<string> GameFinished;

        public MainViewModel(int? userId = null)
        {
            _gameManager = new GameManager();
            _board = _gameManager.Board;
            _selectedPosition = Position.Invalid;
            _apiService = new ApiService();

            // ИНИЦИАЛИЗАЦИЯ API В GAMEMANAGER
            _gameManager.InitializeApiService(_apiService);

            InitializeAnimationTimer();

            // ИСПРАВЛЕНО: используем асинхронный метод
            StartNewGameCommand = new RelayCommand(async () => await StartNewGameAsync());

            CellClickCommand = new RelayCommand<Position>(HandleCellClick);

            _gameManager.PropertyChanged += GameManager_PropertyChanged;

            // ИСПРАВЛЕНО: перенаправляем события через ViewModel
            _gameManager.AIMoveStarted += (msg) => AIMoveStarted?.Invoke(msg);
            _gameManager.AIMoveCompleted += (move) => AIMoveCompleted?.Invoke(move);
            _gameManager.GameFinished += (result) => GameFinished?.Invoke(result);

            if (userId.HasValue)
            {
                _currentUserId = userId.Value;
            }
            else
            {
                _currentUserId = 0;
            }

            _currentPlayerDisplayColor = "Белые";
        }

        public MainViewModel() : this(null)
        {
        }

        // Публичное свойство для доступа к GameManager
        public GameManager GameManager => _gameManager;

        public Board Board
        {
            get => _board;
            private set
            {
                _board = value;
                OnPropertyChanged(nameof(Board));
            }
        }

        public void SetUserIsWhite(bool isWhite)
        {
            _userIsWhite = isWhite;
            if (_gameManager != null)
                _gameManager.UserIsWhite = isWhite;
            OnPropertyChanged(nameof(CurrentPlayerDisplay));
        }

        public string CurrentPlayerDisplay
        {
            get
            {
                if (GameManager?.CurrentPlayer == null)
                    return "Белые";

                if (_userIsWhite)
                {
                    return GameManager.CurrentPlayer == PieceColor.White
                        ? "Ваш ход (Белые)"
                        : "Противник (Черные)";
                }
                else
                {
                    return GameManager.CurrentPlayer == PieceColor.White
                        ? "Противник (Белые)"
                        : "Ваш ход (Черные)";
                }
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

        public string CurrentPlayerColor
        {
            get => _currentPlayerDisplayColor;
            private set
            {
                if (_currentPlayerDisplayColor != value)
                {
                    _currentPlayerDisplayColor = value;
                    OnPropertyChanged(nameof(CurrentPlayerColor));
                    PlayerTurnChanged?.Invoke(value);
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

        public bool IsAITurn
        {
            get => _isAITurn;
            set
            {
                _isAITurn = value;
                OnPropertyChanged(nameof(IsAITurn));
                OnPropertyChanged(nameof(IsPlayerTurn));
            }
        }

        public bool IsPlayerTurn => !_isAITurn;

        public ICommand StartNewGameCommand { get; }
        public ICommand CellClickCommand { get; }

        public double AnimationProgress => _animationProgress;
        public bool IsAnimating => _animationTimer?.IsEnabled ?? false;

        // ===== МЕТОД НОВОЙ ИГРЫ =====
        public async Task StartNewGameAsync()
        {
            try
            {
                _isGameActive = true;

                // Начинаем игру с ИИ
                await _gameManager.StartNewGameAsync(
                    gameMode: "Человек vs Компьютер",
                    difficulty: _difficulty,
                    userIsWhite: _userIsWhite
                );

                Board = _gameManager.Board;
                SelectedPosition = Position.Invalid;
                ResetSelection();

                if (_gameManager.CurrentPlayer == PieceColor.White)
                {
                    CurrentPlayerColor = "Белые";
                }
                else
                {
                    CurrentPlayerColor = "Черные";
                }

                // Создаем игру на сервере
                if (_currentUserId > 0)
                {
                    await _gameManager.CreateAIGameOnServerAsync(
                        _currentUserId,
                        _difficulty,
                        _userIsWhite ? "White" : "Black"
                    );
                }

                // Уведомляем через события вместо прямого вызова
                PlayerTurnChanged?.Invoke(CurrentPlayerColor);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании игры: {ex.Message}");
            }
        }

        // СТАРЫЙ МЕТОД - больше не используется
        [Obsolete("Используйте StartNewGameAsync()")]
        public void StartNewGame()
        {
            Console.WriteLine("⚠️ Вызван устаревший метод StartNewGame()");
            _ = StartNewGameAsync();
        }

        // ===== ОБРАБОТЧИК КЛИКА ПО КЛЕТКЕ =====
        public async void HandleCellClick(Position position)
        {
            if (IsAnimating || !position.IsValid() || !_isGameActive || IsAITurn)
                return;

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

        // ===== МЕТОД ХОДА =====
        private async Task<bool> TryMakeMove(Position from, Position to)
        {
            bool moveMade = false;

            if (_gameMode == "Человек vs Компьютер")
            {
                moveMade = await _gameManager.MakeMoveVsAIAsync(from, to, _currentUserId);
            }
            else
            {
                moveMade = await _gameManager.MakeMove(from, to);
            }

            if (moveMade)
            {
                StartAnimation(from, to);
                ResetSelection();

                // Уведомляем через события вместо прямого вызова
                PlayerTurnChanged?.Invoke(CurrentPlayerColor);

                return true;
            }
            else
            {
                ResetSelection();
                return false;
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

        // ===== УДАЛЕНЫ ПРЯМЫЕ ВЫЗОВЫ MAINWINDOW =====
        // Вместо них используем события AIMoveStarted, AIMoveCompleted, GameFinished

        // ===== МЕТОДЫ ДЛЯ РАБОТЫ С API =====
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

        // ===== АНИМАЦИЯ =====
        private void InitializeAnimationTimer()
        {
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(50);
            _animationTimer.Tick += AnimationTimer_Tick;
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

        // ===== ОБРАБОТЧИК ИЗМЕНЕНИЙ GAMEMANAGER =====
        private void GameManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_gameManager == null) return;

            if (e.PropertyName == nameof(GameManager.CurrentPlayer))
            {
                if (_gameManager.CurrentPlayer == PieceColor.White)
                {
                    CurrentPlayerColor = "Белые";
                }
                else
                {
                    CurrentPlayerColor = "Черные";
                }
                OnPropertyChanged(nameof(CurrentPlayerText));
            }
            else if (e.PropertyName == nameof(GameManager.Board))
            {
                Board = _gameManager.Board;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}