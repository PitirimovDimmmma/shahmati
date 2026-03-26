using shahmati.Helpers;
using shahmati.models;
using shahmati.Models;
using shahmati.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

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
        private bool _userIsBlack = false;
        private bool _isGameStarting = false;

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

            _gameManager.InitializeApiService(_apiService);

            InitializeAnimationTimer();

            StartNewGameCommand = new RelayCommand(async () => await StartNewGameAsync());
            CellClickCommand = new RelayCommand<Position>(HandleCellClick);
            ResignCommand = new RelayCommand(async () => await ResignAsync());

            _gameManager.PropertyChanged += GameManager_PropertyChanged;

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

        public GameManager GameManager => _gameManager;
        public bool IsGameStarting => _isGameStarting;

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
            _userIsBlack = !isWhite;
            if (_gameManager != null)
                _gameManager.UserIsWhite = isWhite;
            OnPropertyChanged(nameof(CurrentPlayerDisplay));
            OnPropertyChanged(nameof(UserIsWhite));
            OnPropertyChanged(nameof(UserIsBlack));
        }

        public bool UserIsWhite
        {
            get => _userIsWhite;
            set
            {
                if (_userIsWhite != value)
                {
                    _userIsWhite = value;
                    _userIsBlack = !value;
                    OnPropertyChanged(nameof(UserIsWhite));
                    OnPropertyChanged(nameof(UserIsBlack));
                    SetUserIsWhite(value);
                }
            }
        }

        public bool UserIsBlack
        {
            get => _userIsBlack;
            set
            {
                if (_userIsBlack != value)
                {
                    _userIsBlack = value;
                    _userIsWhite = !value;
                    OnPropertyChanged(nameof(UserIsBlack));
                    OnPropertyChanged(nameof(UserIsWhite));
                    SetUserIsWhite(!value);
                }
            }
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

        public string AITurnStatus
        {
            get
            {
                if (IsAITurn)
                    return "🤖 ИИ думает...";
                if (_gameManager?.IsAITurn == true)
                    return "🤖 ИИ думает...";
                return "✅ Очередь игрока";
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
                OnPropertyChanged(nameof(AITurnStatus));
            }
        }

        public bool IsPlayerTurn => !_isAITurn;

        public ICommand StartNewGameCommand { get; }
        public ICommand CellClickCommand { get; }
        public ICommand ResignCommand { get; }

        public double AnimationProgress => _animationProgress;
        public bool IsAnimating => _animationTimer?.IsEnabled ?? false;

        // ========== МЕТОДЫ ДЛЯ ПОДСВЕТКИ ВСЕХ ХОДОВ ==========

        /// <summary>
        /// Получает ВСЕ возможные ходы для фигуры (включая заблокированные)
        /// Используется для подсветки при планировании стратегии
        /// </summary>
        private List<Position> GetAllPossibleMovesForPiece(ChessPiece piece, Position position, Board board)
        {
            var allMoves = new List<Position>();

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    allMoves = GetAllPawnMoves(position, piece.Color, board);
                    break;
                case PieceType.Knight:
                    allMoves = GetAllKnightMoves(position, board);
                    break;
                case PieceType.Bishop:
                    allMoves = GetAllBishopMoves(position, board);
                    break;
                case PieceType.Rook:
                    allMoves = GetAllRookMoves(position, board);
                    break;
                case PieceType.Queen:
                    allMoves = GetAllQueenMoves(position, board);
                    break;
                case PieceType.King:
                    allMoves = GetAllKingMoves(position, board);
                    break;
            }

            return allMoves;
        }

        // Пешка - показывает все диагонали и ходы вперед
        private List<Position> GetAllPawnMoves(Position pos, PieceColor color, Board board)
        {
            var moves = new List<Position>();
            int direction = color == PieceColor.White ? -1 : 1;
            int startRow = color == PieceColor.White ? 6 : 1;

            // Ход вперед на 1
            var oneForward = new Position(pos.Row + direction, pos.Column);
            if (oneForward.IsValid())
            {
                moves.Add(oneForward);

                // Ход вперед на 2 с начальной позиции
                if (pos.Row == startRow)
                {
                    var twoForward = new Position(pos.Row + 2 * direction, pos.Column);
                    if (twoForward.IsValid())
                    {
                        moves.Add(twoForward);
                    }
                }
            }

            // Диагонали ВСЕГДА показываем (для планирования)
            var leftDiag = new Position(pos.Row + direction, pos.Column - 1);
            var rightDiag = new Position(pos.Row + direction, pos.Column + 1);

            if (leftDiag.IsValid())
            {
                moves.Add(leftDiag);
            }
            if (rightDiag.IsValid())
            {
                moves.Add(rightDiag);
            }

            return moves;
        }

        // Конь - показывает все 8 ходов
        private List<Position> GetAllKnightMoves(Position pos, Board board)
        {
            var moves = new List<Position>();
            int[,] knightMoves = {
                {2, 1}, {2, -1}, {-2, 1}, {-2, -1},
                {1, 2}, {1, -2}, {-1, 2}, {-1, -2}
            };

            for (int i = 0; i < knightMoves.GetLength(0); i++)
            {
                var newPos = new Position(
                    pos.Row + knightMoves[i, 0],
                    pos.Column + knightMoves[i, 1]
                );

                if (newPos.IsValid())
                {
                    moves.Add(newPos);
                }
            }

            return moves;
        }

        // Слон - показывает все диагонали ДО КОНЦА (игнорируя фигуры)
        private List<Position> GetAllBishopMoves(Position pos, Board board)
        {
            var moves = new List<Position>();
            int[,] directions = { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };

            for (int d = 0; d < directions.GetLength(0); d++)
            {
                for (int i = 1; i < 8; i++)
                {
                    var newPos = new Position(
                        pos.Row + i * directions[d, 0],
                        pos.Column + i * directions[d, 1]
                    );

                    if (!newPos.IsValid()) break;
                    moves.Add(newPos);
                }
            }

            return moves;
        }

        // Ладья - показывает все вертикали и горизонтали ДО КОНЦА
        private List<Position> GetAllRookMoves(Position pos, Board board)
        {
            var moves = new List<Position>();
            int[] directions = { -1, 1 };

            // Горизонталь
            foreach (var dir in directions)
            {
                for (int i = 1; i < 8; i++)
                {
                    var newPos = new Position(pos.Row, pos.Column + i * dir);
                    if (!newPos.IsValid()) break;
                    moves.Add(newPos);
                }
            }

            // Вертикаль
            foreach (var dir in directions)
            {
                for (int i = 1; i < 8; i++)
                {
                    var newPos = new Position(pos.Row + i * dir, pos.Column);
                    if (!newPos.IsValid()) break;
                    moves.Add(newPos);
                }
            }

            return moves;
        }

        // Ферзь - комбинация слона и ладьи
        private List<Position> GetAllQueenMoves(Position pos, Board board)
        {
            var moves = new List<Position>();
            moves.AddRange(GetAllRookMoves(pos, board));
            moves.AddRange(GetAllBishopMoves(pos, board));
            return moves.Distinct().ToList();
        }

        // Король - показывает все соседние клетки
        private List<Position> GetAllKingMoves(Position pos, Board board)
        {
            var moves = new List<Position>();
            int[,] kingMoves = {
                {1, 0}, {-1, 0}, {0, 1}, {0, -1},
                {1, 1}, {1, -1}, {-1, 1}, {-1, -1}
            };

            for (int i = 0; i < kingMoves.GetLength(0); i++)
            {
                var newPos = new Position(
                    pos.Row + kingMoves[i, 0],
                    pos.Column + kingMoves[i, 1]
                );

                if (newPos.IsValid())
                {
                    moves.Add(newPos);
                }
            }

            return moves;
        }

        // ========== ОСТАЛЬНЫЕ МЕТОДЫ ==========

        public async Task TestAIConnection()
        {
            try
            {
                Console.WriteLine("=== ТЕСТ ПОДКЛЮЧЕНИЯ К ИИ ===");

                var isConnected = await _apiService.TestConnectionAsync();
                Console.WriteLine($"Тест 1 - Общее подключение к API: {(isConnected ? "✅" : "❌")}");

                try
                {
                    var testResponse = await _apiService.GetAIDifficultiesAsync();
                    Console.WriteLine($"Тест 2 - Получение уровней сложности: {(testResponse != null ? "✅" : "❌")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Тест 2 - Ошибка: {ex.Message}");
                }

                if (_currentUserId > 0)
                {
                    var game = await _apiService.CreateAIGameAsync(_currentUserId, "Easy", "White");
                    if (game?.Success == true)
                    {
                        Console.WriteLine($"Тест 3 - Создание игры: ✅ (GameId={game.GameId})");

                        var moveResult = await _apiService.PlayAgainstAIAsync(game.GameId, "e2e4");
                        if (moveResult?.Success == true)
                        {
                            Console.WriteLine($"Тест 4 - Ход в игре: ✅ (Ответ ИИ: {moveResult.AIMove})");
                        }
                        else
                        {
                            Console.WriteLine($"Тест 4 - Ход в игре: ❌");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Тест 3 - Создание игры: ❌");
                    }
                }

                Console.WriteLine("=== ТЕСТ ЗАВЕРШЕН ===");

                MessageBox.Show("Тест API завершен. Проверьте консоль для деталей.\n\n" +
                               "Если есть ошибки, проверьте:\n" +
                               "1. Запущен ли API (https://localhost:7259)\n" +
                               "2. Есть ли папка Engines с stockfish.exe\n" +
                               "3. Логи в консоли API",
                               "Тест API", MessageBoxButton.OK,
                               isConnected ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка теста: {ex.Message}");
                MessageBox.Show($"Ошибка теста API: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task StartNewGameAsync()
        {
            if (_isGameStarting) return;
            _isGameStarting = true;

            try
            {
                Console.WriteLine($"=== НАЧАЛО НОВОЙ ИГРЫ ===");
                Console.WriteLine($"UserId: {_currentUserId}");
                Console.WriteLine($"Difficulty: {_difficulty}");
                Console.WriteLine($"UserIsWhite: {_userIsWhite}");
                Console.WriteLine($"GameMode: {_gameMode}");

                _isGameActive = true;

                await _gameManager.StartNewGameAsync(
                    gameMode: _gameMode,
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

                if (_gameMode == "Человек vs Компьютер" && _currentUserId > 0 && _gameManager != null)
                {
                    Console.WriteLine($"Создание игры на сервере...");
                    int gameId = await _gameManager.CreateAIGameOnServerAsync(
                        _currentUserId,
                        _difficulty,
                        _userIsWhite ? "White" : "Black"
                    );

                    if (gameId > 0)
                    {
                        Console.WriteLine($"✅ Игра создана на сервере: ID={gameId}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Не удалось создать игру на сервере");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ Игра без сервера (режим: {_gameMode})");
                }

                PlayerTurnChanged?.Invoke(CurrentPlayerColor);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при создании игры: {ex.Message}");
            }
            finally
            {
                _isGameStarting = false;
            }
        }

        [Obsolete("Используйте StartNewGameAsync()")]
        public void StartNewGame()
        {
            Console.WriteLine("⚠️ Вызван устаревший метод StartNewGame()");
            _ = StartNewGameAsync();
        }

        public async Task ResignAsync()
        {
            if (_gameManager != null && _isGameActive)
            {
                await _gameManager.ResignAsync(_gameManager.CurrentPlayer);
            }
        }

        public async void HandleCellClick(Position position)
        {
            if (IsAnimating || !position.IsValid() || !_isGameActive || IsAITurn)
                return;

            var clickedPiece = Board.GetPieceAt(position);

            // Если кликнули на фигуру
            if (clickedPiece != null)
            {
                // Если это своя фигура - выбираем её
                if (clickedPiece.Color == _gameManager?.CurrentPlayer)
                {
                    if (_gameMode == "Человек vs Компьютер" && IsAITurn)
                        return;
                    SelectPiece(position);
                }
                // Если это чужая фигура И есть выбранная фигура - пытаемся сделать ход
                else if (SelectedPosition.IsValid())
                {
                    var selectedPiece = Board.GetPieceAt(SelectedPosition);
                    if (selectedPiece?.Color == _gameManager?.CurrentPlayer)
                    {
                        await TryMakeMove(SelectedPosition, position);
                    }
                }
                return;
            }

            // Если кликнули на пустую клетку и есть выбранная фигура
            if (SelectedPosition.IsValid())
            {
                var selectedPiece = Board.GetPieceAt(SelectedPosition);
                if (selectedPiece?.Color == _gameManager?.CurrentPlayer)
                {
                    await TryMakeMove(SelectedPosition, position);
                }
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
                    var validMoves = piece.GetPossibleMoves(position, Board);
                    foreach (var move in validMoves)
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

        private async Task<bool> TryMakeMove(Position from, Position to)
        {
            Console.WriteLine($"=== ПОПЫТКА ХОДА ===");
            Console.WriteLine($"From: {GetSquareNotation(from)}");
            Console.WriteLine($"To: {GetSquareNotation(to)}");
            Console.WriteLine($"GameMode: {_gameMode}");
            Console.WriteLine($"CurrentUserId: {_currentUserId}");

            bool moveMade = false;

            if (_gameMode == "Человек vs Компьютер")
            {
                moveMade = await _gameManager.MakeMoveVsAIAsync(from, to, _currentUserId);
                Console.WriteLine($"Результат хода против ИИ: {moveMade}");
            }
            else
            {
                moveMade = await _gameManager.MakeMove(from, to);
                Console.WriteLine($"Результат обычного хода: {moveMade}");
            }

            if (moveMade)
            {
                StartAnimation(from, to);
                ResetSelection();
                PlayerTurnChanged?.Invoke(CurrentPlayerColor);
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Ход не удался");
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
                    var possibleMoves = GetAllPossibleMovesForPiece(piece, _selectedPosition, Board);
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
            Console.WriteLine($"=== ПОПЫТКА ЛОГИНА ===");
            Console.WriteLine($"Username: {username}");

            var user = await _apiService.LoginAsync(username, password);
            if (user != null)
            {
                _currentUserId = user.Id;
                Console.WriteLine($"✅ Логин успешен: UserId={_currentUserId}");
                return true;
            }

            Console.WriteLine($"❌ Логин failed");
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

        private string GetSquareNotation(Position position)
        {
            char file = (char)('a' + position.Column);
            int rank = 8 - position.Row;
            return $"{file}{rank}";
        }

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
        }

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

        public void ForceBoardUpdate()
        {
            if (GameManager?.Board != null)
            {
                GameManager.Board.ForceUpdate();
                OnPropertyChanged(nameof(GameManager));
                OnPropertyChanged(nameof(GameManager.Board));
                OnPropertyChanged(nameof(GameManager.Board.CellsFlat));
                OnPropertyChanged(nameof(Board));
                OnPropertyChanged(nameof(Board.CellsFlat));

                Console.WriteLine("✅ Принудительное обновление доски выполнено");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}