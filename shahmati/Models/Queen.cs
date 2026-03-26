using System.Collections.Generic;

namespace shahmati.models
{
    public class Queen : ChessPiece
    {
        public Queen(PieceColor color) : base(color)
        {
            Type = PieceType.Queen;
            ImagePath = color == PieceColor.White ? "ChessPieces/wq.png" : "ChessPieces/bq.png";
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, Board board)
        {
            var moves = new List<Position>();

            // Все 8 направлений
            int[,] directions = {
                {-1, 0},  // вверх
                {1, 0},   // вниз
                {0, -1},  // влево
                {0, 1},   // вправо
                {-1, -1}, // вверх-влево
                {-1, 1},  // вверх-вправо
                {1, -1},  // вниз-влево
                {1, 1}    // вниз-вправо
            };

            for (int d = 0; d < directions.GetLength(0); d++)
            {
                for (int i = 1; i < 8; i++)
                {
                    var newPos = new Position(
                        currentPosition.Row + i * directions[d, 0],
                        currentPosition.Column + i * directions[d, 1]
                    );

                    if (!newPos.IsValid()) break;

                    var piece = board.GetPieceAt(newPos);
                    if (piece == null)
                    {
                        moves.Add(newPos);
                    }
                    else
                    {
                        if (piece.Color != Color) moves.Add(newPos);
                        break;
                    }
                }
            }

            return moves;
        }
    }
}