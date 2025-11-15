using System.Collections.Generic;

namespace shahmati.models
{
    public class Rook : ChessPiece
    {
        public Rook(PieceColor color) : base(color)
        {
            Type = PieceType.Rook;
            ImagePath = color == PieceColor.White ? "ChessPieces/wr.png" : "ChessPieces/br.png";
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, Board board)
        {
            var moves = new List<Position>();

            int[] directions = { -1, 1 };

            // Горизонталь
            foreach (var dir in directions)
            {
                for (int i = 1; i < 8; i++)
                {
                    var newPos = new Position(currentPosition.Row, currentPosition.Column + i * dir);
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

            // Вертикаль
            foreach (var dir in directions)
            {
                for (int i = 1; i < 8; i++)
                {
                    var newPos = new Position(currentPosition.Row + i * dir, currentPosition.Column);
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