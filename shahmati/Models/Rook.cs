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

            // Вверх
            for (int i = 1; i < 8; i++)
            {
                var newPos = new Position(currentPosition.Row - i, currentPosition.Column);
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

            // Вниз
            for (int i = 1; i < 8; i++)
            {
                var newPos = new Position(currentPosition.Row + i, currentPosition.Column);
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

            // Влево
            for (int i = 1; i < 8; i++)
            {
                var newPos = new Position(currentPosition.Row, currentPosition.Column - i);
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

            // Вправо
            for (int i = 1; i < 8; i++)
            {
                var newPos = new Position(currentPosition.Row, currentPosition.Column + i);
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

            return moves;
        }
    }
}