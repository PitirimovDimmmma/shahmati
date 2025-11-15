using System.Collections.Generic;

namespace shahmati.models
{
    public class Bishop : ChessPiece
    {
        public Bishop(PieceColor color) : base(color)
        {
            Type = PieceType.Bishop;
            ImagePath = color == PieceColor.White ? "ChessPieces/wb.png" : "ChessPieces/bb.png";
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, Board board)
        {
            var moves = new List<Position>();

            int[,] directions = { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };

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
                    else if (piece.Color != Color)
                    {
                        moves.Add(newPos);
                        break;
                    }
                    else break;
                }
            }

            return moves;
        }
    }
}