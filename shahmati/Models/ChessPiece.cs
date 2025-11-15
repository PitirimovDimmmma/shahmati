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

        protected bool IsValidMove(Position position, Board board)
        {
            if (!position.IsValid()) return false;

            var pieceAtTarget = board.GetPieceAt(position);
            return pieceAtTarget == null || pieceAtTarget.Color != this.Color;
        }

        public override string ToString() => $"{Color} {Type}";
    }

    public enum PieceType { Pawn, Rook, Knight, Bishop, Queen, King }
    public enum PieceColor { White, Black }
}