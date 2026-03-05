using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using shahmati.models;
using shahmati.Services;

namespace shahmati.Services
{
    public class GameManager : INotifyPropertyChanged
    {
        // ========== ПОЛЯ ==========
        private Board _board;
        private PieceColor _currentPlayer;
        private bool _isGameActive;
        private string _gameResult;
        private List<string> _moveHistory;
        private string _gameMode;
        private string _difficulty;

        // API поля
        private ApiService _apiService;
        private int _currentGameId;
        private string _gameDifficulty = "Medium";
        private bool _isAITurn = false;

        // ========== СВОЙСТВА ==========
        public bool UserIsWhite { get; set; } = true;
        public string GameMode => _gameMode;

        public Board Board
        {
            get => _board;
            private set
            {
                _board = value;
                OnPropertyChanged(nameof(Board));
            }
        }

        public PieceColor CurrentPlayer
        {
            get => _currentPlayer;
            private set
            {
                _currentPlayer = value;
                OnPropertyChanged(nameof(CurrentPlayer));
            }
        }

        public bool IsGameInProgress => _isGameActive;
        public string GameResult => _gameResult;
        public List<string> MoveHistory => _moveHistory;
        public bool IsAITurn => _isAITurn;

        // ========== СОБЫТИЯ ==========
        public event Action<string> GameFinished;
        public event Action<string> MoveMade;
        public event Action<string> AIMoveStarted;
        public event Action<string> AIMoveCompleted;
        public event PropertyChangedEventHandler PropertyChanged;
        public Action<string> UpdateHistoryCallback { get; set; }

        // ========== КОНСТРУКТОР ==========
        public GameManager()
        {
            _board = new Board();
            _currentPlayer = PieceColor.White;
            _isGameActive = true;
            _moveHistory = new List<string>();
        }

        // ========== ИНИЦИАЛИЗАЦИЯ API ==========
        public void InitializeApiService(ApiService apiService)
        {
            _apiService = apiService;
            Console.WriteLine("✅ GameManager: API Service инициализирован");
        }

        // ========== НАЧАЛО ИГРЫ ==========
        public async Task StartNewGameAsync(string gameMode = "Человек vs Компьютер",
                                            string difficulty = "Medium",
                                            bool userIsWhite = true)
        {
            _board = new Board();
            _currentPlayer = PieceColor.White;
            _isGameActive = true;
            _gameResult = null;
            _moveHistory.Clear();
            _gameMode = gameMode;
            _difficulty = difficulty;
            _gameDifficulty = difficulty;
            UserIsWhite = userIsWhite;
            _isAITurn = false;
            _currentGameId = 0;

            OnPropertyChanged(nameof(Board));
            OnPropertyChanged(nameof(CurrentPlayer));

            UpdateHistoryCallback?.Invoke("Новая игра начата!");

            // Если игра с ИИ и пользователь черными - ИИ ходит первым
            if (gameMode == "Человек vs Компьютер" && !userIsWhite)
            {
                await MakeAIMoveViaApiAsync();
            }
        }

        // ========== СОЗДАНИЕ ИГРЫ НА СЕРВЕРЕ ==========
        public async Task<int> CreateAIGameOnServerAsync(int userId, string difficulty = "Medium", string color = "White")
        {
            try
            {
                if (_apiService == null)
                {
                    Console.WriteLine("❌ API Service не инициализирован");
                    return 0;
                }

                var response = await _apiService.CreateAIGameAsync(userId, difficulty, color);

                if (response?.Success == true)
                {
                    _currentGameId = response.GameId;
                    Console.WriteLine($"✅ Игра #{_currentGameId} создана на сервере");
                    return _currentGameId;
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось создать игру на сервере");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка создания игры: {ex.Message}");
                return 0;
            }
        }

