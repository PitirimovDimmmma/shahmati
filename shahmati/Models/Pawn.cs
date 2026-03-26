using System.Collections.Generic;

namespace shahmati.models
{
    public class Pawn : ChessPiece
    {
        public Pawn(PieceColor color) : base(color)
        {
            Type = PieceType.Pawn;
            ImagePath = color == PieceColor.White ? "ChessPieces/wp.png" : "ChessPieces/bp.png";
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, Board board)
        {
            var moves = new List<Position>();
            int direction = Color == PieceColor.White ? -1 : 1;
            int startRow = Color == PieceColor.White ? 6 : 1;

            // Ход вперед на 1 клетку
            var oneForward = new Position(currentPosition.Row + direction, currentPosition.Column);
            if (oneForward.IsValid() && board.GetPieceAt(oneForward) == null)
            {
                moves.Add(oneForward);

                // Ход вперед на 2 клетки (только с начальной позиции и если нет фигур на пути)
                if (currentPosition.Row == startRow)
                {
                    var twoForward = new Position(currentPosition.Row + 2 * direction, currentPosition.Column);
                    if (twoForward.IsValid() && board.GetPieceAt(twoForward) == null)
                    {
                        moves.Add(twoForward);
                    }
                }
            }

            // Взятие по диагонали влево
            var captureLeft = new Position(currentPosition.Row + direction, currentPosition.Column - 1);
            if (captureLeft.IsValid())
            {
                var piece = board.GetPieceAt(captureLeft);
                if (piece != null && piece.Color != Color)
                {
                    moves.Add(captureLeft);
                }
            }

            // Взятие по диагонали вправо
            var captureRight = new Position(currentPosition.Row + direction, currentPosition.Column + 1);
            if (captureRight.IsValid())
            {
                var piece = board.GetPieceAt(captureRight);
                if (piece != null && piece.Color != Color)
                {
                    moves.Add(captureRight);
                }
            }

            return moves;
        }
    }
}