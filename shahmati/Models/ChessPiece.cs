using System.Collections.Generic;

namespace shahmati.models
{
    public abstract class ChessPiece
    {
        public PieceType Type { get; protected set; }
        public PieceColor Color { get; protected set; }
        public bool HasMoved { get; set; } = false;
        public string ImagePath { get; protected set; }

        protected ChessPiece(PieceColor color)
        {
            Color = color;
        }

        public abstract List<Position> GetPossibleMoves(Position currentPosition, Board board);

        // Добавляем метод IsValidMove обратно
        protected bool IsValidMove(Position newPosition, Board board)
        {
            // Проверка на выход за границы доски
            if (newPosition.Row < 0 || newPosition.Row >= 8 ||
                newPosition.Column < 0 || newPosition.Column >= 8)
                return false;

            var pieceAtTarget = board.GetPieceAt(newPosition);

            // Если на целевой клетке есть фигура того же цвета - ход невозможен
            if (pieceAtTarget != null && pieceAtTarget.Color == this.Color)
                return false;

            return true;
        }

        public override string ToString() => $"{Color} {Type}";
    }

    public enum PieceType { Pawn, Rook, Knight, Bishop, Queen, King }
    public enum PieceColor { White, Black }
}