        // ========== ХОД ПОЛЬЗОВАТЕЛЯ ПРОТИВ ИИ ==========
        public async Task<bool> MakeMoveVsAIAsync(Position from, Position to, int userId)
        {
            if (!_isGameActive || _isAITurn)
                return false;

            var piece = Board.GetPieceAt(from);
            if (piece == null || piece.Color != CurrentPlayer)
                return false;

            // Проверяем валидность хода локально
            if (!Board.IsValidMove(from, to, CurrentPlayer))
                return false;

            string moveNotation = $"{GetSquareNotation(from)}{GetSquareNotation(to)}";

            // Если есть активная игра на сервере - отправляем ход
            if (_currentGameId > 0 && _apiService != null && _gameMode == "Человек vs Компьютер")
            {
                return await MakeMoveViaApiAsync(moveNotation);
            }
            else
            {
                // Локальный ход (без сервера)
                return MakeMoveLocal(from, to);
            }
        }

        // ========== ОТПРАВКА ХОДА НА СЕРВЕР ==========
        private async Task<bool> MakeMoveViaApiAsync(string moveNotation)
        {
            try
            {
                _isAITurn = true;
                AIMoveStarted?.Invoke("Stockfish думает...");

                var response = await _apiService.PlayAgainstAIAsync(_currentGameId, moveNotation);

                if (response?.Success == true)
                {
                    // Применяем ход пользователя
                    string userFrom = moveNotation.Substring(0, 2);
                    string userTo = moveNotation.Substring(2, 2);

                    var fromPos = SquareToPosition(userFrom);
                    var toPos = SquareToPosition(userTo);

                    // Делаем ход пользователя
                    Board.MovePiece(fromPos, toPos);
                    Board.UpdateSquaresFromCells();

                    string userNotation = $"{GetSquareNotation(fromPos)}{GetSquareNotation(toPos)}";
                    _moveHistory.Add($"{_moveHistory.Count + 1}. {userNotation}");
                    UpdateHistoryCallback?.Invoke(string.Join("\n", _moveHistory));
                    MoveMade?.Invoke(userNotation);

                    // Проверяем, не закончилась ли игра
                    if (response.GameFinished)
                    {
                        EndGame(GetResultMessage(response.Result));
                        _isAITurn = false;
                        return true;
                    }

                    // Применяем ход ИИ
                    if (!string.IsNullOrEmpty(response.AIMove) && response.AIMove.Length >= 4)
                    {
                        string aiFrom = response.AIMove.Substring(0, 2);
                        string aiTo = response.AIMove.Substring(2, 2);

                        var aiFromPos = SquareToPosition(aiFrom);
                        var aiToPos = SquareToPosition(aiTo);

                        // Делаем ход ИИ
                        if (aiFromPos.IsValid() && aiToPos.IsValid())
                        {
                            Board.MovePiece(aiFromPos, aiToPos);
                            Board.UpdateSquaresFromCells();

                            string aiNotation = $"{GetSquareNotation(aiFromPos)}{GetSquareNotation(aiToPos)}";
                            _moveHistory.Add($"{_moveHistory.Count + 1}. {aiNotation}");
                            UpdateHistoryCallback?.Invoke(string.Join("\n", _moveHistory));

                            AIMoveCompleted?.Invoke(response.AIMove);
                            MoveMade?.Invoke(aiNotation);
                        }
                    }

                    // Обновляем текущего игрока
                    CurrentPlayer = CurrentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;
                    OnPropertyChanged(nameof(CurrentPlayer));

                    // Проверяем завершение игры
                    if (response.GameFinished)
                    {
                        EndGame(GetResultMessage(response.Result));
                    }

                    _isAITurn = false;
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка при ходе: {response?.Message}");
                    _isAITurn = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка API хода: {ex.Message}");
                _isAITurn = false;
                return false;
            }
        }

        // ========== ЛОКАЛЬНЫЙ ХОД ==========
        private bool MakeMoveLocal(Position from, Position to)
        {
            var capturedPiece = Board.GetPieceAt(to);

            // Делаем ход
            Board.MovePiece(from, to);
            Board.UpdateSquaresFromCells();

            string moveNotation = $"{GetSquareNotation(from)}{GetSquareNotation(to)}";
            _moveHistory.Add($"{_moveHistory.Count + 1}. {moveNotation}");
            UpdateHistoryCallback?.Invoke(string.Join("\n", _moveHistory));
            MoveMade?.Invoke(moveNotation);

            // Проверяем условия окончания игры
            CheckGameEndConditions();

            if (_isGameActive)
            {
                // Переключаем игрока
                CurrentPlayer = CurrentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;
                OnPropertyChanged(nameof(CurrentPlayer));
            }

            return true;
        }

        // ========== ХОД ИИ ЧЕРЕЗ API ==========
        public async Task MakeAIMoveViaApiAsync()
        {
            if (!_isGameActive || _currentGameId == 0 || _apiService == null)
            {
                Console.WriteLine("❌ Невозможно сделать ход ИИ");
                return;
            }

            try
            {
                _isAITurn = true;
                AIMoveStarted?.Invoke("Stockfish думает...");

                // Получаем текущую FEN позицию
                string fen = GetFenFromBoard();

                // Запрашиваем ход у ИИ
                string aiMove = await _apiService.GetAIMoveAsync(fen, _gameDifficulty);

                if (!string.IsNullOrEmpty(aiMove) && aiMove.Length >= 4)
                {
                    string from = aiMove.Substring(0, 2);
                    string to = aiMove.Substring(2, 2);

                    var fromPos = SquareToPosition(from);
                    var toPos = SquareToPosition(to);

                    if (fromPos.IsValid() && toPos.IsValid())
                    {
                        // Делаем ход ИИ
                        Board.MovePiece(fromPos, toPos);
                        Board.UpdateSquaresFromCells();

                        string aiNotation = $"{GetSquareNotation(fromPos)}{GetSquareNotation(toPos)}";
                        _moveHistory.Add($"{_moveHistory.Count + 1}. {aiNotation}");
                        UpdateHistoryCallback?.Invoke(string.Join("\n", _moveHistory));

                        AIMoveCompleted?.Invoke(aiMove);
                        MoveMade?.Invoke(aiNotation);

                        // Переключаем игрока
                        CurrentPlayer = CurrentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;
                        OnPropertyChanged(nameof(CurrentPlayer));

                        // Проверяем условия окончания игры
                        CheckGameEndConditions();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при ходе ИИ: {ex.Message}");
            }
            finally
            {
                _isAITurn = false;
            }
        }

        // ========== ОБЫЧНЫЙ ХОД (ДЛЯ ИГРЫ ЧЕЛОВЕК VS ЧЕЛОВЕК) ==========
        public async Task<bool> MakeMove(Position from, Position to)
        {
            if (!_isGameActive)
                return false;

            var piece = Board.GetPieceAt(from);
            if (piece == null || piece.Color != CurrentPlayer)
                return false;

            if (!Board.IsValidMove(from, to, CurrentPlayer))
                return false;

            return MakeMoveLocal(from, to);
        }

        // ========== ПРОВЕРКА ОКОНЧАНИЯ ИГРЫ ==========
        private void CheckGameEndConditions()
        {
            if (IsCheckmate(PieceColor.White))
            {
                EndGame("Победа черных! Мат белому королю.");
                return;
            }

            if (IsCheckmate(PieceColor.Black))
            {
                EndGame("Победа белых! Мат черному королю.");
                return;
            }

            if (IsStalemate(CurrentPlayer))
            {
                EndGame("Ничья! Пат.");
                return;
            }

            if (IsInsufficientMaterial())
            {
                EndGame("Ничья! Недостаточно материала для мата.");
                return;
            }

            if (_moveHistory.Count >= 100)
            {
                EndGame("Ничья по правилу 50 ходов.");
                return;
            }
        }

        // ========== СДАЧА ==========
        public async Task ResignAsync(PieceColor resigningColor)
        {
            if (!_isGameActive) return;

            Console.WriteLine($"=== RESIGN CALLED ===");
            Console.WriteLine($"Resigning color: {resigningColor}");
            Console.WriteLine($"User is white: {UserIsWhite}");

            string result = "";
            string apiResult = "";

            if (UserIsWhite)
            {
                if (resigningColor == PieceColor.White)
                {
                    result = "Черные победили (белые сдались)";
                    apiResult = "Black";
                }
                else
                {
                    result = "Белые победили (черные сдались)";
                    apiResult = "White";
                }
            }
            else
            {
                if (resigningColor == PieceColor.White)
                {
                    result = "Черные победили (белые сдались)";
                    apiResult = "Black";
                }
                else
                {
                    result = "Белые победили (черные сдались)";
                    apiResult = "White";
                }
            }

            // Завершаем игру на сервере
            if (_currentGameId > 0 && _apiService != null)
            {
                try
                {
                    await _apiService.FinishGameAsync(_currentGameId, apiResult);
                    Console.WriteLine($"✅ Игра #{_currentGameId} завершена на сервере");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка завершения игры на сервере: {ex.Message}");
                }
            }

            EndGame(result);
        }

        // ========== ПРЕДЛОЖЕНИЕ НИЧЬЕЙ ==========
        public void OfferDraw()
        {
            if (!_isGameActive)
                return;

            EndGame("Ничья по соглашению игроков.");
        }

        // ========== ЗАВЕРШЕНИЕ ИГРЫ ==========
        private void EndGame(string result)
        {
            _isGameActive = false;
            _gameResult = result;
            _isAITurn = false;

            Console.WriteLine($"Game ended: {result}");
            GameFinished?.Invoke(result);

            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(result, "Игра окончена",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        // ========== ПОЛУЧИТЬ ВОЗМОЖНЫЕ ХОДЫ ==========
        public List<Move> GetPossibleMoves(string position)
        {
            var moves = new List<Move>();
            try
            {
                var pos = SquareToPosition(position);
                if (!pos.IsValid()) return moves;

                var piece = Board.GetPieceAt(pos);
                if (piece == null || piece.Color != CurrentPlayer) return moves;

                var possiblePositions = piece.GetPossibleMoves(pos, Board);

                foreach (var targetPos in possiblePositions)
                {
                    moves.Add(new Move
                    {
                        OriginalPosition = new Position(pos.Row, pos.Column),
                        NewPosition = new Position(targetPos.Row, targetPos.Column)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении возможных ходов: {ex.Message}");
            }
            return moves;
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        private string GetPieceSymbol(ChessPiece piece)
        {
            return piece.Type switch
            {
                PieceType.King => "K",
                PieceType.Queen => "Q",
                PieceType.Rook => "R",
                PieceType.Bishop => "B",
                PieceType.Knight => "N",
                _ => ""
            };
        }

        private string GetSquareNotation(Position position)
        {
            char file = (char)('a' + position.Column);
            int rank = 8 - position.Row;
            return $"{file}{rank}";
        }

        private Position SquareToPosition(string square)
        {
            if (string.IsNullOrEmpty(square) || square.Length < 2)
                return Position.Invalid;

            char file = square[0];
            char rank = square[1];

            int col = file - 'a';
            int row = 8 - (rank - '0');

            if (row < 0 || row > 7 || col < 0 || col > 7)
                return Position.Invalid;

            return new Position(row, col);
        }

        private string GetFenFromBoard()
        {
            StringBuilder fen = new StringBuilder();

            for (int row = 0; row < 8; row++)
            {
                int emptyCount = 0;
                for (int col = 0; col < 8; col++)
                {
                    var piece = Board.Cells[row, col].Piece;
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

            fen.Append(CurrentPlayer == PieceColor.White ? " w " : " b ");
            fen.Append("KQkq - 0 1");

            return fen.ToString();
        }

        private string GetResultMessage(string apiResult)
        {
            return apiResult switch
            {
                "White" => UserIsWhite ? "Победа белых! Поздравляем!" : "Победа белых",
                "Black" => !UserIsWhite ? "Победа черных! Поздравляем!" : "Победа черных",
                "Draw" => "Ничья!",
                _ => "Игра завершена"
            };
        }

        // ========== МЕТОДЫ ПРОВЕРКИ ШАХМАТНЫХ УСЛОВИЙ ==========

        private bool IsCheckmate(PieceColor color)
        {
            var kingPosition = FindKing(color);
            if (!kingPosition.IsValid())
                return false;

            if (!IsInCheck(color, kingPosition))
                return false;

            return !HasAnyLegalMove(color);
        }

        private bool IsStalemate(PieceColor color)
        {
            var kingPosition = FindKing(color);
            if (IsInCheck(color, kingPosition))
                return false;

            return !HasAnyLegalMove(color);
        }

        private bool IsInCheck(PieceColor color, Position kingPosition)
        {
            var opponentColor = color == PieceColor.White ? PieceColor.Black : PieceColor.White;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var piece = Board.GetPieceAt(new Position(row, col));
                    if (piece != null && piece.Color == opponentColor)
                    {
                        var moves = piece.GetPossibleMoves(new Position(row, col), Board);
                        if (moves.Contains(kingPosition))
                            return true;
                    }
                }
            }

            return false;
        }

        private bool HasAnyLegalMove(PieceColor color)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var position = new Position(row, col);
                    var piece = Board.GetPieceAt(position);

                    if (piece != null && piece.Color == color)
                    {
                        var moves = piece.GetPossibleMoves(position, Board);

                        foreach (var move in moves)
                        {
                            var capturedPiece = Board.GetPieceAt(move);
                            Board.MovePiece(position, move);

                            var kingPosition = FindKing(color);
                            bool stillInCheck = IsInCheck(color, kingPosition);

                            Board.MovePiece(move, position);
                            if (capturedPiece != null)
                            {
                                Board.Cells[move.Row, move.Column].Piece = capturedPiece;
                                Board.UpdateSquaresFromCells();
                            }

                            if (!stillInCheck)
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        private Position FindKing(PieceColor color)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var piece = Board.GetPieceAt(new Position(row, col));
                    if (piece != null &&
                        piece.Color == color &&
                        piece.Type == PieceType.King)
                    {
                        return new Position(row, col);
                    }
                }
            }

            return Position.Invalid;
        }

        private bool IsInsufficientMaterial()
        {
            int whitePieces = 0;
            int blackPieces = 0;
            bool whiteHasMinor = false;
            bool blackHasMinor = false;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var piece = Board.GetPieceAt(new Position(row, col));
                    if (piece != null && piece.Type != PieceType.King)
                    {
                        if (piece.Color == PieceColor.White)
                        {
                            whitePieces++;
                            if (piece.Type == PieceType.Bishop || piece.Type == PieceType.Knight)
                                whiteHasMinor = true;
                        }
                        else
                        {
                            blackPieces++;
                            if (piece.Type == PieceType.Bishop || piece.Type == PieceType.Knight)
                                blackHasMinor = true;
                        }
                    }
                }
            }

            if (whitePieces == 0 && blackPieces == 0)
                return true;

            if ((whitePieces == 1 && whiteHasMinor && blackPieces == 0) ||
                (blackPieces == 1 && blackHasMinor && whitePieces == 0))
                return true;

            return false;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Класс Move для хранения информации о ходе
    public class Move
    {
        public Position OriginalPosition { get; set; }
        public Position NewPosition { get; set; }
    }
}