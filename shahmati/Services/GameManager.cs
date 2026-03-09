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
                OnPropertyChanged(nameof(CurrentPlayerDisplay));
            }
        }

        public string CurrentPlayerDisplay
        {
            get
            {
                if (UserIsWhite)
                {
                    return _currentPlayer == PieceColor.White ? "Ваш ход (Белые)" : "Ход ИИ (Черные)";
                }
                else
                {
                    return _currentPlayer == PieceColor.White ? "Ход ИИ (Белые)" : "Ваш ход (Черные)";
                }
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
            try
            {
                Console.WriteLine($"=== НАЧАЛО НОВОЙ ИГРЫ ===");
                Console.WriteLine($"GameMode: {gameMode}");
                Console.WriteLine($"Difficulty: {difficulty}");
                Console.WriteLine($"UserIsWhite: {userIsWhite}");

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
                OnPropertyChanged(nameof(CurrentPlayerDisplay));

                UpdateHistoryCallback?.Invoke("Новая игра начата!");

                // Если игра с ИИ и пользователь черными - ИИ ходит первым
                if (gameMode == "Человек vs Компьютер" && !userIsWhite)
                {
                    Console.WriteLine("ИИ ходит первым (пользователь играет черными)");
                    await Task.Delay(500); // Небольшая задержка для наглядности
                    await MakeAIMoveViaApiAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при начале игры: {ex.Message}");
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

                Console.WriteLine($"=== СОЗДАНИЕ ИГРЫ НА СЕРВЕРЕ ===");
                Console.WriteLine($"UserId: {userId}, Difficulty: {difficulty}, Color: {color}");

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
            try
            {
                if (!_isGameActive || _isAITurn)
                {
                    Console.WriteLine($"Ход невозможен: _isGameActive={_isGameActive}, _isAITurn={_isAITurn}");
                    return false;
                }

                var piece = Board.GetPieceAt(from);
                if (piece == null)
                {
                    Console.WriteLine("Нет фигуры в исходной позиции");
                    return false;
                }

                if (piece.Color != CurrentPlayer)
                {
                    Console.WriteLine($"Не ваша очередь. Текущий игрок: {CurrentPlayer}, цвет фигуры: {piece.Color}");
                    return false;
                }

                // Проверяем валидность хода локально
                if (!Board.IsValidMove(from, to, CurrentPlayer))
                {
                    Console.WriteLine("Ход невалиден");
                    return false;
                }

                string moveNotation = GetSquareNotation(from).ToUpper() + GetSquareNotation(to).ToUpper();
                Console.WriteLine($"Ход пользователя: {moveNotation}");

                // Если есть активная игра на сервере - отправляем ход
                if (_currentGameId > 0 && _apiService != null && _gameMode == "Человек vs Компьютер")
                {
                    return await MakeMoveViaApiAsync(moveNotation);
                }
                else
                {
                    Console.WriteLine("Локальный ход (без сервера)");
                    // Локальный ход (без сервера)
                    return MakeMoveLocal(from, to);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка в MakeMoveVsAIAsync: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> MakeMoveViaApiAsync(string moveNotation)
        {
            try
            {
                _isAITurn = true;
                AIMoveStarted?.Invoke("Stockfish думает...");

                Console.WriteLine($"=== ОТПРАВКА ХОДА НА СЕРВЕР ===");
                Console.WriteLine($"GameId: {_currentGameId}, Move: {moveNotation}");

                if (_currentGameId == 0)
                {
                    Console.WriteLine("⚠️ Игра не создана на сервере");
                    _isAITurn = false;
                    return false;
                }

                // Используем существующий ApiService
                Console.WriteLine("Отправляем запрос в API...");
                var response = await _apiService.PlayAgainstAIAsync(_currentGameId, moveNotation);

                Console.WriteLine($"Ответ от API получен");

                if (response?.Success == true)
                {
                    Console.WriteLine($"✅ Ход успешно обработан API");
                    Console.WriteLine($"AIMove: {response.AIMove}");
                    Console.WriteLine($"FenAfterUserMove: {response.FenAfterUserMove}");
                    Console.WriteLine($"FenAfterAIMove: {response.FenAfterAIMove}");

                    // Применяем ход пользователя
                    string userFrom = moveNotation.Substring(0, 2);
                    string userTo = moveNotation.Substring(2, 2);

                    Console.WriteLine($"Применяем ход пользователя: {userFrom} -> {userTo}");

                    var fromPos = Position.FromString(userFrom);
                    var toPos = Position.FromString(userTo);

                    // Делаем ход пользователя
                    Board.MovePiece(fromPos, toPos);
                    Board.UpdateSquaresFromCells();

                    Console.WriteLine($"Ход пользователя применен к доске");

                    string userNotation = $"{GetSquareNotation(fromPos)}{GetSquareNotation(toPos)}";
                    _moveHistory.Add($"{_moveHistory.Count + 1}. {userNotation}");

                    // Обновляем UI в главном потоке
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdateHistoryCallback?.Invoke(string.Join("\n", _moveHistory));
                        MoveMade?.Invoke(userNotation);
                        OnPropertyChanged(nameof(Board));
                    });

                    // Если есть ход ИИ, применяем его
                    if (!string.IsNullOrEmpty(response.AIMove) && response.AIMove.Length >= 4)
                    {
                        string aiFrom = response.AIMove.Substring(0, 2);
                        string aiTo = response.AIMove.Substring(2, 2);

                        Console.WriteLine($"Применяем ход ИИ: {aiFrom} -> {aiTo}");

                        var aiFromPos = Position.FromString(aiFrom);
                        var aiToPos = Position.FromString(aiTo);

                        if (aiFromPos.IsValid() && aiToPos.IsValid())
                        {
                            // Делаем ход ИИ
                            Board.MovePiece(aiFromPos, aiToPos);
                            Board.UpdateSquaresFromCells();

                            Console.WriteLine($"Ход ИИ применен к доске");

                            string aiNotation = $"{GetSquareNotation(aiFromPos)}{GetSquareNotation(aiToPos)}";
                            _moveHistory.Add($"{_moveHistory.Count + 1}. {aiNotation}");

                            // Обновляем UI в главном потоке
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                UpdateHistoryCallback?.Invoke(string.Join("\n", _moveHistory));
                                AIMoveCompleted?.Invoke(response.AIMove);
                                MoveMade?.Invoke(aiNotation);
                                OnPropertyChanged(nameof(Board));
                            });
                        }
                        else
                        {
                            Console.WriteLine($"❌ Неверная позиция хода ИИ: {aiFrom}->{aiTo}");
                        }
                    }

                    // Обновляем текущего игрока
                    CurrentPlayer = CurrentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OnPropertyChanged(nameof(CurrentPlayer));
                        OnPropertyChanged(nameof(CurrentPlayerDisplay));
                        OnPropertyChanged(nameof(Board));
                    });

                    Console.WriteLine($"Текущий игрок после хода: {CurrentPlayer}");

                    _isAITurn = false;

                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка API: {response?.Message ?? "Unknown error"}");
                    _isAITurn = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка API хода: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                _isAITurn = false;
                return false;
            }
            // После всех обновлений, принудительно обновляем UI
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(Board));
                // Дополнительно вызываем событие для CellsFlat
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Board.CellsFlat"));
            });
        }
        // ========== ХОД ИИ ЧЕРЕЗ API ==========
        public async Task MakeAIMoveViaApiAsync()
        {
            try
            {
                if (!_isGameActive)
                {
                    Console.WriteLine("❌ Игра не активна");
                    return;
                }

                if (_currentGameId == 0)
                {
                    Console.WriteLine("❌ Игра не создана на сервере");
                    return;
                }

                if (_apiService == null)
                {
                    Console.WriteLine("❌ API Service не инициализирован");
                    return;
                }

                _isAITurn = true;
                AIMoveStarted?.Invoke("Stockfish думает...");

                Console.WriteLine($"=== ХОД ИИ ЧЕРЕЗ API ===");
                Console.WriteLine($"GameId: {_currentGameId}");

                // Получаем текущую FEN позицию
                string fen = GetFenFromBoard();
                Console.WriteLine($"Текущая FEN: {fen}");

                // Запрашиваем ход у ИИ
                string aiMove = await _apiService.GetAIMoveAsync(fen, _gameDifficulty);

                if (!string.IsNullOrEmpty(aiMove) && aiMove.Length >= 4)
                {
                    Console.WriteLine($"ИИ выбрал ход: {aiMove}");

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

                        // Обновляем UI после хода ИИ
                        OnPropertyChanged(nameof(Board));

                        // Переключаем игрока
                        CurrentPlayer = CurrentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;
                        OnPropertyChanged(nameof(CurrentPlayer));
                        OnPropertyChanged(nameof(CurrentPlayerDisplay));

                        // Проверяем условия окончания игры
                        CheckGameEndConditions();

                        // Если игра закончилась, завершаем её на сервере
                        if (!_isGameActive && _currentGameId > 0)
                        {
                            string result = _gameResult switch
                            {
                                "Победа белых! Мат черному королю." => "White",
                                "Победа черных! Мат белому королю." => "Black",
                                _ => "Draw"
                            };
                            await _apiService.FinishGameAsync(_currentGameId, result);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Неверная позиция хода ИИ: {from}->{to}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось получить ход от ИИ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при ходе ИИ: {ex.Message}");
            }
            finally
            {
                _isAITurn = false;
                // Финальное обновление UI
                OnPropertyChanged(nameof(Board));
            }
        }

        // ========== ЛОКАЛЬНЫЙ ХОД ==========
        private bool MakeMoveLocal(Position from, Position to)
        {
            try
            {
                var capturedPiece = Board.GetPieceAt(to);

                // Делаем ход
                Board.MovePiece(from, to);
                Board.UpdateSquaresFromCells();

                string moveNotation = GetSquareNotation(from).ToUpper() + GetSquareNotation(to).ToUpper();
                _moveHistory.Add($"{_moveHistory.Count + 1}. {moveNotation}");
                UpdateHistoryCallback?.Invoke(string.Join("\n", _moveHistory));
                MoveMade?.Invoke(moveNotation);

                // Обновляем UI
                OnPropertyChanged(nameof(Board));

                // Проверяем условия окончания игры
                CheckGameEndConditions();

                if (_isGameActive)
                {
                    // Переключаем игрока
                    CurrentPlayer = CurrentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;
                    OnPropertyChanged(nameof(CurrentPlayer));
                    OnPropertyChanged(nameof(CurrentPlayerDisplay));
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка локального хода: {ex.Message}");
                return false;
            }
        }

        // ========== ОБЫЧНЫЙ ХОД (ДЛЯ ИГРЫ ЧЕЛОВЕК VS ЧЕЛОВЕК) ==========
        public async Task<bool> MakeMove(Position from, Position to)
        {
            try
            {
                if (!_isGameActive)
                {
                    Console.WriteLine("Игра не активна");
                    return false;
                }

                var piece = Board.GetPieceAt(from);
                if (piece == null || piece.Color != CurrentPlayer)
                {
                    Console.WriteLine("Не ваша фигура или не ваша очередь");
                    return false;
                }

                if (!Board.IsValidMove(from, to, CurrentPlayer))
                {
                    Console.WriteLine("Ход невалиден");
                    return false;
                }

                return MakeMoveLocal(from, to);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка в MakeMove: {ex.Message}");
                return false;
            }
        }

        // ========== ПРОВЕРКА ОКОНЧАНИЯ ИГРЫ ==========
        private void CheckGameEndConditions()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке окончания игры: {ex.Message}");
            }
        }

        // ========== СДАЧА ==========
        public async Task ResignAsync(PieceColor resigningColor)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при сдаче: {ex.Message}");
            }
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

                            // Отменяем ход
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