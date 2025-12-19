using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using shahmati.models;

namespace shahmati.Services
{
    public class GameManager : INotifyPropertyChanged
    {
        // Добавьте свойство
        public bool UserIsWhite { get; set; } = true;

        // Обновите метод Resign
        public void Resign(PieceColor resigningColor)
        {
            if (!IsGameInProgress) return;

            Console.WriteLine($"=== RESIGN CALLED ===");
            Console.WriteLine($"Resigning color: {resigningColor}");
            Console.WriteLine($"User is white: {UserIsWhite}");

            string result = "";
            if (UserIsWhite)
            {
                // Пользователь = белые
                if (resigningColor == PieceColor.White)
                {
                    result = "Черные победили (белые сдались)";
                    Console.WriteLine($"User (White) resigns, Black wins");
                }
                else
                {
                    result = "Белые победили (черные сдались)";
                    Console.WriteLine($"Opponent (Black) resigns, White wins");
                }
            }
            else
            {
                // Пользователь = черные (на всякий случай)
                if (resigningColor == PieceColor.White)
                    result = "Черные победили (белые сдались)";
                else
                    result = "Белые победили (черные сдались)";
            }

            Console.WriteLine($"Ending game with result: {result}");
            EndGame(result);
        }

        private Board _board;
        private PieceColor _currentPlayer;
        private bool _isGameActive;
        private string _gameResult;
        private List<string> _moveHistory;
        private ChessAIService _aiService;
        private string _gameMode;
        private string _difficulty;

        // События для уведомления о конце игры
        public event Action<string> GameFinished; // параметр - результат игры
        public event Action<string> MoveMade; // параметр - нотация хода

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

        // Добавьте это свойство для связи с MainWindow
        public Action<string> UpdateHistoryCallback { get; set; }

        public GameManager()
        {
            _board = new Board();
            _currentPlayer = PieceColor.White;
            _isGameActive = true;
            _moveHistory = new List<string>();
            _aiService = new ChessAIService();
        }

        public void StartNewGame(string gameMode = "Человек vs Человек", string difficulty = "Средний")
        {
            _board = new Board();
            _currentPlayer = PieceColor.White;
            _isGameActive = true;
            _gameResult = null;
            _moveHistory.Clear();
            _gameMode = gameMode;
            _difficulty = difficulty;

            OnPropertyChanged(nameof(Board));
            OnPropertyChanged(nameof(CurrentPlayer));

            // Уведомляем MainWindow о начале игры
            UpdateHistoryCallback?.Invoke("Новая игра начата!");

            // Проверяем, нужно ли ИИ делать первый ход
            if (gameMode == "Компьютер vs Компьютер" ||
                (gameMode == "Человек vs Компьютер" && _currentPlayer == PieceColor.Black))
            {
                _ = MakeAIMoveAsync();
            }
        }

        public async Task<bool> MakeMove(Position from, Position to)
        {
            if (!_isGameActive || !from.IsValid() || !to.IsValid())
                return false;

            var piece = Board.GetPieceAt(from);
            if (piece == null || piece.Color != CurrentPlayer)
                return false;

            // Проверяем валидность хода
            if (!Board.IsValidMove(from, to, CurrentPlayer))
                return false;

            // Сохраняем информацию о взятии
            var capturedPiece = Board.GetPieceAt(to);
            string capturedNotation = capturedPiece != null ? $"x{GetSquareNotation(to)}" : "";

            // Делаем ход
            Board.MovePiece(from, to);

            // Записываем ход в историю
            string pieceSymbol = GetPieceSymbol(piece);
            string moveNotation = $"{pieceSymbol}{GetSquareNotation(from)}{capturedNotation}{GetSquareNotation(to)}";

            if (capturedPiece != null)
            {
                moveNotation = $"{pieceSymbol}{GetSquareNotation(from)}x{GetSquareNotation(to)}";
            }

            _moveHistory.Add($"{_moveHistory.Count + 1}. {moveNotation}");

            // Обновляем историю в UI
            UpdateHistoryCallback?.Invoke(string.Join("\n", _moveHistory));

            // Уведомляем о сделанном ходе
            MoveMade?.Invoke(moveNotation);

            // Проверяем условия окончания игры
            CheckGameEndConditions();

            if (_isGameActive)
            {
                // Переключаем игрока
                CurrentPlayer = CurrentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;

                // После проверки конца игры и смены игрока
                OnPropertyChanged(nameof(CurrentPlayer));
            }

            return true;
        }

        private void CheckGameEndConditions()
        {
            // 1. Проверяем мат
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

            // 2. Проверяем пат
            if (IsStalemate(CurrentPlayer))
            {
                EndGame("Ничья! Пат.");
                return;
            }

            // 3. Проверяем недостаток материала
            if (IsInsufficientMaterial())
            {
                EndGame("Ничья! Недостаточно материала для мата.");
                return;
            }

            // 4. Правило 50 ходов
            if (_moveHistory.Count >= 100) // 50 ходов каждым игроком
            {
                EndGame("Ничья по правилу 50 ходов.");
                return;
            }
        }

