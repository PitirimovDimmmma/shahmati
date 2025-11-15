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

            int[,] knightMoves = {
                {2, 1}, {2, -1}, {-2, 1}, {-2, -1},
                {1, 2}, {1, -2}, {-1, 2}, {-1, -2}
            };

            for (int i = 0; i < knightMoves.GetLength(0); i++)
            {
                var newPos = new Position(
                    currentPosition.Row + knightMoves[i, 0],
                    currentPosition.Column + knightMoves[i, 1]
                );

                if (IsValidMove(newPos, board))
                    moves.Add(newPos);
            }

            return moves;
        }
    }
}