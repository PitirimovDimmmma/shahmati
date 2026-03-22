using shahmati.models;
using shahmati.Models;
using shahmati.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        private bool _isGameStarting = false;

        // ========== СВОЙСТВА ==========
        public bool UserIsWhite { get; set; } = true;
        public string GameMode => _gameMode;
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
            if (_isGameStarting) return;
            _isGameStarting = true;

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

                if (gameMode == "Человек vs Компьютер" && !userIsWhite)
                {
                    Console.WriteLine("ИИ ходит первым (пользователь играет черными)");
                    await Task.Delay(500);
                    await MakeAIMoveViaApiAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при начале игры: {ex.Message}");
            }
            finally
            {
                _isGameStarting = false;
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

                if (_currentGameId > 0)
                {
                    Console.WriteLine($"⚠️ Уже есть активная игра с ID: {_currentGameId}");
                    return _currentGameId;
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

        private void ForceUpdateAllCells()
        {
            if (Board == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Обновляем каждую клетку индивидуально
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        var cell = Board.Cells[row, col];
                        cell.OnPropertyChanged(nameof(BoardCell.Piece));
                        cell.OnPropertyChanged(nameof(BoardCell.PieceImagePath));
                        cell.OnPropertyChanged(nameof(BoardCell.HasPiece));
                    }
                }

                // Обновляем Board
                OnPropertyChanged(nameof(Board));
                OnPropertyChanged(nameof(Board.CellsFlat));

                Console.WriteLine("✅ Принудительное обновление всех клеток выполнено");
            });
        }

        public async Task<bool> MakeMoveVsAIAsync(Position from, Position to, int userId)
        {
            try
            {
                Console.WriteLine($"=== MakeMoveVsAIAsync CALLED ===");
                Console.WriteLine($"From: {GetSquareNotation(from)} To: {GetSquareNotation(to)}");
                Console.WriteLine($"IsGameActive: {_isGameActive}, IsAITurn: {_isAITurn}");
                Console.WriteLine($"CurrentPlayer: {CurrentPlayer}, UserIsWhite: {UserIsWhite}");

                if (!_isGameActive)
                {
                    Console.WriteLine("❌ Игра не активна");
                    return false;
                }

                if (_isAITurn)
                {
                    Console.WriteLine("❌ Сейчас ход ИИ, нельзя ходить");
                    return false;
                }

                // ПРОВЕРЯЕМ ТОЛЬКО ЧТО ЕСТЬ ФИГУРА
                var piece = Board.GetPieceAt(from);
                if (piece == null)
                {
                    Console.WriteLine("❌ Нет фигуры в исходной позиции");
                    return false;
                }

                if (piece.Color != CurrentPlayer)
                {
                    Console.WriteLine($"❌ Не ваша очередь. Текущий игрок: {CurrentPlayer}, цвет фигуры: {piece.Color}");
                    return false;
                }

                string moveNotation = GetSquareNotation(from).ToUpper() + GetSquareNotation(to).ToUpper();
                Console.WriteLine($"Отправляем ход на сервер: {moveNotation}");

                // НЕ ПРОВЕРЯЕМ ХОД ЛОКАЛЬНО, СРАЗУ ОТПРАВЛЯЕМ НА СЕРВЕР
                if (_currentGameId > 0 && _apiService != null && _gameMode == "Человек vs Компьютер")
                {
                    return await MakeMoveViaApiAsync(moveNotation);
                }
                else
                {
                    Console.WriteLine("Локальный режим (без сервера)");
                    return MakeMoveLocal(from, to);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка в MakeMoveVsAIAsync: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                _isAITurn = false;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AIMoveCompleted?.Invoke("");
                });
                return false;
            }
        }

        private async Task<bool> MakeMoveViaApiAsync(string moveNotation)
        {
            try
            {
                Console.WriteLine($"=== MakeMoveViaApiAsync START ===");
                Console.WriteLine($"GameId: {_currentGameId}, Move: {moveNotation}");

                _isAITurn = true;
                AIMoveStarted?.Invoke("Stockfish думает...");

                if (_currentGameId == 0)
                {
                    Console.WriteLine("⚠️ Игра не создана на сервере");
                    _isAITurn = false;
                    AIMoveCompleted?.Invoke("");
                    return false;
                }

                var response = await _apiService.PlayAgainstAIAsync(_currentGameId, moveNotation);
                Console.WriteLine($"Ответ от API: Success={response?.Success}, AIMove={response?.AIMove}");
                Console.WriteLine($"FenAfterUserMove: {response?.FenAfterUserMove}");
                Console.WriteLine($"FenAfterAIMove: {response?.FenAfterAIMove}");

                if (response?.Success == true)
                {
                    // ВАЖНО: Обновляем доску из FEN, который вернул API
                    // Это гарантирует, что доска будет в правильном состоянии

                    // 1. Обновляем доску после хода пользователя (используем FEN от API)
                    if (!string.IsNullOrEmpty(response.FenAfterUserMove))
                    {
                        Console.WriteLine($"Обновляем доску из FEN после хода пользователя");
                        LoadBoardFromFen(response.FenAfterUserMove);
                    }

                    // Добавляем ход пользователя в историю
                    string userFrom = moveNotation.Substring(0, 2);
                    string userTo = moveNotation.Substring(2, 2);
                    string userNotation = $"{userFrom}{userTo}";
                    _moveHistory.Add($"{_moveHistory.Count + 1}. {userNotation}");

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        UpdateHistoryCallback?.Invoke(string.Join("\n", _moveHistory));
                        MoveMade?.Invoke(userNotation);
                        ForceUpdateAllCells();
                    });

                    // 2. Обновляем доску после хода ИИ (используем FEN от API)
                    if (!string.IsNullOrEmpty(response.FenAfterAIMove))
                    {
                        Console.WriteLine($"Обновляем доску из FEN после хода ИИ: {response.FenAfterAIMove}");
                        LoadBoardFromFen(response.FenAfterAIMove);

                        string aiNotation = response.AIMove;
                        _moveHistory.Add($"{_moveHistory.Count + 1}. {aiNotation}");

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            UpdateHistoryCallback?.Invoke(string.Join("\n", _moveHistory));
                            MoveMade?.Invoke(aiNotation);
                            ForceUpdateAllCells();
                            AIMoveCompleted?.Invoke(aiNotation);
                        });
                    }

                    // 3. Переключаем ход на белых
                    CurrentPlayer = PieceColor.White;

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        OnPropertyChanged(nameof(CurrentPlayer));
                        OnPropertyChanged(nameof(CurrentPlayerDisplay));
                        ForceUpdateAllCells();
                    });

                    Console.WriteLine($"Текущий игрок: {CurrentPlayer}");

                    _isAITurn = false;
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка API");
                    _isAITurn = false;
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        AIMoveCompleted?.Invoke("");
                    });
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                _isAITurn = false;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AIMoveCompleted?.Invoke("");
                });
                return false;
            }
        }

        private void LoadBoardFromFen(string fen)
        {
            try
            {
                Console.WriteLine($"Загрузка доски из FEN: {fen}");

                // Разбираем FEN строку
                string[] parts = fen.Split(' ');
                string boardPart = parts[0];
                string[] rows = boardPart.Split('/');

                // Очищаем текущую доску
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        Board.Cells[row, col].Piece = null;
                    }
                }

                // Заполняем доску из FEN
                for (int row = 0; row < 8; row++)
                {
                    string rowStr = rows[row];
                    int col = 0;

                    for (int i = 0; i < rowStr.Length; i++)
                    {
                        char c = rowStr[i];

                        if (char.IsDigit(c))
                        {
                            // Пропускаем пустые клетки
                            int emptyCount = int.Parse(c.ToString());
                            col += emptyCount;
                        }
                        else
                        {
                            // Определяем фигуру
                            PieceColor color = char.IsUpper(c) ? PieceColor.White : PieceColor.Black;
                            PieceType type = char.ToLower(c) switch
                            {
                                'k' => PieceType.King,
                                'q' => PieceType.Queen,
                                'r' => PieceType.Rook,
                                'b' => PieceType.Bishop,
                                'n' => PieceType.Knight,
                                'p' => PieceType.Pawn,
                                _ => PieceType.Pawn
                            };

                            // Создаем фигуру
                            ChessPiece piece = type switch
                            {
                                PieceType.King => new King(color),
                                PieceType.Queen => new Queen(color),
                                PieceType.Rook => new Rook(color),
                                PieceType.Bishop => new Bishop(color),
                                PieceType.Knight => new Knight(color),
                                _ => new Pawn(color)
                            };

                            Board.Cells[row, col].Piece = piece;
                            col++;
                        }
                    }
                }

                Board.UpdateSquaresFromCells();
                Board.ForceUpdate();

                Console.WriteLine("✅ Доска загружена из FEN");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки FEN: {ex.Message}");
            }
        }

        public async Task MakeAIMoveViaApiAsync()
        {
            try
            {
                if (!_isGameActive)
                {
                    Console.WriteLine("❌ Игра не активна");
                    AIMoveCompleted?.Invoke(""); // Выключаем индикатор
                    return;
                }

                if (_currentGameId == 0)
                {
                    Console.WriteLine("❌ Игра не создана на сервере");
                    AIMoveCompleted?.Invoke(""); // Выключаем индикатор
                    return;
                }

                if (_apiService == null)
                {
                    Console.WriteLine("❌ API Service не инициализирован");
                    AIMoveCompleted?.Invoke(""); // Выключаем индикатор
                    return;
                }

                _isAITurn = true;
                AIMoveStarted?.Invoke("Stockfish думает...");

                Console.WriteLine($"=== ХОД ИИ ЧЕРЕЗ API ===");
                Console.WriteLine($"GameId: {_currentGameId}");

                string fen = GetFenFromBoard();
                Console.WriteLine($"Текущая FEN: {fen}");

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
                        Board.MovePiece(fromPos, toPos);
                        Board.ForceUpdate();

                        string aiNotation = $"{GetSquareNotation(fromPos)}{GetSquareNotation(toPos)}";
                        _moveHistory.Add($"{_moveHistory.Count + 1}. {aiNotation}");

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            UpdateHistoryCallback?.Invoke(string.Join("\n", _moveHistory));
                            MoveMade?.Invoke(aiNotation);
                            ForceUpdateAllCells();
                            // ВАЖНО: Вызываем AIMoveCompleted ПОСЛЕ обновления доски
                            AIMoveCompleted?.Invoke(aiMove);
                        });

                        CurrentPlayer = UserIsWhite ? PieceColor.White : PieceColor.Black;
                        OnPropertyChanged(nameof(CurrentPlayer));
                        OnPropertyChanged(nameof(CurrentPlayerDisplay));

                        CheckGameEndConditions();

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
                        // Если ход неверный, все равно выключаем индикатор
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            AIMoveCompleted?.Invoke("");
                        });
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось получить ход от ИИ");
                    // Если нет хода, выключаем индикатор
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        AIMoveCompleted?.Invoke("");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при ходе ИИ: {ex.Message}");
                // При ошибке выключаем индикатор
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AIMoveCompleted?.Invoke("");
                });
            }
            finally
            {
                _isAITurn = false;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ForceUpdateAllCells();
                });
            }
        }

        private bool MakeMoveLocal(Position from, Position to)
        {
            try
            {
                Console.WriteLine($"=== MakeMoveLocal ===");
                Console.WriteLine($"From: {GetSquareNotation(from)} To: {GetSquareNotation(to)}");

                var piece = Board.GetPieceAt(from);
                if (piece == null)
                {
                    Console.WriteLine("❌ Нет фигуры");
                    return false;
                }

                Console.WriteLine($"Фигура: {piece.Type} {piece.Color}");

                // Делаем ход
                Board.MovePiece(from, to);
                Board.ForceUpdate();

                string moveNotation = GetSquareNotation(from).ToUpper() + GetSquareNotation(to).ToUpper();
                _moveHistory.Add($"{_moveHistory.Count + 1}. {moveNotation}");

                Console.WriteLine($"Ход добавлен в историю: {moveNotation}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateHistoryCallback?.Invoke(string.Join("\n", _moveHistory));
                    MoveMade?.Invoke(moveNotation);
                    ForceUpdateAllCells();
                });

                CheckGameEndConditions();

                if (_isGameActive)
                {
                    CurrentPlayer = CurrentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;
                    Console.WriteLine($"Смена игрока: {CurrentPlayer}");
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

        // ========== ОБЫЧНЫЙ ХОД ==========
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

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

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
                                Board.ForceUpdate();
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

    public class Move
    {
        public Position OriginalPosition { get; set; }
        public Position NewPosition { get; set; }
    }
}