using shahmati.models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace shahmati.Services
{
    public class AIPlayer
    {
        private PieceColor _color;
        private int _difficultyLevel;
        private Random _random;
        private Dictionary<PieceType, int> _pieceValues;

        public AIPlayer(PieceColor color, int difficultyLevel = 2)
        {
            _color = color;
            _difficultyLevel = difficultyLevel;
            _random = new Random();

            // Ценности фигур
            _pieceValues = new Dictionary<PieceType, int>
            {
                [PieceType.Pawn] = 10,
                [PieceType.Knight] = 30,
                [PieceType.Bishop] = 30,
                [PieceType.Rook] = 50,
                [PieceType.Queen] = 90,
                [PieceType.King] = 900
            };
        }

        public Move GetBestMove(Board board)
        {
            var possibleMoves = GetAllPossibleMoves(board, _color);
            if (!possibleMoves.Any()) return null;

            var validMoves = possibleMoves.Where(move => IsMoveValid(board, move)).ToList();
            if (!validMoves.Any()) return null;

            return _difficultyLevel switch
            {
                0 => GetRandomMove(validMoves), // Новичок - случайные ходы
                1 => GetGreedyMove(board, validMoves), // Лёгкий - жадный алгоритм
                2 => GetMiniMaxMove(board, validMoves, 2), // Средний - минимакс 2 уровня
                3 => GetMiniMaxMove(board, validMoves, 3), // Сложный - минимакс 3 уровня
                4 => GetAlphaBetaMove(board, validMoves, 4), // Эксперт - альфа-бета 4 уровня
                _ => GetMiniMaxMove(board, validMoves, 2)
            };
        }

        // 1. Случайные ходы (самый простой)
        private Move GetRandomMove(List<Move> moves)
        {
            return moves[_random.Next(moves.Count)];
        }

        // 2. Жадный алгоритм (берет лучший немедленный ход)
        private Move GetGreedyMove(Board board, List<Move> moves)
        {
            var scoredMoves = moves.Select(move => new
            {
                Move = move,
                Score = EvaluateImmediateMove(board, move)
            }).ToList();

            scoredMoves.Sort((a, b) => b.Score.CompareTo(a.Score));
            return scoredMoves.First().Move;
        }

        // 3. Минимакс алгоритм
        private Move GetMiniMaxMove(Board board, List<Move> moves, int depth)
        {
            Move bestMove = null;
            int bestScore = int.MinValue;

            foreach (var move in moves)
            {
                var testBoard = CloneBoard(board);
                MakeMoveOnBoard(testBoard, move);

                int score = MiniMax(testBoard, depth - 1, false);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove ?? GetGreedyMove(board, moves);
        }

        // 4. Альфа-бета отсечение (оптимизированный минимакс)
        private Move GetAlphaBetaMove(Board board, List<Move> moves, int depth)
        {
            Move bestMove = null;
            int bestScore = int.MinValue;

            foreach (var move in moves)
            {
                var testBoard = CloneBoard(board);
                MakeMoveOnBoard(testBoard, move);

                int score = AlphaBeta(testBoard, depth - 1, int.MinValue, int.MaxValue, false);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove ?? GetMiniMaxMove(board, moves, depth - 1);
        }

        private int MiniMax(Board board, int depth, bool maximizingPlayer)
        {
            if (depth == 0)
                return EvaluateBoard(board);

            var currentPlayer = maximizingPlayer ? _color : GetOpponentColor(_color);
            var moves = GetAllPossibleMoves(board, currentPlayer)
                .Where(m => IsMoveValid(board, m)).ToList();

            if (!moves.Any())
            {
                if (IsKingInCheck(board, currentPlayer))
                    return maximizingPlayer ? -10000 : 10000;
                return 0;
            }

            if (maximizingPlayer)
            {
                int maxEval = int.MinValue;
                foreach (var move in moves)
                {
                    var testBoard = CloneBoard(board);
                    MakeMoveOnBoard(testBoard, move);
                    int eval = MiniMax(testBoard, depth - 1, false);
                    maxEval = Math.Max(maxEval, eval);
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (var move in moves)
                {
                    var testBoard = CloneBoard(board);
                    MakeMoveOnBoard(testBoard, move);
                    int eval = MiniMax(testBoard, depth - 1, true);
                    minEval = Math.Min(minEval, eval);
                }
                return minEval;
            }
        }

        private int AlphaBeta(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
        {
            if (depth == 0)
                return EvaluateBoard(board);

            var currentPlayer = maximizingPlayer ? _color : GetOpponentColor(_color);
            var moves = GetAllPossibleMoves(board, currentPlayer)
                .Where(m => IsMoveValid(board, m)).ToList();

            if (!moves.Any())
            {
                if (IsKingInCheck(board, currentPlayer))
                    return maximizingPlayer ? -10000 : 10000;
                return 0;
            }

            if (maximizingPlayer)
            {
                int maxEval = int.MinValue;
                foreach (var move in moves)
                {
                    var testBoard = CloneBoard(board);
                    MakeMoveOnBoard(testBoard, move);
                    int eval = AlphaBeta(testBoard, depth - 1, alpha, beta, false);
                    maxEval = Math.Max(maxEval, eval);
                    alpha = Math.Max(alpha, eval);
                    if (beta <= alpha)
                        break;
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (var move in moves)
                {
                    var testBoard = CloneBoard(board);
                    MakeMoveOnBoard(testBoard, move);
                    int eval = AlphaBeta(testBoard, depth - 1, alpha, beta, true);
                    minEval = Math.Min(minEval, eval);
                    beta = Math.Min(beta, eval);
                    if (beta <= alpha)
                        break;
                }
                return minEval;
            }
        }

        // Оценка немедленного хода (для жадного алгоритма)
        private int EvaluateImmediateMove(Board board, Move move)
        {
            int score = 0;

            // Ценность взятия фигуры
            if (move.CapturedPiece != null)
            {
                score += _pieceValues[move.CapturedPiece.Type] * 10;
            }

            // Контроль центра
            if (IsCentralSquare(move.To))
            {
                score += 3;
            }

            // Развитие фигур (поощряем ходы в начале игры)
            if (!move.Piece.HasMoved && (move.Piece.Type == PieceType.Knight || move.Piece.Type == PieceType.Bishop))
            {
                score += 2;
            }

            // Безопасность короля
            if (move.Piece.Type == PieceType.King)
            {
                // Штраф за ранние ходы королем
                score -= 5;
            }

            return score;
        }

        // Полная оценка позиции
        private int EvaluateBoard(Board board)
        {
            int score = 0;

            // 1. Материальный баланс
            score += EvaluateMaterial(board);

            // 2. Позиционная оценка
            score += EvaluatePosition(board);

            // 3. Мобильность
            score += EvaluateMobility(board);

            // 4. Безопасность короля
            score += EvaluateKingSafety(board);

            // 5. Структура пешек
            score += EvaluatePawnStructure(board);

            return score;
        }

        private int EvaluateMaterial(Board board)
        {
            int score = 0;
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var piece = board.GetPieceAt(new Position(row, col));
                    if (piece != null)
                    {
                        int value = _pieceValues[piece.Type];
                        if (piece.Color == _color)
                            score += value;
                        else
                            score -= value;
                    }
                }
            }
            return score;
        }

        private int EvaluatePosition(Board board)
        {
            int score = 0;

            // Таблицы позиционных весов для разных фигур
            int[,] pawnTable = {
                { 0,  0,  0,  0,  0,  0,  0,  0 },
                { 5, 10, 10,-20,-20, 10, 10,  5 },
                { 5, -5,-10,  0,  0,-10, -5,  5 },
                { 0,  0,  0, 20, 20,  0,  0,  0 },
                { 5,  5, 10, 25, 25, 10,  5,  5 },
                {10, 10, 20, 30, 30, 20, 10, 10 },
                {50, 50, 50, 50, 50, 50, 50, 50 },
                { 0,  0,  0,  0,  0,  0,  0,  0 }
            };

            int[,] knightTable = {
                {-50,-40,-30,-30,-30,-30,-40,-50 },
                {-40,-20,  0,  5,  5,  0,-20,-40 },
                {-30,  5, 10, 15, 15, 10,  5,-30 },
                {-30,  0, 15, 20, 20, 15,  0,-30 },
                {-30,  5, 15, 20, 20, 15,  5,-30 },
                {-30,  0, 10, 15, 15, 10,  0,-30 },
                {-40,-20,  0,  0,  0,  0,-20,-40 },
                {-50,-40,-30,-30,-30,-30,-40,-50 }
            };

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var piece = board.GetPieceAt(new Position(row, col));
                    if (piece != null)
                    {
                        int positionalScore = 0;
                        int actualRow = piece.Color == PieceColor.White ? 7 - row : row;

                        switch (piece.Type)
                        {
                            case PieceType.Pawn:
                                positionalScore = pawnTable[actualRow, col];
                                break;
                            case PieceType.Knight:
                                positionalScore = knightTable[actualRow, col];
                                break;
                            case PieceType.Bishop:
                                // Слоны лучше в центре
                                positionalScore = Math.Min(Math.Min(row, 7 - row), Math.Min(col, 7 - col)) * 2;
                                break;
                            case PieceType.Rook:
                                // Ладьи на открытых вертикалях
                                positionalScore = IsOpenFile(board, col) ? 10 : 0;
                                break;
                        }

                        if (piece.Color == _color)
                            score += positionalScore;
                        else
                            score -= positionalScore;
                    }
                }
            }

            return score;
        }

        private int EvaluateMobility(Board board)
        {
            var myMoves = GetAllPossibleMoves(board, _color).Count;
            var opponentMoves = GetAllPossibleMoves(board, GetOpponentColor(_color)).Count;
            return (myMoves - opponentMoves) * 2;
        }

        private int EvaluateKingSafety(Board board)
        {
            int score = 0;
            var kingPosition = FindKing(board, _color);

            if (kingPosition.IsValid())
            {
                // Штраф за короля на краю доски
                if (kingPosition.Row == 0 || kingPosition.Row == 7 || kingPosition.Column == 0 || kingPosition.Column == 7)
                    score -= 15;

                // Поощряем рокировку
                var king = board.GetPieceAt(kingPosition);
                if (king != null && king.HasMoved)
                    score -= 10;
            }

            return score;
        }

        private int EvaluatePawnStructure(Board board)
        {
            int score = 0;
            int doubledPawns = 0;
            int isolatedPawns = 0;

            // Простой анализ структуры пешек
            for (int col = 0; col < 8; col++)
            {
                int pawnsInFile = 0;
                for (int row = 0; row < 8; row++)
                {
                    var piece = board.GetPieceAt(new Position(row, col));
                    if (piece != null && piece.Type == PieceType.Pawn && piece.Color == _color)
                    {
                        pawnsInFile++;
                    }
                }

                if (pawnsInFile > 1) doubledPawns++;
                if (pawnsInFile > 0 && !HasFriendlyPawnsInAdjacentFiles(board, col, _color)) isolatedPawns++;
            }

            score -= doubledPawns * 5;
            score -= isolatedPawns * 10;

            return score;
        }

        private bool HasFriendlyPawnsInAdjacentFiles(Board board, int file, PieceColor color)
        {
            int leftFile = file - 1;
            int rightFile = file + 1;

            for (int row = 0; row < 8; row++)
            {
                if (leftFile >= 0)
                {
                    var piece = board.GetPieceAt(new Position(row, leftFile));
                    if (piece != null && piece.Type == PieceType.Pawn && piece.Color == color)
                        return true;
                }

                if (rightFile < 8)
                {
                    var piece = board.GetPieceAt(new Position(row, rightFile));
                    if (piece != null && piece.Type == PieceType.Pawn && piece.Color == color)
                        return true;
                }
            }

            return false;
        }

        private bool IsOpenFile(Board board, int file)
        {
            for (int row = 0; row < 8; row++)
            {
                var piece = board.GetPieceAt(new Position(row, file));
                if (piece != null && piece.Type == PieceType.Pawn)
                    return false;
            }
            return true;
        }

        // Вспомогательные методы
        private List<Move> GetAllPossibleMoves(Board board, PieceColor color)
        {
            var moves = new List<Move>();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var position = new Position(row, col);
                    var piece = board.GetPieceAt(position);

                    if (piece != null && piece.Color == color)
                    {
                        var possibleMoves = piece.GetPossibleMoves(position, board);
                        foreach (var target in possibleMoves)
                        {
                            moves.Add(new Move(position, target, piece)
                            {
                                CapturedPiece = board.GetPieceAt(target)
                            });
                        }
                    }
                }
            }

            return moves;
        }

        private bool IsMoveValid(Board board, Move move)
        {
            var testBoard = CloneBoard(board);
            MakeMoveOnBoard(testBoard, move);
            return !IsKingInCheck(testBoard, _color);
        }

        private bool IsKingInCheck(Board board, PieceColor kingColor)
        {
            Position kingPosition = FindKing(board, kingColor);
            if (!kingPosition.IsValid()) return false;

            var opponentColor = GetOpponentColor(kingColor);
            var opponentMoves = GetAllPossibleMoves(board, opponentColor);

            return opponentMoves.Any(move => move.To == kingPosition);
        }

        private Position FindKing(Board board, PieceColor color)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var position = new Position(row, col);
                    var piece = board.GetPieceAt(position);
                    if (piece != null && piece.Type == PieceType.King && piece.Color == color)
                        return position;
                }
            }
            return Position.Invalid;
        }

        private bool IsCentralSquare(Position position)
        {
            return (position.Row >= 2 && position.Row <= 5 && position.Column >= 2 && position.Column <= 5);
        }

        private PieceColor GetOpponentColor(PieceColor color)
        {
            return color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        private Board CloneBoard(Board original)
        {
            var clone = new Board();
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var originalPiece = original.GetPieceAt(new Position(row, col));
                    if (originalPiece != null)
                    {
                        ChessPiece clonedPiece = originalPiece.Type switch
                        {
                            PieceType.Pawn => new Pawn(originalPiece.Color),
                            PieceType.Rook => new Rook(originalPiece.Color),
                            PieceType.Knight => new Knight(originalPiece.Color),
                            PieceType.Bishop => new Bishop(originalPiece.Color),
                            PieceType.Queen => new Queen(originalPiece.Color),
                            PieceType.King => new King(originalPiece.Color),
                            _ => throw new NotImplementedException()
                        };
                        clonedPiece.HasMoved = originalPiece.HasMoved;
                        clone.Cells[row, col].Piece = clonedPiece;
                    }
                }
            }

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    clone.Squares[row, col] = clone.Cells[row, col].Piece;
                }
            }

            return clone;
        }

        private void MakeMoveOnBoard(Board board, Move move)
        {
            board.MovePiece(move.From, move.To);
        }
    }
}