        private bool IsCheckmate(PieceColor color)
        {
            // Находим короля
            var kingPosition = FindKing(color);
            if (!kingPosition.IsValid())
                return false;

            // Проверяем, находится ли король под шахом
            if (!IsInCheck(color, kingPosition))
                return false;

            // Проверяем, есть ли хотя бы один легальный ход
            return !HasAnyLegalMove(color);
        }

        private bool IsStalemate(PieceColor color)
        {
            // Король не под шахом
            var kingPosition = FindKing(color);
            if (IsInCheck(color, kingPosition))
                return false;

            // Нет легальных ходов
            return !HasAnyLegalMove(color);
        }

        private bool IsInCheck(PieceColor color, Position kingPosition)
        {
            var opponentColor = color == PieceColor.White ? PieceColor.Black : PieceColor.White;

            // Проверяем все фигуры противника
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
            // Перебираем все фигуры текущего игрока
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var position = new Position(row, col);
                    var piece = Board.GetPieceAt(position);

                    if (piece != null && piece.Color == color)
                    {
                        var moves = piece.GetPossibleMoves(position, Board);

                        // Проверяем каждый возможный ход
                        foreach (var move in moves)
                        {
                            // Сохраняем оригинальное состояние
                            var capturedPiece = Board.GetPieceAt(move);

                            // Делаем временный ход
                            Board.MovePiece(position, move);

                            // Проверяем, находится ли король под шахом
                            var kingPosition = FindKing(color);
                            bool stillInCheck = IsInCheck(color, kingPosition);

                            // Отменяем ход
                            Board.MovePiece(move, position);

                            // Восстанавливаем взятый фигуру
                            if (capturedPiece != null)
                            {
                                // Восстанавливаем фигуру на место
                                // Поскольку у Board нет метода SetPieceAt, нужно обновить Cells
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

            // Считаем фигуры
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var piece = Board.GetPieceAt(new Position(row, col));
                    if (piece != null)
                    {
                        if (piece.Type != PieceType.King)
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
            }

            // Недостаток материала если:
            // 1. Только короли
            if (whitePieces == 0 && blackPieces == 0)
                return true;

            // 2. Король + слон/конь против короля
            if ((whitePieces == 1 && whiteHasMinor && blackPieces == 0) ||
                (blackPieces == 1 && blackHasMinor && whitePieces == 0))
                return true;

            // 3. Король + слон против короля + слона (одинакового цвета полей)
            // Здесь нужна дополнительная логика

            return false;
        }


        private void EndGame(string result)
        {
            Console.WriteLine($"=== END GAME ===");
            Console.WriteLine($"Result: {result}");

            _isGameActive = false;
            _gameResult = result;

            // Уведомляем о конце игры
            Console.WriteLine($"Invoking GameFinished event...");
            GameFinished?.Invoke(result);

            // Показываем сообщение
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(result, "Игра окончена",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });


        }



        private string GetPieceSymbol(ChessPiece piece)
        {
            return piece.Type switch
            {
                PieceType.King => "K",
                PieceType.Queen => "Q",
                PieceType.Rook => "R",
                PieceType.Bishop => "B",
                PieceType.Knight => "N",
                _ => "" // Для пешки символ не ставится
            };
        }

        private string GetSquareNotation(Position position)
        {
            char file = (char)('a' + position.Column);
            int rank = 8 - position.Row;
            return $"{file}{rank}";
        }

        public async Task MakeAIMoveAsync()
        {
            if (!_isGameActive || CurrentPlayer != PieceColor.Black)
                return;

            try
            {
                // ИИ делает ход
                var aiMove = await _aiService.GetBestMoveAsync(Board, CurrentPlayer, _difficulty);

                if (aiMove.From.IsValid() && aiMove.To.IsValid())
                {
                    await Task.Delay(500); // Небольшая задержка для реалистичности
                    await MakeMove(aiMove.From, aiMove.To);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка ИИ: {ex.Message}");
            }
        }

       

        // Метод для предложения ничьи
        public void OfferDraw()
        {
            if (!_isGameActive)
                return;

            // В реальном приложении здесь должно быть ожидание ответа от противника
            EndGame("Ничья по соглашению игроков.");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Простой класс для ИИ (заглушка)
    public class ChessAIService
    {
        public async Task<(Position From, Position To)> GetBestMoveAsync(Board board, PieceColor color, string difficulty)
        {
            await Task.Delay(100); // Имитация мышления

            // Простой ИИ: выбирает случайный легальный ход
            var random = new Random();
            var pieces = new List<(Position pos, ChessPiece piece)>();

            // Собираем все фигуры нужного цвета
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var pos = new Position(row, col);
                    var piece = board.GetPieceAt(pos);
                    if (piece != null && piece.Color == color)
                    {
                        pieces.Add((pos, piece));
                    }
                }
            }

            // Пытаемся найти легальный ход
            for (int attempt = 0; attempt < 100; attempt++)
            {
                if (pieces.Count == 0)
                    break;

                var randomPiece = pieces[random.Next(pieces.Count)];
                var moves = randomPiece.piece.GetPossibleMoves(randomPiece.pos, board);

                if (moves.Count > 0)
                {
                    var randomMove = moves[random.Next(moves.Count)];
                    return (randomPiece.pos, randomMove);
                }
            }

            return (Position.Invalid, Position.Invalid);
        }
    }
}