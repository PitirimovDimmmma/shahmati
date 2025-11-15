using System.Collections.Generic;

namespace shahmati.models
{
    public class King : ChessPiece
    {
        public King(PieceColor color) : base(color)
        {
            Type = PieceType.King;
            ImagePath = color == PieceColor.White ? "ChessPieces/wk.png" : "ChessPieces/bk.png";
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, Board board)
        {
            var moves = new List<Position>();

            int[,] kingMoves = {
                {1, 0}, {-1, 0}, {0, 1}, {0, -1},
                {1, 1}, {1, -1}, {-1, 1}, {-1, -1}
            };

            for (int i = 0; i < kingMoves.GetLength(0); i++)
            {
                var newPos = new Position(
                    currentPosition.Row + kingMoves[i, 0],
                    currentPosition.Column + kingMoves[i, 1]
                );

                if (IsValidMove(newPos, board))
                    moves.Add(newPos);
            }

            return moves;
        }
    }
}