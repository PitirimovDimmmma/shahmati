using System.Collections.Generic;

namespace shahmati.models
{
    public class Knight : ChessPiece
    {
        public Knight(PieceColor color) : base(color)
        {
            Type = PieceType.Knight;
            ImagePath = color == PieceColor.White ? "ChessPieces/wn.png" : "ChessPieces/bn.png";
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, Board board)
        {
            var moves = new List<Position>();

            // Все возможные ходы коня (буквой Г)
            int[,] knightMoves = {
                {2, 1},   // 2 вверх, 1 вправо
                {2, -1},  // 2 вверх, 1 влево
                {-2, 1},  // 2 вниз, 1 вправо
                {-2, -1}, // 2 вниз, 1 влево
                {1, 2},   // 1 вверх, 2 вправо
                {1, -2},  // 1 вверх, 2 влево
                {-1, 2},  // 1 вниз, 2 вправо
                {-1, -2}  // 1 вниз, 2 влево
            };

            for (int i = 0; i < knightMoves.GetLength(0); i++)
            {
                var newPos = new Position(
                    currentPosition.Row + knightMoves[i, 0],
                    currentPosition.Column + knightMoves[i, 1]
                );

                if (newPos.IsValid())
                {
                    var piece = board.GetPieceAt(newPos);
                    // Конь может ходить на пустую клетку или бить вражескую фигуру
                    if (piece == null || piece.Color != Color)
                    {
                        moves.Add(newPos);
                    }
                }
            }

            return moves;
        }
    }
}