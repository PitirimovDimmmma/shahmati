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

            // Вверх-вправо
            for (int i = 1; i < 8; i++)
            {
                var newPos = new Position(currentPosition.Row - i, currentPosition.Column + i);
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

            // Вверх-влево
            for (int i = 1; i < 8; i++)
            {
                var newPos = new Position(currentPosition.Row - i, currentPosition.Column - i);
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

            // Вниз-вправо
            for (int i = 1; i < 8; i++)
            {
                var newPos = new Position(currentPosition.Row + i, currentPosition.Column + i);
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

            // Вниз-влево
            for (int i = 1; i < 8; i++)
            {
                var newPos = new Position(currentPosition.Row + i, currentPosition.Column - i);
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