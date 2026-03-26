using System.Collections.Generic;

namespace shahmati.models
{
    public abstract class ChessPiece
    {
        public PieceType Type { get; protected set; }
        public PieceColor Color { get; protected set; }
        public bool HasMoved { get; set; } = false;

        private string _imagePath;
        public string ImagePath
        {
            get
            {
                if (string.IsNullOrEmpty(_imagePath))
                {
                    string colorStr = Color == PieceColor.White ? "White" : "Black";
                    _imagePath = $"/Resources/Images/{colorStr}_{Type}.png";
                }
                return _imagePath;
            }
            protected set => _imagePath = value;
        }

        protected ChessPiece(PieceColor color)
        {
            Color = color;
        }

        public abstract List<Position> GetPossibleMoves(Position currentPosition, Board board);

        protected bool IsValidMove(Position newPosition, Board board)
        {
            if (newPosition.Row < 0 || newPosition.Row >= 8 ||
                newPosition.Column < 0 || newPosition.Column >= 8)
                return false;

            var pieceAtTarget = board.GetPieceAt(newPosition);
            return pieceAtTarget == null || pieceAtTarget.Color != Color;
        }

        public override string ToString() => $"{Color} {Type}";
    }

    public enum PieceType { Pawn, Rook, Knight, Bishop, Queen, King }
    public enum PieceColor { White, Black }
}